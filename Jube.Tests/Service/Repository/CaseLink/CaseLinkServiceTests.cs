/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Repository.CaseLink;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseLink;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Test.Service.Repository.CaseLink
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseLinkServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly CaseRiskSeeder seeder = new(fx);

        private string User => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.CaseLink.Where(w => seeder.CaseIds.Contains(w.CaseIdLow) || seeder.CaseIds.Contains(w.CaseIdHigh))
                .DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private static Task<CaseLinkService> BuildAsync(DbContext dbContext, string user) =>
            CaseLinkService.CreateAsync(dbContext, user, TestLog.NoOp, new NullServiceChangeBus());

        private async Task<(int A, int B)> TwoVisibleCasesAsync(DbContext dbContext)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, User);

            return (await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "AccountA"),
                await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 3, "AccountB"));
        }

        [Fact]
        public async Task ALinkIsSeenFromBothCasesWithTheOtherCasesKeyAndStatusAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (a, b) = await TwoVisibleCasesAsync(dbContext);
            var service = await BuildAsync(dbContext, User);

            await service.LinkAsync(new CaseLinkRequestDto { CaseId = a, OtherCaseId = b, Reason = "  Same scam.  " });

            var fromA = (await service.ListAsync(a)).Should().ContainSingle().Subject;
            fromA.OtherCaseId.Should().Be(b);
            fromA.CaseKeyValue.Should().Be("AccountB");
            fromA.ClosedStatusId.Should().Be(3);
            fromA.Reason.Should().Be("Same scam.");
            fromA.CreatedUser.Should().Be(User);

            var fromB = (await service.ListAsync(b)).Should().ContainSingle().Subject;
            fromB.OtherCaseId.Should().Be(a);
            fromB.CaseKeyValue.Should().Be("AccountA");
        }

        [Fact]
        public async Task LinkingAndUnlinkingWriteACaseEventOnBothCasesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (a, b) = await TwoVisibleCasesAsync(dbContext);
            var service = await BuildAsync(dbContext, User);

            await service.LinkAsync(new CaseLinkRequestDto { CaseId = a, OtherCaseId = b, Reason = "Same scam." });

            var linked = await dbContext.CaseEvent
                .Where(w => (w.CaseId == a || w.CaseId == b)
                            && w.CaseEventTypeId == CaseEventRepository.CaseLinkedEventTypeId).ToListAsync();

            linked.Should().HaveCount(2);
            linked.Single(w => w.CaseId == a).After.Should().Be($"Case {b}: Same scam.");
            linked.Single(w => w.CaseId == b).After.Should().Be($"Case {a}: Same scam.");
            linked.Should().OnlyContain(w => w.CreatedUser == User);

            await service.UnlinkAsync(new CaseLinkRequestDto { CaseId = b, OtherCaseId = a });

            var unlinked = await dbContext.CaseEvent
                .Where(w => (w.CaseId == a || w.CaseId == b)
                            && w.CaseEventTypeId == CaseEventRepository.CaseUnlinkedEventTypeId).ToListAsync();

            unlinked.Should().HaveCount(2);
            unlinked.Single(w => w.CaseId == a).Before.Should().Be($"Case {b}");
            unlinked.Single(w => w.CaseId == b).Before.Should().Be($"Case {a}");
        }

        [Fact]
        public async Task LinkingTheSamePairAgainInEitherDirectionIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (a, b) = await TwoVisibleCasesAsync(dbContext);
            var service = await BuildAsync(dbContext, User);
            await service.LinkAsync(new CaseLinkRequestDto { CaseId = a, OtherCaseId = b });

            await ((Func<Task>)(() => service.LinkAsync(new CaseLinkRequestDto { CaseId = b, OtherCaseId = a })))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();
            await ((Func<Task>)(() => service.LinkAsync(new CaseLinkRequestDto { CaseId = a, OtherCaseId = b })))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();
        }

        [Fact]
        public async Task ASelfLinkAMissingCaseAndAnOverlongReasonAreRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (a, b) = await TwoVisibleCasesAsync(dbContext);
            var service = await BuildAsync(dbContext, User);

            await ((Func<Task>)(() => service.LinkAsync(new CaseLinkRequestDto { CaseId = a, OtherCaseId = a })))
                .Should().ThrowAsync<ArgumentException>();
            await ((Func<Task>)(() => service.LinkAsync(new CaseLinkRequestDto { CaseId = 0, OtherCaseId = b })))
                .Should().ThrowAsync<ArgumentException>();
            await ((Func<Task>)(() => service.LinkAsync(new CaseLinkRequestDto
                { CaseId = a, OtherCaseId = b, Reason = new string('x', 501) }))).Should()
                .ThrowAsync<ArgumentException>();
            await ((Func<Task>)(() => service.LinkAsync(new CaseLinkRequestDto { CaseId = a, OtherCaseId = int.MaxValue })))
                .Should().ThrowAsync<CaseExceptions.NotFoundException>();

            (await service.ListAsync(a)).Should().BeEmpty();
        }

        [Fact]
        public async Task LinkingNeedsThePermissionAndBothCasesMustBeVisibleToTheCallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (a, b) = await TwoVisibleCasesAsync(dbContext);

            var without = await BuildAsync(dbContext, fx.Seed.UserWithoutPermission);
            await ((Func<Task>)(() => without.LinkAsync(new CaseLinkRequestDto { CaseId = a, OtherCaseId = b })))
                .Should().ThrowAsync<CaseExceptions.ForbiddenException>();

            var otherTenant = await BuildAsync(dbContext, fx.Seed.UserTenantB);
            await ((Func<Task>)(() => otherTenant.LinkAsync(new CaseLinkRequestDto { CaseId = a, OtherCaseId = b })))
                .Should().ThrowAsync<CaseExceptions.NotFoundException>();
            await ((Func<Task>)(() => otherTenant.ListAsync(a))).Should().ThrowAsync<CaseExceptions.NotFoundException>();
        }

        [Fact]
        public async Task ALinkedCaseTheCallerCannotSeeIsNeverRevealedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (a, _) = await TwoVisibleCasesAsync(dbContext);
            var hiddenWorkflow = await seeder.SeedWorkflowAsync(dbContext);
            var hidden = await seeder.SeedCaseAsync(dbContext, hiddenWorkflow, hiddenWorkflow.Neutral,
                caseKeyValue: "SecretAccount");
            await new CaseLinkRepository(dbContext).InsertAsync(a, hidden, "Made by someone with wider access.",
                "other.user");
            var service = await BuildAsync(dbContext, User);

            (await service.ListAsync(a)).Should().BeEmpty();
        }

        [Fact]
        public async Task UnlinkingRemovesTheLinkFromBothSidesAndUnlinkingAgainIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (a, b) = await TwoVisibleCasesAsync(dbContext);
            var service = await BuildAsync(dbContext, User);
            await service.LinkAsync(new CaseLinkRequestDto { CaseId = a, OtherCaseId = b });

            await service.UnlinkAsync(new CaseLinkRequestDto { CaseId = b, OtherCaseId = a });

            (await service.ListAsync(a)).Should().BeEmpty();
            (await service.ListAsync(b)).Should().BeEmpty();
            await ((Func<Task>)(() => service.UnlinkAsync(new CaseLinkRequestDto { CaseId = a, OtherCaseId = b })))
                .Should().ThrowAsync<CaseExceptions.NotFoundException>();
        }
    }
}

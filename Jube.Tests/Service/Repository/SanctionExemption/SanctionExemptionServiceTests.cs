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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Repository.SanctionExemption;
using Jube.Service.Exceptions.Repository.SanctionExemption;
using Jube.Service.Reactivity;
using Jube.Service.Repository.SanctionExemption;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.Service.Repository.SanctionExemption
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SanctionExemptionServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly string marker = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}"[..24];

        private string Approver => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.SanctionExemption.Where(w => w.ScreenedName.StartsWith(marker)).DeleteAsync();
        }

        private static Task<SanctionExemptionService> BuildAsync(DbContext dbContext, string user) =>
            SanctionExemptionService.CreateAsync(dbContext, user, TestLog.NoOp, new NullServiceChangeBus());

        private static async Task<int> SourceAsync(DbContext dbContext) =>
            (await dbContext.SanctionEntrySource.OrderBy(o => o.Id).FirstAsync()).Id;

        private async Task<int> TenantAsync(DbContext dbContext, string user) =>
            (await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, user)).Required();

        private async Task<SanctionExemptionRequestDto> RequestDtoAsync(DbContext dbContext, string suffix = "a",
            string reference = "R1")
        {
            return new SanctionExemptionRequestDto
            {
                ScreenedName = $"{marker} Petrov {suffix}",
                SanctionEntrySourceId = await SourceAsync(dbContext),
                SanctionEntryReference = reference,
                Reason = "A different person: date of birth does not match.",
                ExpiryDate = DateTimeOffset.UtcNow.AddDays(30)
            };
        }

        private async Task<int> PendingBySomeoneElseAsync(DbContext dbContext, string user, string suffix = "x")
        {
            var row = new Jube.Data.Poco.SanctionExemption
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = await TenantAsync(dbContext, user),
                ScreenedName = $"{marker} Other {suffix}",
                NameKey = $"{marker}-{suffix}".ToLowerInvariant(),
                SanctionEntrySourceId = await SourceAsync(dbContext),
                SanctionEntryReference = "R9",
                Reason = "Seeded.",
                StateId = SanctionExemptionRepository.Pending,
                RequestedUser = "someone.else",
                RequestedDate = DateTime.UtcNow
            };

            return (await new SanctionExemptionRepository(dbContext).InsertAsync(row)).Required().Id;
        }

        [Fact]
        public async Task ARequestIsStoredPendingWithTheNormalisedNameAndTheRequesterAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, Approver);

            var created = await service.RequestAsync(await RequestDtoAsync(dbContext));

            created.StateId.Should().Be(SanctionExemptionRepository.Pending);
            created.RequestedUser.Should().Be(Approver);
            var row = await dbContext.SanctionExemption.SingleAsync(w => w.Id == created.Id);
            row.NameKey.Should().Be(Jube.Engine.Sanctions.SanctionExemptionSet.NameKey(row.ScreenedName));
            row.TenantRegistryId.Should().Be(await TenantAsync(dbContext, Approver));
            (await service.ListAsync(null)).Should().Contain(d => d.Id == created.Id);
        }

        [Fact]
        public async Task ARequestWithoutTheSanctionsPermissionIsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, fx.Seed.UserWithoutPermission);

            await ((Func<Task>)(async () => await service.RequestAsync(await RequestDtoAsync(dbContext)))).Should()
                .ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task ARequestThatIsIncompleteInThePastOrForAnUnknownListIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, Approver);

            async Task<SanctionExemptionRequestDto> Dto(Action<SanctionExemptionRequestDto> change)
            {
                var dto = await RequestDtoAsync(dbContext);
                change(dto);

                return dto;
            }

            foreach (var bad in new[]
                     {
                         await Dto(d => d.ScreenedName = " "),
                         await Dto(d => d.ScreenedName = new string('x', 501)),
                         await Dto(d => d.ScreenedName = "-- ."),
                         await Dto(d => d.SanctionEntryReference = null),
                         await Dto(d => d.Reason = ""),
                         await Dto(d => d.Reason = new string('x', 1001)),
                         await Dto(d => d.ExpiryDate = DateTimeOffset.UtcNow.AddDays(-1)),
                         await Dto(d => d.SanctionEntrySourceId = int.MaxValue)
                     })
            {
                await ((Func<Task>)(() => service.RequestAsync(bad))).Should().ThrowAsync<ArgumentException>();
            }

            (await dbContext.SanctionExemption.CountAsync(w => w.ScreenedName.StartsWith(marker))).Should().Be(0);
        }

        [Fact]
        public async Task AnExemptionAlreadyPendingOrApprovedForTheSameNameAndEntryIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, Approver);
            await service.RequestAsync(await RequestDtoAsync(dbContext));

            var again = await RequestDtoAsync(dbContext);
            again.ScreenedName = again.ScreenedName!.ToUpperInvariant();

            await ((Func<Task>)(() => service.RequestAsync(again))).Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task AnotherApproverApprovesAndTheRequesterCannotApproveTheirOwnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, Approver);
            var own = await service.RequestAsync(await RequestDtoAsync(dbContext, "own"));
            var other = await PendingBySomeoneElseAsync(dbContext, Approver);

            await ((Func<Task>)(() => service.DecideAsync(new SanctionExemptionDecisionDto { Id = own.Id }, true)))
                .Should().ThrowAsync<ForbiddenException>();
            (await dbContext.SanctionExemption.SingleAsync(w => w.Id == own.Id)).StateId
                .Should().Be(SanctionExemptionRepository.Pending);

            await service.DecideAsync(new SanctionExemptionDecisionDto { Id = other, Note = "Checked." }, true);

            var approved = await dbContext.SanctionExemption.SingleAsync(w => w.Id == other);
            approved.StateId.Should().Be(SanctionExemptionRepository.Approved);
            approved.DecidedUser.Should().Be(Approver);
            approved.DecisionNote.Should().Be("Checked.");
            approved.DecidedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task RejectingRecordsTheDecisionAndFreesTheNameAndEntryForANewRequestAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, Approver);
            var other = await PendingBySomeoneElseAsync(dbContext, Approver, "rej");

            await service.DecideAsync(new SanctionExemptionDecisionDto { Id = other }, false);

            (await dbContext.SanctionExemption.SingleAsync(w => w.Id == other)).StateId
                .Should().Be(SanctionExemptionRepository.Rejected);

            var row = await dbContext.SanctionExemption.SingleAsync(w => w.Id == other);
            var again = new SanctionExemptionRequestDto
            {
                ScreenedName = row.ScreenedName, SanctionEntrySourceId = row.SanctionEntrySourceId,
                SanctionEntryReference = row.SanctionEntryReference, Reason = "Second attempt."
            };
            (await service.RequestAsync(again)).StateId.Should().Be(SanctionExemptionRepository.Pending);
        }

        [Fact]
        public async Task OnlyAnApproverMayDecideAndADecidedExemptionCannotBeDecidedAgainAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var other = await PendingBySomeoneElseAsync(dbContext, Approver, "perm");

            var analyst = await BuildAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);
            await ((Func<Task>)(() => analyst.DecideAsync(new SanctionExemptionDecisionDto { Id = other }, true)))
                .Should().ThrowAsync<ForbiddenException>();

            var service = await BuildAsync(dbContext, Approver);
            await service.DecideAsync(new SanctionExemptionDecisionDto { Id = other }, true);

            await ((Func<Task>)(() => service.DecideAsync(new SanctionExemptionDecisionDto { Id = other }, false)))
                .Should().ThrowAsync<ConflictException>();
            await ((Func<Task>)(() => service.DecideAsync(new SanctionExemptionDecisionDto { Id = int.MaxValue },
                true))).Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task AnotherTenantCannotSeeOrDecideAnExemptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var other = await PendingBySomeoneElseAsync(dbContext, Approver, "tenant");
            var tenantB = await BuildAsync(dbContext, fx.Seed.UserTenantB);

            (await tenantB.ListAsync(null)).Should().NotContain(d => d.Id == other);
            await ((Func<Task>)(() => tenantB.DecideAsync(new SanctionExemptionDecisionDto { Id = other }, true)))
                .Should().ThrowAsync<NotFoundException>();
            await ((Func<Task>)(() => tenantB.RetractAsync(other))).Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task TheRequesterCanRetractAndSoCanAnApproverButNotAnUninvolvedAnalystAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, Approver);
            var mine = await service.RequestAsync(await RequestDtoAsync(dbContext, "mine"));
            var theirs = await PendingBySomeoneElseAsync(dbContext, Approver, "theirs");
            var analyst = await BuildAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            await ((Func<Task>)(() => analyst.RetractAsync(theirs))).Should().ThrowAsync<ForbiddenException>();

            await service.RetractAsync(mine.Id);
            await service.RetractAsync(theirs);

            (await dbContext.SanctionExemption.Where(w => w.Id == mine.Id || w.Id == theirs)
                .Select(s => s.StateId).ToListAsync())
                .Should().OnlyContain(s => s == SanctionExemptionRepository.Retracted);
            await ((Func<Task>)(() => service.RetractAsync(mine.Id))).Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task AnApprovedExemptionCanBeRetractedAndThenLeavesTheApprovedListAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, Approver);
            var other = await PendingBySomeoneElseAsync(dbContext, Approver, "appr");
            await service.DecideAsync(new SanctionExemptionDecisionDto { Id = other }, true);
            var repository = new SanctionExemptionRepository(dbContext);

            (await repository.ListApprovedAsync()).Should().Contain(r => r.Id == other);

            await service.RetractAsync(other);

            (await repository.ListApprovedAsync()).Should().NotContain(r => r.Id == other);
        }
    }
}

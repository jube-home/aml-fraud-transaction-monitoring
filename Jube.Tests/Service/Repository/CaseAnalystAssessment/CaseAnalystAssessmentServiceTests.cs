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
using Jube.Dto.Options;
using Jube.Dto.Repository.CaseAnalystAssessment;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseAnalystAssessment;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Test.Service.Repository.CaseAnalystAssessment
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseAnalystAssessmentServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly CaseRiskSeeder seeder = new(fx);

        private string User => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.CaseAnalystAssessment.Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await dbContext.CaseEvent.Where(w => w.CaseId != null && seeder.CaseIds.Contains(w.CaseId.Value))
                .DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private static Task<CaseAnalystAssessmentService> BuildAsync(DbContext dbContext, string user) =>
            CaseAnalystAssessmentService.CreateAsync(dbContext, user, TestLog.NoOp, new NullServiceChangeBus());

        private async Task<int> CaseAsync(DbContext dbContext, params string[] alsoGranted)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, [User, .. alsoGranted]);

            return await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
        }

        private static CaseAnalystAssessmentDto View(int caseId, CaseAnalystLikelihood likelihood, string reason) =>
            new() { CaseId = caseId, LikelihoodId = (int)likelihood, Reason = reason };

        [Fact]
        public async Task AnAnalystRecordsTheirViewBesideTheComputedLikelihoodAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await CaseAsync(dbContext);

            await dbContext.Case.Where(c => c.Id == caseId).Set(c => c.RiskSequence, 4)
                .Set(c => c.RiskLikelihoodProbability, 0.12).UpdateAsync();

            var service = await BuildAsync(dbContext, User);
            var recorded = await service.RecordAsync(View(caseId, CaseAnalystLikelihood.High, " Known mule pattern. "));

            (recorded.LikelihoodId, recorded.Reason, recorded.CreatedUser, recorded.Mine, recorded.WithdrawnDate)
                .Should().Be((3, "Known mule pattern.", User, true, null));
            recorded.ComputedLikelihoodProbability.Should().Be(0.12);

            var stored = await dbContext.CaseAnalystAssessment.SingleAsync(w => w.CaseId == caseId);
            (stored.ComputedSequence, stored.ComputedLikelihoodProbability).Should().Be(((int?)4, (double?)0.12));

            var state = await service.ListAsync(caseId);
            state.Current!.Guid.Should().Be(recorded.Guid);
            state.History.Should().ContainSingle();
        }

        [Fact]
        public async Task RecordingAViewChangesNothingAboutTheRankingOfTheCaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await CaseAsync(dbContext);

            await dbContext.Case.Where(c => c.Id == caseId).Set(c => c.RiskSequence, 2)
                .Set(c => c.RiskLikelihoodProbability, 0.4).Set(c => c.RiskPriority, 31).UpdateAsync();

            var before = await dbContext.Case.SingleAsync(c => c.Id == caseId);
            var assessments = await dbContext.CaseRiskAssessment.CountAsync(a => a.CaseId == caseId);

            await (await BuildAsync(dbContext, User)).RecordAsync(View(caseId, CaseAnalystLikelihood.Low, "Benign."));

            var after = await dbContext.Case.SingleAsync(c => c.Id == caseId);

            after.Should().BeEquivalentTo(before, options => options.Excluding(c => c.CaseWorkflow)
                .Excluding(c => c.CaseWorkflowStatus).Excluding(c => c.ChecklistStaleDate));
            (await dbContext.CaseRiskAssessment.CountAsync(a => a.CaseId == caseId)).Should().Be(assessments);
        }

        [Fact]
        public async Task ALaterViewBecomesCurrentAndWithdrawingItRestoresTheOneBeforeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await CaseAsync(dbContext);
            var service = await BuildAsync(dbContext, User);

            var first = await service.RecordAsync(View(caseId, CaseAnalystLikelihood.Low, "First look."));
            var second = await service.RecordAsync(View(caseId, CaseAnalystLikelihood.High, "New evidence."));

            (await service.ListAsync(caseId)).Current!.Guid.Should().Be(second.Guid);

            await service.WithdrawAsync(new CaseAnalystAssessmentWithdrawDto { Guid = second.Guid });
            await service.WithdrawAsync(new CaseAnalystAssessmentWithdrawDto { Guid = second.Guid });

            var state = await service.ListAsync(caseId);
            state.Current!.Guid.Should().Be(first.Guid);
            state.History.Select(h => h.Guid).Should().Equal(second.Guid, first.Guid);
            state.History[0].WithdrawnDate.Should().NotBeNull();

            await service.WithdrawAsync(new CaseAnalystAssessmentWithdrawDto { Guid = first.Guid });

            (await service.ListAsync(caseId)).Current.Should().BeNull();
        }

        [Fact]
        public async Task RecordingAndWithdrawingEachWriteOneCaseEventHoldingOnlyEnumerationIdsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await CaseAsync(dbContext);
            var service = await BuildAsync(dbContext, User);

            await service.RecordAsync(View(caseId, CaseAnalystLikelihood.Low, "First look."));
            var second = await service.RecordAsync(View(caseId, CaseAnalystLikelihood.High, "New evidence."));

            await service.WithdrawAsync(new CaseAnalystAssessmentWithdrawDto { Guid = second.Guid });
            await service.WithdrawAsync(new CaseAnalystAssessmentWithdrawDto { Guid = second.Guid });

            var events = await dbContext.CaseEvent.Where(e => e.CaseId == caseId).OrderBy(e => e.Id).ToListAsync();

            events.Select(e => (e.CaseEventTypeId, Before: (string?)e.Before, After: (string?)e.After, e.CreatedUser))
                .Should().Equal(
                    ((int?)34, (string?)null, (string?)"1", User),
                    ((int?)34, (string?)"1", (string?)"3", User),
                    ((int?)35, (string?)"3", (string?)null, User));
            events.Should().OnlyContain(e => e.CaseKeyValue == "K1");
        }

        [Fact]
        public async Task OnlyTheAuthorCanWithdrawAViewAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await CaseAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);
            var recorded = await (await BuildAsync(dbContext, User))
                .RecordAsync(View(caseId, CaseAnalystLikelihood.Medium, "Unsure."));

            var other = await BuildAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            (await other.ListAsync(caseId)).Current!.Mine.Should().BeFalse();

            await ((Func<Task>)(() => other.WithdrawAsync(new CaseAnalystAssessmentWithdrawDto { Guid = recorded.Guid })))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();

            (await dbContext.CaseAnalystAssessment.SingleAsync(w => w.Guid == recorded.Guid)).WithdrawnDate
                .Should().BeNull();
        }

        [Theory]
        [InlineData(0, "A reason.")]
        [InlineData(4, "A reason.")]
        [InlineData(2, "")]
        [InlineData(2, "   ")]
        public async Task ALikelihoodOutsideOneToThreeOrAMissingReasonIsRefusedAsync(int likelihood, string reason)
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await CaseAsync(dbContext);
            var service = await BuildAsync(dbContext, User);

            await ((Func<Task>)(() => service.RecordAsync(new CaseAnalystAssessmentDto
                    { CaseId = caseId, LikelihoodId = likelihood, Reason = reason })))
                .Should().ThrowAsync<ArgumentException>();

            (await dbContext.CaseAnalystAssessment.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
        }

        [Fact]
        public async Task AReasonOverTheLimitAndAMissingCaseAreRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await CaseAsync(dbContext);
            var service = await BuildAsync(dbContext, User);

            await ((Func<Task>)(() => service.RecordAsync(View(caseId, CaseAnalystLikelihood.Low,
                    new string('x', CaseAnalystAssessmentService.MaximumReason + 1)))))
                .Should().ThrowAsync<ArgumentException>();

            await ((Func<Task>)(() => service.RecordAsync(View(int.MaxValue, CaseAnalystLikelihood.Low, "x"))))
                .Should().ThrowAsync<CaseExceptions.NotFoundException>();

            await ((Func<Task>)(() => service.WithdrawAsync(new CaseAnalystAssessmentWithdrawDto { Guid = Guid.NewGuid() })))
                .Should().ThrowAsync<CaseExceptions.NotFoundException>();
        }

        [Fact]
        public async Task PermissionAndTenantAreEnforcedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await CaseAsync(dbContext);
            var recorded = await (await BuildAsync(dbContext, User))
                .RecordAsync(View(caseId, CaseAnalystLikelihood.High, "Mine."));
            var view = View(caseId, CaseAnalystLikelihood.Low, "Theirs.");
            var withdraw = new CaseAnalystAssessmentWithdrawDto { Guid = recorded.Guid };

            var without = await BuildAsync(dbContext, fx.Seed.UserWithoutPermission);
            await ((Func<Task>)(() => without.RecordAsync(view))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            await ((Func<Task>)(() => without.ListAsync(caseId))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            await ((Func<Task>)(() => without.WithdrawAsync(withdraw))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();

            var otherTenant = await BuildAsync(dbContext, fx.Seed.UserTenantB);
            await ((Func<Task>)(() => otherTenant.RecordAsync(view))).Should().ThrowAsync<CaseExceptions.NotFoundException>();
            await ((Func<Task>)(() => otherTenant.ListAsync(caseId))).Should().ThrowAsync<CaseExceptions.NotFoundException>();
            await ((Func<Task>)(() => otherTenant.WithdrawAsync(withdraw))).Should().ThrowAsync<CaseExceptions.NotFoundException>();

            (await dbContext.CaseAnalystAssessment.SingleAsync(w => w.CaseId == caseId)).WithdrawnDate.Should().BeNull();
        }
    }
}

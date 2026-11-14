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
using Jube.Dto.Options;
using Jube.Dto.Repository.CaseAgentRun;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseAgentRun;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Test.Service.Repository.CaseAgentRun
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseAgentRunServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly CaseRiskSeeder seeder = new(fx);

        private Guid workflowGuid;

        private string User => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.CaseAgentRun.Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await dbContext.CaseEvent.Where(w => seeder.CaseIds.Contains(w.CaseId.Value)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private static Task<CaseAgentRunService> BuildAsync(DbContext dbContext, string user) =>
            CaseAgentRunService.CreateAsync(dbContext, user, TestLog.NoOp, new NullServiceChangeBus());

        private async Task<(int CaseId, Guid Run)> CaseWithRunAsync(DbContext dbContext)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            workflowGuid = workflow.WorkflowGuid;
            await seeder.GrantAsync(dbContext, workflow, User);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var run = Guid.NewGuid();

            await dbContext.InsertAsync(new Data.Poco.CaseAgentRun
            {
                Guid = run, TenantRegistryId = (await Jube.Data.Repository.UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, User))!.Value, CaseId = caseId, RecommendedOutcomeId = 1,
                Rationale = "Why.", Evidence = "[\"fact one\"]", FactsHash = new string('a', 64),
                AgentDefinitionHash = new string('b', 64), StateId = (short)CaseAgentRunState.Pending,
                CreatedDate = DateTime.UtcNow, ModelName = "model-x"
            });

            return (caseId, run);
        }

        [Fact]
        public async Task TheRecommendationsOfAVisibleCaseAreListedWithTheirEvidenceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (caseId, run) = await CaseWithRunAsync(dbContext);

            var listed = await (await BuildAsync(dbContext, User)).ListAsync(caseId);

            var item = listed.Should().ContainSingle().Subject;
            item.Guid.Should().Be(run);
            item.Evidence.Should().Equal("fact one");
            item.StateId.Should().Be((int)CaseAgentRunState.Pending);
            item.ModelName.Should().Be("model-x");
        }

        [Fact]
        public async Task AcceptingRecordsTheAnalystAndCannotBeDoneTwiceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (caseId, run) = await CaseWithRunAsync(dbContext);
            var service = await BuildAsync(dbContext, User);

            await service.DecideAsync(new CaseAgentRunDecisionDto { Guid = run, Accept = true });

            var row = await dbContext.CaseAgentRun.SingleAsync(w => w.Guid == run);
            (row.StateId, row.DecidedUser).Should().Be(((short)CaseAgentRunState.Accepted, User));
            row.DecidedDate.Should().NotBeNull();

            await ((Func<Task>)(() => service.DecideAsync(new CaseAgentRunDecisionDto { Guid = run, Accept = false, Reason = "x" })))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();
        }

        [Fact]
        public async Task OverridingNeedsAReasonAndStoresItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, run) = await CaseWithRunAsync(dbContext);
            var service = await BuildAsync(dbContext, User);

            await ((Func<Task>)(() => service.DecideAsync(new CaseAgentRunDecisionDto { Guid = run, Accept = false })))
                .Should().ThrowAsync<ArgumentException>();
            await ((Func<Task>)(() => service.DecideAsync(new CaseAgentRunDecisionDto
                    { Guid = run, Accept = false, Reason = new string('x', 501) })))
                .Should().ThrowAsync<ArgumentException>();

            await service.DecideAsync(new CaseAgentRunDecisionDto { Guid = run, Accept = false, Reason = " Known customer. " });

            var row = await dbContext.CaseAgentRun.SingleAsync(w => w.Guid == run);
            (row.StateId, row.OverrideReason).Should().Be(((short)CaseAgentRunState.Overridden, "Known customer."));
        }

        [Fact]
        public async Task ADecisionNeverChangesTheCaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (caseId, run) = await CaseWithRunAsync(dbContext);
            var before = await dbContext.Case.Where(w => w.Id == caseId).Select(s => new { s.ClosedStatusId, s.CaseWorkflowStatusGuid }).SingleAsync();

            await (await BuildAsync(dbContext, User)).DecideAsync(new CaseAgentRunDecisionDto { Guid = run, Accept = true });

            var after = await dbContext.Case.Where(w => w.Id == caseId).Select(s => new { s.ClosedStatusId, s.CaseWorkflowStatusGuid }).SingleAsync();
            after.Should().BeEquivalentTo(before);
        }

        [Fact]
        public async Task AcceptingAndOverridingEachWriteATimelineEventWithTheOutcomeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (accepted, acceptedRun) = await CaseWithRunAsync(dbContext);
            var (overridden, overriddenRun) = await CaseWithRunAsync(dbContext);
            var service = await BuildAsync(dbContext, User);

            await service.DecideAsync(new CaseAgentRunDecisionDto { Guid = acceptedRun, Accept = true });
            await service.DecideAsync(new CaseAgentRunDecisionDto { Guid = overriddenRun, Accept = false, Reason = "Known customer." });

            var accept = await dbContext.CaseEvent.SingleAsync(w => w.CaseId == accepted && w.CaseEventTypeId == 30);
            (accept.Before, accept.After, accept.CreatedUser).Should().Be(("1", null, User));

            var over = await dbContext.CaseEvent.SingleAsync(w => w.CaseId == overridden && w.CaseEventTypeId == 31);
            (over.Before, over.After).Should().Be(("1", "Known customer."));
        }

        [Fact]
        public async Task AnOverrideDrawsTheCaseAgainForQualityReviewButAnAcceptDoesNotAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (overridden, overriddenRun) = await CaseWithRunAsync(dbContext);
            await dbContext.CaseWorkflow.Where(w => w.Guid == workflowGuid).Set(s => s.RiskQaSampleRate, 1d).UpdateAsync();
            var (accepted, acceptedRun) = await CaseWithRunAsync(dbContext);
            await dbContext.CaseWorkflow.Where(w => w.Guid == workflowGuid).Set(s => s.RiskQaSampleRate, 1d).UpdateAsync();
            var service = await BuildAsync(dbContext, User);

            await service.DecideAsync(new CaseAgentRunDecisionDto { Guid = acceptedRun, Accept = true });
            (await dbContext.Case.Where(w => w.Id == accepted).Select(s => s.QaSampleStatusId).SingleAsync()).Should().Be(0);

            await service.DecideAsync(new CaseAgentRunDecisionDto { Guid = overriddenRun, Accept = false, Reason = "No." });
            var row = await dbContext.Case.Where(w => w.Id == overridden).SingleAsync();
            row.QaSampleStatusId.Should().Be(2);
            row.QaSampleDate.Should().NotBeNull();
            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == overridden && w.CaseEventTypeId == 20)).Should().Be(1);
        }

        [Fact]
        public async Task AWorkflowWithQaSamplingTurnedOffIsNeverBoostedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (caseId, run) = await CaseWithRunAsync(dbContext);
            await dbContext.CaseWorkflow.Where(w => w.Guid == workflowGuid).Set(s => s.RiskQaSampleRate, 0d).UpdateAsync();

            await (await BuildAsync(dbContext, User)).DecideAsync(new CaseAgentRunDecisionDto { Guid = run, Accept = false, Reason = "No." });

            (await dbContext.Case.Where(w => w.Id == caseId).Select(s => s.QaSampleStatusId).SingleAsync()).Should().Be(0);
        }

        [Fact]
        public async Task PermissionTenantAndUnknownGuidAreRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (caseId, run) = await CaseWithRunAsync(dbContext);

            var without = await BuildAsync(dbContext, fx.Seed.UserWithoutPermission);
            await ((Func<Task>)(() => without.ListAsync(caseId))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            await ((Func<Task>)(() => without.DecideAsync(new CaseAgentRunDecisionDto { Guid = run, Accept = true })))
                .Should().ThrowAsync<CaseExceptions.ForbiddenException>();

            var otherTenant = await BuildAsync(dbContext, fx.Seed.UserTenantB);
            await ((Func<Task>)(() => otherTenant.ListAsync(caseId))).Should().ThrowAsync<CaseExceptions.NotFoundException>();
            await ((Func<Task>)(() => otherTenant.DecideAsync(new CaseAgentRunDecisionDto { Guid = run, Accept = true })))
                .Should().ThrowAsync<CaseExceptions.NotFoundException>();

            var service = await BuildAsync(dbContext, User);
            await ((Func<Task>)(() => service.DecideAsync(new CaseAgentRunDecisionDto { Guid = Guid.NewGuid(), Accept = true })))
                .Should().ThrowAsync<CaseExceptions.NotFoundException>();
        }
    }
}

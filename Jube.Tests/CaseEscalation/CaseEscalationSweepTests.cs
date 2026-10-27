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
using Jube.Dto.Options;
using Jube.Engine.CaseEscalation;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.CaseEscalation
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseEscalationSweepTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly CaseRiskSeeder seeder = new(fx);

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GetTable<CaseStatusTransition>().Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private static Task<CaseEscalationSweepResult> SweepAsync(DbContext dbContext,
            Func<Jube.Data.Poco.Case, CaseWorkflowStatus, Task>? afterMove = null) =>
            CaseEscalationSweep.RunAsync(dbContext, DateTime.UtcNow, 1000, TestLog.NoOp, afterMove);

        private static async Task RuleAsync(DbContext dbContext, Guid source, Guid target, CaseEscalationTrigger trigger,
            int? threshold, bool releaseLock = false, byte sourcePriority = 3, byte targetPriority = 1)
        {
            await dbContext.CaseWorkflowStatus.Where(w => w.Guid == target)
                .Set(s => s.Priority, targetPriority).UpdateAsync();
            await dbContext.CaseWorkflowStatus.Where(w => w.Guid == source)
                .Set(s => s.Priority, sourcePriority)
                .Set(s => s.EscalationTriggerId, (byte?)trigger)
                .Set(s => s.EscalationThreshold, threshold)
                .Set(s => s.EscalationTargetCaseWorkflowStatusGuid, target)
                .Set(s => s.EscalationReleaseLock, (byte)(releaseLock ? 1 : 0))
                .UpdateAsync();
        }

        private async Task<int> CaseInAsync(DbContext dbContext, CaseRiskSeeder.Seeded workflow, Guid status,
            int minutesInStatus = 0, byte closedStatusId = 0, int ageMinutes = 0)
        {
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, status, closedStatusId);

            if (ageMinutes > 0)
            {
                await dbContext.Case.Where(w => w.Id == caseId)
                    .Set(s => s.CreatedDate, DateTime.UtcNow.AddMinutes(-ageMinutes)).UpdateAsync();
            }

            await new CaseStatusTransitionRepository(dbContext).EnterAsync(caseId, workflow.WorkflowGuid, status,
                CaseTransitionEnterReason.Created, null, DateTime.UtcNow.AddMinutes(-minutesInStatus));

            return caseId;
        }

        private static Task<Jube.Data.Poco.Case> ReadAsync(DbContext dbContext, int caseId) =>
            dbContext.Case.FirstAsync(w => w.Id == caseId);

        [Fact]
        public async Task ACaseThatHasStayedInAStatusLongEnoughMovesOnAndTheRecordShowsWhyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await RuleAsync(dbContext, workflow.Neutral, workflow.Confirmed, CaseEscalationTrigger.TimeInStatus, 240);
            var overdue = await CaseInAsync(dbContext, workflow, workflow.Neutral, 300);
            var fresh = await CaseInAsync(dbContext, workflow, workflow.Neutral, 60);

            var result = await SweepAsync(dbContext);

            result.Moved.Should().BeGreaterThanOrEqualTo(1);
            (await ReadAsync(dbContext, overdue)).CaseWorkflowStatusGuid.Should().Be(workflow.Confirmed);
            (await ReadAsync(dbContext, fresh)).CaseWorkflowStatusGuid.Should().Be(workflow.Neutral);

            var stays = await new CaseStatusTransitionRepository(dbContext).ListForCaseAsync(overdue);
            stays.Should().HaveCount(2);
            stays[0].ExitedDate.Should().NotBeNull();
            stays[1].CaseWorkflowStatusGuid.Should().Be(workflow.Confirmed);
            stays[1].EnterReasonId.Should().Be((byte)CaseTransitionEnterReason.Escalated);
            stays[1].EnteredUser.Should().Be(CaseStatusMover.AutomationUser);
            stays[1].PreviousCaseWorkflowStatusGuid.Should().Be(workflow.Neutral);

            var events = await dbContext.CaseEvent.Where(w => w.CaseId == overdue).ToListAsync();
            var escalation = events.Should().ContainSingle().Which;
            escalation.CaseEventTypeId.Should().Be(CaseStatusMover.EscalationEventTypeId);
            escalation.CreatedUser.Should().Be(CaseStatusMover.AutomationUser);
            escalation.Before.Should().StartWith(DatabaseFixture.Prefix);
            escalation.After.Should().StartWith(DatabaseFixture.Prefix);
            escalation.Before.Should().NotBe(escalation.After);
            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == fresh)).Should().Be(0);
        }

        [Fact]
        public async Task OnlyOpenCasesInTheRuleStatusAreEverMovedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await RuleAsync(dbContext, workflow.Neutral, workflow.Confirmed, CaseEscalationTrigger.TimeInStatus, 1);
            var open = await CaseInAsync(dbContext, workflow, workflow.Neutral, 600);
            var suspended = await CaseInAsync(dbContext, workflow, workflow.Neutral, 600, 1);
            var closed = await CaseInAsync(dbContext, workflow, workflow.Neutral, 600, 3);
            var elsewhere = await CaseInAsync(dbContext, workflow, workflow.NotConfirmed, 600);

            var result = await SweepAsync(dbContext);

            result.Examined.Should().Be(1);
            (await ReadAsync(dbContext, open)).CaseWorkflowStatusGuid.Should().Be(workflow.Confirmed);
            (await ReadAsync(dbContext, suspended)).CaseWorkflowStatusGuid.Should().Be(workflow.Neutral);
            (await ReadAsync(dbContext, closed)).CaseWorkflowStatusGuid.Should().Be(workflow.Neutral);
            (await ReadAsync(dbContext, elsewhere)).CaseWorkflowStatusGuid.Should().Be(workflow.NotConfirmed);
        }

        [Fact]
        public async Task ReleasingTheLockIsOptionalAndOtherwiseTheAnalystKeepsTheCaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var kept = await CaseInAsync(dbContext, workflow, workflow.Neutral, 600);
            await dbContext.Case.Where(w => w.Id == kept).Set(s => s.Locked, (byte?)1)
                .Set(s => s.LockedUser, "alice").Set(s => s.LockedDate, DateTime.UtcNow).UpdateAsync();

            await RuleAsync(dbContext, workflow.Neutral, workflow.Confirmed, CaseEscalationTrigger.TimeInStatus, 1);
            await SweepAsync(dbContext);

            var stillHeld = await ReadAsync(dbContext, kept);
            stillHeld.CaseWorkflowStatusGuid.Should().Be(workflow.Confirmed);
            stillHeld.Locked.Should().Be(1);
            stillHeld.LockedUser.Should().Be("alice");

            var released = await CaseInAsync(dbContext, workflow, workflow.NotConfirmed, 600);
            await dbContext.Case.Where(w => w.Id == released).Set(s => s.Locked, (byte?)1)
                .Set(s => s.LockedUser, "alice").Set(s => s.LockedDate, DateTime.UtcNow).UpdateAsync();
            await RuleAsync(dbContext, workflow.NotConfirmed, workflow.Confirmed, CaseEscalationTrigger.TimeInStatus, 1,
                true, 4);
            await SweepAsync(dbContext);

            var free = await ReadAsync(dbContext, released);
            free.CaseWorkflowStatusGuid.Should().Be(workflow.Confirmed);
            free.Locked.Should().Be(0);
            free.LockedUser.Should().BeNull();
            free.LockedDate.Should().BeNull();
        }

        [Fact]
        public async Task ACaseOlderThanTheWorkflowResolutionTargetMovesAndNoTargetMeansNoRuleAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await RuleAsync(dbContext, workflow.Neutral, workflow.Confirmed,
                CaseEscalationTrigger.ResolutionTargetBreached, null);
            var old = await CaseInAsync(dbContext, workflow, workflow.Neutral, 0, 0, 120);
            var young = await CaseInAsync(dbContext, workflow, workflow.Neutral, 0, 0, 10);

            await SweepAsync(dbContext);
            (await ReadAsync(dbContext, old)).CaseWorkflowStatusGuid.Should().Be(workflow.Neutral);

            await dbContext.CaseWorkflow.Where(w => w.Guid == workflow.WorkflowGuid)
                .Set(s => s.ResolutionTargetMinutes, 60).UpdateAsync();
            await SweepAsync(dbContext);

            (await ReadAsync(dbContext, old)).CaseWorkflowStatusGuid.Should().Be(workflow.Confirmed);
            (await ReadAsync(dbContext, young)).CaseWorkflowStatusGuid.Should().Be(workflow.Neutral);
        }

        [Fact]
        public async Task TheLanePriorityAndDeadlineTriggersReadOnlyTheLatestAssessmentAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var urgent = await CaseInAsync(dbContext, workflow, workflow.Neutral);
            var calm = await CaseInAsync(dbContext, workflow, workflow.Neutral);
            var superseded = await CaseInAsync(dbContext, workflow, workflow.Neutral);
            await seeder.AssessAsync(dbContext, urgent, 2, 80);
            await seeder.AssessAsync(dbContext, calm, 4, 20);
            var old = await seeder.AssessAsync(dbContext, superseded, 1, 95);
            await dbContext.CaseRiskAssessment.Where(w => w.CaseId == superseded && w.Sequence == old.Sequence)
                .Set(s => s.DeadlineBreached, (byte)1).UpdateAsync();
            await seeder.AssessAsync(dbContext, superseded, 4, 10);

            await RuleAsync(dbContext, workflow.Neutral, workflow.Confirmed, CaseEscalationTrigger.LaneAtLeastUrgent, 2);
            await SweepAsync(dbContext);
            (await ReadAsync(dbContext, urgent)).CaseWorkflowStatusGuid.Should().Be(workflow.Confirmed);
            (await ReadAsync(dbContext, calm)).CaseWorkflowStatusGuid.Should().Be(workflow.Neutral);
            (await ReadAsync(dbContext, superseded)).CaseWorkflowStatusGuid.Should().Be(workflow.Neutral);

            await RuleAsync(dbContext, workflow.Neutral, workflow.Confirmed, CaseEscalationTrigger.PriorityAtLeast, 50);
            var high = await CaseInAsync(dbContext, workflow, workflow.Neutral);
            await seeder.AssessAsync(dbContext, high, 4, 50);
            await SweepAsync(dbContext);
            (await ReadAsync(dbContext, high)).CaseWorkflowStatusGuid.Should().Be(workflow.Confirmed);
            (await ReadAsync(dbContext, calm)).CaseWorkflowStatusGuid.Should().Be(workflow.Neutral);
            (await ReadAsync(dbContext, superseded)).CaseWorkflowStatusGuid.Should().Be(workflow.Neutral);

            await RuleAsync(dbContext, workflow.Neutral, workflow.Confirmed, CaseEscalationTrigger.DeadlineOverdue, null);
            var late = await CaseInAsync(dbContext, workflow, workflow.Neutral);
            var assessment = await seeder.AssessAsync(dbContext, late, 3, 30);
            await dbContext.CaseRiskAssessment.Where(w => w.CaseId == late && w.Sequence == assessment.Sequence)
                .Set(s => s.DeadlineBreached, (byte)1).UpdateAsync();
            await SweepAsync(dbContext);
            (await ReadAsync(dbContext, late)).CaseWorkflowStatusGuid.Should().Be(workflow.Confirmed);
            (await ReadAsync(dbContext, calm)).CaseWorkflowStatusGuid.Should().Be(workflow.Neutral);
            (await ReadAsync(dbContext, superseded)).CaseWorkflowStatusGuid.Should().Be(workflow.Neutral);
        }

        [Fact]
        public async Task ARuleWhoseTargetIsNotAStrongerPriorityOrNotInTheWorkflowIsSkippedAndNeverLoopsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var other = await seeder.SeedWorkflowAsync(dbContext);
            var caseId = await CaseInAsync(dbContext, workflow, workflow.Neutral, 600);

            await RuleAsync(dbContext, workflow.Neutral, workflow.Confirmed, CaseEscalationTrigger.TimeInStatus, 1, false,
                2, 2);
            var same = await SweepAsync(dbContext);
            same.Skipped.Should().BeGreaterThanOrEqualTo(1);
            (await ReadAsync(dbContext, caseId)).CaseWorkflowStatusGuid.Should().Be(workflow.Neutral);

            await RuleAsync(dbContext, workflow.Neutral, workflow.Confirmed, CaseEscalationTrigger.TimeInStatus, 1, false,
                1, 3);
            await SweepAsync(dbContext);
            (await ReadAsync(dbContext, caseId)).CaseWorkflowStatusGuid.Should().Be(workflow.Neutral);

            await RuleAsync(dbContext, workflow.Neutral, other.Confirmed, CaseEscalationTrigger.TimeInStatus, 1);
            await SweepAsync(dbContext);
            (await ReadAsync(dbContext, caseId)).CaseWorkflowStatusGuid.Should().Be(workflow.Neutral);

            await dbContext.CaseWorkflowStatus.Where(w => w.Guid == workflow.Confirmed)
                .Set(s => s.Active, (byte?)0).UpdateAsync();
            await RuleAsync(dbContext, workflow.Neutral, workflow.Confirmed, CaseEscalationTrigger.TimeInStatus, 1);
            await SweepAsync(dbContext);
            (await ReadAsync(dbContext, caseId)).CaseWorkflowStatusGuid.Should().Be(workflow.Neutral);
        }

        [Fact]
        public async Task TwoRulesPointingBackAtEachOtherMoveACaseOnceAndThenLeaveItAloneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var caseId = await CaseInAsync(dbContext, workflow, workflow.Neutral, 600);

            await RuleAsync(dbContext, workflow.Neutral, workflow.Confirmed, CaseEscalationTrigger.TimeInStatus, 1);
            await RuleAsync(dbContext, workflow.Confirmed, workflow.Neutral, CaseEscalationTrigger.TimeInStatus, 1, false, 1, 3);

            await SweepAsync(dbContext);
            await SweepAsync(dbContext);
            await SweepAsync(dbContext);

            (await ReadAsync(dbContext, caseId)).CaseWorkflowStatusGuid.Should().Be(workflow.Confirmed);
            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == caseId)).Should().Be(1);
        }

        [Fact]
        public async Task ChainedRulesCarryACaseOnStepByStepAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var caseId = await CaseInAsync(dbContext, workflow, workflow.Neutral, 600);

            await RuleAsync(dbContext, workflow.Neutral, workflow.NotConfirmed, CaseEscalationTrigger.TimeInStatus, 1, false, 4, 2);
            await RuleAsync(dbContext, workflow.NotConfirmed, workflow.Confirmed, CaseEscalationTrigger.TimeInStatus, 1, false, 2);

            await SweepAsync(dbContext);
            await dbContext.GetTable<CaseStatusTransition>().Where(w => w.CaseId == caseId && w.ExitedDate == null)
                .Set(s => s.EnteredDate, DateTime.UtcNow.AddMinutes(-30)).UpdateAsync();
            await SweepAsync(dbContext);

            (await ReadAsync(dbContext, caseId)).CaseWorkflowStatusGuid.Should().Be(workflow.Confirmed);
            (await new CaseStatusTransitionRepository(dbContext).ListForCaseAsync(caseId)).Should().HaveCount(3);
        }

        [Fact]
        public async Task AMoveIsAtomicAgainstTheCaseHavingAlreadyLeftTheStatusAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var caseId = await CaseInAsync(dbContext, workflow, workflow.Neutral, 5);
            var move = new CaseMove(caseId, workflow.WorkflowGuid, "AccountId", "K1", workflow.Neutral, "From",
                workflow.Confirmed, "To", false);

            (await CaseStatusMover.MoveAsync(dbContext, move, DateTime.UtcNow)).Should().BeTrue();
            (await CaseStatusMover.MoveAsync(dbContext, move, DateTime.UtcNow)).Should().BeFalse();

            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == caseId)).Should().Be(1);
            (await new CaseStatusTransitionRepository(dbContext).ListForCaseAsync(caseId)).Should().HaveCount(2);
        }

        [Fact]
        public async Task TheAfterMoveCallbackRunsOncePerMovedCaseAndItsFailureNeverUndoesTheMoveAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await RuleAsync(dbContext, workflow.Neutral, workflow.Confirmed, CaseEscalationTrigger.TimeInStatus, 1);
            var first = await CaseInAsync(dbContext, workflow, workflow.Neutral, 600);
            var second = await CaseInAsync(dbContext, workflow, workflow.Neutral, 600);
            var seen = new System.Collections.Concurrent.ConcurrentBag<int>();

            var result = await SweepAsync(dbContext, (moved, target) =>
            {
                if (!new[] { first, second }.Contains(moved.Id))
                {
                    return Task.CompletedTask;
                }

                seen.Add(moved.Id);
                target.Guid.Should().Be(workflow.Confirmed);

                return moved.Id == first ? throw new InvalidOperationException("notification down") : Task.CompletedTask;
            });

            seen.Should().BeEquivalentTo(new[] { first, second });
            result.Failed.Should().BeGreaterThanOrEqualTo(1);
            (await ReadAsync(dbContext, first)).CaseWorkflowStatusGuid.Should().Be(workflow.Confirmed);
            (await ReadAsync(dbContext, second)).CaseWorkflowStatusGuid.Should().Be(workflow.Confirmed);
        }
    }
}

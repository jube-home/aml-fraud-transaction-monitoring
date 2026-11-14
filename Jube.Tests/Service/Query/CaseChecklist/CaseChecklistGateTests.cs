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
using Jube.Dto.Options;
using Jube.Dto.Query.CaseChecklist;
using Jube.Engine.CaseEscalation;
using Jube.Service.Exceptions.Query.CaseChecklist;
using Jube.Service.Query.CaseChecklist;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;
using ChecklistIncompleteException = Jube.Service.Exceptions.Repository.Case.ChecklistIncompleteException;

namespace Jube.Test.Service.Query.CaseChecklist
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseChecklistGateTests : CaseChecklistDemoTestBase, IDisposable
    {
        private readonly ICaseChecklistBypass? previousBypass;

        public CaseChecklistGateTests(DatabaseFixture fx) : base(fx)
        {
            previousBypass = CaseChecklistBypassRegistry.Current;
            CaseChecklistBypassRegistry.Current = new ServiceCaseChecklistBypass(DynamicEnvironment, TestLog.NoOp);
        }

        public void Dispose()
        {
            CaseChecklistBypassRegistry.Current = previousBypass!;
        }

        private static ManualUtcTimeProvider Recent() => new(DateTime.UtcNow.AddMinutes(-10));

        private async Task<(int CaseId, CaseChecklistService Holder)> OpenCaseAsync(
            Jube.Data.Context.DbContext dbContext, string status = FirstLine, decimal amount = 100)
        {
            var caseId = await CreateCaseAsync(dbContext, status, UniqueKey(), amount, Users.Holder);
            var holder = await ChecklistAsync(dbContext, Users.Holder, new ManualUtcTimeProvider(Start));
            await holder.GetAsync(caseId);

            return (caseId, holder);
        }

        [Fact]
        public async Task AStatusMoveWithShownItemsOpenIsRefusedAndTheRefusalIsAuditedOutsideTheRefusedUpdate()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, _) = await OpenCaseAsync(dbContext);
            var supervisor = await StatusGuidAsync(dbContext, Supervisor);
            var cases = await CasesAsync(dbContext, Users.Holder);

            var refusal = (await FluentActions.Awaiting(() => cases.UpdateAsync(
                Move(caseId, supervisor, 0, Users.Holder))).Should().ThrowAsync<ChecklistIncompleteException>()).Which;

            refusal.Code.Should().Be("ChecklistIncomplete");
            refusal.OpenItems.Should().Equal("Review the triggering transactions", "Record an investigation note");
            refusal.Message.Should().Contain("2 checklist item(s)");
            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).CaseWorkflowStatusGuid
                .Should().Be(await StatusGuidAsync(dbContext, FirstLine));
            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.GateBlocked)).Should().Be(1);
            (await TimelineCountAsync(dbContext, caseId, CaseChecklistService.GateBlockedEventTypeId)).Should().Be(1);
        }

        [Fact]
        public async Task ClosingWithShownItemsOpenIsRefused()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, _) = await OpenCaseAsync(dbContext);
            var current = await StatusGuidAsync(dbContext, FirstLine);
            var cases = await CasesAsync(dbContext, Users.Holder);

            await FluentActions.Awaiting(() => cases.UpdateAsync(Move(caseId, current, 3, Users.Holder))).Should()
                .ThrowAsync<ChecklistIncompleteException>();

            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).ClosedStatusId.Should().Be(0);
            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.GateBlocked)).Should().Be(1);
        }

        [Fact]
        public async Task TheMoveIsAllowedOnceEveryShownItemIsTickedAndHiddenOnesDoNotCount()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, holder) = await OpenCaseAsync(dbContext);
            await AddNoteAsync(dbContext, caseId, Users.Holder);
            await holder.CheckAsync(caseId, await ItemIdAsync(dbContext, caseId, "Review the triggering transactions"));
            await holder.CheckAsync(caseId, await ItemIdAsync(dbContext, caseId, "Record an investigation note"));
            var supervisor = await StatusGuidAsync(dbContext, Supervisor);
            var cases = await CasesAsync(dbContext, Users.Holder);

            await cases.UpdateAsync(Move(caseId, supervisor, 0, Users.Holder));

            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).CaseWorkflowStatusGuid.Should().Be(supervisor);
            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.GateBlocked)).Should().Be(0);
        }

        [Fact]
        public async Task AStatusWithNoChecklistsIsNotGated()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, "Reported", UniqueKey(), 100, Users.Holder);
            var restricted = await StatusGuidAsync(dbContext, "Restricted");
            var cases = await CasesAsync(dbContext, Users.Holder);

            await cases.UpdateAsync(Move(caseId, restricted, 0, Users.Holder));

            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).CaseWorkflowStatusGuid.Should().Be(restricted);
        }

        [Fact]
        public async Task EditsThatDoNotMoveOrCloseTheCaseAreNotGated()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, _) = await OpenCaseAsync(dbContext);
            var current = await StatusGuidAsync(dbContext, FirstLine);
            var cases = await CasesAsync(dbContext, Users.Holder);
            var edit = Move(caseId, current, 0, Users.Holder);
            edit.Rating = 3;

            await cases.UpdateAsync(edit);

            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).Rating.Should().Be(3);
        }

        [Fact]
        public async Task ReopeningAClosedCaseIsNotGatedEvenWithOpenRows()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder, 3);
            var current = await StatusGuidAsync(dbContext, FirstLine);

            await dbContext.InsertAsync(new Data.Poco.CaseChecklistItem
            {
                CaseId = caseId,
                CaseWorkflowChecklistItemGuid = Guid.NewGuid(),
                CaseWorkflowChecklistGuid = Guid.NewGuid(),
                CaseWorkflowStatusGuid = current,
                ChecklistName = "zz",
                ItemName = "zz",
                StateId = (byte)CaseChecklistItemState.Shown,
                FirstShownDate = Start,
                ShownDate = Start,
                CreatedDate = Start
            });

            var cases = await CasesAsync(dbContext, Users.Holder);

            await cases.UpdateAsync(Move(caseId, current, 0, Users.Holder));

            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).ClosedStatusId.Should().Be(0);
        }

        [Fact]
        public async Task TheHolderOfTheCaseCannotOverrideItsGateEvenWithThePermission()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Administrator);
            var admin = await ChecklistAsync(dbContext, Administrator, new ManualUtcTimeProvider(Start));
            await admin.GetAsync(caseId);

            await FluentActions.Awaiting(() => admin.OverrideAsync(caseId,
                new CaseChecklistOverrideRequestDto { Reason = "Urgent." })).Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task AnOverrideNeedsAReasonOfSensibleLengthAndSomethingOpen()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, holder) = await OpenCaseAsync(dbContext);
            var admin = await ChecklistAsync(dbContext, Administrator, new ManualUtcTimeProvider(Start));

            await FluentActions.Awaiting(() => admin.OverrideAsync(caseId,
                new CaseChecklistOverrideRequestDto { Reason = "  " })).Should().ThrowAsync<DtoValidationException>();
            await FluentActions.Awaiting(() => admin.OverrideAsync(caseId, null)).Should()
                .ThrowAsync<DtoValidationException>();
            await FluentActions.Awaiting(() => admin.OverrideAsync(caseId,
                new CaseChecklistOverrideRequestDto { Reason = new string('x', 1001) })).Should()
                .ThrowAsync<DtoValidationException>();

            var reported = await CreateCaseAsync(dbContext, "Reported", UniqueKey(), 100, Users.Holder);

            await FluentActions.Awaiting(() => admin.OverrideAsync(reported,
                new CaseChecklistOverrideRequestDto { Reason = "Nothing to do." })).Should()
                .ThrowAsync<ConflictException>();
            (await dbContext.CaseChecklistOverride.CountAsync(w => w.CaseId == caseId || w.CaseId == reported))
                .Should().Be(0);
            _ = holder;
        }

        [Fact]
        public async Task AGrantIsConsumedByTheHoldersMoveAndTheNextMoveIsGatedAgain()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, holder) = await OpenCaseAsync(dbContext);
            var admin = await ChecklistAsync(dbContext, Administrator, Recent());

            var granted = await admin.OverrideAsync(caseId,
                new CaseChecklistOverrideRequestDto { Reason = "Customer verified by phone." });

            granted.OpenItems.Should().Equal("Review the triggering transactions", "Record an investigation note");
            await FluentActions.Awaiting(() => admin.OverrideAsync(caseId,
                new CaseChecklistOverrideRequestDto { Reason = "Again." })).Should().ThrowAsync<ConflictException>();
            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.GateOverridden)).Should().Be(1);
            (await TimelineCountAsync(dbContext, caseId, CaseChecklistService.GateOverriddenEventTypeId))
                .Should().Be(1);
            (await dbContext.CaseChecklistItem.CountAsync(w => w.CaseId == caseId
                && w.StateId == (byte)CaseChecklistItemState.Done)).Should().Be(0);

            var supervisor = await StatusGuidAsync(dbContext, Supervisor);
            var fraudulent = await StatusGuidAsync(dbContext, "Fraudulent");
            var cases = await CasesAsync(dbContext, Users.Holder);

            await cases.UpdateAsync(Move(caseId, supervisor, 0, Users.Holder));

            var grant = await dbContext.CaseChecklistOverride.FirstAsync(w => w.CaseId == caseId);
            grant.ConsumedUser.Should().Be(Users.Holder);
            grant.ConsumedDate.Should().NotBeNull();
            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).CaseWorkflowStatusGuid.Should().Be(supervisor);

            await holder.GetAsync(caseId);

            await FluentActions.Awaiting(() => cases.UpdateAsync(Move(caseId, fraudulent, 0, Users.Holder)))
                .Should().ThrowAsync<ChecklistIncompleteException>();
        }

        [Fact]
        public async Task AGrantIsDeadOnceTheCaseHasEnteredAnotherStatusEvenIfItComesBack()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, _) = await OpenCaseAsync(dbContext);
            var admin = await ChecklistAsync(dbContext, Administrator, Recent());
            await admin.OverrideAsync(caseId, new CaseChecklistOverrideRequestDto { Reason = "Customer verified." });

            var first = await StatusGuidAsync(dbContext, FirstLine);
            var second = await StatusGuidAsync(dbContext, Supervisor);
            var workflowGuid = await dbContext.CaseWorkflow.Where(w => w.Name == WorkflowName).Select(s => s.Guid)
                .FirstAsync();
            var row = await dbContext.Case.FirstAsync(w => w.Id == caseId);

            (await CaseStatusMover.MoveAsync(dbContext, new CaseMove(caseId, workflowGuid, row.CaseKey,
                row.CaseKeyValue, first, FirstLine, second, Supervisor, false), DateTime.UtcNow)).Should().BeTrue();
            (await CaseStatusMover.MoveAsync(dbContext, new CaseMove(caseId, workflowGuid, row.CaseKey,
                row.CaseKeyValue, second, Supervisor, first, FirstLine, false), DateTime.UtcNow)).Should().BeTrue();

            var cases = await CasesAsync(dbContext, Users.Holder);

            await FluentActions.Awaiting(() => cases.UpdateAsync(Move(caseId, second, 0, Users.Holder))).Should()
                .ThrowAsync<ChecklistIncompleteException>();
            (await dbContext.CaseChecklistOverride.FirstAsync(w => w.CaseId == caseId)).ConsumedDate.Should().BeNull();
        }

        [Fact]
        public async Task TheUserWhoGrantedAnOverrideCannotBeTheOneWhoUsesIt()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var admin = await ChecklistAsync(dbContext, Administrator, Recent());
            await admin.GetAsync(caseId);
            await admin.OverrideAsync(caseId, new CaseChecklistOverrideRequestDto { Reason = "Customer verified." });
            var supervisor = await StatusGuidAsync(dbContext, Supervisor);
            var cases = await CasesAsync(dbContext, Administrator);

            await FluentActions.Awaiting(() => cases.UpdateAsync(Move(caseId, supervisor, 0, null))).Should()
                .ThrowAsync<ChecklistIncompleteException>();
        }

        [Fact]
        public async Task AutomationMovesTheCaseWithItemsOpenAndRecordsTheBypass()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, _) = await OpenCaseAsync(dbContext);
            var first = await StatusGuidAsync(dbContext, FirstLine);
            var second = await StatusGuidAsync(dbContext, Supervisor);
            var workflowGuid = await dbContext.CaseWorkflow.Where(w => w.Name == WorkflowName).Select(s => s.Guid)
                .FirstAsync();
            var row = await dbContext.Case.FirstAsync(w => w.Id == caseId);

            var moved = await CaseStatusMover.MoveAsync(dbContext, new CaseMove(caseId, workflowGuid, row.CaseKey,
                row.CaseKeyValue, first, FirstLine, second, Supervisor, false), DateTime.UtcNow);

            moved.Should().BeTrue();
            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).CaseWorkflowStatusGuid.Should().Be(second);
            var audit = await dbContext.CaseChecklistEvent.FirstAsync(w => w.CaseId == caseId
                && w.ActionId == (byte)CaseChecklistAction.GateBypassed);
            audit.Actor.Should().Be(CaseChecklistBypassSignal.EscalationActor);
            audit.Detail.Should().Contain("Review the triggering transactions");
            audit.CaseEventId.Should().NotBeNull();
            (await TimelineCountAsync(dbContext, caseId, CaseChecklistService.GateBypassedEventTypeId)).Should().Be(1);
        }

        [Fact]
        public async Task AutomationThatLostTheRaceMovesNothingAndRecordsNoBypass()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, _) = await OpenCaseAsync(dbContext);
            var first = await StatusGuidAsync(dbContext, FirstLine);
            var second = await StatusGuidAsync(dbContext, Supervisor);
            var workflowGuid = await dbContext.CaseWorkflow.Where(w => w.Name == WorkflowName).Select(s => s.Guid)
                .FirstAsync();
            var row = await dbContext.Case.FirstAsync(w => w.Id == caseId);

            await dbContext.Case.Where(w => w.Id == caseId).Set(s => s.CaseWorkflowStatusGuid, second).UpdateAsync();

            (await CaseStatusMover.MoveAsync(dbContext, new CaseMove(caseId, workflowGuid, row.CaseKey,
                row.CaseKeyValue, first, FirstLine, second, Supervisor, false), DateTime.UtcNow)).Should().BeFalse();

            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.GateBypassed)).Should().Be(0);
        }

        [Fact]
        public async Task AutomationMovingACaseWithNothingOpenRecordsNoBypass()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, "Reported", UniqueKey(), 100);
            var from = await StatusGuidAsync(dbContext, "Reported");
            var to = await StatusGuidAsync(dbContext, "Restricted");
            var workflowGuid = await dbContext.CaseWorkflow.Where(w => w.Name == WorkflowName).Select(s => s.Guid)
                .FirstAsync();
            var row = await dbContext.Case.FirstAsync(w => w.Id == caseId);

            (await CaseStatusMover.MoveAsync(dbContext, new CaseMove(caseId, workflowGuid, row.CaseKey,
                row.CaseKeyValue, from, "Reported", to, "Restricted", false), DateTime.UtcNow)).Should().BeTrue();

            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.GateBypassed)).Should().Be(0);
        }

        [Fact]
        public async Task AFailureRecordingTheBypassNeverStopsTheAutomatedMove()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, _) = await OpenCaseAsync(dbContext);
            var first = await StatusGuidAsync(dbContext, FirstLine);
            var second = await StatusGuidAsync(dbContext, Supervisor);
            var workflowGuid = await dbContext.CaseWorkflow.Where(w => w.Name == WorkflowName).Select(s => s.Guid)
                .FirstAsync();
            var row = await dbContext.Case.FirstAsync(w => w.Id == caseId);
            CaseChecklistBypassRegistry.Current = new ThrowingChecklistBypass();

            var moved = await CaseStatusMover.MoveAsync(dbContext, new CaseMove(caseId, workflowGuid, row.CaseKey,
                row.CaseKeyValue, first, FirstLine, second, Supervisor, false), DateTime.UtcNow);

            moved.Should().BeTrue();
            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).CaseWorkflowStatusGuid.Should().Be(second);
        }
    }
}

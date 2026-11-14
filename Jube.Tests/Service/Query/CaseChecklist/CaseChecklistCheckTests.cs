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
using Jube.Service.Exceptions.Query.CaseChecklist;
using Jube.Service.Query.CaseChecklist;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.Service.Query.CaseChecklist
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseChecklistCheckTests(DatabaseFixture fx) : CaseChecklistDemoTestBase(fx)
    {
        private const string Unconditional = "Review the triggering transactions";
        private const string NeedsANote = "Record an investigation note";
        private const string Related = "Check related cases on the case key";

        [Fact]
        public async Task CheckingAnItemWithNoValidationTicksItAndWritesOneAuditAndOneTimelineEvent()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var clock = new ManualUtcTimeProvider(Start);
            var service = await ChecklistAsync(dbContext, Users.Holder, clock);
            await service.GetAsync(caseId);
            var itemId = await ItemIdAsync(dbContext, caseId, Unconditional);

            clock.Advance(TimeSpan.FromMinutes(5));
            var result = await service.CheckAsync(caseId, itemId);

            var row = await dbContext.CaseChecklistItem.FirstAsync(w => w.Id == itemId);
            row.StateId.Should().Be((byte)CaseChecklistItemState.Done);
            row.CompletedUser.Should().Be(Users.Holder);
            row.CompletedDate.Should().Be(Start.AddMinutes(5));
            result.Done.Should().Be(1);
            result.Open.Should().Be(1);
            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.Checked)).Should().Be(1);
            (await TimelineCountAsync(dbContext, caseId, CaseChecklistService.CheckedEventTypeId)).Should().Be(1);

            var audit = await dbContext.CaseChecklistEvent.FirstAsync(w => w.CaseId == caseId
                && w.ActionId == (byte)CaseChecklistAction.Checked);
            audit.Actor.Should().Be(Users.Holder);
            audit.CaseEventId.Should().NotBeNull();
            (await dbContext.CaseEvent.FirstAsync(w => w.Id == audit.CaseEventId)).After.Should()
                .Be(audit.Id.ToString());
        }

        [Fact]
        public async Task AFailingValidationReturnsItsMessageAuditsTheRefusalAndLeavesNoTimelineEvent()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var service = await ChecklistAsync(dbContext, Users.Holder, new ManualUtcTimeProvider(Start));
            await service.GetAsync(caseId);
            var itemId = await ItemIdAsync(dbContext, caseId, NeedsANote);

            var failure = (await FluentActions.Awaiting(() => service.CheckAsync(caseId, itemId)).Should()
                .ThrowAsync<DtoValidationException>()).Which;

            failure.Result.Errors.Should().ContainSingle().Which.ErrorMessage.Should()
                .Be("Add at least one note to the case before ticking this item.");
            (await dbContext.CaseChecklistItem.FirstAsync(w => w.Id == itemId)).StateId
                .Should().Be((byte)CaseChecklistItemState.Shown);
            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.ValidationFailed)).Should().Be(1);
            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.Checked)).Should().Be(0);
            (await TimelineCountAsync(dbContext, caseId, CaseChecklistService.CheckedEventTypeId)).Should().Be(0);
            (await dbContext.CaseChecklistEvent.FirstAsync(w => w.CaseId == caseId
                && w.ActionId == (byte)CaseChecklistAction.ValidationFailed)).CaseEventId.Should().BeNull();
        }

        [Fact]
        public async Task APassingValidationTicksTheItemOnce()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var service = await ChecklistAsync(dbContext, Users.Holder, new ManualUtcTimeProvider(Start));
            await service.GetAsync(caseId);
            var itemId = await ItemIdAsync(dbContext, caseId, NeedsANote);
            await AddNoteAsync(dbContext, caseId, Users.Holder);

            await service.CheckAsync(caseId, itemId);

            (await dbContext.CaseChecklistItem.FirstAsync(w => w.Id == itemId)).StateId
                .Should().Be((byte)CaseChecklistItemState.Done);
            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.Checked)).Should().Be(1);
        }

        [Fact]
        public async Task AValidationConditionThatErrorsCountsAsARefusalNotAnError()
        {
            await using var dbContext = Fx.GetDbContext();
            var statusGuid = await StatusGuidAsync(dbContext, "Restricted");
            var checklistGuid = Guid.NewGuid();
            var itemGuid = Guid.NewGuid();

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowChecklist
            {
                Guid = checklistGuid, CaseWorkflowStatusGuid = statusGuid, Name = "zzChkErrors", Priority = 0,
                Active = 1, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = "zzChk"
            });
            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowChecklistItem
            {
                Guid = itemGuid, CaseWorkflowChecklistGuid = checklistGuid, Name = "zzChkErrorItem", Priority = 0,
                EnableValidation = 1, ValidateSql = "select 1 / 0 = 1", Active = 1, Deleted = 0, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = "zzChk"
            });

            try
            {
                var caseId = await CreateCaseAsync(dbContext, "Restricted", UniqueKey(), 100, Users.Holder);
                var service = await ChecklistAsync(dbContext, Users.Holder, new ManualUtcTimeProvider(Start));
                await service.GetAsync(caseId);
                var itemId = await ItemIdAsync(dbContext, caseId, "zzChkErrorItem");

                var failure = (await FluentActions.Awaiting(() => service.CheckAsync(caseId, itemId)).Should()
                    .ThrowAsync<DtoValidationException>()).Which;

                failure.Result.Errors.Should().ContainSingle().Which.ErrorMessage.Should()
                    .Be("The condition for ticking this item is not met.");
                (await dbContext.CaseChecklistItem.FirstAsync(w => w.Id == itemId)).StateId
                    .Should().Be((byte)CaseChecklistItemState.Shown);

                var audit = await dbContext.CaseChecklistEvent.FirstAsync(w => w.CaseId == caseId
                    && w.ActionId == (byte)CaseChecklistAction.ValidationFailed);
                audit.Detail.Should().Contain("failed");
            }
            finally
            {
                await dbContext.CaseChecklistEvent.Where(w => w.CaseWorkflowChecklistItemGuid == itemGuid)
                    .DeleteAsync();
                await dbContext.CaseChecklistItem.Where(w => w.CaseWorkflowChecklistItemGuid == itemGuid)
                    .DeleteAsync();
                await dbContext.CaseWorkflowChecklistItem.Where(w => w.Guid == itemGuid).DeleteAsync();
                await dbContext.CaseWorkflowChecklist.Where(w => w.Guid == checklistGuid).DeleteAsync();
            }
        }

        [Fact]
        public async Task OnlyTheHolderOfTheCaseCanCheckOrUncheck()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var clock = new ManualUtcTimeProvider(Start);
            await (await ChecklistAsync(dbContext, Users.Holder, clock)).GetAsync(caseId);
            var itemId = await ItemIdAsync(dbContext, caseId, Unconditional);

            var other = await ChecklistAsync(dbContext, Users.Other, clock);

            await FluentActions.Awaiting(() => other.CheckAsync(caseId, itemId)).Should()
                .ThrowAsync<ConflictException>();
            await FluentActions.Awaiting(() => other.UncheckAsync(caseId, itemId)).Should()
                .ThrowAsync<ConflictException>();

            var unlocked = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            await (await ChecklistAsync(dbContext, Users.Holder, clock)).GetAsync(unlocked);
            var unlockedItem = await ItemIdAsync(dbContext, unlocked, Unconditional);

            await FluentActions.Awaiting(() => other.CheckAsync(unlocked, unlockedItem)).Should()
                .ThrowAsync<ConflictException>();
            (await dbContext.CaseChecklistEvent.CountAsync(w => w.ActionId == (byte)CaseChecklistAction.Checked
                && (w.CaseId == caseId || w.CaseId == unlocked))).Should().Be(0);
        }

        [Fact]
        public async Task UncheckingPutsTheItemBackToShownWithItsClockUnchangedAndAuditsIt()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var clock = new ManualUtcTimeProvider(Start);
            var service = await ChecklistAsync(dbContext, Users.Holder, clock);
            await service.GetAsync(caseId);
            var itemId = await ItemIdAsync(dbContext, caseId, Unconditional);
            await AddNoteAsync(dbContext, caseId, Users.Holder);
            var noteId = await ItemIdAsync(dbContext, caseId, NeedsANote);
            await service.CheckAsync(caseId, noteId);
            var before = await dbContext.CaseChecklistItem.FirstAsync(w => w.Id == noteId);

            clock.Advance(TimeSpan.FromMinutes(90));
            var result = await service.UncheckAsync(caseId, noteId);

            var after = await dbContext.CaseChecklistItem.FirstAsync(w => w.Id == noteId);
            after.StateId.Should().Be((byte)CaseChecklistItemState.Shown);
            after.CompletedDate.Should().BeNull();
            after.CompletedUser.Should().BeNull();
            after.DueDate.Should().Be(before.DueDate);
            after.FirstShownDate.Should().Be(before.FirstShownDate);
            result.Checklists[0].Items.Single(s => s.Id == noteId).SlaStateId
                .Should().Be((int)CaseChecklistSlaState.Breached);
            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.Unchecked)).Should().Be(1);
            (await TimelineCountAsync(dbContext, caseId, CaseChecklistService.UncheckedEventTypeId)).Should().Be(1);
            _ = itemId;
        }

        [Fact]
        public async Task CheckingATickedItemOrUncheckingAnUntickedOneIsAConflictAndWritesNothing()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var service = await ChecklistAsync(dbContext, Users.Holder, new ManualUtcTimeProvider(Start));
            await service.GetAsync(caseId);
            var itemId = await ItemIdAsync(dbContext, caseId, Unconditional);

            await FluentActions.Awaiting(() => service.UncheckAsync(caseId, itemId)).Should()
                .ThrowAsync<ConflictException>();

            await service.CheckAsync(caseId, itemId);

            await FluentActions.Awaiting(() => service.CheckAsync(caseId, itemId)).Should()
                .ThrowAsync<ConflictException>();

            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.Checked)).Should().Be(1);
            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.Unchecked)).Should().Be(0);
        }

        [Fact]
        public async Task UncheckingAnItemWhoseShowConditionNoLongerPassesHidesIt()
        {
            await using var dbContext = Fx.GetDbContext();
            var key = UniqueKey();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, key, 100, Users.Holder);
            var second = await CreateCaseAsync(dbContext, FirstLine, key, 100);
            var service = await ChecklistAsync(dbContext, Users.Holder, new ManualUtcTimeProvider(Start));
            await service.GetAsync(caseId);
            var itemId = await ItemIdAsync(dbContext, caseId, Related);

            await service.CheckAsync(caseId, itemId);
            await dbContext.Case.Where(w => w.Id == second).Set(s => s.CaseKeyValue, "zzChkMoved").UpdateAsync();
            await service.GetAsync(caseId);
            (await dbContext.CaseChecklistItem.FirstAsync(w => w.Id == itemId)).StateId
                .Should().Be((byte)CaseChecklistItemState.Done);

            await service.UncheckAsync(caseId, itemId);

            (await dbContext.CaseChecklistItem.FirstAsync(w => w.Id == itemId)).StateId
                .Should().Be((byte)CaseChecklistItemState.Hidden);
            (await TimelineCountAsync(dbContext, caseId, CaseChecklistService.HiddenEventTypeId)).Should().Be(1);

            await FluentActions.Awaiting(() => service.CheckAsync(caseId, itemId)).Should()
                .ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task AClosedCaseCannotBeChecked()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder, 3);
            var service = await ChecklistAsync(dbContext, Users.Holder, new ManualUtcTimeProvider(Start));

            await FluentActions.Awaiting(() => service.CheckAsync(caseId, 1)).Should()
                .ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task AnItemOfAnotherCaseIsNotFound()
        {
            await using var dbContext = Fx.GetDbContext();
            var clock = new ManualUtcTimeProvider(Start);
            var first = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var second = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var service = await ChecklistAsync(dbContext, Users.Holder, clock);
            await service.GetAsync(first);
            await service.GetAsync(second);
            var foreign = await ItemIdAsync(dbContext, second, Unconditional);

            await FluentActions.Awaiting(() => service.CheckAsync(first, foreign)).Should()
                .ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task AnotherTenantCanNeitherCheckNorUncheckNorOverride()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var clock = new ManualUtcTimeProvider(Start);
            await (await ChecklistAsync(dbContext, Users.Holder, clock)).GetAsync(caseId);
            var itemId = await ItemIdAsync(dbContext, caseId, Unconditional);
            var stranger = await ChecklistAsync(dbContext, Fx.Seed.UserTenantB, clock);

            await FluentActions.Awaiting(() => stranger.CheckAsync(caseId, itemId)).Should()
                .ThrowAsync<NotFoundException>();
            await FluentActions.Awaiting(() => stranger.UncheckAsync(caseId, itemId)).Should()
                .ThrowAsync<NotFoundException>();
            await FluentActions.Awaiting(() => stranger.OverrideAsync(caseId,
                new CaseChecklistOverrideRequestDto { Reason = "No." })).Should().ThrowAsync<ForbiddenException>();

            (await dbContext.CaseChecklistItem.FirstAsync(w => w.Id == itemId)).StateId
                .Should().Be((byte)CaseChecklistItemState.Shown);
            (await dbContext.CaseChecklistOverride.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
        }

        [Fact]
        public async Task AnAnonymousCallerCannotCheck()
        {
            await using var dbContext = Fx.GetDbContext();

            await FluentActions.Awaiting(() => ChecklistAsync(dbContext, string.Empty, new ManualUtcTimeProvider(Start)))
                .Should().ThrowAsync<NotAuthenticatedException>();
        }
    }
}

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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Dto.Options;
using Jube.Service.Query.CaseChecklist;
using Jube.Service.Query.CaseChecklist.Models;
using Xunit;

namespace Jube.Test.Service.Query.CaseChecklist
{
    [Trait("Category", "Unit")]
    public sealed class CaseChecklistReconcilePlanTests
    {
        private static readonly DateTime now = new(2026, 11, 4, 9, 0, 0, DateTimeKind.Utc);
        private static readonly Guid status = Guid.NewGuid();
        private static readonly Guid otherStatus = Guid.NewGuid();
        private static readonly Guid checklist = Guid.NewGuid();

        private static ChecklistPlanDefinition Definition(Guid item, ChecklistShowOutcome show, int? sla = null,
            bool conditional = true)
        {
            return new ChecklistPlanDefinition(item, checklist, status, "List", $"Item {item:N}"[..12], 1, sla,
                conditional, show);
        }

        private static ChecklistPlanExisting Existing(Guid item, CaseChecklistItemState state, Guid? inStatus = null,
            long id = 1)
        {
            return new ChecklistPlanExisting(id, item, inStatus ?? status, "Row", state);
        }

        [Fact]
        public void ANewItemThatIsShownIsInsertedWithItsClockAndDueDate()
        {
            var item = Guid.NewGuid();

            var operations = CaseChecklistReconcilePlan.Compute(status,
                [Definition(item, ChecklistShowOutcome.Shown, 60)], [], now);

            var operation = operations.Should().ContainSingle().Subject;
            operation.Kind.Should().Be(ChecklistPlanOperationKind.Insert);
            operation.ShownDate.Should().Be(now);
            operation.DueDate.Should().Be(now.AddMinutes(60));
        }

        [Fact]
        public void ANewItemWithoutAnSlaHasNoDueDate()
        {
            var operations = CaseChecklistReconcilePlan.Compute(status,
                [Definition(Guid.NewGuid(), ChecklistShowOutcome.Shown, conditional: false)], [], now);

            operations.Single().DueDate.Should().BeNull();
        }

        [Theory]
        [InlineData(ChecklistShowOutcome.NotShown)]
        [InlineData(ChecklistShowOutcome.Error)]
        public void ANewItemThatIsNotShownCreatesNothing(ChecklistShowOutcome outcome)
        {
            CaseChecklistReconcilePlan.Compute(status, [Definition(Guid.NewGuid(), outcome)], [], now)
                .Should().BeEmpty();
        }

        [Fact]
        public void AShownItemWhoseConditionFailsIsHidden()
        {
            var item = Guid.NewGuid();

            var operations = CaseChecklistReconcilePlan.Compute(status,
                [Definition(item, ChecklistShowOutcome.NotShown)], [Existing(item, CaseChecklistItemState.Shown, id: 7)],
                now);

            var operation = operations.Should().ContainSingle().Subject;
            operation.Kind.Should().Be(ChecklistPlanOperationKind.Hide);
            operation.ExistingId.Should().Be(7);
        }

        [Theory]
        [InlineData(ChecklistShowOutcome.Shown)]
        [InlineData(ChecklistShowOutcome.Error)]
        public void AShownItemWhoseConditionPassesOrFailsToEvaluateIsLeftAlone(ChecklistShowOutcome outcome)
        {
            var item = Guid.NewGuid();

            CaseChecklistReconcilePlan.Compute(status, [Definition(item, outcome)],
                [Existing(item, CaseChecklistItemState.Shown)], now).Should().BeEmpty();
        }

        [Fact]
        public void AHiddenItemWhoseConditionPassesIsShownAgainWithoutRestartingTheClock()
        {
            var item = Guid.NewGuid();

            var operations = CaseChecklistReconcilePlan.Compute(status,
                [Definition(item, ChecklistShowOutcome.Shown, 30)],
                [Existing(item, CaseChecklistItemState.Hidden, id: 9)], now);

            var operation = operations.Should().ContainSingle().Subject;
            operation.Kind.Should().Be(ChecklistPlanOperationKind.Show);
            operation.ExistingId.Should().Be(9);
            operation.ShownDate.Should().Be(now);
            operation.DueDate.Should().BeNull();
        }

        [Theory]
        [InlineData(ChecklistShowOutcome.NotShown)]
        [InlineData(ChecklistShowOutcome.Error)]
        public void AHiddenItemStaysHiddenUnlessItsConditionPasses(ChecklistShowOutcome outcome)
        {
            var item = Guid.NewGuid();

            CaseChecklistReconcilePlan.Compute(status, [Definition(item, outcome)],
                [Existing(item, CaseChecklistItemState.Hidden)], now).Should().BeEmpty();
        }

        [Theory]
        [InlineData(ChecklistShowOutcome.Shown)]
        [InlineData(ChecklistShowOutcome.NotShown)]
        [InlineData(ChecklistShowOutcome.Error)]
        public void ADoneItemIsNeverChanged(ChecklistShowOutcome outcome)
        {
            var item = Guid.NewGuid();

            CaseChecklistReconcilePlan.Compute(status, [Definition(item, outcome)],
                [Existing(item, CaseChecklistItemState.Done)], now).Should().BeEmpty();
        }

        [Fact]
        public void ADoneItemWhoseDefinitionHasGoneIsNeverChanged()
        {
            CaseChecklistReconcilePlan.Compute(status, [],
                [Existing(Guid.NewGuid(), CaseChecklistItemState.Done)], now).Should().BeEmpty();
        }

        [Fact]
        public void AShownItemWhoseDefinitionIsNoLongerInForceIsHidden()
        {
            var operations = CaseChecklistReconcilePlan.Compute(status, [],
                [Existing(Guid.NewGuid(), CaseChecklistItemState.Shown, id: 3)], now);

            var operation = operations.Should().ContainSingle().Subject;
            operation.Kind.Should().Be(ChecklistPlanOperationKind.Hide);
            operation.Definition.Should().BeNull();
        }

        [Fact]
        public void ARowOfAnotherStatusIsIgnored()
        {
            CaseChecklistReconcilePlan.Compute(status, [],
                [Existing(Guid.NewGuid(), CaseChecklistItemState.Shown, otherStatus)], now).Should().BeEmpty();
        }

        [Fact]
        public void AnAlreadyHiddenOrphanIsLeftAlone()
        {
            CaseChecklistReconcilePlan.Compute(status, [],
                [Existing(Guid.NewGuid(), CaseChecklistItemState.Hidden)], now).Should().BeEmpty();
        }

        [Fact]
        public void ASteadyStateProducesNoOperationsAtAll()
        {
            var shown = Guid.NewGuid();
            var hidden = Guid.NewGuid();
            var done = Guid.NewGuid();

            CaseChecklistReconcilePlan.Compute(status,
                [
                    Definition(shown, ChecklistShowOutcome.Shown), Definition(hidden, ChecklistShowOutcome.NotShown),
                    Definition(done, ChecklistShowOutcome.NotShown)
                ],
                [
                    Existing(shown, CaseChecklistItemState.Shown, id: 1),
                    Existing(hidden, CaseChecklistItemState.Hidden, id: 2),
                    Existing(done, CaseChecklistItemState.Done, id: 3)
                ], now).Should().BeEmpty();
        }

        [Fact]
        public void ADefinitionListedTwiceIsPlannedOnce()
        {
            var item = Guid.NewGuid();

            CaseChecklistReconcilePlan.Compute(status,
                [Definition(item, ChecklistShowOutcome.Shown), Definition(item, ChecklistShowOutcome.Shown)], [], now)
                .Should().ContainSingle();
        }

        private static CaseChecklistItem Row(CaseChecklistItemState state, DateTime? due, DateTime? completed = null,
            Guid? inStatus = null)
        {
            return new CaseChecklistItem
            {
                Id = 1,
                CaseWorkflowChecklistItemGuid = Guid.NewGuid(),
                CaseWorkflowChecklistGuid = checklist,
                CaseWorkflowStatusGuid = inStatus ?? status,
                ChecklistName = "List",
                ItemName = "Item",
                StateId = (byte)state,
                FirstShownDate = now.AddHours(-2),
                ShownDate = now.AddHours(-2),
                DueDate = due,
                CompletedDate = completed
            };
        }

        [Fact]
        public void AnOpenItemWithoutAnSlaHasNoSlaState()
        {
            var item = CaseChecklistService.Item(Row(CaseChecklistItemState.Shown, null), now);

            item.SlaStateId.Should().Be((int)CaseChecklistSlaState.None);
            item.MinutesRemaining.Should().BeNull();
        }

        [Fact]
        public void AnOpenItemBeforeItsDueDateIsOnTrackWithTheMinutesLeft()
        {
            var item = CaseChecklistService.Item(Row(CaseChecklistItemState.Shown, now.AddMinutes(30)), now);

            item.SlaStateId.Should().Be((int)CaseChecklistSlaState.OnTrack);
            item.MinutesRemaining.Should().Be(30);
        }

        [Fact]
        public void AnOpenItemPastItsDueDateIsBreachedWithNegativeMinutes()
        {
            var item = CaseChecklistService.Item(Row(CaseChecklistItemState.Shown, now.AddSeconds(-90)), now);

            item.SlaStateId.Should().Be((int)CaseChecklistSlaState.Breached);
            item.MinutesRemaining.Should().Be(-2);
        }

        [Fact]
        public void ADoneItemIsCompletedOnTimeOrLateAgainstItsDueDate()
        {
            CaseChecklistService.Item(
                Row(CaseChecklistItemState.Done, now.AddMinutes(10), now.AddMinutes(-5)), now).SlaStateId
                .Should().Be((int)CaseChecklistSlaState.CompletedOnTime);

            var late = CaseChecklistService.Item(
                Row(CaseChecklistItemState.Done, now.AddMinutes(-10), now.AddMinutes(-5)), now);

            late.SlaStateId.Should().Be((int)CaseChecklistSlaState.CompletedLate);
            late.MinutesRemaining.Should().BeNull();
        }

        [Fact]
        public void TheRollUpCountsOnlyTheCurrentStatusAndIgnoresHiddenItems()
        {
            var rows = new List<CaseChecklistItem>
            {
                Row(CaseChecklistItemState.Shown, now.AddMinutes(-1)),
                Row(CaseChecklistItemState.Shown, null),
                Row(CaseChecklistItemState.Done, null),
                Row(CaseChecklistItemState.Hidden, null),
                Row(CaseChecklistItemState.Shown, null, inStatus: otherStatus)
            };

            var result = CaseChecklistService.Build(5, status, rows, new Dictionary<Guid, int> { [checklist] = 1 }, now);

            result.Required.Should().Be(3);
            result.Done.Should().Be(1);
            result.Open.Should().Be(2);
            result.Breached.Should().Be(1);
            result.Complete.Should().BeFalse();
            result.Checklists.Should().ContainSingle().Which.Items.Should().HaveCount(3);
        }

        [Fact]
        public void ACaseWithNothingOpenIsComplete()
        {
            CaseChecklistService.Build(5, status, [Row(CaseChecklistItemState.Done, null)],
                new Dictionary<Guid, int>(), now).Complete.Should().BeTrue();
            CaseChecklistService.Build(5, status, [], new Dictionary<Guid, int>(), now).Complete.Should().BeTrue();
        }
    }
}

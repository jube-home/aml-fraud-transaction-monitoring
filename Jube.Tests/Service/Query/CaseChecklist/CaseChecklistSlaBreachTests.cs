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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Options;
using Jube.Engine.BackgroundTasks.TaskStarters;
using Jube.Engine.CaseEscalation;
using Jube.Service.Query.CaseChecklist;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Infrastructure.Reactivity;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.Service.Query.CaseChecklist
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseChecklistSlaBreachTests(DatabaseFixture fx) : CaseChecklistDemoTestBase(fx)
    {
        private const string TimedItem = "Record an investigation note";
        private const string UntimedItem = "Review the triggering transactions";
        private const int Tenant = 1;

        private static Task<int> BreachAuditsAsync(DbContext dbContext, int caseId)
        {
            return AuditCountAsync(dbContext, caseId, CaseChecklistAction.SlaBreached);
        }

        private async Task<(int CaseId, CaseChecklistService Service, ManualUtcTimeProvider Clock)> ShownCaseAsync(
            DbContext dbContext, string? holder = null)
        {
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, holder);
            var clock = new ManualUtcTimeProvider(Start);
            var service = await ChecklistAsync(dbContext, holder ?? Users.Holder, clock);
            await service.GetAsync(caseId);

            return (caseId, service, clock);
        }

        [Fact]
        public async Task AnOpenItemPastItsDueTimeIsRecordedOnceWithAnAuditRowAndATimelineEvent()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, service, clock) = await ShownCaseAsync(dbContext);

            clock.Advance(TimeSpan.FromMinutes(61));
            var result = await service.GetAsync(caseId);

            var item = await dbContext.CaseChecklistItem.FirstAsync(w => w.CaseId == caseId && w.ItemName == TimedItem);
            item.SlaBreachedDate.Should().Be(Start.AddMinutes(61));
            (await BreachAuditsAsync(dbContext, caseId)).Should().Be(1);

            var audit = await dbContext.CaseChecklistEvent.FirstAsync(w => w.CaseId == caseId
                && w.ActionId == (byte)CaseChecklistAction.SlaBreached);
            audit.Actor.Should().Be("System");
            audit.CaseChecklistItemId.Should().Be(item.Id);
            audit.Detail.Should().Contain(TimedItem);

            var timeline = await dbContext.CaseEvent.FirstAsync(w => w.CaseId == caseId
                && w.CaseEventTypeId == CaseChecklistService.SlaBreachedEventTypeId);
            timeline.Before.Should().Be(TimedItem);
            timeline.After.Should().Be(audit.Id.ToString());
            timeline.CreatedUser.Should().Be("System");
            audit.CaseEventId.Should().Be(timeline.Id);

            result.Checklists.SelectMany(s => s.Items).First(i => i.Name == TimedItem).SlaBreachedDate
                .Should().NotBeNull();
        }

        [Fact]
        public async Task ReadingAgainAfterTheBreachWritesNothingMore()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, service, clock) = await ShownCaseAsync(dbContext);
            clock.Advance(TimeSpan.FromMinutes(61));
            await service.GetAsync(caseId);

            clock.Advance(TimeSpan.FromMinutes(90));
            await service.GetAsync(caseId);

            (await BreachAuditsAsync(dbContext, caseId)).Should().Be(1);
            (await TimelineCountAsync(dbContext, caseId, CaseChecklistService.SlaBreachedEventTypeId)).Should().Be(1);
        }

        [Fact]
        public async Task NothingIsRecordedBeforeTheDueTime()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, service, clock) = await ShownCaseAsync(dbContext);

            clock.Advance(TimeSpan.FromMinutes(59));
            await service.GetAsync(caseId);

            (await BreachAuditsAsync(dbContext, caseId)).Should().Be(0);
        }

        [Fact]
        public async Task AnItemWithNoSlaIsNeverRecordedHoweverLongItStaysOpen()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, service, clock) = await ShownCaseAsync(dbContext);

            clock.Advance(TimeSpan.FromDays(30));
            await service.GetAsync(caseId);

            var untimed = await dbContext.CaseChecklistItem.FirstAsync(w => w.CaseId == caseId
                && w.ItemName == UntimedItem);
            untimed.DueDate.Should().BeNull();
            untimed.SlaBreachedDate.Should().BeNull();
            (await BreachAuditsAsync(dbContext, caseId)).Should().Be(1);
        }

        [Fact]
        public async Task ADoneItemIsNeverRecordedAsBreachedLater()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, service, clock) = await ShownCaseAsync(dbContext, Users.Holder);
            await AddNoteAsync(dbContext, caseId, Users.Holder);
            clock.Advance(TimeSpan.FromMinutes(10));
            await service.CheckAsync(caseId, await ItemIdAsync(dbContext, caseId, TimedItem));

            clock.Advance(TimeSpan.FromHours(5));
            await service.GetAsync(caseId);

            (await BreachAuditsAsync(dbContext, caseId)).Should().Be(0);
            (await dbContext.CaseChecklistItem.FirstAsync(w => w.CaseId == caseId && w.ItemName == TimedItem))
                .SlaBreachedDate.Should().BeNull();
        }

        [Fact]
        public async Task ACaseClosedUnderneathNeverHasABreachRecorded()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, _, clock) = await ShownCaseAsync(dbContext);
            await dbContext.Case.Where(w => w.Id == caseId).Set(s => s.ClosedStatusId, (byte)3).UpdateAsync();
            clock.Advance(TimeSpan.FromHours(3));
            var item = await dbContext.CaseChecklistItem.FirstAsync(w => w.CaseId == caseId && w.ItemName == TimedItem);
            var recorder = new CaseChecklistSlaBreachRecorder(dbContext, clock, TestLog.NoOp);

            (await recorder.RecordItemAsync(item.Id, caseId, Tenant)).Should().BeFalse();
            (await recorder.RecordOpenBreachesAsync(
                await dbContext.Case.FirstAsync(w => w.Id == caseId), Tenant, clock.Now)).Should().Be(0);
            (await BreachAuditsAsync(dbContext, caseId)).Should().Be(0);
        }

        [Fact]
        public async Task AHiddenItemIsNeverRecordedAsBreached()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, _, clock) = await ShownCaseAsync(dbContext);
            var item = await dbContext.CaseChecklistItem.FirstAsync(w => w.CaseId == caseId && w.ItemName == TimedItem);
            await dbContext.CaseChecklistItem.Where(w => w.Id == item.Id)
                .Set(s => s.StateId, (byte)CaseChecklistItemState.Hidden).UpdateAsync();
            clock.Advance(TimeSpan.FromHours(3));

            (await new CaseChecklistSlaBreachRecorder(dbContext, clock, TestLog.NoOp)
                .RecordItemAsync(item.Id, caseId, Tenant)).Should().BeFalse();
            (await BreachAuditsAsync(dbContext, caseId)).Should().Be(0);
        }

        [Fact]
        public async Task TickingAnItemAfterItFellDueRecordsTheBreachOnceAndTheItemCompletesLate()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, service, clock) = await ShownCaseAsync(dbContext, Users.Holder);
            await AddNoteAsync(dbContext, caseId, Users.Holder);
            var itemId = await ItemIdAsync(dbContext, caseId, TimedItem);

            clock.Advance(TimeSpan.FromMinutes(61));
            var result = await service.CheckAsync(caseId, itemId);

            (await BreachAuditsAsync(dbContext, caseId)).Should().Be(1);
            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.Checked)).Should().Be(1);
            (await dbContext.CaseChecklistItem.FirstAsync(w => w.Id == itemId)).SlaBreachedDate.Should().NotBeNull();
            result.Checklists.SelectMany(s => s.Items).First(i => i.Name == TimedItem).SlaStateId
                .Should().Be((int)CaseChecklistSlaState.CompletedLate);
        }

        [Fact]
        public async Task ACompletedLateItemWithNoBreachYetIsRecordedOnceByTheCompletionPath()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, _, clock) = await ShownCaseAsync(dbContext);
            var item = await dbContext.CaseChecklistItem.FirstAsync(w => w.CaseId == caseId && w.ItemName == TimedItem);
            item.StateId = (byte)CaseChecklistItemState.Done;
            item.CompletedDate = item.DueDate!.Value.AddMinutes(1);
            await dbContext.CaseChecklistItem.Where(w => w.Id == item.Id)
                .Set(s => s.StateId, item.StateId).Set(s => s.CompletedDate, item.CompletedDate).UpdateAsync();
            var caseRow = await dbContext.Case.FirstAsync(w => w.Id == caseId);
            var recorder = new CaseChecklistSlaBreachRecorder(dbContext, clock, TestLog.NoOp);

            (await recorder.RecordCompletedLateAsync(caseRow, Tenant, item, "someone", clock.Now)).Should().BeTrue();
            (await recorder.RecordCompletedLateAsync(caseRow, Tenant, item, "someone", clock.Now)).Should().BeFalse();

            (await BreachAuditsAsync(dbContext, caseId)).Should().Be(1);
            (await dbContext.CaseChecklistEvent.FirstAsync(w => w.CaseId == caseId
                && w.ActionId == (byte)CaseChecklistAction.SlaBreached)).Detail.Should().Contain("ticked by someone");
        }

        [Fact]
        public async Task ACompletionBeforeTheDueTimeIsNotABreach()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, _, clock) = await ShownCaseAsync(dbContext);
            var item = await dbContext.CaseChecklistItem.FirstAsync(w => w.CaseId == caseId && w.ItemName == TimedItem);
            item.CompletedDate = item.DueDate!.Value.AddMinutes(-1);
            var caseRow = await dbContext.Case.FirstAsync(w => w.Id == caseId);

            (await new CaseChecklistSlaBreachRecorder(dbContext, clock, TestLog.NoOp)
                .RecordCompletedLateAsync(caseRow, Tenant, item, "someone", clock.Now)).Should().BeFalse();
        }

        [Fact]
        public async Task ConcurrentCallersRecordTheBreachExactlyOnce()
        {
            await using var dbContext = Fx.GetDbContext();
            var (caseId, _, clock) = await ShownCaseAsync(dbContext);
            var itemId = await ItemIdAsync(dbContext, caseId, TimedItem);
            clock.Advance(TimeSpan.FromHours(2));

            var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
            {
                await using var own = Fx.GetDbContext();

                return await new CaseChecklistSlaBreachRecorder(own, clock, TestLog.NoOp)
                    .RecordItemAsync(itemId, caseId, Tenant);
            }));

            results.Count(c => c).Should().Be(1);
            (await BreachAuditsAsync(dbContext, caseId)).Should().Be(1);
            (await TimelineCountAsync(dbContext, caseId, CaseChecklistService.SlaBreachedEventTypeId)).Should().Be(1);
        }

        [Fact]
        public async Task ASweepPassRespectsTheBatchSizeAndEveryBreachIsRecordedOnceAcrossPasses()
        {
            await using var dbContext = Fx.GetDbContext();
            var cases = new List<int>();
            ManualUtcTimeProvider clock = new(Start);

            for (var i = 0; i < 3; i++)
            {
                var (caseId, _, shownClock) = await ShownCaseAsync(dbContext);
                cases.Add(caseId);
                clock = shownClock;
            }

            clock.Advance(TimeSpan.FromHours(2));
            var bus = new CapturingBus();
            var sweep = new ServiceCaseChecklistSlaSweep(bus, clock, TestLog.NoOp);

            var first = await sweep.SweepAsync(dbContext, 2);
            first.Should().Be(2);

            var total = first;
            int pass;

            do
            {
                pass = await sweep.SweepAsync(dbContext, 2);
                total += pass;
            } while (pass > 0);

            total.Should().BeGreaterThanOrEqualTo(3);

            foreach (var caseId in cases)
            {
                (await BreachAuditsAsync(dbContext, caseId)).Should().Be(1);
            }

            (await sweep.SweepAsync(dbContext, 200)).Should().Be(0);
            bus.Published.Where(c => c.Area == "CaseChecklist" && cases.Contains(c.EntityId!.Value))
                .Select(s => s.EntityId!.Value).Should().BeEquivalentTo(cases);
            bus.Published.Where(c => cases.Contains(c.EntityId!.Value)).Should().OnlyContain(c => c.Actor == "System");
        }

        [Fact]
        public void TheSweepIsEnabledByDefaultAndSwitchedOffByItsKey()
        {
            CaseChecklistSlaSweepStarter.IsEnabled(DynamicEnvironment).Should().BeTrue();
            CaseChecklistSlaSweepStarter.IsEnabled(EnvironmentWith("EnableCaseChecklistSlaSweep", "False"))
                .Should().BeFalse();
            CaseChecklistSlaSweepStarter.IsEnabled(EnvironmentWith("EnableCaseChecklistSlaSweep", "true"))
                .Should().BeTrue();
        }

        [Fact]
        public async Task AFailingOrMissingSweepNeverThrowsIntoTheEngine()
        {
            await using var dbContext = Fx.GetDbContext();
            var previous = CaseChecklistSlaSweepRegistry.Current;

            try
            {
                CaseChecklistSlaSweepRegistry.Current = null!;
                (await CaseChecklistSlaSweepSignal.RunAsync(dbContext, 10)).Should().Be(0);

                CaseChecklistSlaSweepRegistry.Current = new ThrowingChecklistSlaSweep();
                (await CaseChecklistSlaSweepSignal.RunAsync(dbContext, 10)).Should().Be(0);
            }
            finally
            {
                CaseChecklistSlaSweepRegistry.Current = previous;
            }
        }
    }
}

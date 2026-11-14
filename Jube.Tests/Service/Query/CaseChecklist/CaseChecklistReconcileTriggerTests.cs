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
using Jube.Service.Query.CaseChecklist;
using Jube.Service.Repository.CaseWorkflowFormValidator;
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
    public sealed class CaseChecklistReconcileTriggerTests(DatabaseFixture fx) : CaseChecklistDemoTestBase(fx)
    {
        private static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(60);

        private static CaseChecklistReconciler Reconciler(DbContext dbContext, TimeProvider clock,
            out CountingSqlRunner runner, Func<Task>? whileEvaluating = null)
        {
            runner = new CountingSqlRunner(new FormSqlRunner(Connection(), Administrator, TestLog.NoOp),
                whileEvaluating);

            return new CaseChecklistReconciler(dbContext, runner, clock, TestLog.NoOp);
        }

        private static Task<DateTime?> ReconciledAsync(DbContext dbContext, int caseId)
        {
            return dbContext.Case.Where(w => w.Id == caseId).Select(s => s.ChecklistReconciledDate).FirstAsync();
        }

        private static Task MarkStaleAsync(DbContext dbContext, int caseId)
        {
            return new CaseEventRepository(dbContext, Administrator).InsertAsync(new CaseEvent
            {
                CaseId = caseId,
                CaseEventTypeId = 3,
                CaseKey = "AccountId",
                CaseKeyValue = "n/a",
                CreatedUser = Administrator
            });
        }

        [Fact]
        public async Task ACaseThatHasNeverBeenReconciledIsReconciledOnTheFirstRead()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var reconciler = Reconciler(dbContext, new ManualUtcTimeProvider(Start), out var runner);

            var changes = await reconciler.ReconcileIfNeededAsync(caseId, "System", "Test", MaxAge);

            changes.Should().Be(2);
            runner.Calls.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task AFreshCaseThatIsNotStaleRunsNoShowConditionAndWritesNothing()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var clock = new ManualUtcTimeProvider(Start);
            var reconciler = Reconciler(dbContext, clock, out var runner);
            await reconciler.ReconcileAsync(caseId, "System", "Test");
            var calls = runner.Calls;
            var reconciled = await ReconciledAsync(dbContext, caseId);
            var audit = await dbContext.CaseChecklistEvent.CountAsync(w => w.CaseId == caseId);

            clock.Advance(TimeSpan.FromSeconds(59));
            var changes = await reconciler.ReconcileIfNeededAsync(caseId, "System", "Test", MaxAge);

            changes.Should().Be(0);
            runner.Calls.Should().Be(calls);
            (await ReconciledAsync(dbContext, caseId)).Should().Be(reconciled);
            (await dbContext.CaseChecklistEvent.CountAsync(w => w.CaseId == caseId)).Should().Be(audit);
        }

        [Fact]
        public async Task AStaleCaseIsReconciledEvenWhenItWasReconciledAMomentAgo()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var clock = new ManualUtcTimeProvider(DateTime.UtcNow.AddMinutes(-5));
            var reconciler = Reconciler(dbContext, clock, out var runner);
            await reconciler.ReconcileAsync(caseId, "System", "Test");
            var calls = runner.Calls;

            await MarkStaleAsync(dbContext, caseId);
            await reconciler.ReconcileIfNeededAsync(caseId, "System", "Test", TimeSpan.FromHours(1));

            runner.Calls.Should().BeGreaterThan(calls);
        }

        [Fact]
        public async Task ACaseNotStaleIsReconciledOnceItIsOlderThanTheMaximumAge()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var clock = new ManualUtcTimeProvider(Start);
            var reconciler = Reconciler(dbContext, clock, out var runner);
            await reconciler.ReconcileAsync(caseId, "System", "Test");
            var calls = runner.Calls;

            clock.Advance(MaxAge + TimeSpan.FromSeconds(1));
            await reconciler.ReconcileIfNeededAsync(caseId, "System", "Test", MaxAge);

            runner.Calls.Should().BeGreaterThan(calls);
            (await ReconciledAsync(dbContext, caseId)).Should().Be(Start.Add(MaxAge).AddSeconds(1));
        }

        [Fact]
        public async Task AMaximumAgeOfZeroReconcilesOnEveryRead()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var reconciler = Reconciler(dbContext, new ManualUtcTimeProvider(Start), out var runner);
            await reconciler.ReconcileAsync(caseId, "System", "Test");
            var calls = runner.Calls;

            await reconciler.ReconcileIfNeededAsync(caseId, "System", "Test", TimeSpan.Zero);
            await reconciler.ReconcileIfNeededAsync(caseId, "System", "Test", TimeSpan.Zero);

            runner.Calls.Should().Be(calls * 3);
        }

        [Fact]
        public async Task AnEventArrivingWhileTheShowConditionsRunLeavesTheCaseStale()
        {
            await using var dbContext = Fx.GetDbContext();
            await using var other = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var raised = false;

            var reconciler = Reconciler(dbContext, TimeProvider.System, out var runner, async () =>
            {
                if (raised)
                {
                    return;
                }

                raised = true;
                await MarkStaleAsync(other, caseId);
            });

            await reconciler.ReconcileAsync(caseId, "System", "Test");
            var calls = runner.Calls;

            var row = await dbContext.Case.Where(w => w.Id == caseId)
                .Select(s => new { s.ChecklistReconciledDate, s.ChecklistStaleDate }).FirstAsync();
            row.ChecklistStaleDate.Should().BeAfter(row.ChecklistReconciledDate!.Value);

            await reconciler.ReconcileIfNeededAsync(caseId, "System", "Test", TimeSpan.FromHours(1));

            runner.Calls.Should().BeGreaterThan(calls);
        }

        [Fact]
        public async Task ReadingAFreshCaseThroughTheServiceLeavesItsReconciledDateAlone()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var clock = new ManualUtcTimeProvider(Start);
            var service = await ChecklistAsync(dbContext, Users.Holder, clock);
            await service.GetAsync(caseId);
            var reconciled = await ReconciledAsync(dbContext, caseId);

            clock.Advance(TimeSpan.FromSeconds(30));
            await service.GetAsync(caseId);

            (await ReconciledAsync(dbContext, caseId)).Should().Be(reconciled);
        }

        [Fact]
        public async Task ReadingAStaleCaseThroughTheServiceReconcilesIt()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var clock = new ManualUtcTimeProvider(DateTime.UtcNow.AddMinutes(-5));
            var service = await ChecklistAsync(dbContext, Users.Holder, clock);
            await service.GetAsync(caseId);
            var reconciled = await ReconciledAsync(dbContext, caseId);
            await MarkStaleAsync(dbContext, caseId);
            clock.Advance(TimeSpan.FromSeconds(5));

            await service.GetAsync(caseId);

            (await ReconciledAsync(dbContext, caseId)).Should().BeAfter(reconciled!.Value);
        }

        [Fact]
        public async Task TheMaximumAgeSettingOfZeroMakesTheServiceReconcileOnEveryRead()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var clock = new ManualUtcTimeProvider(Start);

            var service = await ChecklistAsync(dbContext, Users.Holder, clock, null,
                EnvironmentWith("CaseChecklistReconcileMaxAgeSeconds", "0"));
            await service.GetAsync(caseId);
            var reconciled = await ReconciledAsync(dbContext, caseId);

            clock.Advance(TimeSpan.FromSeconds(1));
            await service.GetAsync(caseId);

            (await ReconciledAsync(dbContext, caseId)).Should().Be(reconciled!.Value.AddSeconds(1));
        }

        [Fact]
        public void TheMaximumAgeDefaultsToSixtySecondsAndIgnoresAnUnreadableSetting()
        {
            CaseChecklistService.ReconcileMaxAge(DynamicEnvironment).Should().Be(TimeSpan.FromSeconds(60));
            CaseChecklistService.ReconcileMaxAge(EnvironmentWith("CaseChecklistReconcileMaxAgeSeconds", "soon"))
                .Should().Be(TimeSpan.FromSeconds(60));
            CaseChecklistService.ReconcileMaxAge(EnvironmentWith("CaseChecklistReconcileMaxAgeSeconds", "-5"))
                .Should().Be(TimeSpan.FromSeconds(60));
            CaseChecklistService.ReconcileMaxAge(EnvironmentWith("CaseChecklistReconcileMaxAgeSeconds", "15"))
                .Should().Be(TimeSpan.FromSeconds(15));
        }

        [Fact]
        public async Task ClaimingACaseShowsItsItemsWithoutAnyoneReadingTheChecklist()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var current = await StatusGuidAsync(dbContext, FirstLine);
            (await dbContext.CaseChecklistItem.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
            var cases = await CasesAsync(dbContext, Users.Holder);

            await cases.UpdateAsync(Move(caseId, current, 0, Users.Holder));

            (await dbContext.CaseChecklistItem.Where(w => w.CaseId == caseId
                                                         && w.StateId == (byte)CaseChecklistItemState.Shown)
                .OrderBy(o => o.Priority).Select(s => s.ItemName).ToListAsync())
                .Should().Equal("Review the triggering transactions", "Record an investigation note");
            (await dbContext.CaseChecklistEvent.Where(w => w.CaseId == caseId).Select(s => s.Actor).Distinct()
                .ToListAsync()).Should().Equal(Users.Holder);
        }

        [Fact]
        public async Task MovingTheStatusReconcilesAgainstTheNewStatus()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, "Restricted", UniqueKey(), 100, Users.Holder);
            var supervisor = await StatusGuidAsync(dbContext, Supervisor);
            var cases = await CasesAsync(dbContext, Users.Holder);

            await cases.UpdateAsync(Move(caseId, supervisor, 0, Users.Holder));

            var rows = await dbContext.CaseChecklistItem.Where(w => w.CaseId == caseId).ToListAsync();
            rows.Should().ContainSingle().Which.ItemName.Should().Be("Second-person review");
            rows[0].CaseWorkflowStatusGuid.Should().Be(supervisor);
        }

        [Fact]
        public async Task AFailingReconcileAfterTheSaveDoesNotFailOrUndoTheUpdate()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var current = await StatusGuidAsync(dbContext, FirstLine);
            var cases = await CasesAsync(dbContext, Users.Holder, null, _ => new ThrowingChecklistReconciler());

            var saved = await cases.UpdateAsync(Move(caseId, current, 0, Users.Holder));

            saved.Locked.Should().Be(1);
            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).LockedUser.Should().Be(Users.Holder);
            (await dbContext.CaseChecklistItem.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
        }

        [Fact]
        public async Task AnUpdateThatNeitherClaimsNorMovesTheCaseDoesNotReconcile()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var current = await StatusGuidAsync(dbContext, FirstLine);
            var cases = await CasesAsync(dbContext, Users.Holder, null, _ => new ThrowingChecklistReconciler());
            var move = Move(caseId, current, 0, Users.Holder);
            move.Rating = 4;

            await cases.UpdateAsync(move);

            (await dbContext.CaseChecklistItem.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
        }

        [Fact]
        public async Task ChangeEventsArePublishedForReadsTicksUnticksAndTheClaimHook()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var bus = new CapturingBus();
            var service = await ChecklistAsync(dbContext, Users.Holder, new ManualUtcTimeProvider(Start), bus);

            await service.GetAsync(caseId);
            bus.Published.Should().ContainSingle(c => c.Area == "CaseChecklist" && c.EntityId == caseId);

            var itemId = await ItemIdAsync(dbContext, caseId, "Review the triggering transactions");
            await service.CheckAsync(caseId, itemId);
            await service.UncheckAsync(caseId, itemId);

            bus.Published.Where(c => c.Area == "CaseChecklist" && c.EntityId == caseId).Should().HaveCount(3);

            var claimed = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var current = await StatusGuidAsync(dbContext, FirstLine);
            var claimBus = new CapturingBus();
            var cases = await CasesAsync(dbContext, Users.Holder, claimBus);

            await cases.UpdateAsync(Move(claimed, current, 0, Users.Holder));

            claimBus.Published.Should().Contain(c => c.Area == "CaseChecklist" && c.EntityId == claimed
                                                     && c.Actor == Users.Holder);
        }
    }
}

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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Options;
using Jube.Service.Exceptions.Query.CaseChecklist;
using Jube.Service.Query.CaseChecklist;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseWorkflowFormValidator;
using Jube.Service.Repository.CaseWorkflowFormValidator.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Service.Query.CaseChecklist
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseChecklistReconcilerTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string Administrator = "Administrator";
        private const string WorkflowName = "Detailed Account Financial Transactions Cases";

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly JubeDynamicEnvironment dynamicEnvironment = TestDynamicEnvironment.Create(
            new Dictionary<string, string>
            {
                ["ConnectionString"] = Connection(),
                ["ReportConnectionString"] = Connection(),
            });

        private static readonly DateTime start = new(2026, 11, 4, 9, 0, 0, DateTimeKind.Utc);
        private readonly List<int> caseIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.CaseChecklistEvent.Where(w => caseIds.Contains(w.CaseId)).DeleteAsync();
            await dbContext.CaseChecklistItem.Where(w => caseIds.Contains(w.CaseId)).DeleteAsync();
            await dbContext.CaseEvent.Where(w => w.CaseId != null && caseIds.Contains(w.CaseId.Value)).DeleteAsync();
            await dbContext.Case.Where(w => caseIds.Contains(w.Id)).DeleteAsync();
        }

        private static string Connection()
        {
            return Environment.GetEnvironmentVariable("JubeTestConnectionString")
                   ?? Environment.GetEnvironmentVariable("ConnectionString")
                   ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=20;";
        }

        private static CaseChecklistReconciler Reconciler(DbContext dbContext, TimeProvider clock,
            IFormSqlRunner? runner = null)
        {
            return new CaseChecklistReconciler(dbContext,
                runner ?? new FormSqlRunner(Connection(), Administrator, TestLog.NoOp), clock, TestLog.NoOp);
        }

        private async Task<int> CreateCaseAsync(DbContext dbContext, string statusName, string key,
            decimal? amount, byte closedStatusId = 0)
        {
            var workflowGuid = await dbContext.CaseWorkflow.Where(w => w.Name == WorkflowName)
                .Select(s => s.Guid).FirstAsync();

            var statusGuid = await (from s in dbContext.CaseWorkflowStatus
                join w in dbContext.CaseWorkflow on s.CaseWorkflowId equals w.Id
                where w.Name == WorkflowName && s.Name == statusName
                select s.Guid).FirstAsync();

            var json = amount.HasValue
                ? $"{{\"payload\":{{\"CurrencyAmount\":\"{amount.Value}\"}}}}"
                : "{\"payload\":{}}";

            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid,
                CaseKey = "AccountId",
                CaseKeyValue = key,
                Json = json,
                CreatedDate = start,
                Locked = 0,
                ClosedStatusId = closedStatusId,
                Rating = 0,
                LastClosedStatus = 0,
            });
            caseIds.Add(id);

            return id;
        }

        private static string UniqueKey() => $"{DatabaseFixture.Prefix}Chk{Guid.NewGuid():N}"[..30];

        private static async Task<List<string>> ShownNamesAsync(DbContext dbContext, int caseId)
        {
            return await dbContext.CaseChecklistItem
                .Where(w => w.CaseId == caseId && w.StateId == (byte)CaseChecklistItemState.Shown)
                .OrderBy(o => o.Priority).Select(s => s.ItemName!).ToListAsync();
        }

        private static Task<int> AuditCountAsync(DbContext dbContext, int caseId)
        {
            return dbContext.CaseChecklistEvent.CountAsync(w => w.CaseId == caseId);
        }

        private static Task<int> TimelineCountAsync(DbContext dbContext, int caseId)
        {
            return dbContext.CaseEvent.CountAsync(w => w.CaseId == caseId && w.CaseEventTypeId >= 21
                                                       && w.CaseEventTypeId <= 28);
        }

        [Fact]
        public async Task OnlyTheItemsWhoseConditionsPassAreShownAndEachIsAuditedOnceOnTheTimeline()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, "First Line Review", UniqueKey(), 100);
            var clock = new ManualUtcTimeProvider(start);

            var changes = await Reconciler(dbContext, clock).ReconcileAsync(caseId, "System", "Test");

            changes.Should().Be(2);
            (await ShownNamesAsync(dbContext, caseId)).Should().Equal("Review the triggering transactions",
                "Record an investigation note");
            (await AuditCountAsync(dbContext, caseId)).Should().Be(2);
            (await TimelineCountAsync(dbContext, caseId)).Should().Be(2);

            var audit = await dbContext.CaseChecklistEvent.Where(w => w.CaseId == caseId).ToListAsync();
            audit.Should().OnlyContain(o => o.ActionId == (byte)CaseChecklistAction.Shown && o.Actor == "System"
                                            && o.CaseEventId != null);

            var timeline = await dbContext.CaseEvent.Where(w => w.CaseId == caseId && w.CaseEventTypeId == 21)
                .ToListAsync();
            timeline.Select(s => s.After).Should().BeEquivalentTo(audit.Select(a => a.Id.ToString()));

            (await dbContext.Case.Where(w => w.Id == caseId).Select(s => s.ChecklistReconciledDate).FirstAsync())
                .Should().NotBeNull();
        }

        [Fact]
        public async Task AnotherReconcileWithNothingChangedWritesNothing()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, "First Line Review", UniqueKey(), 100);
            var reconciler = Reconciler(dbContext, new ManualUtcTimeProvider(start));

            await reconciler.ReconcileAsync(caseId, "System", "Test");

            (await reconciler.ReconcileAsync(caseId, "System", "Test")).Should().Be(0);
            (await AuditCountAsync(dbContext, caseId)).Should().Be(2);
            (await TimelineCountAsync(dbContext, caseId)).Should().Be(2);
        }

        [Fact]
        public async Task AHighValueCaseAlsoShowsTheCustomerContactItem()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, "First Line Review", UniqueKey(), 5000);

            await Reconciler(dbContext, new ManualUtcTimeProvider(start)).ReconcileAsync(caseId, "System", "Test");

            (await ShownNamesAsync(dbContext, caseId)).Should().Contain("Contact the customer").And.HaveCount(3);
        }

        [Fact]
        public async Task TheRelatedCasesItemFollowsASecondCaseOnTheKeyAndKeepsItsClockWhenShownAgain()
        {
            await using var dbContext = fx.GetDbContext();
            var key = UniqueKey();
            var caseId = await CreateCaseAsync(dbContext, "First Line Review", key, 100);
            var clock = new ManualUtcTimeProvider(start);
            var reconciler = Reconciler(dbContext, clock);

            await reconciler.ReconcileAsync(caseId, "System", "Test");
            (await ShownNamesAsync(dbContext, caseId)).Should().NotContain("Check related cases on the case key");

            var second = await CreateCaseAsync(dbContext, "First Line Review", key, 100);
            clock.Advance(TimeSpan.FromMinutes(10));
            (await reconciler.ReconcileAsync(caseId, "System", "Test")).Should().Be(1);

            var row = await dbContext.CaseChecklistItem.FirstAsync(w => w.CaseId == caseId
                && w.ItemName == "Check related cases on the case key");
            row.StateId.Should().Be((byte)CaseChecklistItemState.Shown);
            row.DueDate.Should().Be(start.AddMinutes(10 + 120));
            var firstShown = row.FirstShownDate;

            await dbContext.Case.Where(w => w.Id == second).DeleteAsync();
            caseIds.Remove(second);
            clock.Advance(TimeSpan.FromMinutes(5));
            (await reconciler.ReconcileAsync(caseId, "System", "Test")).Should().Be(1);

            (await dbContext.CaseChecklistItem.FirstAsync(w => w.Id == row.Id)).StateId
                .Should().Be((byte)CaseChecklistItemState.Hidden);
            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == caseId && w.CaseEventTypeId == 22))
                .Should().Be(1);

            await CreateCaseAsync(dbContext, "First Line Review", key, 100);
            clock.Advance(TimeSpan.FromMinutes(20));
            (await reconciler.ReconcileAsync(caseId, "System", "Test")).Should().Be(1);

            var again = await dbContext.CaseChecklistItem.FirstAsync(w => w.Id == row.Id);
            again.StateId.Should().Be((byte)CaseChecklistItemState.Shown);
            again.FirstShownDate.Should().Be(firstShown);
            again.DueDate.Should().Be(row.DueDate);
            again.ShownDate.Should().Be(start.AddMinutes(10 + 5 + 20));
        }

        [Fact]
        public async Task ATickedItemStaysVisibleAndTickedWhenItsConditionStopsPassing()
        {
            await using var dbContext = fx.GetDbContext();
            var key = UniqueKey();
            var caseId = await CreateCaseAsync(dbContext, "First Line Review", key, 100);
            var second = await CreateCaseAsync(dbContext, "First Line Review", key, 100);
            var reconciler = Reconciler(dbContext, new ManualUtcTimeProvider(start));

            await reconciler.ReconcileAsync(caseId, "System", "Test");

            await dbContext.CaseChecklistItem.Where(w => w.CaseId == caseId
                                                         && w.ItemName == "Check related cases on the case key")
                .Set(s => s.StateId, (byte)CaseChecklistItemState.Done).UpdateAsync();

            await dbContext.Case.Where(w => w.Id == second).DeleteAsync();
            caseIds.Remove(second);

            var audit = await AuditCountAsync(dbContext, caseId);

            (await reconciler.ReconcileAsync(caseId, "System", "Test")).Should().Be(0);
            (await AuditCountAsync(dbContext, caseId)).Should().Be(audit);
            (await dbContext.CaseChecklistItem.FirstAsync(w => w.CaseId == caseId
                                                               && w.ItemName == "Check related cases on the case key"))
                .StateId.Should().Be((byte)CaseChecklistItemState.Done);
        }

        [Fact]
        public async Task AClosedCaseIsLeftAlone()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, "First Line Review", UniqueKey(), 100, 3);

            (await Reconciler(dbContext, new ManualUtcTimeProvider(start)).ReconcileAsync(caseId, "System", "Test"))
                .Should().Be(0);
            (await dbContext.CaseChecklistItem.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
        }

        [Fact]
        public async Task AStatusWithNoChecklistsShowsNothing()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, "Reported", UniqueKey(), 100);

            (await Reconciler(dbContext, new ManualUtcTimeProvider(start)).ReconcileAsync(caseId, "System", "Test"))
                .Should().Be(0);
        }

        [Fact]
        public async Task AFailingShowConditionKeepsTheStateAndCreatesNothing()
        {
            await using var dbContext = fx.GetDbContext();
            var key = UniqueKey();
            var caseId = await CreateCaseAsync(dbContext, "First Line Review", key, 5000);
            await CreateCaseAsync(dbContext, "First Line Review", key, 100);
            var clock = new ManualUtcTimeProvider(start);

            await Reconciler(dbContext, clock).ReconcileAsync(caseId, "System", "Test");
            (await ShownNamesAsync(dbContext, caseId)).Should().HaveCount(4);

            var audit = await AuditCountAsync(dbContext, caseId);

            (await Reconciler(dbContext, clock, new ThrowingRunner()).ReconcileAsync(caseId, "System", "Test"))
                .Should().Be(0);
            (await ShownNamesAsync(dbContext, caseId)).Should().HaveCount(4);
            (await AuditCountAsync(dbContext, caseId)).Should().Be(audit);

            var freshCase = await CreateCaseAsync(dbContext, "First Line Review", UniqueKey(), 5000);

            await Reconciler(dbContext, clock, new ThrowingRunner()).ReconcileAsync(freshCase, "System", "Test");
            (await ShownNamesAsync(dbContext, freshCase)).Should().Equal("Review the triggering transactions",
                "Record an investigation note");
        }

        [Fact]
        public async Task TwoReconcilesAtOnceDoNotDuplicateItems()
        {
            await using var first = fx.GetDbContext();
            await using var second = fx.GetDbContext();
            var caseId = await CreateCaseAsync(first, "First Line Review", UniqueKey(), 100);
            var clock = new ManualUtcTimeProvider(start);

            await Task.WhenAll(Reconciler(first, clock).ReconcileAsync(caseId, "System", "A"),
                Reconciler(second, clock).ReconcileAsync(caseId, "System", "B"));

            (await first.CaseChecklistItem.CountAsync(w => w.CaseId == caseId)).Should().Be(2);
            (await AuditCountAsync(first, caseId)).Should().Be(2);
            (await TimelineCountAsync(first, caseId)).Should().Be(2);
        }

        private static Task<CaseChecklistService> ServiceAsync(DbContext dbContext, string user, TimeProvider clock)
        {
            return CaseChecklistService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), dynamicEnvironment, TestLog.NoOp, clock);
        }

        [Fact]
        public async Task TheServiceReturnsTheChecklistWithSlaPositionAsTimePasses()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, "First Line Review", UniqueKey(), 100);
            var clock = new ManualUtcTimeProvider(start);
            var service = await ServiceAsync(dbContext, Administrator, clock);

            var result = await service.GetAsync(caseId);

            result.Checklists.Should().ContainSingle().Which.Name.Should().Be("Initial Triage");
            var items = result.Checklists[0].Items;
            items.Select(s => s.Name).Should().Equal("Review the triggering transactions",
                "Record an investigation note");
            result.Required.Should().Be(2);
            result.Open.Should().Be(2);
            result.Complete.Should().BeFalse();
            items[0].SlaStateId.Should().Be((int)CaseChecklistSlaState.None);
            items[1].SlaStateId.Should().Be((int)CaseChecklistSlaState.OnTrack);
            items[1].MinutesRemaining.Should().Be(60);

            clock.Advance(TimeSpan.FromMinutes(30));
            (await service.GetAsync(caseId)).Checklists[0].Items[1].MinutesRemaining.Should().Be(30);

            clock.Advance(TimeSpan.FromMinutes(31));
            var breached = await service.GetAsync(caseId);
            breached.Checklists[0].Items[1].SlaStateId.Should().Be((int)CaseChecklistSlaState.Breached);
            breached.Checklists[0].Items[1].MinutesRemaining.Should().Be(-1);
            breached.Breached.Should().Be(1);

            await dbContext.CaseChecklistItem.Where(w => w.CaseId == caseId)
                .Set(s => s.StateId, (byte)CaseChecklistItemState.Done)
                .Set(s => s.CompletedDate, clock.GetUtcNow().UtcDateTime)
                .Set(s => s.CompletedUser, Administrator).UpdateAsync();

            var done = await service.GetAsync(caseId);
            done.Complete.Should().BeTrue();
            done.Done.Should().Be(2);
            done.Checklists[0].Items[1].SlaStateId.Should().Be((int)CaseChecklistSlaState.CompletedLate);
        }

        [Fact]
        public async Task AnotherTenantCannotReadTheChecklistOfACase()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, "First Line Review", UniqueKey(), 100);
            var stranger = await ServiceAsync(dbContext, fx.Seed.UserTenantB, new ManualUtcTimeProvider(start));

            await FluentActions.Awaiting(() => stranger.GetAsync(caseId)).Should().ThrowAsync<NotFoundException>();
            (await dbContext.CaseChecklistItem.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
        }

        [Fact]
        public async Task AnUnknownCaseIsNotFound()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, Administrator, new ManualUtcTimeProvider(start));

            await FluentActions.Awaiting(() => service.GetAsync(int.MaxValue)).Should()
                .ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task AnAnonymousCallerIsNotAuthenticated()
        {
            await using var dbContext = fx.GetDbContext();

            await FluentActions.Awaiting(() => ServiceAsync(dbContext, string.Empty, new ManualUtcTimeProvider(start)))
                .Should().ThrowAsync<NotAuthenticatedException>();
        }

        private sealed class ThrowingRunner : IFormSqlRunner
        {
            public Task<bool> PassesAsync(string sql, IReadOnlyDictionary<string, object> parameters,
                CancellationToken token)
            {
                throw new InvalidOperationException("The show condition failed.");
            }
        }
    }
}

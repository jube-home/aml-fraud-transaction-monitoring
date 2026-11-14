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
using Jube.Data.Poco;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseAnalytics;
using Jube.Dto.Query.CaseSearch;
using Jube.Service.Exceptions.Query.CaseSearch;
using Jube.Service.Query.CaseAnalytics;
using Jube.Service.Query.CaseSearch;
using Jube.Service.Query.CaseSummary;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseChecklistReadModelTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly string connectionString = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                                          ?? Environment.GetEnvironmentVariable("ConnectionString")
                                                          ?? string.Empty;

        private static readonly JubeDynamicEnvironment dynamicEnvironment = TestDynamicEnvironment.Create(
            new Dictionary<string, string>
            {
                ["ConnectionString"] = connectionString,
                ["ReportConnectionString"] = connectionString
            });

        private readonly CaseRiskSeeder seeder = new(fx);

        private string Me => fx.Seed.UserWithPermission;
        private string Other => fx.Seed.UserWithPermissionNoApproval;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GetTable<CaseChecklistItem>().Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private static Task<CaseSearchService> SearchAsync(DbContext dbContext, string user) =>
            CaseSearchService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                dynamicEnvironment, TestLog.NoOp);

        private static Task<CaseSummaryService> SummaryAsync(DbContext dbContext, string user) =>
            CaseSummaryService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                dynamicEnvironment, TestLog.NoOp);

        private static Task<CaseAnalyticsService> AnalyticsAsync(DbContext dbContext, string user) =>
            CaseAnalyticsService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                dynamicEnvironment, TestLog.NoOp);

        private static CaseSearchRequestDto Request(CaseRiskSeeder.Seeded workflow, CaseSearchView view,
            int take = 50, int skip = 0) => new()
        {
            CaseWorkflowGuid = workflow.WorkflowGuid, View = view, Sort = CaseSearchSort.Age, Take = take, Skip = skip
        };

        private async Task<CaseRiskSeeder.Seeded> WorkflowAsync(DbContext dbContext)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, Me);

            return workflow;
        }

        private static Task ItemAsync(DbContext dbContext, int caseId, Guid statusGuid, Guid? definition = null,
            byte state = (byte)CaseChecklistItemState.Shown, int? dueInMinutes = null, bool breachStamped = false,
            DateTime? shown = null, DateTime? completed = null, string name = "Item")
        {
            var first = shown ?? DateTime.UtcNow.AddHours(-2);

            return dbContext.InsertAsync(new CaseChecklistItem
            {
                CaseId = caseId,
                CaseWorkflowChecklistItemGuid = definition ?? Guid.NewGuid(),
                CaseWorkflowChecklistGuid = Guid.NewGuid(),
                CaseWorkflowStatusGuid = statusGuid,
                ChecklistName = "List",
                ItemName = name,
                Priority = 0,
                StateId = state,
                FirstShownDate = first,
                ShownDate = first,
                DueDate = dueInMinutes.HasValue ? DateTime.UtcNow.AddMinutes(dueInMinutes.Value) : null,
                CompletedDate = completed,
                SlaBreachedDate = breachStamped ? DateTime.UtcNow.AddMinutes(-1) : null,
                CreatedDate = first
            });
        }

        [Fact]
        public async Task ThePeekReportsRequiredDoneOpenBreachedAndTheNextDueDateAndNeverCountsHiddenItemsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            await ItemAsync(dbContext, caseId, workflow.Neutral, dueInMinutes: 90);
            await ItemAsync(dbContext, caseId, workflow.Neutral, dueInMinutes: -30);
            await ItemAsync(dbContext, caseId, workflow.Neutral, dueInMinutes: 15, breachStamped: true);
            await ItemAsync(dbContext, caseId, workflow.Neutral);
            await ItemAsync(dbContext, caseId, workflow.Neutral, state: (byte)CaseChecklistItemState.Done,
                dueInMinutes: -600, completed: DateTime.UtcNow.AddHours(-1));
            await ItemAsync(dbContext, caseId, workflow.Neutral, state: (byte)CaseChecklistItemState.Hidden,
                dueInMinutes: -900);
            await ItemAsync(dbContext, caseId, workflow.Confirmed, dueInMinutes: -900);

            var earliest = await dbContext.GetTable<CaseChecklistItem>()
                .Where(w => w.CaseId == caseId && w.CaseWorkflowStatusGuid == workflow.Neutral
                                               && w.StateId == (byte)CaseChecklistItemState.Shown
                                               && w.DueDate != null).MinAsync(m => m.DueDate!.Value);

            var summary = await (await SummaryAsync(dbContext, Me)).GetAsync(caseId);

            summary.Checklist.Required.Should().Be(5);
            summary.Checklist.Done.Should().Be(1);
            summary.Checklist.Open.Should().Be(4);
            summary.Checklist.Breached.Should().Be(2);
            summary.Checklist.NextDueDate!.Value.UtcDateTime.Should().BeCloseTo(earliest, TimeSpan.FromSeconds(1));
            summary.Checklist.Complete.Should().BeFalse();
            summary.Case.ChecklistOpen.Should().Be(4);
            summary.Case.ChecklistBreached.Should().Be(2);
        }

        [Fact]
        public async Task ACaseNeverReconciledReportsZerosAndACompleteChecklistAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            var checklist = (await (await SummaryAsync(dbContext, Me)).GetAsync(caseId)).Checklist;

            checklist.Required.Should().Be(0);
            checklist.Done.Should().Be(0);
            checklist.Open.Should().Be(0);
            checklist.Breached.Should().Be(0);
            checklist.NextDueDate.Should().BeNull();
            checklist.Complete.Should().BeTrue();
        }

        [Fact]
        public async Task AClosedCaseReportsZerosEvenWhenItStillHoldsShownItemsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, closedStatusId: 3);

            await ItemAsync(dbContext, caseId, workflow.Confirmed, dueInMinutes: -30);

            var checklist = (await (await SummaryAsync(dbContext, Me)).GetAsync(caseId)).Checklist;

            checklist.Open.Should().Be(0);
            checklist.Breached.Should().Be(0);
            checklist.Complete.Should().BeTrue();
        }

        [Fact]
        public async Task EachRowCarriesItsOpenAndBreachedCountsAndOnlyTheStatusItIsInCountsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var clean = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "A");
            var breached = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "B");

            await ItemAsync(dbContext, breached, workflow.Neutral, dueInMinutes: -10);
            await ItemAsync(dbContext, breached, workflow.Neutral, dueInMinutes: 60);
            await ItemAsync(dbContext, breached, workflow.Neutral, state: (byte)CaseChecklistItemState.Done,
                dueInMinutes: -10, completed: DateTime.UtcNow.AddMinutes(-20));
            await ItemAsync(dbContext, breached, workflow.Confirmed, dueInMinutes: -10);

            var page = await (await SearchAsync(dbContext, Me)).RunAsync(Request(workflow, CaseSearchView.All));

            page.Rows.Single(r => r.CaseId == clean).ChecklistOpen.Should().Be(0);
            page.Rows.Single(r => r.CaseId == clean).ChecklistBreached.Should().Be(0);
            page.Rows.Single(r => r.CaseId == breached).ChecklistOpen.Should().Be(2);
            page.Rows.Single(r => r.CaseId == breached).ChecklistBreached.Should().Be(1);
        }

        [Fact]
        public async Task TheChecklistViewsListOnlyOpenCasesWithOpenOrBreachedItemsInTheCurrentStatusAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "A");
            var open = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "B");
            var breached = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "C");
            var stamped = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "D");
            var done = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "E");
            var hidden = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "F");
            var earlier = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "G");
            var closed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3, "H");

            await ItemAsync(dbContext, open, workflow.Neutral, dueInMinutes: 60);
            await ItemAsync(dbContext, breached, workflow.Neutral, dueInMinutes: -5);
            await ItemAsync(dbContext, stamped, workflow.Neutral, dueInMinutes: 30, breachStamped: true);
            await ItemAsync(dbContext, done, workflow.Neutral, state: (byte)CaseChecklistItemState.Done,
                dueInMinutes: -5, completed: DateTime.UtcNow);
            await ItemAsync(dbContext, hidden, workflow.Neutral, state: (byte)CaseChecklistItemState.Hidden,
                dueInMinutes: -5);
            await ItemAsync(dbContext, earlier, workflow.Confirmed, dueInMinutes: -5);
            await ItemAsync(dbContext, closed, workflow.Confirmed, dueInMinutes: -5);

            var service = await SearchAsync(dbContext, Me);

            (await service.RunAsync(Request(workflow, CaseSearchView.ChecklistBreached))).Rows
                .Select(r => r.CaseId).OrderBy(i => i).Should().Equal(new[] { breached, stamped }.OrderBy(i => i));
            (await service.RunAsync(Request(workflow, CaseSearchView.ChecklistOpen))).Rows
                .Select(r => r.CaseId).OrderBy(i => i).Should().Equal(new[] { open, breached, stamped }.OrderBy(i => i));

            var counts = await service.CountsAsync(workflow.WorkflowGuid);

            counts.Single(c => c.Kind == "View" && c.Key == ((int)CaseSearchView.ChecklistBreached).ToString())
                .Count.Should().Be(2);
            counts.Single(c => c.Kind == "View" && c.Key == ((int)CaseSearchView.ChecklistOpen).ToString())
                .Count.Should().Be(3);
        }

        [Fact]
        public async Task TheChecklistViewsStayInsideTheWorkflowAndTheRolesOfTheCallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var mine = await WorkflowAsync(dbContext);
            var theirs = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, theirs, Other);

            var inMine = await seeder.SeedCaseAsync(dbContext, mine, mine.Neutral, caseKeyValue: "A");
            var inTheirs = await seeder.SeedCaseAsync(dbContext, theirs, theirs.Neutral, caseKeyValue: "B");

            await ItemAsync(dbContext, inMine, mine.Neutral, dueInMinutes: -5);
            await ItemAsync(dbContext, inTheirs, theirs.Neutral, dueInMinutes: -5);

            var service = await SearchAsync(dbContext, Me);

            (await service.RunAsync(Request(mine, CaseSearchView.ChecklistBreached))).Rows
                .Select(r => r.CaseId).Should().Equal(inMine);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.RunAsync(Request(theirs, CaseSearchView.ChecklistBreached)));
            await Assert.ThrowsAsync<NotFoundException>(() => service.CountsAsync(theirs.WorkflowGuid));
        }

        [Fact]
        public async Task PagingTheBreachedViewVisitsEveryCaseOnceAndTheTotalStaysTheSameAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var expected = new List<int>();

            for (var i = 0; i < 7; i++)
            {
                var id = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: $"P{i}");
                await ItemAsync(dbContext, id, workflow.Neutral, dueInMinutes: -5);
                expected.Add(id);
            }

            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "Q");

            var service = await SearchAsync(dbContext, Me);
            var seen = new List<int>();
            var totals = new HashSet<long>();

            for (var skip = 0; skip < 9; skip += 3)
            {
                var page = await service.RunAsync(Request(workflow, CaseSearchView.ChecklistBreached, 3, skip));
                seen.AddRange(page.Rows.Select(r => r.CaseId));
                totals.Add(page.Total);
            }

            seen.Should().HaveCount(7);
            seen.Distinct().Should().HaveCount(7);
            seen.OrderBy(i => i).Should().Equal(expected.OrderBy(i => i));
            totals.Should().Equal(7);
        }

        [Fact]
        public async Task TheAnalyticsSectionCountsItemsFirstShownInThePeriodForTheWorkflowOnlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var other = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, other, Me);

            var one = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "A");
            var two = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "B");
            var elsewhere = await seeder.SeedCaseAsync(dbContext, other, other.Neutral, caseKeyValue: "C");
            var old = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "D");

            var definition = Guid.NewGuid();
            var shown = DateTime.UtcNow.AddHours(-5);

            await ItemAsync(dbContext, one, workflow.Neutral, definition, (byte)CaseChecklistItemState.Done, -240,
                shown: shown, completed: shown.AddMinutes(40), name: "Note");
            await ItemAsync(dbContext, two, workflow.Neutral, definition, (byte)CaseChecklistItemState.Done, -240,
                shown: shown, completed: shown.AddMinutes(100), name: "Note");
            await ItemAsync(dbContext, one, workflow.Neutral, Guid.NewGuid(), dueInMinutes: -30, shown: shown,
                name: "Contact");
            await ItemAsync(dbContext, old, workflow.Neutral, definition, (byte)CaseChecklistItemState.Done, -240,
                shown: DateTime.UtcNow.AddDays(-60), completed: DateTime.UtcNow.AddDays(-60), name: "Note");
            await ItemAsync(dbContext, elsewhere, other.Neutral, definition, (byte)CaseChecklistItemState.Done, -240,
                shown: shown, completed: shown.AddMinutes(10), name: "Note");

            var report = (await (await AnalyticsAsync(dbContext, Me)).GetAsync(new CaseAnalyticsRequestDto
            {
                CaseWorkflowGuid = workflow.WorkflowGuid
            })).ChecklistSla;

            report.Shown.Should().Be(3);
            report.Completed.Should().Be(2);
            report.Breached.Should().Be(1);

            var note = report.Items.Single(i => i.ItemName == "Note");

            note.Shown.Should().Be(2);
            note.Completed.Should().Be(2);
            note.MedianMinutes.Should().Be(70);
            note.P90Minutes.Should().BeApproximately(94, 1e-6);
            note.CompletedWithinSla.Should().Be(1);
            note.CompletedLate.Should().Be(1);
            note.Attainment.Should().Be(0.5);
            report.Items.Single(i => i.ItemName == "Contact").Breached.Should().Be(1);
        }
    }
}

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
using Jube.Data.Query.GetCaseChecklistItemValidationQuery;
using Jube.Data.Query.GetCaseChecklistSlaBreachCandidatesQuery;
using Jube.Data.Query.GetCaseKeyOutcomeHistoryQuery;
using Jube.Data.Query.GetCaseWorkflowOutcomeCountsQuery;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseAnalytics;
using Jube.Dto.Query.CaseSearch;
using Jube.Service.Query.CaseAnalytics;
using Jube.Service.Query.CaseChecklist;
using Jube.Service.Query.CaseSearch;
using Jube.Service.Reactivity;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Preservation
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PreservationImportTwinsTests(DatabaseFixture fx) : IAsyncLifetime
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
        private readonly List<Guid> checklistItemGuids = [];
        private readonly List<Guid> checklistGuids = [];

        private string Me => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GetTable<CaseChecklistEvent>().Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await dbContext.GetTable<CaseChecklistItem>().Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await dbContext.GetTable<CaseWorkflowChecklistItem>().Where(w => checklistItemGuids.Contains(w.Guid)).DeleteAsync();
            await dbContext.GetTable<CaseWorkflowChecklist>().Where(w => checklistGuids.Contains(w.Guid)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private async Task<CaseRiskSeeder.Seeded> TwinnedWorkflowAsync(DbContext dbContext)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, Me);
            await seeder.SeedImportTwinsAsync(dbContext, workflow);

            return workflow;
        }

        private static async Task<int> TenantAsync(DbContext dbContext, CaseRiskSeeder.Seeded workflow) =>
            await dbContext.CaseWorkflow
                .Where(w => w.Guid == workflow.WorkflowGuid && (w.Deleted == 0 || w.Deleted == null))
                .Select(w => w.EntityAnalysisModel.TenantRegistryId ?? 0).FirstAsync();

        private static Task<CaseSearchService> SearchAsync(DbContext dbContext, string user) =>
            CaseSearchService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                dynamicEnvironment, TestLog.NoOp);

        private static Task<CaseAnalyticsService> AnalyticsAsync(DbContext dbContext, string user) =>
            CaseAnalyticsService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                dynamicEnvironment, TestLog.NoOp);

        private static CaseSearchRequestDto Request(CaseRiskSeeder.Seeded workflow, CaseSearchSort sort) => new()
        {
            CaseWorkflowGuid = workflow.WorkflowGuid, View = CaseSearchView.All, Sort = sort, Take = 50, Skip = 0
        };

        [Fact]
        public async Task TheSeededTwinsReallyMultiplyAnUnfilteredGuidJoinAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await TwinnedWorkflowAsync(dbContext);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            var unfiltered = await (from c in dbContext.Case
                join s in dbContext.CaseWorkflowStatus on c.CaseWorkflowStatusGuid equals s.Guid
                join w in dbContext.CaseWorkflow on c.CaseWorkflowGuid equals w.Guid
                where c.Id == caseId
                select c.Id).CountAsync();

            unfiltered.Should().Be(4,
                "a live row and a soft-deleted twin on both the workflow and the status is exactly what a preservation import leaves behind");
        }

        [Theory]
        [InlineData(CaseSearchSort.Triage)]
        [InlineData(CaseSearchSort.Priority)]
        [InlineData(CaseSearchSort.Age)]
        [InlineData(CaseSearchSort.Due)]
        [InlineData(CaseSearchSort.Value)]
        [InlineData(CaseSearchSort.Likelihood)]
        [InlineData(CaseSearchSort.Newest)]
        public async Task TheCasesPageListsEachCaseOnceWhateverTheSortWhenWorkflowAndStatusesCarryTwinsAsync(
            CaseSearchSort sort)
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await TwinnedWorkflowAsync(dbContext);

            var first = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "A");
            var second = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "B");
            var third = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, caseKeyValue: "C");

            var page = await (await SearchAsync(dbContext, Me)).RunAsync(Request(workflow, sort));

            page.Total.Should().Be(3);
            page.Rows.Select(r => r.CaseId).Should().BeEquivalentTo([first, second, third],
                "each case must appear exactly once, not once per soft-deleted twin");
        }

        [Fact]
        public async Task TheCasesPageNamesTheLiveStatusAndNeverTheDeadTwinAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await TwinnedWorkflowAsync(dbContext);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            var page = await (await SearchAsync(dbContext, Me)).RunAsync(Request(workflow, CaseSearchSort.Triage));

            var live = await dbContext.CaseWorkflowStatus
                .Where(w => w.Guid == workflow.Neutral && (w.Deleted == 0 || w.Deleted == null))
                .Select(w => w.Name).SingleAsync();

            page.Rows.Single(r => r.CaseId == caseId).StatusName.Should().Be(live);
        }

        [Fact]
        public async Task TheCasesCountsAndTheRiskMatrixAgreeWithThePageWhenTwinsExistAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await TwinnedWorkflowAsync(dbContext);

            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "A");
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "B");

            var service = await SearchAsync(dbContext, Me);
            var request = Request(workflow, CaseSearchSort.Triage);

            var page = await service.RunAsync(request);
            var matrix = await service.RiskMatrixAsync(request);

            page.Rows.Should().HaveCount(2);
            matrix.Cells.Sum(c => c.Cases).Should().Be(2);
        }

        [Fact]
        public async Task TheAnalyticsReportCountsEachCaseOnceWhenWorkflowAndStatusesCarryTwinsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await TwinnedWorkflowAsync(dbContext);

            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "A");
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "B");
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3, "C");

            var report = await (await AnalyticsAsync(dbContext, Me)).GetAsync(
                new CaseAnalyticsRequestDto { CaseWorkflowGuid = workflow.WorkflowGuid });

            report.Flow.Sum(day => day.Opened).Should().Be(3);
            report.Flow.Sum(day => day.Closed).Should().Be(1);
            report.Aging.Open.Should().Be(2);
        }

        [Fact]
        public async Task TheWorkflowOutcomeCountsUseTheLiveStatusOutcomeAndCountEachCaseOnceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await TwinnedWorkflowAsync(dbContext);
            await seeder.SeedClosedAsync(dbContext, workflow, 2, 1);

            var counts = await new GetCaseWorkflowOutcomeCountsQuery(dbContext).ExecuteAsync(workflow.WorkflowGuid);

            counts.Closed.Should().Be(3);
            counts.Confirmed.Should().Be(2,
                "the dead twins carry the opposite outcomes, so a lookup that reaches them gets the confirmed count wrong as well as doubling it");
        }

        [Fact]
        public async Task TheKeyOutcomeHistoryUsesTheLiveStatusOutcomeAndCountsEachCaseOnceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await TwinnedWorkflowAsync(dbContext);
            await seeder.SeedClosedAsync(dbContext, workflow, 2, 1);

            var history = await new GetCaseKeyOutcomeHistoryQuery(dbContext).ExecuteAsync(workflow.WorkflowGuid,
                "AccountId", "HISTORY", 0);

            history.Closed.Should().Be(3);
            history.Confirmed.Should().Be(2);
        }

        [Fact]
        public async Task ChecklistItemsAndEventsListOnceForACaseInATwinnedWorkflowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await TwinnedWorkflowAsync(dbContext);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var tenant = await TenantAsync(dbContext, workflow);

            await dbContext.InsertAsync(new CaseChecklistItem
            {
                CaseId = caseId,
                CaseWorkflowChecklistItemGuid = Guid.NewGuid(),
                CaseWorkflowChecklistGuid = Guid.NewGuid(),
                CaseWorkflowStatusGuid = workflow.Neutral,
                ChecklistName = "List",
                ItemName = "Item",
                Priority = 0,
                StateId = 1,
                FirstShownDate = DateTime.UtcNow,
                ShownDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            });

            await new CaseChecklistEventRepository(dbContext, tenant).InsertAsync(new CaseChecklistEvent
            {
                CaseId = caseId, ActionId = 1, Actor = Me, CreatedDate = DateTime.UtcNow
            });

            (await new CaseChecklistItemRepository(dbContext, tenant).ListForCaseAsync(caseId)).Should().ContainSingle();
            (await new CaseChecklistEventRepository(dbContext, tenant).ListForCaseAsync(caseId)).Should()
                .ContainSingle();
            (await new CaseChecklistEventRepository(dbContext, tenant).ListRecentForCaseAsync(caseId, 10)).Should()
                .ContainSingle();
        }

        [Fact]
        public async Task TheChecklistSlaSweepOffersEachBreachedItemOnceInATwinnedWorkflowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await TwinnedWorkflowAsync(dbContext);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var tenant = await TenantAsync(dbContext, workflow);

            await dbContext.InsertAsync(new CaseChecklistItem
            {
                CaseId = caseId,
                CaseWorkflowChecklistItemGuid = Guid.NewGuid(),
                CaseWorkflowChecklistGuid = Guid.NewGuid(),
                CaseWorkflowStatusGuid = workflow.Neutral,
                ChecklistName = "List",
                ItemName = "Overdue",
                Priority = 0,
                StateId = 1,
                FirstShownDate = DateTime.UtcNow.AddHours(-3),
                ShownDate = DateTime.UtcNow.AddHours(-3),
                DueDate = DateTime.UtcNow.AddHours(-1),
                CreatedDate = DateTime.UtcNow.AddHours(-3)
            });

            var candidates = (await new GetCaseChecklistSlaBreachCandidatesQuery(dbContext)
                .ExecuteAsync(DateTime.UtcNow, 100000)).Where(c => c.CaseId == caseId).ToList();

            candidates.Should().ContainSingle();
            candidates[0].TenantRegistryId.Should().Be(tenant);
        }

        [Fact]
        public async Task TheCaseChecklistLoadsWhenItsChecklistDefinitionCarriesASoftDeletedTwinAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await TwinnedWorkflowAsync(dbContext);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var checklistGuid = Guid.NewGuid();
            checklistGuids.Add(checklistGuid);

            foreach (var (priority, deleted) in new[] { (1, (byte)0), (9, (byte)1) })
            {
                await dbContext.InsertAsync(new CaseWorkflowChecklist
                {
                    Guid = checklistGuid,
                    CaseWorkflowStatusGuid = workflow.Neutral,
                    Name = $"{DatabaseFixture.Prefix}Cl{Guid.NewGuid():N}"[..30],
                    Priority = priority,
                    Active = deleted == 0 ? (byte)1 : (byte)0,
                    Deleted = deleted,
                    CreatedUser = Me,
                    CreatedDate = DateTime.UtcNow
                });
            }

            await dbContext.InsertAsync(new CaseChecklistItem
            {
                CaseId = caseId,
                CaseWorkflowChecklistItemGuid = Guid.NewGuid(),
                CaseWorkflowChecklistGuid = checklistGuid,
                CaseWorkflowStatusGuid = workflow.Neutral,
                ChecklistName = "List",
                ItemName = "Item",
                Priority = 0,
                StateId = 1,
                FirstShownDate = DateTime.UtcNow,
                ShownDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            });

            var service = await CaseChecklistService.CreateAsync(dbContext, Me, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), dynamicEnvironment);

            var checklist = await service.GetAsync(caseId);

            checklist.Should().NotBeNull(
                "two checklist definitions sharing a Guid used to throw an item-with-the-same-key error and fail the page with a 500");
        }

        [Fact]
        public async Task TheChecklistItemValidationReadsTheLiveItemAndNeverTheDeadTwinAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await TwinnedWorkflowAsync(dbContext);
            var itemGuid = Guid.NewGuid();
            checklistItemGuids.Add(itemGuid);

            var checklistGuid = Guid.NewGuid();
            checklistGuids.Add(checklistGuid);

            await dbContext.InsertAsync(new CaseWorkflowChecklist
            {
                Guid = checklistGuid,
                CaseWorkflowStatusGuid = workflow.Neutral,
                Name = $"{DatabaseFixture.Prefix}Checklist{Guid.NewGuid():N}"[..30],
                Active = 1,
                Deleted = 0,
                CreatedUser = Me,
                CreatedDate = DateTime.UtcNow
            });

            await dbContext.InsertAsync(new CaseWorkflowChecklistItem
            {
                Guid = itemGuid,
                CaseWorkflowChecklistGuid = checklistGuid,
                Name = "Dead",
                ValidateSql = "select 0",
                EnableValidation = 1,
                Active = 0,
                Deleted = 1,
                CreatedUser = Me,
                CreatedDate = DateTime.UtcNow
            });

            await dbContext.InsertAsync(new CaseWorkflowChecklistItem
            {
                Guid = itemGuid,
                CaseWorkflowChecklistGuid = checklistGuid,
                Name = "Live",
                ValidateSql = "select 1",
                EnableValidation = 1,
                Active = 1,
                Deleted = 0,
                CreatedUser = Me,
                CreatedDate = DateTime.UtcNow
            });

            var validation = await new GetCaseChecklistItemValidationQuery(dbContext).ExecuteAsync(itemGuid);

            validation.ItemName.Should().Be("Live");
            validation.ValidateSql.Should().Be("select 1");
        }
    }
}

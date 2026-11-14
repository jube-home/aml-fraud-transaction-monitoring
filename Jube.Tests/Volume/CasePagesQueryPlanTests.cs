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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Query.ArchiveKeyLookup;
using Jube.Data.Query.GetCaseAnalyticsFactsQuery;
using Jube.Data.Query.GetCaseKeyJournalMatchQuery;
using Jube.Data.Query.GetCaseKeyJournalRowsQuery;
using Jube.Data.Query.GetCaseSimilarityDueQuery;
using Jube.Data.Query.GetCaseTriageCountsQuery;
using Jube.Data.Query.GetCaseTriagePageQuery;
using Jube.Data.Query.GetGraphNodeCasesQuery;
using Jube.Data.Query.GetSimilarCaseComponentsQuery;
using Jube.Dto.Options;
using Jube.Migrations.Branches.ArchiveKeyIndexing;
using Jube.Service.Query.CaseSearch;
using Jube.Service.Query.CaseSimilarity;
using Jube.Service.Repository.SessionCaseSearchCompiledSql;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using Newtonsoft.Json;
using Npgsql;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.Volume
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CasePagesQueryPlanTests(DatabaseFixture fx, CasePagesQueryPlanHolder holder,
        ITestOutputHelper output) : IClassFixture<CasePagesQueryPlanHolder>
    {
        private const string EmptyRules = "{\"condition\":\"AND\",\"rules\":[]}";
        private const int Take = 50;

        private static readonly string[] HotCaseTables = ["Case", "CaseEvent", "CaseRiskAssessment"];

        private Task<CasePagesQueryPlanWorld> WorldAsync() => holder.GetAsync(fx);

        private async Task<(string WhereSql, List<object> Tokens)> ScopeAsync(CasePagesQueryPlanWorld world)
        {
            await using var db = fx.GetDbContext();
            var compiled = await SessionCaseSearchCompiler.CompileAsync(db, world.Workflow.WorkflowGuid, null,
                EmptyRules, EmptyRules, world.User, TestLog.NoOp, fx.ConnectionString, default, false, false);

            return (compiled.WhereSql,
                JsonConvert.DeserializeObject<List<object>>(compiled.FilterTokens ?? "[]") ?? []);
        }

        private static void NoSequentialScans(QueryPlan plan, params string[] relations)
        {
            foreach (var relation in relations)
            {
                plan.SeqScans(relation).Should().BeEmpty("{0} is a hot table and must be reached through an index",
                    relation);
            }
        }

        [Fact]
        public async Task EveryIndexTheCasePagesMigrationCreatesExistsAndIsValidAsync()
        {
            await WorldAsync();
            await using var db = fx.GetDbContext();

            var created = AddCaseAndCasesPageIndexes.Statements
                .Where(s => s.StartsWith("CREATE INDEX", StringComparison.Ordinal))
                .Select(s => Regex.Match(s, "\"(IX_[A-Za-z0-9_]+)\"").Groups[1].Value).ToList();
            var dropped = AddCaseAndCasesPageIndexes.Statements
                .Where(s => s.StartsWith("DROP INDEX", StringComparison.Ordinal))
                .Select(s => Regex.Match(s, "\"(IX_[A-Za-z0-9_]+)\"").Groups[1].Value).ToList();

            created.Should().NotBeEmpty();

            var state = (await db.QueryToListAsync<IndexValidity>(
                "select c.relname as \"Name\", i.indisvalid as \"Valid\" from pg_class c join pg_index i on i.indexrelid = c.oid " +
                "where c.relname = any(@names)", new DataParameter("names", created.Append("IX_ArchiveKey_Entry_Request").ToArray())))
                .ToDictionary(r => r.Name, r => r.Valid);

            foreach (var name in created.Append("IX_ArchiveKey_Entry_Request"))
            {
                state.Should().ContainKey(name, "the migration creates {0}", name);
                state[name].Should().BeTrue("{0} must be valid, not left half built", name);
            }

            foreach (var name in dropped)
            {
                var exists = await db.QueryToListAsync<string>(
                    "select relname from pg_class where relname = @name", new DataParameter("name", name));
                exists.Should().BeEmpty("{0} was dropped by the migration", name);
            }
        }

        [Fact]
        public async Task TheTriagePageReadsABoundedCandidateSetNotTheWholeQueueAsync()
        {
            var world = await WorldAsync();
            var (whereSql, tokens) = await ScopeAsync(world);
            var parameters = new List<object>(tokens) { world.User };
            var extra = CaseTriageSql.Resolve(CaseSearchViewSql.Condition(CaseSearchView.All), parameters.Count);
            var sql = CaseTriageSql.Page(whereSql, extra, parameters.Count, CaseSearchSort.Triage, Take, 0);

            var plan = await QueryPlan.ExplainAsync(fx.ConnectionString, sql, parameters);
            output.WriteLine(plan.Witness("triage page, triage order"));

            NoSequentialScans(plan, HotCaseTables);
            plan.Nodes[0].Rows.Should().BeLessOrEqualTo(Take);

            var candidates = Take + world.HeldCases + world.QaSamples;
            plan.MaxLoops("IX_CaseRiskAssessment_CaseId_Sequence_Desc").Should().BeLessOrEqualTo(candidates)
                .And.BeLessThan(world.OpenCases,
                    "the latest assessment is read for the candidates only, never for every open case");
        }

        [Theory]
        [InlineData(CaseSearchSort.Priority)]
        [InlineData(CaseSearchSort.Age)]
        [InlineData(CaseSearchSort.Due)]
        [InlineData(CaseSearchSort.Value)]
        [InlineData(CaseSearchSort.Likelihood)]
        [InlineData(CaseSearchSort.Newest)]
        public async Task EveryOtherSortReadsOnlyThePageItReturnsAsync(CaseSearchSort sort)
        {
            var world = await WorldAsync();
            var (whereSql, tokens) = await ScopeAsync(world);
            var parameters = new List<object>(tokens) { world.User };
            var extra = CaseTriageSql.Resolve(CaseSearchViewSql.Condition(CaseSearchView.All), parameters.Count);
            var sql = CaseTriageSql.Page(whereSql, extra, parameters.Count, sort, Take, 0);

            var plan = await QueryPlan.ExplainAsync(fx.ConnectionString, sql, parameters);
            output.WriteLine(plan.Witness("triage page, " + sort));

            NoSequentialScans(plan, HotCaseTables);
            plan.Nodes[0].Rows.Should().BeLessOrEqualTo(Take);
            plan.MaxLoops("IX_CaseRiskAssessment_CaseId_Sequence_Desc").Should().BeLessOrEqualTo(Take);
        }

        [Fact]
        public async Task TheRailCountsRunAsOneStatementAndAgreeWithTheSeedAsync()
        {
            var world = await WorldAsync();
            var (whereSql, tokens) = await ScopeAsync(world);
            var views = Enum.GetValues<CaseSearchView>();
            var conditions = views.Select(CaseSearchViewSql.Condition).ToList();
            var parameters = new List<object>(tokens) { world.User };
            var resolved = conditions.Select(c => CaseTriageSql.Resolve(c, parameters.Count)).ToList();
            var sql = CaseTriageSql.Counts(whereSql, resolved);

            var plan = await QueryPlan.ExplainAsync(fx.ConnectionString, sql, parameters);
            output.WriteLine(plan.Witness("rail counts"));

            NoSequentialScans(plan, "CaseEvent", "CaseRiskAssessment");

            var counts = await new GetCaseTriageCountsQuery(fx.ConnectionString, TestLog.NoOp)
                .ExecuteAsync(whereSql, tokens, conditions, world.User);

            counts[Array.IndexOf(views, CaseSearchView.All)].Should().Be(world.OpenCases);
            counts[Array.IndexOf(views, CaseSearchView.Mine)].Should().Be(world.HeldCases);
            world.QaSamples.Should().BeGreaterThan(0);
            counts[Array.IndexOf(views, CaseSearchView.QaSamples)].Should().Be(world.QaSamples);
        }

        [Fact]
        public async Task TheAnalyticsFactsReachTheirEventsThroughTheCaseEventIndexAsync()
        {
            var world = await WorldAsync();
            var (whereSql, tokens) = await ScopeAsync(world);
            var to = DateTime.UtcNow;
            var from = to.AddDays(-30);
            var parameters = new List<object>(tokens) { from, to };
            var sql = GetCaseAnalyticsFactsQuery.Statement(whereSql, parameters.Count);

            var plan = await QueryPlan.ExplainAsync(fx.ConnectionString, sql, parameters);
            output.WriteLine(plan.Witness("analytics facts, 30 days"));

            plan.Uses("IX_CaseEvent_CaseId_Id").Should().BeTrue();
            NoSequentialScans(plan, "CaseEvent");

            await using var db = fx.GetDbContext();
            var expected = await db.Case.CountAsync(c => c.CaseWorkflowGuid == world.Workflow.WorkflowGuid
                && ((c.CreatedDate >= from && c.CreatedDate < to) || (c.ClosedDate >= from && c.ClosedDate < to)
                    || c.ClosedStatusId == 0 || c.ClosedStatusId == 1));

            plan.Nodes[0].Rows.Should().Be(Math.Min(expected, GetCaseAnalyticsFactsQuery.MaximumRows + 1));
        }

        [Fact]
        public async Task TheNetworkCasesPerNodeUseTheKeyIndexAsync()
        {
            var world = await WorldAsync();
            await using var db = fx.GetDbContext();
            var values = await db.Case.Where(c => c.CaseWorkflowGuid == world.Workflow.WorkflowGuid)
                .Select(c => c.CaseKeyValue).Distinct().OrderBy(v => v).Take(3).ToListAsync();

            var plan = await QueryPlan.ExplainAsync(fx.ConnectionString, GetGraphNodeCasesQuery.Sql,
                new NpgsqlParameter("tenant", world.TenantRegistryId),
                new NpgsqlParameter("user", world.User),
                new NpgsqlParameter("keys", values.Select(_ => "AccountId").ToArray()),
                new NpgsqlParameter("values", values.ToArray()));
            output.WriteLine(plan.Witness("network cases per node"));

            plan.Uses("IX_Case_Key_Value_Workflow_Closed").Should().BeTrue();
            NoSequentialScans(plan, "Case");
            plan.Nodes[0].Rows.Should().Be(values.Count);
        }

        [Fact]
        public async Task TheJournalForAHeavyKeyTouchesOnlyTheTransactionsThatMatchAsync()
        {
            var world = await WorldAsync();
            var tokens = new List<object>();
            var sql = GetCaseKeyJournalRowsQuery.Statement(world.ModelId, world.User,
                ArchiveKeyPredicate.RequestXPath("AccountId", ArchiveKeyValueColumn.String, world.HeavyAccount),
                DateTime.UtcNow.AddYears(-5), DateTime.UtcNow.AddDays(1), 0, 0, 501, tokens);

            var plan = await QueryPlan.ExplainAsync(fx.ConnectionString, sql, tokens);
            output.WriteLine(plan.Witness("journal rows, heaviest key (" + world.HeavyAccountRows + " rows)"));

            plan.Uses("IX_ArchiveKey_Key_KeyValueString").Should().BeTrue();
            plan.Uses("IX_Archive_EntityAnalysisModelInstanceEntryGuid").Should().BeTrue();
            NoSequentialScans(plan, "Archive", "ArchiveKey");
            plan.RowsTouched("Archive").Should().BeLessOrEqualTo(world.HeavyAccountRows,
                "the cost is O(matches), not O(table)");
            plan.Nodes[0].Rows.Should().Be(Math.Min(501, world.HeavyAccountRows));
        }

        [Fact]
        public async Task TheJournalForALightKeyReturnsExactlyItsTransactionsAsync()
        {
            var world = await WorldAsync();
            var tokens = new List<object>();
            var sql = GetCaseKeyJournalRowsQuery.Statement(world.ModelId, world.User,
                ArchiveKeyPredicate.RequestXPath("AccountId", ArchiveKeyValueColumn.String, world.LightAccount),
                DateTime.UtcNow.AddYears(-5), DateTime.UtcNow.AddDays(1), 0, 0, 501, tokens);

            var plan = await QueryPlan.ExplainAsync(fx.ConnectionString, sql, tokens);
            output.WriteLine(plan.Witness("journal rows, light key (" + world.LightAccountRows + " rows)"));

            NoSequentialScans(plan, "Archive", "ArchiveKey");
            plan.Nodes[0].Rows.Should().Be(world.LightAccountRows);
        }

        [Fact]
        public async Task TheJournalCappedCountUsesTheArchiveKeyIndexAsync()
        {
            var world = await WorldAsync();
            var tokens = new List<object>();
            var sql = GetCaseKeyJournalMatchQuery.CountStatement(world.ModelId, world.User,
                ArchiveKeyPredicate.RequestXPath("AccountId", ArchiveKeyValueColumn.String, world.HeavyAccount),
                DateTime.UtcNow.AddYears(-5), DateTime.UtcNow.AddDays(1), 0, 0, 20000, tokens);

            var plan = await QueryPlan.ExplainAsync(fx.ConnectionString, sql, tokens);
            output.WriteLine(plan.Witness("journal capped count, heaviest key"));

            plan.Uses("IX_ArchiveKey_Key_KeyValueString").Should().BeTrue();
            NoSequentialScans(plan, "Archive", "ArchiveKey");
        }

        // ReSharper disable UnusedAutoPropertyAccessor.Local
        [Fact]
        public async Task SimilarCasesAreFoundThroughTheComponentLookupFromABoundedCandidateSetAsync()
        {
            var world = await WorldAsync();
            await using var db = fx.GetDbContext();
            var caseId = await db.Case.Where(c => c.CaseWorkflowGuid == world.Workflow.WorkflowGuid)
                .OrderBy(c => c.Id).Select(c => c.Id).FirstAsync();

            var plan = await QueryPlan.ExplainAsync(fx.ConnectionString, GetSimilarCaseComponentsQuery.Statement,
                new NpgsqlParameter("case", caseId),
                new NpgsqlParameter("workflow", world.Workflow.WorkflowGuid),
                new NpgsqlParameter("seeds", CaseSimilarityService.Seeds),
                new NpgsqlParameter("seedCap", CaseSimilarityService.SeedCap),
                new NpgsqlParameter("candidates", CaseSimilarityService.Candidates));
            output.WriteLine(plan.Witness("similar case components"));

            plan.Uses("IX_CaseSimilarityComponent_Lookup").Should().BeTrue();
            NoSequentialScans(plan, "Case", "CaseSimilarityComponent", "CaseSimilarityComponentFrequency");
            plan.Nodes[0].Rows.Should().BeInRange(8, 4 * (CaseSimilarityService.Candidates + 1));
        }

        [Fact]
        public async Task TheSimilaritySweepFindsItsNextCasesThroughTheDueIndexAsync()
        {
            await WorldAsync();

            var plan = await QueryPlan.ExplainAsync(fx.ConnectionString, GetCaseSimilarityDueQuery.Statement,
                new NpgsqlParameter("stale", NpgsqlTypes.NpgsqlDbType.Timestamp)
                    { Value = DateTime.UtcNow.AddMinutes(-30) },
                new NpgsqlParameter("take", 500));
            output.WriteLine(plan.Witness("similarity sweep due cases"));

            plan.Uses("IX_Case_SimilarityDue").Should().BeTrue();
            NoSequentialScans(plan, "Case");
            plan.Nodes[0].Rows.Should().Be(500);
        }

        private sealed class IndexValidity
        {
            public string Name { get; set; } = null!;
            public bool Valid { get; set; }
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Local
    }
}

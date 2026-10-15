/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.Tools.SqlQuery.Interfaces;
using Jube.AskJooby.SqlQuery.Models;
using Jube.AskJooby.Tools.SqlQuery;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class SqlPreFlightToolTests
    {
        [Fact]
        public async Task ValidateSqlQueryReportsTheDatabaseErrorRatherThanAGenericFailureAsync()
        {
            var tool = Build(new SqlValidation(false, "column \"Nmae\" does not exist", "42703"));

            var result = await tool.ValidateSqlQuery("SELECT \"Nmae\" FROM \"Case\"");

            result.Should().Contain("Nmae",
                "the whole point of validating is to hand back the exact thing the database objected to");

            result.Should().Contain("42703");
        }

        [Fact]
        public async Task ValidateSqlQuerySaysNothingAboutCostWhenTheStatementIsValidAsync()
        {
            var tool = Build(new SqlValidation(true, null, null));

            var result = await tool.ValidateSqlQuery("SELECT 1 FROM \"Case\"");

            result.Should().Contain("Valid");

            result.Should().Contain("ExplainSqlQuery",
                "a valid statement can still be ruinous, so the next step has to be named");
        }

        [Fact]
        public async Task ExplainSqlQueryNamesTheRelationsScannedSequentiallyAsync()
        {
            var tool = Build(plan: new SqlPlanSummary(81234.5, 0, 12, ["Archive"], [],
                ["Seq Scan on \"Archive\"  (cost=0.00..81234.50 rows=12 width=64)"]));

            var result = await tool.ExplainSqlQuery("SELECT * FROM \"Archive\"");

            result.Should().Contain("Sequential scan on Archive");
            result.Should().Contain("81234.5");
        }

        [Fact]
        public async Task InspectTableStatisticsWarnsWhenTheTableIsLargeAsync()
        {
            var tool = Build(statistics: [new SqlTableStatistic("Archive", 4000000, 3, 912)]);

            var result = await tool.InspectTableStatistics("SELECT * FROM \"Archive\"");

            result.Should().Contain("large table",
                "a million-row archive and a ten-row configuration table need different treatment");
        }

        [Fact]
        public async Task InspectTableStatisticsStaysQuietForASmallTableAsync()
        {
            var tool = Build(statistics: [new SqlTableStatistic("CaseWorkflow", 12, 400, 2)]);

            var result = await tool.InspectTableStatistics("SELECT * FROM \"CaseWorkflow\"");

            result.Should().NotContain("large table");
            result.Should().Contain("CaseWorkflow");
        }

        [Fact]
        public async Task AnalyseQueryHistoryFallsThroughToExplainWhenNothingWasRecordedAsync()
        {
            var tool = Build(history: new SqlQueryHistory(false, 0, 0, 0, 0, 0));

            var result = await tool.AnalyseQueryHistory("SELECT * FROM \"Case\"");

            result.Should().Contain("ExplainSqlQuery",
                "with no history the cold plan is the only evidence available");
        }

        [Fact]
        public async Task AnalyseQueryHistoryCallsOutSkewWhenTheWorstCaseDwarfsTheAverageAsync()
        {
            var tool = Build(history: new SqlQueryHistory(true, 500, 10, 4000, 20, 99));

            var result = await tool.AnalyseQueryHistory("SELECT * FROM \"Case\"");

            result.Should().Contain("more than ten times the average");

            result.Should().NotContain("explicit LIMIT",
                "twenty rows a call is not a runaway return and should not be warned about");
        }

        [Fact]
        public async Task AnalyseQueryHistoryCallsOutDiskReadingAndWideReturnsTogetherAsync()
        {
            var tool = Build(history: new SqlQueryHistory(true, 400, 50, 60, 9000, 40));

            var result = await tool.AnalyseQueryHistory("SELECT * FROM \"Archive\"");

            result.Should().Contain("cache");
            result.Should().Contain("explicit LIMIT");

            result.Should().NotContain("more than ten times the average",
                "sixty against fifty is predictable, and a false skew warning would teach the wrong lesson");
        }

        [Fact]
        public async Task AnalyseQueryHistoryStaysQuietOnABusyButWellBehavedShapeAsync()
        {
            var tool = Build(history: new SqlQueryHistory(true, 50000, 2, 5, 12, 99.8));

            var result = await tool.AnalyseQueryHistory("SELECT * FROM \"CaseWorkflow\"");

            result.Should().Contain("50,000");
            result.Should().NotContain("more than ten times the average");
            result.Should().NotContain("explicit LIMIT");
            result.Should().NotContain("going to disk");
        }

        private static RunSqlQueryTool Build(
            SqlValidation? validation = null,
            SqlPlanSummary? plan = null,
            IReadOnlyList<SqlTableStatistic>? statistics = null,
            SqlQueryHistory? history = null)
        {
            return new RunSqlQueryTool(
                new StubSqlQueryClient(
                    validation ?? new SqlValidation(true, null, null),
                    plan ?? new SqlPlanSummary(0, 0, 0, [], [], []),
                    statistics ?? [],
                    history ?? new SqlQueryHistory(false, 0, 0, 0, 0, 0)),
                new StubCache());
        }

        private sealed class StubSqlQueryClient(
            SqlValidation validation,
            SqlPlanSummary plan,
            IReadOnlyList<SqlTableStatistic> statistics,
            SqlQueryHistory history) : ISqlQueryClient
        {
            public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExecuteAsync(
                string sql, CancellationToken token = default)
            {
                throw new NotSupportedException("These tests never execute a query.");
            }

            public Task<SqlValidation> ValidateAsync(string sql, CancellationToken token = default)
            {
                return Task.FromResult(validation);
            }

            public Task<SqlPlanSummary> ExplainAsync(string sql, CancellationToken token = default)
            {
                return Task.FromResult(plan);
            }

            public Task<IReadOnlyList<SqlTableStatistic>> TableStatisticsAsync(
                string sql, CancellationToken token = default)
            {
                return Task.FromResult(statistics);
            }

            public Task<SqlQueryHistory> QueryHistoryAsync(string sql, CancellationToken token = default)
            {
                return Task.FromResult(history);
            }

            public IReadOnlyList<string> ListQueryableTables()
            {
                return ["Case", "CaseWorkflow", "Archive"];
            }

            public Task<IReadOnlyDictionary<string, string>> TableCommentsAsync(IReadOnlyList<string> tables,
                CancellationToken token = default)
            {
                throw new NotSupportedException("These tests never execute a query.");
            }
        }

        private sealed class StubCache : IToolResultCache
        {
            public Task<string> StoreAsync(
                CachedTable table, string? label = null, TimeSpan? ttl = null, CancellationToken token = default)
            {
                throw new NotSupportedException("These tests never store anything.");
            }

            public Task<CachedTable?> GetAsync(string pointerKey, CancellationToken token = default)
            {
                throw new NotSupportedException("These tests never read anything back.");
            }
        }
    }
}
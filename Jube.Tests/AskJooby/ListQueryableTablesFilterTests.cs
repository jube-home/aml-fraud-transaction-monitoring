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
    [Trait("Category", "Unit")]
    public sealed class ListQueryableTablesFilterTests
    {
        [Fact]
        public async Task AFilteredListingCarriesOnlyWhatWasAskedForAsync()
        {
            var tool = new RunSqlQueryTool(new FakeQueryClient(), new FakeCache());

            var filtered = await tool.ListQueryableTables("case");

            filtered.Should().Contain("CaseWorkflow").And.Contain("Case");

            filtered.Should().NotContain("EntityAnalysisModel",
                "a filtered call should only name the tables actually asked about");

            filtered.Should().NotContain("pg_stat_activity");
        }

        [Fact]
        public async Task AFilteredListingCarriesTheDocumentationCommentForEachMatchAsync()
        {
            var tool = new RunSqlQueryTool(new FakeQueryClient(), new FakeCache());

            var filtered = await tool.ListQueryableTables("case");

            filtered.Should().Contain("identified by CaseKey and CaseKeyValue",
                "the whole point is to settle Case versus CaseWorkflow without a separate Pasture search");

            filtered.Should().Contain("CaseWorkflow (no documentation comment recorded)",
                "a table with no comment should say so rather than silently dropping it");
        }

        [Fact]
        public async Task AnUnfilteredListingStillWorksAndCarriesCommentsTooAsync()
        {
            var tool = new RunSqlQueryTool(new FakeQueryClient(), new FakeCache());

            var all = await tool.ListQueryableTables();

            all.Should().Contain("EntityAnalysisModel").And.Contain("pg_stat_activity");

            all.Should().Contain("identified by CaseKey and CaseKeyValue",
                "none of these comments are big enough to be worth withholding, even unfiltered");
        }

        [Fact]
        public async Task AFilterMatchingNothingSaysSoRatherThanInvitingAGuessAsync()
        {
            var tool = new RunSqlQueryTool(new FakeQueryClient(), new FakeCache());

            var none = await tool.ListQueryableTables("nosuchthing");

            none.Should().Contain("Do not guess",
                "a guessed table name fails the whole query, so the dead end must be explicit");
        }

        private sealed class FakeQueryClient : ISqlQueryClient
        {
            public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExecuteAsync(string sql,
                CancellationToken token = default)
            {
                IReadOnlyList<IReadOnlyDictionary<string, object?>> rows = [];

                return Task.FromResult(rows);
            }

            public Task<SqlValidation> ValidateAsync(string sql,
                CancellationToken token = default)
            {
                return Task.FromResult(new SqlValidation(true, null, null));
            }

            public Task<SqlPlanSummary> ExplainAsync(string sql,
                CancellationToken token = default)
            {
                return Task.FromResult(new SqlPlanSummary(0, 0, 0, [], [], []));
            }

            public Task<IReadOnlyList<SqlTableStatistic>> TableStatisticsAsync(
                string sql, CancellationToken token = default)
            {
                IReadOnlyList<SqlTableStatistic> statistics = [new("Case", 2500000, 10, 4000)];

                return Task.FromResult(statistics);
            }

            public Task<SqlQueryHistory> QueryHistoryAsync(
                string sql, CancellationToken token = default)
            {
                return Task.FromResult(new SqlQueryHistory(true, 400, 12, 600, 9000, 40));
            }

            public IReadOnlyList<string> ListQueryableTables()
            {
                return ["Case", "CaseWorkflow", "EntityAnalysisModel", "pg_stat_activity"];
            }

            public Task<IReadOnlyDictionary<string, string>> TableCommentsAsync(IReadOnlyList<string> tables,
                CancellationToken token = default)
            {
                IReadOnlyDictionary<string, string> comments = new Dictionary<string, string>
                {
                    ["Case"] = "A case raised against one archived invocation, identified by CaseKey and " +
                               "CaseKeyValue."
                };

                return Task.FromResult(comments);
            }
        }

        private sealed class FakeCache : IToolResultCache
        {
            public Task<string> StoreAsync(CachedTable table, string? label = null,
                TimeSpan? ttl = null, CancellationToken token = default)
            {
                return Task.FromResult("tbl_test");
            }

            public Task<CachedTable?> GetAsync(string pointerKey,
                CancellationToken token = default)
            {
                return Task.FromResult<CachedTable?>(null);
            }
        }
    }
}
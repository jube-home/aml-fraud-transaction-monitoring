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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.Tools.DataCache;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class CachedDataToolTests
    {
        private static CachedTable BuildTable()
        {
            return new CachedTable(
                [
                    new CachedColumn("id", "long"), new CachedColumn("status", "string"),
                    new CachedColumn("amount", "double")
                ],
                [
                    new Dictionary<string, object?> { ["id"] = 1L, ["status"] = "active", ["amount"] = 150.0 },
                    new Dictionary<string, object?> { ["id"] = 2L, ["status"] = "closed", ["amount"] = 40.0 },
                    new Dictionary<string, object?> { ["id"] = 3L, ["status"] = "active", ["amount"] = 99.0 },
                    new Dictionary<string, object?> { ["id"] = 4L, ["status"] = null, ["amount"] = 0.0 }
                ]);
        }

        [Fact]
        public async Task InspectDataSchemaReportsRowCountColumnCountAndNullCountsAsync()
        {
            var tool = new CachedDataTool(new FakeCache(BuildTable()));

            var result = await tool.InspectDataSchema("tbl_x");

            result.Should().Contain("4 row(s), 3 column(s)");
            result.Should().Contain("status (string): 1 null(s)");
            result.Should().Contain("id (long): 0 null(s)");
        }

        [Fact]
        public async Task InspectDataSchemaReportsNotFoundForAMissingPointerAsync()
        {
            var tool = new CachedDataTool(new FakeCache(null));

            var result = await tool.InspectDataSchema("tbl_missing");

            result.Should().Contain("No cached table matches that pointer");
        }

        [Fact]
        public async Task PreviewDataRowsClampsAboveTheCeilingAsync()
        {
            var tool = new CachedDataTool(new FakeCache(BuildTable()));

            var result = await tool.PreviewDataRows("tbl_x", 500);

            result.Should().Contain("Showing rows 1 to 4 of 4.");
        }

        [Fact]
        public async Task PreviewDataRowsClampsBelowOneAsync()
        {
            var tool = new CachedDataTool(new FakeCache(BuildTable()));

            var result = await tool.PreviewDataRows("tbl_x", 0);

            result.Should().Contain("Showing rows 1 to 1 of 4.");
        }

        [Fact]
        public async Task InspectDataSchemaReportsWhatReadingTheWholeTableWouldCostAsync()
        {
            var tool = new CachedDataTool(new FakeCache(BuildTable()));

            var result = await tool.InspectDataSchema("tbl_x");

            result.Should().MatchRegex(@"about \d+ characters",
                "a decision about whether to read more of a table is a decision about cost, so the "
                + "size has to arrive with the schema rather than being discovered by reading it");
            result.Should().MatchRegex(@"roughly \d+ tokens",
                "characters mean nothing to a model budgeting its own context; tokens are the unit "
                + "it actually spends");
        }

        [Fact]
        public async Task InspectDataSchemaScalesTheReportedCostWithTheSizeOfTheTableAsync()
        {
            var wide = new List<IReadOnlyDictionary<string, object?>>();

            for (var i = 0; i < 200; i++)
            {
                wide.Add(new Dictionary<string, object?>
                {
                    ["id"] = (long)i, ["status"] = "active", ["amount"] = 1.0
                });
            }

            var small = await new CachedDataTool(new FakeCache(BuildTable())).InspectDataSchema("tbl_x");

            var large = await new CachedDataTool(new FakeCache(new CachedTable(
            [
                new CachedColumn("id", "long"), new CachedColumn("status", "string"),
                new CachedColumn("amount", "double")
            ], wide))).InspectDataSchema("tbl_x");

            ExtractTokens(large).Should().BeGreaterThan(ExtractTokens(small) * 10,
                "a cost signal that does not move with the data is not a signal, and the whole "
                + "point is telling a fifty row table apart from an absurd one");
        }

        private static int ExtractTokens(string report)
        {
            var match = Regex.Match(report, @"roughly (\d+) tokens");
            match.Success.Should().BeTrue();

            return int.Parse(match.Groups[1].Value);
        }

        [Fact]
        public async Task PreviewDataRowsWalksPastTheStartOfTheTableWithAnOffsetAsync()
        {
            var tool = new CachedDataTool(new FakeCache(BuildTable()));

            var result = await tool.PreviewDataRows("tbl_x", 2, 2);

            result.Should().Contain("Showing rows 3 to 4 of 4.");
            result.Should().Contain("id=3").And.Contain("id=4");

            result.Should().NotContain("id=1",
                "without an offset a table that does not filter cleanly is unreachable past its "
                + "first window, which is the only reason a model would ask for all of it");
        }

        [Fact]
        public async Task PreviewDataRowsSaysSoRatherThanReturningNothingWhenTheOffsetIsPastTheEndAsync()
        {
            var tool = new CachedDataTool(new FakeCache(BuildTable()));

            var result = await tool.PreviewDataRows("tbl_x", offset: 99);

            result.Should().Contain("past the end of a 4 row table",
                "an empty window and an exhausted table look identical to a model, and it will "
                + "keep paging if it cannot tell them apart");
        }

        [Fact]
        public async Task PreviewDataRowsTreatsANegativeOffsetAsTheStartAsync()
        {
            var tool = new CachedDataTool(new FakeCache(BuildTable()));

            var result = await tool.PreviewDataRows("tbl_x", 1, -5);

            result.Should().Contain("Showing rows 1 to 1 of 4.");
        }

        [Fact]
        public async Task QueryCachedDataFiltersOnAnEqualityConditionAsync()
        {
            var tool = new CachedDataTool(new FakeCache(BuildTable()));

            var result = await tool.QueryCachedData("tbl_x", "status = \"active\"");

            result.Should().Contain("2 row(s) matched");
            result.Should().Contain("id=1");
            result.Should().Contain("id=3");
            result.Should().NotContain("id=2");
        }

        [Fact]
        public async Task QueryCachedDataFiltersOnANumericGreaterThanConditionAsync()
        {
            var tool = new CachedDataTool(new FakeCache(BuildTable()));

            var result = await tool.QueryCachedData("tbl_x", "amount > 100");

            result.Should().Contain("1 row(s) matched");
            result.Should().Contain("id=1");
        }

        [Fact]
        public async Task QueryCachedDataCombinesConditionsWithAndAsync()
        {
            var tool = new CachedDataTool(new FakeCache(BuildTable()));

            var result = await tool.QueryCachedData("tbl_x", "status = \"active\" && amount < 100");

            result.Should().Contain("1 row(s) matched");
            result.Should().Contain("id=3");
        }

        [Fact]
        public async Task QueryCachedDataSupportsContainsAsync()
        {
            var tool = new CachedDataTool(new FakeCache(BuildTable()));

            var result = await tool.QueryCachedData("tbl_x", "status contains \"clos\"");

            result.Should().Contain("1 row(s) matched");
            result.Should().Contain("id=2");
        }

        [Fact]
        public async Task QueryCachedDataReportsNoMatchesDistinctlyAsync()
        {
            var tool = new CachedDataTool(new FakeCache(BuildTable()));

            var result = await tool.QueryCachedData("tbl_x", "status = \"archived\"");

            result.Trim().Should().Be("No rows matched that filter.");
        }

        [Fact]
        public Task QueryCachedDataThrowsForAnUnknownColumnAsync()
        {
            var tool = new CachedDataTool(new FakeCache(BuildTable()));

            var act = () => tool.QueryCachedData("tbl_x", "nonexistent = \"x\"");

            return act.Should().ThrowAsync<ArgumentException>().WithMessage("*Unknown column*");
        }

        [Fact]
        public Task QueryCachedDataThrowsForAnUnparseableConditionAsync()
        {
            var tool = new CachedDataTool(new FakeCache(BuildTable()));

            var act = () => tool.QueryCachedData("tbl_x", "this is not a condition");

            return act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task QueryCachedDataReportsNotFoundForAMissingPointerAsync()
        {
            var tool = new CachedDataTool(new FakeCache(null));

            var result = await tool.QueryCachedData("tbl_missing", "id = 1");

            result.Should().Contain("No cached table matches that pointer");
        }

        private static CachedTable BuildReorderedTable()
        {
            return new CachedTable(
                [
                    new CachedColumn("Dependent", "double"), new CachedColumn("IsChip", "double"),
                    new CachedColumn("ATM", "double")
                ],
                [
                    new Dictionary<string, object?> { ["ATM"] = 0.0, ["IsChip"] = 1.0, ["Dependent"] = 1.0 }
                ]);
        }

        [Fact]
        public async Task PreviewDataRowsOrdersColumnsByTheTableNotTheRowAsync()
        {
            var tool = new CachedDataTool(new FakeCache(BuildReorderedTable()));

            var result = await tool.PreviewDataRows("tbl_x");

            result.Should().Contain("Dependent=1, IsChip=1, ATM=0");
        }

        [Fact]
        public async Task QueryCachedDataOrdersColumnsByTheTableNotTheRowAsync()
        {
            var tool = new CachedDataTool(new FakeCache(BuildReorderedTable()));

            var result = await tool.QueryCachedData("tbl_x", "Dependent = 1");

            result.Should().Contain("Dependent=1, IsChip=1, ATM=0");
        }

        private sealed class FakeCache(CachedTable? table) : IToolResultCache
        {
            public Task<string> StoreAsync(CachedTable stored, string? label = null, TimeSpan? ttl = null,
                CancellationToken token = default)
            {
                return Task.FromResult("tbl_fake");
            }

            public Task<CachedTable?> GetAsync(string pointerKey, CancellationToken token = default)
            {
                return Task.FromResult(table);
            }
        }
    }
}
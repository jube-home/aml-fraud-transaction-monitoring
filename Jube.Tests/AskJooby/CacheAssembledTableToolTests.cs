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
using Jube.AskJooby.Tools.Cache;
using Jube.AskJooby.Tools.DataCache;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class CacheAssembledTableToolTests
    {
        [Fact]
        public async Task CacheAssembledTableReturnsAPointerAndAPreviewAsync()
        {
            var cache = new InMemoryToolResultCache();
            var tool = new CacheAssembledTableTool(cache);

            var result = await tool.CacheAssembledTable(
                "[{\"RuleType\":\"Activation Rule\",\"Count\":9},{\"RuleType\":\"Gateway Rule\",\"Count\":1}]",
                "rule counts by type");

            result.Should().Contain("2 row(s)");
            result.Should().Contain("data_pointer");
            result.Should().Contain("RuleType");
            result.Should().Contain("Count");
        }

        [Fact]
        public async Task CacheAssembledTableRoundTripsThroughCachedDataToolAsync()
        {
            var cache = new InMemoryToolResultCache();
            var producer = new CacheAssembledTableTool(cache);
            var consumer = new CachedDataTool(cache);

            var produced = await producer.CacheAssembledTable(
                "[{\"RuleType\":\"Activation Rule\",\"Count\":9},{\"RuleType\":\"Gateway Rule\",\"Count\":1}]");

            var pointer = ExtractPointer(produced);

            var schema = await consumer.InspectDataSchema(pointer);
            schema.Should().Contain("2 row(s), 2 column(s)");
            schema.Should().Contain("RuleType");
            schema.Should().Contain("Count");

            var preview = await consumer.PreviewDataRows(pointer);
            preview.Should().Contain("Activation Rule");
            preview.Should().Contain("Gateway Rule");
        }

        [Fact]
        public Task CacheAssembledTableRejectsRowsWithDifferingKeysAsync()
        {
            var tool = new CacheAssembledTableTool(new InMemoryToolResultCache());

            var act = async () => await tool.CacheAssembledTable(
                "[{\"RuleType\":\"Activation Rule\",\"Count\":9},{\"RuleType\":\"Gateway Rule\"}]");

            return act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*same set of keys*");
        }

        [Fact]
        public Task CacheAssembledTableRejectsAnEmptyArrayAsync()
        {
            var tool = new CacheAssembledTableTool(new InMemoryToolResultCache());

            var act = async () => await tool.CacheAssembledTable("[]");

            return act.Should().ThrowAsync<ArgumentException>().WithMessage("*at least one row*");
        }

        private static string ExtractPointer(string report)
        {
            const string marker = "data_pointer: \"";
            var start = report.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
            var end = report.IndexOf('"', start);

            return report[start..end];
        }

        private sealed class InMemoryToolResultCache : IToolResultCache
        {
            private readonly Dictionary<string, CachedTable> tables = new();
            private int nextPointer;

            public Task<string> StoreAsync(CachedTable table, string? label = null, TimeSpan? ttl = null,
                CancellationToken token = default)
            {
                var pointer = $"tbl_{++nextPointer}";
                tables[pointer] = table;

                return Task.FromResult(pointer);
            }

            public Task<CachedTable?> GetAsync(string pointerKey, CancellationToken token = default)
            {
                return Task.FromResult(tables.GetValueOrDefault(pointerKey));
            }
        }
    }
}
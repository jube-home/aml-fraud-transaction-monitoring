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
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.Service.Agent.PageAgentToolBinder;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class LargeResultPointerTests
    {
        private static JsonElement Payload(int rows)
        {
            var items = Enumerable.Range(1, rows).Select(index => new
            {
                id = index,
                level = "ERROR",
                message = new string('m', 400),
                instance = "node-" + index
            });

            return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
            {
                rows = items,
                total = rows,
                statistics = new { columns = new { id = new { min = 1, max = rows } } }
            }));
        }

        [Fact]
        public async Task APayloadWrappingItsRowsIsCachedAsThoseRowsAndNotAsOneOpaqueCellAsync()
        {
            var cache = new RecordingCache();

            await LargeResultPointer.ReduceAsync(Payload(9), "ApplicationLogEntryList", cache, 500, null,
                CancellationToken.None);

            cache.Stored.Should().NotBeNull();

            cache.Stored!.Columns.Select(column => column.Name).Should()
                .BeEquivalentTo(["id", "level", "message", "instance"],
                    "the rows carried inside the payload are the table, and reflecting over the "
                    + "wrapper instead yields a single ValueKind cell that no filter can reach");

            cache.Stored.Rows.Should().HaveCount(9);
            cache.Stored.Rows[0]["level"].Should().Be("ERROR");
        }

        [Fact]
        public async Task TheModelIsToldTheRealRowCountRatherThanOneAsync()
        {
            var cache = new RecordingCache();

            var reduced = await LargeResultPointer.ReduceAsync(Payload(9), "ApplicationLogEntryList", cache, 500,
                null, CancellationToken.None);

            reduced?.ToString().Should().Contain("9 row(s)",
                "an agent told a nine row table is one row of Object has no way to know the data is "
                + "right there, and will keep re-querying for it");

            reduced?.ToString().Should().NotContain("ValueKind");
        }

        [Fact]
        public async Task ASmallResultIsStillReturnedWholeRatherThanPointerisedAsync()
        {
            var cache = new RecordingCache();

            var reduced = await LargeResultPointer.ReduceAsync(Payload(1), "Small", cache, 1_000_000, null,
                CancellationToken.None);

            cache.Stored.Should().BeNull("nothing under the inline cap should be cached at all");
            reduced.Should().BeOfType<JsonElement>();
        }

        [Fact]
        public async Task APayloadWithNoRowsArrayFallsBackToASingleReadableCellAsync()
        {
            var cache = new RecordingCache();

            var shaped = JsonSerializer.Deserialize<JsonElement>(
                JsonSerializer.Serialize(new { summary = new string('x', 2000) }));

            await LargeResultPointer.ReduceAsync(shaped, "Odd", cache, 500, null, CancellationToken.None);

            cache.Stored!.Rows.Should().ContainSingle();

            cache.Stored.Rows[0]["Value"]?.ToString().Should().Contain("summary",
                "an unrecognised shape must still be readable as its own JSON rather than becoming "
                + "an empty placeholder");
        }

        private sealed class RecordingCache : IToolResultCache
        {
            public CachedTable? Stored { get; private set; }

            public Task<string> StoreAsync(CachedTable table, string? label = null, TimeSpan? ttl = null,
                CancellationToken token = default)
            {
                Stored = table;

                return Task.FromResult("tbl_test");
            }

            public Task<CachedTable?> GetAsync(string pointerKey, CancellationToken token = default)
            {
                return Task.FromResult(Stored);
            }
        }
    }
}
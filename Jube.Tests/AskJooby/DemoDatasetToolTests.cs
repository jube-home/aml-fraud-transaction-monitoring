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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.Tools.DemoDataset;
using Jube.Service.DemoDataset;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class DemoDatasetToolTests
    {
        [Fact]
        public void TheEmbeddedDemoDatasetLoadsFromTheAssemblyRatherThanAFilePath()
        {
            var table = new AskJoobyDemoDatasetProvider().Load("transactions");

            table.Rows.Count.Should().BeGreaterThan(1000,
                "the sample is only useful for training and analysis if it is big enough to be worth "
                + "training on");

            table.Columns.Select(column => column.Name).Should().Contain("Dependent",
                "the label column is what makes it a supervised sample rather than a table of numbers");
        }

        [Fact]
        public void EveryRowCarriesEveryColumnSoTheSampleIsRectangular()
        {
            var table = new AskJoobyDemoDatasetProvider().Load("transactions");
            var names = table.Columns.Select(column => column.Name).ToList();

            table.Rows.All(row => names.All(row.ContainsKey)).Should().BeTrue(
                "a ragged table silently becomes nulls downstream, which reads as missing data rather "
                + "than as a parsing fault");
        }

        [Fact]
        public void TheLabelColumnHoldsBothOutcomesAndNothingElse()
        {
            var table = new AskJoobyDemoDatasetProvider().Load("transactions");
            var labels = table.Rows.Select(row => row["Dependent"]).Distinct().ToList();

            labels.Should().BeEquivalentTo([0d, 1d],
                "a demo of fraud detection needs both fraud and genuine in it, and nothing a model "
                + "would have to treat as a third class");
        }

        [Fact]
        public void ValuesParseAsNumbersRatherThanStayingAsText()
        {
            var table = new AskJoobyDemoDatasetProvider().Load("transactions");

            table.Rows.First()["TransactionAmt"].Should().BeOfType<double>(
                "a column left as text cannot be charted or trained on, and the failure would surface "
                + "far from here");
        }

        [Fact]
        public async Task GetDemoDatasetHandsBackAPointerAndNotTheRowsAsync()
        {
            var cache = new RecordingCache();
            var tool = new DemoDatasetTool(new AskJoobyDemoDatasetProvider(), cache);

            var result = await tool.GetDemoDataset();

            result.Should().Contain("data_pointer");
            result.Should().Contain("Dependent");
            cache.Stored.Should().NotBeNull();

            cache.Stored!.Rows.Count.Should().BeGreaterThan(1000,
                "the whole sample goes into the cache where a consumer tool can reach it, while the "
                + "model gets only its shape");

            result.Length.Should().BeLessThan(2000,
                "a dataset tool that returns a pointer and then prints the data anyway has defeated "
                + "the point of the pointer");
        }

        [Fact]
        public async Task GetDemoDatasetNamesTheDatasetsThatExistWhenAskedForOneThatDoesNotAsync()
        {
            var cache = new RecordingCache();
            var tool = new DemoDatasetTool(new AskJoobyDemoDatasetProvider(), cache);

            var result = await tool.GetDemoDataset("sanctions");

            result.Should().Contain("not a demo dataset").And.Contain("transactions");
            cache.Stored.Should().BeNull();
        }

        private sealed class RecordingCache : IToolResultCache
        {
            public CachedTable? Stored { get; private set; }

            public Task<string> StoreAsync(CachedTable table, string? label = null, TimeSpan? ttl = null,
                CancellationToken token = default)
            {
                Stored = table;

                return Task.FromResult("tbl_demo");
            }

            public Task<CachedTable?> GetAsync(string pointerKey, CancellationToken token = default)
            {
                return Task.FromResult(Stored);
            }
        }
    }
}
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
using FluentAssertions;
using Jube.Data.Query.GetCaseWorkflowXPathByCaseWorkflowIdQuery;
using Jube.Service.Query.CaseKeyJournal;
using Xunit;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Unit")]
    public sealed class CaseKeyJournalPeerAccumulatorTests
    {
        private static GetCaseWorkflowXPathByCaseWorkflowIdQueryDto XPath(string name, bool drill = false,
            int role = 0, bool segment = false)
        {
            return new GetCaseWorkflowXPathByCaseWorkflowIdQueryDto
            {
                Name = name, XPath = $"$.payload.{name}", Drill = drill, RoleId = role, Segment = segment
            };
        }

        private static string Json(double? amount, string? tier = null, double? volume = null, string? device = null)
        {
            var payload = new List<string>();

            if (amount.HasValue) payload.Add($"\"Amount\":{amount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            if (tier is not null) payload.Add($"\"Tier\":\"{tier}\"");
            if (device is not null) payload.Add($"\"Device\":\"{device}\"");

            var abstraction = volume.HasValue
                ? $",\"abstraction\":{{\"Volume\":{volume.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}"
                : string.Empty;

            return $"{{\"payload\":{{{string.Join(",", payload)}}}{abstraction}}}";
        }

        private static CaseKeyJournalPeerAccumulator Fill(int keys, Func<int, string> json,
            params GetCaseWorkflowXPathByCaseWorkflowIdQueryDto[] xPaths)
        {
            var accumulator = new CaseKeyJournalPeerAccumulator(xPaths);

            for (var key = 0; key < keys; key++)
            {
                accumulator.Add($"K{key}", json(key));
                accumulator.Add($"K{key}", json(key));
            }

            return accumulator;
        }

        [Fact]
        public void BuildsOneDistributionPerMeasureFromTheMedianOfEachKey()
        {
            var accumulator = Fill(25, key => Json(100 + key, volume: key), XPath("Amount"), XPath("Tier"));

            var built = accumulator.Build();

            accumulator.Keys.Should().Be(25);
            accumulator.Rows.Should().Be(50);
            built.Select(peer => peer.Field).Should().Equal("Amount", "abstraction:Volume");
            built.Should().OnlyContain(peer => peer.Segment == null && peer.KeyCount == 25 && peer.RowCount == 50);
            built.Single(peer => peer.Field == "Amount").Median.Should().Be(112);
            built.Single(peer => peer.Field == "abstraction:Volume").Median.Should().Be(12);
        }

        [Fact]
        public void NothingIsBuiltForFewerThanTwentyKeys()
        {
            Fill(19, key => Json(100 + key), XPath("Amount")).Build().Should().BeEmpty();
        }

        [Fact]
        public void KeysAreCountedOnceWhateverTheirRows()
        {
            var accumulator = new CaseKeyJournalPeerAccumulator([XPath("Amount")]);

            for (var i = 0; i < 100; i++)
            {
                accumulator.Add("same", Json(i));
            }

            accumulator.Keys.Should().Be(1);
            accumulator.Rows.Should().Be(100);
        }

        [Fact]
        public void EachSegmentGetsItsOwnDistributionTakenFromTheKeysNewestRow()
        {
            var accumulator = new CaseKeyJournalPeerAccumulator([XPath("Amount"), XPath("Tier", segment: true)]);

            for (var key = 0; key < 30; key++)
            {
                var tier = key < 22 ? "Gold" : "Silver";
                accumulator.Add($"K{key}", Json(500 + key, tier));
                accumulator.Add($"K{key}", Json(500 + key, "Changed"));
            }

            var built = accumulator.Build().Where(peer => peer.Field == "Amount").ToList();

            built.Should().Contain(peer => peer.Segment == null && peer.KeyCount == 30);
            built.Should().Contain(peer => peer.Segment == "Gold" && peer.KeyCount == 22);
            built.Should().NotContain(peer => peer.Segment == "Silver" || peer.Segment == "Changed");
        }

        [Fact]
        public void IdentifiersAndCategoriesAreNotMeasures()
        {
            var accumulator = new CaseKeyJournalPeerAccumulator(
            [
                new GetCaseWorkflowXPathByCaseWorkflowIdQueryDto
                {
                    Name = "Amount", XPath = "$.payload.Amount", Drill = true
                },
                new GetCaseWorkflowXPathByCaseWorkflowIdQueryDto
                {
                    Name = "Score", XPath = "$.payload.Score", RoleId = 2
                },
                XPath("Volume")
            ]);

            for (var key = 0; key < 25; key++)
            {
                accumulator.Add($"K{key}",
                    $"{{\"payload\":{{\"Amount\":{key},\"Score\":{key},\"Volume\":{key}}}}}");
            }

            accumulator.Build().Select(peer => peer.Field).Should().Equal("Volume");
        }

        [Theory]
        [InlineData(null, "{}")]
        [InlineData("", "{}")]
        [InlineData("K", null)]
        [InlineData("K", "")]
        [InlineData("K", "{not json")]
        public void RowsWithoutAKeyOrReadableJsonAreIgnored(string? key, string? json)
        {
            var accumulator = new CaseKeyJournalPeerAccumulator([XPath("Amount")]);

            accumulator.Add(key, json);

            accumulator.Rows.Should().Be(0);
            accumulator.Keys.Should().Be(0);
        }
    }
}

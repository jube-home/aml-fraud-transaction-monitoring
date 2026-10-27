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


using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.CaseRisk.Journal;
using Xunit;

namespace Jube.Test.CaseRisk.Journal
{
    [Trait("Category", "Unit")]
    public sealed class PeerDistributionCalculatorTests
    {
        private static List<IReadOnlyList<double>> Keys(int count, double start = 1)
        {
            return Enumerable.Range(0, count)
                .Select(index => (IReadOnlyList<double>)new[] { start + index - 1, start + index, start + index + 1 })
                .ToList();
        }

        [Fact]
        public void BuildsTheDistributionOfKeyMediansAndCountsKeysAndRows()
        {
            var peer = PeerDistributionCalculator.Build("Amount", "Gold", Keys(21))!;

            peer.Field.Should().Be("Amount");
            peer.Segment.Should().Be("Gold");
            peer.KeyCount.Should().Be(21);
            peer.RowCount.Should().Be(63);
            peer.Median.Should().Be(11);
            peer.Minimum.Should().Be(1);
            peer.Maximum.Should().Be(21);
            peer.Mean.Should().Be(11);
            peer.Q1.Should().Be(6);
            peer.Q3.Should().Be(16);
            peer.Scale.Should().BeGreaterThan(0);
            peer.Bins.Sum(bin => bin.Count).Should().Be(21);
        }

        [Fact]
        public void NeedsAtLeastTwentyKeysThatHaveValues()
        {
            PeerDistributionCalculator.Build("Amount", null, Keys(19)).Should().BeNull();
            PeerDistributionCalculator.Build("Amount", null, Keys(20)).Should().NotBeNull();

            var withEmpty = Keys(19);
            withEmpty.Add([]);
            withEmpty.Add([double.NaN, double.PositiveInfinity]);

            PeerDistributionCalculator.Build("Amount", null, withEmpty).Should().BeNull();
            PeerDistributionCalculator.Build("Amount", null, []).Should().BeNull();
        }

        [Fact]
        public void IgnoresValuesThatAreNotNumbers()
        {
            var keys = Keys(20);
            keys[0] = [double.NaN, 5, double.PositiveInfinity];

            var peer = PeerDistributionCalculator.Build("Amount", null, keys)!;

            peer.RowCount.Should().Be(1 + 19 * 3);
            peer.KeyCount.Should().Be(20);
        }

        [Fact]
        public void KeepsASingleBinWhenEveryKeyIsTheSameAndAScaleOfZero()
        {
            var same = Enumerable.Range(0, 20).Select(_ => (IReadOnlyList<double>)new[] { 7d }).ToList();

            var peer = PeerDistributionCalculator.Build("Amount", null, same)!;

            peer.Median.Should().Be(7);
            peer.Scale.Should().Be(0);
            peer.Bins.Should().ContainSingle().Which.Count.Should().Be(20);
        }
    }
}

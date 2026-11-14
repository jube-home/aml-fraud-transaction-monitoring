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
    public sealed class PeerComparisonCalculatorTests
    {
        private static readonly PeerDistribution Peer = PeerDistributionCalculator.Build("Amount", null,
            Enumerable.Range(1, 40).Select(value => (IReadOnlyList<double>)new[] { (double)value }).ToList())!;

        private static double[] Values(double value, int count = 25)
        {
            return Enumerable.Repeat(value, count).ToArray();
        }

        [Fact]
        public void AKeyAtThePeerMedianIsTypicalAtTheMiddlePercentile()
        {
            var comparison = PeerComparisonCalculator.Compare(Peer, Values(Peer.Median))!;

            comparison.Direction.Should().Be(PeerComparisonCalculator.Typical);
            comparison.Z.Should().BeApproximately(0, 1e-9);
            comparison.Percentile.Should().BeApproximately(0.5, 0.05);
            comparison.PeerKeys.Should().Be(40);
            comparison.KeyRows.Should().Be(25);
            comparison.LowConfidence.Should().BeFalse();
        }

        [Fact]
        public void AKeyFarAboveOrBelowThePeersIsMarkedAndClampedAtFive()
        {
            var above = PeerComparisonCalculator.Compare(Peer, Values(1000))!;
            var below = PeerComparisonCalculator.Compare(Peer, Values(-1000))!;

            above.Direction.Should().Be(PeerComparisonCalculator.Above);
            above.Z.Should().Be(5);
            above.Percentile.Should().Be(1);
            below.Direction.Should().Be(PeerComparisonCalculator.Below);
            below.Z.Should().Be(-5);
            below.Percentile.Should().Be(0);
        }

        [Fact]
        public void TheThresholdDecidesWhenAKeyStandsOut()
        {
            var value = Peer.Median + 2.5 * Peer.Scale;

            PeerComparisonCalculator.Compare(Peer, Values(value))!.Direction.Should()
                .Be(PeerComparisonCalculator.Typical);
            PeerComparisonCalculator.Compare(Peer, Values(value), 2)!.Direction.Should()
                .Be(PeerComparisonCalculator.Above);
        }

        [Fact]
        public void TheKeyIsComparedByItsMedianAndFewRowsAreLowConfidence()
        {
            var comparison = PeerComparisonCalculator.Compare(Peer, [1, 2, 3, 100, 200])!;

            comparison.KeyMedian.Should().Be(3);
            comparison.LowConfidence.Should().BeTrue();
        }

        [Fact]
        public void NothingToCompareGivesNoComparison()
        {
            PeerComparisonCalculator.Compare(Peer, []).Should().BeNull();
            PeerComparisonCalculator.Compare(Peer, [double.NaN]).Should().BeNull();
        }

        [Fact]
        public void TooFewPeerKeysAreNotJudged()
        {
            var thin = Peer with { KeyCount = 3 };

            var comparison = PeerComparisonCalculator.Compare(thin, Values(1000))!;

            comparison.Z.Should().BeNull();
            comparison.Direction.Should().Be(PeerComparisonCalculator.Typical);
        }

        [Fact]
        public void ThePercentileInterpolatesWithinABinAndStopsAtTheEnds()
        {
            var bins = new[] { new DistributionBin(0, 10, 10), new DistributionBin(10, 20, 30) };
            var peer = Peer with { Minimum = 0, Maximum = 20, Bins = bins };

            PeerComparisonCalculator.Percentile(peer, -1).Should().Be(0);
            PeerComparisonCalculator.Percentile(peer, 0).Should().Be(0);
            PeerComparisonCalculator.Percentile(peer, 5).Should().BeApproximately(5d / 40, 1e-9);
            PeerComparisonCalculator.Percentile(peer, 10).Should().BeApproximately(10d / 40, 1e-9);
            PeerComparisonCalculator.Percentile(peer, 15).Should().BeApproximately(25d / 40, 1e-9);
            PeerComparisonCalculator.Percentile(peer, 20).Should().Be(1);
            PeerComparisonCalculator.Percentile(peer with { Bins = [] }, 5).Should().Be(0);
        }

        [Fact]
        public void ABinOfNoWidthCountsOnceTheValueIsPastIt()
        {
            var peer = Peer with { Minimum = 5, Maximum = 9, Bins = [new DistributionBin(5, 5, 4)] };

            PeerComparisonCalculator.Percentile(peer, 6).Should().Be(1);
            PeerComparisonCalculator.Percentile(peer, 5).Should().Be(0);
        }
    }
}

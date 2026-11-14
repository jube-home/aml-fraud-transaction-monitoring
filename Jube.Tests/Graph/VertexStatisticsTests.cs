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
using System.Linq;
using FluentAssertions;
using Jube.Graph.Algorithms;
using Xunit;

namespace Jube.Test.Graph
{
    [Trait("Category", "Unit")]
    public sealed class VertexStatisticsTests
    {
        private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        [Fact]
        public void TinyLedgerGivesTheExpectedFigures()
        {
            var statistics = VertexStatistics.Compute(
            [
                (7L, T0, 100d, false),
                (7L, T0.AddHours(1), 100d, true),
                (7L, T0.AddHours(2), 250d, false),
                (7L, T0.AddHours(5), 200d, true),
                (3L, T0, 50d, false)
            ]);

            statistics.Select(s => s.NodeId).Should().Equal(3L, 7L);
            var node = statistics[1];
            node.InboundAmounts.Count.Should().Be(2);
            node.InboundAmounts.Sum.Should().Be(350d);
            node.InboundAmounts.Mean.Should().Be(175d);
            node.InboundAmounts.Min.Should().Be(100d);
            node.InboundAmounts.Max.Should().Be(250d);
            node.InboundAmounts.Median.Should().Be(175d);
            node.InboundAmounts.Variance.Should().BeApproximately(11250d, 1e-9);
            node.OutboundAmounts.Sum.Should().Be(300d);
            node.InterArrivalSeconds.Count.Should().Be(3);
            node.InterArrivalSeconds.Median.Should().Be(3600d);
            node.InterArrivalSeconds.Max.Should().Be(10800d);
            node.RoundAmountShare.Should().Be(0.75d);
            node.PassThroughRatio.Should().BeApproximately(300d / 350d, 1e-12);
            node.MedianHoldSeconds.Should().Be(7200d);
            var other = statistics[0];
            other.PassThroughRatio.Should().Be(0d);
            other.MedianHoldSeconds.Should().BeNull();
            other.OutboundAmounts.Should().Be(Moments.Empty);
        }

        [Fact]
        public void MomentsOfASymmetricSampleHaveZeroSkewness()
        {
            var moments = Moments.Of([1d, 2d, 3d, 4d, 5d]);

            moments.Mean.Should().Be(3d);
            moments.Variance.Should().Be(2.5d);
            moments.Skewness.Should().BeApproximately(0d, 1e-12);
            moments.Kurtosis.Should().BeApproximately(-1.3d, 1e-12);
            Moments.Of([4d, 4d, 4d]).Skewness.Should().Be(0d);
        }

        [Fact]
        public void RoundAmountsAreMultiplesOfOneHundred()
        {
            VertexStatistics.IsRoundAmount(100d).Should().BeTrue();
            VertexStatistics.IsRoundAmount(1000d).Should().BeTrue();
            VertexStatistics.IsRoundAmount(250d).Should().BeFalse();
            VertexStatistics.IsRoundAmount(0d).Should().BeFalse();
        }
    }
}

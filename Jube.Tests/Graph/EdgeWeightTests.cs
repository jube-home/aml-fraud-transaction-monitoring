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
using FluentAssertions;
using Jube.Graph.Weights;
using Xunit;

namespace Jube.Test.Graph
{
    [Trait("Category", "Unit")]
    public sealed class EdgeWeightTests
    {
        private static readonly DateTimeOffset Now = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public void OneHalfLifeAgoContributesExactlyHalf()
        {
            EdgeWeight.Decay(1d, Now.AddDays(-30), Now, 30d).Should().Be(0.5d);
            EdgeWeight.Decay(4d, Now.AddDays(-60), Now, 30d).Should().Be(1d);
            EdgeWeight.Decay(1d, Now, Now, 30d).Should().Be(1d);
        }

        [Fact]
        public void SpecificityFallsWithCounterparts()
        {
            EdgeWeight.Specificity(0).Should().BeApproximately(1d / Math.Log(2d), 1e-12);
            EdgeWeight.Specificity(1).Should().BeApproximately(1d / Math.Log(3d), 1e-12);
            EdgeWeight.Specificity(10).Should().BeLessThan(EdgeWeight.Specificity(1));
        }

        [Fact]
        public void WeightFollowsTheFormula()
        {
            var expected = 0.5 * 0.9 * 0.7 * (1 + Math.Log(1 + 3d)) * (1 + 2d);

            EdgeWeight.Weight(0.5, 0.9, 0.7, 3d, 2d).Should().BeApproximately(expected, 1e-12);
            EdgeWeight.Weight(1d, 1d, 1d, 0d, 0d).Should().Be(1d);
        }

        [Fact]
        public void FoldDecaysEveryEventToTheAnchor()
        {
            var (count, amount) = EdgeWeight.Fold(
                [(Now.AddDays(-30), 100d), (Now, 300d)], 30d, 100d, Now);

            count.Should().BeApproximately(1.5d, 1e-12);
            amount.Should().BeApproximately(0.5 * Math.Log(2d) + Math.Log(4d), 1e-12);
        }

        [Fact]
        public void FoldAnchorsOnTheLatestEventWhenItIsAfterAsOf()
        {
            var (count, _) = EdgeWeight.Fold([(Now.AddDays(30), 1d), (Now, 1d)], 30d, 1d, Now);

            count.Should().BeApproximately(1.5d, 1e-12);
            EdgeWeight.Fold([], 30d, 1d, Now).Should().Be((0d, 0d));
        }
    }
}

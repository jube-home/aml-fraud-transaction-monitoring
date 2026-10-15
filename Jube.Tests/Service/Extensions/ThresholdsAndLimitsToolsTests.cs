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
using Jube.Service.Extensions.ThresholdsAndLimits;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class ThresholdsAndLimitsToolsTests
    {
        [Fact]
        [Trait("Covers", "DistanceToThreshold")]
        [Trait("Covers", "PercentBelowThreshold")]
        [Trait("Covers", "IsJustBelowThreshold")]
        public void NearThresholdMeasures()
        {
            ThresholdsAndLimitsTools.DistanceToThreshold(9600, 10000).Should().Be(400);
            ThresholdsAndLimitsTools.DistanceToThreshold(10400, 10000).Should().Be(400);
            ThresholdsAndLimitsTools.PercentBelowThreshold(9500, 10000).Should().Be(5);
            ThresholdsAndLimitsTools.IsJustBelowThreshold(9600, 10000, 5).Should().BeTrue();
            ThresholdsAndLimitsTools.IsJustBelowThreshold(10000, 10000, 5).Should().BeFalse();
        }

        [Fact]
        [Trait("Covers", "HeadroomToLimit")]
        [Trait("Covers", "ExcessOverLimit")]
        [Trait("Covers", "ShortfallBelowMinimum")]
        public void LimitMeasures()
        {
            ThresholdsAndLimitsTools.HeadroomToLimit(700, 1000).Should().Be(300);
            ThresholdsAndLimitsTools.HeadroomToLimit(1200, 1000).Should().Be(-200);
            ThresholdsAndLimitsTools.ExcessOverLimit(1200, 1000).Should().Be(200);
            ThresholdsAndLimitsTools.ExcessOverLimit(900, 1000).Should().Be(0);
            ThresholdsAndLimitsTools.ShortfallBelowMinimum(80, 100).Should().Be(20);
        }

        [Fact]
        [Trait("Covers", "IsRoundAmount")]
        [Trait("Covers", "LeadingSignificantDigit")]
        [Trait("Covers", "PositionInRange")]
        public void AmountShapeMeasures()
        {
            ThresholdsAndLimitsTools.IsRoundAmount(5000, 1000).Should().BeTrue();
            ThresholdsAndLimitsTools.IsRoundAmount(5001, 1000).Should().BeFalse();
            ThresholdsAndLimitsTools.LeadingSignificantDigit(0.0456).Should().Be(4);
            ThresholdsAndLimitsTools.PositionInRange(75, 50, 150).Should().Be(25);
            var act = () => ThresholdsAndLimitsTools.PositionInRange(1, 5, 5);
            act.Should().Throw<ArgumentException>();
            var infinite = () => ThresholdsAndLimitsTools.LeadingSignificantDigit(double.PositiveInfinity);
            infinite.Should().Throw<ArgumentException>();
        }
    }
}
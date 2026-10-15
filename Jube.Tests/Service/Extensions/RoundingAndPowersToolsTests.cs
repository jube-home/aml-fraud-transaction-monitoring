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
using Jube.Service.Extensions.RoundingAndPowers;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class RoundingAndPowersToolsTests
    {
        [Fact]
        [Trait("Covers", "RoundNumber")]
        public void RoundNumberHonoursTheMidpointRule()
        {
            RoundingAndPowersTools.RoundNumber(2.5, 0).Should().Be(3);
            RoundingAndPowersTools.RoundNumber(2.5, 0, "ToEven").Should().Be(2);
            RoundingAndPowersTools.RoundNumber(1.2345, 2, "ToZero").Should().Be(1.23);
            var act = () => RoundingAndPowersTools.RoundNumber(1, 16);
            act.Should().Throw<ArgumentException>();
            var badMode = () => RoundingAndPowersTools.RoundNumber(1, 1, "Sideways");
            badMode.Should().Throw<ArgumentException>().WithMessage("*AwayFromZero*");
        }

        [Fact]
        [Trait("Covers", "RoundToNearestIncrement")]
        public void RoundToNearestIncrementRefusesABadIncrement()
        {
            RoundingAndPowersTools.RoundToNearestIncrement(1234, 50).Should().Be(1250);
            var act = () => RoundingAndPowersTools.RoundToNearestIncrement(1234, 0);
            act.Should().Throw<ArgumentException>().WithMessage("*undefined*");
        }

        [Fact]
        [Trait("Covers", "RoundDownToWholeNumber")]
        [Trait("Covers", "RoundUpToWholeNumber")]
        [Trait("Covers", "TruncateToWholeNumber")]
        public void WholeNumberRoundingFollowsItsDirection()
        {
            RoundingAndPowersTools.RoundDownToWholeNumber(-2.5).Should().Be(-3);
            RoundingAndPowersTools.RoundUpToWholeNumber(2.1).Should().Be(3);
            RoundingAndPowersTools.TruncateToWholeNumber(-2.7).Should().Be(-2);
            var act = () => RoundingAndPowersTools.RoundDownToWholeNumber(double.NaN);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ClampNumber")]
        public void ClampNumberLimitsToTheRange()
        {
            RoundingAndPowersTools.ClampNumber(150, 0, 100).Should().Be(100);
            RoundingAndPowersTools.ClampNumber(-5, 0, 100).Should().Be(0);
            var act = () => RoundingAndPowersTools.ClampNumber(1, 10, 0);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "RaiseToPower")]
        [Trait("Covers", "NthRoot")]
        [Trait("Covers", "Logarithm")]
        public void PowersRootsAndLogarithmsAreExact()
        {
            RoundingAndPowersTools.RaiseToPower(2, 10).Should().Be(1024);
            RoundingAndPowersTools.NthRoot(-8, 3).Should().BeApproximately(-2, 1e-12);
            RoundingAndPowersTools.Logarithm(1000).Should().BeApproximately(3, 1e-12);
            RoundingAndPowersTools.Logarithm(8, 2).Should().BeApproximately(3, 1e-12);
            var negativeRoot = () => RoundingAndPowersTools.NthRoot(-16, 2);
            negativeRoot.Should().Throw<ArgumentException>();
            var badLog = () => RoundingAndPowersTools.Logarithm(-1);
            badLog.Should().Throw<ArgumentException>();
            var badPower = () => RoundingAndPowersTools.RaiseToPower(-8, 0.5);
            badPower.Should().Throw<ArgumentException>();
        }
    }
}
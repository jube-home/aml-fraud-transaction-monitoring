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
using Jube.Service.Extensions.PercentagesAndChanges;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class PercentagesAndChangesToolsTests
    {
        [Fact]
        [Trait("Covers", "PercentOfTotal")]
        [Trait("Covers", "PercentageChange")]
        [Trait("Covers", "PercentDifference")]
        public void PercentagesAreComputedExactly()
        {
            PercentagesAndChangesTools.PercentOfTotal(25, 200).Should().Be(12.5);
            PercentagesAndChangesTools.PercentageChange(100, 80).Should().Be(25);
            PercentagesAndChangesTools.PercentageChange(80, 100).Should().Be(-20);
            PercentagesAndChangesTools.PercentDifference(90, 110).Should().Be(20);
            var act = () => PercentagesAndChangesTools.PercentageChange(5, 0);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "RatioOfValues")]
        [Trait("Covers", "LogRatio")]
        [Trait("Covers", "CompoundGrowthRate")]
        public void RatiosAndGrowthRates()
        {
            PercentagesAndChangesTools.RatioOfValues(30, 120).Should().Be(0.25);
            PercentagesAndChangesTools.LogRatio(2, 1).Should().BeApproximately(Math.Log(2), 1e-12);
            PercentagesAndChangesTools.CompoundGrowthRate(121, 100, 2).Should().BeApproximately(0.1, 1e-12);
            var act = () => PercentagesAndChangesTools.RatioOfValues(1, 0);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "AddPercentage")]
        [Trait("Covers", "SubtractPercentage")]
        [Trait("Covers", "AmountBeforePercentageAdded")]
        [Trait("Covers", "IsWithinPercentOf")]
        public void PercentageAdjustments()
        {
            PercentagesAndChangesTools.AddPercentage(100, 20).Should().BeApproximately(120, 1e-9);
            PercentagesAndChangesTools.SubtractPercentage(100, 20).Should().BeApproximately(80, 1e-9);
            PercentagesAndChangesTools.AmountBeforePercentageAdded(120, 20).Should().BeApproximately(100, 1e-9);
            PercentagesAndChangesTools.IsWithinPercentOf(104, 100, 5).Should().BeTrue();
            PercentagesAndChangesTools.IsWithinPercentOf(106, 100, 5).Should().BeFalse();
        }
    }
}
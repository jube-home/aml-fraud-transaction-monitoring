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
using Jube.Service.Extensions.Geospatial;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class GeospatialToolsTests
    {
        [Fact]
        [Trait("Covers", "DistanceBetweenPoints")]
        [Trait("Covers", "IsWithinRadius")]
        [Trait("Covers", "ImpliedTravelSpeed")]
        public void DistancesAndSpeed()
        {
            var london = (51.5074, -0.1278);
            var paris = (48.8566, 2.3522);
            var distance = GeospatialTools.DistanceBetweenPoints(london.Item1, london.Item2, paris.Item1, paris.Item2);
            distance.Kilometres.Should().BeApproximately(343.5, 1);
            distance.Miles.Should().BeApproximately(213.5, 1);
            GeospatialTools.IsWithinRadius(london.Item1, london.Item2, paris.Item1, paris.Item2, 400).Should().BeTrue();

            GeospatialTools.ImpliedTravelSpeed(london.Item1, london.Item2, paris.Item1, paris.Item2, 0.25).Should()
                .BeGreaterThan(1300);

            var noTime = () => GeospatialTools.ImpliedTravelSpeed(0, 0, 1, 1, 0);
            noTime.Should().Throw<ArgumentException>();
            var swapped = () => GeospatialTools.DistanceBetweenPoints(-0.1278, 151.5, 0, 0);
            swapped.Should().NotThrow();
            var outOfRange = () => GeospatialTools.DistanceBetweenPoints(151.5, 0, 0, 0);
            outOfRange.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "InitialBearing")]
        [Trait("Covers", "EncodeGeohash")]
        [Trait("Covers", "DecodeGeohash")]
        [Trait("Covers", "IsValidCoordinate")]
        public void BearingsAndGeohashes()
        {
            GeospatialTools.InitialBearing(0, 0, 0, 10).Should().BeApproximately(90, 1e-9);
            GeospatialTools.EncodeGeohash(57.64911, 10.40744, 11).Should().Be("u4pruydqqvj");
            GeospatialTools.DecodeGeohash("u4pruydqqvj").Latitude.Should().BeApproximately(57.64911, 1e-5);
            GeospatialTools.IsValidCoordinate(91, 0).Should().BeFalse();
            var act = () => GeospatialTools.DecodeGeohash("aaaa");
            act.Should().Throw<ArgumentException>();
        }
    }
}
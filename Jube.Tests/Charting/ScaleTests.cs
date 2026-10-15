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
using Jube.Charting.Scales;
using Xunit;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting
{
    public class ScaleTests
    {
        [Theory]
        [InlineData(0, 100, 0, 500, 25, 125)]
        [InlineData(-10, 10, 300, 0, 0, 150)]
        [InlineData(5, 15, 0, 1, 10, 0.5)]
        public void LinearMapsAndInvertsBack(double d0, double d1, double r0, double r1, double value, double expected)
        {
            var scale = new LinearScale(d0, d1, r0, r1);
            scale.Map(value).Should().BeApproximately(expected, 1e-9);

            scale.Invert(scale.Map(value)).Should().BeApproximately(value, 1e-9);
        }

        [Fact]
        public void LinearMapsEndpoints()
        {
            var scale = new LinearScale(2, 4, 10, 110);
            scale.Map(2).Should().Be(10);
            scale.Map(4).Should().Be(110);
            scale.Map(double.NaN).Should().Be(double.NaN);
        }

        [Fact]
        public void DegenerateDomainIsPadded()
        {
            var scale = new LinearScale(5, 5, 0, 100);
            scale.DomainMin.Should().BeLessThan(5);
            scale.DomainMax.Should().BeGreaterThan(5);
            scale.Map(5).Should().BeApproximately(50, 1e-9);
        }

        [Fact]
        public void NonFiniteBoundsAreRejected()
        {
            var act = () => new LinearScale(double.NaN, 1, 0, 1);
            act.Should().Throw<ChartSpecificationException>();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(123.456)]
        [InlineData(1000)]
        public void LogRoundTrips(double value)
        {
            var scale = new LogScale(1, 1000, 0, 300);
            scale.Invert(scale.Map(value)).Should().BeApproximately(value, value * 1e-9);
        }

        [Fact]
        public void LogPlacesDecadesEvenlyAndRejectsNonPositive()
        {
            var scale = new LogScale(1, 1000, 0, 300);
            scale.Map(10).Should().BeApproximately(100, 1e-9);
            scale.Map(100).Should().BeApproximately(200, 1e-9);
            scale.Map(0).Should().Be(double.NaN);
            scale.Map(-5).Should().Be(double.NaN);
            var act = () => new LogScale(0, 10, 0, 1);
            act.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void TimeScaleRoundTrips()
        {
            var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var t1 = new DateTime(2026, 1, 11, 0, 0, 0, DateTimeKind.Utc);
            var scale = new TimeScale(t0, t1, 0, 1000);
            scale.Map(t0.AddDays(5)).Should().BeApproximately(500, 1e-6);
            scale.MapBack(500).Should().BeCloseTo(t0.AddDays(5), TimeSpan.FromMilliseconds(1));
        }

        [Fact]
        public void BandScaleLaysOutBandsAndInverts()
        {
            var scale = new BandScale(4, 0, 400);
            scale.Step.Should().Be(100);
            scale.Bandwidth.Should().BeApproximately(80, 1e-9);
            scale.Start(0).Should().BeApproximately(10, 1e-9);
            scale.Center(1).Should().BeApproximately(150, 1e-9);

            for (var i = 0; i < 4; i++)
            {
                scale.Invert(scale.Center(i)).Should().Be(i);
            }

            scale.Invert(-50).Should().Be(0);
            scale.Invert(5000).Should().Be(3);
        }

        [Fact]
        public void BandScaleNeedsABand()
        {
            var act = () => new BandScale(0, 0, 1);
            act.Should().Throw<ChartSpecificationException>();
        }
    }
}
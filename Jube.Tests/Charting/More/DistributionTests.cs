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
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.More;
using Xunit;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.More
{
    public class DistributionTests
    {
        private static readonly double[] sample = [2.1, 3.4, 1.9, 5.6, 4.4, 3.8, 2.7, 6.1, 4.9, 3.3];

        [Fact]
        public void SilvermanBandwidthMatchesTheIndependentReference()
        {
            DensityMath.SilvermanBandwidth(sample).Should().BeApproximately(0.8078816866919472, 1e-12);
        }

        [Fact]
        public void QuantilesUseTheLinearInterpolationDefinition()
        {
            var sorted = DensityMath.Finite(sample);
            DensityMath.Quantile(sorted, 0.25).Should().BeApproximately(2.85, 1e-12);
            DensityMath.Quantile(sorted, 0.5).Should().BeApproximately(3.6, 1e-12);
            DensityMath.Quantile(sorted, 0.75).Should().BeApproximately(4.775, 1e-12);
        }

        [Theory]
        [InlineData(2.0, 0.16127321153526578, 0.19078543148158647)]
        [InlineData(3.5, 0.23069400856505193, 0.26013658013792884)]
        [InlineData(4.4, 0.19857808886794112, 0.18988704309331544)]
        [InlineData(6.0, 0.12086710136986559, 0.1437237184468675)]
        public void KernelDensityMatchesTheIndependentReference(double x, double silverman, double half)
        {
            Density(sample, null, x).Should().BeApproximately(silverman, 1e-9);
            Density(sample, 0.5, x).Should().BeApproximately(half, 1e-9);
        }

        private static double Density(double[] values, double? bandwidth, double x)
        {
            var h = bandwidth ?? DensityMath.SilvermanBandwidth(values);

            return DensityMath.Estimate(values, h, 8, 3, x, x + 1).Density[0];
        }

        [Fact]
        public void DensityIntegratesToOneAndBandwidthOverrideIsUsed()
        {
            var estimate = DensityMath.Estimate(sample, null, 2001);
            var sum = 0.0;

            for (var i = 1; i < estimate.Grid.Count; i++)
            {
                sum += (estimate.Density[i] + estimate.Density[i - 1]) / 2 * (estimate.Grid[i] - estimate.Grid[i - 1]);
            }

            sum.Should().BeApproximately(0.99965, 1e-4);
            DensityMath.Estimate(sample, 0.25).Bandwidth.Should().Be(0.25);
        }

        [Fact]
        public void BinnedEstimateForLargeSamplesTracksTheExactOne()
        {
            var big = Enumerable.Range(0, 6000).Select(i => 50 + 10 * ChartStats.NormalQuantile((i + 0.5) / 6000))
                .ToArray();

            var estimate = DensityMath.Estimate(big, 2, 64);
            var index = 32;
            var x = estimate.Grid[index];

            var exact = big.Sum(v => Math.Exp(-0.5 * Math.Pow((x - v) / 2, 2)) / Math.Sqrt(2 * Math.PI)) /
                        (big.Length * 2);

            estimate.Density[index].Should().BeApproximately(exact, exact * 0.01);
        }

        [Fact]
        public void EcdfStepsAreCumulativeSharesWithTies()
        {
            var steps = DensityMath.Ecdf([3.0, 1, 2, 2, double.NaN]);
            steps.Should().Equal((1.0, 0.25), (2.0, 0.75), (3.0, 1.0));
        }

        [Fact]
        public void DegenerateSamplesStillGetAPositiveBandwidth()
        {
            DensityMath.SilvermanBandwidth([5.0, 5, 5, 5]).Should().BeGreaterThan(0);
            DensityMath.SilvermanBandwidth([]).Should().Be(1);
            Action bad = () => DensityMath.Estimate(sample, -1);
            bad.Should().Throw<ChartSpecificationException>();
            Action grid = () => DensityMath.Estimate(sample, null, 3);
            grid.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void DensityChartShowsBandwidthAndKeepsTheFillOutOfTheTooltip()
        {
            var svg = MoreSamples.Density().Render();
            svg.Should().Contain("bandwidth 5.429").And.Contain("class=\"cib\"");
            svg.Should().NotContain("Density: 0 to");
            MoreAssert.IsSafe(svg);
            var custom = Chart.Density().Values("a", sample).Bandwidth(0.3).Filled(false).Render();
            custom.Should().Contain("bandwidth 0.3").And.NotContain("class=\"cib\"");
        }

        [Fact]
        public void EcdfChartHasBandsAndComplementaryForm()
        {
            var svg = MoreSamples.Ecdf().Render();
            svg.Should().Contain("95% band (DKW)").And.Contain("class=\"ln\"");
            var complementary = Chart.Ecdf().Values("a", sample).Complementary().Render();
            complementary.Should().Contain("100%");
            Action alpha = () => Chart.Ecdf().Values("a", sample).ConfidenceBand(2).Render();
            alpha.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void ViolinDrawsOneShapeBoxAndMedianPerGroup()
        {
            var svg = MoreSamples.Violin(false).Render();
            Regex.Matches(svg, "class=\"vi\"").Count.Should().Be(3);
            Regex.Matches(svg, "class=\"vbx\"").Count.Should().Be(3);
            Regex.Matches(svg, "class=\"vmd\"").Count.Should().Be(3);
            svg.Should().Contain("Bandwidth:").And.Contain(".mk:not(:hover) .vi");
            MoreAssert.IsSafe(svg);
            MoreAssert.IsSafe(MoreSamples.Violin(true).Render());
        }

        [Fact]
        public void ViolinHandlesEmptyAndSingleValueGroups()
        {
            var svg = Chart.Violin().Values("none", []).Values("one", [4.0]).Values("nan", [double.NaN, 1, 2, 3])
                .Render();
            MoreAssert.IsSafe(svg);
            svg.Should().Contain("n: 0").And.Contain("n: 1");
            Chart.Violin().Render().Should().Contain("No data");
        }

        [Fact]
        public void RidgelineDrawsOneRidgePerGroupAndValidatesOverlap()
        {
            var svg = MoreSamples.Ridgeline().Render();
            Regex.Matches(svg, "class=\"ri\"").Count.Should().Be(5);
            svg.Should().Contain("class=\"rb\"");
            MoreAssert.IsSafe(svg);
            Action overlap = () => Chart.Ridgeline().Values("a", sample).Overlap(20).Render();
            overlap.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void CapsAreEnforcedAcrossDistributionCharts()
        {
            var big = new double[ChartLimits.MaxScoredObservations + 1];
            Action density = () => Chart.Density().Values("a", big).Render();
            density.Should().Throw<ChartLimitException>();
            var many = Chart.Violin();

            for (var i = 0; i < ChartLimits.MaxSeries + 1; i++)
            {
                many.Values("g" + i, sample);
            }

            Action violin = () => many.Render();
            violin.Should().Throw<ChartLimitException>();
            Action adjust = () => Chart.Violin().Values("a", sample).BandwidthAdjust(0).Render();
            adjust.Should().Throw<ChartSpecificationException>();
        }
    }
}
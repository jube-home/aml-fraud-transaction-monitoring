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
using Jube.Charting;
using Jube.Charting.Exceptions;
using Jube.Charting.More;
using Jube.Charting.More.Models;
using Xunit;

namespace Jube.Test.Charting.More
{
    public class SurvivalTests
    {
        private static SurvivalCurve Curve(ConfidenceMethod method)
        {
            return SurvivalMath.KaplanMeier(MoreSamples.MercaptopurineTimes, MoreSamples.MercaptopurineEvents, 0.95,
                method);
        }

        [Theory]
        [InlineData(6, 21, 0.8571428571, 0.0763603548)]
        [InlineData(7, 17, 0.8067226891, 0.0869352852)]
        [InlineData(10, 15, 0.7529411765, 0.0963496530)]
        [InlineData(13, 12, 0.6901960784, 0.1068147078)]
        [InlineData(16, 11, 0.6274509804, 0.1140538653)]
        [InlineData(22, 7, 0.5378151261, 0.1282337517)]
        [InlineData(23, 6, 0.4481792717, 0.1345914568)]
        public void KaplanMeierEstimateAndGreenwoodErrorMatchTheIndependentReference(double time, int atRisk,
            double survival, double error)
        {
            var step = Curve(ConfidenceMethod.LogLog).Steps.Single(s => s.Time == time);
            step.AtRisk.Should().Be(atRisk);
            step.Survival.Should().BeApproximately(survival, 1e-9);
            step.StandardError.Should().BeApproximately(error, 1e-9);
        }

        [Theory]
        [InlineData(ConfidenceMethod.Linear, 6, 0.7074793118, 1.0)]
        [InlineData(ConfidenceMethod.Linear, 23, 0.1843848638, 0.7119736796)]
        [InlineData(ConfidenceMethod.Log, 6, 0.7198170839, 1.0)]
        [InlineData(ConfidenceMethod.Log, 22, 0.3370366162, 0.8582008480)]
        [InlineData(ConfidenceMethod.LogLog, 6, 0.6197179553, 0.9515517476)]
        [InlineData(ConfidenceMethod.LogLog, 13, 0.4316102225, 0.8490659633)]
        [InlineData(ConfidenceMethod.LogLog, 23, 0.1880520060, 0.6801426285)]
        public void ConfidenceIntervalsMatchTheIndependentReference(ConfidenceMethod method, double time, double lower,
            double upper)
        {
            var step = Curve(method).Steps.Single(s => s.Time == time);
            step.Lower.Should().BeApproximately(lower, 1e-8);
            step.Upper.Should().BeApproximately(upper, 1e-8);
        }

        [Fact]
        public void TiesCensoringAndMedianAreHandled()
        {
            var curve = Curve(ConfidenceMethod.LogLog);
            curve.Count.Should().Be(21);
            curve.EventCount.Should().Be(9);
            curve.CensorTimes.Should().HaveCount(12);
            curve.Steps[0].Time.Should().Be(0);
            curve.Steps[0].Survival.Should().Be(1);
            curve.Median.Should().Be(23);
            curve.AtRiskAt(0).Should().Be(21);
            curve.AtRiskAt(23).Should().Be(6);
            curve.AtRiskAt(36).Should().Be(0);
        }

        [Fact]
        public void LogRankMatchesTheIndependentReference()
        {
            var result = SurvivalMath.LogRank(
                [MoreSamples.MercaptopurineTimes, MoreSamples.PlaceboTimes],
                [MoreSamples.MercaptopurineEvents, [.. MoreSamples.PlaceboTimes.Select(_ => true)]]);
            result.Statistic.Should().BeApproximately(16.79294098921654, 1e-8);
            result.DegreesOfFreedom.Should().Be(1);
            result.PValue.Should().BeApproximately(4.168809109334527e-05, 1e-9);
        }

        [Fact]
        public void NormalAndStudentQuantilesMatchKnownValues()
        {
            ChartStats.NormalQuantile(0.975).Should().BeApproximately(1.9599639845400536, 1e-7);
            ChartStats.NormalQuantile(0.5).Should().BeApproximately(0, 1e-9);
            ChartStats.StudentTQuantile(0.975, 5).Should().BeApproximately(2.570581835636314, 1e-8);
            ChartStats.StudentTQuantile(0.975, 30).Should().BeApproximately(2.0422724563012373, 1e-8);
            ChartStats.ChiSquareSurvival(3.841458820694124, 1).Should().BeApproximately(0.05, 1e-9);
        }

        [Fact]
        public void BadSpecificationsAreRejected()
        {
            Action mismatch = () => SurvivalMath.KaplanMeier([1.0, 2], [true]);
            mismatch.Should().Throw<ChartSpecificationException>();
            Action negative = () => SurvivalMath.KaplanMeier([-1.0], [true]);
            negative.Should().Throw<ChartSpecificationException>();
            Action level = () => SurvivalMath.KaplanMeier([1.0], [true], 1.5);
            level.Should().Throw<ChartSpecificationException>();
            Action render = () => Chart.Survival().Group("a", [1.0, 2], [true]).Render();
            render.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void ObservationCapIsEnforced()
        {
            var big = new double[ChartLimits.MaxScoredObservations + 1];
            var flags = new bool[big.Length];
            Action act = () => SurvivalMath.KaplanMeier(big, flags);
            act.Should().Throw<ChartLimitException>();
        }

        [Fact]
        public void ChartCarriesBandsCensorTicksAtRiskAndMedian()
        {
            var svg = MoreSamples.Survival().Render();

            svg.Should().Contain("class=\"cib\"").And.Contain("class=\"ck\"").And.Contain("Number at risk")
                .And.Contain("Log-rank p &lt; 0.001").And.Contain("class=\"med\"");

            svg.Should().Contain("95% CI");
            SvgAssert.IsSafe(svg);
        }

        [Fact]
        public void ManyGroupsAreCappedAndEmptyChartsRender()
        {
            var builder = Chart.Survival();

            for (var i = 0; i < ChartLimits.MaxSeries + 1; i++)
            {
                builder.Group("g" + i, [1.0], [true]);
            }

            Action act = () => builder.Render();
            act.Should().Throw<ChartLimitException>();
            var empty = Chart.Survival().Group("none", [], Array.Empty<bool>()).Render();
            empty.Should().Contain("No data");
            SvgAssert.IsSafe(empty);
        }

        [Fact]
        public void CumulativeIncidenceFlipsTheCurve()
        {
            var svg = Chart.Survival().Cumulative().Group("a", [1.0, 2, 3], [true, true, false]).Render();
            svg.Should().Contain("Cumulative incidence");
            SvgAssert.IsSafe(svg);
        }
    }
}
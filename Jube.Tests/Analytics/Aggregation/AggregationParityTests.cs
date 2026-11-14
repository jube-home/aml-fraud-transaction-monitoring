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

using FluentAssertions;
using Jube.Analytics.Aggregation;
using Jube.Service.Agent.AdvancedAnalyticsTools.DescriptiveStatistics;
using Xunit;

namespace Jube.Test.Analytics.Aggregation
{
    [Trait("Category", "Unit")]
    public sealed class AggregationParityTests
    {
        private const double Tight = 1e-9;
        private readonly CentralTendencyTools central = new();
        private readonly QuantileRankAndFrequencyTools quantile = new();
        private readonly SpreadAndShapeTools spread = new();

        public static TheoryData<double[]> Vectors =>
        [
            [2, 4, 4, 4, 5, 5, 7, 9],
            [12.5, 3.1, 44.0, 7.7, 19.2, 3.1, 88.8, 0.5, 15.0, 27.3, 9.9],
            [100, 250.5, 75.25, 1200, 33.3, 410],
            [0.01, 0.02, 5000, 0.03, 0.01, 0.04, 0.02, 0.05, 0.01, 0.03, 0.02, 0.06, 0.01]
        ];

        [Theory]
        [MemberData(nameof(Vectors))]
        public void CentralTendencyAgreesWithTheDescriptiveTools(double[] values)
        {
            foreach (var fraction in new[] { 0, 0.1, 0.2, 0.25, 0.4 })
            {
                CentralTendencyAggregations.TrimmedMean(values, fraction).Should()
                    .BeApproximately(central.TrimmedMean(values, fraction).Value, Tight);

                CentralTendencyAggregations.WinsorisedMean(values, fraction).Should()
                    .BeApproximately(central.WinsorisedMean(values, fraction).Value, Tight);
            }

            foreach (var alpha in new[] { 0.1, 0.3, 0.7, 1 })
            {
                CentralTendencyAggregations.ExponentialWeightedMean(values, alpha).Should()
                    .BeApproximately(central.ExponentialWeightedMean(values, alpha).Value, Tight);
            }

            CentralTendencyAggregations.GeometricMean(values).Should()
                .BeApproximately(central.GeometricMean(values).Value, Tight);

            CentralTendencyAggregations.HarmonicMean(values).Should()
                .BeApproximately(central.HarmonicMean(values).Value, Tight);

            CentralTendencyAggregations.RootMeanSquare(values).Should()
                .BeApproximately(central.QuadraticMean(values).Value, Tight);
        }

        [Theory]
        [MemberData(nameof(Vectors))]
        public void DispersionAndShapeAgreeWithTheDescriptiveTools(double[] values)
        {
            foreach (var flag in new[] { true, false })
            {
                DispersionAggregations.Variance(values, flag).Should()
                    .BeApproximately(spread.Variance(values, flag).Value, Tight * 1e6);

                DispersionAggregations.CoefficientOfVariation(values, flag).Should()
                    .BeApproximately(spread.CoefficientOfVariation(values, flag).Value, Tight);

                DispersionAggregations.MedianAbsoluteDeviation(values, flag).Should()
                    .BeApproximately(spread.MedianAbsoluteDeviation(values, flag).Value, Tight);

                DispersionAggregations.MeanAbsoluteDeviation(values, flag).Should()
                    .BeApproximately(spread.MeanAbsoluteDeviation(values, flag).Value, Tight);
            }

            DispersionAggregations.InterquartileRange(values).Should()
                .BeApproximately(spread.InterquartileRange(values).Value, Tight);

            ShapeAggregations.GiniCoefficient(values).Should()
                .BeApproximately(spread.GiniCoefficient(values).Value, Tight);
        }

        [Theory]
        [MemberData(nameof(Vectors))]
        public void PercentilesAndThresholdSharesAgreeWithTheDescriptiveTools(double[] values)
        {
            foreach (var percentile in new[] { 0d, 5, 25, 50, 75, 90, 95, 99, 100 })
            {
                DispersionAggregations.Percentile(values, percentile).Should()
                    .BeApproximately(quantile.Quantile(values, percentile / 100d).Value, Tight);
            }

            foreach (var threshold in new[] { 0d, 4, 5, 10, 100 })
            {
                var share = quantile.ThresholdShare(values, threshold);

                ThresholdAggregations.ShareAbove(values, threshold).Should().BeApproximately(share.ProportionAbove, Tight);
                ThresholdAggregations.CountAbove(values, threshold).Should().Be(share.Above);
                ThresholdAggregations.CountBelow(values, threshold).Should().Be(share.Below);
            }
        }
    }
}

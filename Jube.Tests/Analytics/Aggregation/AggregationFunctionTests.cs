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
using Jube.Analytics.Aggregation;
using Xunit;

namespace Jube.Test.Analytics.Aggregation
{
    [Trait("Category", "Unit")]
    public sealed class AggregationFunctionTests
    {
        private const double Tight = 1e-9;
        private static readonly double[] textbook = [2, 4, 4, 4, 5, 5, 7, 9];

        [Fact]
        public void CentralTendencyKnownAnswers()
        {
            CentralTendencyAggregations.GeometricMean([1, 3, 9]).Should().BeApproximately(3, Tight);
            CentralTendencyAggregations.HarmonicMean([1, 2, 4]).Should().BeApproximately(12d / 7d, Tight);
            CentralTendencyAggregations.TrimmedMean([100, 1, 4, 2, 3], 0.2).Should().BeApproximately(3, Tight);
            CentralTendencyAggregations.WinsorisedMean([100, 1, 4, 2, 3], 0.2).Should().BeApproximately(3, Tight);
            CentralTendencyAggregations.ExponentialWeightedMean([10, 20, 30], 0.5).Should().BeApproximately(42.5 / 1.75, Tight);
            CentralTendencyAggregations.ExponentialWeightedMean([10, 20, 30], 1).Should().Be(30);
            CentralTendencyAggregations.RootMeanSquare([3, 4]).Should().BeApproximately(Math.Sqrt(12.5), Tight);
        }

        [Fact]
        public void GeometricAndHarmonicMeansAreUndefinedForValuesThatAreNotPositive()
        {
            CentralTendencyAggregations.GeometricMean([1, 0, 9]).Should().Be(double.NaN);
            CentralTendencyAggregations.HarmonicMean([1, -2, 4]).Should().Be(double.NaN);
        }

        [Fact]
        public void DispersionKnownAnswers()
        {
            DispersionAggregations.Variance(textbook).Should().BeApproximately(32d / 7d, Tight);
            DispersionAggregations.Variance(textbook, false).Should().BeApproximately(4, Tight);
            DispersionAggregations.Range(textbook).Should().Be(7);
            DispersionAggregations.InterquartileRange(textbook).Should().BeApproximately(1.5, Tight);
            DispersionAggregations.Percentile(textbook, 50).Should().BeApproximately(4.5, Tight);
            DispersionAggregations.Percentile([5, 1, 4, 2, 3]).Should().BeApproximately(4.8, Tight);
            DispersionAggregations.Percentile(textbook, 0).Should().Be(2);
            DispersionAggregations.Percentile(textbook, 100).Should().Be(9);
            DispersionAggregations.MedianAbsoluteDeviation(textbook, false).Should().BeApproximately(0.5, Tight);
            DispersionAggregations.MedianAbsoluteDeviation(textbook).Should().BeApproximately(0.741301109252801, Tight);
            DispersionAggregations.MeanAbsoluteDeviation(textbook).Should().BeApproximately(1.5, Tight);
            DispersionAggregations.MeanAbsoluteDeviation(textbook, true).Should().BeApproximately(1.5, Tight);
            DispersionAggregations.CoefficientOfVariation(textbook).Should().BeApproximately(Math.Sqrt(32d / 7d) / 5d, Tight);
        }

        [Fact]
        public void ShapeKnownAnswers()
        {
            ShapeAggregations.Entropy([1, 1, 2, 2]).Should().BeApproximately(1, Tight);
            ShapeAggregations.Entropy([5, 5, 5]).Should().Be(0);
            ShapeAggregations.GiniCoefficient([1, 1, 1, 1]).Should().BeApproximately(0, Tight);
            ShapeAggregations.GiniCoefficient([0, 10, 0, 0]).Should().BeApproximately(0.75, Tight);
            ShapeAggregations.GiniCoefficient([1, -1]).Should().Be(double.NaN);
        }

        [Fact]
        public void PositionalKnownAnswersFollowTheOrderGiven()
        {
            PositionalAggregations.NthValue([7, 8, 9], 2).Should().Be(8);
            PositionalAggregations.NthValue([7, 8, 9]).Should().Be(7);
            PositionalAggregations.NthValue([7], 3).Should().Be(double.NaN);
            PositionalAggregations.FirstToLastChange([3, 9, 5]).Should().Be(2);
            PositionalAggregations.FirstToLastChange([5, 9, 3]).Should().Be(-2);
            PositionalAggregations.LargestStep([1, 4, 2, 10]).Should().Be(8);
            PositionalAggregations.LargestStep([1]).Should().Be(double.NaN);
        }

        [Fact]
        public void ThresholdKnownAnswersTreatAValueOnTheThresholdAsNeitherAboveNorBelow()
        {
            ThresholdAggregations.CountAbove([1, 5, 10, 5], 5).Should().Be(1);
            ThresholdAggregations.CountBelow([1, 5, 10, 5], 5).Should().Be(1);
            ThresholdAggregations.ShareAbove([1, 5, 10, 5], 5).Should().Be(0.25);
            ThresholdAggregations.SumAbove([1, 5, 10, 20], 5).Should().Be(30);
            ThresholdAggregations.LongestRunAbove([6, 7, 1, 8, 9, 10, 2], 5).Should().Be(3);
            ThresholdAggregations.LongestRunAbove([1, 2], 5).Should().Be(0);
        }

        [Fact]
        public void LastValueKnownAnswersScoreTheLastValueAgainstTheOnesBeforeIt()
        {
            LastValueAggregations.LastValueZScore([10, 12, 14, 30]).Should().BeApproximately(9, Tight);
            LastValueAggregations.LastValueModifiedZScore([10, 12, 14, 30]).Should().BeApproximately(6.0705, Tight);
            LastValueAggregations.LastValuePercentileRank([1, 2, 3, 4, 2.5]).Should().Be(50);
            LastValueAggregations.LastValueToMeanRatio([10, 20, 45]).Should().BeApproximately(3, Tight);
            LastValueAggregations.LastValueZScore([10, 30]).Should().Be(double.NaN);
        }

        [Fact]
        public void TrendKnownAnswers()
        {
            TrendAggregations.TrendSlope([1, 3, 5, 7]).Should().BeApproximately(2, Tight);
            TrendAggregations.TrendSlope([7, 5, 3, 1]).Should().BeApproximately(-2, Tight);
            TrendAggregations.Autocorrelation([1, 2, 3, 4, 5]).Should().BeApproximately(0.4, Tight);
            TrendAggregations.Autocorrelation([1, 2], 2).Should().Be(double.NaN);
        }

        [Fact]
        public void EveryFunctionAnswersAnEmptyArrayWithoutThrowing()
        {
            foreach (var descriptor in AggregationCatalogue.Entries)
            {
                var result = descriptor.Bind(null)([]);

                (double.IsNaN(result) || result == 0).Should().BeTrue(descriptor.Name);
            }
        }

        [Fact]
        public void NoFunctionChangesTheArrayItIsGiven()
        {
            foreach (var descriptor in AggregationCatalogue.Entries)
            {
                double[] values = [9, 1, 8, 2, 7, 3];

                descriptor.Bind(null)(values);

                values.Should().Equal([9, 1, 8, 2, 7, 3], descriptor.Name);
            }
        }
    }
}

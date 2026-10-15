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
using Jube.Charting.Charts;
using Jube.Charting.Performance;
using Jube.Charting.Performance.Models;
using Xunit;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.Performance
{
    public class PerformanceMathTests
    {
        private const double Tolerance = 1e-12;

        [Fact]
        public void GainsMatchesThePythonReferenceAtEveryDistinctScore()
        {
            var g = PerformanceMath.Gains(PerformanceData.Actual, PerformanceData.Scores);

            g.Positives.Should().Be(12);
            g.Negatives.Should().Be(28);
            g.Population.Length.Should().Be(34);
            g.Population[0].Should().Be(0);
            g.Captured[0].Should().Be(0);
            g.Population[1].Should().BeApproximately(0.025, Tolerance);
            g.Captured[1].Should().BeApproximately(1 / 12.0, Tolerance);
            g.Population[3].Should().BeApproximately(0.1, Tolerance);
            g.Captured[3].Should().BeApproximately(0.25, Tolerance);
            g.Population[19].Should().BeApproximately(0.55, Tolerance);
            g.Captured[19].Should().BeApproximately(1.0, Tolerance);
            g.Population[^1].Should().BeApproximately(1.0, Tolerance);
            g.Captured[^1].Should().BeApproximately(1.0, Tolerance);
            g.Thresholds[0].Should().Be(double.PositiveInfinity);
            g.Thresholds[1].Should().Be(0.95);
        }

        [Fact]
        public void GainsIsMonotoneAndAValidCurveWhateverTheTies()
        {
            var g = PerformanceMath.Gains(PerformanceData.Actual, PerformanceData.Scores);

            for (var i = 1; i < g.Population.Length; i++)
            {
                g.Population[i].Should().BeGreaterThan(g.Population[i - 1]);
                g.Captured[i].Should().BeGreaterThanOrEqualTo(g.Captured[i - 1]);
            }
        }

        [Fact]
        public void DecilesMatchThePythonReferenceIncludingFractionalTies()
        {
            var rows = PerformanceMath.Deciles(PerformanceData.Actual, PerformanceData.Scores);

            rows.Count.Should().Be(10);
            var positives = new[] { 3.0, 3, 2, 1.5, 0.5, 2, 0, 0, 0, 0 };

            var decileLift = new[]
                { 2.5, 2.5, 5 / 3.0, 1.25, 5 / 12.0, 5 / 3.0, 0, 0, 0, 0 };

            var cumulativeLift = new[]
                { 2.5, 2.5, 20 / 9.0, 95 / 48.0, 5 / 3.0, 5 / 3.0, 10 / 7.0, 1.25, 10 / 9.0, 1 };

            var captured = new[] { 0.25, 0.5, 2 / 3.0, 19 / 24.0, 5 / 6.0, 1, 1, 1, 1, 1 };

            for (var i = 0; i < 10; i++)
            {
                rows[i].Decile.Should().Be(i + 1);
                rows[i].Population.Should().BeApproximately(0.1, Tolerance);
                rows[i].Positives.Should().BeApproximately(positives[i], Tolerance);
                rows[i].DecileLift.Should().BeApproximately(decileLift[i], Tolerance);
                rows[i].CumulativeLift.Should().BeApproximately(cumulativeLift[i], Tolerance);
                rows[i].CumulativeCaptured.Should().BeApproximately(captured[i], Tolerance);
            }
        }

        [Fact]
        public void DecilePositivesAlwaysSumToTheTotalPositives()
        {
            var rows = PerformanceMath.Deciles(PerformanceData.Actual, PerformanceData.Scores, 7);

            rows.Sum(r => r.Positives).Should().BeApproximately(12, 1e-9);
            rows.Sum(r => r.Population).Should().BeApproximately(1, 1e-9);
        }

        [Fact]
        public void KsMatchesThePythonReference()
        {
            var ks = PerformanceMath.Ks(PerformanceData.Actual, PerformanceData.Scores);

            ks.Statistic.Should().BeApproximately(0.6428571428571429, Tolerance);
            ks.AtScore.Should().Be(0.35);
            ks.PositiveAtMax.Should().Be(0);
            ks.NegativeAtMax.Should().BeApproximately(0.6428571428571429, Tolerance);
            ks.PositiveCdf[^1].Should().Be(1);
            ks.NegativeCdf[^1].Should().Be(1);
            ks.Scores.Should().BeInAscendingOrder();
        }

        [Fact]
        public void KsAgreesWithTheMaximumOfTprMinusFprFromTheRocCurve()
        {
            var roc = CurveMath.Roc(PerformanceData.Actual, PerformanceData.Scores);
            var expected = roc.TruePositiveRate.Zip(roc.FalsePositiveRate, (t, f) => t - f).Max();

            PerformanceMath.Ks(PerformanceData.Actual, PerformanceData.Scores).Statistic
                .Should().BeApproximately(expected, Tolerance);
        }

        [Fact]
        public void KsThinningKeepsTheMaximumSeparationPoint()
        {
            var random = new Random(3);
            var scores = Enumerable.Range(0, 5000).Select(_ => random.NextDouble()).ToArray();
            var actual = scores.Select(s => random.NextDouble() < s * s).ToArray();

            var full = PerformanceMath.Ks(actual, scores, 100_000);
            var thin = PerformanceMath.Ks(actual, scores, 50);

            thin.Scores.Length.Should().BeLessThan(200);
            thin.Statistic.Should().Be(full.Statistic);
            thin.Scores.Should().Contain(full.AtScore);
        }

        [Fact]
        public void KsWithUniformWeightsMatchesTheUnweightedResult()
        {
            var uniform = Enumerable.Repeat(1.0, PerformanceData.Actual.Length).ToArray();

            var weighted = PerformanceMath.Ks(PerformanceData.Actual, PerformanceData.Scores, weights: uniform);
            var unweighted = PerformanceMath.Ks(PerformanceData.Actual, PerformanceData.Scores);

            weighted.Statistic.Should().Be(unweighted.Statistic);
            weighted.AtScore.Should().Be(unweighted.AtScore);
            weighted.PositiveCdf.Should().Equal(unweighted.PositiveCdf);
            weighted.NegativeCdf.Should().Equal(unweighted.NegativeCdf);
        }

        [Fact]
        public void KsWeightingByValueCanShiftTheMaximumSeparationPoint()
        {
            bool[] actual = [true, false, true, false, true, false];
            double[] scores = [0.9, 0.8, 0.7, 0.6, 0.4, 0.2];
            double[] weights = [1, 20, 1, 1, 1, 1];

            var unweighted = PerformanceMath.Ks(actual, scores);
            var weighted = PerformanceMath.Ks(actual, scores, weights: weights);

            unweighted.AtScore.Should().Be(0.8);
            unweighted.Statistic.Should().BeApproximately(0.33333333333333337, Tolerance);
            weighted.AtScore.Should().Be(0.7);
            weighted.Statistic.Should().BeApproximately(0.5757575757575757, Tolerance);
        }

        [Fact]
        public void SweepMatchesThePythonReferenceAtSelectedThresholds()
        {
            var sweep = PerformanceMath.Sweep(PerformanceData.Actual, PerformanceData.Scores, 0, 1);

            sweep.Points.Length.Should().Be(101);
            Check(sweep, 0, 0.3, 1.0, 0.46153846153846156, 0.0);
            Check(sweep, 10, 1 / 3.0, 1.0, 0.5, 0.21821789023599236);
            Check(sweep, 25, 0.42857142857142855, 1.0, 0.6, 0.42857142857142855);
            Check(sweep, 50, 0.5882352941176471, 0.8333333333333334, 0.6896551724137931, 0.5407518998772001);
            Check(sweep, 75, 0.75, 0.5, 0.6, 0.49099025303098287);
            Check(sweep, 90, 0.75, 0.25, 0.375, 0.32732683535398854);
            sweep.Points[100].Precision.Should().Be(double.NaN);
            sweep.Points[100].Recall.Should().Be(0);
            sweep.Points[100].F1.Should().Be(0);
            sweep.Points[100].Mcc.Should().Be(0);
            sweep.Points[50].Specificity.Should().BeApproximately(0.75, Tolerance);
        }

        [Fact]
        public void SweepBestF1IsTheMaximumOverTheGrid()
        {
            var sweep = PerformanceMath.Sweep(PerformanceData.Actual, PerformanceData.Scores, 0, 1);

            sweep.Points[sweep.BestF1Index].F1.Should().Be(sweep.Points.Max(p => p.F1));
            sweep.Points[sweep.BestF1Index].Threshold.Should().BeApproximately(0.59, Tolerance);
            sweep.Points[sweep.BestF1Index].F1.Should().BeApproximately(0.72, 0.005);
        }

        [Fact]
        public void DistributionBinsEveryObservationOnceAndPutsTheTopScoreInTheLastBin()
        {
            var d = PerformanceMath.Distribution(PerformanceData.Actual, PerformanceData.Scores, 0, 1, 10);

            d.Positives.Should().Be(12);
            d.Negatives.Should().Be(28);
            (d.PositiveCounts.Sum() + d.NegativeCounts.Sum()).Should().Be(40);
            d.PositiveCounts[9].Should().Be(3);
            d.NegativeCounts[9].Should().Be(1);
            d.NegativeCounts[0].Should().Be(4);
            d.Edges.Length.Should().Be(11);
        }

        [Fact]
        public void ScoresOutsideTheRangeAreClampedIntoTheEndBinsAndNonFiniteScoresAreDropped()
        {
            var d = PerformanceMath.Distribution([true, false, true, false], [-3, double.NaN, 4, 0.5], 0, 1, 4);

            d.PositiveCounts.Should().Equal(1, 0, 0, 1);
            d.NegativeCounts.Should().Equal(0, 0, 1, 0);
        }

        [Fact]
        public void DistributionWeightsScaleTheBinsAndTotalsInsteadOfCountingRows()
        {
            bool[] actual = [true, true, false];
            double[] scores = [0.2, 0.7, 0.7];
            double[] weights = [10, 1, 5];

            var d = PerformanceMath.Distribution(actual, scores, 0, 1, 2, weights);

            d.Positives.Should().Be(11);
            d.Negatives.Should().Be(5);
            d.PositiveCounts.Should().Equal(10, 1);
            d.NegativeCounts.Should().Equal(0, 5);
        }

        [Fact]
        public void SingleClassOrEmptyInputGivesEmptyResultsRatherThanThrowing()
        {
            var scores = new[] { 0.1, 0.5, 0.9 };
            var allNegative = new[] { false, false, false };

            PerformanceMath.Gains(allNegative, scores).Population.Should().BeEmpty();
            PerformanceMath.Deciles(allNegative, scores).Should().BeEmpty();
            PerformanceMath.Ks(allNegative, scores).Scores.Should().BeEmpty();
            double.IsNaN(PerformanceMath.Ks(allNegative, scores).Statistic).Should().BeTrue();
            PerformanceMath.Sweep(allNegative, scores, 0, 1).Points.Should().BeEmpty();
            PerformanceMath.Gains([], []).Population.Should().BeEmpty();
        }

        [Fact]
        public void MismatchedLengthsAreRejected()
        {
            var act = () => PerformanceMath.Gains([true], [0.1, 0.2]);

            act.Should().Throw<ChartSpecificationException>();
        }

        private static void Check(SweepResult sweep, int index, double precision, double recall, double f1,
            double mcc)
        {
            var p = sweep.Points[index];
            p.Precision.Should().BeApproximately(precision, Tolerance);
            p.Recall.Should().BeApproximately(recall, Tolerance);
            p.F1.Should().BeApproximately(f1, Tolerance);
            p.Mcc.Should().BeApproximately(mcc, Tolerance);
        }
    }
}
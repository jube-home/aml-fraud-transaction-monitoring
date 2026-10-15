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
using Jube.Analytics;
using Jube.Analytics.Models;
using Xunit;

namespace Jube.Test.Analytics
{
    public class BinaryMetricsTests
    {
        private static readonly double[] s = FoundationFixtures.D1Scores;
        private static readonly int[] y = FoundationFixtures.D1Labels;

        [Fact]
        public void Confusion_KnownAnswers()
        {
            var c = BinaryMetrics.Confusion(s, y, 0.5);
            (c.TruePositives, c.FalsePositives, c.TrueNegatives, c.FalseNegatives).Should().Be((3, 1, 4, 2));
            c.Accuracy.Should().BeApproximately(0.7, 1e-12);
            c.Precision.Should().BeApproximately(0.75, 1e-12);
            c.Recall.Should().BeApproximately(0.6, 1e-12);
            c.Specificity.Should().BeApproximately(0.8, 1e-12);
            c.F1.Should().BeApproximately(0.6666666666666665, 1e-12);
            c.FBeta(2).Should().BeApproximately(0.625, 1e-12);
            c.FBeta(0.5).Should().BeApproximately(0.7142857142857143, 1e-12);
            c.Mcc.Should().BeApproximately(0.408248290463863, 1e-12);
            c.CohenKappa.Should().BeApproximately(0.3999999999999999, 1e-12);
        }

        [Fact]
        public void Confusion_ThresholdIsInclusive()
        {
            var c = BinaryMetrics.Confusion([0.4, 0.4], [1, 0], 0.4);
            (c.TruePositives, c.FalsePositives).Should().Be((1, 1));
        }

        [Fact]
        public void ConfusionMatrix_DegenerateCountsAreDefined()
        {
            var none = new ConfusionMatrix(0, 0, 5, 0);
            none.Precision.Should().Be(0);
            none.Recall.Should().Be(0);
            none.F1.Should().Be(0);
            none.Mcc.Should().Be(0);
            none.CohenKappa.Should().Be(0);
            none.Accuracy.Should().Be(1);
            double.IsNaN(new ConfusionMatrix(0, 0, 0, 0).Accuracy).Should().BeTrue();

            var perfect = new ConfusionMatrix(4, 0, 6, 0);
            perfect.Mcc.Should().BeApproximately(1, 1e-12);
            perfect.CohenKappa.Should().BeApproximately(1, 1e-12);

            var inverted = new ConfusionMatrix(0, 6, 0, 4);
            inverted.Mcc.Should().BeApproximately(-1, 1e-12);
        }

        [Fact]
        public void FBeta_RejectsNonPositiveBeta()
        {
            Action act = () => new ConfusionMatrix(1, 1, 1, 1).FBeta(0);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void RocAuc_WithTies_MatchesIndependentPairCount()
        {
            BinaryMetrics.RocAuc(s, y).Should().BeApproximately(0.72, 1e-12);
            BinaryMetrics.Gini(s, y).Should().BeApproximately(0.44, 1e-12);
        }

        [Fact]
        public void RocAuc_Extremes()
        {
            BinaryMetrics.RocAuc([0.1, 0.2, 0.8, 0.9], [0, 0, 1, 1]).Should().Be(1);
            BinaryMetrics.RocAuc([0.9, 0.8, 0.2, 0.1], [0, 0, 1, 1]).Should().Be(0);
            BinaryMetrics.RocAuc([0.5, 0.5, 0.5, 0.5], [0, 1, 0, 1]).Should().Be(0.5);
        }

        [Fact]
        public void RocAuc_SingleClassIsNaN()
        {
            double.IsNaN(BinaryMetrics.RocAuc([0.1, 0.2], [1, 1])).Should().BeTrue();
            double.IsNaN(BinaryMetrics.AveragePrecision([0.1, 0.2], [0, 0])).Should().BeTrue();
            var result = BinaryMetrics.RocAucWithStandardError([0.1, 0.2], [0, 0]);
            double.IsNaN(result.Auc).Should().BeTrue();
            double.IsNaN(result.StandardError).Should().BeTrue();
        }

        [Fact]
        public void RocAuc_IsInvariantToMonotoneTransformAndDropsNaN()
        {
            var transformed = s.Select(score => Math.Exp(5 * score)).ToArray();
            BinaryMetrics.RocAuc(transformed, y).Should().BeApproximately(0.72, 1e-12);

            BinaryMetrics.RocAuc([.. s, double.NaN], [.. y, 1])
                .Should().BeApproximately(0.72, 1e-12);
        }

        [Fact]
        public void DeLong_KnownAnswer()
        {
            var result = BinaryMetrics.RocAucWithStandardError(s, y);
            result.Auc.Should().BeApproximately(0.72, 1e-12);
            result.StandardError.Should().BeApproximately(0.17262676501632068, 1e-12);
            var (lower, upper) = result.Interval();
            lower.Should().BeApproximately(Math.Max(0, 0.72 - 1.959963984540054 * result.StandardError), 1e-12);
            upper.Should().BeLessThanOrEqualTo(1);
        }

        [Fact]
        public void DeLong_TooFewRowsInAClassGivesNaNStandardError()
        {
            var result = BinaryMetrics.RocAucWithStandardError([0.1, 0.4, 0.9], [0, 0, 1]);
            result.Auc.Should().Be(1);
            double.IsNaN(result.StandardError).Should().BeTrue();
        }

        [Fact]
        public void RocCurve_GroupsTiesAndEndsAtOneOne()
        {
            var curve = BinaryMetrics.RocCurve(s, y);
            curve[0].Should().Be(new RocPoint(double.PositiveInfinity, 0, 0));
            curve.Length.Should().Be(1 + s.Distinct().Count());
            curve[^1].FalsePositiveRate.Should().Be(1);
            curve[^1].TruePositiveRate.Should().Be(1);
            curve.Select(p => p.FalsePositiveRate).Should().BeInAscendingOrder();
            curve.Select(p => p.TruePositiveRate).Should().BeInAscendingOrder();

            var trapezoid = 0d;

            for (var i = 1; i < curve.Length; i++)
            {
                trapezoid += (curve[i].FalsePositiveRate - curve[i - 1].FalsePositiveRate) *
                    (curve[i].TruePositiveRate + curve[i - 1].TruePositiveRate) / 2;
            }

            trapezoid.Should().BeApproximately(0.72, 1e-12);
        }

        [Fact]
        public void RocCurve_SingleClassThrows()
        {
            Action act = () => BinaryMetrics.RocCurve([0.1, 0.2], [1, 1]);
            act.Should().Throw<InvalidOperationException>();
            act = () => BinaryMetrics.PrecisionRecallCurve([0.1, 0.2], [0, 0]);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void KolmogorovSmirnov_KnownAnswer()
        {
            BinaryMetrics.KolmogorovSmirnov(s, y).Should().BeApproximately(0.39999999999999997, 1e-12);
            BinaryMetrics.KolmogorovSmirnov([0.1, 0.2, 0.8, 0.9], [0, 0, 1, 1]).Should().Be(1);
        }

        [Fact]
        public void AveragePrecision_KnownAnswer_AndPerfectRanking()
        {
            BinaryMetrics.AveragePrecision(s, y).Should().BeApproximately(0.7087301587301588, 1e-12);
            BinaryMetrics.AveragePrecision([0.1, 0.2, 0.8, 0.9], [0, 0, 1, 1]).Should().BeApproximately(1, 1e-12);
            var curve = BinaryMetrics.PrecisionRecallCurve(s, y);
            curve[^1].Recall.Should().Be(1);
            curve[^1].Precision.Should().BeApproximately(0.5, 1e-12);
        }

        [Fact]
        public void LogLossAndBrier_KnownAnswers()
        {
            BinaryMetrics.LogLoss(s, y).Should().BeApproximately(0.6052717846500758, 1e-12);
            BinaryMetrics.BrierScore(s, y).Should().BeApproximately(0.21200000000000002, 1e-12);
        }

        [Fact]
        public void LogLossAndBrier_WeightsMatchIndependentValuesAndDuplicatedRows()
        {
            double[] w = [1, 2, 1, 1, 1, 1, 1, 1, 1, 3];
            BinaryMetrics.LogLoss(s, y, w).Should().BeApproximately(0.49896787916389385, 1e-12);
            BinaryMetrics.BrierScore(s, y, w).Should().BeApproximately(0.1676923076923077, 1e-12);

            var dupS = s.Append(s[1]).ToArray();
            var dupY = y.Append(y[1]).ToArray();
            var weights = new double[] { 1, 2, 1, 1, 1, 1, 1, 1, 1, 1 };
            BinaryMetrics.LogLoss(dupS, dupY).Should().BeApproximately(BinaryMetrics.LogLoss(s, y, weights), 1e-12);
            BinaryMetrics.BrierScore(dupS, dupY).Should()
                .BeApproximately(BinaryMetrics.BrierScore(s, y, weights), 1e-12);
        }

        [Fact]
        public void LogLoss_ExtremeProbabilitiesAreClippedNotInfinite()
        {
            var value = BinaryMetrics.LogLoss([0d, 1d], [1, 0]);
            double.IsFinite(value).Should().BeTrue();
            value.Should().BeApproximately(-Math.Log(1e-15), 1e-3);
            BinaryMetrics.LogLoss([1d, 0d], [1, 0]).Should().BeApproximately(0, 1e-12);
            BinaryMetrics.BrierScore([1d, 0d], [0, 1]).Should().Be(1);
        }

        [Fact]
        public void Reliability_BinsAndExpectedCalibrationError_KnownAnswers()
        {
            BinaryMetrics.ExpectedCalibrationError(s, y, 5).Should().BeApproximately(0.10000000000000003, 1e-12);
            BinaryMetrics.ExpectedCalibrationError(s, y).Should().BeApproximately(0.18000000000000005, 1e-12);

            var bins = BinaryMetrics.Reliability(s, y, 5);
            bins.Length.Should().Be(5);
            bins.Sum(b => b.Count).Should().Be(10);
            bins[0].Lower.Should().Be(0);
            bins[4].Upper.Should().Be(1);
        }

        [Fact]
        public void Reliability_EmptyBinsAreNaN_AndProbabilityOneLandsInLastBin()
        {
            var bins = BinaryMetrics.Reliability([1d, 0.05], [1, 0], 4);
            bins[3].Count.Should().Be(1);
            bins[3].ObservedRate.Should().Be(1);
            bins[1].Count.Should().Be(0);
            double.IsNaN(bins[1].MeanPredicted).Should().BeTrue();
        }

        [Fact]
        public void PerfectlyCalibratedSet_HasZeroCalibrationError()
        {
            double[] p = [0.25, 0.25, 0.25, 0.25, 0.75, 0.75, 0.75, 0.75];
            int[] labels = [1, 0, 0, 0, 1, 1, 1, 0];
            BinaryMetrics.ExpectedCalibrationError(p, labels).Should().BeApproximately(0, 1e-12);
        }

        [Fact]
        public void InvalidInputs_ThrowStandardExceptions()
        {
            Action act = () => BinaryMetrics.RocAuc([], []);
            act.Should().Throw<ArgumentException>();
            act = () => BinaryMetrics.RocAuc([0.1, 0.2], [1]);
            act.Should().Throw<ArgumentException>();
            act = () => BinaryMetrics.RocAuc(null!, [1]);
            act.Should().Throw<ArgumentNullException>();
            act = () => BinaryMetrics.RocAuc([0.1], null!);
            act.Should().Throw<ArgumentNullException>();
            act = () => BinaryMetrics.RocAuc([double.NaN], [1]);
            act.Should().Throw<ArgumentException>();
            act = () => BinaryMetrics.Confusion([0.1], [3], 0.5);
            act.Should().Throw<ArgumentException>();
            act = () => BinaryMetrics.Confusion([0.1], [1], double.NaN);
            act.Should().Throw<ArgumentOutOfRangeException>();
            act = () => BinaryMetrics.LogLoss([1.5], [1]);
            act.Should().Throw<ArgumentException>();
            act = () => BinaryMetrics.BrierScore([-0.1], [1]);
            act.Should().Throw<ArgumentException>();
            act = () => BinaryMetrics.Reliability([0.1], [1], 0);
            act.Should().Throw<ArgumentOutOfRangeException>();
            act = () => BinaryMetrics.LogLoss([0.1], [1], epsilon: 0.9);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void NaNScoresAreDroppedFromEveryMetric()
        {
            var scores = s.Append(double.NaN).ToArray();
            var labels = y.Append(0).ToArray();
            BinaryMetrics.Confusion(scores, labels, 0.5).Total.Should().Be(10);
            BinaryMetrics.AveragePrecision(scores, labels).Should().BeApproximately(0.7087301587301588, 1e-12);
            BinaryMetrics.LogLoss(scores, labels).Should().BeApproximately(0.6052717846500758, 1e-12);
        }

        [Fact]
        public void LargeRandomSet_RankAucMatchesPairwiseDefinition()
        {
            var rng = new Lcg(99);
            var scores = new double[300];
            var labels = new int[300];

            for (var i = 0; i < 300; i++)
            {
                labels[i] = rng.Next() < 0.3 ? 1 : 0;
                scores[i] = Math.Round(rng.Next() * 10 + labels[i], 1);
            }

            double total = 0;
            var pos = Enumerable.Range(0, 300).Where(i => labels[i] == 1).ToArray();
            var neg = Enumerable.Range(0, 300).Where(i => labels[i] == 0).ToArray();

            foreach (var p in pos)
            {
                foreach (var n in neg)
                {
                    total += scores[p] > scores[n] ? 1 : scores[p] == scores[n] ? 0.5 : 0;
                }
            }

            BinaryMetrics.RocAuc(scores, labels).Should().BeApproximately(total / (pos.Length * neg.Length), 1e-12);
        }
    }
}
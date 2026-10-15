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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Analytics;
using Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class CategoricalAndSurvivalClassifierToolsTests
    {
        private const double Tight = 1e-9;
        private static readonly double[] scores = [0.9, 0.8, 0.7, 0.6, 0.55, 0.5, 0.4, 0.3, 0.2, 0.1];
        private static readonly int[] labels = [1, 1, 0, 1, 0, 1, 0, 0, 1, 0];

        private static readonly double[][] wikipediaCatsDogsRabbits =
            [[5d, 3d, 0d], [2d, 3d, 1d], [0d, 2d, 11d]];

        private readonly ProbabilityCalibrationTools calibration = new();
        private readonly ConfusionMatrixTools confusion = new();
        private readonly ScoreRankingTools ranking = new();

        private static (string[] Actual, string[] Predicted) WikipediaLabels()
        {
            var names = new[] { "cat", "dog", "rabbit" };
            var actual = new List<string>();
            var predicted = new List<string>();

            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    for (var k = 0; k < (int)wikipediaCatsDogsRabbits[i][j]; k++)
                    {
                        actual.Add(names[i]);
                        predicted.Add(names[j]);
                    }
                }
            }

            return ([.. actual], [.. predicted]);
        }

        [Fact]
        [Trait("Covers", "CalculateBinaryClassificationMetricsFromCounts")]
        public void CalculateBinaryClassificationMetricsFromCounts_MatchesHandComputation()
        {
            var result = confusion.CalculateBinaryClassificationMetricsFromCounts(3, 1, 4, 2, 1d, 0.95, 10d, 1d);

            result.Accuracy.Should().BeApproximately(0.7, Tight);
            result.Precision.Should().BeApproximately(0.75, Tight);
            result.Recall.Should().BeApproximately(0.6, Tight);
            result.Specificity.Should().BeApproximately(0.8, Tight);
            result.NegativePredictiveValue.Should().BeApproximately(4d / 6d, Tight);
            result.F1.Should().BeApproximately(2d / 3d, 1e-9);
            result.MatthewsCorrelation.Should().BeApproximately(0.408248290463863, 1e-12);
            result.CohenKappa.Should().BeApproximately(0.4, 1e-9);
            result.BalancedAccuracy.Should().BeApproximately(0.7, Tight);
            result.YoudenJ.Should().BeApproximately(0.4, Tight);
            result.GeometricMean.Should().BeApproximately(Math.Sqrt(0.48), Tight);
            result.PositiveLikelihoodRatio.Should().BeApproximately(3d, Tight);
            result.NegativeLikelihoodRatio.Should().BeApproximately(0.5, Tight);
            result.DiagnosticOddsRatio.Should().BeApproximately(6d, Tight);
            result.FalseDiscoveryRate.Should().BeApproximately(0.25, Tight);
            result.FalseOmissionRate.Should().BeApproximately(1d / 3d, Tight);
            result.Markedness.Should().BeApproximately(0.75 + 4d / 6d - 1d, Tight);
            result.PredictedPositiveRate.Should().BeApproximately(0.4, Tight);
            result.Lift.Should().BeApproximately(1.5, Tight);
            result.ExpectedCostPerCase.Should().BeApproximately(2.1, Tight);
            result.PrecisionLower.Should().BeLessThan(0.75);
            result.PrecisionUpper.Should().BeGreaterThan(0.75);
        }

        [Fact]
        [Trait("Covers", "CalculateBinaryClassificationMetricsFromCounts")]
        public void CalculateBinaryClassificationMetricsFromCounts_FBetaFollowsBeta()
        {
            var f2 = confusion.CalculateBinaryClassificationMetricsFromCounts(3, 1, 4, 2, 2d);
            var half = confusion.CalculateBinaryClassificationMetricsFromCounts(3, 1, 4, 2, 0.5);

            f2.FBeta.Should().BeApproximately(0.625, Tight);
            half.FBeta.Should().BeApproximately(0.7142857143, 1e-9);
            double.IsNaN(f2.ExpectedCostPerCase).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "CalculateBinaryClassificationMetricsFromCounts")]
        public void CalculateBinaryClassificationMetricsFromCounts_DegenerateAndInvalidInputs()
        {
            var noPositives = confusion.CalculateBinaryClassificationMetricsFromCounts(0, 0, 5, 0);
            Action zero = () => confusion.CalculateBinaryClassificationMetricsFromCounts(0, 0, 0, 0);
            Action negative = () => confusion.CalculateBinaryClassificationMetricsFromCounts(-1, 0, 5, 0);
            Action beta = () => confusion.CalculateBinaryClassificationMetricsFromCounts(1, 1, 1, 1, 0d);
            Action oneCost = () => confusion.CalculateBinaryClassificationMetricsFromCounts(1, 1, 1, 1, 1d, 0.95, 5d);

            noPositives.Accuracy.Should().Be(1d);
            double.IsNaN(noPositives.FalseNegativeRate).Should().BeTrue();
            double.IsNaN(noPositives.PositiveLikelihoodRatio).Should().BeTrue();
            zero.Should().Throw<ArgumentException>();
            negative.Should().Throw<ArgumentException>();
            beta.Should().Throw<ArgumentException>().WithParameterName("beta");
            oneCost.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "CalculateBinaryClassificationMetricsFromLabels")]
        public void CalculateBinaryClassificationMetricsFromLabels_CountsTheConfusionMatrix()
        {
            var actual = new[] { 1, 1, 1, 0, 0, 0, 0, 0, 1, 1 };
            var predicted = new[] { 1, 1, 1, 1, 0, 0, 0, 0, 0, 0 };

            var result = confusion.CalculateBinaryClassificationMetricsFromLabels(actual, predicted);

            (result.TruePositives, result.FalsePositives, result.TrueNegatives, result.FalseNegatives)
                .Should().Be((3, 1, 4, 2));

            result.Recall.Should().BeApproximately(0.6, Tight);
        }

        [Fact]
        [Trait("Covers", "CalculateBinaryClassificationMetricsFromLabels")]
        public void CalculateBinaryClassificationMetricsFromLabels_RejectsBadFlags()
        {
            Action nonBinary = () =>
                confusion.CalculateBinaryClassificationMetricsFromLabels([1, 2], [1, 0]);

            Action mismatch = () =>
                confusion.CalculateBinaryClassificationMetricsFromLabels([1], [1, 0]);

            Action empty = () =>
                confusion.CalculateBinaryClassificationMetricsFromLabels([], []);

            nonBinary.Should().Throw<ArgumentException>().WithParameterName("actual");
            mismatch.Should().Throw<ArgumentException>();
            empty.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "CalculateBinaryClassificationMetricsAtThreshold")]
        public void CalculateBinaryClassificationMetricsAtThreshold_ThresholdIsInclusiveAndNaNIsDropped()
        {
            var withNaNScore = scores.Concat([double.NaN]).ToArray();
            var withExtraLabel = labels.Concat([1]).ToArray();

            var result = confusion.CalculateBinaryClassificationMetricsAtThreshold(withNaNScore, withExtraLabel, 0.5);

            (result.TruePositives, result.FalsePositives, result.FalseNegatives, result.TrueNegatives)
                .Should().Be((4, 2, 1, 3));

            result.F1.Should().BeApproximately(8d / 11d, Tight);
            result.Threshold.Should().Be(0.5);
        }

        [Fact]
        [Trait("Covers", "CalculateBinaryClassificationMetricsAtThreshold")]
        public void CalculateBinaryClassificationMetricsAtThreshold_RejectsNaNThresholdAndAllNaNScores()
        {
            Action nan = () => confusion.CalculateBinaryClassificationMetricsAtThreshold(scores, labels, double.NaN);

            Action allNaN = () =>
                confusion.CalculateBinaryClassificationMetricsAtThreshold([double.NaN], [1], 0.5);

            nan.Should().Throw<ArgumentException>().WithParameterName("threshold");
            allNaN.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "CalculateMulticlassClassificationMetrics")]
        public void CalculateMulticlassClassificationMetrics_MatchesWikipediaCatsDogsRabbits()
        {
            var (actual, predicted) = WikipediaLabels();

            var result = confusion.CalculateMulticlassClassificationMetrics(actual, predicted);

            result.Classes.Should().Equal("cat", "dog", "rabbit");
            result.Total.Should().Be(27);
            result.Accuracy.Should().BeApproximately(19d / 27d, Tight);
            result.PerClass[0].Precision.Should().BeApproximately(5d / 7d, Tight);
            result.PerClass[0].Recall.Should().BeApproximately(5d / 8d, Tight);
            result.PerClass[1].Precision.Should().BeApproximately(3d / 8d, Tight);
            result.PerClass[2].Recall.Should().BeApproximately(11d / 13d, Tight);
            result.PerClass[2].Support.Should().Be(13);
            result.Macro.Precision.Should().BeApproximately((5d / 7d + 3d / 8d + 11d / 12d) / 3d, Tight);
            result.Micro.Precision.Should().BeApproximately(19d / 27d, Tight);
            result.Weighted.Recall.Should().BeApproximately(19d / 27d, Tight);
            result.BalancedAccuracy.Should().BeApproximately((5d / 8d + 0.5 + 11d / 13d) / 3d, Tight);
            result.CohenKappa.Should().BeApproximately((19d / 27d - 260d / 729d) / (1d - 260d / 729d), 1e-9);
            result.TopConfusions[0].Count.Should().Be(3);
            double.IsFinite(result.CohenKappaStandardError).Should().BeTrue();
            result.MatthewsCorrelation.Should().BeInRange(0.4, 0.7);
        }

        [Fact]
        [Trait("Covers", "CalculateMulticlassClassificationMetrics")]
        public void CalculateMulticlassClassificationMetrics_WeightedKappaClassOrderAndErrors()
        {
            var (actual, predicted) = WikipediaLabels();

            var quadratic = confusion.CalculateMulticlassClassificationMetrics(actual, predicted, 1d, 0.95,
                KappaWeighting.Quadratic, ["cat", "dog", "rabbit"]);

            var plain = confusion.CalculateMulticlassClassificationMetrics(actual, predicted);

            Action missing = () => confusion.CalculateMulticlassClassificationMetrics(actual, predicted, 1d, 0.95,
                KappaWeighting.None, ["cat", "dog"]);

            Action mismatch = () =>
                confusion.CalculateMulticlassClassificationMetrics(["a"], ["a", "b"]);

            quadratic.KappaWeightingUsed.Should().Be("Quadratic");
            quadratic.CohenKappa.Should().NotBeApproximately(plain.CohenKappa, 1e-6);
            missing.Should().Throw<ArgumentException>().WithParameterName("classOrder");
            mismatch.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "CalculateMulticlassMetricsFromConfusionMatrix")]
        public void CalculateMulticlassMetricsFromConfusionMatrix_MatchesLabelsVersion()
        {
            var result = confusion.CalculateMulticlassMetricsFromConfusionMatrix(wikipediaCatsDogsRabbits,
                ["cat", "dog", "rabbit"]);

            result.Accuracy.Should().BeApproximately(19d / 27d, Tight);
            result.PerClass[1].Recall.Should().BeApproximately(0.5, Tight);
            result.PerClass[1].Specificity.Should().BeApproximately(1d - 5d / 21d, Tight);
            result.CohenKappa.Should().BeApproximately((19d / 27d - 260d / 729d) / (1d - 260d / 729d), 1e-9);
        }

        [Fact]
        [Trait("Covers", "CalculateMulticlassMetricsFromConfusionMatrix")]
        public void CalculateMulticlassMetricsFromConfusionMatrix_RejectsNonSquareAndFractionalCounts()
        {
            Action nonSquare = () =>
                confusion.CalculateMulticlassMetricsFromConfusionMatrix([[1d, 2d, 3d], [1d, 2d, 3d]]);

            Action fractional = () =>
                confusion.CalculateMulticlassMetricsFromConfusionMatrix([[1.5, 2d], [1d, 2d]]);
            Action empty = () =>
                confusion.CalculateMulticlassMetricsFromConfusionMatrix([[0d, 0d], [0d, 0d]]);

            nonSquare.Should().Throw<ArgumentException>().WithMessage("*square*");
            fractional.Should().Throw<ArgumentException>();
            empty.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ComputeRocCurve")]
        public void ComputeRocCurve_AucIsRankProbabilityByHand()
        {
            var result = ranking.ComputeRocCurve(scores, labels);

            result.Auc.Should().BeApproximately(18d / 25d, Tight);
            result.Gini.Should().BeApproximately(0.44, Tight);
            result.TotalPoints.Should().Be(11);
            double.IsNaN(result.Points[0].Threshold).Should().BeTrue();
            result.Points[^1].TruePositiveRate.Should().Be(1d);
            result.Points[^1].FalsePositiveRate.Should().Be(1d);
            result.YoudenJ.Should().BeApproximately(0.4, Tight);
            result.YoudenThreshold.Should().BeApproximately(0.8, Tight);
            result.KolmogorovSmirnov.Should().BeApproximately(0.4, Tight);
            result.AucLower.Should().BeLessThan(0.72);
            result.AucUpper.Should().BeGreaterThan(0.72);
            (result.Positives, result.Negatives).Should().Be((5, 5));
        }

        [Fact]
        [Trait("Covers", "ComputeRocCurve")]
        public void ComputeRocCurve_ThinsPointsDropsNaNAndRejectsSingleClass()
        {
            var withNaN = scores.Concat([double.NaN]).ToArray();
            var withExtraLabel = labels.Concat([1]).ToArray();

            var thinned = ranking.ComputeRocCurve(withNaN, withExtraLabel, 0.95, 4);
            Action single = () => ranking.ComputeRocCurve([0.1, 0.2], [1, 1]);
            Action negativeMax = () => ranking.ComputeRocCurve(scores, labels, 0.95, -1);

            thinned.Points.Should().HaveCount(4);
            thinned.TotalPoints.Should().Be(11);
            thinned.SkippedRows.Should().Be(1);
            thinned.Auc.Should().BeApproximately(0.72, Tight);
            single.Should().Throw<ArgumentException>().WithMessage("*one class*");
            negativeMax.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ComputeRocCurve")]
        public void ComputeRocCurve_TiedScoresGiveHalfCredit()
        {
            var result = ranking.ComputeRocCurve([0.5, 0.5, 0.5, 0.5], [1, 1, 0, 0]);

            result.Auc.Should().BeApproximately(0.5, Tight);
        }

        [Fact]
        [Trait("Covers", "ComparePairedRocAucDelong")]
        public void ComparePairedRocAucDelong_InvertedScoreHasMirroredAucAndDoubleStandardError()
        {
            var inverted = scores.Select(s => 1d - s).ToArray();
            var single = ranking.ComputeRocCurve(scores, labels);

            var result = ranking.ComparePairedRocAucDelong(scores, inverted, labels);

            result.AucFirst.Should().BeApproximately(0.72, Tight);
            result.AucSecond.Should().BeApproximately(0.28, Tight);
            result.Difference.Should().BeApproximately(0.44, Tight);
            result.StandardError.Should().BeApproximately(2d * single.AucStandardError, 1e-9);
            result.CorrelationOfScores.Should().BeApproximately(-1d, 1e-9);
            result.ZStatistic.Should().BeApproximately(result.Difference / result.StandardError, 1e-9);
        }

        [Fact]
        [Trait("Covers", "ComparePairedRocAucDelong")]
        public void ComparePairedRocAucDelong_IdenticalScoresHaveNoDifference()
        {
            var result = ranking.ComparePairedRocAucDelong(scores, scores, labels);
            Action mismatch = () => ranking.ComparePairedRocAucDelong(scores, [0.1], labels);
            Action allNaN = () => ranking.ComparePairedRocAucDelong([double.NaN], [0.1], [1]);

            result.Difference.Should().Be(0d);
            result.StandardError.Should().BeApproximately(0d, 1e-12);
            mismatch.Should().Throw<ArgumentException>();
            allNaN.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ComputePrecisionRecallCurve")]
        public void ComputePrecisionRecallCurve_AveragePrecisionMatchesHandComputation()
        {
            var result = ranking.ComputePrecisionRecallCurve(scores, labels);

            result.AveragePrecision.Should().BeApproximately(0.2 * (1d + 1d + 0.75 + 4d / 6d + 5d / 9d), Tight);
            result.Prevalence.Should().BeApproximately(0.5, Tight);
            result.BestF1.Should().BeApproximately(8d / 11d, Tight);
            result.BestF1Threshold.Should().BeApproximately(0.5, Tight);
            result.Points.Should().HaveCount(10);
            result.Points[0].Precision.Should().Be(1d);
            result.Points[^1].Recall.Should().Be(1d);
        }

        [Fact]
        [Trait("Covers", "ComputePrecisionRecallCurve")]
        public void ComputePrecisionRecallCurve_RejectsNoPositives()
        {
            Action act = () => ranking.ComputePrecisionRecallCurve([0.1, 0.2], [0, 0]);

            act.Should().Throw<ArgumentException>().WithMessage("*no events*");
            ranking.ComputePrecisionRecallCurve(scores, labels, 3).Points.Should().HaveCount(3);
        }

        [Fact]
        [Trait("Covers", "ComputeLiftGainTable")]
        public void ComputeLiftGainTable_MatchesHandComputedQuintiles()
        {
            var result = ranking.ComputeLiftGainTable(scores, labels, 5);

            result.Groups.Select(g => g.Events).Should().Equal(2, 1, 1, 0, 1);
            result.Groups.Select(g => g.CumulativeGain).Should().Equal(0.4, 0.6, 0.8, 0.8, 1.0);
            result.Groups[0].Lift.Should().BeApproximately(2d, Tight);
            result.Groups[0].ScoreMax.Should().Be(0.9);
            result.Groups[0].ScoreMin.Should().Be(0.8);
            result.Groups[1].CumulativeLift.Should().BeApproximately(1.5, Tight);
            result.Groups[2].CumulativeEventRate.Should().BeApproximately(4d / 6d, Tight);
            result.MaximumKolmogorovSmirnov.Should().BeApproximately(0.4, Tight);
            result.MaximumKolmogorovSmirnovGroup.Should().Be(1);
            result.Groups.Sum(g => g.Count).Should().Be(10);
        }

        [Fact]
        [Trait("Covers", "ComputeLiftGainTable")]
        public void ComputeLiftGainTable_UnevenGroupSizesAndInvalidGroupCounts()
        {
            var result = ranking.ComputeLiftGainTable(scores, labels, 4);
            Action tooMany = () => ranking.ComputeLiftGainTable(scores, labels, 11);
            Action one = () => ranking.ComputeLiftGainTable(scores, labels, 1);

            result.Groups.Select(g => g.Count).Should().Equal(3, 3, 2, 2);
            tooMany.Should().Throw<ArgumentException>().WithParameterName("groups");
            one.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ComputeKolmogorovSmirnovSeparation")]
        public void ComputeKolmogorovSmirnovSeparation_MatchesHandComputation()
        {
            var result = ranking.ComputeKolmogorovSmirnovSeparation(scores, labels);

            result.KolmogorovSmirnov.Should().BeApproximately(0.4, Tight);
            result.Threshold.Should().BeApproximately(0.8, Tight);
            result.SensitivityAtThreshold.Should().BeApproximately(0.4, Tight);
            result.FalsePositiveRateAtThreshold.Should().BeApproximately(0d, Tight);
            result.Auc.Should().BeApproximately(0.72, Tight);
            result.MeanScoreEvents.Should().BeApproximately(0.6, Tight);
            result.MeanScoreNonEvents.Should().BeApproximately(0.41, Tight);
            result.MedianScoreEvents.Should().BeApproximately(0.6, Tight);
            result.MedianScoreNonEvents.Should().BeApproximately(0.4, Tight);
        }

        [Fact]
        [Trait("Covers", "ComputeKolmogorovSmirnovSeparation")]
        public void ComputeKolmogorovSmirnovSeparation_PerfectSeparationIsOneAndSingleClassIsRejected()
        {
            var perfect =
                ranking.ComputeKolmogorovSmirnovSeparation([0.9, 0.8, 0.2, 0.1], [1, 1, 0, 0]);

            Action single = () => ranking.ComputeKolmogorovSmirnovSeparation([0.9, 0.8], [1, 1]);

            perfect.KolmogorovSmirnov.Should().BeApproximately(1d, Tight);
            single.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ComputeThresholdSweep")]
        public void ComputeThresholdSweep_ExplicitThresholdsMatchHandCounts()
        {
            var result = ranking.ComputeThresholdSweep(scores, labels, [0.5, 0.75], 20, 1d, 10d, 1d);

            result.Rows.Select(r => r.Threshold).Should().Equal(0.75, 0.5);
            result.Rows[0].AlertRate.Should().BeApproximately(0.2, Tight);
            result.Rows[0].Precision.Should().Be(1d);
            result.Rows[0].Recall.Should().BeApproximately(0.4, Tight);
            result.Rows[1].TruePositives.Should().Be(4);
            result.Rows[1].FalsePositives.Should().Be(2);
            result.Rows[1].F1.Should().BeApproximately(8d / 11d, Tight);
            result.Rows[1].ExpectedCostPerCase.Should().BeApproximately((10d * 1 + 2d) / 10d, Tight);
        }

        [Fact]
        [Trait("Covers", "ComputeThresholdSweep")]
        public void ComputeThresholdSweep_AutomaticThresholdsAreCappedAndThresholdAboveMaxFlagsNothing()
        {
            var automatic = ranking.ComputeThresholdSweep(scores, labels, null, 4);
            var above = ranking.ComputeThresholdSweep(scores, labels, [5d]);
            Action nan = () => ranking.ComputeThresholdSweep(scores, labels, [double.NaN]);
            Action range = () => ranking.ComputeThresholdSweep(scores, labels, null, 1);

            automatic.Rows.Should().HaveCount(4);
            automatic.Rows[0].Threshold.Should().Be(0.9);
            automatic.Rows[^1].Threshold.Should().Be(0.1);
            above.Rows[0].AlertRate.Should().Be(0d);
            above.Rows[0].Recall.Should().Be(0d);
            nan.Should().Throw<ArgumentException>();
            range.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "SelectThresholdMaximisingFBeta")]
        public void SelectThresholdMaximisingFBeta_PicksHandComputedOptimum()
        {
            var f1 = ranking.SelectThresholdMaximisingFBeta(scores, labels);
            var f2 = ranking.SelectThresholdMaximisingFBeta(scores, labels, 2d);

            f1.Threshold.Should().BeApproximately(0.5, Tight);
            f1.ObjectiveValue.Should().BeApproximately(8d / 11d, Tight);
            f1.Metrics.TruePositives.Should().Be(4);
            f2.Threshold.Should().BeApproximately(0.2, Tight);
            f2.ObjectiveValue.Should().BeApproximately(25d / 29d, Tight);
        }

        [Fact]
        [Trait("Covers", "SelectThresholdMaximisingFBeta")]
        public void SelectThresholdMaximisingFBeta_RejectsBadBetaAndSingleClass()
        {
            Action beta = () => ranking.SelectThresholdMaximisingFBeta(scores, labels, -1d);
            Action single = () => ranking.SelectThresholdMaximisingFBeta([0.1], [1]);

            beta.Should().Throw<ArgumentException>().WithParameterName("beta");
            single.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "SelectThresholdMaximisingYoudenIndex")]
        public void SelectThresholdMaximisingYoudenIndex_TiesChooseTheHighestThreshold()
        {
            var result = ranking.SelectThresholdMaximisingYoudenIndex(scores, labels);

            result.ObjectiveValue.Should().BeApproximately(0.4, Tight);
            result.Threshold.Should().BeApproximately(0.8, Tight);
            result.Metrics.YoudenJ.Should().BeApproximately(0.4, Tight);
        }

        [Fact]
        [Trait("Covers", "SelectThresholdMaximisingYoudenIndex")]
        public void SelectThresholdMaximisingYoudenIndex_PerfectSeparationGivesOne()
        {
            var result =
                ranking.SelectThresholdMaximisingYoudenIndex([0.9, 0.8, 0.2, 0.1], [1, 1, 0, 0]);

            result.ObjectiveValue.Should().BeApproximately(1d, Tight);
            result.Threshold.Should().BeApproximately(0.8, Tight);
        }

        [Fact]
        [Trait("Covers", "SelectThresholdMaximisingMatthewsCorrelation")]
        public void SelectThresholdMaximisingMatthewsCorrelation_PicksHandComputedOptimum()
        {
            var result = ranking.SelectThresholdMaximisingMatthewsCorrelation(scores, labels);

            result.ObjectiveValue.Should().BeApproximately(0.5, Tight);
            result.Threshold.Should().BeApproximately(0.8, Tight);
        }

        [Fact]
        [Trait("Covers", "SelectThresholdMaximisingMatthewsCorrelation")]
        public void SelectThresholdMaximisingMatthewsCorrelation_InvertedScoreStillReturnsBestAvailable()
        {
            var result =
                ranking.SelectThresholdMaximisingMatthewsCorrelation([0.1, 0.2, 0.8, 0.9],
                    [1, 1, 0, 0]);

            result.ObjectiveValue.Should().BeLessThan(0.2);
        }

        [Fact]
        [Trait("Covers", "SelectThresholdMinimisingCost")]
        public void SelectThresholdMinimisingCost_MatchesHandComputedMinimum()
        {
            var result = ranking.SelectThresholdMinimisingCost(scores, labels, 10d, 1d);

            result.Threshold.Should().BeApproximately(0.2, Tight);
            result.ObjectiveValue.Should().BeApproximately(0.4, Tight);
            result.BayesOptimalThreshold.Should().BeApproximately(1d / 11d, Tight);
            result.ExpectedCostFlagNone.Should().BeApproximately(5d, Tight);
            result.ExpectedCostFlagAll.Should().BeApproximately(0.5, Tight);
            result.Metrics.ExpectedCostPerCase.Should().BeApproximately(0.4, Tight);
        }

        [Fact]
        [Trait("Covers", "SelectThresholdMinimisingCost")]
        public void SelectThresholdMinimisingCost_FlaggingNothingCanBeCheapestAndBadCostsAreRejected()
        {
            var none = ranking.SelectThresholdMinimisingCost([0.9, 0.8], [0, 1], 1d, 100d);
            Action zero = () => ranking.SelectThresholdMinimisingCost(scores, labels, 0d, 1d);

            none.Note.Should().Contain("no cases");
            none.Threshold.Should().BeGreaterThan(0.9);
            none.Metrics.TruePositives.Should().Be(0);
            none.ObjectiveValue.Should().BeApproximately(0.5, Tight);
            zero.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "SelectThresholdForTargetRecall")]
        public void SelectThresholdForTargetRecall_ReturnsFewestAlertsReachingTarget()
        {
            var result = ranking.SelectThresholdForTargetRecall(scores, labels, 0.8);

            result.Threshold.Should().BeApproximately(0.5, Tight);
            result.ObjectiveValue.Should().BeApproximately(0.8, Tight);
            result.Metrics.PredictedPositiveRate.Should().BeApproximately(0.6, Tight);
            result.Achievable.Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "SelectThresholdForTargetRecall")]
        public void SelectThresholdForTargetRecall_FullRecallAndInvalidTargets()
        {
            var full = ranking.SelectThresholdForTargetRecall(scores, labels, 1d);
            Action zero = () => ranking.SelectThresholdForTargetRecall(scores, labels, 0d);
            Action high = () => ranking.SelectThresholdForTargetRecall(scores, labels, 1.5);

            full.Threshold.Should().BeApproximately(0.2, Tight);
            zero.Should().Throw<ArgumentException>();
            high.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "SelectThresholdForTargetPrecision")]
        public void SelectThresholdForTargetPrecision_ReturnsLowestThresholdKeepingPrecision()
        {
            var result = ranking.SelectThresholdForTargetPrecision(scores, labels, 0.75);

            result.Threshold.Should().BeApproximately(0.6, Tight);
            result.ObjectiveValue.Should().BeApproximately(0.75, Tight);
            result.Metrics.Recall.Should().BeApproximately(0.6, Tight);
            result.Achievable.Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "SelectThresholdForTargetPrecision")]
        public void SelectThresholdForTargetPrecision_UnreachableTargetIsFlagged()
        {
            var result = ranking.SelectThresholdForTargetPrecision([0.9, 0.1], [0, 1], 0.9);

            result.Achievable.Should().BeFalse();
            result.Note.Should().Contain("No threshold");
            result.ObjectiveValue.Should().BeApproximately(0.5, Tight);
        }

        [Fact]
        [Trait("Covers", "SelectThresholdForAlertBudget")]
        public void SelectThresholdForAlertBudget_UsesLargestQueueWithinBudget()
        {
            var fraction = ranking.SelectThresholdForAlertBudget(scores, labels, 0.3);
            var count = ranking.SelectThresholdForAlertBudget(scores, labels, null, 1);

            fraction.Threshold.Should().BeApproximately(0.7, Tight);
            fraction.Metrics.Precision.Should().BeApproximately(2d / 3d, Tight);
            fraction.Metrics.Recall.Should().BeApproximately(0.4, Tight);
            count.Threshold.Should().BeApproximately(0.9, Tight);
        }

        [Fact]
        [Trait("Covers", "SelectThresholdForAlertBudget")]
        public void SelectThresholdForAlertBudget_TiesOverBudgetAndInvalidArguments()
        {
            var tied = ranking.SelectThresholdForAlertBudget([0.5, 0.5, 0.5, 0.5], [1, 0, 1, 0], null,
                2);

            Action both = () => ranking.SelectThresholdForAlertBudget(scores, labels, 0.3, 3);
            Action neither = () => ranking.SelectThresholdForAlertBudget(scores, labels);
            Action fraction = () => ranking.SelectThresholdForAlertBudget(scores, labels, 1.5);

            tied.Achievable.Should().BeFalse();
            tied.Note.Should().Contain("tied");
            both.Should().Throw<ArgumentException>();
            neither.Should().Throw<ArgumentException>();
            fraction.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "EvaluateTopKAlertQueue")]
        public void EvaluateTopKAlertQueue_MatchesHandComputedQueues()
        {
            var result = ranking.EvaluateTopKAlertQueue(scores, labels, [5, 1, 3]);

            result.Rows.Select(r => r.QueueSize).Should().Equal(1, 3, 5);
            result.Rows.Select(r => r.TruePositives).Should().Equal(1, 2, 3);
            result.Rows[1].Precision.Should().BeApproximately(2d / 3d, Tight);
            result.Rows[1].Recall.Should().BeApproximately(0.4, Tight);
            result.Rows[1].Lift.Should().BeApproximately(4d / 3d, Tight);
            result.Rows[2].MinimumScoreInQueue.Should().Be(0.55);
            result.TotalEvents.Should().Be(5);
        }

        [Fact]
        [Trait("Covers", "EvaluateTopKAlertQueue")]
        public void EvaluateTopKAlertQueue_RejectsOversizedQueuesAndHandlesNoEvents()
        {
            Action big = () => ranking.EvaluateTopKAlertQueue(scores, labels, [11]);
            Action empty = () => ranking.EvaluateTopKAlertQueue(scores, labels, []);
            var noEvents = ranking.EvaluateTopKAlertQueue([0.1, 0.2], [0, 0], [1]);

            big.Should().Throw<ArgumentException>().WithParameterName("queueSizes");
            empty.Should().Throw<ArgumentException>();
            noEvents.Rows[0].Precision.Should().Be(0d);
            double.IsNaN(noEvents.Rows[0].Recall).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "CalculateOneVersusRestMulticlassAuc")]
        public void CalculateOneVersusRestMulticlassAuc_PerfectAndReversedModels()
        {
            var actual = new[] { "a", "b", "c", "a", "b", "c" };

            var perfect = new[]
            {
                new[] { 0.9, 0.05, 0.05 }, new[] { 0.05, 0.9, 0.05 }, new[] { 0.05, 0.05, 0.9 },
                new[] { 0.8, 0.1, 0.1 }, new[] { 0.1, 0.8, 0.1 }, new[] { 0.1, 0.1, 0.8 }
            };

            var reversed = perfect.Select(r => r.Select(v => 1d - v).ToArray()).ToArray();

            var good = ranking.CalculateOneVersusRestMulticlassAuc(perfect, actual, ["a", "b", "c"]);
            var bad = ranking.CalculateOneVersusRestMulticlassAuc(reversed, actual, ["a", "b", "c"]);

            good.AucPerClass.Should().Equal(1d, 1d, 1d);
            good.MacroAuc.Should().BeApproximately(1d, Tight);
            good.WeightedAuc.Should().BeApproximately(1d, Tight);
            good.SupportPerClass.Should().Equal(2, 2, 2);
            bad.MacroAuc.Should().BeApproximately(0d, Tight);
        }

        [Fact]
        [Trait("Covers", "CalculateOneVersusRestMulticlassAuc")]
        public void CalculateOneVersusRestMulticlassAuc_AbsentClassIsNaNAndUnknownLabelIsRejected()
        {
            var rows = new[] { new[] { 0.9, 0.1, 0d }, new[] { 0.2, 0.8, 0d } };

            var result =
                ranking.CalculateOneVersusRestMulticlassAuc(rows, ["a", "b"], ["a", "b", "c"]);

            Action unknown = () =>
                ranking.CalculateOneVersusRestMulticlassAuc(rows, ["a", "z"], ["a", "b", "c"]);

            Action shape = () =>
                ranking.CalculateOneVersusRestMulticlassAuc([[0.5]], ["a"],
                    ["a", "b"]);

            double.IsNaN(result.AucPerClass[2]).Should().BeTrue();
            result.MacroAuc.Should().BeApproximately(1d, Tight);
            unknown.Should().Throw<ArgumentException>().WithParameterName("actual");
            shape.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ComputeCalibrationCurve")]
        public void ComputeCalibrationCurve_EqualCountBinsMatchHandComputation()
        {
            var result = calibration.ComputeCalibrationCurve(scores, labels, 5, CalibrationBinning.EqualCount);

            result.Bins.Select(b => b.ObservedRate).Should().Equal(0.5, 0.0, 0.5, 0.5, 1.0);
            result.Bins[0].MeanPredicted.Should().BeApproximately(0.15, Tight);
            result.ExpectedCalibrationError.Should().BeApproximately(0.205, Tight);
            result.MaximumCalibrationError.Should().BeApproximately(0.35, Tight);
            result.BrierScore.Should().BeApproximately(0.21525, Tight);
            result.LogLoss.Should().BeApproximately(0.6117259, 1e-6);
            result.MeanPredicted.Should().BeApproximately(0.505, Tight);
            result.ObservedRate.Should().BeApproximately(0.5, Tight);
        }

        [Fact]
        [Trait("Covers", "ComputeCalibrationCurve")]
        public void ComputeCalibrationCurve_UniformBinsAgreeWithSharedExpectedCalibrationError()
        {
            var result = calibration.ComputeCalibrationCurve(scores, labels, 5);

            result.Bins.Sum(b => b.Count).Should().Be(10);

            result.ExpectedCalibrationError.Should()
                .BeApproximately(BinaryMetrics.ExpectedCalibrationError(scores, labels, 5), 1e-12);

            result.Bins.Should()
                .OnlyContain(b => b.ObservedLower <= b.ObservedRate && b.ObservedRate <= b.ObservedUpper);
        }

        [Fact]
        [Trait("Covers", "ComputeCalibrationCurve")]
        public void ComputeCalibrationCurve_RejectsProbabilitiesOutsideUnitInterval()
        {
            Action outside = () => calibration.ComputeCalibrationCurve([1.2, 0.1], [1, 0]);
            Action bins = () => calibration.ComputeCalibrationCurve(scores, labels, 0);

            var withNaN = calibration.ComputeCalibrationCurve([.. scores, double.NaN],
                [.. labels, 1], 5);

            outside.Should().Throw<ArgumentException>();
            bins.Should().Throw<ArgumentException>().WithParameterName("bins");
            withNaN.SkippedRows.Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "CalculateBrierScoreDecomposition")]
        public void CalculateBrierScoreDecomposition_SatisfiesMurphyIdentity()
        {
            var result = calibration.CalculateBrierScoreDecomposition(scores, labels);

            result.BrierScore.Should().BeApproximately(0.21525, Tight);
            result.Uncertainty.Should().BeApproximately(0.25, Tight);
            result.BaseRate.Should().BeApproximately(0.5, Tight);
            result.BrierSkillScore.Should().BeApproximately(1d - 0.21525 / 0.25, Tight);

            (result.Reliability - result.Resolution + result.Uncertainty + result.WithinBinTerm)
                .Should().BeApproximately(result.BrierScore, 1e-12);
        }

        [Fact]
        [Trait("Covers", "CalculateBrierScoreDecomposition")]
        public void CalculateBrierScoreDecomposition_SingleBinReliabilityIsSquaredMeanGap()
        {
            var result = calibration.CalculateBrierScoreDecomposition(scores, labels, 1);

            result.Reliability.Should().BeApproximately(0.005 * 0.005, Tight);
            result.Resolution.Should().BeApproximately(0d, Tight);

            (result.Reliability - result.Resolution + result.Uncertainty + result.WithinBinTerm)
                .Should().BeApproximately(result.BrierScore, 1e-12);
        }

        [Fact]
        [Trait("Covers", "CalculateProbabilityScoringRules")]
        public void CalculateProbabilityScoringRules_MatchesHandComputedBrierAndLogLoss()
        {
            var result = calibration.CalculateProbabilityScoringRules(scores, labels);

            result.BrierScore.Should().BeApproximately(0.21525, Tight);
            result.LogLoss.Should().BeApproximately(0.6117259, 1e-6);
            result.BrierScoreOfBaseRate.Should().BeApproximately(0.25, Tight);
            result.LogLossOfBaseRate.Should().BeApproximately(Math.Log(2d), 1e-9);
            result.LogLossSkillScore.Should().BeApproximately(1d - 0.6117259 / Math.Log(2d), 1e-6);
            result.CalibrationInTheLarge.Should().BeApproximately(-0.005, Tight);
        }

        [Fact]
        [Trait("Covers", "CalculateProbabilityScoringRules")]
        public void CalculateProbabilityScoringRules_ConfidentWrongPredictionIsClippedAndBadEpsilonRejected()
        {
            var result = calibration.CalculateProbabilityScoringRules([0d, 1d], [1, 0], 1e-6);
            Action epsilon = () => calibration.CalculateProbabilityScoringRules(scores, labels, 0.6);

            result.LogLoss.Should().BeApproximately(-Math.Log(1e-6), 1e-9);
            result.BrierScore.Should().BeApproximately(1d, Tight);
            epsilon.Should().Throw<ArgumentException>().WithParameterName("epsilon");
        }

        [Fact]
        [Trait("Covers", "TestHosmerLemeshowCalibration")]
        public void TestHosmerLemeshowCalibration_MatchesHandComputedStatistic()
        {
            var result = calibration.TestHosmerLemeshowCalibration(scores, labels, 5);

            result.ChiSquare.Should().BeApproximately(3.554247609975, 1e-9);
            result.DegreesOfFreedom.Should().Be(3);
            result.PValue.Should().BeApproximately(0.3137942431, 1e-6);
            result.Groups[1].ObservedEvents.Should().Be(0);
            result.Groups[1].ExpectedEvents.Should().BeApproximately(0.7, Tight);
            result.Interpretation.Should().Contain("no significant");
        }

        [Fact]
        [Trait("Covers", "TestHosmerLemeshowCalibration")]
        public void TestHosmerLemeshowCalibration_RejectsTooFewGroupsAndGrosslyMiscalibratedIsSignificant()
        {
            var probabilities = Enumerable.Repeat(0.1, 100).Concat(Enumerable.Repeat(0.9, 100)).ToArray();
            var perfectlySeparatedLabels = Enumerable.Repeat(1, 100).Concat(Enumerable.Repeat(0, 100)).ToArray();
            var bad = calibration.TestHosmerLemeshowCalibration(probabilities, perfectlySeparatedLabels, 4);
            Action two = () => calibration.TestHosmerLemeshowCalibration(scores, labels, 2);

            bad.Interpretation.Should().Contain("significant miscalibration");
            bad.PValue.Should().BeLessThan(1e-6);
            two.Should().Throw<ArgumentException>().WithParameterName("groups");
        }

        [Fact]
        [Trait("Covers", "FitCalibrationSlopeAndIntercept")]
        public void FitCalibrationSlopeAndIntercept_PerfectlyCalibratedProbabilitiesGiveSlopeNearOne()
        {
            var probabilities = Enumerable.Repeat(0.2, 100).Concat(Enumerable.Repeat(0.8, 100)).ToArray();

            var mixedLabels = Enumerable.Repeat(1, 20).Concat(Enumerable.Repeat(0, 80))
                .Concat(Enumerable.Repeat(1, 80)).Concat(Enumerable.Repeat(0, 20)).ToArray();

            var result = calibration.FitCalibrationSlopeAndIntercept(probabilities, mixedLabels);

            result.Slope.Should().BeInRange(0.95, 1.05);
            result.Intercept.Should().BeInRange(-0.05, 0.05);
            result.CalibrationInTheLarge.Should().BeApproximately(0d, Tight);
            result.Interpretation.Should().Contain("well calibrated");
        }

        [Fact]
        [Trait("Covers", "FitCalibrationSlopeAndIntercept")]
        public void FitCalibrationSlopeAndIntercept_OverConfidentProbabilitiesHaveSmallSlope()
        {
            var probabilities = Enumerable.Repeat(0.05, 100).Concat(Enumerable.Repeat(0.95, 100)).ToArray();

            var mixedLabels = Enumerable.Repeat(1, 20).Concat(Enumerable.Repeat(0, 80))
                .Concat(Enumerable.Repeat(1, 80)).Concat(Enumerable.Repeat(0, 20)).ToArray();

            var result = calibration.FitCalibrationSlopeAndIntercept(probabilities, mixedLabels);
            var constant = calibration.FitCalibrationSlopeAndIntercept([0.5, 0.5], [1, 0]);

            result.Slope.Should().BeApproximately(Math.Log(4d) / Math.Log(19d), 0.03);
            result.Interpretation.Should().Contain("over-confident");
            double.IsNaN(constant.Slope).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "CalculateMulticlassLogLoss")]
        public void CalculateMulticlassLogLoss_MatchesHandComputation()
        {
            var probabilities = new[] { new[] { 0.7, 0.2, 0.1 }, new[] { 0.1, 0.8, 0.1 } };
            var actual = new[] { "a", "b" };

            var result = calibration.CalculateMulticlassLogLoss(probabilities, actual, ["a", "b", "c"]);

            var unnormalised = calibration.CalculateMulticlassLogLoss(
                [[7d, 2d, 1d], [1d, 8d, 1d]],
                actual, ["a", "b", "c"]);

            result.LogLoss.Should().BeApproximately((-Math.Log(0.7) - Math.Log(0.8)) / 2d, Tight);
            result.BrierScore.Should().BeApproximately(0.10, Tight);
            result.Accuracy.Should().Be(1d);
            result.LogLossPerActualClass[0].Should().BeApproximately(-Math.Log(0.7), Tight);
            double.IsNaN(result.LogLossPerActualClass[2]).Should().BeTrue();
            unnormalised.LogLoss.Should().BeApproximately(result.LogLoss, Tight);
        }

        [Fact]
        [Trait("Covers", "CalculateMulticlassLogLoss")]
        public void CalculateMulticlassLogLoss_RejectsUnknownClassNegativeAndAllNaN()
        {
            Action unknown = () =>
                calibration.CalculateMulticlassLogLoss([[0.5, 0.5]], ["z"],
                    ["a", "b"]);

            Action negative = () =>
                calibration.CalculateMulticlassLogLoss([[-0.5, 1.5]], ["a"],
                    ["a", "b"]);

            Action allNaN = () =>
                calibration.CalculateMulticlassLogLoss([[double.NaN, 0.5]], ["a"],
                    ["a", "b"]);

            unknown.Should().Throw<ArgumentException>().WithParameterName("actual");
            negative.Should().Throw<ArgumentException>();
            allNaN.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "AdjustProbabilitiesForBaseRate")]
        public void AdjustProbabilitiesForBaseRate_ShiftsLogOddsByTheBaseRateDifference()
        {
            var same = calibration.AdjustProbabilitiesForBaseRate([0.3, 0.7], 0.5, 0.5);
            var shifted = calibration.AdjustProbabilitiesForBaseRate([0.5, double.NaN], 0.5, 0.01);

            same[0].Should().BeApproximately(0.3, 1e-12);
            same[1].Should().BeApproximately(0.7, 1e-12);
            shifted[0].Should().BeApproximately(0.01, 1e-12);
            double.IsNaN(shifted[1]).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "AdjustProbabilitiesForBaseRate")]
        public void AdjustProbabilitiesForBaseRate_RejectsInvalidRatesAndProbabilities()
        {
            Action rate = () => calibration.AdjustProbabilitiesForBaseRate([0.5], 0d, 0.1);
            Action target = () => calibration.AdjustProbabilitiesForBaseRate([0.5], 0.5, 1d);
            Action probability = () => calibration.AdjustProbabilitiesForBaseRate([1.5], 0.5, 0.1);

            rate.Should().Throw<ArgumentException>().WithParameterName("trainingBaseRate");
            target.Should().Throw<ArgumentException>().WithParameterName("targetBaseRate");
            probability.Should().Throw<ArgumentException>();
        }
    }
}
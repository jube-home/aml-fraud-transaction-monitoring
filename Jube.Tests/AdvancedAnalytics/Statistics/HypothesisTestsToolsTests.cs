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
using Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests;
using Xunit;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using System.Collections.Generic;
using System.Reflection;
using Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests.Models;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class HypothesisTestsToolsTests
    {
        private const double Exact = 1e-9;
        private const double PValue = 1e-7;
        private static readonly double[] sleepGroup1 = [0.7, -1.6, -0.2, -1.2, -0.1, 3.4, 3.7, 0.8, 0.0, 2.0];
        private static readonly double[] sleepGroup2 = [1.9, 0.8, 1.1, 0.1, -0.1, 4.4, 5.5, 1.6, 4.6, 3.4];
        private static readonly double[] plantControl = [4.17, 5.58, 5.18, 6.11, 4.50, 4.61, 5.17, 4.53, 5.33, 5.14];

        private static readonly double[] plantTreatment1 =
            [4.81, 4.17, 4.41, 3.59, 5.87, 3.83, 6.03, 4.89, 4.32, 4.69];

        private static readonly double[] plantTreatment2 =
            [6.31, 5.12, 5.54, 5.50, 5.37, 5.29, 4.92, 6.15, 5.80, 5.26];

        private static readonly double[][] plantGroups = [plantControl, plantTreatment1, plantTreatment2];
        private static readonly MeanAndVarianceTestTools means = new();
        private static readonly RankAndDistributionTestTools ranks = new();
        private static readonly CategoricalAndAgreementTestTools categories = new();
        private static readonly PowerAndIntervalTools power = new();
        private static readonly HypothesisTestSelectionTools selection = new();

        private static double Sd(double[] values)
        {
            var mean = values.Average();

            return Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / (values.Length - 1));
        }

        private static void Throws(Action action, string parameter)
        {
            action.Should().Throw<ArgumentException>().Which.ParamName.Should().Be(parameter);
        }

        private static readonly int[][] kappaPairTwoByTwo = Table([20, 5], [10, 15]);

        private static readonly int[][] kappaPairThreeByThree =
            Table([20, 5, 0], [4, 15, 6], [1, 3, 16]);

        private static readonly int[][] bowkerTable = Table([10, 3, 4], [7, 12, 5], [2, 9, 15]);
        private static readonly double[] aucScoresOne = [0.9, 0.8, 0.7, 0.6, 0.55, 0.54, 0.53, 0.51, 0.4, 0.3];
        private static readonly double[] aucScoresTwo = [0.6, 0.9, 0.5, 0.7, 0.4, 0.6, 0.2, 0.8, 0.3, 0.1];
        private static readonly int[] aucLabels = [1, 1, 0, 1, 1, 0, 0, 1, 0, 0];

        private static int[][] Table(params int[][] rows)
        {
            return rows;
        }

        [Fact]
        [Trait("Covers", "ChiSquareGoodnessOfFitTest")]
        public void ChiSquareGoodnessOfFitTestMatchesMendelsPeaData()
        {
            var result = categories.ChiSquareGoodnessOfFitTest([315, 108, 101, 32],
                [9, 3, 3, 1]);

            result.Statistic.Should().BeApproximately(0.47002398081534774, 1e-9);
            result.DegreesOfFreedom.Should().Be(3);
            result.PValue.Should().BeApproximately(0.925425895103616, 1e-8);
            result.EffectSize.Should().BeApproximately(0.0290751934124272, 1e-9);
            result.RejectNull.Should().BeFalse();
            result.Warnings.Should().BeEmpty();
        }

        [Fact]
        [Trait("Covers", "ChiSquareGoodnessOfFitTest")]
        public void ChiSquareGoodnessOfFitTestDefaultsToAnEvenSplitAndReducesDegreesOfFreedom()
        {
            var even = categories.ChiSquareGoodnessOfFitTest([10, 20, 30]);
            var estimated = categories.ChiSquareGoodnessOfFitTest([10, 20, 30, 40], null, 1);

            even.Statistic.Should().BeApproximately(10, 1e-9);
            even.DegreesOfFreedom.Should().Be(2);
            even.PValue.Should().BeApproximately(Math.Exp(-5), 1e-9);
            estimated.DegreesOfFreedom.Should().Be(2);
        }

        [Fact]
        [Trait("Covers", "ChiSquareGoodnessOfFitTest")]
        public void ChiSquareGoodnessOfFitTestWarnsForSparseCategoriesAndRejectsBadInput()
        {
            categories.ChiSquareGoodnessOfFitTest([1, 2, 3]).Warnings.Should().NotBeEmpty();
            Throws(() => categories.ChiSquareGoodnessOfFitTest([5]), "observedCounts");
            Throws(() => categories.ChiSquareGoodnessOfFitTest([5, -1]), "observedCounts");
            Throws(() => categories.ChiSquareGoodnessOfFitTest([0, 0]), "observedCounts");

            Throws(() => categories.ChiSquareGoodnessOfFitTest([5, 5], [1]),
                "expectedProportions");

            Throws(() => categories.ChiSquareGoodnessOfFitTest([5, 5], [1, 0]),
                "expectedProportions");

            Throws(() => categories.ChiSquareGoodnessOfFitTest([5, 5, 5], null, 2),
                "estimatedParameters");
        }

        [Fact]
        [Trait("Covers", "ChiSquareIndependenceTest")]
        public void ChiSquareIndependenceTestTwoByTwoMatchesClosedForm()
        {
            var table = Table([10, 20], [20, 10]);

            var plain = categories.ChiSquareIndependenceTest(table);
            var yates = categories.ChiSquareIndependenceTest(table, true);

            plain.Statistic.Should().BeApproximately(6.666666666666667, 1e-9);
            plain.PValue.Should().BeApproximately(0.009823274507519186, 1e-8);
            plain.EffectSize.Should().BeApproximately(1.0 / 3, 1e-9);
            plain.DegreesOfFreedom.Should().Be(1);
            yates.Statistic.Should().BeApproximately(5.4, 1e-9);
            yates.PValue.Should().BeApproximately(0.020136751550346332, 1e-8);
        }

        [Fact]
        [Trait("Covers", "ChiSquareIndependenceTest")]
        public void ChiSquareIndependenceTestTwoByThreeUsesRowsMinusOneTimesColumnsMinusOneDegrees()
        {
            var result = categories.ChiSquareIndependenceTest(Table([30, 20, 10], [10, 20, 30]));

            result.Statistic.Should().BeApproximately(20, 1e-9);
            result.DegreesOfFreedom.Should().Be(2);
            result.PValue.Should().BeApproximately(4.539992976249074e-05, 1e-10);
            result.EffectSize.Should().BeApproximately(0.408248290463863, 1e-9);
        }

        [Fact]
        [Trait("Covers", "ChiSquareIndependenceTest")]
        public void ChiSquareIndependenceTestRejectsBadTablesAndWarnsForSmallExpectedCounts()
        {
            categories.ChiSquareIndependenceTest(Table([1, 2], [2, 1])).Warnings.Should().NotBeEmpty();
            Throws(() => categories.ChiSquareIndependenceTest(Table([1, 2])), "table");
            Throws(() => categories.ChiSquareIndependenceTest(Table([1, 2], [3])), "table");
            Throws(() => categories.ChiSquareIndependenceTest(Table([1, -2], [3, 4])), "table");
            Throws(() => categories.ChiSquareIndependenceTest(Table([0, 0], [3, 4])), "table");
            Throws(() => categories.ChiSquareIndependenceTest(Table([0, 0], [0, 0])), "table");
            Throws(() => categories.ChiSquareIndependenceTest(bowkerTable, true), "yatesCorrection");
        }

        [Fact]
        [Trait("Covers", "FisherExactTest")]
        public void FisherExactTestMatchesFishersLadyTastingTea()
        {
            var table = Table([3, 1], [1, 3]);

            var two = categories.FisherExactTest(table);
            var greater = categories.FisherExactTest(table, HypothesisTail.Greater);

            greater.PValue.Should().BeApproximately(0.24285714285714285, 1e-9);
            two.PValue.Should().BeApproximately(0.4857142857142857, 1e-9);
            two.EstimatedValue.Should().BeApproximately(9, 1e-9);

            categories.FisherExactTest(table, HypothesisTail.Less).PValue.Should()
                .BeApproximately(0.9857142857142858, 1e-9);
        }

        [Fact]
        [Trait("Covers", "FisherExactTest")]
        public void FisherExactTestHandlesZeroCellsAndRejectsNonTwoByTwo()
        {
            var result = categories.FisherExactTest(Table([5, 0], [0, 5]));

            result.EstimatedValue.Should().BeNull();
            result.PValue.Should().BeApproximately(2.0 / 252, 1e-9);
            result.Warnings.Should().NotBeEmpty();
            Throws(() => categories.FisherExactTest(bowkerTable), "table");
            Throws(() => categories.FisherExactTest(Table([1, 2, 3], [1, 2, 3])), "table");
        }

        [Fact]
        [Trait("Covers", "McNemarTest")]
        public void McNemarTestMatchesTheWikipediaWorkedExample()
        {
            var table = Table([101, 121], [59, 33]);

            var plain = categories.McNemarTest(table);
            var corrected = categories.McNemarTest(table, true);

            plain.Statistic.Should().BeApproximately(21.355555555555554, 1e-9);
            plain.PValue.Should().BeApproximately(3.81513586511506e-06, 1e-10);
            plain.DegreesOfFreedom.Should().Be(1);
            corrected.Statistic.Should().BeApproximately(21.0125, 1e-9);
            corrected.PValue.Should().BeApproximately(4.5629663155688505e-06, 1e-10);
        }

        [Fact]
        [Trait("Covers", "McNemarTest")]
        public void McNemarTestRejectsNoDiscordantPairsAndWarnsForFewOfThem()
        {
            Throws(() => categories.McNemarTest(Table([10, 0], [0, 10])), "table");
            Throws(() => categories.McNemarTest(bowkerTable), "table");
            categories.McNemarTest(Table([10, 3], [6, 10])).Warnings.Should().NotBeEmpty();
        }

        [Fact]
        [Trait("Covers", "BowkerSymmetryTest")]
        public void BowkerSymmetryTestMatchesHandComputation()
        {
            var result = categories.BowkerSymmetryTest(bowkerTable);

            result.Statistic.Should().BeApproximately(3.4095238095238094, 1e-9);
            result.DegreesOfFreedom.Should().Be(3);
            result.PValue.Should().BeApproximately(0.3326875469552114, 1e-8);
            result.RejectNull.Should().BeFalse();
        }

        [Fact]
        [Trait("Covers", "BowkerSymmetryTest")]
        public void BowkerSymmetryTestRejectsAnEmptySymmetricPairAndNonSquareTables()
        {
            Throws(() => categories.BowkerSymmetryTest(Table([5, 0, 1], [0, 5, 1], [2, 3, 5])),
                "table");

            Throws(() => categories.BowkerSymmetryTest(Table([1, 2, 3], [1, 2, 3])), "table");
        }

        [Fact]
        [Trait("Covers", "StuartMaxwellHomogeneityTest")]
        public void StuartMaxwellHomogeneityTestMatchesHandComputation()
        {
            var result = categories.StuartMaxwellHomogeneityTest(bowkerTable);

            result.Statistic.Should().BeApproximately(0.3380281690140845, 1e-9);
            result.DegreesOfFreedom.Should().Be(2);
            result.PValue.Should().BeApproximately(0.844497008978201, 1e-8);
            Throws(() => categories.StuartMaxwellHomogeneityTest(Table([1, 2], [3, 4])), "table");
        }

        [Fact]
        [Trait("Covers", "BhapkarHomogeneityTest")]
        public void BhapkarHomogeneityTestMatchesHandComputation()
        {
            var result = categories.BhapkarHomogeneityTest(bowkerTable);

            result.Statistic.Should().BeApproximately(0.33974223536868786, 1e-9);
            result.DegreesOfFreedom.Should().Be(2);
            result.PValue.Should().BeApproximately(0.8437735570787963, 1e-8);
            Throws(() => categories.BhapkarHomogeneityTest(Table([1, 2], [3, 4])), "table");
        }

        [Fact]
        [Trait("Covers", "CohensKappaTest")]
        public void CohensKappaTestMatchesTheGrantProposalExample()
        {
            var result = categories.CohensKappaTest(kappaPairTwoByTwo);

            result.EstimatedValue.Should().BeApproximately(0.4, 1e-9);
            result.Statistic.Should().BeApproximately(2.886751345948128, 1e-8);
            result.PValue.Should().BeApproximately(0.0019462085613893216, 1e-8);
            result.ConfidenceLower.Should().BeApproximately(0.15109229047666106, 1e-8);
            result.ConfidenceUpper.Should().BeApproximately(0.6489077095233388, 1e-8);
            result.Interpretation.Should().Contain("fair");
        }

        [Fact]
        [Trait("Covers", "CohensKappaTest")]
        public void CohensKappaTestAgainstANonZeroHypothesisUsesTheGeneralStandardError()
        {
            var result = categories.CohensKappaTest(kappaPairTwoByTwo, 0.5, HypothesisTail.TwoSided);

            result.Statistic.Should().BeApproximately(-0.7874259854358909, 1e-8);
            result.PValue.Should().BeApproximately(0.4310, 1e-3);
        }

        [Fact]
        [Trait("Covers", "CohensKappaTest")]
        public void CohensKappaTestRejectsBadInput()
        {
            Throws(() => categories.CohensKappaTest(Table([10, 0], [0, 0])), "table");
            Throws(() => categories.CohensKappaTest(Table([5, 5, 5], [5, 5, 5])), "table");
            Throws(() => categories.CohensKappaTest(kappaPairTwoByTwo, 2), "hypothesizedKappa");
            categories.CohensKappaTest(Table([10, 0], [0, 10])).EstimatedValue.Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "WeightedKappaTest")]
        public void WeightedKappaTestMatchesHandComputationForBothWeightings()
        {
            var quadratic = categories.WeightedKappaTest(kappaPairThreeByThree);
            var linear = categories.WeightedKappaTest(kappaPairThreeByThree, HypothesisKappaWeighting.Linear);

            quadratic.EstimatedValue.Should().BeApproximately(0.7597503900156005, 1e-9);
            linear.EstimatedValue.Should().BeApproximately(0.6751740139211136, 1e-9);
            quadratic.PValue.Should().BeLessThan(0.001);
            quadratic.Interpretation.Should().Contain("substantial");
        }

        [Fact]
        [Trait("Covers", "WeightedKappaTest")]
        public void WeightedKappaTestRejectsBadInput()
        {
            Throws(() => categories.WeightedKappaTest(Table([5, 5, 5], [5, 5, 5])), "table");

            Throws(() => categories.WeightedKappaTest(kappaPairThreeByThree, HypothesisKappaWeighting.Linear, -2),
                "hypothesizedKappa");
        }

        [Fact]
        [Trait("Covers", "KappaComparisonTest")]
        public void KappaComparisonTestMatchesHandComputation()
        {
            var result = categories.KappaComparisonTest(kappaPairTwoByTwo, kappaPairThreeByThree);

            result.Statistic.Should().BeApproximately(1.2798871435870607, 1e-8);
            result.PValue.Should().BeApproximately(0.20058482979616113, 1e-8);
            result.EstimatedValue.Should().BeApproximately(0.4 - 0.5920245398773006, 1e-9);

            categories.KappaComparisonTest(kappaPairTwoByTwo, kappaPairTwoByTwo).PValue.Should()
                .BeApproximately(1, 1e-9);

            Throws(() => categories.KappaComparisonTest(kappaPairTwoByTwo, Table([5, 5, 5], [5, 5, 5])),
                "table2");
        }

        [Fact]
        [Trait("Covers", "KappaHomogeneityTest")]
        public void KappaHomogeneityTestMatchesInverseVarianceComputation()
        {
            var result = categories.KappaHomogeneityTest([kappaPairTwoByTwo, kappaPairThreeByThree]);

            result.Statistic.Should().BeApproximately(1.6381111003194457, 1e-8);
            result.PValue.Should().BeApproximately(0.20058482979616132, 1e-8);
            result.EstimatedValue.Should().BeApproximately(0.5375837475920183, 1e-8);
            result.DegreesOfFreedom.Should().Be(1);
            Throws(() => categories.KappaHomogeneityTest([kappaPairTwoByTwo]), "tables");
        }

        [Fact]
        [Trait("Covers", "KappaGroupDifferenceTest")]
        public void KappaGroupDifferenceTestComparesTheMeanKappasOfTwoGroups()
        {
            var groupOne = new[] { kappaPairTwoByTwo, kappaPairThreeByThree };
            var groupTwo = new[] { Table([30, 5], [5, 30]), Table([25, 10], [10, 25]) };

            var pooled = categories.KappaGroupDifferenceTest(groupOne, groupTwo);
            var welch = categories.KappaGroupDifferenceTest(groupOne, groupTwo, false);

            pooled.Statistic.Should().BeApproximately(-0.438152117779638, 1e-6);
            pooled.PValue.Should().BeApproximately(0.7040578046426595, 1e-6);
            welch.PValue.Should().BeApproximately(0.709337979308414, 1e-6);
            pooled.EstimatedValue.Should().BeApproximately(0.4960122699386503 - 0.5714285714285715, 1e-9);
            categories.KappaGroupDifferenceTest(groupOne, groupOne).PValue.Should().BeApproximately(1, 1e-9);
            Throws(() => categories.KappaGroupDifferenceTest([kappaPairTwoByTwo], groupTwo), "tables1");
        }

        [Fact]
        [Trait("Covers", "AucSignificanceTest")]
        public void AucSignificanceTestMatchesDeLongHandComputation()
        {
            var result = categories.AucSignificanceTest(aucScoresOne, aucLabels);

            result.EstimatedValue.Should().BeApproximately(0.8, 1e-9);
            result.Statistic.Should().BeApproximately(1.9364916731037087, 1e-8);
            result.PValue.Should().BeApproximately(0.05280751141611362, 1e-8);
            result.RejectNull.Should().BeFalse();
            result.ConfidenceUpper.Should().BeLessThanOrEqualTo(1);
        }

        [Fact]
        [Trait("Covers", "AucSignificanceTest")]
        public void AucSignificanceTestReportsAnInvertedScoreBelowHalf()
        {
            var inverted = aucScoresOne.Select(s => 1 - s).ToArray();

            var result = categories.AucSignificanceTest(inverted, aucLabels);

            result.EstimatedValue.Should().BeApproximately(0.2, 1e-9);
            result.Statistic.Should().BeApproximately(-1.9364916731037087, 1e-8);
        }

        [Fact]
        [Trait("Covers", "AucSignificanceTest")]
        public void AucSignificanceTestRejectsBadInput()
        {
            Throws(() => categories.AucSignificanceTest(aucScoresOne, [1, 0]), "labels");

            Throws(() => categories.AucSignificanceTest(aucScoresOne, [.. aucLabels.Select(l => l == 1 ? 2 : 0)]),
                "labels");

            Throws(() => categories.AucSignificanceTest(aucScoresOne, [1, 0, 0, 0, 0, 0, 0, 0, 0, 0]),
                "labels");

            Throws(() => categories.AucSignificanceTest([1.0, 2.0, 3.0], [1, 0, 1]), "scores");
            Throws(() => categories.AucSignificanceTest(aucScoresOne, aucLabels, 1.5), "hypothesizedAuc");
            Throws(() => categories.AucSignificanceTest([1.0, 2.0, 3.0, 4.0], [0, 0, 1, 1]), "scores");
        }

        [Fact]
        [Trait("Covers", "AucComparisonTest")]
        public void AucComparisonTestMatchesDeLongCovarianceHandComputation()
        {
            var result = categories.AucComparisonTest(aucScoresOne, aucScoresTwo, aucLabels);

            result.EstimatedValue.Should().BeApproximately(0.8 - 0.9, 1e-9);
            result.Statistic.Should().BeApproximately(-0.5976143046671966, 1e-8);
            result.PValue.Should().BeApproximately(0.5500973172303971, 1e-8);
        }

        [Fact]
        [Trait("Covers", "AucComparisonTest")]
        public void AucComparisonTestRejectsIdenticalModelsAndBadInput()
        {
            Throws(() => categories.AucComparisonTest(aucScoresOne, aucScoresOne, aucLabels), "scores1");
            Throws(() => categories.AucComparisonTest(aucScoresOne, [1.0], aucLabels), "scores2");
            Throws(() => categories.AucComparisonTest(aucScoresOne, aucScoresTwo, [1, 0]), "labels");
        }

        private static readonly double[] twoWayValues = [4, 5, 6, 6, 7, 9, 8, 9, 10, 5, 6, 7];
        private static readonly int[] twoWayFactorA = [0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1];
        private static readonly int[] twoWayFactorB = [0, 0, 0, 1, 1, 1, 0, 0, 0, 1, 1, 1];

        [Fact]
        [Trait("Covers", "OneSampleTTest")]
        public void OneSampleTTestMatchesPublishedSleepGroupTwoResult()
        {
            var result = means.OneSampleTTest(sleepGroup2);

            result.Statistic.Should().BeApproximately(3.679915894795189, Exact);
            result.DegreesOfFreedom.Should().BeApproximately(9, Exact);
            result.PValue.Should().BeApproximately(0.005076132649772399, PValue);
            result.EstimatedValue.Should().BeApproximately(2.33, Exact);
            result.ConfidenceLower.Should().BeApproximately(0.8976775393767054, PValue);
            result.ConfidenceUpper.Should().BeApproximately(3.762322460623295, PValue);
            result.EffectSize.Should().BeApproximately(1.1636915825409357, Exact);
            result.Tail.Should().Be(HypothesisTail.TwoSided);
            result.RejectNull.Should().BeTrue();
            result.Interpretation.Should().Contain("reject the null hypothesis");

            result.Interpretation.Should().Contain("t = 3.6799").And.Contain("p = 0.0050761")
                .And.Contain("with 9 degrees of freedom").And.Contain("alpha = 0.05");
        }

        [Fact]
        [Trait("Covers", "OneSampleTTest")]
        public void OneSampleTTestOneSidedTailsSplitTheTwoSidedPValue()
        {
            means.OneSampleTTest(sleepGroup2, 0, HypothesisTail.Greater).PValue
                .Should().BeApproximately(0.0025380663248861994, PValue);

            means.OneSampleTTest(sleepGroup2, 0, HypothesisTail.Less).PValue
                .Should().BeApproximately(1 - 0.0025380663248861994, PValue);
        }

        [Fact]
        [Trait("Covers", "OneSampleTTest")]
        public void OneSampleTTestOneSidedIntervalIsStillTheTwoSidedIntervalAtOneMinusAlpha()
        {
            var result = means.OneSampleTTest(sleepGroup2, 0, HypothesisTail.Greater);

            result.ConfidenceLower.Should().BeApproximately(0.8976775393767054, PValue);
            result.ConfidenceUpper.Should().BeApproximately(3.762322460623295, PValue);
        }

        [Fact]
        [Trait("Covers", "OneSampleTTest")]
        public void OneSampleTTestNonRejectionUsesAlpha()
        {
            var result = means.OneSampleTTest(sleepGroup2, 0, HypothesisTail.TwoSided, 0.001);

            result.RejectNull.Should().BeFalse();
            result.Interpretation.Should().Contain("fail to reject");
        }

        [Fact]
        [Trait("Covers", "OneSampleTTest")]
        public void OneSampleTTestWhenMeanEqualsHypothesisGivesZeroStatisticAndPValueOne()
        {
            var result = means.OneSampleTTest([1.0, 2.0, 3.0], 2.0);

            result.Statistic.Should().BeApproximately(0, Exact);
            result.PValue.Should().BeApproximately(1, Exact);
        }

        [Fact]
        [Trait("Covers", "OneSampleTTest")]
        public void OneSampleTTestRejectsBadInputWithTheParameterName()
        {
            Throws(() => means.OneSampleTTest([]), "sample");
            Throws(() => means.OneSampleTTest([1.0]), "sample");
            Throws(() => means.OneSampleTTest([1.0, double.NaN, 3.0]), "sample");
            Throws(() => means.OneSampleTTest([5.0, 5.0, 5.0]), "sample");
            Throws(() => means.OneSampleTTest(sleepGroup1, 0, HypothesisTail.TwoSided, 1.5), "alpha");
            Throws(() => means.OneSampleTTest(sleepGroup1, double.PositiveInfinity), "hypothesizedMean");
        }

        [Fact]
        [Trait("Covers", "OneSampleTTest")]
        public void OneSampleTTestHandlesHugeMagnitudesWithoutLosingTheStatistic()
        {
            var values = sleepGroup2.Select(v => v * 1e9 + 1e12).ToArray();

            var result = means.OneSampleTTest(values, 1e12);

            result.Statistic.Should().BeApproximately(3.679915894795189, 1e-6);
        }

        [Fact]
        [Trait("Covers", "OneSampleTTestFromSummary")]
        public void OneSampleTTestFromSummaryEqualsTheRawResult()
        {
            var raw = means.OneSampleTTest(sleepGroup2);
            var summary = means.OneSampleTTestFromSummary(2.33, 2.002248735796829, 10);

            summary.Statistic.Should().BeApproximately(raw.Statistic, Exact);
            summary.PValue.Should().BeApproximately(raw.PValue, Exact);
            summary.ConfidenceLower.Should().BeApproximately(raw.ConfidenceLower!.Value, Exact);
        }

        [Fact]
        [Trait("Covers", "OneSampleTTestFromSummary")]
        public void OneSampleTTestFromSummaryRejectsNonPositiveSpreadAndTinySamples()
        {
            Throws(() => means.OneSampleTTestFromSummary(1, 0, 10), "standardDeviation");
            Throws(() => means.OneSampleTTestFromSummary(1, 1, 1), "sampleSize");
            Throws(() => means.OneSampleTTestFromSummary(double.NaN, 1, 10), "mean");
        }

        [Fact]
        [Trait("Covers", "TwoSampleTTest")]
        public void TwoSampleTTestWelchMatchesPublishedSleepResult()
        {
            var result = means.TwoSampleTTest(sleepGroup1, sleepGroup2);

            result.Statistic.Should().BeApproximately(-1.8608134674868533, Exact);
            result.DegreesOfFreedom.Should().BeApproximately(17.77647351617849, 1e-8);
            result.PValue.Should().BeApproximately(0.07939414018735817, PValue);
            result.EstimatedValue.Should().BeApproximately(-1.58, Exact);
            result.ConfidenceLower.Should().BeApproximately(-3.3654832, 1e-6);
            result.ConfidenceUpper.Should().BeApproximately(0.2054832, 1e-6);
            result.EffectSize.Should().BeApproximately(-0.8321810813495397, Exact);
            result.RejectNull.Should().BeFalse();
            result.TestName.Should().Contain("Welch");
        }

        [Fact]
        [Trait("Covers", "TwoSampleTTest")]
        public void TwoSampleTTestPooledMatchesPublishedSleepResult()
        {
            var result = means.TwoSampleTTest(sleepGroup1, sleepGroup2, true);

            result.Statistic.Should().BeApproximately(-1.8608134674868533, Exact);
            result.DegreesOfFreedom.Should().BeApproximately(18, Exact);
            result.PValue.Should().BeApproximately(0.07918671421593833, PValue);
            result.TestName.Should().Contain("pooled");
        }

        [Fact]
        [Trait("Covers", "TwoSampleTTest")]
        public void TwoSampleTTestOneSidedDirectionIsGroupOneMinusGroupTwo()
        {
            var less = means.TwoSampleTTest(sleepGroup1, sleepGroup2, false, 0, HypothesisTail.Less);
            var greater = means.TwoSampleTTest(sleepGroup1, sleepGroup2, false, 0, HypothesisTail.Greater);

            less.PValue.Should().BeApproximately(0.07939414018735817 / 2, PValue);
            greater.PValue.Should().BeApproximately(1 - 0.07939414018735817 / 2, PValue);
        }

        [Fact]
        [Trait("Covers", "TwoSampleTTest")]
        public void TwoSampleTTestWarnsWhenPooledButVariancesAreVeryDifferent()
        {
            var narrow = new[] { 10.0, 10.1, 9.9, 10.05, 9.95, 10.02, 9.98, 10.03, 9.97, 10.0 };
            var wide = new[] { 1.0, 20.0, 5.0, 30.0, 2.0, 25.0, 8.0, 22.0, 3.0, 28.0 };

            var result = means.TwoSampleTTest(narrow, wide, true);

            result.Warnings.Should().Contain(w => w.Contains("Welch"));
        }

        [Fact]
        [Trait("Covers", "TwoSampleTTest")]
        public void TwoSampleTTestRejectsBadInput()
        {
            Throws(() => means.TwoSampleTTest([1.0], sleepGroup2), "sample1");
            Throws(() => means.TwoSampleTTest(sleepGroup1, []), "sample2");
            Throws(() => means.TwoSampleTTest([2.0, 2.0], [3.0, 3.0]), "sample1");
            Throws(() => means.TwoSampleTTest(sleepGroup1, sleepGroup2, false, double.NaN), "hypothesizedDifference");
        }

        [Fact]
        [Trait("Covers", "TwoSampleTTestFromSummary")]
        public void TwoSampleTTestFromSummaryEqualsTheRawWelchResult()
        {
            var raw = means.TwoSampleTTest(sleepGroup1, sleepGroup2);
            var summary = means.TwoSampleTTestFromSummary(0.75, Sd(sleepGroup1), 10, 2.33, Sd(sleepGroup2), 10);

            summary.Statistic.Should().BeApproximately(raw.Statistic, Exact);
            summary.PValue.Should().BeApproximately(raw.PValue, Exact);
        }

        [Fact]
        [Trait("Covers", "TwoSampleTTestFromSummary")]
        public void TwoSampleTTestFromSummaryRejectsBadInput()
        {
            Throws(() => means.TwoSampleTTestFromSummary(1, 0, 10, 2, 1, 10), "standardDeviation1");
            Throws(() => means.TwoSampleTTestFromSummary(1, 1, 10, 2, 1, 1), "sampleSize2");
        }

        [Fact]
        [Trait("Covers", "PairedTTest")]
        public void PairedTTestMatchesPublishedSleepResult()
        {
            var result = means.PairedTTest(sleepGroup1, sleepGroup2);

            result.Statistic.Should().BeApproximately(-4.062127683382037, Exact);
            result.DegreesOfFreedom.Should().BeApproximately(9, Exact);
            result.PValue.Should().BeApproximately(0.0028328901973842632, PValue);
            result.EstimatedValue.Should().BeApproximately(-1.58, Exact);
            result.ConfidenceLower.Should().BeApproximately(-2.459885763276983, PValue);
            result.ConfidenceUpper.Should().BeApproximately(-0.7001142367230174, PValue);
            result.EffectSize.Should().BeApproximately(-1.2845575625910546, Exact);
            result.RejectNull.Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "PairedTTest")]
        public void PairedTTestRejectsMismatchedLengthsAndIdenticalPairs()
        {
            Throws(() => means.PairedTTest(sleepGroup1, [1.0, 2.0]), "sample2");
            Throws(() => means.PairedTTest(sleepGroup1, sleepGroup1), "sample1");
            Throws(() => means.PairedTTest([1.0], [2.0]), "sample1");
        }

        [Fact]
        [Trait("Covers", "OneSampleZTest")]
        public void OneSampleZTestWithKnownSigmaMatchesClosedForm()
        {
            var sample = new[] { 10.0, 10.8, 11.6, 10.8 };

            var result = means.OneSampleZTest(sample, 10, 2.0);

            result.Statistic.Should().BeApproximately(0.8, Exact);
            result.PValue.Should().BeApproximately(0.42371074, 1e-7);
            result.ConfidenceLower.Should().BeApproximately(10.8 - 1.959963984540054, 1e-6);
            result.ConfidenceUpper.Should().BeApproximately(10.8 + 1.959963984540054, 1e-6);
        }

        [Fact]
        [Trait("Covers", "OneSampleZTest")]
        public void OneSampleZTestWithSampleSpreadWarnsForSmallSamples()
        {
            var result = means.OneSampleZTest(sleepGroup2);

            result.Statistic.Should().BeApproximately(3.679915894795189, Exact);
            result.PValue.Should().BeApproximately(0.000233, 1e-5);
            result.Warnings.Should().Contain(w => w.Contains("30"));
        }

        [Fact]
        [Trait("Covers", "OneSampleZTest")]
        public void OneSampleZTestRejectsBadSigmaAndConstantSampleWithoutSigma()
        {
            Throws(() => means.OneSampleZTest(sleepGroup2, 0, -1.0), "knownStandardDeviation");
            Throws(() => means.OneSampleZTest([4.0, 4.0, 4.0]), "sample");
            Throws(() => means.OneSampleZTest([1.0]), "sample");
        }

        private static double[] ZGroupA()
        {
            return [.. Enumerable.Range(0, 30).Select(i => 10 + i * 7 % 11 * 0.5)];
        }

        private static double[] ZGroupB()
        {
            return [.. Enumerable.Range(0, 30).Select(i => 11 + i * 5 % 13 * 0.5)];
        }

        [Fact]
        [Trait("Covers", "TwoSampleZTest")]
        public void TwoSampleZTestMatchesClosedForm()
        {
            var result = means.TwoSampleZTest(ZGroupA(), ZGroupB());

            result.Statistic.Should().BeApproximately(-2.937046322855295, Exact);
            result.PValue.Should().BeApproximately(0.003313546043095392, PValue);
            result.EstimatedValue.Should().BeApproximately(-1.35, Exact);
            result.ConfidenceLower.Should().BeApproximately(-2.250888541845254, 1e-6);
            result.ConfidenceUpper.Should().BeApproximately(-0.44911145815474507, 1e-6);
            result.Warnings.Should().BeEmpty();
        }

        [Fact]
        [Trait("Covers", "TwoSampleZTest")]
        public void TwoSampleZTestRejectsBadInputAndWarnsForSmallGroups()
        {
            Throws(() => means.TwoSampleZTest([3.0, 3.0], [4.0, 4.0]), "sample1");
            Throws(() => means.TwoSampleZTest([], ZGroupB()), "sample1");
            means.TwoSampleZTest(sleepGroup1, sleepGroup2).Warnings.Should().NotBeEmpty();
        }

        [Fact]
        [Trait("Covers", "TwoSampleZTestFromSummary")]
        public void TwoSampleZTestFromSummaryEqualsRawResult()
        {
            var a = ZGroupA();
            var b = ZGroupB();

            var raw = means.TwoSampleZTest(a, b);

            var summary = means.TwoSampleZTestFromSummary(a.Average(), Math.Sqrt(2.6195402298850574), 30,
                b.Average(), Math.Sqrt(3.7186781609195405), 30);

            summary.Statistic.Should().BeApproximately(raw.Statistic, 1e-9);
            summary.PValue.Should().BeApproximately(raw.PValue, 1e-9);
            Throws(() => means.TwoSampleZTestFromSummary(1, -1, 30, 2, 1, 30), "standardDeviation1");
        }

        [Fact]
        [Trait("Covers", "WaldZTestForEstimate")]
        public void WaldZTestMatchesClosedForm()
        {
            var result = means.WaldZTestForEstimate(1.5, 0, 0.6);

            result.Statistic.Should().BeApproximately(2.5, Exact);
            result.PValue.Should().BeApproximately(0.012419330651552278, PValue);
            result.ConfidenceLower.Should().BeApproximately(0.3240216092759671, 1e-6);
            result.ConfidenceUpper.Should().BeApproximately(2.675978390724033, 1e-6);
            Throws(() => means.WaldZTestForEstimate(1, 0, 0), "standardError");
            Throws(() => means.WaldZTestForEstimate(double.NaN, 0, 1), "estimate");
        }

        [Fact]
        [Trait("Covers", "OneProportionZTest")]
        public void OneProportionZTestSixtyHeadsInAHundredTossesGivesZTwo()
        {
            var result = means.OneProportionZTest(60, 100);

            result.Statistic.Should().BeApproximately(2.0, Exact);
            result.PValue.Should().BeApproximately(0.04550026389635843, PValue);
            result.EstimatedValue.Should().BeApproximately(0.6, Exact);
            result.ConfidenceLower.Should().BeApproximately(0.5020025867910618, 1e-6);
            result.ConfidenceUpper.Should().BeApproximately(0.6905987135675411, 1e-6);
        }

        [Fact]
        [Trait("Covers", "OneProportionZTest")]
        public void OneProportionZTestWarnsForSparseExpectedCountsAndRejectsBadInput()
        {
            means.OneProportionZTest(3, 10, 0.1).Warnings.Should().NotBeEmpty();
            Throws(() => means.OneProportionZTest(11, 10), "successes");
            Throws(() => means.OneProportionZTest(-1, 10), "successes");
            Throws(() => means.OneProportionZTest(1, 0), "trials");
            Throws(() => means.OneProportionZTest(5, 10, 0), "hypothesizedProportion");
            Throws(() => means.OneProportionZTest(5, 10, 1), "hypothesizedProportion");
        }

        [Fact]
        [Trait("Covers", "TwoProportionZTest")]
        public void TwoProportionZTestMatchesPooledClosedForm()
        {
            var result = means.TwoProportionZTest(45, 100, 30, 100);

            result.Statistic.Should().BeApproximately(2.1908902300206647, Exact);
            result.PValue.Should().BeApproximately(0.028459736916310572, PValue);
            result.EstimatedValue.Should().BeApproximately(0.15, Exact);

            means.TwoProportionZTest(45, 100, 30, 100, HypothesisTail.Greater).PValue
                .Should().BeApproximately(0.014229868458155286, PValue);
        }

        [Fact]
        [Trait("Covers", "TwoProportionZTest")]
        public void TwoProportionZTestRejectsDegenerateAndBadInput()
        {
            Throws(() => means.TwoProportionZTest(0, 10, 0, 10), "successes1");
            Throws(() => means.TwoProportionZTest(10, 10, 10, 10), "successes1");
            Throws(() => means.TwoProportionZTest(11, 10, 1, 10), "successes1");
            Throws(() => means.TwoProportionZTest(1, 10, 1, 0), "trials2");
        }

        [Fact]
        [Trait("Covers", "BinomialExactTest")]
        public void BinomialExactTestMatchesPublishedSixtyOfHundredResult()
        {
            var result = means.BinomialExactTest(60, 100);

            result.PValue.Should().BeApproximately(0.05688793364098079, PValue);
            result.ConfidenceLower.Should().BeApproximately(0.4972091504223347, 1e-6);
            result.ConfidenceUpper.Should().BeApproximately(0.6967052312971227, 1e-6);
            result.Statistic.Should().Be(60);

            means.BinomialExactTest(60, 100, 0.5, HypothesisTail.Greater).PValue
                .Should().BeApproximately(0.02844396682049033, PValue);
        }

        [Fact]
        [Trait("Covers", "BinomialExactTest")]
        public void BinomialExactTestHandlesBoundariesAndBadInput()
        {
            means.BinomialExactTest(0, 10).PValue.Should().BeApproximately(2 * Math.Pow(0.5, 10), PValue);
            means.BinomialExactTest(10, 10).ConfidenceUpper.Should().Be(1);
            means.BinomialExactTest(0, 10).ConfidenceLower.Should().Be(0);
            Throws(() => means.BinomialExactTest(5, 0), "trials");
            Throws(() => means.BinomialExactTest(5, 10, 1.5), "hypothesizedProbability");
            Throws(() => means.BinomialExactTest(11, 10), "successes");
        }

        [Fact]
        [Trait("Covers", "BinomialExactTestFromOutcomes")]
        public void BinomialExactTestFromOutcomesCountsTheTrueValues()
        {
            var outcomes = Enumerable.Range(0, 100).Select(i => i < 60).ToArray();

            var result = means.BinomialExactTestFromOutcomes(outcomes);

            result.Statistic.Should().Be(60);
            result.PValue.Should().BeApproximately(0.05688793364098079, PValue);
            Throws(() => means.BinomialExactTestFromOutcomes([]), "outcomes");
        }

        [Fact]
        [Trait("Covers", "FTestTwoVariances")]
        public void FTestTwoVariancesMatchesClosedFormOnPlantGrowth()
        {
            var result = means.FTestTwoVariances(plantControl, plantTreatment1);

            result.Statistic.Should().BeApproximately(0.5397430718837809, Exact);
            result.PValue.Should().BeApproximately(0.3718962712253302, PValue);
            result.DegreesOfFreedom.Should().Be(9);
            result.DegreesOfFreedom2.Should().Be(9);
            result.ConfidenceLower.Should().BeApproximately(0.1340645442252635, 1e-6);
            result.ConfidenceUpper.Should().BeApproximately(2.1730024543778104, 1e-6);
        }

        [Fact]
        [Trait("Covers", "FTestTwoVariances")]
        public void FTestTwoVariancesRejectsBadInput()
        {
            Throws(() => means.FTestTwoVariances([1.0], plantControl), "sample1");
            Throws(() => means.FTestTwoVariances([2.0, 2.0], plantControl), "sample1");
            Throws(() => means.FTestTwoVariances(plantControl, plantTreatment1, HypothesisTail.TwoSided, 0), "alpha");
        }

        [Fact]
        [Trait("Covers", "FTestTwoVariancesFromSummary")]
        public void FTestTwoVariancesFromSummaryEqualsRawResult()
        {
            var raw = means.FTestTwoVariances(plantControl, plantTreatment1);

            var summary = means.FTestTwoVariancesFromSummary(Sd(plantControl) * Sd(plantControl), 10,
                Sd(plantTreatment1) * Sd(plantTreatment1), 10);

            summary.Statistic.Should().BeApproximately(raw.Statistic, Exact);
            summary.PValue.Should().BeApproximately(raw.PValue, Exact);
            Throws(() => means.FTestTwoVariancesFromSummary(0, 10, 1, 10), "variance1");
            Throws(() => means.FTestTwoVariancesFromSummary(1, 1, 1, 10), "sampleSize1");
        }

        [Fact]
        [Trait("Covers", "LeveneTest")]
        public void LeveneTestBrownForsytheMatchesPublishedPlantGrowthResult()
        {
            var result = means.LeveneTest(plantGroups);

            result.Statistic.Should().BeApproximately(1.1191856948703909, 1e-8);
            result.PValue.Should().BeApproximately(0.3412266241254702, PValue);
            result.DegreesOfFreedom.Should().Be(2);
            result.DegreesOfFreedom2.Should().Be(27);
        }

        [Fact]
        [Trait("Covers", "LeveneTest")]
        public void LeveneTestMeanCentreMatchesClosedForm()
        {
            var result = means.LeveneTest(plantGroups, HypothesisVarianceHomogeneityCentre.Mean);

            result.Statistic.Should().BeApproximately(1.2369629544697838, 1e-8);
            result.PValue.Should().BeApproximately(0.30619492299144424, PValue);
        }

        [Fact]
        [Trait("Covers", "LeveneTest")]
        public void LeveneTestTrimmedMeanRunsAndRejectsBadInput()
        {
            var result = means.LeveneTest(plantGroups, HypothesisVarianceHomogeneityCentre.TrimmedMean);

            result.Statistic.Should().BeGreaterThan(0);
            Throws(() => means.LeveneTest([plantControl]), "groups");

            Throws(() => means.LeveneTest(plantGroups, HypothesisVarianceHomogeneityCentre.TrimmedMean, 0.6),
                "trimFraction");

            Throws(() => means.LeveneTest([plantControl, [1.0]]), "groups");
        }

        [Fact]
        [Trait("Covers", "BartlettTest")]
        public void BartlettTestMatchesPublishedPlantGrowthResult()
        {
            var result = means.BartlettTest(plantGroups);

            result.Statistic.Should().BeApproximately(2.8785737872360935, 1e-8);
            result.PValue.Should().BeApproximately(0.23709677363455817, PValue);
            result.DegreesOfFreedom.Should().Be(2);
        }

        [Fact]
        [Trait("Covers", "BartlettTest")]
        public void BartlettTestRejectsConstantGroupAndSingleGroup()
        {
            Throws(() => means.BartlettTest([plantControl, [2.0, 2.0, 2.0]]), "groups");
            Throws(() => means.BartlettTest([plantControl]), "groups");
        }

        [Fact]
        [Trait("Covers", "OneWayAnova")]
        public void OneWayAnovaMatchesPublishedPlantGrowthTable()
        {
            var result = means.OneWayAnova(plantGroups);

            result.FStatistic.Should().BeApproximately(4.846087862380137, 1e-8);
            result.PValue.Should().BeApproximately(0.01590995832562303, PValue);
            result.DegreesOfFreedomBetween.Should().Be(2);
            result.DegreesOfFreedomWithin.Should().Be(27);
            result.EtaSquared.Should().BeApproximately(0.2641482968321197, 1e-9);
            result.Table.Single(r => r.Source == "Between-Groups").SumOfSquares.Should().BeApproximately(3.76634, 1e-9);
            result.Table.Single(r => r.Source == "Within-Groups").SumOfSquares.Should().BeApproximately(10.49209, 1e-9);
            result.GroupMeans[0].Should().BeApproximately(5.032, 1e-9);
            result.GroupSizes.Should().Equal(10, 10, 10);
            result.RejectNull.Should().BeTrue();
            result.Interpretation.Should().Contain("reject the null hypothesis");
        }

        [Fact]
        [Trait("Covers", "OneWayAnova")]
        public void OneWayAnovaWithPerfectlySeparatedConstantGroupsHasInfiniteF()
        {
            var result = means.OneWayAnova([[1.0, 1.0], [2.0, 2.0]]);

            double.IsPositiveInfinity(result.FStatistic).Should().BeTrue();
            result.PValue.Should().Be(0);
            result.RejectNull.Should().BeTrue();
            Throws(() => means.OneWayAnova([plantControl]), "groups");
            Throws(() => means.OneWayAnova(plantGroups, 2), "alpha");
        }

        [Fact]
        [Trait("Covers", "OneWayAnovaFromLabels")]
        public void OneWayAnovaFromLabelsEqualsTheGroupedResult()
        {
            var values = plantGroups.SelectMany(g => g).ToArray();
            var labels = Enumerable.Range(0, 30).Select(i => i / 10).ToArray();

            var result = means.OneWayAnovaFromLabels(values, labels);

            result.FStatistic.Should().BeApproximately(4.846087862380137, 1e-8);
            Throws(() => means.OneWayAnovaFromLabels(values, [0, 1]), "labels");
            Throws(() => means.OneWayAnovaFromLabels(values, [.. Enumerable.Repeat(0, 30)]), "labels");
        }

        [Fact]
        [Trait("Covers", "TwoWayAnova")]
        public void TwoWayAnovaFixedEffectsTestsEverythingAgainstErrorMeanSquare()
        {
            var result = means.TwoWayAnova(twoWayValues, twoWayFactorA, twoWayFactorB);

            result.FactorAfStatistic.Should().BeApproximately(4.0, 1e-8);
            result.FactorApValue.Should().BeApproximately(0.0805162379572627, PValue);
            result.FactorAPartialEtaSquared.Should().BeApproximately(1.0 / 3, 1e-9);
            result.FactorBfStatistic.Should().BeApproximately(0.25, 1e-8);
            result.FactorBpValue.Should().BeApproximately(0.6305360755569767, PValue);
            result.InteractionFStatistic.Should().BeApproximately(16.0, 1e-8);
            result.InteractionPValue.Should().BeApproximately(0.003949772803445328, PValue);
            result.InteractionPartialEtaSquared.Should().BeApproximately(2.0 / 3, 1e-9);
            result.InteractionRejectNull.Should().BeTrue();
            result.FactorARejectNull.Should().BeFalse();
            result.Replications.Should().Be(3);

            result.Table.Single(r => r.Source == "Within-cells (error)").SumOfSquares.Should()
                .BeApproximately(10.666666666666666, 1e-9);
        }

        [Fact]
        [Trait("Covers", "TwoWayAnova")]
        public void TwoWayAnovaRandomEffectsTestsFactorsAgainstTheInteraction()
        {
            var result = means.TwoWayAnova(twoWayValues, twoWayFactorA, twoWayFactorB,
                HypothesisTwoWayAnovaModel.RandomEffects);

            result.FactorAfStatistic.Should().BeApproximately(0.25, 1e-8);
            result.FactorApValue.Should().BeApproximately(0.7048327646991337, PValue);
            result.FactorBfStatistic.Should().BeApproximately(0.015625, 1e-8);
            result.InteractionFStatistic.Should().BeApproximately(16.0, 1e-8);
        }

        [Fact]
        [Trait("Covers", "TwoWayAnova")]
        public void TwoWayAnovaMixedModelTestsFactorAAgainstInteractionAndFactorBAgainstError()
        {
            var result = means.TwoWayAnova(twoWayValues, twoWayFactorA, twoWayFactorB,
                HypothesisTwoWayAnovaModel.FactorAFixedFactorBRandom);

            result.FactorAfStatistic.Should().BeApproximately(0.25, 1e-8);
            result.FactorBfStatistic.Should().BeApproximately(0.25, 1e-8);
            result.InteractionFStatistic.Should().BeApproximately(16.0, 1e-8);
        }

        [Fact]
        [Trait("Covers", "TwoWayAnova")]
        public void TwoWayAnovaRejectsUnbalancedAndMalformedDesigns()
        {
            Throws(() => means.TwoWayAnova([.. twoWayValues.Take(11)], [.. twoWayFactorA.Take(11)],
                [.. twoWayFactorB.Take(11)]), "factorALabels");

            Throws(() => means.TwoWayAnova(twoWayValues, twoWayFactorA, [0, 1]), "factorBLabels");

            Throws(() => means.TwoWayAnova(twoWayValues, [.. twoWayFactorA.Select(a => -a)], twoWayFactorB),
                "factorALabels");

            Throws(() => means.TwoWayAnova(twoWayValues, new int[12], twoWayFactorB), "factorALabels");
            Throws(() => means.TwoWayAnova([1.0, 2.0], [0, 1], [0, 1]), "values");
        }

        [Fact]
        [Trait("Covers", "GrubbsOutlierTest")]
        public void GrubbsOutlierTestFlagsTheGrossOutlier()
        {
            var sample = new[] { 199.31, 199.53, 200.19, 200.82, 201.92, 201.95, 202.18, 245.57 };

            var result = means.GrubbsOutlierTest(sample);

            result.Statistic.Should().BeApproximately(2.46876461121245, 1e-9);
            result.RejectNull.Should().BeTrue();
            result.PValue.Should().BeLessThan(0.001);
            result.EstimatedValue.Should().BeApproximately(245.57, 1e-9);
            means.GrubbsOutlierTest(sample, HypothesisOutlierSide.Minimum).RejectNull.Should().BeFalse();
        }

        [Fact]
        [Trait("Covers", "GrubbsOutlierTest")]
        public void GrubbsOutlierTestRejectsBadInput()
        {
            Throws(() => means.GrubbsOutlierTest([1.0, 2.0]), "sample");
            Throws(() => means.GrubbsOutlierTest([3.0, 3.0, 3.0]), "sample");
        }

        [Fact]
        [Trait("Covers", "EffectSizeTwoSamples")]
        public void EffectSizeTwoSamplesMatchesClosedFormOnSleepData()
        {
            var result = means.EffectSizeTwoSamples(sleepGroup1, sleepGroup2);

            result.CohensD.Should().BeApproximately(-0.8321810813495397, Exact);
            result.HedgesG.Should().BeApproximately(-0.8321810813495397 * (1 - 3.0 / 71), Exact);
            result.MeanDifference.Should().BeApproximately(-1.58, Exact);
            result.GlassDelta.Should().BeApproximately(-1.58 / 2.002248735796829, Exact);
            result.Magnitude.Should().Be("large");
        }

        [Fact]
        [Trait("Covers", "EffectSizeTwoSamples")]
        public void EffectSizeTwoSamplesRejectsBadInput()
        {
            Throws(() => means.EffectSizeTwoSamples([1.0], sleepGroup2), "sample1");
            Throws(() => means.EffectSizeTwoSamples([1.0, 1.0], [2.0, 2.0]), "sample1");
        }

        [Fact]
        [Trait("Covers", "EffectSizeFromSummary")]
        public void EffectSizeFromSummaryMatchesRawResult()
        {
            var result = means.EffectSizeFromSummary(0.75, Sd(sleepGroup1), 10, 2.33, Sd(sleepGroup2), 10);

            result.CohensD.Should().BeApproximately(-0.8321810813495397, Exact);
            Throws(() => means.EffectSizeFromSummary(1, 0, 10, 2, 1, 10), "standardDeviation1");
        }

        private static readonly double[] rawPValues = [0.01, 0.04, 0.03, 0.005];

        [Fact]
        [Trait("Covers", "OneSampleTTestPowerAnalysis")]
        public void OneSampleTTestPowerAnalysisSolvesPowerForHalfSdEffectAndThirtyFourObservations()
        {
            var result = power.OneSampleTTestPowerAnalysis(HypothesisPowerSolveFor.Power, 0.5, 34);

            result.Power.Should().BeApproximately(0.8077775012792783, 1e-4);
            result.SampleSize1.Should().Be(34);
        }

        [Fact]
        [Trait("Covers", "OneSampleTTestPowerAnalysis")]
        public void OneSampleTTestPowerAnalysisNeedsThirtyFourObservationsForPowerEightyAtHalfSd()
        {
            var result = power.OneSampleTTestPowerAnalysis(HypothesisPowerSolveFor.SampleSize, 0.5, null, 0.8);

            result.SampleSize1.Should().Be(34);
            result.Power.Should().BeApproximately(0.8077775012792783, 1e-4);
        }

        [Fact]
        [Trait("Covers", "OneSampleTTestPowerAnalysis")]
        public void OneSampleTTestPowerAnalysisSolvesTheDetectableEffect()
        {
            var result = power.OneSampleTTestPowerAnalysis(HypothesisPowerSolveFor.EffectSize, null, 34, 0.8);

            result.EffectSize.Should().BeApproximately(0.4950280979734406, 1e-3);
            result.Power.Should().Be(0.8);
        }

        [Fact]
        [Trait("Covers", "OneSampleTTestPowerAnalysis")]
        public void OneSampleTTestPowerAnalysisRejectsMissingOrInvalidInputs()
        {
            Throws(() => power.OneSampleTTestPowerAnalysis(HypothesisPowerSolveFor.Power, 0.5), "sampleSize");
            Throws(() => power.OneSampleTTestPowerAnalysis(HypothesisPowerSolveFor.SampleSize, 0.5), "power");

            Throws(() => power.OneSampleTTestPowerAnalysis(HypothesisPowerSolveFor.SampleSize, null, null, 0.8),
                "effectSize");

            Throws(() => power.OneSampleTTestPowerAnalysis(HypothesisPowerSolveFor.Power, 0, 30), "effectSize");
            Throws(() => power.OneSampleTTestPowerAnalysis(HypothesisPowerSolveFor.Power, 0.5, 1), "sampleSize");

            Throws(() => power.OneSampleTTestPowerAnalysis(HypothesisPowerSolveFor.SampleSize, 0.5, null, 1.0),
                "power");

            Throws(() => power.OneSampleTTestPowerAnalysis(HypothesisPowerSolveFor.Power, 0.5, 30, null, 0), "alpha");
        }

        [Fact]
        [Trait("Covers", "OneSampleZTestPowerAnalysis")]
        public void OneSampleZTestPowerAnalysisMatchesTheClosedForm()
        {
            var result = power.OneSampleZTestPowerAnalysis(HypothesisPowerSolveFor.Power, 0.5, 32);
            var size = power.OneSampleZTestPowerAnalysis(HypothesisPowerSolveFor.SampleSize, 0.5, null, 0.8);

            result.Power.Should().BeApproximately(0.8074304194325569, 1e-6);
            size.SampleSize1.Should().Be(32);

            power.OneSampleZTestPowerAnalysis(HypothesisPowerSolveFor.Power, 0.5, 32, null, 0.05,
                    HypothesisTail.Greater)
                .Power.Should().BeGreaterThan(result.Power);
        }

        [Fact]
        [Trait("Covers", "OneSampleZTestPowerAnalysis")]
        public void OneSampleZTestPowerAnalysisLessTailMirrorsTheGreaterTail()
        {
            var greater = power.OneSampleZTestPowerAnalysis(HypothesisPowerSolveFor.SampleSize, 0.5, null, 0.8, 0.05,
                HypothesisTail.Greater);
            var less = power.OneSampleZTestPowerAnalysis(HypothesisPowerSolveFor.SampleSize, 0.5, null, 0.8, 0.05,
                HypothesisTail.Less);

            greater.SampleSize1.Should().Be(25);
            less.SampleSize1.Should().Be(25);
            less.Tail.Should().Be(HypothesisTail.Less);

            power.OneSampleTTestPowerAnalysis(HypothesisPowerSolveFor.Power, 0.5, 30, null, 0.05, HypothesisTail.Less)
                .Power.Should().BeApproximately(
                    power.OneSampleTTestPowerAnalysis(HypothesisPowerSolveFor.Power, 0.5, 30, null, 0.05,
                        HypothesisTail.Greater).Power, 1e-12);
        }

        [Fact]
        [Trait("Covers", "OneSampleZTestPowerAnalysis")]
        public void OneSampleZTestPowerAnalysisRoundTripsTheEffectSize()
        {
            var effect = power.OneSampleZTestPowerAnalysis(HypothesisPowerSolveFor.EffectSize, null, 32, 0.8);

            var back = power.OneSampleZTestPowerAnalysis(HypothesisPowerSolveFor.Power, effect.EffectSize, 32);

            back.Power.Should().BeApproximately(0.8, 1e-4);
            Throws(() => power.OneSampleZTestPowerAnalysis(HypothesisPowerSolveFor.Power, null, 32), "effectSize");
        }

        [Fact]
        [Trait("Covers", "TwoSampleTTestPowerAnalysis")]
        public void TwoSampleTTestPowerAnalysisNeedsSixtyFourPerGroupForHalfSdAtPowerEighty()
        {
            var size = power.TwoSampleTTestPowerAnalysis(HypothesisPowerSolveFor.SampleSize, 0.5, null, null, 0.8);
            var result = power.TwoSampleTTestPowerAnalysis(HypothesisPowerSolveFor.Power, 0.5, 64);

            size.SampleSize1.Should().Be(64);
            size.SampleSize2.Should().Be(64);
            size.TotalSampleSize.Should().Be(128);
            result.Power.Should().BeApproximately(0.8014595579222515, 1e-4);
            result.SampleSize2.Should().Be(64);
        }

        [Fact]
        [Trait("Covers", "TwoSampleTTestPowerAnalysis")]
        public void TwoSampleTTestPowerAnalysisSupportsUnequalAllocationAndRejectsBadInput()
        {
            var unequal = power.TwoSampleTTestPowerAnalysis(HypothesisPowerSolveFor.SampleSize, 0.5, null, null, 0.8,
                0.05, HypothesisTail.TwoSided, 2);

            unequal.SampleSize2.Should().Be(Math.Ceiling(unequal.SampleSize1 * 2));
            unequal.TotalSampleSize.Should().BeLessThan(200);
            power.TwoSampleTTestPowerAnalysis(HypothesisPowerSolveFor.Power, 0.5, 30, 60).SampleSize2.Should().Be(60);
            Throws(() => power.TwoSampleTTestPowerAnalysis(HypothesisPowerSolveFor.Power, 0.5), "sampleSize1");
            Throws(() => power.TwoSampleTTestPowerAnalysis(HypothesisPowerSolveFor.Power, 0.5, 30, 1), "sampleSize1");

            Throws(
                () => power.TwoSampleTTestPowerAnalysis(HypothesisPowerSolveFor.Power, 0.5, 30, null, null, 0.05,
                    HypothesisTail.TwoSided, 0), "allocationRatio");
        }

        [Fact]
        [Trait("Covers", "TwoSampleZTestPowerAnalysis")]
        public void TwoSampleZTestPowerAnalysisNeedsSixtyThreePerGroupForHalfSdAtPowerEighty()
        {
            var size = power.TwoSampleZTestPowerAnalysis(HypothesisPowerSolveFor.SampleSize, 0.5, null, null, 0.8);
            var result = power.TwoSampleZTestPowerAnalysis(HypothesisPowerSolveFor.Power, 0.5, 63);

            size.SampleSize1.Should().Be(63);
            result.Power.Should().BeApproximately(0.8013023941055795, 1e-4);
        }

        [Fact]
        [Trait("Covers", "TwoSampleZTestPowerAnalysis")]
        public void TwoSampleZTestPowerAnalysisSolvesEffectAndRejectsBadInput()
        {
            var effect = power.TwoSampleZTestPowerAnalysis(HypothesisPowerSolveFor.EffectSize, null, 63, null, 0.8);

            effect.EffectSize.Should().BeApproximately(0.5, 0.01);
            Throws(() => power.TwoSampleZTestPowerAnalysis(HypothesisPowerSolveFor.SampleSize, 0.5), "power");

            Throws(() => power.TwoSampleZTestPowerAnalysis(HypothesisPowerSolveFor.SampleSize, -1, null, null, 0.8),
                "effectSize");
        }

        [Fact]
        [Trait("Covers", "MeanConfidenceInterval")]
        public void MeanConfidenceIntervalMatchesPublishedSleepGroupIntervals()
        {
            var two = power.MeanConfidenceInterval(sleepGroup2);
            var one = power.MeanConfidenceInterval(sleepGroup1);

            two.Estimate.Should().BeApproximately(2.33, Exact);
            two.Lower.Should().BeApproximately(0.8976775393767054, 1e-6);
            two.Upper.Should().BeApproximately(3.762322460623295, 1e-6);
            one.Lower.Should().BeApproximately(-0.5297804, 1e-6);
            one.Upper.Should().BeApproximately(2.0297804, 1e-6);
        }

        [Fact]
        [Trait("Covers", "MeanConfidenceInterval")]
        public void MeanConfidenceIntervalWidensWithConfidenceAndRejectsBadInput()
        {
            var narrow = power.MeanConfidenceInterval(sleepGroup2, 0.9);
            var wide = power.MeanConfidenceInterval(sleepGroup2, 0.99);

            (wide.Upper - wide.Lower).Should().BeGreaterThan(narrow.Upper - narrow.Lower);
            power.MeanConfidenceInterval([3.0, 3.0, 3.0]).Lower.Should().Be(3);
            Throws(() => power.MeanConfidenceInterval([1.0]), "sample");
            Throws(() => power.MeanConfidenceInterval(sleepGroup2, 1), "confidenceLevel");
            Throws(() => power.MeanConfidenceInterval(sleepGroup2, 0), "confidenceLevel");
        }

        [Fact]
        [Trait("Covers", "ProportionConfidenceInterval")]
        public void ProportionConfidenceIntervalMatchesClosedFormsForSixtyOfHundred()
        {
            var wilson = power.ProportionConfidenceInterval(60, 100);

            var exact = power.ProportionConfidenceInterval(60, 100, 0.95,
                HypothesisProportionIntervalMethod.ClopperPearson);
            var wald = power.ProportionConfidenceInterval(60, 100, 0.95, HypothesisProportionIntervalMethod.Wald);
            var agresti =
                power.ProportionConfidenceInterval(60, 100, 0.95, HypothesisProportionIntervalMethod.AgrestiCoull);

            wilson.Lower.Should().BeApproximately(0.5020025867910618, 1e-6);
            wilson.Upper.Should().BeApproximately(0.6905987135675411, 1e-6);
            exact.Lower.Should().BeApproximately(0.4972091504223347, 1e-6);
            exact.Upper.Should().BeApproximately(0.6967052312971227, 1e-6);
            wald.Lower.Should().BeApproximately(0.5039817664728937, 1e-6);
            wald.Upper.Should().BeApproximately(0.6960182335271062, 1e-6);
            agresti.Lower.Should().BeApproximately(0.5019327335717484, 1e-6);
            agresti.Upper.Should().BeApproximately(0.6906685667868545, 1e-6);
            wilson.Estimate.Should().BeApproximately(0.6, Exact);
        }

        [Fact]
        [Trait("Covers", "ProportionConfidenceInterval")]
        public void ProportionConfidenceIntervalIsClippedAtTheBoundsAndWarnsForWaldWithFewSuccesses()
        {
            var zero = power.ProportionConfidenceInterval(0, 10);
            var all = power.ProportionConfidenceInterval(10, 10, 0.95, HypothesisProportionIntervalMethod.Wald);

            zero.Lower.Should().Be(0);
            zero.Upper.Should().BeInRange(0.2, 0.4);
            all.Upper.Should().Be(1);
            all.Warnings.Should().NotBeEmpty();
            Throws(() => power.ProportionConfidenceInterval(11, 10), "successes");
            Throws(() => power.ProportionConfidenceInterval(1, 0), "trials");
            Throws(() => power.ProportionConfidenceInterval(1, 10, 1.5), "confidenceLevel");
        }

        [Fact]
        [Trait("Covers", "VarianceConfidenceInterval")]
        public void VarianceConfidenceIntervalMatchesChiSquareHandComputation()
        {
            var sample = new[] { 2.1, 2.5, 1.9, 2.8, 2.2, 2.6, 2.0, 2.4 };

            var variance = power.VarianceConfidenceInterval(sample);
            var deviation = power.VarianceConfidenceInterval(sample, 0.95, true);

            variance.Estimate.Should().BeApproximately(0.09839285714285713, Exact);
            variance.Lower.Should().BeApproximately(0.04301256099118734, 1e-6);
            variance.Upper.Should().BeApproximately(0.4075759282880858, 1e-6);
            deviation.Lower.Should().BeApproximately(Math.Sqrt(0.04301256099118734), 1e-6);
            deviation.Upper.Should().BeApproximately(Math.Sqrt(0.4075759282880858), 1e-6);
            variance.Warnings.Should().NotBeEmpty();
        }

        [Fact]
        [Trait("Covers", "VarianceConfidenceInterval")]
        public void VarianceConfidenceIntervalRejectsBadInput()
        {
            Throws(() => power.VarianceConfidenceInterval([1.0]), "sample");
            Throws(() => power.VarianceConfidenceInterval([1.0, 1.0]), "sample");
            Throws(() => power.VarianceConfidenceInterval([1.0, 2.0], 2), "confidenceLevel");
        }

        [Fact]
        [Trait("Covers", "MedianConfidenceInterval")]
        public void MedianConfidenceIntervalForElevenValuesIsTheSecondToTenthOrderStatistic()
        {
            var sample = Enumerable.Range(1, 11).Select(i => (double)i).Reverse().ToArray();

            var result = power.MedianConfidenceInterval(sample);

            result.Estimate.Should().Be(6);
            result.Lower.Should().Be(2);
            result.Upper.Should().Be(10);
            result.AchievedConfidenceLevel.Should().BeApproximately(0.98828125, Exact);
            result.Warnings.Should().BeEmpty();
        }

        [Fact]
        [Trait("Covers", "MedianConfidenceInterval")]
        public void MedianConfidenceIntervalReturnsTheRangeWithAWarningWhenTheSampleIsTooSmall()
        {
            var result = power.MedianConfidenceInterval([5.0, 1.0, 3.0]);

            result.Lower.Should().Be(1);
            result.Upper.Should().Be(5);
            result.Warnings.Should().NotBeEmpty();
            result.AchievedConfidenceLevel.Should().BeApproximately(0.75, Exact);
            Throws(() => power.MedianConfidenceInterval([1.0]), "sample");
            Throws(() => power.MedianConfidenceInterval([1.0, double.NaN]), "sample");
        }

        [Fact]
        [Trait("Covers", "AdjustPValuesBonferroni")]
        public void AdjustPValuesBonferroniMultipliesByTheNumberOfTests()
        {
            var result = power.AdjustPValuesBonferroni(rawPValues);

            result.AdjustedPValues.Should().Equal(0.04, 0.16, 0.12, 0.02);
            result.RejectNull.Should().Equal(true, false, false, true);
            result.RejectedCount.Should().Be(2);
            power.AdjustPValuesBonferroni([0.6, 0.7]).AdjustedPValues.Should().Equal(1, 1);
        }

        [Fact]
        [Trait("Covers", "AdjustPValuesHolm")]
        public void AdjustPValuesHolmIsStepDownAndMonotone()
        {
            var result = power.AdjustPValuesHolm(rawPValues);

            result.AdjustedPValues.Should().Equal(0.03, 0.06, 0.06, 0.02);
            result.RejectNull.Should().Equal(true, false, false, true);
        }

        [Fact]
        [Trait("Covers", "AdjustPValuesBenjaminiHochberg")]
        public void AdjustPValuesBenjaminiHochbergControlsTheFalseDiscoveryRate()
        {
            var result = power.AdjustPValuesBenjaminiHochberg(rawPValues);

            result.AdjustedPValues.Should().Equal(0.02, 0.04, 0.04, 0.02);
            result.RejectNull.Should().Equal(true, true, true, true);
            power.AdjustPValuesBenjaminiHochberg(rawPValues, 0.03).RejectedCount.Should().Be(2);
        }

        [Fact]
        [Trait("Covers", "AdjustPValuesByMethod")]
        public void AdjustPValuesByMethodCoversSidakHochbergAndBenjaminiYekutieli()
        {
            var sidak = power.AdjustPValuesByMethod(rawPValues, HypothesisPValueAdjustment.Sidak);
            var hochberg = power.AdjustPValuesByMethod(rawPValues, HypothesisPValueAdjustment.Hochberg);
            var yekutieli = power.AdjustPValuesByMethod(rawPValues, HypothesisPValueAdjustment.BenjaminiYekutieli);

            sidak.AdjustedPValues[0].Should().BeApproximately(1 - Math.Pow(0.99, 4), Exact);
            hochberg.AdjustedPValues.Should().Equal(0.03, 0.04, 0.04, 0.02);
            yekutieli.AdjustedPValues[3].Should().BeApproximately(0.02 * (1 + 0.5 + 1.0 / 3 + 0.25), Exact);
            yekutieli.AdjustedPValues[1].Should().BeApproximately(0.04 * (1 + 0.5 + 1.0 / 3 + 0.25), Exact);
        }

        [Fact]
        [Trait("Covers", "AdjustPValuesByMethod")]
        public void AdjustPValuesRejectsEmptyOutOfRangeAndNaNAndKeepsASingleValueUnchanged()
        {
            power.AdjustPValuesByMethod([0.03], HypothesisPValueAdjustment.Holm).AdjustedPValues.Should()
                .Equal(0.03);

            Throws(() => power.AdjustPValuesBonferroni([]), "pValues");
            Throws(() => power.AdjustPValuesHolm([0.1, 1.2]), "pValues");
            Throws(() => power.AdjustPValuesBenjaminiHochberg([double.NaN]), "pValues");
            Throws(() => power.AdjustPValuesByMethod(rawPValues, HypothesisPValueAdjustment.Holm, 0), "alpha");
        }

        [Fact]
        [Trait("Covers", "PValueFromTestStatistic")]
        public void PValueFromTestStatisticMatchesClosedFormsForEachDistribution()
        {
            power.PValueFromTestStatistic(HypothesisStatisticDistribution.StandardNormal, 1.96).PValue
                .Should().BeApproximately(0.04999579029644087, 1e-9);

            power.PValueFromTestStatistic(HypothesisStatisticDistribution.StudentT, 3.679915894795189, 9).PValue
                .Should().BeApproximately(0.005076132649772399, 1e-8);

            power.PValueFromTestStatistic(HypothesisStatisticDistribution.ChiSquare, 3.841458820694124, 1,
                0, HypothesisTail.Greater).PValue.Should().BeApproximately(0.05, 1e-8);
            power.PValueFromTestStatistic(HypothesisStatisticDistribution.F, 4.846087862380137, 2, 27,
                HypothesisTail.Greater).PValue.Should().BeApproximately(0.01590995832562303, 1e-8);
        }

        [Fact]
        [Trait("Covers", "PValueFromTestStatistic")]
        public void PValueFromTestStatisticSupportsLowerTailAndRejectsMissingDegrees()
        {
            power.PValueFromTestStatistic(HypothesisStatisticDistribution.StandardNormal, -1.6448536269514729, 0, 0,
                HypothesisTail.Less).PValue.Should().BeApproximately(0.05, 1e-8);

            Throws(() => power.PValueFromTestStatistic(HypothesisStatisticDistribution.StudentT, 1),
                "degreesOfFreedom");

            Throws(() => power.PValueFromTestStatistic(HypothesisStatisticDistribution.F, 1, 3), "degreesOfFreedom2");

            Throws(() => power.PValueFromTestStatistic(HypothesisStatisticDistribution.StandardNormal, double.NaN),
                "statistic");
        }

        [Fact]
        [Trait("Covers", "CriticalValueForSignificanceLevel")]
        public void CriticalValueForSignificanceLevelMatchesTextbookValues()
        {
            var z = power.CriticalValueForSignificanceLevel(HypothesisStatisticDistribution.StandardNormal);
            var t = power.CriticalValueForSignificanceLevel(HypothesisStatisticDistribution.StudentT, 0.05, 10);

            var chi = power.CriticalValueForSignificanceLevel(HypothesisStatisticDistribution.ChiSquare, 0.05, 1, 0,
                HypothesisTail.Greater);
            var f = power.CriticalValueForSignificanceLevel(HypothesisStatisticDistribution.F, 0.05, 1, 10,
                HypothesisTail.Greater);

            z.UpperCriticalValue.Should().BeApproximately(1.959963984540055, 1e-7);
            z.LowerCriticalValue.Should().BeApproximately(-1.959963984540055, 1e-7);
            t.UpperCriticalValue.Should().BeApproximately(2.2281388519862757, 1e-6);
            chi.UpperCriticalValue.Should().BeApproximately(3.84145882069413, 1e-6);
            chi.LowerCriticalValue.Should().BeNull();
            f.UpperCriticalValue.Should().BeApproximately(4.964602743730715, 1e-6);
        }

        [Fact]
        [Trait("Covers", "CriticalValueForSignificanceLevel")]
        public void CriticalValueForSignificanceLevelOneSidedLowerTailAndBadInput()
        {
            var less = power.CriticalValueForSignificanceLevel(HypothesisStatisticDistribution.StandardNormal, 0.05, 0,
                0,
                HypothesisTail.Less);

            less.LowerCriticalValue.Should().BeApproximately(-1.6448536269514729, 1e-7);
            less.UpperCriticalValue.Should().BeNull();

            Throws(() => power.CriticalValueForSignificanceLevel(HypothesisStatisticDistribution.StandardNormal, 0),
                "alpha");

            Throws(() => power.CriticalValueForSignificanceLevel(HypothesisStatisticDistribution.ChiSquare),
                "degreesOfFreedom");
        }

        [Fact]
        [Trait("Covers", "RecommendHypothesisTest")]
        public void RecommendHypothesisTestChoosesTheClassicTestForEachCommonSituation()
        {
            selection.RecommendHypothesisTest(HypothesisDesign.TwoIndependentGroups, HypothesisDataKind.Continuous,
                    true, 40)
                .PrimaryTool.Should().Be("TwoSampleTTest");

            selection.RecommendHypothesisTest(HypothesisDesign.TwoIndependentGroups, HypothesisDataKind.Continuous,
                    false, 8)
                .PrimaryTool.Should().Be("MannWhitneyUTest");

            selection.RecommendHypothesisTest(HypothesisDesign.TwoPairedOrMatchedGroups, HypothesisDataKind.Continuous,
                    true)
                .PrimaryTool.Should().Be("PairedTTest");

            selection.RecommendHypothesisTest(HypothesisDesign.TwoPairedOrMatchedGroups, HypothesisDataKind.Binary)
                .PrimaryTool.Should().Be("McNemarTest");

            selection.RecommendHypothesisTest(HypothesisDesign.ThreeOrMoreIndependentGroups,
                    HypothesisDataKind.Continuous, true, 20, true, 4)
                .PrimaryTool.Should().Be("OneWayAnova");

            selection.RecommendHypothesisTest(HypothesisDesign.ThreeOrMoreIndependentGroups, HypothesisDataKind.Ordinal)
                .PrimaryTool.Should().Be("KruskalWallisTest");

            selection.RecommendHypothesisTest(HypothesisDesign.AssociationBetweenTwoCategoricalVariables,
                    HypothesisDataKind.Categorical, null, 200)
                .PrimaryTool.Should().Be("ChiSquareIndependenceTest");
            selection.RecommendHypothesisTest(HypothesisDesign.AssociationBetweenTwoCategoricalVariables,
                    HypothesisDataKind.Binary, null, 12)
                .PrimaryTool.Should().Be("FisherExactTest");
            selection.RecommendHypothesisTest(HypothesisDesign.OneSampleAgainstValue, HypothesisDataKind.Binary, null,
                    10)
                .PrimaryTool.Should().Be("BinomialExactTest");

            selection.RecommendHypothesisTest(HypothesisDesign.OneSampleAgainstValue, HypothesisDataKind.Continuous,
                    true)
                .PrimaryTool.Should().Be("OneSampleTTest");

            selection.RecommendHypothesisTest(HypothesisDesign.NormalityOfOneSample)
                .PrimaryTool.Should().Be("ShapiroWilkNormalityTest");

            selection.RecommendHypothesisTest(HypothesisDesign.ComparingVariances, HypothesisDataKind.Continuous, false,
                    null, null, 3)
                .PrimaryTool.Should().Be("LeveneTest");

            selection.RecommendHypothesisTest(HypothesisDesign.AgreementBetweenTwoRaters, HypothesisDataKind.Ordinal)
                .PrimaryTool.Should().Be("WeightedKappaTest");
        }

        [Fact]
        [Trait("Covers", "RecommendHypothesisTest")]
        public void RecommendHypothesisTestOnlyNamesToolsThatExist()
        {
            var registered = AdvancedAnalyticsToolCatalogue.Discover(typeof(HypothesisTestSelectionTools).Assembly)
                .Select(d => d.ToolName)
                .ToHashSet();

            foreach (var design in Enum.GetValues<HypothesisDesign>())
            {
                foreach (var kind in Enum.GetValues<HypothesisDataKind>())
                {
                    foreach (var normal in new bool?[] { null, true, false })
                    {
                        foreach (var size in new int?[] { null, 5, 100 })
                        {
                            var recommendation = selection.RecommendHypothesisTest(design, kind, normal, size, null, 3);

                            var names = new[] { recommendation.PrimaryTool }
                                .Concat(recommendation.AlternativeTools)
                                .Concat(recommendation.CompanionTools);

                            names.Should().OnlyContain(n => registered.Contains(n));
                            recommendation.Reason.Should().NotBeNullOrWhiteSpace();
                        }
                    }
                }
            }
        }

        [Fact]
        [Trait("Covers", "RecommendHypothesisTest")]
        public void RecommendHypothesisTestRejectsBadCounts()
        {
            Throws(
                () => selection.RecommendHypothesisTest(HypothesisDesign.TwoIndependentGroups,
                    HypothesisDataKind.Continuous, null, null, null, 1), "groups");

            Throws(
                () => selection.RecommendHypothesisTest(HypothesisDesign.TwoIndependentGroups,
                    HypothesisDataKind.Continuous, null, 0), "smallestGroupSize");
        }

        [Fact]
        public void EveryHypothesisToolIsSelfDescribedWithoutTheLibraryName()
        {
            var descriptors = AdvancedAnalyticsToolCatalogue.Discover(typeof(HypothesisTestSelectionTools).Assembly)
                .Where(d => d.GroupName == "HypothesisTests")
                .ToList();

            descriptors.Should().HaveCountGreaterThan(50);
            AdvancedAnalyticsToolCatalogue.Validate(descriptors).Should().BeEmpty();
            descriptors.Select(d => d.ToolName).Should().OnlyHaveUniqueItems();
        }

        private static readonly double[] hollanderWolfeX =
            [0.80, 0.83, 1.89, 1.04, 1.45, 1.38, 1.91, 1.64, 0.73, 1.46];

        private static readonly double[] hollanderWolfeY = [1.15, 0.88, 0.90, 0.74, 1.21];
        private static readonly double[] depressionBefore = [1.83, 0.50, 1.62, 2.48, 1.68, 1.88, 1.55, 3.06, 1.30];
        private static readonly double[] depressionAfter = [0.878, 0.647, 0.598, 2.05, 1.06, 1.29, 1.06, 3.14, 1.29];

        private static readonly double[] studentScores =
            [106, 115, 96, 88, 91, 88, 81, 104, 99, 68, 104, 100, 77, 98, 96, 104, 82, 94, 72, 96];

        private static readonly double[] shapiroWilkPaperSample =
            [148, 154, 158, 160, 161, 162, 166, 170, 182, 195, 236];

        private static double[] NormalQuantileSample(int n)
        {
            return [.. Enumerable.Range(1, n).Select(i => NormalQuantile((i - 0.5) / n))];
        }

        private static double NormalQuantile(double p)
        {
            double low = -10;
            double high = 10;

            for (var i = 0; i < 200; i++)
            {
                var mid = (low + high) / 2;
                var cdf = 0.5 * (1 + Erf(mid / Math.Sqrt(2)));

                if (cdf < p)
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            return (low + high) / 2;
        }

        private static double Erf(double x)
        {
            var sum = x;
            var term = x;

            for (var n = 1; n < 200; n++)
            {
                term *= -x * x / n;
                sum += term / (2 * n + 1);
            }

            return 2 / Math.Sqrt(Math.PI) * sum;
        }

        [Fact]
        [Trait("Covers", "MannWhitneyUTest")]
        public void MannWhitneyUTestCompleteSeparationHasExactPValueOneTenth()
        {
            var result = ranks.MannWhitneyUTest([1.0, 2, 3], [4.0, 5, 6]);

            result.Statistic.Should().Be(0);
            result.PValue.Should().BeApproximately(0.1, Exact);
            result.EffectSize.Should().Be(0);
            result.EstimatedValue.Should().Be(0);
            result.Warnings.Should().BeEmpty();
        }

        [Fact]
        [Trait("Covers", "MannWhitneyUTest")]
        public void MannWhitneyUTestMatchesPublishedHollanderWolfeExample()
        {
            var greater = ranks.MannWhitneyUTest(hollanderWolfeX, hollanderWolfeY, HypothesisTail.Greater);
            var less = ranks.MannWhitneyUTest(hollanderWolfeX, hollanderWolfeY, HypothesisTail.Less);
            var two = ranks.MannWhitneyUTest(hollanderWolfeX, hollanderWolfeY);

            greater.Statistic.Should().Be(35);
            greater.PValue.Should().BeApproximately(0.1272061272061272, 1e-6);
            less.PValue.Should().BeApproximately(0.8967698967698968, 1e-6);
            two.PValue.Should().BeApproximately(0.2544122544122544, 1e-6);
            greater.EffectSize.Should().BeApproximately(35.0 / 50, Exact);
        }

        [Fact]
        [Trait("Covers", "MannWhitneyUTest")]
        public void MannWhitneyUTestWithTiesUsesApproximationAndWarns()
        {
            var result = ranks.MannWhitneyUTest([1.0, 2, 2, 3], [2.0, 3, 4, 5]);

            result.Statistic.Should().BeApproximately(2.5, Exact);
            result.Warnings.Should().Contain(w => w.Contains("Tied"));
            result.PValue.Should().BeInRange(0, 1);
        }

        [Fact]
        [Trait("Covers", "MannWhitneyUTest")]
        public void MannWhitneyUTestRejectsBadInput()
        {
            Throws(() => ranks.MannWhitneyUTest([], [1.0]), "sample1");
            Throws(() => ranks.MannWhitneyUTest([1.0], [double.NaN]), "sample2");
            Throws(() => ranks.MannWhitneyUTest([1.0], [2.0], HypothesisTail.TwoSided, 0), "alpha");
        }

        [Fact]
        [Trait("Covers", "WilcoxonSignedRankTest")]
        public void WilcoxonSignedRankTestOfDifferencesMatchesPublishedDepressionExample()
        {
            var differences = depressionBefore.Zip(depressionAfter, (a, b) => a - b).ToArray();

            var greater = ranks.WilcoxonSignedRankTest(differences, 0, HypothesisTail.Greater);
            var two = ranks.WilcoxonSignedRankTest(differences);

            greater.Statistic.Should().Be(40);
            greater.PValue.Should().BeApproximately(0.01953125, Exact);
            two.PValue.Should().BeApproximately(0.0390625, Exact);
        }

        [Fact]
        [Trait("Covers", "WilcoxonSignedRankTest")]
        public void WilcoxonSignedRankTestDropsZerosAndRejectsAllZeroInput()
        {
            var result = ranks.WilcoxonSignedRankTest([0.0, 1.0, 2.0, 3.0, 4.0]);

            result.Warnings.Should().Contain(w => w.Contains("Zero"));
            Throws(() => ranks.WilcoxonSignedRankTest([3.0, 3.0], 3), "sample");
            Throws(() => ranks.WilcoxonSignedRankTest([]), "sample");
        }

        [Fact]
        [Trait("Covers", "WilcoxonSignedRankPairedTest")]
        public void WilcoxonSignedRankPairedTestMatchesPublishedDepressionExample()
        {
            var greater = ranks.WilcoxonSignedRankPairedTest(depressionBefore, depressionAfter, HypothesisTail.Greater);
            var less = ranks.WilcoxonSignedRankPairedTest(depressionBefore, depressionAfter, HypothesisTail.Less);

            greater.Statistic.Should().Be(40);
            greater.PValue.Should().BeApproximately(0.01953125, Exact);
            less.PValue.Should().BeApproximately(0.986328125, Exact);
        }

        [Fact]
        [Trait("Covers", "WilcoxonSignedRankPairedTest")]
        public void WilcoxonSignedRankPairedTestRejectsMismatchAndIdenticalPairs()
        {
            Throws(() => ranks.WilcoxonSignedRankPairedTest(depressionBefore, [1.0]), "sample2");
            Throws(() => ranks.WilcoxonSignedRankPairedTest(depressionBefore, depressionBefore), "sample1");
        }

        [Fact]
        [Trait("Covers", "SignTestOneSample")]
        public void SignTestOneSampleCountsFivePositivesOfNineteenAndSaysMedianIsBelow()
        {
            var less = ranks.SignTestOneSample(studentScores, 100, HypothesisTail.Less);
            var greater = ranks.SignTestOneSample(studentScores, 100, HypothesisTail.Greater);

            less.Statistic.Should().Be(5);
            less.PValue.Should().BeApproximately(0.0317840576171875, Exact);
            greater.PValue.Should().BeApproximately(0.9903945922851562, Exact);
            less.RejectNull.Should().BeTrue();
            less.EstimatedValue.Should().BeApproximately(5.0 / 19, Exact);
        }

        [Fact]
        [Trait("Covers", "SignTestOneSample")]
        public void SignTestOneSampleRejectsAllTiedAndEmptyInput()
        {
            Throws(() => ranks.SignTestOneSample([2.0, 2.0], 2), "sample");
            Throws(() => ranks.SignTestOneSample([]), "sample");
        }

        [Fact]
        [Trait("Covers", "SignTestPaired")]
        public void SignTestPairedAgreesWithTheOneSampleTestOnTheSameSigns()
        {
            var second = Enumerable.Repeat(100.0, studentScores.Length).ToArray();

            var result = ranks.SignTestPaired(studentScores, second, HypothesisTail.Less);

            result.Statistic.Should().Be(5);
            result.PValue.Should().BeApproximately(0.0317840576171875, Exact);
            var reversed = ranks.SignTestPaired(second, studentScores, HypothesisTail.Less);
            reversed.Statistic.Should().Be(14);
            reversed.PValue.Should().BeApproximately(0.9903945922851562, Exact);
        }

        [Fact]
        [Trait("Covers", "SignTestPaired")]
        public void SignTestPairedRejectsMismatchAndAllTied()
        {
            Throws(() => ranks.SignTestPaired([1.0, 2.0], [1.0]), "sample2");
            Throws(() => ranks.SignTestPaired([1.0, 2.0], [1.0, 2.0]), "sample1");
        }

        [Fact]
        [Trait("Covers", "SignTestFromCounts")]
        public void SignTestFromCountsUsesTheExactBinomial()
        {
            ranks.SignTestFromCounts(5, 19, HypothesisTail.Less).PValue
                .Should().BeApproximately(0.0317840576171875, Exact);

            ranks.SignTestFromCounts(10, 10).PValue.Should().BeApproximately(2 * Math.Pow(0.5, 10), Exact);
            Throws(() => ranks.SignTestFromCounts(11, 10), "positiveCount");
            Throws(() => ranks.SignTestFromCounts(0, 0), "totalCount");
        }

        [Fact]
        [Trait("Covers", "KolmogorovSmirnovOneSampleTest")]
        public void KolmogorovSmirnovOneSampleTestUniformExampleHasStatisticPointOneFive()
        {
            var result = ranks.KolmogorovSmirnovOneSampleTest([0.1, 0.35, 0.5, 0.7, 0.95],
                HypothesisReferenceDistribution.Uniform, 0);

            result.Statistic.Should().BeApproximately(0.15, Exact);
            result.PValue.Should().BeGreaterThan(0.9);
            result.RejectNull.Should().BeFalse();
        }

        [Fact]
        [Trait("Covers", "KolmogorovSmirnovOneSampleTest")]
        public void KolmogorovSmirnovOneSampleTestRejectsAWronglyCentredNormal()
        {
            var sample = NormalQuantileSample(50).Select(v => v + 3).ToArray();

            var result = ranks.KolmogorovSmirnovOneSampleTest(sample, HypothesisReferenceDistribution.Normal, 0);

            result.PValue.Should().BeLessThan(0.001);
            result.RejectNull.Should().BeTrue();

            ranks.KolmogorovSmirnovOneSampleTest(NormalQuantileSample(50), HypothesisReferenceDistribution.Normal, 0)
                .PValue.Should().BeGreaterThan(0.9);
        }

        [Fact]
        [Trait("Covers", "KolmogorovSmirnovOneSampleTest")]
        public void KolmogorovSmirnovOneSampleTestSupportsExponentialAndLogNormalAndRejectsBadParameters()
        {
            var exponential = Enumerable.Range(1, 30).Select(i => -Math.Log(1 - (i - 0.5) / 30)).ToArray();

            ranks.KolmogorovSmirnovOneSampleTest(exponential, HypothesisReferenceDistribution.Exponential, 1)
                .PValue.Should().BeGreaterThan(0.9);

            ranks.KolmogorovSmirnovOneSampleTest([.. exponential.Select(Math.Exp)],
                HypothesisReferenceDistribution.LogNormal, 0).Statistic.Should().BeGreaterThan(0);

            Throws(
                () => ranks.KolmogorovSmirnovOneSampleTest(exponential, HypothesisReferenceDistribution.Normal, 0, 0),
                "parameter2");

            Throws(
                () => ranks.KolmogorovSmirnovOneSampleTest(exponential, HypothesisReferenceDistribution.Uniform, 2),
                "parameter2");

            Throws(
                () => ranks.KolmogorovSmirnovOneSampleTest(exponential, HypothesisReferenceDistribution.Exponential,
                    -1), "parameter1");

            Throws(
                () => ranks.KolmogorovSmirnovOneSampleTest([1.0], HypothesisReferenceDistribution.Normal, 0),
                "sample");
        }

        [Fact]
        [Trait("Covers", "KolmogorovSmirnovTwoSampleTest")]
        public void KolmogorovSmirnovTwoSampleTestHandComputedStatisticIsHalf()
        {
            var result = ranks.KolmogorovSmirnovTwoSampleTest([1.0, 2, 3, 4, 5], [3.0, 4, 5, 6, 7, 8]);

            result.Statistic.Should().BeApproximately(0.5, Exact);
            result.PValue.Should().BeInRange(0.2, 0.5);
        }

        [Fact]
        [Trait("Covers", "KolmogorovSmirnovTwoSampleTest")]
        public void KolmogorovSmirnovTwoSampleTestDetectsDisjointSamplesAndRejectsEmpty()
        {
            var a = Enumerable.Range(0, 30).Select(i => (double)i).ToArray();
            var b = Enumerable.Range(100, 30).Select(i => (double)i).ToArray();

            var result = ranks.KolmogorovSmirnovTwoSampleTest(a, b);

            result.Statistic.Should().Be(1);
            result.PValue.Should().BeLessThan(1e-6);
            Throws(() => ranks.KolmogorovSmirnovTwoSampleTest([], b), "sample1");
        }

        [Fact]
        [Trait("Covers", "ShapiroWilkNormalityTest")]
        public void ShapiroWilkNormalityTestMatchesThePaperExampleWithinApproximationError()
        {
            var result = ranks.ShapiroWilkNormalityTest(shapiroWilkPaperSample);

            result.Statistic.Should().BeApproximately(0.7888, 0.005);
            result.PValue.Should().BeInRange(0.003, 0.015);
            result.RejectNull.Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "ShapiroWilkNormalityTest")]
        public void ShapiroWilkNormalityTestAcceptsNormalQuantilesAndRejectsBadInput()
        {
            var result = ranks.ShapiroWilkNormalityTest(NormalQuantileSample(40));

            result.Statistic.Should().BeGreaterThan(0.98);
            result.PValue.Should().BeGreaterThan(0.5);
            Throws(() => ranks.ShapiroWilkNormalityTest([1.0, 2.0, 3.0]), "sample");
            Throws(() => ranks.ShapiroWilkNormalityTest([2.0, 2.0, 2.0, 2.0]), "sample");
        }

        [Fact]
        [Trait("Covers", "AndersonDarlingNormalityTest")]
        public void AndersonDarlingNormalityTestMatchesHandComputedStatistic()
        {
            var result = ranks.AndersonDarlingNormalityTest(shapiroWilkPaperSample);

            result.Statistic.Should().BeApproximately(0.9467718795988862, 1e-5);
            result.PValue.Should().BeApproximately(0.0104540, 0.005);
            result.RejectNull.Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "AndersonDarlingNormalityTest")]
        public void AndersonDarlingNormalityTestAcceptsNormalQuantilesAndRejectsBadInput()
        {
            ranks.AndersonDarlingNormalityTest(NormalQuantileSample(60)).PValue.Should().BeGreaterThan(0.5);
            Throws(() => ranks.AndersonDarlingNormalityTest([1.0, 2.0, 3.0]), "sample");
            Throws(() => ranks.AndersonDarlingNormalityTest([.. Enumerable.Repeat(1.0, 10)]), "sample");
        }

        [Fact]
        [Trait("Covers", "LillieforsNormalityTest")]
        public void LillieforsNormalityTestRejectsSkewedDataAndAcceptsNormalQuantiles()
        {
            var skewed = Enumerable.Range(1, 50).Select(i => -Math.Log(1 - (i - 0.5) / 50)).ToArray();

            ranks.LillieforsNormalityTest(skewed, 4000).PValue.Should().BeLessThan(0.01);
            ranks.LillieforsNormalityTest(NormalQuantileSample(50), 4000).PValue.Should().BeGreaterThan(0.3);
            ranks.LillieforsNormalityTest(skewed, 2000).Warnings.Should().Contain(w => w.Contains("Monte Carlo"));
        }

        [Fact]
        [Trait("Covers", "LillieforsNormalityTest")]
        public void LillieforsNormalityTestRejectsBadInput()
        {
            Throws(() => ranks.LillieforsNormalityTest(NormalQuantileSample(10), 10), "iterations");
            Throws(() => ranks.LillieforsNormalityTest([1.0, 2.0]), "sample");
            Throws(() => ranks.LillieforsNormalityTest([.. Enumerable.Repeat(1.0, 10)]), "sample");
        }

        [Fact]
        [Trait("Covers", "JarqueBeraNormalityTest")]
        public void JarqueBeraNormalityTestMatchesClosedForm()
        {
            var result = ranks.JarqueBeraNormalityTest(shapiroWilkPaperSample);

            result.EstimatedValue.Should().BeApproximately(1.678543901586165, 1e-9);
            result.Statistic.Should().BeApproximately(6.982848237344646, 1e-9);
            result.PValue.Should().BeApproximately(0.030457466224581887, 1e-9);
            result.DegreesOfFreedom.Should().Be(2);
            result.Warnings.Should().NotBeEmpty();
        }

        [Fact]
        [Trait("Covers", "JarqueBeraNormalityTest")]
        public void JarqueBeraNormalityTestRejectsBadInput()
        {
            Throws(() => ranks.JarqueBeraNormalityTest([1.0, 2.0, 3.0]), "sample");
            Throws(() => ranks.JarqueBeraNormalityTest([.. Enumerable.Repeat(1.0, 10)]), "sample");
            ranks.JarqueBeraNormalityTest(NormalQuantileSample(200)).PValue.Should().BeGreaterThan(0.9);
        }

        [Fact]
        [Trait("Covers", "ChiSquareDistributionFitTest")]
        public void ChiSquareDistributionFitTestAcceptsEvenlySpreadUniformData()
        {
            var sample = Enumerable.Range(0, 100).Select(i => (i + 0.5) / 100).ToArray();

            var result = ranks.ChiSquareDistributionFitTest(sample, HypothesisReferenceDistribution.Uniform, 0);

            result.PValue.Should().BeGreaterThan(0.9);
            result.RejectNull.Should().BeFalse();
        }

        [Fact]
        [Trait("Covers", "ChiSquareDistributionFitTest")]
        public void ChiSquareDistributionFitTestRejectsWrongReferenceAndBadInput()
        {
            var sample = Enumerable.Range(0, 100).Select(i => (i + 0.5) / 100).ToArray();

            ranks.ChiSquareDistributionFitTest(sample, HypothesisReferenceDistribution.Normal, 0.5, 0.05)
                .RejectNull.Should().BeTrue();

            Throws(
                () => ranks.ChiSquareDistributionFitTest([1.0, 2.0], HypothesisReferenceDistribution.Uniform,
                    0), "sample");
        }

        [Fact]
        [Trait("Covers", "KruskalWallisTest")]
        public void KruskalWallisTestMatchesPublishedHollanderWolfeExample()
        {
            var groups = new[]
            {
                new[] { 2.9, 3.0, 2.5, 2.6, 3.2 },
                new[] { 3.8, 2.7, 4.0, 2.4 },
                new[] { 2.8, 3.4, 3.7, 2.2, 2.0 }
            };

            var result = ranks.KruskalWallisTest(groups);

            result.Statistic.Should().BeApproximately(0.7714285714285722, 1e-9);
            result.PValue.Should().BeApproximately(0.6799647735788936, 1e-9);
            result.DegreesOfFreedom.Should().Be(2);
            result.EffectSize.Should().BeApproximately(0.7714285714285722 / 13, 1e-9);
            result.RejectNull.Should().BeFalse();
        }

        [Fact]
        [Trait("Covers", "KruskalWallisTest")]
        public void KruskalWallisTestAppliesTheTieCorrection()
        {
            var groups = new[]
            {
                new[] { 1.0, 2, 2, 3 },
                new[] { 2.0, 3, 3, 4 },
                new[] { 3.0, 4, 5, 5 }
            };

            var result = ranks.KruskalWallisTest(groups);

            result.Statistic.Should().BeApproximately(6.630555555555558, 1e-9);
            result.PValue.Should().BeApproximately(0.036323957187715195, 1e-9);
            result.RejectNull.Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "KruskalWallisTest")]
        public void KruskalWallisTestRejectsBadInput()
        {
            Throws(() => ranks.KruskalWallisTest([[1.0, 2.0]]), "groups");
            Throws(() => ranks.KruskalWallisTest([[1.0, 1.0], [1.0]]), "groups");
            Throws(() => ranks.KruskalWallisTest([[1.0], []]), "groups");
        }

        private static readonly double[][] poisonArrays =
        [
            [],
            [double.NaN],
            [1.0],
            [1.0, 1.0, 1.0],
            [1.0, 2.0, 3.0, 4.0, 5.0],
            [1e300, -1e300, 1e300, 2.0, 5.0, 6.0, 1.0, 2.0, 9.0],
            [1.0, double.PositiveInfinity, 3.0, 4.0],
            [0.5, 0.25, 0.75, 0.1, 0.9, 0.3, 0.6, 0.2, 0.8, 0.4, 0.55, 0.65],
            [12.1, 9.8, 11.4, 10.2, 13.3, 8.9, 10.7, 12.6, 9.4, 11.1, 10.0, 12.9, 9.9, 11.8]
        ];

        private static readonly int[][] poisonTables =
        [
            [],
            [0],
            [0, 0, 0, 0],
            [1, 2, 3, 4],
            [-1, 2, 3, 4],
            [int.MaxValue, int.MaxValue, 1, 1]
        ];

        private static object? PickArgument(ParameterInfo parameter, Random random)
        {
            var type = parameter.ParameterType;
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(double[]))
            {
                return poisonArrays[random.Next(poisonArrays.Length)];
            }

            if (type == typeof(double[][]))
            {
                var groups = random.Next(0, 4);

                return Enumerable.Range(0, groups).Select(_ => poisonArrays[random.Next(poisonArrays.Length)])
                    .ToArray();
            }

            if (type == typeof(int[]))
            {
                var length = random.Next(0, 14);

                return Enumerable.Range(0, length).Select(_ => random.Next(-1, 3)).ToArray();
            }

            if (type == typeof(bool[]))
            {
                return Enumerable.Range(0, random.Next(0, 8)).Select(_ => random.Next(2) == 0).ToArray();
            }

            if (type == typeof(int[][]))
            {
                var rows = random.Next(0, 4);

                return Enumerable.Range(0, rows).Select(_ => poisonTables[random.Next(poisonTables.Length)]).ToArray();
            }

            if (type == typeof(int[][][]))
            {
                var tables = random.Next(0, 3);

                return Enumerable.Range(0, tables)
                    .Select(_ => Enumerable.Range(0, random.Next(1, 4))
                        .Select(_ => poisonTables[random.Next(poisonTables.Length)]).ToArray())
                    .ToArray();
            }

            if (underlying.IsEnum)
            {
                var values = Enum.GetValues(underlying);

                return values.GetValue(random.Next(values.Length));
            }

            if (underlying == typeof(int))
            {
                return new[] { -1, 0, 1, 2, 5, 30, 100, 1000000 }[random.Next(8)];
            }

            if (underlying == typeof(double))
            {
                return new[]
                    { -1.0, 0.0, 0.05, 0.5, 0.95, 1.0, 2.5, 1e300, double.NaN, double.PositiveInfinity }[
                    random.Next(10)];
            }

            if (underlying == typeof(bool))
            {
                return random.Next(2) == 0;
            }

            if (type == typeof(string))
            {
                return "value";
            }

            return parameter.HasDefaultValue ? parameter.DefaultValue : null;
        }

        [Fact]
        public void EveryHypothesisToolFailsOnlyWithArgumentExceptionsWhateverTheInput()
        {
            var descriptors = AdvancedAnalyticsToolCatalogue.Discover(typeof(HypothesisTestSelectionTools).Assembly)
                .Where(d => d.GroupName == "HypothesisTests")
                .ToList();

            var seed = int.TryParse(Environment.GetEnvironmentVariable("HypothesisToolFuzzSeed"), out var configured)
                ? configured
                : 20260921;

            var random = new Random(seed);
            var unexpected = new List<string>();

            foreach (var descriptor in descriptors)
            {
                var instance = Activator.CreateInstance(descriptor.GroupType);
                var parameters = descriptor.Method.GetParameters();

                for (var attempt = 0; attempt < 400; attempt++)
                {
                    var arguments = parameters.Select(p => PickArgument(p, random)).ToArray();

                    try
                    {
                        var result = descriptor.Method.Invoke(instance, arguments);
                        result.Should().NotBeNull();

                        var explanation = result!.GetType().GetProperty("Interpretation") ??
                                          result.GetType().GetProperty("Reason");

                        var interpretation = explanation?.GetValue(result) as string;
                        interpretation.Should().NotBeNullOrWhiteSpace($"{descriptor.ToolName} must explain its result");
                    }
                    catch (TargetInvocationException exception) when (exception.InnerException is ArgumentException)
                    {
                    }
                    catch (TargetInvocationException exception)
                    {
                        unexpected.Add(
                            $"{descriptor.ToolName}: {exception.InnerException?.GetType().Name}: {exception.InnerException?.Message}");

                        break;
                    }
                }
            }

            unexpected.Should().BeEmpty();
        }
    }
}
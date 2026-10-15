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
using Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class CategoricalAndSurvivalToolsTests
    {
        private const double Tight = 1e-9;
        private static readonly double[][] twoByTwoCounts = [[10d, 20d], [30d, 40d]];

        private static readonly double[][] agrestiPartyIdentification =
            [[762d, 327d, 468d], [484d, 239d, 477d]];

        private static readonly double[][] agrestiJobSatisfactionByIncome =
        [
            [1d, 3d, 10d, 6d],
            [2d, 3d, 10d, 7d],
            [1d, 6d, 14d, 12d],
            [0d, 1d, 9d, 11d]
        ];

        private readonly ContingencyAssociationTools contingency = new();
        private readonly OddsAndRiskTools odds = new();
        private readonly RateAndInformationValueTools rates = new();

        [Fact]
        [Trait("Covers", "CrossTabulateCategories")]
        public void CrossTabulateCategories_CountsCellsAndSkipsBlankLabels()
        {
            var result = contingency.CrossTabulateCategories(
                ["a", "a", "b", "b", "b", ""], ["x", "y", "x", "x", "y", "x"]);

            result.RowLabels.Should().Equal("a", "b");
            result.ColumnLabels.Should().Equal("x", "y");
            result.Counts.Should().BeEquivalentTo([[1, 1], new[] { 2, 1 }]);
            result.RowTotals.Should().Equal(2, 3);
            result.ColumnTotals.Should().Equal(3, 2);
            result.Total.Should().Be(5);
            result.SkippedPairs.Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "CrossTabulateCategories")]
        public void CrossTabulateCategories_RejectsMismatchedLengthsAndAllBlank()
        {
            Action mismatch = () => contingency.CrossTabulateCategories(["a"], ["x", "y"]);
            Action blank = () => contingency.CrossTabulateCategories(["", " "], ["x", "y"]);

            mismatch.Should().Throw<ArgumentException>().WithParameterName("rowCategories");
            blank.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "AnalyseContingencyTableAssociation")]
        public void AnalyseContingencyTableAssociation_TwoByTwoMatchesHandComputation()
        {
            var result = contingency.AnalyseContingencyTableAssociation(twoByTwoCounts);

            result.Expected.Should().BeEquivalentTo([[12d, 18d], new[] { 28d, 42d }]);
            result.ChiSquare.Should().BeApproximately(0.793650793650, Tight);
            result.DegreesOfFreedom.Should().Be(1);
            result.Phi.Should().BeApproximately(0.0890870806, Tight);
            result.SignedPhi.Should().BeApproximately(-0.0890870806, Tight);
            result.CramersV.Should().BeApproximately(0.0890870806, Tight);
            result.ContingencyCoefficient.Should().BeApproximately(Math.Sqrt(0.793650793650 / 100.793650793650), Tight);
            result.PearsonResiduals[0][0].Should().BeApproximately(-2d / Math.Sqrt(12d), Tight);
            result.PValue.Should().BeApproximately(0.3730, 0.001);
            result.SmallestExpectedCount.Should().BeApproximately(12d, Tight);
            result.YatesChiSquare.Should().BeLessThan(result.ChiSquare);
        }

        [Fact]
        [Trait("Covers", "AnalyseContingencyTableAssociation")]
        public void AnalyseContingencyTableAssociation_MatchesAgrestiPartyIdentificationTable()
        {
            var result = contingency.AnalyseContingencyTableAssociation(agrestiPartyIdentification,
                ["Female", "Male"], ["Democrat", "Independent", "Republican"]);

            result.ChiSquare.Should().BeApproximately(30.1, 0.1);
            result.LikelihoodRatioChiSquare.Should().BeApproximately(30.0, 0.1);
            result.DegreesOfFreedom.Should().Be(2);
            result.PValue.Should().BeApproximately(Math.Exp(-result.ChiSquare / 2d), 1e-12);
            result.CramersV.Should().BeApproximately(Math.Sqrt(result.ChiSquare / 2757d), Tight);
            result.CramersVBiasCorrected.Should().BeLessThan(result.CramersV);
            result.RowLabels.Should().Equal("Female", "Male");
            result.Warnings.Should().BeEmpty();
        }

        [Fact]
        [Trait("Covers", "AnalyseContingencyTableAssociation")]
        public void AnalyseContingencyTableAssociation_RejectsBadTablesAndWarnsOnSparseCells()
        {
            Action negative = () =>
                contingency.AnalyseContingencyTableAssociation([[1d, -1d], [1d, 1d]]);

            Action ragged = () =>
                contingency.AnalyseContingencyTableAssociation([[1d, 1d], [1d]]);

            Action emptyRow = () =>
                contingency.AnalyseContingencyTableAssociation([[0d, 0d], [1d, 1d]]);

            Action oneRow = () => contingency.AnalyseContingencyTableAssociation([[1d, 1d]]);

            Action nan = () =>
                contingency.AnalyseContingencyTableAssociation([[double.NaN, 1d], [1d, 1d]]);

            negative.Should().Throw<ArgumentException>().WithParameterName("counts");
            ragged.Should().Throw<ArgumentException>();
            emptyRow.Should().Throw<ArgumentException>().WithMessage("*no observations*");
            oneRow.Should().Throw<ArgumentException>();
            nan.Should().Throw<ArgumentException>();

            contingency.AnalyseContingencyTableAssociation([[1d, 2d], [2d, 1d]]).Warnings
                .Should().NotBeEmpty();
        }

        [Fact]
        [Trait("Covers", "AnalyseContingencyTableAssociation")]
        public void AnalyseContingencyTableAssociation_HugeCountsRemainFinite()
        {
            var result = contingency.AnalyseContingencyTableAssociation(
                [[1e12, 2e12], [3e12, 4e12]]);

            result.ChiSquare.Should().BeApproximately(0.793650793650 * 1e11, 1d);
            result.CramersV.Should().BeApproximately(0.0890870806, 1e-8);
        }

        [Fact]
        [Trait("Covers", "AnalyseCategoricalAssociationFromLabels")]
        public void AnalyseCategoricalAssociationFromLabels_CrossTabulatesThenAnalyses()
        {
            var rows = Enumerable.Repeat("a", 30).Concat(Enumerable.Repeat("b", 70)).ToArray();

            var columns = Enumerable.Repeat("x", 10).Concat(Enumerable.Repeat("y", 20))
                .Concat(Enumerable.Repeat("x", 30)).Concat(Enumerable.Repeat("y", 40)).ToArray();

            var result = contingency.AnalyseCategoricalAssociationFromLabels(rows, columns);

            result.ChiSquare.Should().BeApproximately(0.793650793650, Tight);
            result.RowLabels.Should().Equal("a", "b");
            result.ColumnLabels.Should().Equal("x", "y");
        }

        [Fact]
        [Trait("Covers", "AnalyseCategoricalAssociationFromLabels")]
        public void AnalyseCategoricalAssociationFromLabels_ConstantVariableIsRejected()
        {
            Action act = () => contingency.AnalyseCategoricalAssociationFromLabels(["a", "a", "a"],
                ["x", "y", "x"]);

            act.Should().Throw<ArgumentException>().WithMessage("*constant*");
        }

        [Fact]
        [Trait("Covers", "RankCategoricalColumnsByAssociationWithOutcome")]
        public void RankCategoricalColumnsByAssociationWithOutcome_RanksInformativeColumnAboveNoise()
        {
            var labels = Enumerable.Range(0, 40).Select(i => i % 4 < 2 ? 1 : 0).ToArray();

            var rows = Enumerable.Range(0, 40)
                .Select(i => new[] { labels[i] == 1 ? "fraud" : "clean", i % 2 == 0 ? "a" : "b", "same" }).ToArray();

            var result =
                contingency.RankCategoricalColumnsByAssociationWithOutcome(rows, ["signal", "noise", "flat"],
                    labels);

            result.Columns.Select(c => c.Column).Should().Equal("signal", "noise", "flat");
            result.Columns[0].CramersV.Should().BeApproximately(1d, Tight);
            result.Columns[0].ChiSquare.Should().BeApproximately(40d, Tight);
            result.Columns[0].PValue.Should().BeLessThan(1e-8);
            result.Columns[0].AdjustedPValue.Should().BeApproximately(result.Columns[0].PValue * 2d, 1e-12);
            result.Columns[0].MutualInformationBits.Should().BeApproximately(1d, Tight);
            result.Columns[1].CramersV.Should().BeApproximately(0d, Tight);
            result.Columns[1].PValue.Should().BeApproximately(1d, Tight);
            double.IsNaN(result.Columns[2].CramersV).Should().BeTrue();
            result.Columns[2].Note.Should().Contain("fewer than two");
            result.TotalEvents.Should().Be(20);
        }

        [Fact]
        [Trait("Covers", "RankCategoricalColumnsByAssociationWithOutcome")]
        public void
            RankCategoricalColumnsByAssociationWithOutcome_RejectsRaggedRowsDuplicateNamesAndSingleClassOutcome()
        {
            Action ragged = () => contingency.RankCategoricalColumnsByAssociationWithOutcome(
                [["a"], ["a", "b"]], ["x"], [0, 1]);

            Action duplicate = () => contingency.RankCategoricalColumnsByAssociationWithOutcome(
                [["a", "b"]], ["x", "x"], [0]);

            var constantOutcome = contingency.RankCategoricalColumnsByAssociationWithOutcome(
                [["a"], ["b"]], ["x"], [1, 1]);

            ragged.Should().Throw<ArgumentException>().WithParameterName("rows");
            duplicate.Should().Throw<ArgumentException>().WithParameterName("columnNames");
            constantOutcome.Columns[0].Note.Should().Contain("single class");
        }

        [Fact]
        [Trait("Covers", "CalculateGoodmanKruskalLambda")]
        public void CalculateGoodmanKruskalLambda_MatchesHandComputation()
        {
            var result = contingency.CalculateGoodmanKruskalLambda([[20d, 10d], [5d, 15d]]);

            result.LambdaColumnGivenRow.Should().BeApproximately(0.4, Tight);
            result.StandardErrorColumnGivenRow.Should().BeApproximately(Math.Sqrt(15d * 20d / 15625d), Tight);
            result.LambdaRowGivenColumn.Should().BeApproximately(0.25, Tight);
            result.StandardErrorRowGivenColumn.Should().BeApproximately(Math.Sqrt(15d * 25d / 8000d), Tight);
            result.LambdaSymmetric.Should().BeApproximately(1d / 3d, Tight);
        }

        [Fact]
        [Trait("Covers", "CalculateGoodmanKruskalLambda")]
        public void CalculateGoodmanKruskalLambda_IndependenceIsZeroAndDegenerateIsNaN()
        {
            contingency.CalculateGoodmanKruskalLambda([[10d, 10d], [10d, 10d]])
                .LambdaSymmetric.Should().BeApproximately(0d, Tight);

            var oneColumn = contingency.CalculateGoodmanKruskalLambda([[5d, 0d], [7d, 0d]]);
            double.IsNaN(oneColumn.LambdaColumnGivenRow).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "CalculateOrdinalAssociationMeasures")]
        public void CalculateOrdinalAssociationMeasures_MatchesAgrestiJobSatisfactionGamma()
        {
            var result = contingency.CalculateOrdinalAssociationMeasures(agrestiJobSatisfactionByIncome);

            result.ConcordantPairs.Should().BeApproximately(1331d, Tight);
            result.DiscordantPairs.Should().BeApproximately(849d, Tight);
            result.Gamma.Should().BeApproximately(0.221, 0.0005);
            result.GammaStandardError.Should().BeApproximately(0.117, 0.0015);
            result.GammaLower.Should().BeLessThan(result.Gamma);
            result.GammaUpper.Should().BeGreaterThan(result.Gamma);
        }

        [Fact]
        [Trait("Covers", "CalculateOrdinalAssociationMeasures")]
        public void CalculateOrdinalAssociationMeasures_TwoByTwoReducesToYuleQAndPhi()
        {
            var result =
                contingency.CalculateOrdinalAssociationMeasures([[20d, 10d], [5d, 15d]]);

            result.Gamma.Should().BeApproximately(250d / 350d, Tight);
            result.GammaStandardError.Should().BeApproximately(0.15808095290642498, 1e-8);
            result.KendallTauB.Should().BeApproximately(0.4082482904638631, 1e-9);
            result.SomersDSymmetric.Should().BeApproximately(result.KendallTauB, 0.2);
        }

        [Fact]
        [Trait("Covers", "CalculateOrdinalAssociationMeasures")]
        public void CalculateOrdinalAssociationMeasures_ReversedOrderIsNegativeOne()
        {
            var result =
                contingency.CalculateOrdinalAssociationMeasures([[0d, 10d], [10d, 0d]]);

            result.Gamma.Should().BeApproximately(-1d, Tight);
            Action bad = () => contingency.CalculateOrdinalAssociationMeasures(twoByTwoCounts, 1.5);
            bad.Should().Throw<ArgumentException>().WithParameterName("confidenceLevel");
        }

        [Fact]
        [Trait("Covers", "CalculateCategoricalMutualInformation")]
        public void CalculateCategoricalMutualInformation_PerfectDependenceIsOneBit()
        {
            var perfect =
                contingency.CalculateCategoricalMutualInformation([[10d, 0d], [0d, 10d]]);
            var independent =
                contingency.CalculateCategoricalMutualInformation([[10d, 10d], [10d, 10d]]);

            var nats = contingency.CalculateCategoricalMutualInformation(
                [[10d, 0d], [0d, 10d]], false);

            perfect.MutualInformation.Should().BeApproximately(1d, Tight);
            perfect.NormalisedMutualInformation.Should().BeApproximately(1d, Tight);
            perfect.RowEntropy.Should().BeApproximately(1d, Tight);
            perfect.Unit.Should().Be("bits");
            independent.MutualInformation.Should().BeApproximately(0d, Tight);
            nats.MutualInformation.Should().BeApproximately(Math.Log(2d), Tight);
            nats.Unit.Should().Be("nats");
        }

        [Fact]
        [Trait("Covers", "CalculateOddsRatio")]
        public void CalculateOddsRatio_MatchesWoolfIntervalByHand()
        {
            var result = odds.CalculateOddsRatio(10, 20, 5, 40);

            result.OddsRatio.Should().BeApproximately(4d, Tight);
            result.LogStandardError.Should().BeApproximately(Math.Sqrt(0.375), Tight);
            result.Lower.Should().BeApproximately(1.2045022873, 1e-7);
            result.Upper.Should().BeApproximately(13.2834949080, 1e-6);
            result.ZeroCellCorrectionApplied.Should().BeFalse();
            result.PValue.Should().BeApproximately(0.0236, 0.0005);
        }

        [Fact]
        [Trait("Covers", "CalculateOddsRatio")]
        public void CalculateOddsRatio_ZeroCellUsesHaldaneCorrectionOnlyWhenAsked()
        {
            var corrected = odds.CalculateOddsRatio(0, 10, 5, 5);
            var raw = odds.CalculateOddsRatio(0, 10, 5, 5, 0.95, false);

            corrected.ZeroCellCorrectionApplied.Should().BeTrue();
            corrected.OddsRatio.Should().BeApproximately(0.5 * 5.5 / (10.5 * 5.5), Tight);
            raw.OddsRatio.Should().Be(0d);
        }

        [Fact]
        [Trait("Covers", "CalculateOddsRatio")]
        public void CalculateOddsRatio_RejectsBadInput()
        {
            Action negative = () => odds.CalculateOddsRatio(-1, 2, 3, 4);
            Action emptyGroup = () => odds.CalculateOddsRatio(0, 0, 3, 4);
            Action level = () => odds.CalculateOddsRatio(1, 2, 3, 4, 1.0);

            negative.Should().Throw<ArgumentException>().WithParameterName("exposedEvents");
            emptyGroup.Should().Throw<ArgumentException>().WithMessage("*exposed group is empty*");
            level.Should().Throw<ArgumentException>().WithParameterName("confidenceLevel");
        }

        [Fact]
        [Trait("Covers", "CalculateRiskRatio")]
        public void CalculateRiskRatio_MatchesPhysiciansHealthStudyAspirinTrial()
        {
            var result = odds.CalculateRiskRatio(104, 11037 - 104, 189, 11034 - 189);

            result.ExposedRisk.Should().BeApproximately(104d / 11037d, Tight);
            result.RiskRatio.Should().BeApproximately(0.5501149812, 1e-9);
            result.LogStandardError.Should().BeApproximately(0.1213472563, 1e-8);
            result.Lower.Should().BeApproximately(0.4336731159, 1e-8);
            result.Upper.Should().BeApproximately(0.6978216575, 1e-8);
            result.RelativeRiskReduction.Should().BeApproximately(0.4498850187, 1e-8);
        }

        [Fact]
        [Trait("Covers", "CalculateRiskRatio")]
        public void CalculateRiskRatio_ZeroEventGroupIsCorrectedAndFlagged()
        {
            var result = odds.CalculateRiskRatio(0, 50, 5, 45);

            result.ZeroCellCorrectionApplied.Should().BeTrue();
            double.IsFinite(result.Upper).Should().BeTrue();
            result.RiskRatio.Should().BeGreaterThan(0d);
        }

        [Fact]
        [Trait("Covers", "CalculateRiskDifference")]
        public void CalculateRiskDifference_MatchesNewcombe1998ScoreIntervalExample()
        {
            var result = odds.CalculateRiskDifference(56, 14, 48, 32);

            result.RiskDifference.Should().BeApproximately(0.2, Tight);
            result.NewcombeLower.Should().BeApproximately(0.0524, 0.0001);
            result.NewcombeUpper.Should().BeApproximately(0.3339, 0.0001);
            result.WaldStandardError.Should().BeApproximately(0.072702918, 1e-8);
            result.ZStatistic.Should().BeApproximately(2.6501719513, 1e-7);
            result.AttributableFractionAmongExposed.Should().BeApproximately(0.25, 1e-9);
            result.PopulationAttributableFraction.Should().BeApproximately(0.1346153846, 1e-9);
        }

        [Fact]
        [Trait("Covers", "CalculateRiskDifference")]
        public void CalculateRiskDifference_IdenticalRatesGiveZeroDifference()
        {
            var result = odds.CalculateRiskDifference(10, 90, 10, 90);

            result.RiskDifference.Should().Be(0d);
            result.PValue.Should().BeApproximately(1d, Tight);
        }

        [Fact]
        [Trait("Covers", "CalculateNumberNeededToTreat")]
        public void CalculateNumberNeededToTreat_InvertsTheRiskDifference()
        {
            var result = odds.CalculateNumberNeededToTreat(56, 14, 48, 32);

            result.NumberNeeded.Should().BeApproximately(5d, 1e-9);
            result.Direction.Should().Contain("harm");
            result.IntervalSpansInfinity.Should().BeFalse();
            result.Lower.Should().BeApproximately(2.9952, 0.001);
            result.Upper.Should().BeApproximately(19.0725, 0.005);
        }

        [Fact]
        [Trait("Covers", "CalculateNumberNeededToTreat")]
        public void CalculateNumberNeededToTreat_NoDifferenceHasNoFiniteNumber()
        {
            var result = odds.CalculateNumberNeededToTreat(10, 90, 10, 90);

            double.IsNaN(result.NumberNeeded).Should().BeTrue();
            result.IntervalSpansInfinity.Should().BeTrue();
            odds.CalculateNumberNeededToTreat(10, 90, 20, 80).Direction.Should().Contain("treat");
        }

        [Fact]
        [Trait("Covers", "CalculateYuleAssociation")]
        public void CalculateYuleAssociation_MatchesClosedForms()
        {
            var result = odds.CalculateYuleAssociation(20, 10, 5, 15);

            result.YuleQ.Should().BeApproximately(250d / 350d, Tight);

            result.YuleQStandardError.Should()
                .BeApproximately(0.5 * (1 - Math.Pow(250d / 350d, 2)) * Math.Sqrt(1d / 20 + 1d / 10 + 1d / 5 + 1d / 15),
                    Tight);

            result.YuleY.Should().BeApproximately(0.4202041029, 1e-9);
            result.Phi.Should().BeApproximately(0.4082482905, 1e-9);
            result.OddsRatio.Should().BeApproximately(6d, Tight);
        }

        [Fact]
        [Trait("Covers", "CalculateYuleAssociation")]
        public void CalculateYuleAssociation_EmptyOffDiagonalGivesPerfectAssociationAndNaNStandardError()
        {
            var result = odds.CalculateYuleAssociation(10, 0, 0, 10);

            result.YuleQ.Should().BeApproximately(1d, Tight);
            double.IsNaN(result.YuleQStandardError).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "CalculateDiagnosticLikelihoodRatios")]
        public void CalculateDiagnosticLikelihoodRatios_MatchesBayesByHand()
        {
            var result = odds.CalculateDiagnosticLikelihoodRatios(90, 20, 80, 10, 0.95, 0.1);

            result.Sensitivity.Should().BeApproximately(0.9, Tight);
            result.Specificity.Should().BeApproximately(0.8, Tight);
            result.PositiveLikelihoodRatio.Should().BeApproximately(4.5, Tight);
            result.NegativeLikelihoodRatio.Should().BeApproximately(0.125, Tight);
            result.DiagnosticOddsRatio.Should().BeApproximately(36d, Tight);
            result.PostTestProbabilityAfterPositive.Should().BeApproximately(1d / 3d, Tight);
            result.PostTestProbabilityAfterNegative.Should().BeApproximately(0.0136986301, 1e-9);
            result.PositiveLikelihoodRatioLower.Should().BeLessThan(4.5);
            result.PositiveLikelihoodRatioUpper.Should().BeGreaterThan(4.5);
            result.PositivePredictiveValue.Should().BeApproximately(90d / 110d, Tight);
        }

        [Fact]
        [Trait("Covers", "CalculateDiagnosticLikelihoodRatios")]
        public void CalculateDiagnosticLikelihoodRatios_DefaultsPretestToPrevalenceAndRejectsBadPretest()
        {
            var result = odds.CalculateDiagnosticLikelihoodRatios(90, 20, 80, 10);
            Action bad = () => odds.CalculateDiagnosticLikelihoodRatios(90, 20, 80, 10, 0.95, 1.0);

            result.PretestProbability.Should().BeApproximately(100d / 200d, Tight);
            bad.Should().Throw<ArgumentException>().WithParameterName("pretestProbability");
        }

        [Fact]
        [Trait("Covers", "AnalyseTwoByTwoTable")]
        public void AnalyseTwoByTwoTable_CombinesEveryMeasure()
        {
            var result = odds.AnalyseTwoByTwoTable(10, 20, 5, 40);

            result.Odds.OddsRatio.Should().BeApproximately(4d, Tight);
            result.Risk.RiskRatio.Should().BeApproximately(3d, Tight);
            result.Difference.RiskDifference.Should().BeApproximately(1d / 3d - 1d / 9d, Tight);
            result.ChiSquare.Should().BeApproximately(50d / 9d, Tight);
            result.SmallestExpectedCount.Should().BeApproximately(6d, Tight);
            result.Yule.YuleQ.Should().BeApproximately(0.6, Tight);
        }

        [Fact]
        [Trait("Covers", "AnalyseTwoByTwoTable")]
        public void AnalyseTwoByTwoTable_RejectsEmptyEventColumn()
        {
            Action act = () => odds.AnalyseTwoByTwoTable(0, 20, 0, 40);

            act.Should().Throw<ArgumentException>().WithMessage("*no association*");
        }

        [Fact]
        [Trait("Covers", "AnalyseBinaryExposureAndOutcome")]
        public void AnalyseBinaryExposureAndOutcome_BuildsTheTableFromFlags()
        {
            var exposure = Enumerable.Repeat(1, 30).Concat(Enumerable.Repeat(0, 45)).ToArray();

            var outcome = Enumerable.Repeat(1, 10).Concat(Enumerable.Repeat(0, 20))
                .Concat(Enumerable.Repeat(1, 5)).Concat(Enumerable.Repeat(0, 40)).ToArray();

            var result = odds.AnalyseBinaryExposureAndOutcome(exposure, outcome);

            result.ExposedEvents.Should().Be(10);
            result.UnexposedNonEvents.Should().Be(40);
            result.Odds.OddsRatio.Should().BeApproximately(4d, Tight);
        }

        [Fact]
        [Trait("Covers", "AnalyseBinaryExposureAndOutcome")]
        public void AnalyseBinaryExposureAndOutcome_RejectsNonBinaryAndMismatched()
        {
            Action nonBinary = () => odds.AnalyseBinaryExposureAndOutcome([0, 2], [0, 1]);
            Action mismatch = () => odds.AnalyseBinaryExposureAndOutcome([0, 1], [0]);

            nonBinary.Should().Throw<ArgumentException>().WithParameterName("exposure");
            mismatch.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "CalculateMantelHaenszelOddsRatio")]
        public void CalculateMantelHaenszelOddsRatio_SingleStratumMatchesHandComputation()
        {
            var result = odds.CalculateMantelHaenszelOddsRatio([[10d, 20d, 5d, 40d]]);

            result.CommonOddsRatio.Should().BeApproximately(4d, Tight);
            result.CochranMantelHaenszelChiSquare.Should().BeApproximately(5.4815, 0.0002);
            result.CochranMantelHaenszelCorrectedChiSquare.Should().BeApproximately(4.1968, 0.0002);
            result.StrataUsed.Should().Be(1);
            double.IsNaN(result.BreslowDayChiSquare).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "CalculateMantelHaenszelOddsRatio")]
        public void CalculateMantelHaenszelOddsRatio_IdenticalStrataAreHomogeneousAndHeterogeneousOnesAreNot()
        {
            var same = odds.CalculateMantelHaenszelOddsRatio(
                [[10d, 20d, 5d, 40d], [10d, 20d, 5d, 40d]]);
            var different = odds.CalculateMantelHaenszelOddsRatio(
                [[30d, 10d, 10d, 30d], [10d, 30d, 30d, 10d]]);

            same.CommonOddsRatio.Should().BeApproximately(4d, Tight);
            same.CochranMantelHaenszelChiSquare.Should().BeApproximately(64d / (2d * 2.918918918918919), 1e-6);
            same.BreslowDayChiSquare.Should().BeApproximately(0d, 1e-6);
            same.BreslowDayPValue.Should().BeApproximately(1d, 1e-6);
            different.StratumOddsRatios.Should().BeEquivalentTo([9d, 1d / 9d]);
            different.BreslowDayPValue.Should().BeLessThan(0.001);
            different.BreslowDayTaronePValue.Should().BeLessThan(0.001);
        }

        [Fact]
        [Trait("Covers", "CalculateMantelHaenszelOddsRatio")]
        public void CalculateMantelHaenszelOddsRatio_RejectsMalformedStrata()
        {
            Action wrong = () => odds.CalculateMantelHaenszelOddsRatio([[1d, 2d, 3d]]);
            Action none = () => odds.CalculateMantelHaenszelOddsRatio([]);
            Action tiny = () => odds.CalculateMantelHaenszelOddsRatio([[1d, 0d, 0d, 0d]]);

            wrong.Should().Throw<ArgumentException>().WithMessage("*four counts*");
            none.Should().Throw<ArgumentException>();
            tiny.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(ProportionIntervalMethod.Wilson, 0.4901624715, 0.9433178485)]
        [InlineData(ProportionIntervalMethod.ClopperPearson, 0.4439045377, 0.9747892737)]
        [InlineData(ProportionIntervalMethod.AgrestiCoull, 0.4793675906, 0.9541127296)]
        [Trait("Covers", "CalculateBinomialProportionConfidenceInterval")]
        public void CalculateBinomialProportionConfidenceInterval_MatchesPublishedEightOfTenIntervals(
            ProportionIntervalMethod method, double lower, double upper)
        {
            var result = odds.CalculateBinomialProportionConfidenceInterval(8, 10, 0.95, method);

            result.Estimate.Should().BeApproximately(0.8, Tight);
            result.Lower.Should().BeApproximately(lower, 1e-6);
            result.Upper.Should().BeApproximately(upper, 1e-6);
        }

        [Fact]
        [Trait("Covers", "CalculateBinomialProportionConfidenceInterval")]
        public void CalculateBinomialProportionConfidenceInterval_WaldIsClippedAndEdgesAreHandled()
        {
            var wald = odds.CalculateBinomialProportionConfidenceInterval(8, 10, 0.95, ProportionIntervalMethod.Wald);
            var zero = odds.CalculateBinomialProportionConfidenceInterval(0, 10, 0.95,
                ProportionIntervalMethod.ClopperPearson);

            var noTrials = odds.CalculateBinomialProportionConfidenceInterval(0, 0);
            Action tooMany = () => odds.CalculateBinomialProportionConfidenceInterval(11, 10);

            wald.Lower.Should().BeApproximately(0.5521, 0.0002);
            wald.Upper.Should().Be(1d);
            zero.Lower.Should().Be(0d);
            zero.Upper.Should().BeApproximately(0.3084, 0.0005);
            double.IsNaN(noTrials.Estimate).Should().BeTrue();
            tooMany.Should().Throw<ArgumentException>().WithParameterName("events");
        }

        [Fact]
        [Trait("Covers", "CalculateInformationValueByCategory")]
        public void CalculateInformationValueByCategory_MatchesClosedFormWeightOfEvidence()
        {
            var categories = Enumerable.Repeat("A", 70).Concat(Enumerable.Repeat("B", 60))
                .Concat(Enumerable.Repeat("C", 70)).ToArray();

            var labels = Enumerable.Repeat(1, 10).Concat(Enumerable.Repeat(0, 60))
                .Concat(Enumerable.Repeat(1, 30)).Concat(Enumerable.Repeat(0, 30))
                .Concat(Enumerable.Repeat(1, 60)).Concat(Enumerable.Repeat(0, 10)).ToArray();

            var result = rates.CalculateInformationValueByCategory(categories, labels);

            result.Bins.Select(b => b.Label).Should().Equal("A", "B", "C");
            result.Bins[0].WeightOfEvidence.Should().BeApproximately(-Math.Log(6d), Tight);
            result.Bins[1].WeightOfEvidence.Should().BeApproximately(0d, Tight);
            result.Bins[2].WeightOfEvidence.Should().BeApproximately(Math.Log(6d), Tight);
            result.TotalInformationValue.Should().BeApproximately(Math.Log(6d), Tight);
            result.Strength.Should().Contain("very strong");
            result.SmoothingApplied.Should().BeFalse();
            result.WeightOfEvidenceIsMonotonic.Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "CalculateInformationValueByCategory")]
        public void CalculateInformationValueByCategory_ZeroCellIsSmoothedAndBlankIsSkipped()
        {
            var categories = new[] { "A", "A", "B", "B", "", "B" };
            var labels = new[] { 0, 0, 1, 0, 1, 0 };

            var result = rates.CalculateInformationValueByCategory(categories, labels);
            Action constant = () => rates.CalculateInformationValueByCategory(["A", "A"], [0, 1]);
            Action unsmoothed = () => rates.CalculateInformationValueByCategory(categories, labels, 0);

            result.SmoothingApplied.Should().BeTrue();
            result.SkippedRows.Should().Be(1);
            double.IsFinite(result.TotalInformationValue).Should().BeTrue();
            constant.Should().Throw<ArgumentException>();
            unsmoothed.Should().NotThrow();
        }

        [Fact]
        [Trait("Covers", "CalculateInformationValueByBins")]
        public void CalculateInformationValueByBins_SeparatedHalvesMatchSmoothedClosedForm()
        {
            var values = Enumerable.Range(1, 100).Select(v => (double)v).ToArray();
            var labels = values.Select(v => v > 50 ? 1 : 0).ToArray();

            var result = rates.CalculateInformationValueByBins(values, labels, 2);

            result.Bins.Should().HaveCount(2);
            result.Bins[0].Events.Should().Be(0);
            result.Bins[1].Events.Should().Be(50);
            result.Bins[0].WeightOfEvidence.Should().BeApproximately(-4.6151205168, 1e-8);
            result.TotalInformationValue.Should().BeApproximately(9.0492559154, 1e-7);
            result.SmoothingApplied.Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "CalculateInformationValueByBins")]
        public void CalculateInformationValueByBins_MissingValuesFormTheirOwnBin()
        {
            var values = new[] { 1d, 2d, 3d, 4d, double.NaN, double.NaN };
            var labels = new[] { 0, 0, 1, 1, 1, 0 };

            var result = rates.CalculateInformationValueByBins(values, labels, 2);
            Action badBins = () => rates.CalculateInformationValueByBins(values, labels, 1);

            result.Bins.Last().Label.Should().Be("Missing");
            result.Bins.Last().Count.Should().Be(2);
            badBins.Should().Throw<ArgumentException>().WithParameterName("bins");
        }

        [Fact]
        [Trait("Covers", "CalculatePopulationStabilityIndexFromCounts")]
        public void CalculatePopulationStabilityIndexFromCounts_MatchesHandComputation()
        {
            var shifted = rates.CalculatePopulationStabilityIndexFromCounts([50d, 50d], [70d, 30d]);
            var same = rates.CalculatePopulationStabilityIndexFromCounts([50d, 50d], [500d, 500d]);
            var empty = rates.CalculatePopulationStabilityIndexFromCounts([100d, 0d], [50d, 50d]);

            shifted.PopulationStabilityIndex.Should().BeApproximately(0.1694595721, 1e-9);
            shifted.Interpretation.Should().Contain("moderate");
            same.PopulationStabilityIndex.Should().BeApproximately(0d, Tight);
            empty.PopulationStabilityIndex.Should().BeApproximately(4.6043184667, 1e-7);
            empty.Interpretation.Should().Contain("significant");
        }

        [Fact]
        [Trait("Covers", "CalculatePopulationStabilityIndexFromCounts")]
        public void CalculatePopulationStabilityIndexFromCounts_RejectsBadInput()
        {
            Action lengths = () => rates.CalculatePopulationStabilityIndexFromCounts([1d, 2d], [1d]);
            Action zero = () => rates.CalculatePopulationStabilityIndexFromCounts([0d, 0d], [1d, 1d]);

            Action negative = () =>
                rates.CalculatePopulationStabilityIndexFromCounts([-1d, 2d], [1d, 1d]);

            lengths.Should().Throw<ArgumentException>();
            zero.Should().Throw<ArgumentException>();
            negative.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "CalculatePopulationStabilityIndexForNumericValues")]
        public void CalculatePopulationStabilityIndexForNumericValues_IdenticalSamplesAreStableAndShiftedOnesAreNot()
        {
            var baseline = Enumerable.Range(1, 100).Select(v => (double)v).ToArray();
            var shifted = Enumerable.Range(51, 100).Select(v => (double)v).ToArray();

            var stable = rates.CalculatePopulationStabilityIndexForNumericValues(baseline, baseline, 4);
            var moved = rates.CalculatePopulationStabilityIndexForNumericValues(baseline, shifted, 4);

            stable.PopulationStabilityIndex.Should().BeApproximately(0d, Tight);
            moved.PopulationStabilityIndex.Should().BeGreaterThan(0.25);
            moved.Bins.Sum(b => b.CurrentShare).Should().BeApproximately(1d, Tight);
        }

        [Fact]
        [Trait("Covers", "CalculatePopulationStabilityIndexForNumericValues")]
        public void CalculatePopulationStabilityIndexForNumericValues_MissingBinAndEmptyBaseline()
        {
            var baseline = new[] { 1d, 2d, 3d, 4d, double.NaN };
            var result = rates.CalculatePopulationStabilityIndexForNumericValues(baseline, baseline, 2);

            Action empty = () =>
                rates.CalculatePopulationStabilityIndexForNumericValues([double.NaN], baseline);

            result.Bins.Last().Label.Should().Be("Missing");
            empty.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "CalculatePopulationStabilityIndexForCategories")]
        public void CalculatePopulationStabilityIndexForCategories_MatchesCountsVersion()
        {
            var baseline = Enumerable.Repeat("a", 50).Concat(Enumerable.Repeat("b", 50)).ToArray();
            var current = Enumerable.Repeat("a", 70).Concat(Enumerable.Repeat("b", 30)).ToArray();

            var result = rates.CalculatePopulationStabilityIndexForCategories(baseline, current);

            result.PopulationStabilityIndex.Should().BeApproximately(0.1694595721, 1e-9);
            result.Bins.Select(b => b.Label).Should().Equal("a", "b");
        }

        [Fact]
        [Trait("Covers", "CalculatePopulationStabilityIndexForCategories")]
        public void CalculatePopulationStabilityIndexForCategories_NewCategoryAppearsAsBin()
        {
            var result = rates.CalculatePopulationStabilityIndexForCategories(["a", "a"], ["a", "z"]);
            Action blank = () => rates.CalculatePopulationStabilityIndexForCategories([""], [" "]);

            result.Bins.Select(b => b.Label).Should().Equal("a", "z");
            result.PopulationStabilityIndex.Should().BeGreaterThan(0.25);
            blank.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "TabulateEventRateBySegment")]
        public void TabulateEventRateBySegment_MatchesHandComputedRatesLiftAndHomogeneity()
        {
            var segments = Enumerable.Repeat("A", 100).Concat(Enumerable.Repeat("B", 100)).ToArray();

            var events = Enumerable.Repeat(1, 10).Concat(Enumerable.Repeat(0, 90))
                .Concat(Enumerable.Repeat(1, 30)).Concat(Enumerable.Repeat(0, 70)).ToArray();

            var result = rates.TabulateEventRateBySegment(segments, events);

            result.Segments.Select(s => s.Segment).Should().Equal("B", "A");
            result.OverallEventRate.Should().BeApproximately(0.2, Tight);
            var a = result.Segments[1];
            a.EventRate.Should().BeApproximately(0.1, Tight);
            a.Lower.Should().BeApproximately(0.0552291371, 1e-8);
            a.Upper.Should().BeApproximately(0.1743656615, 1e-8);
            a.LiftVersusOverall.Should().BeApproximately(0.5, Tight);
            a.ZVersusRest.Should().BeApproximately(-0.2 / Math.Sqrt(0.0032), 1e-9);
            a.ShareOfEvents.Should().BeApproximately(0.25, Tight);
            result.HomogeneityChiSquare.Should().BeApproximately(12.5, Tight);
            result.HomogeneityDegreesOfFreedom.Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "TabulateEventRateBySegment")]
        public void TabulateEventRateBySegment_FlagsSmallSamplesAndHandlesNoEvents()
        {
            var result = rates.TabulateEventRateBySegment(["A", "A", "B"], [0, 0, 0]);
            Action blank = () => rates.TabulateEventRateBySegment([""], [1]);

            result.Segments.Should().OnlyContain(s => s.SmallSample);
            double.IsNaN(result.HomogeneityChiSquare).Should().BeTrue();
            blank.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "TabulateAlertHitRatesBySegment")]
        public void TabulateAlertHitRatesBySegment_ComputesAlertHitAndDetectionRates()
        {
            var segments = Enumerable.Repeat("X", 10).ToArray();
            var alerted = new[] { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 };
            var actual = new[] { 1, 0, 1, 1, 0, 0, 0, 0, 0, 0 };

            var result = rates.TabulateAlertHitRatesBySegment(segments, alerted, actual);

            var row = result.Segments.Single();
            row.AlertRate.Should().BeApproximately(0.3, Tight);
            row.HitRate.Should().BeApproximately(2d / 3d, Tight);
            row.DetectionRate.Should().BeApproximately(2d / 3d, Tight);
            row.FalsePositivesPerTrueHit.Should().BeApproximately(0.5, Tight);
            row.MissedEvents.Should().Be(1);
            result.Overall.Alerts.Should().Be(3);
        }

        [Fact]
        [Trait("Covers", "TabulateAlertHitRatesBySegment")]
        public void TabulateAlertHitRatesBySegment_NoAlertsGiveZeroHitRateAndRejectsBadFlags()
        {
            var result = rates.TabulateAlertHitRatesBySegment(["X", "X"], [0, 0], [1, 0]);
            Action bad = () => rates.TabulateAlertHitRatesBySegment(["X"], [2], [1]);

            double.IsNaN(result.Overall.HitRate).Should().BeTrue();
            result.Overall.DetectionRate.Should().Be(0d);
            bad.Should().Throw<ArgumentException>().WithParameterName("alerted");
        }

        [Fact]
        [Trait("Covers", "CalculatePoissonRateConfidenceInterval")]
        public void CalculatePoissonRateConfidenceInterval_MatchesExactGarwoodLimits()
        {
            var ten = rates.CalculatePoissonRateConfidenceInterval(10, 1);
            var zero = rates.CalculatePoissonRateConfidenceInterval(0, 2);
            var scaled = rates.CalculatePoissonRateConfidenceInterval(5, 15);

            ten.Lower.Should().BeApproximately(4.7953886961, 1e-5);
            ten.Upper.Should().BeApproximately(18.3903560420, 1e-5);
            zero.Lower.Should().Be(0d);
            zero.Upper.Should().BeApproximately(3.6888794541 / 2d, 1e-5);
            scaled.Lower.Should().BeApproximately(1.6234863901 / 15d, 1e-6);
            scaled.Upper.Should().BeApproximately(11.6683320793 / 15d, 1e-6);
        }

        [Fact]
        [Trait("Covers", "CalculatePoissonRateConfidenceInterval")]
        public void CalculatePoissonRateConfidenceInterval_RejectsBadInput()
        {
            Action fraction = () => rates.CalculatePoissonRateConfidenceInterval(1.5, 1);
            Action exposure = () => rates.CalculatePoissonRateConfidenceInterval(1, 0);
            Action negative = () => rates.CalculatePoissonRateConfidenceInterval(-1, 1);

            fraction.Should().Throw<ArgumentException>().WithParameterName("events");
            exposure.Should().Throw<ArgumentException>().WithParameterName("exposure");
            negative.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ComparePoissonRates")]
        public void ComparePoissonRates_MatchesExactBinomialByHand()
        {
            var result = rates.ComparePoissonRates(10, 1, 5, 1);

            result.RateRatio.Should().BeApproximately(2d, Tight);
            result.PValue.Should().BeApproximately(0.3017578125, 1e-9);
            result.PValueMethod.Should().Contain("exact");
            result.RateRatioLower.Should().BeApproximately(0.62286, 0.0005);
            result.RateRatioUpper.Should().BeApproximately(7.4573, 0.005);
            result.RateDifference.Should().BeApproximately(5d, Tight);
        }

        [Fact]
        [Trait("Covers", "ComparePoissonRates")]
        public void ComparePoissonRates_ZeroEventGroupsAreHandled()
        {
            var noSecond = rates.ComparePoissonRates(3, 1, 0, 1);
            var neither = rates.ComparePoissonRates(0, 1, 0, 1);

            double.IsNaN(noSecond.RateRatio).Should().BeTrue();
            double.IsNaN(noSecond.RateRatioUpper).Should().BeTrue();
            noSecond.RateRatioLower.Should().BeGreaterThan(0d);
            double.IsNaN(neither.PValue).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "TestPoissonDispersion")]
        public void TestPoissonDispersion_MatchesChiSquareClosedFormForTwoDegreesOfFreedom()
        {
            var result = rates.TestPoissonDispersion([2d, 4d, 6d]);

            result.Mean.Should().BeApproximately(4d, Tight);
            result.Variance.Should().BeApproximately(4d, Tight);
            result.VarianceToMeanRatio.Should().BeApproximately(1d, Tight);
            result.ChiSquare.Should().BeApproximately(2d, Tight);
            result.DegreesOfFreedom.Should().Be(2);
            result.PValueOverdispersion.Should().BeApproximately(Math.Exp(-1d), 1e-9);
            result.Interpretation.Should().Contain("consistent");
        }

        [Fact]
        [Trait("Covers", "TestPoissonDispersion")]
        public void TestPoissonDispersion_BurstyCountsAreOverdispersedAndZerosAreUntestable()
        {
            var bursty = rates.TestPoissonDispersion([0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 50d, 60d]);
            var zeros = rates.TestPoissonDispersion([0d, 0d, 0d]);
            Action single = () => rates.TestPoissonDispersion([3d]);
            Action fractional = () => rates.TestPoissonDispersion([1.5, 2d]);

            bursty.Interpretation.Should().Contain("overdispersed");
            double.IsNaN(zeros.ChiSquare).Should().BeTrue();
            single.Should().Throw<ArgumentException>();
            fractional.Should().Throw<ArgumentException>();
        }
    }
}
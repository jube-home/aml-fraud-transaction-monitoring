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
using System.Globalization;
using System.Linq;
using FluentAssertions;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotionHistory;
using Jube.Service.Charts;
using Jube.Test.Charting.Performance;
using Xunit;

namespace Jube.Test.Service.ExhaustiveCharts
{
    [Trait("Category", "Service")]
    public sealed class ExhaustiveNarrativeTests
    {
        private static readonly List<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows =
        [
            .. PerformanceData.Scores.Select((s, i) =>
                new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto
                {
                    Predicted = s,
                    Actual = PerformanceData.Actual[i] ? 1 : 0,
                    Error = (PerformanceData.Actual[i] ? 1 : 0) - s
                })
        ];

        private static ExhaustiveSearchInstancePromotedTrialInstanceQueryDto Active(string? calibrationStatus)
        {
            return new ExhaustiveSearchInstancePromotedTrialInstanceQueryDto
                { CalibrationStatus = calibrationStatus, TopologyComplexity = 6, Score = 0.8 };
        }

        private static ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto Prescription(
            string name, double sensitivity, bool positive, double mean, double min, double max)
        {
            return new ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto
            {
                Name = name, Sensitivity = sensitivity, Positive = positive,
                PrescriptionMean = mean, PrescriptionMinimum = min, PrescriptionMaximum = max
            };
        }

        [Theory]
        [InlineData(0.95, "excellent")]
        [InlineData(0.85, "good")]
        [InlineData(0.75, "acceptable")]
        [InlineData(0.65, "poor")]
        [InlineData(0.5, "little better than chance")]
        public void DiscriminationBand_MapsAucToTheHosmerLemeshowConvention(double auc, string expected)
        {
            ExhaustiveNarrativeRules.DiscriminationBand(auc).Should().Be(expected);
        }

        private static string Join(IReadOnlyList<string> paragraphs)
        {
            return string.Join(" ", paragraphs);
        }

        [Fact]
        public void Performance_MentionsTheAucAndItsBandForARealSample()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), []);

            var paragraphs = ExhaustiveNarrativeRules.Performance(facts);

            paragraphs.Should().NotBeEmpty();
            var text = Join(paragraphs);
            text.Should().Contain("AUC of");
            text.Should().Contain(rows.Count.ToString("N0", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Performance_FlagsLowPrevalenceAsItsOwnParagraph()
        {
            var rare = Enumerable.Range(0, 100)
                .Select(i => new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto
                {
                    Predicted = i < 2 ? 0.9 : 0.1, Actual = i < 2 ? 1 : 0
                }).ToList();

            var facts = ExhaustiveNarrativeFacts.Build(rare, Active("Calibrated"), []);

            var paragraphs = ExhaustiveNarrativeRules.Performance(facts);

            paragraphs.Should().HaveCount(2);
            paragraphs[1].Should().Contain("positive class is rare");
        }

        [Fact]
        public void Performance_IsEmptyWhenThereIsNoPerformanceData()
        {
            var facts = ExhaustiveNarrativeFacts.Build([], Active("Calibrated"), []);

            ExhaustiveNarrativeRules.Performance(facts).Should().BeEmpty();
        }

        [Fact]
        public void Calibration_DescribesScoresAsProbabilitiesOnlyWhenCalibrated()
        {
            var calibrated = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), []);
            var uncalibrated = ExhaustiveNarrativeFacts.Build(rows, Active("Skipped"), []);

            Join(ExhaustiveNarrativeRules.Calibration(calibrated)).Should().Contain("calibrated")
                .And.Contain("chance of being the positive class");
            Join(ExhaustiveNarrativeRules.Calibration(uncalibrated)).Should().Contain("not calibrated")
                .And.Contain("ranking");
        }

        [Fact]
        public void Sensitivity_NamesTheDominantVariableWhenOneStandsOut()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"),
            [
                Prescription("Amount", 0.9, true, 100, 50, 150),
                Prescription("Country", 0.1, true, 1, 0, 2)
            ]);

            Join(ExhaustiveNarrativeRules.Sensitivity(facts)).Should().Contain("Amount");
        }

        [Fact]
        public void Sensitivity_SaysNoVariableDominatesWhenTwoAreSimilar()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"),
            [
                Prescription("Amount", 0.5, true, 100, 50, 150),
                Prescription("Country", 0.45, true, 1, 0, 2)
            ]);

            Join(ExhaustiveNarrativeRules.Sensitivity(facts)).Should().Contain("No single variable dominates");
        }

        [Fact]
        public void Sensitivity_IsEmptyWhenThereAreNoVariables()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), []);

            ExhaustiveNarrativeRules.Sensitivity(facts).Should().BeEmpty();
        }

        [Fact]
        public void Prescription_CallsOutAStrongDiscriminatorWhenTheClassRangesBarelyOverlap()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"),
            [
                Prescription("Amount", 0.9, true, 500, 400, 600),
                Prescription("Amount", 0.9, false, 50, 0, 100)
            ]);

            var paragraphs = ExhaustiveNarrativeRules.Prescription(facts);

            paragraphs.Should().ContainSingle();
            paragraphs[0].Should().Contain("Amount").And.Contain("strong discriminator on its own");
        }

        [Fact]
        public void Prescription_OneParagraphPerVariable()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"),
            [
                Prescription("Amount", 0.9, true, 500, 400, 600),
                Prescription("Amount", 0.9, false, 50, 0, 100),
                Prescription("Country", 0.2, true, 50, 0, 100),
                Prescription("Country", 0.2, false, 55, 10, 110)
            ]);

            var paragraphs = ExhaustiveNarrativeRules.Prescription(facts);

            paragraphs.Should().HaveCount(2);
            paragraphs.Should().Contain(p => p.Contains("Amount"));
            paragraphs.Should().Contain(p => p.Contains("Country"));
        }

        [Fact]
        public void Prescription_CallsOutAWeakDiscriminatorWhenTheClassRangesOverlapSubstantially()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"),
            [
                Prescription("Country", 0.2, true, 50, 0, 100),
                Prescription("Country", 0.2, false, 55, 10, 110)
            ]);

            Join(ExhaustiveNarrativeRules.Prescription(facts)).Should().Contain("overlap substantially");
        }

        [Fact]
        public void Prescription_IsEmptyWhenNeitherClassHasPrescriptionData()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), []);

            ExhaustiveNarrativeRules.Prescription(facts).Should().BeEmpty();
        }

        [Fact]
        public void Confusion_ExplainsTheMatrixInPlainTerms()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), []);

            var paragraphs = ExhaustiveNarrativeRules.Confusion(facts);

            paragraphs.Should().HaveCount(2);

            Join(paragraphs).Should().Contain("true positives").And.Contain("false positives")
                .And.Contain("false negatives").And.Contain("Precision is").And.Contain("recall is");
        }

        [Fact]
        public void Confusion_IsEmptyWhenThereIsNoPerformanceData()
        {
            var facts = ExhaustiveNarrativeFacts.Build([], Active("Calibrated"), []);

            ExhaustiveNarrativeRules.Confusion(facts).Should().BeEmpty();
        }

        [Fact]
        public void Discrimination_MentionsAucAndAveragePrecision()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), []);

            var text = Join(ExhaustiveNarrativeRules.Discrimination(facts));

            text.Should().Contain("ROC curve").And.Contain("the AUC, is").And.Contain("average precision");
        }

        [Fact]
        public void Ks_MentionsTheStatisticAndItsScore()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), []);

            Join(ExhaustiveNarrativeRules.Ks(facts)).Should().Contain("Kolmogorov-Smirnov");
        }

        [Fact]
        public void ThresholdSweep_MentionsTheCurrentThresholdTradeOff()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), []);

            var text = Join(ExhaustiveNarrativeRules.ThresholdSweep(facts));

            text.Should().Contain("precision").And.Contain("recall").And.Contain("policy choice");
        }

        [Fact]
        public void ScoreDistribution_DescribesSeparationBetweenClasses()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), []);

            var text = Join(ExhaustiveNarrativeRules.ScoreDistribution(facts));

            text.Should().Contain("Positive class cases score").And.Contain("negative class cases");
        }

        [Fact]
        public void ScoreDistribution_IsEmptyWithTooFewRowsPerClass()
        {
            var tiny = new List<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto>
            {
                new() { Predicted = 0.9, Actual = 1 },
                new() { Predicted = 0.1, Actual = 0 }
            };

            var facts = ExhaustiveNarrativeFacts.Build(tiny, Active("Calibrated"), []);

            ExhaustiveNarrativeRules.ScoreDistribution(facts).Should().BeEmpty();
        }

        [Fact]
        public void Gains_DescribesTopDecileCaptureAndLift()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), []);

            var text = Join(ExhaustiveNarrativeRules.Gains(facts));

            text.Should().Contain("highest-scoring tenth").And.Contain("lift of");
        }

        [Fact]
        public void Gains_IsEmptyWithFewerThanTenRows()
        {
            var tiny = rows.Take(5).ToList();
            var facts = ExhaustiveNarrativeFacts.Build(tiny, Active("Calibrated"), []);

            ExhaustiveNarrativeRules.Gains(facts).Should().BeEmpty();
        }

        [Fact]
        public void Holistic_JoinsWhicheverPartsAreAvailable()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"),
            [
                Prescription("Amount", 0.9, true, 100, 50, 150)
            ]);

            var text = Join(ExhaustiveNarrativeRules.Holistic(facts));

            text.Should().Contain("AUC of");
            text.Should().Contain("calibrated");
            text.Should().Contain("Amount");
        }

        [Fact]
        public void Holistic_FallsBackToASayNothingMessageWhenThereIsNoDataAtAll()
        {
            var facts = ExhaustiveNarrativeFacts.Build([], Active(null), []);

            Join(ExhaustiveNarrativeRules.Holistic(facts)).Should().Contain("not yet enough validation data");
        }

        [Fact]
        public void Build_GroupsPrescriptionRowsByVariableAcrossBothClasses()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"),
            [
                Prescription("Amount", 0.9, true, 500, 400, 600),
                Prescription("Amount", 0.9, false, 50, 0, 100),
                Prescription("Country", 0.2, true, 1, 0, 2)
            ]);

            facts.Variables.Should().HaveCount(2);
            var amount = facts.Variables.Single(v => v.Name == "Amount");
            amount.PositiveMean.Should().Be(500);
            amount.NegativeMean.Should().Be(50);
            var country = facts.Variables.Single(v => v.Name == "Country");
            country.PositiveMean.Should().Be(1);
            country.NegativeMean.Should().BeNull();
        }

        [Fact]
        public void Build_OrdersVariablesBySensitivityDescending()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"),
            [
                Prescription("Low", 0.1, true, 1, 0, 2),
                Prescription("High", 0.9, true, 1, 0, 2)
            ]);

            facts.Variables.Select(v => v.Name).Should().Equal("High", "Low");
        }

        [Fact]
        public void PrecisionRecall_SaysWhichMeasureDominates()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), []);

            var text = Join(ExhaustiveNarrativeRules.PrecisionRecall(facts));

            (text.Contains("Precision") || text.Contains("Recall")).Should().BeTrue();
            text.Should().Contain("current threshold");
        }

        [Fact]
        public void PrecisionRecall_IsEmptyWhenThereIsNoPerformanceData()
        {
            var facts = ExhaustiveNarrativeFacts.Build([], Active("Calibrated"), []);

            ExhaustiveNarrativeRules.PrecisionRecall(facts).Should().BeEmpty();
        }

        [Fact]
        public void Lift_DescribesHowManyDecilesStayAboveRandom()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), []);

            var text = Join(ExhaustiveNarrativeRules.Lift(facts));

            text.Should().Contain("Lift starts at").And.Contain("top decile");
        }

        [Fact]
        public void Lift_IsEmptyWithFewerThanTenRows()
        {
            var tiny = rows.Take(5).ToList();
            var facts = ExhaustiveNarrativeFacts.Build(tiny, Active("Calibrated"), []);

            ExhaustiveNarrativeRules.Lift(facts).Should().BeEmpty();
        }

        [Fact]
        public void Lift_SaysEveryDecileStaysAboveRandomWhenTheModelDiscriminatesThroughout()
        {
            var perfect = Enumerable.Range(0, 20)
                .Select(i => new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto
                {
                    Predicted = 1.0 - i * 0.01, Actual = i % 2 == 0 ? 1 : 0
                }).ToList();

            var facts = ExhaustiveNarrativeFacts.Build(perfect, Active("Calibrated"), []);

            Join(ExhaustiveNarrativeRules.Lift(facts)).Should().Contain("every decile");
        }

        private static ExhaustiveSearchInstancePromotionHistoryDto Promotion(int id, double score,
            int trialInstancesToDate)
        {
            return new ExhaustiveSearchInstancePromotionHistoryDto
            {
                Id = id, Score = score, TrialInstancesToDate = trialInstancesToDate, CreatedDate = DateTime.UtcNow
            };
        }

        [Fact]
        public void LearningCurve_DescribesPromotionsAndTrialsExplored()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), [],
                [Promotion(1, 0.6, 10), Promotion(2, 0.7, 25)]);

            var text = Join(ExhaustiveNarrativeRules.LearningCurve(facts));

            text.Should().Contain("2 promotions").And.Contain("25").And.Contain("0.6").And.Contain("0.7");
        }

        [Fact]
        public void LearningCurve_FlagsWhetherTheMostRecentPromotionsAreStillImproving()
        {
            var improving = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), [],
                [Promotion(1, 0.5, 5), Promotion(2, 0.6, 10), Promotion(3, 0.8, 15)]);
            var plateaued = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), [],
                [Promotion(1, 0.8, 5), Promotion(2, 0.8, 10), Promotion(3, 0.8, 15)]);

            Join(ExhaustiveNarrativeRules.LearningCurve(improving)).Should().Contain("still improving");
            Join(ExhaustiveNarrativeRules.LearningCurve(plateaued)).Should().Contain("plateaued");
        }

        [Fact]
        public void LearningCurve_IsEmptyWithNoPromotionHistory()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), [], []);

            ExhaustiveNarrativeRules.LearningCurve(facts).Should().BeEmpty();
        }

        [Fact]
        public void LearningCurve_SaysNothingAboutTrendWithFewerThanThreePromotions()
        {
            var facts = ExhaustiveNarrativeFacts.Build(rows, Active("Calibrated"), [],
                [Promotion(1, 0.5, 5), Promotion(2, 0.6, 10)]);

            ExhaustiveNarrativeRules.LearningCurve(facts).Should().HaveCount(1);
        }
    }
}
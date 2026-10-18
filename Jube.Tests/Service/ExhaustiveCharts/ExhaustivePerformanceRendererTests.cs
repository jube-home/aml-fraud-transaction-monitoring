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
using System.Text.RegularExpressions;
using System.Xml;
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotionHistory;
using Jube.Service.Charts;
using Jube.Test.Charting.Performance;
using Xunit;

namespace Jube.Test.Service.ExhaustiveCharts
{
    [Trait("Category", "Service")]
    public sealed class ExhaustivePerformanceRendererTests
    {
        private const double Tolerance = 1e-9;

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

        private static List<string> Texts(string svg)
        {
            var document = new XmlDocument { XmlResolver = null };
            document.LoadXml(svg);
            document.GetElementsByTagName("script").Count.Should().Be(0);
            svg.Should().NotContainEquivalentOf("<script");
            svg.Should().NotContainEquivalentOf("foreignObject");
            svg.Should().NotContain("<style");
            svg.Should().NotContain("style=");
            svg.Should().NotContain("NaN");
            Regex.IsMatch(svg, @"\son[a-z]+\s*=", RegexOptions.IgnoreCase).Should().BeFalse();
            document.DocumentElement!.GetAttribute("class").Split(' ').Should().Contain("jc");

            return
            [
                .. document.GetElementsByTagName("text").Cast<XmlNode>().Select(n => n.InnerText),
                .. document.GetElementsByTagName("title").Cast<XmlNode>().Select(n => n.InnerText)
            ];
        }

        private static ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto Row(double predicted,
            double actual)
        {
            return new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto
                { Predicted = predicted, Actual = actual, Error = actual - predicted };
        }

        [Fact]
        public void MeasuresMatchThePythonReference()
        {
            var m = ExhaustivePerformanceRenderer.Measures(rows)!;

            m.Rows.Should().Be(40);
            m.Positives.Should().Be(12);
            m.Negatives.Should().Be(28);
            m.Prevalence.Should().BeApproximately(0.3, Tolerance);
            m.Auc.Should().BeApproximately(0.8764880952380952, Tolerance);
            m.AucStandardError.Should().BeApproximately(0.05420910648204405, Tolerance);
            m.AucLower.Should().BeApproximately(0.7702401988991922, 1e-9);
            m.AucUpper.Should().BeApproximately(0.9827359915769983, 1e-9);
            m.Gini.Should().BeApproximately(0.7529761904761905, Tolerance);
            m.AveragePrecision.Should().BeApproximately(0.7374839213074507, Tolerance);
            m.Ks.Should().BeApproximately(0.6428571428571429, Tolerance);
            m.KsAtScore.Should().Be(0.35);
            m.Brier.Should().BeApproximately(0.153315, Tolerance);
            m.BrierSkill.Should().BeApproximately(1 - 0.153315 / 0.21, Tolerance);
            m.LogLoss.Should().BeApproximately(0.4693241037118476, Tolerance);
            m.ExpectedCalibrationError.Should().BeApproximately(0.1495, Tolerance);
            m.Confusion.TruePositives.Should().Be(10);
            m.Confusion.FalsePositives.Should().Be(7);
            m.Confusion.TrueNegatives.Should().Be(21);
            m.Confusion.FalseNegatives.Should().Be(2);
            m.Accuracy.Should().BeApproximately(0.775, Tolerance);
            m.Precision.Should().BeApproximately(0.5882352941176471, Tolerance);
            m.Recall.Should().BeApproximately(0.8333333333333334, Tolerance);
            m.Specificity.Should().BeApproximately(0.75, Tolerance);
            m.BalancedAccuracy.Should().BeApproximately((0.8333333333333334 + 0.75) / 2, Tolerance);
            m.F1.Should().BeApproximately(0.6896551724137931, Tolerance);
            m.Mcc.Should().BeApproximately(0.5407518998772001, Tolerance);
            m.Kappa.Should().BeApproximately(0.5212765957446809, Tolerance);
        }

        [Fact]
        public void MeasuresAtADifferentThresholdChangeOnlyTheThresholdMeasures()
        {
            var at = ExhaustivePerformanceRenderer.Measures(rows, 0.75)!;

            at.Auc.Should().BeApproximately(0.8764880952380952, Tolerance);
            at.Confusion.TruePositives.Should().Be(6);
            at.Precision.Should().BeApproximately(0.75, Tolerance);
            at.Recall.Should().BeApproximately(0.5, Tolerance);
            at.F1.Should().BeApproximately(0.6, Tolerance);
            at.Mcc.Should().BeApproximately(0.49099025303098287, Tolerance);
        }

        [Fact]
        public void MeasuresAreNullForEmptyOrSingleClassData()
        {
            ExhaustivePerformanceRenderer.Measures([]).Should().BeNull();
            ExhaustivePerformanceRenderer.Measures([Row(0.1, 0), Row(0.9, 0)]).Should().BeNull();
            ExhaustivePerformanceRenderer.Measures([Row(0.1, 1), Row(0.9, 1)]).Should().BeNull();
        }

        [Fact]
        public void ProbabilityMeasuresAreNotAvailableWhenScoresLeaveTheUnitInterval()
        {
            var m = ExhaustivePerformanceRenderer.Measures([Row(-0.5, 0), Row(0.5, 1), Row(1.5, 1), Row(0.1, 0)])!;

            double.IsNaN(m.Brier).Should().BeTrue();
            double.IsNaN(m.LogLoss).Should().BeTrue();
            double.IsNaN(m.ExpectedCalibrationError).Should().BeTrue();
            m.Auc.Should().BeApproximately(1, Tolerance);
        }

        [Fact]
        public void NonFiniteRowsAreDroppedBeforeAnyMeasure()
        {
            var noisy = rows.Concat([Row(double.NaN, 1), Row(0.5, double.PositiveInfinity)]).ToList();

            var m = ExhaustivePerformanceRenderer.Measures(noisy)!;

            m.Rows.Should().Be(40);
            m.Auc.Should().BeApproximately(0.8764880952380952, Tolerance);
        }

        [Fact]
        public void ActualIsTreatedAsPositiveClassFromOneHalfUp()
        {
            var m = ExhaustivePerformanceRenderer.Measures([Row(0.9, 0.5), Row(0.1, 0.49), Row(0.8, 1), Row(0.2, 0)])!;

            m.Positives.Should().Be(2);
            m.Negatives.Should().Be(2);
        }

        [Fact]
        public void MeasuresPanelCarriesEveryMeasureWithItsValueAndInterval()
        {
            var text = Texts(ExhaustivePerformanceRenderer.MeasuresPanel(rows, 0.5, ChartStyleMode.External, "ex-t"));

            text.Should().Contain("Performance Measures");
            text.Should().Contain("40 test rows, 12 positive class (30.0%)");

            text.Should().Contain(new[]
            {
                "AUC", "Gini", "KS", "Average precision", "Brier score", "Brier skill score", "Log loss",
                "Expected calibration error", "Accuracy", "Balanced accuracy", "Precision", "Recall", "Specificity",
                "F1", "MCC", "Cohen's kappa"
            }.AsEnumerable());

            text.Should().Contain("0.876");
            text.Should().Contain("95% interval 0.77 to 0.983");
            text.Should().Contain("0.753");
            text.Should().Contain("0.643");
            text.Should().Contain("at score 0.35");
            text.Should().Contain("0.737");
            text.Should().Contain("0.153");
            text.Should().Contain("0.469");
            text.Should().Contain("0.150");
            text.Should().Contain("0.775");
            text.Should().Contain("0.588");
            text.Should().Contain("0.833");
            text.Should().Contain("0.690");
            text.Should().Contain("0.541");
            text.Should().Contain("At threshold 0.5");
        }

        [Fact]
        public void EveryPerformanceChartRendersSafeExternalSvgWithTheExpectedTitles()
        {
            var expected = new Dictionary<string, string>
            {
                [ExhaustivePerformanceRenderer.ScoreDistribution(rows, 0.5, id: "ex-a")] = "Score Distribution",
                [ExhaustivePerformanceRenderer.Roc(rows, id: "ex-b")] = "ROC",
                [ExhaustivePerformanceRenderer.PrecisionRecall(rows, id: "ex-c")] = "Precision Recall",
                [ExhaustivePerformanceRenderer.Calibration(rows, id: "ex-d")] = "Calibration",
                [ExhaustivePerformanceRenderer.Gains(rows, id: "ex-e")] = "Cumulative Gains",
                [ExhaustivePerformanceRenderer.Lift(rows, id: "ex-f")] = "Lift by Decile",
                [ExhaustivePerformanceRenderer.Ks(rows, 0.5, id: "ex-g")] = "Kolmogorov-Smirnov",
                [ExhaustivePerformanceRenderer.ThresholdSweep(rows, 0.5, id: "ex-h")] = "Threshold Sweep",
                [ExhaustivePerformanceRenderer.Confusion(rows, 0.5, id: "ex-i")] = "Confusion Matrix"
            };

            foreach (var (svg, title) in expected)
            {
                var text = Texts(svg);
                text.Should().Contain(title);
                text.Should().NotContain(ExhaustiveChartRenderer.EmptyMessage);
            }
        }

        [Fact]
        public void ScoreDistributionShowsPositiveAndNegativeAsSeparateSeriesWithTheThreshold()
        {
            var text = Texts(ExhaustivePerformanceRenderer.ScoreDistribution(rows, 0.5, id: "ex-a"));

            text.Should().Contain(ExhaustiveChartRenderer.PositiveSeriesName);
            text.Should().Contain(ExhaustiveChartRenderer.NegativeSeriesName);
            text.Should().Contain(t => t.StartsWith("Decision threshold 0.5"));
            text.Should().Contain("Predicted probability");
        }

        [Fact]
        public void ConfusionChartShowsTheCountsAtTheThreshold()
        {
            var text = Texts(ExhaustivePerformanceRenderer.Confusion(rows, 0.5, id: "ex-i"));

            text.Should().Contain("21");
            text.Should().Contain("10");
            text.Should().Contain("7");
            text.Should().Contain(ExhaustiveChartRenderer.PositiveSeriesName);
            text.Should().Contain(ExhaustiveChartRenderer.NegativeSeriesName);
            text.Should().Contain(t => t.Contains("Test rows at threshold 0.5"));
        }

        [Fact]
        public void ThresholdSweepMarksTheMinimumRiskThresholdOnlyWhenBothCostsAreSupplied()
        {
            var with = Texts(ExhaustivePerformanceRenderer.ThresholdSweep(rows, 0.5, 9, 1, id: "ex-h"));
            var without = Texts(ExhaustivePerformanceRenderer.ThresholdSweep(rows, 0.5, id: "ex-h"));
            var partial = Texts(ExhaustivePerformanceRenderer.ThresholdSweep(rows, 0.5, 9, id: "ex-h"));

            with.Should().Contain(t => t.StartsWith("Minimum-risk threshold 0.1"));
            without.Should().NotContain(t => t.StartsWith("Minimum-risk"));
            partial.Should().NotContain(t => t.StartsWith("Minimum-risk"));
        }

        [Fact]
        public void MinimumRiskThresholdIsTheBayesPointOrNullForUnusableCosts()
        {
            ExhaustivePerformanceRenderer.MinimumRiskThreshold(9, 1).Should().BeApproximately(0.1, Tolerance);
            ExhaustivePerformanceRenderer.MinimumRiskThreshold(1, 1).Should().BeApproximately(0.5, Tolerance);
            ExhaustivePerformanceRenderer.MinimumRiskThreshold(null, 1).Should().BeNull();
            ExhaustivePerformanceRenderer.MinimumRiskThreshold(0, 1).Should().BeNull();
            ExhaustivePerformanceRenderer.MinimumRiskThreshold(-3, 1).Should().BeNull();
            ExhaustivePerformanceRenderer.MinimumRiskThreshold(double.NaN, 1).Should().BeNull();
            ExhaustivePerformanceRenderer.MinimumRiskThreshold(double.PositiveInfinity, 1).Should().BeNull();
        }

        [Theory]
        [InlineData(null, 0.5)]
        [InlineData(0.3, 0.3)]
        [InlineData(0.0, 0.0)]
        [InlineData(1.0, 1.0)]
        [InlineData(-0.1, 0.5)]
        [InlineData(1.1, 0.5)]
        [InlineData(double.NaN, 0.5)]
        [InlineData(double.PositiveInfinity, 0.5)]
        public void SafeThresholdFallsBackToTheDefault(double? input, double expected)
        {
            ExhaustivePerformanceRenderer.SafeThreshold(input).Should().Be(expected);
        }

        [Fact]
        public void EveryChartAnswersTheEmptyStateForNoRowsAndForASingleClass()
        {
            var single = new List<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto>
                { Row(0.1, 0), Row(0.2, 0) };

            foreach (var input in new[]
                     {
                         [], single
                     })
            {
                var outputs = new[]
                {
                    ExhaustivePerformanceRenderer.ScoreDistribution(input, 0.5),
                    ExhaustivePerformanceRenderer.Roc(input),
                    ExhaustivePerformanceRenderer.PrecisionRecall(input),
                    ExhaustivePerformanceRenderer.Calibration(input),
                    ExhaustivePerformanceRenderer.Gains(input),
                    ExhaustivePerformanceRenderer.Lift(input),
                    ExhaustivePerformanceRenderer.Ks(input, 0.5),
                    ExhaustivePerformanceRenderer.ThresholdSweep(input, 0.5),
                    ExhaustivePerformanceRenderer.Confusion(input, 0.5),
                    ExhaustivePerformanceRenderer.MeasuresPanel(input, 0.5, ChartStyleMode.External, null)
                };

                foreach (var svg in outputs)
                {
                    Texts(svg).Should().Contain(t => t.Contains(ExhaustiveChartRenderer.EmptyMessage));
                }
            }
        }

        [Fact]
        public void CalibrationIsEmptyWhenScoresAreNotProbabilities()
        {
            var text = Texts(ExhaustivePerformanceRenderer.Calibration(
                [Row(-2, 0), Row(0.5, 1), Row(3, 1), Row(0.1, 0)]));

            text.Should().Contain(t => t.Contains(ExhaustiveChartRenderer.EmptyMessage));
        }

        [Fact]
        public void PerformanceChartIdsAreUniquePerChart()
        {
            var a = ExhaustivePerformanceRenderer.Gains(rows, id: "ex-pg-1");
            var b = ExhaustivePerformanceRenderer.Lift(rows, id: "ex-pl-1");

            var idA = Regex.Match(a, "aria-labelledby=\"([^\"]+)\"").Groups[1].Value;
            var idB = Regex.Match(b, "aria-labelledby=\"([^\"]+)\"").Groups[1].Value;

            idA.Should().StartWith("ex-pg-1");
            idB.Should().StartWith("ex-pl-1");
            idA.Should().NotBe(idB);
        }

        [Fact]
        public void OutputIsDeterministic()
        {
            ExhaustivePerformanceRenderer.Gains(rows, id: "ex-g").Should()
                .Be(ExhaustivePerformanceRenderer.Gains(rows, id: "ex-g"));

            ExhaustivePerformanceRenderer.MeasuresPanel(rows, 0.5, ChartStyleMode.External, "ex-m").Should()
                .Be(ExhaustivePerformanceRenderer.MeasuresPanel(rows, 0.5, ChartStyleMode.External, "ex-m"));
        }

        [Fact]
        public void InlineModeIsStillAvailable()
        {
            ExhaustivePerformanceRenderer.Gains(rows, ChartStyleMode.Inline, "ex-g").Should().Contain("<style");
        }

        [Fact]
        public void LargeRowSetsStayWithinTheLimits()
        {
            var random = new Random(5);

            var big = Enumerable.Range(0, 80_000).Select(_ =>
            {
                var s = random.NextDouble();

                return Row(s, random.NextDouble() < s * s * 0.2 ? 1 : 0);
            }).ToList();

            foreach (var svg in new[]
                     {
                         ExhaustivePerformanceRenderer.Roc(big), ExhaustivePerformanceRenderer.Gains(big),
                         ExhaustivePerformanceRenderer.Ks(big, 0.5),
                         ExhaustivePerformanceRenderer.ThresholdSweep(big, 0.5),
                         ExhaustivePerformanceRenderer.ScoreDistribution(big, 0.5),
                         ExhaustivePerformanceRenderer.PrecisionRecall(big)
                     })
            {
                svg.Length.Should().BeLessThan(ChartLimits.MaxOutputCharacters);
                Texts(svg).Should().NotBeEmpty();
            }
        }

        [Fact]
        public void HostileTextInRowsCannotExistButHostileHistoryNamesAreNotRenderedAtAll()
        {
            var history = new List<ExhaustiveSearchInstancePromotionHistoryDto>
            {
                new()
                {
                    Id = 1, Score = 0.5, TopologyComplexity = 4, CreatedDate = new DateTime(2024, 1, 1),
                    TrialInstances = 3, TrialInstancesToDate = 3
                }
            };

            var svg = ExhaustivePerformanceRenderer.PromotionHistory(history, id: "ex-ph-1");

            Texts(svg).Should().NotBeEmpty();
        }

        [Fact]
        public void PromotionHistoryPlotsScoreBestSoFarAndComplexityAgainstTime()
        {
            var start = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc);

            var history = new List<ExhaustiveSearchInstancePromotionHistoryDto>
            {
                new()
                {
                    Id = 3, Score = 0.7, TopologyComplexity = 30, CreatedDate = start.AddDays(2), TrialInstances = 5,
                    TrialInstancesToDate = 15, Active = true
                },
                new()
                {
                    Id = 1, Score = 0.5, TopologyComplexity = 10, CreatedDate = start, TrialInstances = 4,
                    TrialInstancesToDate = 4
                },
                new()
                {
                    Id = 2, Score = 0.4, TopologyComplexity = 20, CreatedDate = start.AddDays(1), TrialInstances = 6,
                    TrialInstancesToDate = 10
                }
            };

            var text = Texts(ExhaustivePerformanceRenderer.PromotionHistory(history, id: "ex-ph-9"));

            text.Should().Contain("Promotion Over Time");
            text.Should().Contain("Score at promotion");
            text.Should().Contain("Best score so far");
            text.Should().Contain("Topology complexity");
            text.Should().Contain("Promotion date (UTC)");
            text.Should().Contain(t => t.Contains("3 promotions, best score 0.7, 15 trials explored"));
            text.Should().Contain("Promotion 1 after 4 trials");
            text.Should().Contain("Promotion 2 after 6 trials");
            text.Should().Contain("Promotion 3 after 5 trials, active");
            text.Should().Contain("Score 0.7");
            text.Should().Contain("Topology complexity 30");
            text.Should().Contain("Trials to date 15");
            text.Should().Contain("Promotion date (UTC): 2024-03-01 10:00");
            text.Should().Contain("Score: 0.5");
            text.Should().Contain("Topology complexity: 20");
        }

        [Fact]
        public void PromotionHistoryBestSoFarNeverDecreasesAndComplexityUsesTheSecondaryAxis()
        {
            var start = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc);

            var history = new[] { 0.5, 0.4, 0.6, 0.55 }.Select((s, i) =>
                new ExhaustiveSearchInstancePromotionHistoryDto
                {
                    Id = i + 1, Score = s, TopologyComplexity = 10 * (i + 1), CreatedDate = start.AddHours(i),
                    TrialInstances = 1, TrialInstancesToDate = i + 1
                }).ToList();

            var svg = ExhaustivePerformanceRenderer.PromotionHistory(history, id: "ex-ph-9");

            var document = new XmlDocument { XmlResolver = null };
            document.LoadXml(svg);
            Texts(svg).Should().Contain(t => t.Contains("4 promotions, best score 0.6"));
            Regex.IsMatch(svg, "class=\"[^\"]*\\bs2\\b").Should().BeTrue();
        }

        [Fact]
        public void PromotionHistoryWithASinglePromotionAndWithNoneStillRender()
        {
            var one = new List<ExhaustiveSearchInstancePromotionHistoryDto>
            {
                new()
                {
                    Id = 1, Score = 0.5, TopologyComplexity = 4, CreatedDate = DateTime.UtcNow, TrialInstances = 1,
                    TrialInstancesToDate = 1, Active = true
                }
            };

            Texts(ExhaustivePerformanceRenderer.PromotionHistory(one)).Should().Contain("Promotion Over Time");

            Texts(ExhaustivePerformanceRenderer.PromotionHistory([])).Should()
                .Contain(t => t.Contains(ExhaustiveChartRenderer.EmptyMessage));
        }

        [Fact]
        public void TheFormatOfTimestampsDoesNotDependOnTheCurrentCulture()
        {
            var previous = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                var history = new List<ExhaustiveSearchInstancePromotionHistoryDto>
                {
                    new()
                    {
                        Id = 1, Score = 0.5, TopologyComplexity = 4, CreatedDate = new DateTime(2024, 3, 1, 10, 0, 0),
                        TrialInstances = 1000, TrialInstancesToDate = 1000
                    }
                };

                var text = Texts(ExhaustivePerformanceRenderer.PromotionHistory(history));

                text.Should().Contain("Promotion 1 after 1000 trials");
                text.Should().Contain("Score: 0.5");
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }
    }
}
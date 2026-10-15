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
using System.Xml.Linq;
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Charting.Interfaces;
using Jube.Charting.Performance;
using Jube.Charting.Performance.Models;
using Jube.Test.Charting.External;
using Xunit;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.Performance
{
    public class PerformanceChartTests
    {
        public static IEnumerable<object[]> Names()
        {
            return PerformanceSamples.All().Keys.Select(k => new object[] { k });
        }

        private static IEnumerable<string> Texts(string svg)
        {
            var document = XDocument.Parse(svg);

            return
            [
                .. document.Descendants().Where(e => e.Name.LocalName is "text" or "title" or "desc")
                    .Select(e => e.Value)
            ];
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void InlineOutputIsWellFormedSafeAndDeterministic(string name)
        {
            var first = PerformanceSamples.All()[name].Render();
            var second = PerformanceSamples.All()[name].Render();

            first.Should().Be(second);
            SvgAssert.IsSafe(first);
            XDocument.Parse(first).Root!.Attribute("role")!.Value.Should().Be("img");
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void ExternalOutputHasNoStyleElementOrAttributeAndCarriesTheJcClass(string name)
        {
            var svg = ExternalSamples.ToExternal(PerformanceSamples.All()[name]).Render();

            ExternalStyleTests.AssertExternalSafe(svg);
            svg.Should().NotContain("style=");
            XDocument.Parse(svg).Root!.Attribute("class")!.Value.Split(' ').Should().Contain("jc");
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void EveryChartHasATitleADescriptionAndKeyboardReachableMarks(string name)
        {
            var svg = PerformanceSamples.All()[name].Render();
            var document = XDocument.Parse(svg);

            document.Root!.Attribute("aria-labelledby")!.Value.Should().NotBeNullOrWhiteSpace();
            document.Descendants().Count(e => e.Name.LocalName == "title").Should().BeGreaterThan(1);
            document.Descendants().Any(e => e.Attribute("tabindex")?.Value == "0").Should().BeTrue();
        }

        [Fact]
        public void GainsShowsTheRandomBaselineThePerfectCurveAndTheDecileHover()
        {
            var svg = PerformanceSamples.All()["performance-gains"].Render();
            var text = Texts(svg).ToList();

            text.Should().Contain("Cumulative Gains");
            text.Should().Contain("Random ranking");
            text.Should().Contain("Perfect model");
            text.Should().Contain("Top 10% of scores");
            text.Should().Contain("Captures 25% of positives");
            text.Should().Contain("Lift 2.5");
            text.Should().Contain("Share of population, highest score first");
            text.Should().Contain("Share of positives captured");
        }

        [Fact]
        public void GainsWithoutBaselinesOmitsThem()
        {
            var svg = Chart.Gains().ShowRandom(false).ShowPerfect(false).DecileMarks(false)
                .FromScores("Model", PerformanceData.Actual, PerformanceData.Scores).Render();

            Texts(svg).Should().NotContain("Random ranking");
            Texts(svg).Should().NotContain("Perfect model");
        }

        [Fact]
        public void GainsRejectsMismatchedCurves()
        {
            var act = () => Chart.Gains().Curve("x", [0.0, 1], [0.0]).Render();

            act.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void LiftShowsTenDecilesTheBaseRateLineAndBothSeries()
        {
            var svg = PerformanceSamples.All()["performance-lift"].Render();
            var text = Texts(svg).ToList();

            text.Should().Contain("Lift by Decile");
            text.Should().Contain("Lift in decile");
            text.Should().Contain("Cumulative lift");
            text.Should().Contain("No lift (base rate)");
            text.Should().Contain("Score decile, highest scores first");
            text.Should().Contain(t => t.Contains("2.5"));
        }

        [Fact]
        public void LiftCumulativeSeriesCanBeSwitchedOff()
        {
            var svg = Chart.Lift().Cumulative(false)
                .FromScores(PerformanceData.Actual, PerformanceData.Scores).Render();

            Texts(svg).Should().NotContain("Cumulative lift");
        }

        [Fact]
        public void KsMarksTheMaximumSeparationAndTheThreshold()
        {
            var svg = PerformanceSamples.All()["performance-ks"].Render();
            var text = Texts(svg).ToList();

            text.Should().Contain(t => t.StartsWith("KS 0.643 at score 0.35"));
            text.Should().Contain(t => t.StartsWith("Maximum separation 0.643"));
            text.Should().Contain(t => t.StartsWith("Decision threshold 0.5"));
            text.Should().Contain("Fraud");
            text.Should().Contain("Genuine");
        }

        [Fact]
        public void ThresholdSweepDrawsFourMeasuresBothThresholdsAndTheBestF1()
        {
            var svg = PerformanceSamples.All()["performance-threshold-sweep"].Render();
            var text = Texts(svg).ToList();

            text.Should().Contain(new[] { "Precision", "Recall", "F1", "MCC" }.AsEnumerable());
            text.Should().Contain(t => t.StartsWith("Current threshold 0.5"));
            text.Should().Contain(t => t.StartsWith("Minimum-risk threshold 0.3"));
            text.Should().Contain(t => t.StartsWith("Best F1 0.72 at threshold 0.59"));
        }

        [Fact]
        public void ThresholdSweepWithoutMccOrMinimumRiskOmitsThem()
        {
            var svg = Chart.ThresholdSweep().ShowMcc(false)
                .FromScores(PerformanceData.Actual, PerformanceData.Scores).Render();

            var text = Texts(svg).ToList();

            text.Should().NotContain("MCC");
            text.Should().NotContain(t => t.StartsWith("Minimum-risk"));
            text.Should().Contain(t => t.StartsWith("Current threshold 0.5"));
        }

        [Fact]
        public void ScoreDistributionOverlaysBothClassesWithTheThreshold()
        {
            var svg = PerformanceSamples.All()["performance-score-distribution"].Render();
            var text = Texts(svg).ToList();

            text.Should().Contain("Fraud");
            text.Should().Contain("Genuine");
            text.Should().Contain(t => t.StartsWith("Decision threshold 0.5"));
            text.Should().Contain(t => t.Contains("Fraud: 25% of class (3)"));
            text.Should().Contain("Predicted probability");
            text.Should().Contain("Share of class");
        }

        [Fact]
        public void MetricsPanelShowsEveryMeasureWithHoverDetailAndNotAvailableForNaN()
        {
            var svg = PerformanceSamples.All()["performance-metrics-panel"].Render();
            var text = Texts(svg).ToList();

            text.Should().Contain("AUC");
            text.Should().Contain("0.877");
            text.Should().Contain("95% interval 0.770 to 0.983");
            text.Should().Contain("Ranking");
            text.Should().Contain("At threshold 0.50");
            text.Should().Contain("n/a");
            text.Should().Contain(t => t.Contains("Area under the ROC curve"));
            svg.Should().NotContain("NaN");
        }

        [Fact]
        public void PromotionHistoryShowsScoreBestSoFarComplexityAndTheActivePromotion()
        {
            var svg = PerformanceSamples.All()["performance-promotion-history"].Render();
            var text = Texts(svg).ToList();

            text.Should().Contain("Promotion Over Time");
            text.Should().Contain("Score at promotion");
            text.Should().Contain("Best score so far");
            text.Should().Contain("Topology complexity");
            text.Should().Contain("Promotion date (UTC)");
            text.Should().Contain(t => t.StartsWith("5 promotions, best score 0.79, 268 trials explored"));
            text.Should().Contain("Promotion 2 after 40 trials");
            text.Should().Contain("Promotion 5 after 61 trials, active");
            text.Should().Contain("Topology complexity: 30");
            text.Should().Contain("Score: 0.61");
            text.Should().Contain("Best score: 0.61");
        }

        [Fact]
        public void PromotionHistoryBestSoFarIsMonotoneWhateverTheScoreDoes()
        {
            var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var svg = Chart.PromotionHistory().Promotion(start, 0.5).Promotion(start.AddDays(1), 0.3)
                .Promotion(start.AddDays(2), 0.8).Promotion(start.AddDays(3), 0.6).Render();

            var text = Texts(svg).ToList();

            text.Should().Contain("Best score: 0.5");
            text.Should().NotContain("Best score: 0.3");
            text.Should().Contain("Best score: 0.8");
            text.Should().NotContain("Best score: 0.6");
        }

        [Fact]
        public void PromotionHistoryWithoutComplexityOmitsTheSecondaryAxisAndDropsNaNRows()
        {
            var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var svg = Chart.PromotionHistory().Promotion(start, 0.5).Promotion(start.AddDays(1), double.NaN)
                .Promotion(start.AddDays(2), 0.8).Render();

            var text = Texts(svg).ToList();

            text.Should().NotContain("Topology complexity");
            text.Should().Contain(t => t.StartsWith("2 promotions"));
            svg.Should().NotContain("NaN");
        }

        [Fact]
        public void PromotionHistoryOrdersPromotionsByDateWhateverTheInputOrder()
        {
            var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var svg = Chart.PromotionHistory().Promotion(start.AddDays(2), 0.8, 5, 4).Promotion(start, 0.5, 3, 9)
                .Render();

            var text = Texts(svg).ToList();

            text.Should().Contain("Promotion 1 after 9 trials");
            text.Should().Contain("Promotion 2 after 4 trials");
        }

        [Fact]
        public void MetricsPanelGrowsWithTheNumberOfRows()
        {
            var small = Chart.MetricsPanel().Group("A", [new MetricTile("a", 1)]).Render();

            var big = Chart.MetricsPanel().Group("A", Enumerable.Range(0, 12).Select(i => new MetricTile("m" + i, i)))
                .Render();

            Height(big).Should().BeGreaterThan(Height(small));
        }

        [Theory]
        [InlineData("<script>alert(1)</script>")]
        [InlineData("\"><script>alert(1)</script>")]
        [InlineData("' onload='alert(1)")]
        [InlineData("</text></svg><script>alert(1)</script>")]
        [InlineData("<foreignObject><body onload=alert(1)></foreignObject>")]
        [InlineData("\u0000\u0001\u0008 x")]
        [InlineData("]]><script>x</script><![CDATA[")]
        public void HostileTextNeverProducesMarkupInEitherMode(string hostile)
        {
            var charts = new ISvgChart[]
            {
                Chart.Gains().Title(hostile).XAxisTitle(hostile).YAxisTitle(hostile).Description(hostile)
                    .FromScores(hostile, PerformanceData.Actual, PerformanceData.Scores),
                Chart.Lift().Title(hostile).Subtitle(hostile)
                    .FromScores(PerformanceData.Actual, PerformanceData.Scores),
                Chart.Ks().Title(hostile).Classes(hostile, hostile + "b")
                    .FromScores(PerformanceData.Actual, PerformanceData.Scores).Threshold(0.4),
                Chart.ThresholdSweep().Title(hostile).YAxisTitle(hostile)
                    .FromScores(PerformanceData.Actual, PerformanceData.Scores),
                Chart.ScoreDistribution().Title(hostile).Classes(hostile, hostile + "b")
                    .FromScores(PerformanceData.Actual, PerformanceData.Scores).Threshold(0.4),
                Chart.MetricsPanel().Title(hostile).Group(hostile,
                    [new MetricTile(hostile, 1, hostile, hostile), new MetricTile("b", 2)]),
                Chart.PromotionHistory().Title(hostile).XAxisTitle(hostile).YAxisTitle(hostile)
                    .Promotion(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 0.5, 3, 2, 2, true)
                    .Promotion(new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc), 0.7, 4, 2, 4)
            };

            foreach (var chart in charts)
            {
                var inline = chart.Render();
                inline.Should().NotContain("<script");
                inline.Should().NotContain("<foreignObject");
                XDocument.Parse(inline);
                var external = ExternalSamples.ToExternal(chart).Render();
                ExternalStyleTests.AssertExternalSafe(external, false);
            }
        }

        [Fact]
        public void HostileIdsAreRejected()
        {
            var act = () => Chart.Gains().FromScores("m", PerformanceData.Actual, PerformanceData.Scores)
                .Id("a\"><script>").Render();

            act.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void EmptyDataRendersAnEmptyStateForEveryChart()
        {
            var charts = new[]
            {
                Chart.Gains().Render(),
                Chart.Lift().Render(),
                Chart.Ks().Render(),
                Chart.ThresholdSweep().Render(),
                Chart.ScoreDistribution().Render(),
                Chart.MetricsPanel().Render(),
                Chart.PromotionHistory().Render()
            };

            foreach (var svg in charts)
            {
                XDocument.Parse(svg);
                svg.Should().NotContain("NaN");
                svg.Should().NotContain("Infinity");
            }
        }

        [Fact]
        public void TooManyObservationsAreRefusedByTheCap()
        {
            var big = new bool[ChartLimits.MaxScoredObservations + 1];
            var scores = new double[big.Length];

            var act = () => PerformanceMath.Gains(big, scores);

            act.Should().Throw<ChartLimitException>();
        }

        [Fact]
        public void LargeInputsStayWithinTheOutputAndPointCaps()
        {
            var random = new Random(11);
            var scores = Enumerable.Range(0, 60_000).Select(_ => random.NextDouble()).ToArray();
            var actual = scores.Select(s => random.NextDouble() < s * s * 0.3).ToArray();

            foreach (var chart in new ISvgChart[]
                     {
                         Chart.Gains().FromScores("m", actual, scores),
                         Chart.Ks().FromScores(actual, scores),
                         Chart.ThresholdSweep().FromScores(actual, scores),
                         Chart.ScoreDistribution().FromScores(actual, scores),
                         Chart.Lift().FromScores(actual, scores)
                     })
            {
                var svg = chart.Render();
                svg.Length.Should().BeLessThan(ChartLimits.MaxOutputCharacters);
                XDocument.Parse(svg);
            }
        }

        [Fact]
        public void BuiltChartsSupportStyleModeSwitching()
        {
            var chart = Chart.Gains().FromScores("m", PerformanceData.Actual, PerformanceData.Scores).Build();

            var external = ((IStyleModeChart)chart).WithStyleMode(ChartStyleMode.External).Render();

            external.Should().NotContain("<style");
            chart.Render().Should().Contain("<style");
        }

        private static double Height(string svg)
        {
            var box = XDocument.Parse(svg).Root!.Attribute("viewBox")!.Value.Split(' ');

            return double.Parse(box[3], CultureInfo.InvariantCulture);
        }
    }
}
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
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.Charts;
using Jube.Charting.Core.Models;
using Jube.Charting.Interfaces;
using Xunit;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting
{
    public class ChartBehaviourTests
    {
        [Fact]
        public void NaNValuesInALineBecomeGapsNotInvalidPathData()
        {
            var svg = Chart.Line().Series("s", [1.0, 2, double.NaN, 4, 5]).Render();
            SvgAssert.IsSafe(svg);
            var path = Regex.Match(svg, "<path class=\"ln\" d=\"([^\"]+)\"").Groups[1].Value;
            Regex.Matches(path, "M").Count.Should().Be(2);
        }

        [Fact]
        public void InfinityAndNaNInEveryPositionNeverLeakIntoOutput()
        {
            var values = new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, 1, double.NaN };
            SvgAssert.IsSafe(Chart.Line().Series("s", values).Render());
            SvgAssert.IsSafe(Chart.Area().Series("s", values).Render());
            SvgAssert.IsSafe(Chart.Column().Categories(["a", "b", "c", "d", "e"]).Series("s", values).Render());
            SvgAssert.IsSafe(Chart.Scatter().Series("s", values, values).Render());
            SvgAssert.IsSafe(Chart.Histogram().Values("s", values).Render());
            SvgAssert.IsSafe(Chart.Pie().Slice("a", double.NaN).Slice("b", -3).Slice("c", 2).Render());

            SvgAssert.IsSafe(Chart.Heatmap().Rows(["a", "b"]).Columns(["c", "d"])
                .Values([[double.NaN, 1], [double.PositiveInfinity, 2]]).Render());
        }

        [Fact]
        public void AllNaNSeriesRenderANoDataChart()
        {
            var svg = Chart.Line().Series("s", [double.NaN, double.NaN]).Render();
            SvgAssert.IsSafe(svg);
            svg.Should().Contain("No data");
        }

        [Fact]
        public void EmptyChartsRenderWithoutThrowing()
        {
            foreach (var chart in new ISvgChart[]
                     {
                         Chart.Line(), Chart.Area(), Chart.Column(), Chart.Bar(), Chart.Scatter(), Chart.Histogram(),
                         Chart.Pie(), Chart.Donut(), Chart.Heatmap(), Chart.Roc(), Chart.PrecisionRecall(),
                         Chart.Calibration(), Chart.LearningCurve()
                     })
            {
                SvgAssert.IsSafe(chart.Render());
            }
        }

        [Fact]
        public void IsolatedPointsBetweenGapsAreStillDrawn()
        {
            var svg = Chart.Line().Series("s", [1.0, double.NaN, 3, double.NaN, 5]).Markers(MarkerMode.Off).Render();
            Regex.Matches(svg, "<circle class=\"pt").Count.Should().BeGreaterOrEqualTo(3);
        }

        [Fact]
        public void PointCapThrowsAClearException()
        {
            var values = new double[ChartLimits.MaxPoints + 1];
            var act = () => Chart.Line().Series("s", values).Render();
            var e = act.Should().Throw<ChartLimitException>().Which;
            e.Limit.Should().Be("point count");
            e.Maximum.Should().Be(ChartLimits.MaxPoints);
            e.Actual.Should().Be(ChartLimits.MaxPoints + 1);
            e.Message.Should().Contain("point count");
        }

        [Fact]
        public void PointCapAppliesAcrossSeries()
        {
            var half = new double[ChartLimits.MaxPoints / 2 + 1];
            var act = () => Chart.Scatter().Series("a", half, half).Series("b", half, half).Render();
            act.Should().Throw<ChartLimitException>();
        }

        [Fact]
        public void MaximumPointsRenderWithinTheOutputCap()
        {
            var random = new Random(1);
            var xs = Enumerable.Range(0, ChartLimits.MaxPoints).Select(_ => random.NextDouble()).ToArray();
            var ys = Enumerable.Range(0, ChartLimits.MaxPoints).Select(_ => random.NextDouble()).ToArray();
            var svg = Chart.Scatter().Series("s", xs, ys).Render();
            svg.Length.Should().BeLessThan(ChartLimits.MaxOutputCharacters);
            svg.Should().NotContain("class=\"tt\"");
        }

        [Fact]
        public void SeriesCategoryAndScoredObservationCapsThrow()
        {
            var many = Chart.Line();

            for (var i = 0; i <= ChartLimits.MaxSeries; i++)
            {
                many.Series("s" + i, [1.0, 2]);
            }

            var act1 = () => many.Render();
            act1.Should().Throw<ChartLimitException>();

            var act2 = () => Chart.Column()
                .Categories(Enumerable.Range(0, ChartLimits.MaxCategories + 1).Select(i => "c" + i))
                .Series("s", new double[ChartLimits.MaxCategories + 1]).Render();
            act2.Should().Throw<ChartLimitException>();
            var act3 = () => ChartLimits.Output(ChartLimits.MaxOutputCharacters + 1);
            act3.Should().Throw<ChartLimitException>();
        }

        [Fact]
        public void HeatmapCellCapThrows()
        {
            var labels = Enumerable.Range(0, 201).Select(i => "l" + i).ToArray();

            var act = () => Chart.Heatmap().Rows(labels).Columns(labels)
                .Values(labels.Select(_ => new double[201].AsEnumerable())).Render();
            act.Should().Throw<ChartLimitException>();
        }

        [Fact]
        public void MismatchedInputsThrowSpecificationExceptions()
        {
            Action a = () => Chart.Column().Categories(["a", "b"]).Series("s", [1.0]).Render();
            a.Should().Throw<ChartSpecificationException>();
            Action b = () => Chart.Line().Series("s", [1.0, 2], [1.0]);
            b.Should().Throw<ChartSpecificationException>();
            Action c = () => Chart.Heatmap().Rows(["a"]).Columns(["b", "c"]).Values([[1.0]]).Render();
            c.Should().Throw<ChartSpecificationException>();
            Action d = () => Chart.Histogram().Bins("h", [0.0, 1], [1.0, 2]).Render();
            d.Should().Throw<ChartSpecificationException>();
            Action e = () => Chart.Line().Size(10, 10).Series("s", [1.0, 2]).Render();
            e.Should().Throw<ChartSpecificationException>();
            Action f = () => Chart.Area().Stacked().Series("a", [1.0, 2]).Series("b", [1.0]).Render();
            f.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void LegendIsShownForManySeriesAndHiddenForOne()
        {
            Chart.Line().Series("only", [1.0, 2]).Render().Should().NotContain("class=\"lg ");
            Chart.Line().Series("a", [1.0, 2]).Series("b", [2.0, 1]).Render().Should().Contain("class=\"lg s1\"");

            Chart.Line().Series("only", [1.0, 2]).Legend(LegendPosition.Top).Render().Should()
                .Contain("class=\"lg s0\"");

            Chart.Line().Series("a", [1.0, 2]).Series("b", [2.0, 1]).Legend(LegendPosition.None).Render()
                .Should().NotContain("class=\"lg ");
        }

        [Theory]
        [InlineData(LegendPosition.Top)]
        [InlineData(LegendPosition.Bottom)]
        [InlineData(LegendPosition.Right)]
        public void LegendPositionsAllRender(LegendPosition position)
        {
            var svg = Chart.Column().Categories(["a", "b"]).Series("A", [1.0, 2]).Series("B", [2.0, 3]).Legend(position)
                .Render();
            SvgAssert.IsSafe(svg);
            svg.Should().Contain("class=\"lg s0\"");
        }

        [Fact]
        public void ManyLegendEntriesWrapOntoRows()
        {
            var chart = Chart.Line();

            for (var i = 0; i < 12; i++)
            {
                chart.Series("A reasonably long series name " + i, [1.0 + i, 2]);
            }

            var svg = chart.Render();
            SvgAssert.IsSafe(svg);

            var ys = Regex.Matches(svg, "class=\"hit\" x=\"[-0-9.]+\" y=\"([0-9.]+)\"").Select(m => m.Groups[1].Value)
                .Distinct();
            ys.Count().Should().BeGreaterThan(1);
        }

        [Fact]
        public void NegativeValuesDrawBarsBelowTheBaseline()
        {
            var svg = Chart.Column().Categories(["a", "b"]).Series("s", [5.0, -5]).Render();
            SvgAssert.IsSafe(svg);
            svg.Should().Contain("-5");
        }

        [Fact]
        public void StackedColumnsShowTotalsInTooltips()
        {
            var svg = Chart.Column().Categories(["a"]).Stacked().Series("x", [3.0]).Series("y", [4.0]).Render();
            svg.Should().Contain("Total: 7");
        }

        [Fact]
        public void ErrorBarsAreDrawnWhenSupplied()
        {
            var svg = Chart.Column().Categories(["a", "b"]).Series("x", [3.0, 4], null, [0.5, 0.5]).Render();
            svg.Should().Contain("class=\"eb\"");
            svg.Should().Contain("Error: 0.5");
        }

        [Fact]
        public void PieFoldsTheTailIntoOther()
        {
            var chart = Chart.Pie();

            for (var i = 0; i < 12; i++)
            {
                chart.Slice("s" + i, 12 - i);
            }

            var svg = chart.Render();
            svg.Should().Contain(">Other<");
            Regex.Matches(svg, "class=\"lg s").Count.Should().Be(8);
        }

        [Fact]
        public void SingleSliceDonutRendersAFullRing()
        {
            var svg = Chart.Donut().Slice("only", 5).Render();
            SvgAssert.IsSafe(svg);
            svg.Should().Contain("100%");
        }

        [Fact]
        public void HistogramFromRawValuesCountsEveryValue()
        {
            var values = Enumerable.Range(0, 1000).Select(i => i / 10.0).ToArray();
            var edges = HistogramChartBuilder.BinEdges(values, null);
            var counts = HistogramChartBuilder.Count(values, edges);
            counts.Sum().Should().Be(1000);
            edges.Zip(edges.Skip(1), (a, b) => b > a).Should().AllBeEquivalentTo(true);
        }

        [Fact]
        public void HistogramEdgeValuesLandInTheLastBin()
        {
            var counts = HistogramChartBuilder.Count([0.0, 1, 2, 3, 4], [0, 2, 4]);
            counts.Should().Equal(2, 3);
        }

        [Fact]
        public void HistogramOverlaysTwoSeriesWithSharedEdges()
        {
            var svg = Chart.Histogram().Values("Genuine", ChartSamples.Gaussian(1, 500, 0, 1))
                .Values("Fraud", ChartSamples.Gaussian(2, 50, 2, 1)).Render();
            svg.Should().Contain("class=\"sr s0\"");
            svg.Should().Contain("class=\"sr s1\"");
            svg.Should().Contain("of series");
        }

        [Fact]
        public void PrecomputedHistogramBinsRender()
        {
            var svg = Chart.Histogram().Bins("h", [0.0, 1, 2, 3], [5.0, 9, 2]).Render();
            SvgAssert.IsSafe(svg);
            svg.Should().Contain("h: 5");
        }

        [Fact]
        public void HeatmapUsesSequentialAndDivergingClasses()
        {
            var seq = Chart.Heatmap().Rows(["a", "b"]).Columns(["c", "d"])
                .Values([[1.0, 2], [3.0, 4]]).Render();
            seq.Should().Contain("cell m q0");
            seq.Should().Contain("cell m q8");
            var div = ChartSamples.Correlation().Render();
            div.Should().Contain("cell m d3");
            div.Should().Contain("cell m d10");
            div.Should().Contain("cell m d5");
        }

        [Fact]
        public void HeatmapLegendBarHasOneBlockPerBin()
        {
            var svg = ChartSamples.Correlation().Render();
            Regex.Matches(svg, "<rect class=\"d\\d+\" x=").Count.Should().Be(11);
        }

        [Fact]
        public void ConfusionMatrixTooltipsCarryRowAndColumnPercentages()
        {
            var svg = Chart.ConfusionMatrix().Binary(90, 10, 5, 45).Render();
            svg.Should().Contain("Of actual Negative class: 90%");
            svg.Should().Contain("Of predicted Positive class: 81.8%");
            svg.Should().Contain("Of all: 60%");
            svg.Should().Contain("Accuracy 90%");
        }

        [Fact]
        public void ConfusionMatrixFromPredictionsCounts()
        {
            var svg = Chart.ConfusionMatrix().FromPredictions([0, 0, 1, 1, 1], [0, 1, 1, 1, 0]).Render();
            svg.Should().Contain("Count: 2");
        }

        [Fact]
        public void ConfusionMatrixMustBeSquare()
        {
            Action act = () =>
                Chart.ConfusionMatrix().Labels(["a", "b"]).Matrix(new[,] { { 1.0, 2, 3 }, { 1, 2, 3 } }).Render();
            act.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void RocLegendCarriesAucAndChanceDiagonalAndThresholdMarks()
        {
            var svg = ChartSamples.Roc().Render();
            svg.Should().Contain("AUC 0.9");
            svg.Should().Contain("class=\"ref\"");
            svg.Should().Contain("Threshold ");
            svg.Should().Contain("Best cut-off");
        }

        [Fact]
        public void PrecisionRecallCarriesAverageAndNoSkillBaseline()
        {
            var svg = ChartSamples.PrecisionRecall().Render();
            svg.Should().Contain("(AP ");
            svg.Should().Contain("No skill");
        }

        [Fact]
        public void CalibrationHasIdealDiagonalAndCountStrip()
        {
            var svg = ChartSamples.Calibration().Render();
            svg.Should().Contain("Perfectly calibrated");
            svg.Should().Contain("(ECE ");
            svg.Should().Contain("Bin ");
        }

        [Fact]
        public void LearningCurveHasCrosshairBandsAndBestMarker()
        {
            var svg = ChartSamples.LearningCurve().Render();
            svg.Should().Contain("class=\"vl\"");
            svg.Should().Contain("Iteration 26");
            svg.Should().Contain("class=\"cx s0\"");
            svg.Should().Contain("class=\"cx s1\"");
            svg.Should().Contain("Best validation Log loss");
            Regex.Matches(svg, "class=\"mk\" tabindex").Count.Should().Be(80);
        }

        [Fact]
        public void PredictedVersusActualDrawsTheIdentityLine()
        {
            var svg = ChartSamples.PredictedActual().Render();
            svg.Should().Contain("Perfect prediction");
        }

        [Fact]
        public void LogAxesRenderDecades()
        {
            var svg = Chart.Line().LogY().Series("s", [1.0, 10, 100, 1000]).Render();
            SvgAssert.IsSafe(svg);
            svg.Should().Contain(">1000<");
            svg.Should().Contain(">10<");
        }

        [Fact]
        public void LogAxisDropsNonPositiveValuesAsGaps()
        {
            var svg = Chart.Line().LogY().Series("s", [1.0, 0, -5, 100]).Render();
            SvgAssert.IsSafe(svg);
        }

        [Fact]
        public void PointLabelsAppearInTooltips()
        {
            var svg = Chart.Scatter().Series("s", [new DataPoint(1, 2, "Merchant Acme")]).Render();
            svg.Should().Contain("Merchant Acme");
        }

        [Fact]
        public void PerSeriesColoursOverrideThePalette()
        {
            var svg = Chart.Column().Categories(["a"]).Series("s", [1.0], "#123456").Render();
            svg.Should().Contain(".s0{--sc:#123456}");
        }

        [Fact]
        public void SummaryDescribesTheChart()
        {
            var svg = ChartSamples.Line().Render();
            var desc = Regex.Match(svg, "<desc id=\"[^\"]+\">([^<]*)</desc>").Groups[1].Value;
            desc.Should().Contain("Line chart with 3 series");
            desc.Should().Contain("Card present");
        }

        [Fact]
        public void CustomDescriptionOverridesTheSummary()
        {
            var svg = Chart.Line().Description("Custom words").Series("s", [1.0, 2]).Render();
            svg.Should().Contain(">Custom words</desc>");
        }

        [Fact]
        public void CurveMathMatchesHandComputedAnswers()
        {
            var r = CurveMath.Roc([true, true, false, true, false, false], [0.9, 0.8, 0.7, 0.6, 0.4, 0.3]);
            r.Auc.Should().BeApproximately(8.0 / 9.0, 1e-12);
            var perfect = CurveMath.Roc([true, true, false, false], [0.9, 0.8, 0.2, 0.1]);
            perfect.Auc.Should().BeApproximately(1, 1e-12);
            var inverted = CurveMath.Roc([true, true, false, false], [0.1, 0.2, 0.8, 0.9]);
            inverted.Auc.Should().BeApproximately(0, 1e-12);
            var ties = CurveMath.Roc([true, false, true, false], [0.5, 0.5, 0.5, 0.5]);
            ties.Auc.Should().BeApproximately(0.5, 1e-12);
            var pr = CurveMath.PrecisionRecall([true, false, true, false], [0.9, 0.8, 0.7, 0.1]);
            pr.AveragePrecision.Should().BeApproximately(0.5 * 1 + 0.5 * (2.0 / 3.0), 1e-12);
            pr.Prevalence.Should().Be(0.5);
        }

        [Fact]
        public void CurveMathRejectsSingleClassAndMismatchedInput()
        {
            Action a = () => CurveMath.Roc([true, true], [0.1, 0.2]);
            a.Should().Throw<ChartSpecificationException>();
            Action b = () => CurveMath.Roc([true, false], [0.1]);
            b.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void CalibrationBinsAndEceAreCorrect()
        {
            var r = CurveMath.Calibration([false, false, true, true], [0.1, 0.2, 0.8, 0.9], 2);
            r.Counts.Should().Equal(2, 2);
            r.MeanPredicted[0].Should().BeApproximately(0.15, 1e-12);
            r.FractionPositive[1].Should().Be(1);
            r.ExpectedCalibrationError.Should().BeApproximately(0.15 * 0.5 + 0.15 * 0.5, 1e-12);
        }

        [Fact]
        public void LargeScoredInputsAreThinnedToBoundedCurves()
        {
            var random = new Random(3);
            var actual = Enumerable.Range(0, 20000).Select(_ => random.NextDouble() < 0.1).ToArray();
            var scores = actual.Select(a => random.NextDouble() + (a ? 0.3 : 0)).ToArray();
            var r = CurveMath.Roc(actual, scores);
            r.FalsePositiveRate.Length.Should().BeLessOrEqualTo(2000);
            r.FalsePositiveRate[^1].Should().Be(1);
        }
    }
}
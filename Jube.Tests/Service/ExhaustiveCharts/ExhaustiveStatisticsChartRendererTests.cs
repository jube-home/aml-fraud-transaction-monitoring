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
using System.Text.RegularExpressions;
using System.Xml;
using FluentAssertions;
using Jube.Charting.Core.Models;
using Jube.Dto.Query.ExhaustiveSearchInstanceVariableStatistics;
using Jube.Service.Charts;
using Xunit;

namespace Jube.Test.Service.ExhaustiveCharts
{
    [Trait("Category", "Service")]
    public sealed class ExhaustiveStatisticsChartRendererTests
    {
        private static List<ExhaustiveSearchInstanceVariableStatisticsBinDto> Bins(double start, double width,
            params int[] frequencies)
        {
            return
            [
                .. frequencies.Select((f, i) => new ExhaustiveSearchInstanceVariableStatisticsBinDto
                {
                    Start = start + i * width, End = start + (i + 1) * width, Frequency = f
                })
            ];
        }

        private static ExhaustiveSearchInstanceVariableStatisticsSummaryDto Summary(double mean, double sd,
            double min, double max, double iqr)
        {
            return new ExhaustiveSearchInstanceVariableStatisticsSummaryDto
            {
                Mean = mean, StandardDeviation = sd, Minimum = min, Maximum = max, Iqr = iqr
            };
        }

        private static ExhaustiveSearchInstanceVariableStatisticsDto Aligned(string name = "Amount")
        {
            return new ExhaustiveSearchInstanceVariableStatisticsDto
            {
                Id = 1, Name = name,
                Population = Summary(5, 2, 0, 10, 3),
                PopulationBins = Bins(0, 5, 60, 40),
                NegativeBins = Bins(0, 5, 55, 25),
                PositiveBins = Bins(0, 5, 5, 15),
                Anomaly = Summary(7.5, 1, 5, 10, 1)
            };
        }

        private static ExhaustiveSearchInstanceVariableStatisticsDto WithMonteCarlo(
            ExhaustiveSearchInstanceVariableStatisticsDto variable)
        {
            variable.PrescriptionBins = Bins(4, 2, 1, 6, 9, 4);
            variable.Prescription = Summary(7, 1.5, 4, 12, 2);

            return variable;
        }

        private static ExhaustiveSearchInstanceVariableStatisticsDto Older()
        {
            return new ExhaustiveSearchInstanceVariableStatisticsDto
            {
                Id = 2, Name = "Old",
                Population = Summary(5, 2, 0, 10, 3),
                PopulationBins = Bins(0, 2, 10, 30, 40, 20, 5),
                ClassificationBins = Bins(6, 1, 2, 9, 4)
            };
        }

        private static XmlDocument Load(string svg)
        {
            var document = new XmlDocument { XmlResolver = null };
            document.LoadXml(svg);

            return document;
        }

        private static List<string> Texts(string svg)
        {
            var document = Load(svg);

            return
            [
                .. document.GetElementsByTagName("text").Cast<XmlNode>().Select(n => n.InnerText),
                .. document.GetElementsByTagName("title").Cast<XmlNode>().Select(n => n.InnerText)
            ];
        }

        private static void Safe(string svg)
        {
            svg.Should().NotContain("<style");
            svg.Should().NotContain("style=");
            svg.Should().NotContainEquivalentOf("<script");
            svg.Should().NotContainEquivalentOf("foreignObject");
            Regex.IsMatch(svg, @"\son[a-z]+\s*=", RegexOptions.IgnoreCase).Should().BeFalse();
            svg.Should().NotContain("NaN").And.NotContain("Infinity");
        }

        [Fact]
        public void AlignedClassesAreTwoSeriesNegativeFirstThenPositiveInTheirFixedColourSlots()
        {
            var svg = ExhaustiveStatisticsChartRenderer.VariableClasses(Aligned());

            Safe(svg);
            var texts = Texts(svg);
            texts.Should().Contain("Amount").And.Contain("Negative class").And.Contain("Positive class");
            texts.IndexOf("Negative class").Should().BeLessThan(texts.IndexOf("Positive class"));
            svg.Should().Contain("s0").And.Contain("s1");
            Regex.Matches(svg, "class=\"sr s2").Count.Should().Be(0);
            texts.Should().NotContain("All rows");
        }

        [Fact]
        public void AlignedClassesAreShownAsShareOfClassByDefaultAndAsCountsOnRequest()
        {
            var share = Texts(ExhaustiveStatisticsChartRenderer.VariableClasses(Aligned()));
            var counts = Texts(ExhaustiveStatisticsChartRenderer.VariableClasses(Aligned(), true));

            share.Should().Contain("Share");
            share.Should().Contain(t => t.Contains("Negative class: 55") && t.Contains("68.8% of series"));
            counts.Should().Contain("Frequency");
            counts.Should().NotContain("Share");

            ExhaustiveStatisticsChartRenderer.VariableClasses(Aligned(), large: true).Should()
                .Contain(ExhaustiveStatisticsChartRenderer.AlignedCaption)
                .And.Contain("Share of class");

            ExhaustiveStatisticsChartRenderer.VariableClasses(Aligned(), true, true).Should()
                .Contain(ExhaustiveStatisticsChartRenderer.AlignedCountsCaption);
        }

        [Fact]
        public void EachClassIsNormalisedByItsOwnTotalSoTheImbalanceDoesNotHideThePositiveClass()
        {
            var variable = Aligned();
            variable.NegativeBins = Bins(0, 5, 800, 200);
            variable.PositiveBins = Bins(0, 5, 2, 8);

            var svg = ExhaustiveStatisticsChartRenderer.VariableClasses(variable);

            var texts = Texts(svg);
            texts.Should().Contain(t => t.Contains("Negative class: 800") && t.Contains("80%"));
            texts.Should().Contain(t => t.Contains("Positive class: 8") && t.Contains("80%"));
        }

        [Fact]
        public void ClassesAreAnEmptyStateWithoutDataWhateverTheVariableName()
        {
            var unknown = ExhaustiveStatisticsChartRenderer.VariableClasses(null);

            var blank = ExhaustiveStatisticsChartRenderer.VariableClasses(
                new ExhaustiveSearchInstanceVariableStatisticsDto { Id = 5, Name = "Secret" });

            unknown.Should().Contain(ExhaustiveStatisticsChartRenderer.EmptyMessage);
            blank.Should().Contain(ExhaustiveStatisticsChartRenderer.EmptyMessage);
            blank.Should().Be(unknown);
            blank.Should().NotContain("Secret");
            Safe(unknown);
        }

        [Fact]
        public void OlderInstancesShowAllRowsAndThePositiveClassAsDensitiesWithAClearCaption()
        {
            var svg = ExhaustiveStatisticsChartRenderer.VariableClasses(Older());

            Safe(svg);
            var texts = Texts(svg);
            texts.Should().Contain("All rows").And.Contain("Positive class");
            texts.Should().NotContain("Negative class");
            texts.Should().Contain("Density");
            svg.Should().Contain(ExhaustiveStatisticsChartRenderer.SeparateCaption);
            texts.Should().Contain(ExhaustiveStatisticsChartRenderer.SeparateShort);

            ExhaustiveStatisticsChartRenderer.VariableClasses(Older(), large: true).Should()
                .Contain("class histograms were kept");

            texts.IndexOf("All rows").Should().BeLessThan(texts.IndexOf("Positive class"));
        }

        [Fact]
        public void OlderInstanceDensitiesHaveUnitAreaOnASharedNumericAxis()
        {
            var svg = ExhaustiveStatisticsChartRenderer.VariableClasses(Older());

            var texts = Texts(svg);
            texts.Should().Contain(t => t.Contains("All rows: 10") && t.Contains("Density: 0.0476"));
            texts.Should().Contain(t => t.Contains("Positive class: 2") && t.Contains("Density: 0.133"));
        }

        [Fact]
        public void OlderInstanceWithAnAnomalyFallsBackToTheAnomalyRowsAsThePositiveClass()
        {
            var variable = Older();
            variable.ClassificationBins = [];
            variable.AnomalyBins = Bins(6, 2, 3, 1);

            Texts(ExhaustiveStatisticsChartRenderer.VariableClasses(variable)).Should().Contain("Positive class");
        }

        [Fact]
        public void OlderInstanceWithoutAnyPositiveClassSaysSoAndKeepsTheAllRowsSeries()
        {
            var variable = Older();
            variable.ClassificationBins = [];

            var texts = Texts(ExhaustiveStatisticsChartRenderer.VariableClasses(variable));

            texts.Should().Contain("All rows").And.NotContain("Positive class");
            texts.Should().Contain(ExhaustiveStatisticsChartRenderer.NotStoredShort);
        }

        [Fact]
        public void LargeViewIsBiggerThanTheGridCell()
        {
            var small = Load(ExhaustiveStatisticsChartRenderer.VariableClasses(Aligned())).DocumentElement!;

            var large = Load(ExhaustiveStatisticsChartRenderer.VariableClasses(Aligned(), large: true))
                .DocumentElement!;

            small.GetAttribute("viewBox").Should().NotBe(large.GetAttribute("viewBox"));
        }

        [Fact]
        public void MonteCarloOverlaysTheModelProfileOnTheNegativeAndPositiveClassDensities()
        {
            var svg = ExhaustiveStatisticsChartRenderer.MonteCarlo(WithMonteCarlo(Aligned()));

            Safe(svg);
            var texts = Texts(svg);

            texts.Should().Contain("Negative class").And.Contain("Positive class")
                .And.Contain("Model profile (Monte Carlo)");

            texts.IndexOf("Positive class").Should().BeLessThan(texts.IndexOf("Model profile (Monte Carlo)"));
            texts.Should().Contain("Density");

            texts.Should().Contain(t => t.Contains("Model profile (Monte Carlo): 9") &&
                                        t.Contains("Density: 0.225"));

            svg.Should().Contain("s2");
        }

        [Fact]
        public void MonteCarloOnAnOlderInstanceUsesAllRowsAndTheSeparatelyBinnedPositiveClass()
        {
            var variable = Older();
            WithMonteCarlo(variable);

            var texts = Texts(ExhaustiveStatisticsChartRenderer.MonteCarlo(variable));

            texts.Should().Contain("All rows").And.Contain("Positive class")
                .And.Contain("Model profile (Monte Carlo)");

            texts.Should().NotContain("Negative class");
        }

        [Fact]
        public void MonteCarloWithoutAnyClassDataKeepsTheModelProfileInItsOwnColourSlot()
        {
            var variable = new ExhaustiveSearchInstanceVariableStatisticsDto { Id = 3, Name = "Only" };
            WithMonteCarlo(variable);

            var svg = ExhaustiveStatisticsChartRenderer.MonteCarlo(variable);

            Safe(svg);
            var texts = Texts(svg);
            texts.Should().Contain("Model profile (Monte Carlo)");
            texts.Should().Contain(t => t.Contains("(not available)"));
            svg.Should().Contain("s2");
        }

        [Fact]
        public void MonteCarloCountsShowsFrequencyInsteadOfDensity()
        {
            var variable = WithMonteCarlo(Aligned());

            var density = Texts(ExhaustiveStatisticsChartRenderer.MonteCarlo(variable));
            var counts = Texts(ExhaustiveStatisticsChartRenderer.MonteCarlo(variable, true));

            density.Should().Contain("Density").And.NotContain("Count");
            counts.Should().Contain("Count").And.NotContain("Density");
        }

        [Fact]
        public void MonteCarloEmptyStateNamesTheCausesAndIsIdenticalForUnknownAndUnprescribedVariables()
        {
            var unknown = ExhaustiveStatisticsChartRenderer.MonteCarlo(null);
            var unprescribed = ExhaustiveStatisticsChartRenderer.MonteCarlo(Aligned("Named"));

            unprescribed.Should().Be(unknown);
            unknown.Should().Contain("No Monte Carlo statistics");

            ExhaustiveStatisticsChartRenderer.MonteCarlo(null, large: true).Should()
                .Contain("a variable the model does not use");

            unknown.Should().NotContain("Named");

            ExhaustiveStatisticsChartRenderer.MonteCarloSummary(Aligned()).Should()
                .Contain("No Monte Carlo statistics");
        }

        [Fact]
        public void MonteCarloSummaryCompareseachStatisticInStandardDeviationsOfAllRows()
        {
            var variable = WithMonteCarlo(Aligned());
            variable.Classification = Summary(8, 1, 6, 10, 1.5);
            variable.Anomaly = null;

            var svg = ExhaustiveStatisticsChartRenderer.MonteCarloSummary(variable);

            Safe(svg);
            var texts = Texts(svg);

            texts.Should().Contain("Mean").And.Contain("Standard deviation").And.Contain("Minimum")
                .And.Contain("Maximum").And.Contain("Interquartile range");

            texts.Should().Contain("Negative class").And.Contain("Positive class")
                .And.Contain("Model profile (Monte Carlo)");

            svg.Should().Contain(ExhaustiveStatisticsChartRenderer.SummaryCaption);
            texts.Should().Contain(t => t.Contains("1.5") && t.Contains("Positive class"));
            texts.Should().Contain(t => t.Contains("1") && t.Contains("Model profile (Monte Carlo)"));
        }

        [Fact]
        public void NegativeSummaryIsThePopulationWhenTheClassificationSelectedThePositiveRows()
        {
            var variable = Aligned();
            variable.Classification = Summary(8, 1, 6, 10, 1.5);

            ExhaustiveStatisticsChartRenderer.NegativeSummary(variable).Should().BeSameAs(variable.Population);
        }

        [Fact]
        public void NegativeSummaryMeanAndDeviationAreExactBySubtractionFromThePopulation()
        {
            double[] negative = [1, 2, 3, 4, 5, 6, 7, 8];
            double[] positive = [9, 10, 11];
            var all = negative.Concat(positive).ToArray();

            static double Sd(double[] v)
            {
                var m = v.Average();

                return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / (v.Length - 1));
            }

            var variable = new ExhaustiveSearchInstanceVariableStatisticsDto
            {
                Population = Summary(all.Average(), Sd(all), 1, 11, 5),
                Anomaly = Summary(positive.Average(), Sd(positive), 9, 11, 1),
                NegativeBins = Bins(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0),
                PositiveBins = Bins(1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1)
            };

            var result = ExhaustiveStatisticsChartRenderer.NegativeSummary(variable)!;

            result.Mean.Should().BeApproximately(negative.Average(), 1e-9);
            result.StandardDeviation.Should().BeApproximately(Sd(negative), 1e-9);
            result.Minimum.Should().Be(1);
        }

        [Fact]
        public void NegativeSummaryIsUnavailableWithoutAnAnomalyOrClassificationSummary()
        {
            ExhaustiveStatisticsChartRenderer.NegativeSummary(new ExhaustiveSearchInstanceVariableStatisticsDto
            {
                NegativeBins = Bins(0, 1, 5), PositiveBins = Bins(0, 1, 5)
            }).Should().BeNull();
        }

        [Fact]
        public void QuantileInterpolatesInsideTheBin()
        {
            var bins = Bins(0, 10, 10, 10);

            ExhaustiveStatisticsChartRenderer.Quantile(bins, 0.5).Should().BeApproximately(10, 1e-9);
            ExhaustiveStatisticsChartRenderer.Quantile(bins, 0.25).Should().BeApproximately(5, 1e-9);
            ExhaustiveStatisticsChartRenderer.Quantile(bins, 0.75).Should().BeApproximately(15, 1e-9);
        }

        [Fact]
        public void EdgesRejectOverlapsNonFiniteValuesAndWidenAConstantBin()
        {
            ExhaustiveStatisticsChartRenderer.Edges([]).Should().BeNull();
            ExhaustiveStatisticsChartRenderer.Edges(Bins(0, 1, 1, 1)).Should().Equal(0, 1, 2);

            ExhaustiveStatisticsChartRenderer.Edges(
                    [new ExhaustiveSearchInstanceVariableStatisticsBinDto { Start = 3, End = 3, Frequency = 4 }])
                .Should().Equal(2.5, 3.5);

            ExhaustiveStatisticsChartRenderer.Edges(
            [
                new ExhaustiveSearchInstanceVariableStatisticsBinDto { Start = 0, End = 2, Frequency = 1 },
                new ExhaustiveSearchInstanceVariableStatisticsBinDto { Start = 1, End = 3, Frequency = 1 }
            ]).Should().BeNull();

            ExhaustiveStatisticsChartRenderer.Edges(
                [
                    new ExhaustiveSearchInstanceVariableStatisticsBinDto { Start = double.NaN, End = 1, Frequency = 1 }
                ])
                .Should().BeNull();
        }

        [Fact]
        public void HostileNamesAreEscapedAndOutputIsDeterministicAndExternalByDefault()
        {
            var name = "<script>alert(1)</script>\"><img src=x onerror=alert(1)>&amp;'</title><svg onload=alert(1)>";
            var variable = WithMonteCarlo(Aligned(name));

            foreach (var svg in new[]
                     {
                         ExhaustiveStatisticsChartRenderer.VariableClasses(variable),
                         ExhaustiveStatisticsChartRenderer.MonteCarlo(variable),
                         ExhaustiveStatisticsChartRenderer.MonteCarloSummary(variable)
                     })
            {
                svg.Should().NotContain("<style").And.NotContain("style=");
                svg.Should().NotContain("<img").And.NotContain("<script");
                Load(svg).SelectNodes("//*[@*[starts-with(name(), 'on')]]")!.Count.Should().Be(0);
                Regex.IsMatch(svg, "<svg[^>]* class=\"jc\"").Should().BeTrue();
                Texts(svg).Should().Contain(t => t.Contains("alert(1)"));
            }

            ExhaustiveStatisticsChartRenderer.VariableClasses(variable)
                .Should().Be(ExhaustiveStatisticsChartRenderer.VariableClasses(variable));
        }

        [Fact]
        public void NonFiniteAndHugeNumbersNeverBreakTheSvg()
        {
            var variable = Aligned();
            variable.Population = Summary(double.MaxValue, double.MaxValue, -double.MaxValue, double.MaxValue, 0);
            variable.NegativeBins = Bins(-1e300, 1e300, int.MaxValue, int.MaxValue);
            variable.PositiveBins = Bins(-1e300, 1e300, 0, 0);
            WithMonteCarlo(variable);
            variable.Prescription = Summary(1e308, 1e308, -1e308, 1e308, 1e308);

            foreach (var svg in new[]
                     {
                         ExhaustiveStatisticsChartRenderer.VariableClasses(variable),
                         ExhaustiveStatisticsChartRenderer.VariableClasses(variable, true),
                         ExhaustiveStatisticsChartRenderer.MonteCarlo(variable),
                         ExhaustiveStatisticsChartRenderer.MonteCarloSummary(variable)
                     })
            {
                Load(svg);
            }
        }

        [Fact]
        public void InlineStyleModeStillGivesASelfContainedSvg()
        {
            ExhaustiveStatisticsChartRenderer.VariableClasses(Aligned(), styleMode: ChartStyleMode.Inline)
                .Should().Contain("<style>");
        }

        [Fact]
        public void AChartIdKeepsTheTitleAndDescriptionIdsUnique()
        {
            var svg = ExhaustiveStatisticsChartRenderer.VariableClasses(Aligned(), id: "ex-vc-1-2");

            svg.Should().Contain("id=\"ex-vc-1-2-t\"");
        }
    }
}
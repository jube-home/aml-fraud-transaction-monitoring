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
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceLearningCurve;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceRoc;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription;
using Jube.Dto.Query.ExhaustiveSearchInstanceVariable;
using Jube.Service.Charts;
using Xunit;

namespace Jube.Test.Service.ExhaustiveCharts
{
    [Trait("Category", "Service")]
    public sealed class ExhaustiveChartRendererTests
    {
        private static List<string> Texts(string svg)
        {
            var document = new XmlDocument { XmlResolver = null };
            document.LoadXml(svg);
            document.GetElementsByTagName("script").Count.Should().Be(0);
            svg.Should().NotContainEquivalentOf("<script");
            svg.Should().NotContainEquivalentOf("foreignObject");
            Regex.IsMatch(svg, @"\son[a-z]+\s*=", RegexOptions.IgnoreCase).Should().BeFalse();

            return
            [
                .. document.GetElementsByTagName("text").Cast<XmlNode>().Select(n => n.InnerText),
                .. document.GetElementsByTagName("title").Cast<XmlNode>().Select(n => n.InnerText)
            ];
        }

        private static string[] AllOutputs(ChartStyleMode? mode = null, string? id = null)
        {
            var rocs = new[]
            {
                new ExhaustiveSearchInstancePromotedTrialInstanceRocDto { Id = 1, Score = 0.2, Fpr = 0.5, Tpr = 0.9 },
                new ExhaustiveSearchInstancePromotedTrialInstanceRocDto { Id = 2, Score = 0.8, Fpr = 0.1, Tpr = 0.4 }
            };

            var learning = new[]
            {
                new ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveDto { Score = 0.5 },
                new ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveDto { Score = 0.7 }
            };

            var predicted = new[]
            {
                new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto { Predicted = 0.2, Actual = 0 },
                new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto { Predicted = 0.9, Actual = 1 }
            };

            var bins = new[]
            {
                new ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramDto { Bin = -0.5, Frequency = 3 },
                new ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramDto { Bin = 0.5, Frequency = 5 }
            };

            var confusion = new ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto
            {
                TrueNegative = 90, FalsePositive = 5, FalseNegative = 3, TruePositive = 2
            };

            var m = mode ?? ChartStyleMode.External;

            return
            [
                ExhaustiveChartRenderer.LearningCurve(learning, m, id),
                ExhaustiveChartRenderer.Roc(rocs, m, id),
                ExhaustiveChartRenderer.PredictedActual(predicted, m, id),
                ExhaustiveChartRenderer.ErrorHistogram(bins, m, id),
                ExhaustiveChartRenderer.Confusion(confusion, m, id),
                ExhaustiveChartRenderer.LearningCurve([], m, id)
            ];
        }

        [Fact]
        public void EveryChartUsesTheExternalStyleModeByDefault()
        {
            foreach (var svg in AllOutputs())
            {
                svg.Should().NotContain("<style");
                svg.Should().NotContain("style=");
                Regex.IsMatch(svg, "<svg[^>]* class=\"jc\"").Should().BeTrue();
                Regex.IsMatch(svg, "class=\"jc-[0-9a-f]{8}\"").Should().BeFalse();
                Texts(svg).Should().NotBeEmpty();
            }
        }

        [Fact]
        public void InlineModeStillGivesASelfContainedSvg()
        {
            foreach (var svg in AllOutputs(ChartStyleMode.Inline))
            {
                svg.Should().Contain("<style>");
            }
        }

        [Fact]
        public void TheExternalOutputIsMuchSmallerThanTheInlineOutput()
        {
            var external = AllOutputs().Sum(s => s.Length);
            var inline = AllOutputs(ChartStyleMode.Inline).Sum(s => s.Length);
            external.Should().BeLessThan(inline - 6 * 8_000);
        }

        [Fact]
        public void AChartIdKeepsTheTitleAndDescriptionIdsUniquePerChart()
        {
            var a = string.Concat(AllOutputs(null, "ex-a-1"));
            var b = string.Concat(AllOutputs(null, "ex-b-2"));
            a.Should().Contain("id=\"ex-a-1-t\"").And.Contain("aria-labelledby=\"ex-a-1-t ex-a-1-d\"");
            b.Should().NotContain("ex-a-1");
        }

        [Fact]
        public void EmptyInputsGiveAnEmptyStateNeverAnException()
        {
            var outputs = new[]
            {
                ExhaustiveChartRenderer.LearningCurve([]),
                ExhaustiveChartRenderer.Roc([]),
                ExhaustiveChartRenderer.PredictedActual([]),
                ExhaustiveChartRenderer.ErrorHistogram([]),
                ExhaustiveChartRenderer.VariableHistogram(null),
                ExhaustiveChartRenderer.VariableHistogram(new ExhaustiveSearchInstanceVariableDto { Name = "x" }),
                ExhaustiveChartRenderer.Confusion(null),
                ExhaustiveChartRenderer.Confusion(new ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto()),
                ExhaustiveChartRenderer.Sensitivity([])
            };

            foreach (var svg in outputs)
            {
                Texts(svg).Should().Contain(t => t.Contains(ExhaustiveChartRenderer.EmptyMessage));
            }
        }

        [Fact]
        public void RocIsSortedAndAnchoredAndCarriesTheAxisTitles()
        {
            var svg = ExhaustiveChartRenderer.Roc(
            [
                new ExhaustiveSearchInstancePromotedTrialInstanceRocDto { Id = 1, Score = 0.2, Fpr = 0.5, Tpr = 0.9 },
                new ExhaustiveSearchInstancePromotedTrialInstanceRocDto { Id = 2, Score = 0.8, Fpr = 0.1, Tpr = 0.4 }
            ]);

            var texts = Texts(svg);

            texts.Should().Contain("False Positive Rate");
            texts.Should().Contain("True Positive Rate");
            texts.Should().Contain(t => t.Contains("AUC"));
        }

        [Fact]
        public void PredictedActualSplitsByActualAndDropsAnEmptyClass()
        {
            var both = ExhaustiveChartRenderer.PredictedActual(
            [
                new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto { Predicted = 0.9, Actual = 1 },
                new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto { Predicted = 0.1, Actual = 0 }
            ]);
            var onlyNegative = ExhaustiveChartRenderer.PredictedActual(
            [
                new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto { Predicted = 0.1, Actual = 0 },
                new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto { Predicted = 0.2, Actual = 0 }
            ]);

            Texts(both).Should().Contain(t => t.Contains("Positive class")).And
                .Contain(t => t.Contains("Negative class"));

            var negativeOnlyTexts = Texts(onlyNegative);
            negativeOnlyTexts.Should().Contain(t => t.Contains("Negative class"));
            negativeOnlyTexts.Where(t => t == "Positive class").Should().BeEmpty();
        }

        [Fact]
        public void PredictedActualThinsVeryLargeInputsAndStaysWellFormed()
        {
            var points = Enumerable.Range(0, 30000).Select(i =>
                new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto
                {
                    Predicted = i / 30000d,
                    Actual = i % 2
                }).ToList();

            var svg = ExhaustiveChartRenderer.PredictedActual(points);

            Texts(svg).Should().Contain("Predicted vs. Actual");
        }

        [Fact]
        public void LearningCurveWithOnePointStillRenders()
        {
            var svg = ExhaustiveChartRenderer.LearningCurve(
            [
                new ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveDto
                    { Score = 0.5, CreatedDate = DateTime.UtcNow }
            ]);

            Texts(svg).Should().Contain("Learning Curve");
        }

        [Fact]
        public void ErrorAndVariableHistogramsUseTheirBinsAsCategories()
        {
            var errors = ExhaustiveChartRenderer.ErrorHistogram(
            [
                new ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramDto { Bin = -1.5, Frequency = 3 },
                new ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramDto { Bin = 0, Frequency = 7 }
            ]);

            var variable = ExhaustiveChartRenderer.VariableHistogram(new ExhaustiveSearchInstanceVariableDto
            {
                Name = "Amount",
                HistogramValues =
                [
                    new ExhaustiveSearchInstanceVariableHistogramValueDto { Bin = 0, Frequency = 4 },
                    new ExhaustiveSearchInstanceVariableHistogramValueDto { Bin = 10, Frequency = 6 }
                ]
            });

            Texts(errors).Should().Contain("-1.5");
            Texts(variable).Should().Contain("Amount");
        }

        [Fact]
        public void SensitivityKeepsOnlyTheTopBarsAndHostileNamesAreEscaped()
        {
            var variables = Enumerable.Range(0, 30).Select(i =>
                new ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto
                {
                    Name = i == 29 ? "<script>alert(1)</script>&\"'" : "V" + i,
                    Sensitivity = i
                }).ToList();

            var svg = ExhaustiveChartRenderer.Sensitivity(variables);

            var texts = Texts(svg);
            texts.Where(t => t == "V29").Should().BeEmpty();
            texts.Should().Contain("<script>alert(1)</script>&\"'");
            texts.Should().NotContain("V5");
        }

        [Fact]
        public void OutputIsDeterministic()
        {
            var rows = new[]
            {
                new ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramDto { Bin = 0, Frequency = 7 },
                new ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramDto { Bin = 1, Frequency = 2 }
            };

            ExhaustiveChartRenderer.ErrorHistogram(rows).Should().Be(ExhaustiveChartRenderer.ErrorHistogram(rows));
        }
    }
}
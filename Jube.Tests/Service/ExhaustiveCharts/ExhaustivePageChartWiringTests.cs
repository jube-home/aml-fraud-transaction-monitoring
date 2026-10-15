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
using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Jube.Test.Service.ExhaustiveCharts
{
    [Trait("Category", "Service")]
    public sealed class ExhaustivePageChartWiringTests
    {
        private static string Read(string relative)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Jube.sln")))
            {
                directory = directory.Parent;
            }

            directory.Should().NotBeNull();

            return File.ReadAllText(Path.Combine(directory!.FullName, relative));
        }

        [Theory]
        [InlineData("Jube.App/Pages/Model/Frame/Exhaustive.cshtml")]
        [InlineData("Jube.App/wwwroot/js/Model/Frame/Exhaustive.js")]
        [InlineData("Jube.App/wwwroot/js/Model/Frame/ExhaustiveCharts.js")]
        public void ExhaustivePageAndScriptsDoNotReferenceKendoChart(string path)
        {
            var content = Read(path);

            content.Should().NotContainEquivalentOf("kendoChart");
            content.Should().NotContainEquivalentOf("kendo.dataviz");
        }

        [Fact]
        public void ExhaustivePageLoadsTheChartScriptAndScriptUsesEveryChartEndpoint()
        {
            Read("Jube.App/Pages/Model/Frame/Exhaustive.cshtml").Should()
                .Contain("/js/Model/Frame/ExhaustiveCharts.js");

            var script = Read("Jube.App/wwwroot/js/Model/Frame/Exhaustive.js") +
                         Read("Jube.App/wwwroot/js/Model/Frame/ExhaustiveCharts.js");

            foreach (var route in new[]
                     {
                         "/api/Chart/Exhaustive/LearningCurve/", "/api/Chart/Exhaustive/PromotionHistory/",
                         "/api/Chart/Exhaustive/VariableHistogram/", "/api/Chart/Exhaustive/Measures/",
                         "/api/Chart/Exhaustive/PerformanceConfusion/", "/api/Chart/Exhaustive/ScoreDistribution/",
                         "/api/Chart/Exhaustive/PerformanceRoc/", "/api/Chart/Exhaustive/PrecisionRecall/",
                         "/api/Chart/Exhaustive/Calibration/", "/api/Chart/Exhaustive/Gains/",
                         "/api/Chart/Exhaustive/Lift/", "/api/Chart/Exhaustive/Ks/",
                         "/api/Chart/Exhaustive/ThresholdSweep/", "/api/Chart/Exhaustive/Topology/"
                     })
            {
                script.Should().Contain(route);
            }
        }

        [Fact]
        public void PageNoLongerDrawsPredictedVersusActualTheErrorHistogramOrTheHtmlConfusionTable()
        {
            var script = Read("Jube.App/wwwroot/js/Model/Frame/Exhaustive.js") +
                         Read("Jube.App/wwwroot/js/Model/Frame/ExhaustiveCharts.js");

            var page = Read("Jube.App/Pages/Model/Frame/Exhaustive.cshtml");

            script.Should().NotContain("/api/Chart/Exhaustive/PredictedActual/");
            script.Should().NotContain("/api/Chart/Exhaustive/ErrorHistogram/");
            script.Should().NotContain("GetExhaustiveSearchInstancePromotedTrialInstanceConfusion");
            page.Should().NotContain("id=\"RightChart\"");
            page.Should().NotContain("id=\"LeftChart\"");
            page.Should().NotContain("TPRowTotal");
            page.Should().NotContain("<table style=\"table-border-color-light");
        }

        [Fact]
        public void EveryPerformanceContainerInThePageIsFilledByTheScriptOnce()
        {
            var page = Read("Jube.App/Pages/Model/Frame/Exhaustive.cshtml");
            var script = Read("Jube.App/wwwroot/js/Model/Frame/ExhaustiveCharts.js");

            var containers = new[]
            {
                "TopologyChart", "PerformanceMeasures", "PerformanceConfusion", "ScoreDistributionChart",
                "PerformanceRocChart",
                "PrecisionRecallChart", "CalibrationChart", "GainsChart", "LiftChart", "KsChart",
                "ThresholdSweepChart"
            };

            foreach (var container in containers)
            {
                page.Should().Contain("id=\"" + container + "\"");
                Regex.Matches(script, "\"#" + container + "\"").Count.Should().Be(1);
            }

            page.Should().Contain("id=\"PromotionChart\"");
        }

        [Fact]
        public void EveryChartHasAMatchingNarrativeDivAndGroup()
        {
            var page = Read("Jube.App/Pages/Model/Frame/Exhaustive.cshtml");
            var script = Read("Jube.App/wwwroot/js/Model/Frame/ExhaustiveCharts.js");

            var chartToNarrative = new (string Chart, string NarrativeDiv, string Group)[]
            {
                ("PerformanceMeasures", "NarrativePerformance", "Performance"),
                ("PerformanceConfusion", "NarrativeConfusion", "Confusion"),
                ("ScoreDistributionChart", "NarrativeScoreDistribution", "ScoreDistribution"),
                ("PerformanceRocChart", "NarrativeDiscrimination", "Discrimination"),
                ("PrecisionRecallChart", "NarrativePrecisionRecall", "PrecisionRecall"),
                ("CalibrationChart", "NarrativeCalibration", "Calibration"),
                ("GainsChart", "NarrativeGains", "Gains"),
                ("LiftChart", "NarrativeLift", "Lift"),
                ("KsChart", "NarrativeKs", "Ks"),
                ("ThresholdSweepChart", "NarrativeThresholdSweep", "ThresholdSweep"),
                ("TopologyChart", "NarrativeSensitivity", "Sensitivity"),
                ("PromotionChart", "NarrativeLearningCurve", "LearningCurve"),
                ("statistics", "NarrativePrescription", "Prescription")
            };

            foreach (var (chart, narrativeDiv, group) in chartToNarrative)
            {
                page.Should().Contain("id=\"" + chart + "\"", $"the {chart} chart must be on the page");

                page.Should().Contain("id=\"" + narrativeDiv + "\"",
                    $"the {chart} chart must have a matching narrative div ({narrativeDiv})");

                script.Should().Contain("\"#" + narrativeDiv + "\", \"" + group + "\"",
                    $"{narrativeDiv} must be registered against the '{group}' narrative group");
            }
        }

        [Fact]
        public void PerformanceEndpointsAreGatedLikeTheOtherChartEndpointsAndAnswerSvgWithTheSameHeaders()
        {
            var endpoint = Read("Jube.App/Endpoints/ChartsEndpoints.cs");

            endpoint.Should().Contain(".RequireAuthorization()");
            endpoint.Should().Contain("\"default-src 'none'\"");
            endpoint.Should().Contain("nosniff");
            endpoint.Should().Contain("no-store");
            endpoint.Should().NotContain("style-src");
            Read("Jube.App/Startup.cs").Should().Contain("endpoints.MapChartsEndpoints();");
        }

        [Fact]
        public void ChartScriptCarriesNoInlineStyleSoAStrictStyleSrcWorks()
        {
            var script = Read("Jube.App/wwwroot/js/Model/Frame/ExhaustiveCharts.js");

            script.Should().NotContain("style=");
            script.Should().NotContain(".css(");
            script.Should().Contain("jc-message");
        }

        [Fact]
        public void TheSharedLayoutCarriesTheChartStylesheetSoInjectedMarkupIsStyledByTheSelfPolicy()
        {
            Read("Jube.App/Pages/Shared/_Layout.cshtml").Should()
                .Contain("/css/jube-charting.css?v=@Jube.Charting.ChartStylesheet.Version");
            Read("Jube.App/Pages/Model/Frame/Exhaustive.cshtml").Should().NotContain("jube-charting.css");
        }

        [Fact]
        public void ChartEndpointsNoLongerNeedInlineStylesInTheirContentSecurityPolicy()
        {
            var endpoint = Read("Jube.App/Endpoints/ChartsEndpoints.cs");

            endpoint.Should().Contain("\"default-src 'none'\"");
            endpoint.Should().NotContain("style-src");
            endpoint.Should().NotContain("unsafe-inline");
        }

        [Fact]
        public void ChartScriptInjectsMarkupAndCarriesNoScriptExecution()
        {
            var script = Read("Jube.App/wwwroot/js/Model/Frame/ExhaustiveCharts.js");

            script.Should().Contain(".html(");
            script.Should().NotContain("eval(");
            script.Should().NotContain("new Function");
        }
    }
}
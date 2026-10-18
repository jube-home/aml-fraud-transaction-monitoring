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
using FluentAssertions;
using Xunit;

namespace Jube.Test.Service.ExhaustiveCharts
{
    [Trait("Category", "Service")]
    public sealed class ExhaustiveStatisticsPageWiringTests
    {
        private const string Script = "Jube.App/wwwroot/js/Model/Frame/ExhaustiveStatisticsClasses.js";

        private static string Read(string relative)
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, relative)))
            {
                directory = directory.Parent;
            }

            directory.Should().NotBeNull();

            return File.ReadAllText(Path.Combine(directory!.FullName, relative));
        }

        [Fact]
        public void PageIncludesTheStatisticsScriptAfterTheChartScript()
        {
            var page = Read("Jube.App/Pages/Model/Frame/Exhaustive.cshtml");

            page.Should().Contain("/js/Model/Frame/ExhaustiveStatisticsClasses.js");
            page.IndexOf("ExhaustiveCharts.js").Should().BeLessThan(page.IndexOf("ExhaustiveStatisticsClasses.js"));
        }

        [Fact]
        public void ScriptUsesTheGridClassesMonteCarloSummaryAndSingleVariableRoutes()
        {
            var script = Read(Script);

            foreach (var route in new[]
                     {
                         "/api/Chart/ExhaustiveStatistics/", "Grid/", "VariableClasses/", "MonteCarlo/",
                         "MonteCarloSummary/"
                     })
            {
                script.Should().Contain(route);
            }

            script.Should().Contain("loadExhaustiveChart(").And.Contain("jc-message");
        }

        [Fact]
        public void ScriptCarriesNoInlineStyleKendoChartOrComment()
        {
            var script = Read(Script);

            script.Should().NotContain("style=");
            script.Should().NotContain(".css(");
            script.Should().NotContainEquivalentOf("kendoChart");
            var body = script[(script.IndexOf("*/", StringComparison.Ordinal) + 2)..];
            body.Replace("//# sourceURL", string.Empty).Should().NotContain("//");
            body.Should().NotContain("/*");
        }

        [Fact]
        public void TheStatisticsGridDelegatesToTheScriptAndKeepsTheSingleChartFallback()
        {
            var script = Read("Jube.App/wwwroot/js/Model/Frame/Exhaustive.js");

            script.Should().Contain("loadExhaustiveStatisticsGrid(grid, id)");
            script.Should().Contain("exhaustiveStatisticsLoadDetail");
            script.Should().Contain("/api/Chart/Exhaustive/VariableHistogram/");
        }

        [Fact]
        public void EndpointsAreRegisteredAdditivelyAndKeepTheHardenedSvgHeaders()
        {
            var startup = Read("Jube.App/Startup.cs");
            var endpoint = Read("Jube.App/Endpoints/ChartsEndpoints.cs");

            startup.Should().Contain("endpoints.MapChartsEndpoints();");
            endpoint.Should().Contain("RequireAuthorization()");
            endpoint.Should().Contain("\"default-src 'none'\"").And.Contain("nosniff").And.Contain("no-store");
            endpoint.Should().NotContain("style-src").And.NotContain("unsafe-inline");

            foreach (var route in new[] { "VariableClasses/", "MonteCarlo/", "MonteCarloSummary/", "Grid/" })
            {
                endpoint.Should().Contain(route);
            }
        }

        [Fact]
        public void NoNewSourceWordsRefertoTheExcludedLibraryName()
        {
            foreach (var path in new[]
                     {
                         Script, "Jube.App/Endpoints/ChartsEndpoints.cs",
                         "Jube.Service/Charts/ExhaustiveStatisticsChartRenderer.cs",
                         "Jube.Service/Charts/ExhaustiveStatisticsChartService.cs",
                         "Jube.Engine/Exhaustive/Statistics/ClassHistogramComputer.cs"
                     })
            {
                Read(path).Should().NotContain("Accord.", "the new charting and service code is standalone");
            }
        }
    }
}
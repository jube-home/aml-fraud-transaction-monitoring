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

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using FluentAssertions;
using Jube.Dto.Query.EntityAnalysisModelIntegrity;
using Jube.Service.Charts;
using Xunit;

namespace Jube.Test.Service.ExhaustiveCharts
{
    [Trait("Category", "Service")]
    public sealed class ModelDependencyChartRendererTests
    {
        private static ModelGraphDto Graph()
        {
            return new ModelGraphDto
            {
                Nodes =
                [
                    new ModelGraphNodeDto { Id = "ActivationRule:1", Label = "High value", Kind = "ActivationRule", Active = true },
                    new ModelGraphNodeDto { Id = "TtlCounter:2", Label = "Spend", Kind = "TtlCounter", Active = false },
                    new ModelGraphNodeDto { Id = "RequestXPath:3", Label = "Amount", Kind = "RequestXPath", Active = true },
                    new ModelGraphNodeDto { Id = "Missing:Gone", Label = "Gone", Kind = "Missing", Active = false }
                ],
                Edges =
                [
                    new ModelGraphEdgeDto { From = "ActivationRule:1", To = "TtlCounter:2", Label = "Spend > 10" },
                    new ModelGraphEdgeDto { From = "TtlCounter:2", To = "RequestXPath:3", Label = "setting", Dashed = true },
                    new ModelGraphEdgeDto { From = "ActivationRule:1", To = "Missing:Gone", Label = "Gone" }
                ]
            };
        }

        [Fact]
        public void EveryKindFallsInOneOfTheSevenFamiliesAndMissingIsItsOwn()
        {
            var kinds = new Dictionary<string, string>
            {
                ["RequestXPath"] = "Inputs", ["InlineScriptProperty"] = "Inputs", ["InlineFunction"] = "Logic",
                ["InlineScript"] = "Logic", ["GatewayRule"] = "Logic", ["AbstractionRule"] = "Abstraction",
                ["AbstractionCalculation"] = "Abstraction", ["ActivationRule"] = "Decisions",
                ["ReprocessingRule"] = "Decisions", ["TtlCounter"] = "Data", ["Sanction"] = "Data", ["List"] = "Data",
                ["Dictionary"] = "Data", ["HttpAdaptation"] = "Integrations", ["ExhaustiveAdaptation"] = "Integrations",
                ["CaseWorkflow"] = "Integrations", ["Missing"] = "Missing"
            };

            foreach (var (kind, family) in kinds)
            {
                ModelDependencyChartRenderer.GroupOf(kind).Should().Be(family, kind);
            }

            ModelDependencyChartRenderer.Groups.Should().HaveCount(7);
        }

        [Fact]
        public void TheGraphKeepsStableFamilyColoursMarksInactiveNodesDashedAndNamesMissingOnesInRed()
        {
            var svg = ModelDependencyChartRenderer.Render(Graph(), false);

            new XmlDocument { XmlResolver = null }.Invoking(d => d.LoadXml(svg)).Should().NotThrow();
            Regex.IsMatch(svg, "nd s3[^>]*data-node=\"ActivationRule:1\"").Should().BeTrue();
            Regex.IsMatch(svg, "nd s4[^>]*data-node=\"TtlCounter:2\"").Should().BeTrue();
            Regex.IsMatch(svg, "nd s0[^>]*data-node=\"RequestXPath:3\"").Should().BeTrue();
            Regex.IsMatch(svg, "nd s7[^>]*data-node=\"Missing:Gone\"").Should().BeTrue();
            Regex.Matches(svg, "class=\"nb dash\"").Count.Should().Be(1);
            svg.Should().Contain("data-from=\"ActivationRule:1\"").And.Contain("data-to=\"Missing:Gone\"");
        }

        [Fact]
        public void SeveralDependenciesBetweenTheSameTwoEntitiesBecomeOneArrowAndDanglingEdgesAreDropped()
        {
            var graph = Graph();

            graph.Edges.Add(new ModelGraphEdgeDto { From = "ActivationRule:1", To = "TtlCounter:2", Label = "Spend < 5" });
            graph.Edges.Add(new ModelGraphEdgeDto { From = "ActivationRule:1", To = "Nowhere:9", Label = "x" });

            var svg = ModelDependencyChartRenderer.Render(graph, false);

            Regex.Matches(svg, "data-from=\"ActivationRule:1\" data-to=\"TtlCounter:2\"").Count.Should().Be(1);
            svg.Should().Contain("Spend &gt; 10, Spend &lt; 5").And.NotContain("Nowhere");
        }

        [Fact]
        public void EntitiesWithNoArcsAreDroppedFromTheDiagram()
        {
            var graph = Graph();

            graph.Nodes.Add(new ModelGraphNodeDto { Id = "List:7", Label = "Loose list", Kind = "List", Active = true });

            var svg = ModelDependencyChartRenderer.Render(graph, false);

            svg.Should().NotContain("Loose list").And.Contain("High value");
            Regex.Matches(svg, "data-node=\"").Count.Should().Be(4);
        }

        [Fact]
        public void AModelWhoseEntitiesDoNotUseEachOtherStillDrawsAndSaysSo()
        {
            var graph = new ModelGraphDto
            {
                Nodes = [new ModelGraphNodeDto { Id = "List:7", Label = "Loose list", Kind = "List", Active = true }]
            };

            var svg = ModelDependencyChartRenderer.Render(graph, false);

            svg.Should().Contain("No entity uses another").And.NotContain("Loose list");
        }

        [Fact]
        public void TheThemeFollowsTheCallerAndHostileNamesAreEscaped()
        {
            var graph = Graph();

            graph.Nodes[0].Label = "<script>alert(1)</script>";

            var light = ModelDependencyChartRenderer.Render(Graph(), false);
            var dark = ModelDependencyChartRenderer.Render(Graph(), true);
            var hostile = ModelDependencyChartRenderer.Render(graph, false);

            light.Should().NotBe(dark);
            hostile.Should().NotContainEquivalentOf("<script");
        }
    }
}

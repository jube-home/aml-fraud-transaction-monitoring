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
using FluentAssertions;
using Jube.Charting.Diagrams;
using Jube.Dto.Query.EntityAnalysisModelIntegrity;
using Jube.Service.Charts;
using Xunit;

namespace Jube.Test.Service.ExhaustiveCharts
{
    [Trait("Category", "Service")]
    public sealed class ModelDependencyClusterTests
    {
        private static readonly string[] Kinds =
            ["RequestXPath", "AbstractionRule", "ActivationRule", "TtlCounter", "Dictionary", "List", "Sanction"];

        private static ModelGraphDto Model(int perKind, int edgesPerNode, int flagged = 0)
        {
            var graph = new ModelGraphDto();
            var random = new Random(7);

            foreach (var kind in Kinds)
            {
                for (var i = 0; i < perKind; i++)
                {
                    graph.Nodes.Add(new ModelGraphNodeDto
                        { Id = kind + ":" + i, Label = kind + " name " + i, Kind = kind, Active = true });
                }
            }

            for (var f = 0; f < flagged; f++)
            {
                graph.Nodes[f * 3].Warnings = 1;
            }

            var seen = new HashSet<(string, string)>();

            foreach (var node in graph.Nodes)
            {
                for (var e = 0; e < edgesPerNode; e++)
                {
                    var target = graph.Nodes[random.Next(graph.Nodes.Count)];

                    if (target.Id != node.Id && seen.Add((node.Id, target.Id)))
                    {
                        graph.Edges.Add(new ModelGraphEdgeDto { From = node.Id, To = target.Id, Label = "uses " + e });
                    }
                }
            }

            return graph;
        }

        private static double Width(string svg)
        {
            return double.Parse(Regex.Match(svg, "viewBox=\"0 0 ([0-9.]+)").Groups[1].Value,
                CultureInfo.InvariantCulture);
        }

        [Fact]
        public void ALargeModelDrawsAsOneBoxPerKindWithACount()
        {
            var svg = ModelDependencyChartRenderer.Render(Model(20, 2), false);

            foreach (var kind in Kinds)
            {
                svg.Should().Contain("data-node=\"Cluster:" + kind + "\"");
            }

            svg.Should().Contain("Activation rule ×20");
            Regex.Matches(svg, "data-node=\"").Count.Should().Be(Kinds.Length);
        }

        [Fact]
        public void ASmallModelIsDrawnInFullWithNoBoxesGrouped()
        {
            var svg = ModelDependencyChartRenderer.Render(Model(3, 1), false);

            svg.Should().NotContain("data-node=\"Cluster:");
        }

        [Fact]
        public void ExpandingAKindDrawsItsEntitiesIndividuallyAndLeavesTheOthersGrouped()
        {
            var svg = ModelDependencyChartRenderer.Render(Model(20, 2), false, ["TtlCounter"]);

            svg.Should().NotContain("data-node=\"Cluster:TtlCounter\"");
            svg.Should().Contain("data-node=\"TtlCounter:0\"");
            svg.Should().Contain("data-node=\"Cluster:Dictionary\"");
        }

        [Fact]
        public void AnEmptyExpansionListMeansEverythingGroupedEvenForASmallModel()
        {
            var svg = ModelDependencyChartRenderer.Render(Model(3, 1), false, []);

            svg.Should().Contain("data-node=\"Cluster:");
        }

        [Fact]
        public void EntitiesWithFindingsStayOnTheirOwnAndAreRingedWhileTheRestAreGrouped()
        {
            var graph = Model(20, 2, 3);

            var svg = ModelDependencyChartRenderer.Render(graph, false);

            var flagged = graph.Nodes.Where(n => n.Warnings > 0).Select(n => n.Id).ToList();

            foreach (var id in flagged)
            {
                svg.Should().Contain("data-node=\"" + id + "\"");
            }

            Regex.Matches(svg, "class=\"nb s3\"").Count.Should().BeGreaterThanOrEqualTo(flagged.Count);
        }

        [Fact]
        public void EdgesBetweenGroupedBoxesAreMergedAndCountTheirUses()
        {
            var svg = ModelDependencyChartRenderer.Render(Model(20, 2), false);

            Regex.Matches(svg, "data-from=\"Cluster:ActivationRule\" data-to=\"Cluster:TtlCounter\"").Count
                .Should().BeLessThanOrEqualTo(1);

            svg.Should().MatchRegex("[0-9]+ uses");
        }

        [Fact]
        public void ExpandingEverythingOnAHugeModelStillDrawsInsideTheCanvas()
        {
            var graph = Model(60, 3, 40);

            var svg = ModelDependencyChartRenderer.Render(graph, false, Kinds);

            Width(svg).Should().BeLessThanOrEqualTo(DiagramLimits.MaxCanvas);
            Regex.Matches(svg, "data-node=\"").Count.Should().BeLessThanOrEqualTo(DiagramLimits.MaxGraphNodes);
        }

        [Fact]
        public void AModelWithEveryEntityFlaggedStillDraws()
        {
            var graph = Model(40, 3);

            foreach (var node in graph.Nodes)
            {
                node.Errors = 1;
            }

            var svg = ModelDependencyChartRenderer.Render(graph, false);

            Width(svg).Should().BeLessThanOrEqualTo(DiagramLimits.MaxCanvas);
        }

        [Fact]
        public void AThousandEntitiesAndManyUsesStillDrawInsideTheCanvas()
        {
            var graph = Model(150, 4, 20);

            var svg = ModelDependencyChartRenderer.Render(graph, true, Kinds);

            Width(svg).Should().BeLessThanOrEqualTo(DiagramLimits.MaxCanvas);
        }
    }
}

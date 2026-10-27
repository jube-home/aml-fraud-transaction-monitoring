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

// ReSharper disable NotAccessedPositionalProperty.Global

using Jube.Charting.Core.Models;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Jube.Dto.Query.EntityAnalysisModelIntegrity;

namespace Jube.Service.Charts
{
    public static class ModelDependencyChartRenderer
    {
        public const string MissingGroup = "Missing";

        public static readonly string[] Groups =
            ["Inputs", "Logic", "Abstraction", "Decisions", "Data", "Integrations", MissingGroup];

        public static string GroupOf(string kind)
        {
            return kind switch
            {
                "Missing" => MissingGroup,
                "RequestXPath" or "InlineScriptProperty" => "Inputs",
                "InlineFunction" or "InlineScript" or "GatewayRule" => "Logic",
                "AbstractionRule" or "AbstractionCalculation" => "Abstraction",
                "ActivationRule" or "ReprocessingRule" => "Decisions",
                "TtlCounter" or "Sanction" or "List" or "Dictionary" => "Data",
                _ => "Integrations"
            };
        }

        public static string Render(ModelGraphDto graph, bool dark)
        {
            var builder = Diagram.Graph()
                .Title("Model dependencies")
                .Subtitle("Each entity points to the entities it uses. Dashed lines are settings, dashed boxes are " +
                          "inactive and red boxes are names that do not exist.")
                .Theme(dark ? ThemeMode.Dark : ThemeMode.Light)
                .Layout(GraphLayoutKind.Layered)
                .MaxExtent(8000)
                .GroupOrder(Groups);

            var known = graph.Nodes.Select(node => node.Id).ToHashSet();

            var arcs = graph.Edges.Where(edge => known.Contains(edge.From) && known.Contains(edge.To))
                .GroupBy(edge => (edge.From, edge.To)).ToList();

            var connected = arcs.SelectMany(pair => new[] { pair.Key.From, pair.Key.To }).ToHashSet();

            foreach (var node in graph.Nodes.Where(node => connected.Contains(node.Id)))
            {
                var detail = node.Kind + (node.Active || node.Kind == "Missing" ? string.Empty : ", inactive") +
                             (string.IsNullOrWhiteSpace(node.Detail) ? string.Empty : ". " + node.Detail);

                builder.Node(node.Id, node.Label, GraphNodeRole.Feature, detail, GroupOf(node.Kind),
                    !node.Active && node.Kind != "Missing");
            }

            foreach (var pair in arcs)
            {
                var labels = pair.Select(edge => edge.Label).Where(label => !string.IsNullOrWhiteSpace(label))
                    .Distinct().ToList();

                builder.Edge(pair.Key.From, pair.Key.To,
                    label: labels.Count == 0 ? null : string.Join(", ", labels),
                    dashed: pair.All(edge => edge.Dashed));
            }

            if (connected.Count == 0)
            {
                builder.Node("none", "No entity uses another", GraphNodeRole.Other);
            }

            return builder.Render();
        }
    }
}

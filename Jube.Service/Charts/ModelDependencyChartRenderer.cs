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
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Diagrams.Models;
using Jube.Dto.Query.EntityAnalysisModelIntegrity;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Charts
{
    public static class ModelDependencyChartRenderer
    {
        public const string MissingGroup = "Missing";
        public const int CollapseAbove = 40;
        public const int MaxPerLayer = 12;

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

        public static string Render(ModelGraphDto graph, bool dark, IReadOnlyCollection<string>? expanded = null,
            IStringLocalizer? strings = null)
        {
            DiagramTooLargeException? last = null;

            foreach (var drawing in ModelDependencyClusterer.Levels(graph, expanded, strings))
            {
                try
                {
                    return Build(drawing, dark, strings).Render();
                }
                catch (DiagramTooLargeException exception)
                {
                    last = exception;
                }
            }

            throw (Exception?)last ?? new InvalidOperationException("The dependency diagram had nothing to draw.");
        }

        private static GraphDiagramBuilder Build(ModelDependencyDrawing drawing, bool dark, IStringLocalizer? strings)
        {
            var builder = Diagram.Graph()
                .Title(ChartText.Of(strings, ModelDependencyChartResources.Title, "Model dependencies"))
                .Subtitle(ChartText.Of(strings, ModelDependencyChartResources.Subtitle, "Each entity points to the entities it uses. Dashed lines are settings, dashed boxes are inactive and red boxes are names that do not exist."))
                .Theme(dark ? ThemeMode.Dark : ThemeMode.Light)
                .Layout(GraphLayoutKind.Layered)
                .Direction(GraphDirection.LeftToRight)
                .MaxPerLayer(MaxPerLayer)
                .MaxExtent(DiagramLimits.MaxCanvas - 100)
                .GroupOrder(Groups);

            foreach (var node in drawing.Nodes)
            {
                builder.Node(new GraphNode(node.Id, node.Label, GraphNodeRole.Feature, node.Detail)
                    { Group = node.Group, Dashed = node.Dashed, Emphasis = node.Emphasis });
            }

            foreach (var edge in drawing.Edges)
            {
                builder.Edge(edge.From, edge.To, label: string.IsNullOrWhiteSpace(edge.Label) ? null : edge.Label,
                    dashed: edge.Dashed);
            }

            if (drawing.Nodes.Count == 0)
            {
                builder.Node("none", ChartText.Of(strings, ModelDependencyChartResources.NoEntity, "No entity uses another"), GraphNodeRole.Other);
            }

            return builder;
        }
    }
}

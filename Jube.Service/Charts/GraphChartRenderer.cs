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

using System.Globalization;
using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Icons;
using Jube.Charting.Diagrams.Models;
using Jube.Dto.Query.Graph;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Charts
{
    public static class GraphChartRenderer
    {
        public const int MaxLabel = 28;
        private const double MaxExtentPixels = 4000;

        public static (string Svg, IReadOnlyList<GraphAnchor> Positions) Render(GraphNeighbourhoodDto graph,
            GraphRenderRequestDto request, int layoutId, IStringLocalizer? strings = null)
        {
            var builder = Diagram.Graph()
                .Theme(request.Dark ? ThemeMode.Dark : ThemeMode.Light)
                .Layout(LayoutOf(layoutId))
                .MaxExtent(MaxExtentPixels)
                .SizeBy(MeasureOf(request.MeasureId));
            var keys = graph.Nodes.Select(n => n.Key ?? string.Empty).Distinct().OrderBy(k => k, StringComparer.Ordinal)
                .ToList();
            var seedKey = graph.Seed?.Key ?? keys.FirstOrDefault() ?? string.Empty;
            builder.GroupOrder(keys.OrderBy(k => k == seedKey ? 0 : 1).ThenBy(k => k, StringComparer.Ordinal).ToArray());
            var nodeIcons = NodeIcons(graph.Definitions);
            var edgeIcons = graph.Definitions.Where(d => GraphIcons.Exists(d.IconName))
                .GroupBy(d => d.Guid).ToDictionary(g => g.Key, g => g.First().IconName);
            var nodeIds = graph.Nodes.Select(n => n.Id.ToString(CultureInfo.InvariantCulture)).ToHashSet();

            foreach (var node in graph.Nodes.OrderBy(n => n.Depth).ThenBy(n => n.Id))
            {
                var id = node.Id.ToString(CultureInfo.InvariantCulture);
                var badges = new List<GraphBadge>();

                if (node.Cases.Confirmed > 0)
                {
                    badges.Add(new GraphBadge(GraphBadgeKind.Confirmed));
                }

                if (node.Cases.Open > 0)
                {
                    badges.Add(new GraphBadge(GraphBadgeKind.OpenCase, node.Cases.Open.ToString(CultureInfo.InvariantCulture)));
                }

                if (node.Hub)
                {
                    badges.Add(new GraphBadge(GraphBadgeKind.Hub, node.Degree.ToString(CultureInfo.InvariantCulture)));
                }

                builder.Node(id, Label(node), node.Hub ? GraphNodeShape.Hexagon : GraphNodeShape.Circle,
                    SizeOf(node, request.MeasureId), node.Key, badges.Count == 0 ? null : badges, Detail(node, strings),
                    IconOf(node.Key, nodeIcons));
                builder.Depth(id, node.Depth);
                builder.Lane(id, node.Key ?? string.Empty);
                builder.Time(id, node.FirstSeen);
            }

            if (graph.Seed != null)
            {
                builder.Seed(graph.Seed.Id.ToString(CultureInfo.InvariantCulture));
            }

            foreach (var combo in graph.Combos)
            {
                var id = "c:" + combo.Id;
                builder.Node(id, combo.Count.ToString(CultureInfo.InvariantCulture) + " " + combo.Key, GraphNodeShape.Stack,
                    null, combo.Key, [new GraphBadge(GraphBadgeKind.Cluster, combo.Count.ToString(CultureInfo.InvariantCulture))],
                    Text(strings, GraphResources.DetailCollapsed, "{0} nodes collapsed", combo.Count),
                    IconOf(combo.Key, nodeIcons));
                builder.Edge(combo.ViaNodeId.ToString(CultureInfo.InvariantCulture), id, dashed: true);
            }

            foreach (var edge in graph.Edges.OrderBy(e => e.Id))
            {
                var from = edge.FromNodeId.ToString(CultureInfo.InvariantCulture);
                var to = edge.ToNodeId.ToString(CultureInfo.InvariantCulture);

                if (!nodeIds.Contains(from) || !nodeIds.Contains(to))
                {
                    continue;
                }

                builder.Edge(new GraphEdge(from, to)
                {
                    Id = edge.Id.ToString(CultureInfo.InvariantCulture),
                    Weight = edge.Weight,
                    Label = edge.DefinitionName,
                    Dashed = edge.KindId == 1 && edge.Specificity < 0.5,
                    First = edge.WindowFirst ?? edge.FirstSeen,
                    Last = edge.WindowLast ?? edge.LastSeen,
                    Directed = edge.KindId != 1,
                    Count = edge.WindowCount > 0 ? edge.WindowCount : edge.Count,
                    Icon = edgeIcons.GetValueOrDefault(edge.DefinitionGuid)
                });
            }

            foreach (var anchor in request.Anchors.Where(a => !string.IsNullOrEmpty(a.Id)))
            {
                builder.Anchor(anchor.Id!, anchor.X, anchor.Y);
            }

            if (request.Captions.Count > 0)
            {
                builder.Subtitle(string.Join(" ", request.Captions.Take(3)));
            }

            var diagram = builder.Build();

            return (diagram.Render(), diagram.Layout().Positions);
        }

        public static string TimeStrip(IReadOnlyList<DateTimeOffset> times, int bins, bool dark)
        {
            return Chart.TimeStrip()
                .Theme(dark ? ThemeMode.Dark : ThemeMode.Light)
                .Times(times, Math.Clamp(bins, 6, 120))
                .Render();
        }

        public static GraphLayoutKind LayoutOf(int layoutId)
        {
            return layoutId switch
            {
                2 => GraphLayoutKind.Radial,
                3 => GraphLayoutKind.Flow,
                4 => GraphLayoutKind.Layered,
                _ => GraphLayoutKind.Force
            };
        }

        private static GraphMeasure MeasureOf(int? measureId)
        {
            return measureId switch
            {
                0 => GraphMeasure.None,
                2 => GraphMeasure.Amount,
                3 => GraphMeasure.Weight,
                _ => GraphMeasure.Degree
            };
        }

        private static double? SizeOf(GraphNodeDto node, int? measureId)
        {
            return measureId switch
            {
                0 => null,
                2 => node.DegreeMoney,
                3 => node.PageRank ?? node.Degree,
                _ => node.Degree
            };
        }

        private static Dictionary<string, string> NodeIcons(IEnumerable<GraphEdgeDefinitionDto> definitions)
        {
            var icons = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var definition in definitions)
            {
                if (!string.IsNullOrEmpty(definition.FromNodeKey) && GraphIcons.Exists(definition.FromNodeIcon))
                {
                    icons.TryAdd(definition.FromNodeKey, definition.FromNodeIcon!);
                }

                if (!string.IsNullOrEmpty(definition.ToNodeKey) && GraphIcons.Exists(definition.ToNodeIcon))
                {
                    icons.TryAdd(definition.ToNodeKey, definition.ToNodeIcon!);
                }
            }

            return icons;
        }

        private static string IconOf(string? key, Dictionary<string, string> configured)
        {
            return key is not null && configured.TryGetValue(key, out var icon) ? icon : GraphIcons.ForNodeKey(key);
        }

        private static string Label(GraphNodeDto node)
        {
            var value = node.Value ?? string.Empty;

            if (value.Length <= MaxLabel)
            {
                return value;
            }

            var length = MaxLabel - 1;

            if (char.IsHighSurrogate(value[length - 1]))
            {
                length--;
            }

            return value[..length] + "\u2026";
        }

        private static string Text(IStringLocalizer? strings, string key, string fallback, params object[] arguments)
        {
            var format = strings == null ? fallback : strings[key].Value;

            return string.Format(CultureInfo.InvariantCulture, format, arguments);
        }

        private static string Detail(GraphNodeDto node, IStringLocalizer? strings)
        {
            var parts = new List<string>
            {
                node.Key ?? string.Empty, Text(strings, GraphResources.DetailDegree, "degree {0}", node.Degree)
            };

            if (node.Hub)
            {
                parts.Add(Text(strings, GraphResources.DetailHub, "hub"));
            }

            if (node.DistanceToConfirmed.HasValue)
            {
                parts.Add(Text(strings, GraphResources.DetailConfirmedDistance, "{0} hop(s) from a confirmed case",
                    node.DistanceToConfirmed.Value));
            }

            if (node.Cases.Open > 0)
            {
                parts.Add(Text(strings, GraphResources.DetailOpenCases, "{0} open case(s)", node.Cases.Open));
            }

            if (node.Cases.Confirmed > 0)
            {
                parts.Add(Text(strings, GraphResources.DetailConfirmedCases, "{0} confirmed", node.Cases.Confirmed));
            }

            return string.Join(", ", parts);
        }
    }
}

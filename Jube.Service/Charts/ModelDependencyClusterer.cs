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

using System.Text.RegularExpressions;
using Jube.Charting.Diagrams.Models;
using Jube.Dto.Query.EntityAnalysisModelIntegrity;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Charts
{
    internal static class ModelDependencyClusterer
    {
        public const string ClusterPrefix = "Cluster:";

        public static IEnumerable<ModelDependencyDrawing> Levels(ModelGraphDto graph,
            IReadOnlyCollection<string>? expanded, IStringLocalizer? strings)
        {
            var known = graph.Nodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);

            var arcs = graph.Edges.Where(edge => known.Contains(edge.From) && known.Contains(edge.To)
                                                 && edge.From != edge.To).ToList();

            var connected = arcs.SelectMany(edge => new[] { edge.From, edge.To }).ToHashSet(StringComparer.Ordinal);
            var nodes = graph.Nodes.Where(node => connected.Contains(node.Id)).ToList();

            var open = expanded is null
                ? nodes.Count <= ModelDependencyChartRenderer.CollapseAbove
                    ? nodes.Select(node => node.Kind).ToHashSet(StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal)
                : expanded.ToHashSet(StringComparer.Ordinal);

            var demoted = new HashSet<string>(StringComparer.Ordinal);

            yield return Compose(nodes, arcs, open, demoted, strings);

            while (true)
            {
                var biggest = nodes.Where(node => open.Contains(node.Kind))
                    .GroupBy(node => node.Kind).OrderByDescending(group => group.Count())
                    .ThenBy(group => group.Key, StringComparer.Ordinal).FirstOrDefault();

                if (biggest is null)
                {
                    break;
                }

                open.Remove(biggest.Key);

                yield return Compose(nodes, arcs, open, demoted, strings);
            }

            while (true)
            {
                var flagged = nodes.Where(node => Flagged(node) && node.Kind != ModelDependencyChartRenderer.MissingGroup
                                                                && !demoted.Contains(node.Id))
                    .OrderBy(node => node.Errors * 1000 + node.Warnings).ThenBy(node => node.Id, StringComparer.Ordinal)
                    .ToList();

                if (flagged.Count == 0)
                {
                    break;
                }

                foreach (var node in flagged.Take((flagged.Count + 1) / 2))
                {
                    demoted.Add(node.Id);
                }

                yield return Compose(nodes, arcs, open, demoted, strings);
            }

            foreach (var node in nodes.Where(node => node.Kind == ModelDependencyChartRenderer.MissingGroup))
            {
                demoted.Add(node.Id);
            }

            yield return Compose(nodes, arcs, open, demoted, strings);
        }

        public static string ClusterId(string kind)
        {
            return ClusterPrefix + kind;
        }

        public static string KindName(string kind)
        {
            var spaced = Regex.Replace(kind, "(?<=[a-z])(?=[A-Z])", " ");

            return spaced.Length == 0
                ? spaced
                : (spaced[..1] + spaced[1..].ToLowerInvariant()).Replace("Ttl", "TTL").Replace("ttl", "TTL");
        }

        private static bool Flagged(ModelGraphNodeDto node)
        {
            return node.Errors > 0 || node.Warnings > 0;
        }

        private static ModelDependencyDrawing Compose(List<ModelGraphNodeDto> nodes,
            List<ModelGraphEdgeDto> arcs, HashSet<string> open, HashSet<string> demoted, IStringLocalizer? strings)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            var drawn = new List<ModelDependencyDrawnNode>();
            var clusters = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var node in nodes)
            {
                var individual = !demoted.Contains(node.Id)
                                 && (node.Kind == ModelDependencyChartRenderer.MissingGroup
                                     || open.Contains(node.Kind)
                                     || Flagged(node));

                if (individual)
                {
                    map[node.Id] = node.Id;

                    var detail = node.Kind
                                 + (node.Active || node.Kind == ModelDependencyChartRenderer.MissingGroup
                                     ? string.Empty
                                     : ChartText.Of(strings, ModelDependencyChartResources.Inactive, ", inactive"))
                                 + (string.IsNullOrWhiteSpace(node.Detail) ? string.Empty : ". " + node.Detail)
                                 + (Flagged(node)
                                     ? ". " + ChartText.Of(strings, ModelDependencyChartResources.Findings,
                                         "{0} errors, {1} warnings", node.Errors, node.Warnings)
                                     : string.Empty);

                    drawn.Add(new ModelDependencyDrawnNode(node.Id, node.Label, node.Kind, detail,
                        ModelDependencyChartRenderer.GroupOf(node.Kind),
                        !node.Active && node.Kind != ModelDependencyChartRenderer.MissingGroup,
                        node.Errors > 0 ? GraphEmphasis.Error : node.Warnings > 0 ? GraphEmphasis.Warning : GraphEmphasis.None));
                }
                else
                {
                    map[node.Id] = ClusterId(node.Kind);
                    clusters[node.Kind] = clusters.GetValueOrDefault(node.Kind) + 1;
                }
            }

            foreach (var (kind, count) in clusters.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                drawn.Add(new ModelDependencyDrawnNode(ClusterId(kind),
                    ChartText.Of(strings, ModelDependencyChartResources.Cluster, "{0} ×{1}", KindName(kind), count),
                    kind,
                    ChartText.Of(strings, ModelDependencyChartResources.ClusterDetail,
                        "{0} entities of this kind; select to show each one", count),
                    ModelDependencyChartRenderer.GroupOf(kind), false, GraphEmphasis.None));
            }

            var edges = new List<ModelDependencyDrawnEdge>();

            foreach (var pair in arcs.GroupBy(edge => (From: map[edge.From], To: map[edge.To])))
            {
                if (pair.Key.From == pair.Key.To)
                {
                    continue;
                }

                var items = pair.ToList();

                var aggregated = pair.Key.From.StartsWith(ClusterPrefix, StringComparison.Ordinal)
                                 || pair.Key.To.StartsWith(ClusterPrefix, StringComparison.Ordinal);

                var labels = items.Select(edge => edge.Label).Where(label => !string.IsNullOrWhiteSpace(label))
                    .Distinct(StringComparer.Ordinal).ToList();

                var label = aggregated
                    ? ChartText.Of(strings, ModelDependencyChartResources.Uses, "{0} uses", items.Count)
                    : string.Join(", ", labels);

                edges.Add(new ModelDependencyDrawnEdge(pair.Key.From, pair.Key.To, label, items.All(edge => edge.Dashed)));
            }

            return new ModelDependencyDrawing(drawn, edges);
        }
    }
}

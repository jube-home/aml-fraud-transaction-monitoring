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

namespace Jube.Engine.Integrity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Parser.Dependency;

    public sealed record ModelGraphNode(string Id, string Label, string Kind, bool Active, string Detail);

    public sealed record ModelGraphEdge(string From, string To, string Label, bool Dashed);

    public sealed record ModelGraph(
        IReadOnlyList<ModelGraphNode> Nodes,
        IReadOnlyList<ModelGraphEdge> Edges,
        bool Truncated,
        string Focus);

    public static class ModelGraphBuilder
    {
        public const int MaxNodes = 150;
        public const string Missing = "Missing";

        public static string NodeId(ModelEntity entity)
        {
            return $"{entity.Kind}:{entity.Id}";
        }

        public static ModelGraph Build(ModelDependencyGraph graph, ModelEntity focus = null, int radius = 2,
            int maxNodes = MaxNodes)
        {
            ArgumentNullException.ThrowIfNull(graph);

            var nodes = new Dictionary<string, ModelGraphNode>(StringComparer.Ordinal);
            foreach (var entity in graph.Entities)
            {
                nodes.TryAdd(NodeId(entity), new ModelGraphNode(NodeId(entity), entity.Name, entity.Kind.ToString(),
                    entity.Active, entity.Active ? null : "Inactive"));
            }

            var edges = new List<ModelGraphEdge>();
            foreach (var dependency in graph.Dependencies)
            {
                var name = string.IsNullOrEmpty(dependency.Namespace)
                    ? dependency.Name
                    : $"{dependency.Namespace}.{dependency.Name}";
                string to;
                if (dependency.Target == null)
                {
                    to = $"{Missing}:{name}";
                    nodes.TryAdd(to, new ModelGraphNode(to, name, Missing, false, "Names nothing that exists"));
                }
                else
                {
                    to = NodeId(dependency.Target);
                }

                var from = NodeId(dependency.Dependent);
                if (from == to)
                {
                    continue;
                }

                var ruleText = dependency.Kind == ModelDependencyKind.RuleText;
                var edge = new ModelGraphEdge(from, to, ruleText ? name : dependency.Kind.ToString(), !ruleText);
                if (!edges.Contains(edge))
                {
                    edges.Add(edge);
                }
            }

            var keep = focus == null
                ? nodes.Keys.ToHashSet(StringComparer.Ordinal)
                : Neighbourhood(NodeId(focus), edges, radius);
            var truncated = false;

            if (keep.Count > maxNodes)
            {
                var degree = edges.SelectMany(e => new[] { e.From, e.To })
                    .GroupBy(n => n, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
                var focusId = focus == null ? null : NodeId(focus);
                keep = keep
                    .OrderByDescending(n => n == focusId)
                    .ThenByDescending(n => degree.GetValueOrDefault(n))
                    .ThenBy(n => n, StringComparer.Ordinal)
                    .Take(maxNodes)
                    .ToHashSet(StringComparer.Ordinal);
                truncated = true;
            }

            return new ModelGraph(
                nodes.Values.Where(n => keep.Contains(n.Id)).OrderBy(n => n.Kind, StringComparer.Ordinal)
                    .ThenBy(n => n.Label, StringComparer.Ordinal).ToList(),
                edges.Where(e => keep.Contains(e.From) && keep.Contains(e.To)).ToList(),
                truncated,
                focus == null ? null : NodeId(focus));
        }

        private static HashSet<string> Neighbourhood(string focus, List<ModelGraphEdge> edges, int radius)
        {
            var keep = new HashSet<string>(StringComparer.Ordinal) { focus };
            var frontier = new List<string> { focus };
            for (var step = 0; step < Math.Max(radius, 0) && frontier.Count > 0; step++)
            {
                var next = new List<string>();
                foreach (var edge in edges)
                {
                    if (frontier.Contains(edge.From) && keep.Add(edge.To))
                    {
                        next.Add(edge.To);
                    }

                    if (frontier.Contains(edge.To) && keep.Add(edge.From))
                    {
                        next.Add(edge.From);
                    }
                }

                frontier = next;
            }

            return keep;
        }
    }
}
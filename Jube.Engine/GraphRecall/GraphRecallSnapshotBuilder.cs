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

namespace Jube.Engine.GraphRecall
{
    public sealed class GraphRecallSnapshotBuilder(int tenantRegistryId, bool hashKeys, int maximumNodes,
        int maximumEdges, int distanceCap)
    {
        private readonly List<GraphRecallDefinition> definitions = [];
        private readonly Dictionary<Guid, Dictionary<(long From, long To), GraphRecallEdge>> edges = new();
        private readonly HashSet<long> nodeIds = [];
        private readonly Dictionary<string, GraphRecallNodeIndex> nodes = new(StringComparer.Ordinal);
        private readonly SortedSet<string> tags = new(StringComparer.Ordinal);

        public int NodeCount { get; private set; }
        public int EdgeCount { get; private set; }
        public bool Full => NodeCount >= maximumNodes;
        public bool Truncated { get; private set; }
        public bool EdgesTruncated { get; private set; }

        public bool TryAddNode(string key, string value, in GraphRecallNode node)
        {
            if (Full)
            {
                Truncated = true;

                return false;
            }

            if (!nodes.TryGetValue(key, out var index))
            {
                index = new GraphRecallNodeIndex(hashKeys);
                nodes.Add(key, index);
            }

            index.Add(value, node);
            nodeIds.Add(node.Id);
            NodeCount += 1;

            if (node.Tags == null)
            {
                return true;
            }

            foreach (var tag in node.Tags)
            {
                tags.Add(tag.Tag);
            }

            return true;
        }

        public bool TryAddEdge(long fromNodeId, long toNodeId, Guid registryGuid, GraphRecallEdge edge)
        {
            if (!nodeIds.Contains(fromNodeId) || !nodeIds.Contains(toNodeId))
            {
                return false;
            }

            if (!edges.TryGetValue(registryGuid, out var registryEdges))
            {
                registryEdges = new Dictionary<(long From, long To), GraphRecallEdge>();
                edges.Add(registryGuid, registryEdges);
            }

            var pair = (fromNodeId, toNodeId);

            if (registryEdges.TryGetValue(pair, out var existing))
            {
                registryEdges[pair] = existing.Merge(edge);

                return true;
            }

            if (EdgeCount >= maximumEdges)
            {
                EdgesTruncated = true;

                return false;
            }

            registryEdges.Add(pair, edge);
            EdgeCount += 1;

            return true;
        }

        public void AddDefinition(GraphRecallDefinition definition)
        {
            definitions.Add(definition);
        }

        public GraphRecallSnapshot Build(TimeSpan loadTime, long bytes)
        {
            return new GraphRecallSnapshot(tenantRegistryId, nodes, edges, definitions, [.. tags], distanceCap,
                Truncated, EdgesTruncated, loadTime, bytes);
        }
    }
}

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
using System.Linq;

namespace Jube.Engine.GraphRecall
{
    public sealed class GraphRecallSnapshot
    {
        private readonly IReadOnlyDictionary<Guid, Dictionary<(long From, long To), GraphRecallEdge>> edges;
        private readonly IReadOnlyDictionary<string, GraphRecallNodeIndex> nodes;

        public GraphRecallSnapshot(int tenantRegistryId, IReadOnlyDictionary<string, GraphRecallNodeIndex> nodes,
            IReadOnlyDictionary<Guid, Dictionary<(long From, long To), GraphRecallEdge>> edges,
            IReadOnlyList<GraphRecallDefinition> definitions, IReadOnlyList<string> tags, int distanceCap,
            bool truncated, bool edgesTruncated, TimeSpan loadTime, long bytes)
        {
            this.nodes = nodes;
            this.edges = edges;
            Definitions = definitions;
            TenantRegistryId = tenantRegistryId;
            Tags = tags;
            DistanceCap = distanceCap;
            Truncated = truncated;
            EdgesTruncated = edgesTruncated;
            LoadTime = loadTime;
            Bytes = bytes;
            NodeCount = nodes.Values.Sum(index => index.Count);
            EdgeCount = edges.Values.Sum(registry => registry.Count);
        }

        public int TenantRegistryId { get; }
        public IReadOnlyList<GraphRecallDefinition> Definitions { get; }
        public IReadOnlyList<string> Tags { get; }
        public int DistanceCap { get; }
        public bool Truncated { get; }
        public bool EdgesTruncated { get; }
        public TimeSpan LoadTime { get; }
        public long Bytes { get; }
        public int NodeCount { get; }
        public int EdgeCount { get; }
        public IEnumerable<string> NodeKeys => nodes.Keys;

        public bool TryGetNode(string nodeKey, string value, out GraphRecallNode node)
        {
            if (nodeKey != null && nodes.TryGetValue(nodeKey, out var index))
            {
                return index.TryGet(value, out node);
            }

            node = default;

            return false;
        }

        public bool TryGetEdge(long a, long b, Guid? registryGuid, out GraphRecallEdge edge)
        {
            if (registryGuid.HasValue)
            {
                edge = default;

                return edges.TryGetValue(registryGuid.Value, out var registryEdges) &&
                       TryGetEitherWay(registryEdges, a, b, out edge);
            }

            var found = false;
            edge = default;

            foreach (var registryEdges in edges.Values)
            {
                if (!TryGetEitherWay(registryEdges, a, b, out var candidate))
                {
                    continue;
                }

                edge = found ? edge.Merge(candidate) : candidate;
                found = true;
            }

            return found;
        }

        public bool Connects(string nodeKeyA, string nodeKeyB, Guid? registryGuid)
        {
            return Definitions.Any(d => (!registryGuid.HasValue || d.RegistryGuid == registryGuid.Value) &&
                                        d.Connects(nodeKeyA, nodeKeyB));
        }

        private static bool TryGetEitherWay(Dictionary<(long From, long To), GraphRecallEdge> registryEdges, long a,
            long b, out GraphRecallEdge edge)
        {
            return registryEdges.TryGetValue((a, b), out edge) || registryEdges.TryGetValue((b, a), out edge);
        }
    }
}

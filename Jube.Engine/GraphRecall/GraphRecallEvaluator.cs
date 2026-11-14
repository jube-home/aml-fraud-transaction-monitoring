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
using Jube.Dictionary;
using Jube.Dto.Options;
using Jube.GraphRecallProtocol;

namespace Jube.Engine.GraphRecall
{
    public static class GraphRecallEvaluator
    {
        private const int TaggedWithinHops = 2;

        private static readonly GraphPatternKind[] PatternKinds = Enum.GetValues<GraphPatternKind>();

        public static void Evaluate(GraphRecallPlan plan, GraphRecallSnapshot snapshot, string[] values,
            DateTime referenceDate, PooledDictionary<string, GraphEntity> target)
        {
            var count = plan.Entities.Count;

            if (count == 0)
            {
                return;
            }

            var beyond = plan.Beyond;
            var present = new bool[count];
            var known = new bool[count];
            var nodes = new GraphRecallNode[count];
            var entities = new GraphEntity[count];
            var taggedNearby = new bool[count];
            var taggedNearbyCount = 0;

            for (var i = 0; i < count; i++)
            {
                var definition = plan.Entities[i];
                var value = i < values.Length ? values[i] : null;
                present[i] = !string.IsNullOrEmpty(value);

                known[i] = present[i] && snapshot != null &&
                           snapshot.TryGetNode(definition.NodeKey, value, out nodes[i]);

                entities[i] = new GraphEntity(beyond) { Value = value ?? string.Empty, ReportTable = definition.ReportTable };

                if (!known[i])
                {
                    continue;
                }

                Fill(entities[i], in nodes[i], beyond);

                foreach (var proximity in entities[i].Tags.Values)
                {
                    if (proximity.Distance > TaggedWithinHops)
                    {
                        continue;
                    }

                    taggedNearby[i] = true;
                    taggedNearbyCount++;

                    break;
                }
            }

            for (var i = 0; i < count; i++)
            {
                var entity = entities[i];
                var definition = plan.Entities[i];
                var j = definition.CounterpartIndex;

                entity.BridgesToTagged = present[i] && taggedNearbyCount - (taggedNearby[i] ? 1 : 0) > 0;

                if (j < 0 || j >= count || snapshot == null)
                {
                    continue;
                }

                var connected = present[i] && present[j] &&
                                snapshot.Connects(definition.NodeKey, plan.Entities[j].NodeKey,
                                    definition.GraphRegistryGuid);

                if (connected && !entity.Hub && !entities[j].Hub)
                {
                    entity.RelaxedDistanceToConfirmed = Math.Min(entity.DistanceToConfirmed,
                        Math.Min(entities[j].DistanceToConfirmed + 1, beyond));
                }

                if (!connected || !known[i] || !known[j])
                {
                    continue;
                }

                entity.BridgesCommunities = nodes[i].CommunityId != GraphRecallNode.Unknown &&
                                            nodes[j].CommunityId != GraphRecallNode.Unknown &&
                                            nodes[i].CommunityId != nodes[j].CommunityId;

                if (!snapshot.TryGetEdge(nodes[i].Id, nodes[j].Id, definition.GraphRegistryGuid, out var edge))
                {
                    continue;
                }

                entity.Edge = new GraphEdgeFacts
                {
                    Exists = true,
                    Count = edge.Count,
                    AmountSum = edge.AmountSum,
                    FirstSeenDays = Math.Max(0, (referenceDate - edge.FirstSeen).TotalDays),
                    LastSeenDays = Math.Max(0, (referenceDate - edge.LastSeen).TotalDays),
                    Kind = Enum.IsDefined(typeof(GraphEdgeKind), (int)edge.KindId)
                        ? ((GraphEdgeKind)edge.KindId).ToString()
                        : edge.KindId.ToString()
                };
            }

            for (var i = 0; i < count; i++)
            {
                target[plan.Entities[i].Name] = entities[i];
            }
        }

        private static void Fill(GraphEntity entity, in GraphRecallNode node, int beyond)
        {
            entity.Known = true;
            entity.Hub = node.Hub;
            entity.Degree = node.Degree;
            entity.DegreeMoney = node.DegreeMoney;
            entity.DegreeIdentity = node.DegreeIdentity;
            entity.HubNeighbourCount = node.HubNeighbourCount;
            entity.DistanceToConfirmed = Distance(node.DistanceToConfirmed, beyond);
            entity.DistanceToNotConfirmed = Distance(node.DistanceToNotConfirmed, beyond);
            entity.RelaxedDistanceToConfirmed = entity.DistanceToConfirmed;
            entity.ConfirmedWithin1 = node.ConfirmedWithin1;
            entity.ConfirmedWithin2 = node.ConfirmedWithin2;
            entity.ConfirmedWithin3 = node.ConfirmedWithin3;
            entity.CommunityId = node.CommunityId == GraphRecallNode.Unknown ? 0 : node.CommunityId;
            entity.CommunitySize = node.CommunitySize;
            entity.CommunityConfirmedRate = node.CommunityConfirmedRate;
            entity.CommunityRateRatio = node.CommunityTenantRateRatio;
            entity.PageRank = node.PageRank;
            entity.HubScore = node.HubScore;
            entity.AuthorityScore = node.AuthorityScore;
            entity.ClusteringCoefficient = node.ClusteringCoefficient;
            entity.PropagationScore = node.PropagationScore;
            entity.CoreNumber = node.CoreNumber;

            if (node.Tags != null)
            {
                foreach (var tag in node.Tags)
                {
                    entity.AddTag(tag.Tag, new GraphTagProximity(beyond)
                    {
                        Distance = Distance(tag.Distance, beyond),
                        Within1 = tag.Within1,
                        Within2 = tag.Within2,
                        Within3 = tag.Within3,
                        Known = true
                    });
                }
            }

            foreach (var kind in PatternKinds)
            {
                if (node.InPattern((int)kind - 1))
                {
                    entity.AddPattern(kind.ToString());
                }
            }
        }

        private static int Distance(short stored, int beyond)
        {
            return stored < 0 ? beyond : Math.Min(stored, beyond);
        }
    }
}

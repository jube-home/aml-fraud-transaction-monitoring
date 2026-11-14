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
using Newtonsoft.Json;

namespace Jube.GraphRecallProtocol
{
    public sealed class GraphEntity
    {
        public const int DefaultBeyond = 5;

        private readonly SortedDictionary<string, GraphTagProximity> tags = new(StringComparer.Ordinal);
        private readonly SortedSet<string> patterns = new(StringComparer.Ordinal);

        public GraphEntity(int beyond = DefaultBeyond)
        {
            Beyond = beyond;
            DistanceToConfirmed = beyond;
            DistanceToNotConfirmed = beyond;
            NearestTagDistance = beyond;
            RelaxedDistanceToConfirmed = beyond;
        }

        [JsonIgnore] public int Beyond { get; }
        [JsonIgnore] public bool ReportTable { get; set; } = true;

        public string Value { get; set; } = string.Empty;
        public bool Known { get; set; }
        public bool Hub { get; set; }
        public int Degree { get; set; }
        public int DegreeMoney { get; set; }
        public int DegreeIdentity { get; set; }
        public int HubNeighbourCount { get; set; }
        public int DistanceToConfirmed { get; set; }
        public int DistanceToNotConfirmed { get; set; }
        public int ConfirmedWithin1 { get; set; }
        public int ConfirmedWithin2 { get; set; }
        public int ConfirmedWithin3 { get; set; }
        public long CommunityId { get; set; }
        public int CommunitySize { get; set; }
        public double CommunityConfirmedRate { get; set; }
        public double CommunityRateRatio { get; set; }
        public double PageRank { get; set; }
        public double HubScore { get; set; }
        public double AuthorityScore { get; set; }
        public double ClusteringCoefficient { get; set; }
        public double PropagationScore { get; set; }
        public int CoreNumber { get; set; }
        public string NearestTag { get; set; } = string.Empty;
        public int NearestTagDistance { get; set; }
        public GraphEdgeFacts Edge { get; set; } = new();
        public bool BridgesCommunities { get; set; }
        public bool BridgesToTagged { get; set; }
        public int RelaxedDistanceToConfirmed { get; set; }
        public IReadOnlyDictionary<string, GraphTagProximity> Tags => tags;
        public IReadOnlyCollection<string> Patterns => patterns;

        public void AddTag(string name, GraphTagProximity proximity)
        {
            if (string.IsNullOrEmpty(name) || proximity == null)
            {
                return;
            }

            tags[name] = proximity;

            if (proximity.Distance >= NearestTagDistance)
            {
                return;
            }

            NearestTag = name;
            NearestTagDistance = proximity.Distance;
        }

        public void AddPattern(string kind)
        {
            if (!string.IsNullOrEmpty(kind))
            {
                patterns.Add(kind);
            }
        }

        public GraphTagProximity Tag(string name)
        {
            return name != null && tags.TryGetValue(name, out var proximity)
                ? proximity
                : GraphTagProximity.Unknown(Beyond);
        }

        public bool Pattern(string kind)
        {
            return kind != null && patterns.Contains(kind);
        }
    }
}

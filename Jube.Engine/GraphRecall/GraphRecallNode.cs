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

namespace Jube.Engine.GraphRecall
{
    public readonly record struct GraphRecallNode
    {
        public const short Unknown = -1;

        public long Id { get; init; }
        public bool Hub { get; init; }
        public int Degree { get; init; }
        public int DegreeMoney { get; init; }
        public int DegreeIdentity { get; init; }
        public int HubNeighbourCount { get; init; }
        public int CommunitySize { get; init; }
        public long CommunityId { get; init; } = Unknown;
        public short CoreNumber { get; init; }
        public float PageRank { get; init; }
        public float HubScore { get; init; }
        public float AuthorityScore { get; init; }
        public float ClusteringCoefficient { get; init; }
        public float PropagationScore { get; init; }
        public float CommunityConfirmedRate { get; init; }
        public float CommunityTenantRateRatio { get; init; }
        public short DistanceToConfirmed { get; init; } = Unknown;
        public short DistanceToNotConfirmed { get; init; } = Unknown;
        public int ConfirmedWithin1 { get; init; }
        public int ConfirmedWithin2 { get; init; }
        public int ConfirmedWithin3 { get; init; }
        public int PatternKinds { get; init; }
        public GraphRecallTag[] Tags { get; init; }

        public GraphRecallNode()
        {
        }

        public bool InPattern(int kindOrdinal)
        {
            return (PatternKinds & (1 << kindOrdinal)) != 0;
        }

        public bool TryGetTag(string tag, out GraphRecallTag found)
        {
            if (Tags != null)
            {
                foreach (var candidate in Tags)
                {
                    if (!string.Equals(candidate.Tag, tag, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    found = candidate;

                    return true;
                }
            }

            found = default;

            return false;
        }
    }
}

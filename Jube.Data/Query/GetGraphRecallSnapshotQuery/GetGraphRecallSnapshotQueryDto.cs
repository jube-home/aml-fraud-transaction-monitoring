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
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Data.Query.GetGraphRecallSnapshotQuery
{
    public class GetGraphRecallSnapshotQueryDto
    {
        public long Id { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        public short Hub { get; set; }

        public int Degree { get; set; }

        public int DegreeMoney { get; set; }

        public int DegreeIdentity { get; set; }

        public int HubNeighbourCount { get; set; }

        public long? CommunityId { get; set; }

        public int? CoreNumber { get; set; }

        public double? PageRank { get; set; }

        public double? HubScore { get; set; }

        public double? AuthorityScore { get; set; }

        public double? ClusteringCoefficient { get; set; }

        public short? DistanceToConfirmed { get; set; }

        public int? ConfirmedWithin1 { get; set; }

        public int? ConfirmedWithin2 { get; set; }

        public int? ConfirmedWithin3 { get; set; }

        public short? DistanceToNotConfirmed { get; set; }

        public double? PropagationScore { get; set; }

        public int? CommunitySize { get; set; }

        public double? CommunityConfirmedRate { get; set; }

        public double? CommunityTenantRateRatio { get; set; }

        public int PatternKinds { get; set; }

        public string Tags { get; set; }
    }
}

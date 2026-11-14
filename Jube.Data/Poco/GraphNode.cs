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
using LinqToDB;
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class GraphNode
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public long Id { get; set; }

        [Column] [Key(1)] public int TenantRegistryId { get; set; }
        [Column] [Key(2)] public string Key { get; set; }
        [Column] [Key(3)] public string Value { get; set; }
        [Column] [Key(4)] public byte Hub { get; set; }
        [Column] [Key(5)] public byte HubOverrideId { get; set; }
        [Column] [Nullable] [Key(6)] public Guid[] HubByEdgeGuids { get; set; }
        [Column] [Key(7)] public int Degree { get; set; }
        [Column] [Key(8)] public int DegreeMoney { get; set; }
        [Column] [Key(9)] public int DegreeIdentity { get; set; }
        [Column] [Key(10)] public int HubNeighbourCount { get; set; }
        [Column] [Nullable] [Key(11)] public long? ResolvedEntityId { get; set; }
        [Column] [Nullable] [Key(12)] public long? ComponentId { get; set; }
        [Column] [Nullable] [Key(13)] public int? ComponentSize { get; set; }
        [Column] [Nullable] [Key(14)] public long? CommunityId { get; set; }
        [Column] [Nullable] [Key(15)] public int? CoreNumber { get; set; }
        [Column] [Nullable] [Key(16)] public double? PageRank { get; set; }
        [Column] [Nullable] [Key(17)] public double? HubScore { get; set; }
        [Column] [Nullable] [Key(18)] public double? AuthorityScore { get; set; }
        [Column] [Nullable] [Key(19)] public double? ClusteringCoefficient { get; set; }
        [Column] [Nullable] [Key(20)] public short? DistanceToConfirmed { get; set; }
        [Column] [Nullable] [Key(21)] public int? ConfirmedWithin1 { get; set; }
        [Column] [Nullable] [Key(22)] public int? ConfirmedWithin2 { get; set; }
        [Column] [Nullable] [Key(23)] public int? ConfirmedWithin3 { get; set; }
        [Column] [Nullable] [Key(24)] public short? DistanceToNotConfirmed { get; set; }
        [Column] [Nullable] [Key(25)] public double? PropagationScore { get; set; }
        [Column] [Nullable] [DataType(DataType.BinaryJson)] [Key(26)] public string Statistics { get; set; }
        [Column] [Key(27)] public DateTime FirstSeen { get; set; }
        [Column] [Key(28)] public DateTime LastSeen { get; set; }
        [Column] [Nullable] [Key(29)] public DateTime? ComputedDate { get; set; }
    }
}

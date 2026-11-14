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
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class GraphEdge
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public long Id { get; set; }

        [Column] [Key(1)] public int TenantRegistryId { get; set; }
        [Column] [Key(2)] public Guid GraphRegistryEdgeGuid { get; set; }
        [Column] [Key(3)] public byte KindId { get; set; }
        [Column] [Key(4)] public long FromNodeId { get; set; }
        [Column] [Key(5)] public long ToNodeId { get; set; }
        [Column] [Key(6)] public int Count { get; set; }
        [Column] [Key(7)] public double AmountSum { get; set; }
        [Column] [Key(8)] public DateTime FirstSeen { get; set; }
        [Column] [Key(9)] public DateTime LastSeen { get; set; }
        [Column] [Key(10)] public double DecayedCount { get; set; }
        [Column] [Key(11)] public double DecayedAmount { get; set; }
        [Column] [Key(12)] public DateTime DecayedAt { get; set; }
        [Column] [Key(13)] public float Specificity { get; set; }
        [Column] [Key(14)] public float Confidence { get; set; }
        [Column] [Key(15)] public float Weight { get; set; }
        [Column] [Key(16)] public byte FromHub { get; set; }
        [Column] [Key(17)] public byte ToHub { get; set; }
        [Column] [Key(18)] public byte Restricted { get; set; }
    }
}

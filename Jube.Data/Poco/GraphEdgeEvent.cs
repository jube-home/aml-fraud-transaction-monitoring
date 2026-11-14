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
    public class GraphEdgeEvent
    {
        [Column]
        [PrimaryKey(2)]
        [Identity]
        [Key(0)]
        public long Id { get; set; }

        [Column] [Key(1)] public int TenantRegistryId { get; set; }
        [Column] [Key(2)] public long GraphEdgeId { get; set; }
        [Column] [Key(3)] public long ArchiveId { get; set; }
        [Column] [Key(4)] public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }
        [Column] [Nullable] [Key(5)] public int? ArchiveKeyVersion { get; set; }
        [Column] [Nullable] [Key(6)] public double? Amount { get; set; }
        [Column] [Key(7)] public byte Retracted { get; set; }
        [Column] [PrimaryKey(1)] [Key(8)] public DateTime OccurredDate { get; set; }
    }
}

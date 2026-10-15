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
    public class ArchiverStagePerformanceCounter
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid EntityAnalysisModelGuid { get; set; }
        [Column] [Nullable] [Key(2)] public int? StageId { get; set; }
        [Column] [Nullable] [Key(3)] public long? TotalMicroseconds { get; set; }
        [Column] [Nullable] [Key(4)] public long? MinMicroseconds { get; set; }
        [Column] [Nullable] [Key(5)] public long? MaxMicroseconds { get; set; }
        [Column] [Nullable] [Key(6)] public int? InvokeCount { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string Instance { get; set; }

        [Association(ThisKey = "EntityAnalysisModelGuid", OtherKey = "Guid", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(9)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }
    }
}
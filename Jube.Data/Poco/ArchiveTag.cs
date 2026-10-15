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
    public class ArchiveTag
    {
        [Column]
        [Identity]
        [Key(0)]
        [PrimaryKey]
        public long Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Key(3)] public int? Version { get; set; }
        [Column] [Key(4)] public string CreatedUser { get; set; }
        [Column] [Key(5)] public DateTime? CreatedDate { get; set; }
        [Column] [Key(6)] public byte? Deleted { get; set; }
        [Column] [Key(7)] public string DeletedUser { get; set; }
        [Column] [Key(8)] public DateTime? DeletedDate { get; set; }
    }
}
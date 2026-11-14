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
    public class GraphNodeTag
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public long Id { get; set; }

        [Column] [Key(1)] public int TenantRegistryId { get; set; }
        [Column] [Key(2)] public long GraphNodeId { get; set; }
        [Column] [Key(3)] public string Tag { get; set; }
        [Column] [Key(4)] public short Distance { get; set; }
        [Column] [Key(5)] public int Within1 { get; set; }
        [Column] [Key(6)] public int Within2 { get; set; }
        [Column] [Key(7)] public int Within3 { get; set; }
        [Column] [Key(8)] public DateTime ComputedDate { get; set; }
    }
}

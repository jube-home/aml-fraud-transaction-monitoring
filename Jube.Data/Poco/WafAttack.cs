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
    public class WafAttack
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public long Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(2)] public string Transport { get; set; }
        [Column] [Nullable] [Key(3)] public string Route { get; set; }
        [Column] [Nullable] [Key(4)] public string Method { get; set; }
        [Column] [Nullable] [Key(5)] public string RemoteIp { get; set; }
        [Column] [Nullable] [Key(6)] public string UserName { get; set; }
        [Column] [Nullable] [Key(7)] public int? WafSignatureId { get; set; }
        [Column] [Nullable] [Key(8)] public string SignatureName { get; set; }
        [Column] [Nullable] [Key(9)] public string Category { get; set; }
        [Column] [Nullable] [Key(10)] public string MatchedField { get; set; }
        [Column] [Nullable] [Key(11)] public string MatchedValue { get; set; }
        [Column] [Nullable] [Key(12)] public string Action { get; set; }
        [Column] [Nullable] [Key(13)] public string CorrelationId { get; set; }
        [Column] [Nullable] [Key(14)] public string Instance { get; set; }
    }
}
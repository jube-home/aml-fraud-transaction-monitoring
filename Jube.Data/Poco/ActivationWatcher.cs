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
    public class ActivationWatcher
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Key(1)] public int? TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(2)] public string Key { get; set; }
        [Column] [Nullable] [Key(3)] public string KeyValue { get; set; }
        [Column] [Nullable] [Key(4)] public double? Longitude { get; set; }
        [Column] [Nullable] [Key(5)] public double? Latitude { get; set; }
        [Column] [Nullable] [Key(6)] public string ActivationRuleSummary { get; set; }
        [Column] [Nullable] [Key(7)] public string ResponseElevationContent { get; set; }
        [Column] [Nullable] [Key(8)] public double? ResponseElevation { get; set; }
        [Column] [Nullable] [Key(9)] public string BackColor { get; set; }
        [Column] [Nullable] [Key(10)] public string ForeColor { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(12)] public double? PreviousLatitude { get; set; }
        [Column] [Nullable] [Key(13)] public double? PreviousLongitude { get; set; }
        [Column] [Nullable] [Key(14)] public DateTime? PreviousCreatedDate { get; set; }
        [Column] [Nullable] [Key(15)] public string ResponseElevationName { get; set; }
    }
}
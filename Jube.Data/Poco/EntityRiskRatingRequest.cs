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
    public class EntityRiskRatingRequest
    {
        [Column] [PrimaryKey] [Identity] [Key(0)] public long Id { get; set; }
        [Column] [Key(1)] public Guid Guid { get; set; }
        [Column] [Key(2)] public int TenantRegistryId { get; set; }
        [Column] [Key(3)] public string KeyName { get; set; }
        [Column] [Key(4)] public string KeyValue { get; set; }
        [Column] [Key(5)] public short TierId { get; set; }
        [Column] [Nullable] [Key(6)] public double? Score { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? ReviewByDate { get; set; }
        [Column] [Key(8)] public string Reason { get; set; }
        [Column] [Key(9)] public int BasedOnVersion { get; set; }
        [Column] [Key(10)] public short StateId { get; set; }
        [Column] [Nullable] [Key(11)] public int? ResultingVersion { get; set; }
        [Column] [Key(12)] public string RequestedUser { get; set; }
        [Column] [Key(13)] public DateTime RequestedDate { get; set; }
        [Column] [Nullable] [Key(14)] public string DecidedUser { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? DecidedDate { get; set; }
        [Column] [Nullable] [Key(16)] public string DecisionNote { get; set; }
    }
}

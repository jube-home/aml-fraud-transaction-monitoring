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

using Jube.Data.Repository.Models;
using System;
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class AdvancedAnalyticsTrainedModel
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Key(1)] public Guid Guid { get; set; }
        [Column] [Key(2)] public int TenantRegistryId { get; set; }
        [Column] [Key(3)] public int AdvancedAnalyticsInstanceId { get; set; }
        [Column] [Key(4)] public string Name { get; set; }
        [Column] [Key(5)] public AdvancedAnalyticsKind KindId { get; set; }
        [Column] [Key(6)] public byte Active { get; set; }
        [Column] [Nullable] [Key(7)] public int? ActiveVersionId { get; set; }
        [Column] [Key(8)] public AdvancedAnalyticsSource SourceId { get; set; }
        [Column] [Nullable] [Key(9)] public string CorrelationKey { get; set; }
        [Column] [Key(10)] public DateTime CreatedDate { get; set; }
        [Column] [Nullable] [Key(11)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? UpdatedDate { get; set; }
        [Column] [Key(13)] public int Version { get; set; }
        [Column] [Key(14)] public byte Deleted { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(16)] public string DeletedUser { get; set; }
    }
}
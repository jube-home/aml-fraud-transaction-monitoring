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
    public class EntityAnalysisModelRequestXpathVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelRequestXpathId { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public int? DataTypeId { get; set; }
        [Column] [Nullable] [Key(5)] public string XPath { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(8)] public byte? SearchKey { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(10)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(11)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(12)] public int? Version { get; set; }
        [Column] [Nullable] [Key(13)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(14)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(15)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(16)] public byte? SearchKeyCache { get; set; }
        [Column] [Nullable] [Key(17)] public string SearchKeyCacheInterval { get; set; }
        [Column] [Nullable] [Key(18)] public int? SearchKeyCacheValue { get; set; }
        [Column] [Nullable] [Key(19)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(20)] public string SearchKeyTtlInterval { get; set; }
        [Column] [Nullable] [Key(21)] public int? SearchKeyTtlIntervalValue { get; set; }
        [Column] [Nullable] [Key(22)] public string SearchKeyCacheTtlInterval { get; set; }
        [Column] [Nullable] [Key(23)] public int? SearchKeyCacheTtlValue { get; set; }
        [Column] [Nullable] [Key(24)] public int? SearchKeyCacheFetchLimit { get; set; }
        [Column] [Nullable] [Key(25)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(26)] public byte? SearchKeyCacheSample { get; set; }
        [Column] [Nullable] [Key(27)] public byte? EnableOverride { get; set; }
        [Column] [Nullable] [Key(28)] public string DefaultValue { get; set; }
        [Column] [Nullable] [Key(29)] public int? SearchKeyFetchLimit { get; set; }
        [Column] [Nullable] [Key(30)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(31)] public byte? Cache { get; set; }
        [Column] [Nullable] [Key(33)] public int? CacheIndexId { get; set; }
        [Column] [Nullable] [Key(34)] public byte? EncryptionId { get; set; }
    }
}
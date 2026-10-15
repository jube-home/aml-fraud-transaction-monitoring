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
using LinqToDB;
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelAbstractionRuleVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelAbstractionRuleId { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public string BuilderRuleScript { get; set; }
        [Column] [Nullable] [Key(4)] public string Name { get; set; }
        [Column] [Nullable] [Key(5)] public string SearchKey { get; set; }
        [Column] [Nullable] [Key(6)] public int? SearchFunctionTypeId { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string SearchInterval { get; set; }
        [Column] [Nullable] [Key(9)] public string SearchFunctionKey { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(10)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(11)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(12)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(13)] public int SearchValue { get; set; }
        [Column] [Nullable] [Key(14)] public byte? Search { get; set; }
        [Column] [Nullable] [Key(15)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(16)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(17)] public int? Version { get; set; }
        [Column] [Nullable] [Key(18)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(19)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(20)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(21)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(22)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(23)] public byte? Offset { get; set; }
        [Column] [Nullable] [Key(24)] public byte? OffsetTypeId { get; set; }
        [Column] [Nullable] [Key(25)] public int? OffsetValue { get; set; }
        [Column] [Nullable] [Key(26)] public string CoderRuleScript { get; set; }
        [Column] [Nullable] [Key(27)] public byte? RuleScriptTypeId { get; set; }
        [Column] [Nullable] [Key(28)] public Guid Guid { get; set; }
    }
}
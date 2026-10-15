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
    public class MockArchive
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(1)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(2)] public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }
        [Column] [Nullable] [Key(3)] public string EntryKeyValue { get; set; }
        [Column] [Nullable] [Key(4)] public double? ResponseElevation { get; set; }
        [Column] [Nullable] [Key(5)] public int? EntityAnalysisModelActivationRuleId { get; set; }
        [Column] [Nullable] [Key(6)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(7)] public int? ActivationRuleCount { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? ReferenceDate { get; set; }
    }
}
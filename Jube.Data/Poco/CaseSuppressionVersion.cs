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
    public class CaseSuppressionVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Key(1)] public int CaseSuppressionId { get; set; }
        [Column] [Key(2)] public int CaseId { get; set; }
        [Column] [Nullable] [Key(3)] public string CaseKey { get; set; }
        [Column] [Nullable] [Key(4)] public string CaseKeyValue { get; set; }
        [Column] [Key(5)] public string OverrideKey { get; set; }
        [Column] [Key(6)] public string OverrideKeyValue { get; set; }
        [Column] [Key(7)] public int PathId { get; set; } = 1;
        [Column] [Nullable] [Key(8)] public string Reason { get; set; }
        [Column] [Key(9)] public DateTime ExpiryDate { get; set; }
        [Column] [Key(10)] public int StateId { get; set; } = 1;
        [Column] [Key(11)] public int SourceId { get; set; } = 1;
        [Column] [Key(12)] public int Version { get; set; } = 1;
        [Column] [Key(13)] public int CountInCase { get; set; }
        [Column] [Nullable] [Key(14)] public DateTime? FirstSeenDate { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? LastSeenDate { get; set; }
        [Column] [Key(16)] public int WiderCountStateId { get; set; } = 2;
        [Column] [Nullable] [Key(17)] public int? WiderTransactions { get; set; }
        [Column] [Nullable] [Key(18)] public int? WiderKeys { get; set; }
        [Column] [Key(19)] public byte WiderCapped { get; set; }
        [Column] [Nullable] [Key(20)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(21)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(22)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(23)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(24)] public DateTime? DecidedDate { get; set; }
        [Column] [Nullable] [Key(25)] public string DecidedUser { get; set; }
    }
}

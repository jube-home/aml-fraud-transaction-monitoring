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
    public class CaseKeyJournalTagJob
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public long Id { get; set; }

        [Column] [Key(1)] public Guid Guid { get; set; }
        [Column] [Key(2)] public Guid CaseWorkflowGuid { get; set; }
        [Column] [Key(3)] public string RequestedUser { get; set; }
        [Column] [Key(4)] public byte KindId { get; set; }
        [Column] [Key(5)] public byte StateId { get; set; }
        [Column] [Key(6)] public string CaseKey { get; set; }
        [Column] [Key(7)] public string KeyValue { get; set; }
        [Column] [Key(8)] public DateTime FromDate { get; set; }
        [Column] [Key(9)] public DateTime ToDate { get; set; }
        [Column] [Key(10)] public byte ActivationsOnly { get; set; }
        [Column] [Key(11)] public double ResponseElevation { get; set; }
        [Column] [Nullable] [Key(12)] public string MergeJson { get; set; }
        [Column] [Nullable] [Key(13)] public string AddJson { get; set; }
        [Column] [Nullable] [Key(14)] public string RemoveJson { get; set; }
        [Column] [Nullable] [Key(15)] public Guid? UndoOfGuid { get; set; }
        [Column] [Key(16)] public int Total { get; set; }
        [Column] [Key(17)] public int Processed { get; set; }
        [Column] [Key(18)] public int Changed { get; set; }
        [Column] [Key(19)] public int Skipped { get; set; }
        [Column] [Key(20)] public int Locked { get; set; }
        [Column] [Key(21)] public byte CancelRequested { get; set; }
        [Column] [Nullable] [Key(22)] public string Error { get; set; }
        [Column] [Key(23)] public DateTime CreatedDate { get; set; }
        [Column] [Nullable] [Key(24)] public DateTime? StartedDate { get; set; }
        [Column] [Nullable] [Key(25)] public DateTime? HeartbeatDate { get; set; }
        [Column] [Nullable] [Key(26)] public DateTime? FinishedDate { get; set; }
        [Column] [Nullable] [Key(27)] public string FilterJson { get; set; }
    }
}

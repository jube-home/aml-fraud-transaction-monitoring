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
    public class CaseKeyJournalPeer
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public long Id { get; set; }

        [Column] [Key(1)] public Guid CaseWorkflowGuid { get; set; }
        [Column] [Key(2)] public string CaseKey { get; set; }
        [Column] [Key(3)] public string Field { get; set; }
        [Column] [Nullable] [Key(4)] public string Segment { get; set; }
        [Column] [Key(5)] public int KeyCount { get; set; }
        [Column] [Key(6)] public int RowCount { get; set; }
        [Column] [Key(7)] public double Median { get; set; }
        [Column] [Key(8)] public double Scale { get; set; }
        [Column] [Key(9)] public double Q1 { get; set; }
        [Column] [Key(10)] public double Q3 { get; set; }
        [Column] [Key(11)] public double Minimum { get; set; }
        [Column] [Key(12)] public double Maximum { get; set; }
        [Column] [Key(13)] public double Mean { get; set; }
        [Column] [DataType(DataType.BinaryJson)] [Key(14)] public string Bins { get; set; }
        [Column] [Key(15)] public int WindowDays { get; set; }
        [Column] [Key(16)] public DateTime ComputedDate { get; set; }
        [Column] [Key(17)] public byte Deleted { get; set; }
        [Column] [Nullable] [Key(18)] public DateTime? DeletedDate { get; set; }
    }
}

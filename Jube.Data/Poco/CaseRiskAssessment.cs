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
    public class CaseRiskAssessment
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public long Id { get; set; }

        [Column] [Key(1)] public int CaseId { get; set; }
        [Column] [Key(2)] public int Sequence { get; set; }
        [Column] [Key(3)] public DateTime CreatedDate { get; set; }
        [Column] [Key(4)] public byte TriggerId { get; set; }
        [Column] [Key(5)] public byte MethodId { get; set; }
        [Column] [Nullable] [Key(6)] public int? MethodVersion { get; set; }
        [Column] [Nullable] [Key(7)] public double? BaseRate { get; set; }
        [Column] [Key(8)] public double LikelihoodProbability { get; set; }
        [Column] [Key(9)] public byte LikelihoodBandId { get; set; }
        [Column] [Key(10)] public int EvidenceCount { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(11)]
        public string ContributionsJson { get; set; }

        [Column] [Key(12)] public byte ImpactId { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(13)]
        public string ImpactComponentsJson { get; set; }

        [Column] [Key(14)] public byte LaneId { get; set; }
        [Column] [Nullable] [Key(15)] public string LaneReason { get; set; }
        [Column] [Nullable] [Key(16)] public DateTime? NextDeadlineDate { get; set; }
        [Column] [Key(17)] public byte DeadlineBreached { get; set; }
        [Column] [Key(18)] public double ExpectedHarm { get; set; }
        [Column] [Key(19)] public int Priority { get; set; }
    }
}

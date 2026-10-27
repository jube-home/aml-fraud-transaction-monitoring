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
    public class Case
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? DiaryDate { get; set; }
        [Column] [Nullable] [Key(3)] public Guid CaseWorkflowGuid { get; set; }
        [Column] [Nullable] [Key(4)] public Guid CaseWorkflowStatusGuid { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(7)] public string LockedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? LockedDate { get; set; }
        [Column] [Nullable] [Key(9)] public byte? ClosedStatusId { get; set; }
        [Column] [Nullable] [Key(10)] public DateTime? ClosedDate { get; set; }
        [Column] [Nullable] [Key(11)] public string ClosedUser { get; set; }
        [Column] [Nullable] [Key(12)] public string CaseKey { get; set; }
        [Column] [Nullable] [Key(13)] public byte? Diary { get; set; }
        [Column] [Nullable] [Key(14)] public string DiaryUser { get; set; }
        [Column] [Nullable] [Key(15)] public byte? Rating { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(16)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(17)] public string CaseKeyValue { get; set; }
        [Column] [Nullable] [Key(18)] public byte? LastClosedStatus { get; set; }
        [Column] [Nullable] [Key(19)] public DateTime? ClosedStatusMigrationDate { get; set; }

        [Association(ThisKey = "CaseWorkflowGuid", OtherKey = "Guid", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(20)]
        public CaseWorkflow CaseWorkflow { get; set; }

        [Association(ThisKey = "CaseWorkflowStatusGuid", OtherKey = "Guid", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(21)]
        public CaseWorkflowStatus CaseWorkflowStatus { get; set; }

        [Column] [Nullable] [Key(22)] public Guid? EntityAnalysisModelActivationRuleGuid { get; set; }
        [Column] [Nullable] [Key(23)] public DateTime? RiskCheckedDate { get; set; }
        [Column] [Key(24)] public byte DiaryTypeId { get; set; } = 1;
        [Column] [Nullable] [Key(25)] public byte? RiskRecheckTriggerId { get; set; }
        [Column] [Key(26)] public byte QaSampleStatusId { get; set; }
        [Column] [Nullable] [Key(27)] public DateTime? QaSampleDate { get; set; }
    }
}
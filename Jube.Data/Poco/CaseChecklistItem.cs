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
    public class CaseChecklistItem
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public long Id { get; set; }

        [Column] [Key(1)] public int CaseId { get; set; }
        [Column] [Key(2)] public Guid CaseWorkflowChecklistItemGuid { get; set; }
        [Column] [Key(3)] public Guid CaseWorkflowChecklistGuid { get; set; }
        [Column] [Key(4)] public Guid CaseWorkflowStatusGuid { get; set; }
        [Column] [Nullable] [Key(5)] public string ChecklistName { get; set; }
        [Column] [Nullable] [Key(6)] public string ItemName { get; set; }
        [Column] [Key(7)] public int Priority { get; set; }
        [Column] [Key(8)] public byte StateId { get; set; }
        [Column] [Key(9)] public DateTime FirstShownDate { get; set; }
        [Column] [Key(10)] public DateTime ShownDate { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? DueDate { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? CompletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public string CompletedUser { get; set; }
        [Column] [Nullable] [Key(14)] public DateTime? LastEvaluatedDate { get; set; }
        [Column] [Key(15)] public DateTime CreatedDate { get; set; }
        [Column] [Nullable] [Key(16)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(17)] public DateTime? SlaBreachedDate { get; set; }
    }
}

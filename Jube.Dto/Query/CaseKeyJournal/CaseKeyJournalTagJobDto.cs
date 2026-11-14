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

using System.ComponentModel;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseKeyJournal
{
    [Description("A background bulk tagging: what it was asked to do and how far it has got.")]
    [MessagePackObject]
    public class CaseKeyJournalTagJobDto
    {
        [Key(0)]
        public Guid Guid { get; set; }
        [Key(1)]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("Tag, or Undo for a job that restores what an earlier job changed.")]
        [Key(2)]
        public string Kind { get; set; } = "Tag";

        [Description("Queued, Running, Done, Failed or Cancelled.")]
        [Key(3)]
        public string State { get; set; } = "Queued";

        [Key(4)]
        public string Key { get; set; } = string.Empty;
        [Key(5)]
        public string Value { get; set; } = string.Empty;
        [Key(6)]
        public string[] Add { get; set; } = [];
        [Key(7)]
        public string[] Remove { get; set; } = [];

        [Description("For an undo, the job it reverses.")]
        [Key(8)]
        public Guid? UndoOf { get; set; }

        [Description("How many transactions matched when the job started.")]
        [Key(9)]
        public int Total { get; set; }

        [Key(10)]
        public int Processed { get; set; }

        [Description("How many had their Tags changed.")]
        [Key(11)]
        public int Changed { get; set; }

        [Description("How many were left alone because they are no longer in the workflow's model.")]
        [Key(12)]
        public int Skipped { get; set; }

        [Description("How many were left alone because their case is locked to another user.")]
        [Key(13)]
        public int Locked { get; set; }

        [Description("Why a failed job failed.")]
        [Key(14)]
        public string? Error { get; set; }

        [Description("True once the job is Done, changed something and has not been undone, so it can be undone.")]
        [Key(15)]
        public bool CanUndo { get; set; }

        [Key(16)]
        public DateTimeOffset CreatedDate { get; set; }
        [Key(17)]
        public DateTimeOffset? StartedDate { get; set; }
        [Key(18)]
        public DateTimeOffset? FinishedDate { get; set; }
    }
}

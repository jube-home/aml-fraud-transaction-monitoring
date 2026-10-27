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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseKeyJournal
{
    [Description("A background bulk tagging: what it was asked to do and how far it has got.")]
    public class CaseKeyJournalTagJobDto
    {
        public Guid Guid { get; set; }
        public Guid CaseWorkflowGuid { get; set; }

        [Description("Tag, or Undo for a job that restores what an earlier job changed.")]
        public string Kind { get; set; } = "Tag";

        [Description("Queued, Running, Done, Failed or Cancelled.")]
        public string State { get; set; } = "Queued";

        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string[] Add { get; set; } = [];
        public string[] Remove { get; set; } = [];

        [Description("For an undo, the job it reverses.")]
        public Guid? UndoOf { get; set; }

        [Description("How many transactions matched when the job started.")]
        public int Total { get; set; }

        public int Processed { get; set; }

        [Description("How many had their Tags changed.")]
        public int Changed { get; set; }

        [Description("How many were left alone because they are no longer in the workflow's model.")]
        public int Skipped { get; set; }

        [Description("How many were left alone because their case is locked to another user.")]
        public int Locked { get; set; }

        [Description("Why a failed job failed.")]
        public string? Error { get; set; }

        [Description("True once the job is Done, changed something and has not been undone, so it can be undone.")]
        public bool CanUndo { get; set; }

        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset? StartedDate { get; set; }
        public DateTimeOffset? FinishedDate { get; set; }
    }
}

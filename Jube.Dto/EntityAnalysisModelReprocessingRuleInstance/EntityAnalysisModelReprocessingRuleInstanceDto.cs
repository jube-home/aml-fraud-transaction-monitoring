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
using Jube.Dto.Forms;
using Jube.Dto.Interfaces;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Dto.EntityAnalysisModelReprocessingRuleInstance
{
    [FormEndpoint("EntityAnalysisModelReprocessingRuleInstance")]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelReprocessingRuleId))]
    [FormGroup("Status", Order = 10)]
    [FormGroup("Counts", Order = 20)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class EntityAnalysisModelReprocessingRuleInstanceDto : IUpdated, ITreeChild
    {
        [Description("Identifier of the Reprocessing Rule this instance belongs to. Set from the parent " +
                     "Reprocessing Rule context; not user-editable.")]
        public int EntityAnalysisModelReprocessingRuleId { get; set; }

        [Description("Lifecycle status of this reprocessing run: 0 = Not Allocated, 1 = Allocated, 2 = Initial " +
                     "Count, 3 = Processing, 4 = Completed. Server-assigned by the reprocessing engine.")]
        [FormField(Group = "Status", Order = 10, ReadOnly = true)]
        [ListColumn(Order = 10, Title = "Status")]
        public int StatusId { get; set; }

        [Description("Timestamp (UTC) the reprocessing engine started working this instance. Null until " +
                     "allocated. Server-assigned.")]
        [FormField(Group = "Status", Order = 30, ReadOnly = true, Widget = "date")]
        [ListColumn(Order = 30, Title = "Started Date")]
        public DateTimeOffset? StartedDate { get; set; }

        [Description("Count of Archive records available for reprocessing as of ReferenceDate. Server-assigned.")]
        [FormField(Group = "Counts", Order = 10, ReadOnly = true)]
        [ListColumn(Order = 60, Title = "Available Count")]
        public int AvailableCount { get; set; }

        [Description("Count of Archive records randomly sampled in, per the Reprocessing Rule's " +
                     "ReprocessingSample percentage. Server-assigned.")]
        [FormField(Group = "Counts", Order = 20, ReadOnly = true)]
        [ListColumn(Order = 70, Title = "Sampled Count")]
        public int SampledCount { get; set; }

        [Description("Count of sampled Archive records that matched the Reprocessing Rule's script. " +
                     "Server-assigned.")]
        [FormField(Group = "Counts", Order = 30, ReadOnly = true)]
        [ListColumn(Order = 80, Title = "Matched Count")]
        public int MatchedCount { get; set; }

        [Description("Count of matched Archive records that have been replayed through invoke so far. " +
                     "Server-assigned.")]
        [FormField(Group = "Counts", Order = 40, ReadOnly = true)]
        [ListColumn(Order = 90, Title = "Processed Count")]
        public int ProcessedCount { get; set; }

        [Description("Timestamp (UTC) the reprocessing engine finished working this instance. Null until " +
                     "completed. Server-assigned.")]
        [FormField(Group = "Status", Order = 40, ReadOnly = true, Widget = "date")]
        [ListColumn(Order = 100, Title = "Completed Date")]
        public DateTimeOffset? CompletedDate { get; set; }

        [Description("Count of Archive records that errored while being replayed through invoke. " +
                     "Server-assigned.")]
        [FormField(Group = "Counts", Order = 50, ReadOnly = true)]
        [ListColumn(Order = 110, Title = "Error Count")]
        public int ErrorCount { get; set; }

        [Description("The reference date the reprocessing window (ReprocessingValue/ReprocessingInterval on the " +
                     "parent Reprocessing Rule) is measured back from. Server-assigned.")]
        [FormField(Group = "Status", Order = 20, ReadOnly = true, Widget = "date")]
        [ListColumn(Order = 40, Title = "Reference Date")]
        public DateTimeOffset? ReferenceDate { get; set; }

        [Description("Server-assigned row identifier. A successful Update assigns a new Id -- this instance is " +
                     "superseded rather than updated in place. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("User who created this row. Server-assigned. Read-only. Reset to the calling user on every " +
                     "Update, since Update supersedes the row rather than updating it in place -- see the " +
                     "migration report.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this row was created. Server-assigned. Read-only. Reset to the Update time " +
                     "on every Update -- see CreatedUser and the migration report.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        [ListColumn(Order = 20, Title = "Created Date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("No backing column -- always null when read back (pre-existing quirk, see migration " +
                     "report). Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this row was last updated by the reprocessing engine's count/status " +
                     "reporting. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every Update. Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this row, if soft-deleted. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this row was soft-deleted, if applicable. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}
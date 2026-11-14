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

namespace Jube.Dto.Query.CaseById
{
    [Description("A single case as shown on the case screen, with its workflow status colours, the payload " +
                 "fields the caller's roles may see and the visible activations. Read-only.")]
    [MessagePackObject]
    public class CaseByIdDto
    {
        [Description("Server-assigned integer identifier of the case.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Guid of the archived model invocation entry that raised the case.")]
        [Key(1)]
        public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }

        [Description("UTC diary date of the case.")]
        [Key(2)]
        public DateTimeOffset DiaryDate { get; set; }

        [Description("Guid of the case workflow the case belongs to.")]
        [Key(3)]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("Guid of the case workflow status the case is currently in.")]
        [Key(4)]
        public Guid CaseWorkflowStatusGuid { get; set; }

        [Description("UTC date the case was created.")]
        [Key(5)]
        public DateTimeOffset CreatedDate { get; set; }

        [Description("True when the case is locked by a user.")]
        [Key(6)]
        public bool Locked { get; set; }

        [Description("User holding the lock, or empty.")]
        [Key(7)]
        public string? LockedUser { get; set; }

        [Description("UTC date the case was locked.")]
        [Key(8)]
        public DateTimeOffset LockedDate { get; set; }

        [Description("Closed status identifier of the case.")]
        [Key(9)]
        public byte ClosedStatusId { get; set; }

        [Description("UTC date the case was closed.")]
        [Key(10)]
        public DateTimeOffset ClosedDate { get; set; }

        [Description("User that closed the case, or empty.")]
        [Key(11)]
        public string? ClosedUser { get; set; }

        [Description("Name of the payload field that keys the case.")]
        [Key(12)]
        public string? CaseKey { get; set; }

        [Description("True when the case is in a diary.")]
        [Key(13)]
        public bool Diary { get; set; }

        [Description("User whose diary holds the case, or empty.")]
        [Key(14)]
        public string? DiaryUser { get; set; }

        [Description("Rating of the case.")] [Key(15)] public int Rating { get; set; }

        [Description("Value of the case key for this case.")]
        [Key(16)]
        public string? CaseKeyValue { get; set; }

        [Description("Previous closed status identifier of the case.")]
        [Key(17)]
        public int LastClosedStatus { get; set; }

        [Description("UTC date the closed status last migrated.")]
        [Key(18)]
        public DateTimeOffset ClosedStatusMigrationDate { get; set; }

        [Description("Foreground colour of the workflow status.")]
        [Key(19)]
        public string? ForeColor { get; set; }

        [Description("Background colour of the workflow status.")]
        [Key(20)]
        public string? BackColor { get; set; }

        [Description("Raw JSON of the case as archived.")]
        [Key(21)]
        public string? Json { get; set; }

        [Description("Payload fields configured as XPaths for the workflow and visible to the caller's roles.")]
        [Key(22)]
        public List<CaseByIdFieldEntryDto>? FormattedPayload { get; set; }

        [Description("Names of the activations that are visible on the case.")]
        [Key(23)]
        public List<CaseByIdActivationDto>? Activation { get; set; }

        [Description("True when visualisation is enabled for the workflow.")]
        [Key(24)]
        public bool EnableVisualisation { get; set; }

        [Description("Guid of the visualisation registry attached to the workflow.")]
        [Key(25)]
        public Guid VisualisationRegistryGuid { get; set; }

        [Description("True when the network graph is enabled for the workflow.")]
        [Key(26)]
        public bool EnableGraph { get; set; }

        [Description("Guid of the Graph Registry attached to the workflow, when the graph is enabled.")]
        [Key(27)]
        public Guid? GraphRegistryGuid { get; set; }

        [Description("Integer identifier of the entity analysis model the case workflow belongs to.")]
        [Key(28)]
        public int EntityAnalysisModelId { get; set; }
    }
}
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

namespace Jube.Dto.Query.CaseBySessionCaseSearchCompile
{
    [MessagePackObject]
    public class CaseBySessionCaseSearchCompileDto
    {
        [Description("Integer identifier of the case.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Guid of the entity analysis model instance entry the case was created from.")]
        [Key(1)]
        public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }

        [Description("Diary date of the case (UTC).")]
        [Key(2)]
        public DateTimeOffset DiaryDate { get; set; }

        [Description("Guid of the case workflow the case belongs to.")]
        [Key(3)]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("Guid of the case workflow status the case is currently in.")]
        [Key(4)]
        public Guid CaseWorkflowStatusGuid { get; set; }

        [Description("Date the case was created (UTC).")]
        [Key(5)]
        public DateTimeOffset CreatedDate { get; set; }

        [Description("Whether the case is locked. Always true on a successful response, as retrieving the " +
                     "case locks it to the calling user.")]
        [Key(6)]
        public bool Locked { get; set; }

        [Description("User the case is locked to. Always the calling user on a successful response.")]
        [Key(7)]
        public string? LockedUser { get; set; }

        [Description("Date the case was locked (UTC), as held before this retrieval.")]
        [Key(8)]
        public DateTimeOffset LockedDate { get; set; }

        [Description("Closed status identifier: 0 Open, 1 Suspend Open, 2 Suspend Closed, 3 Closed, 4 Suspend Bypass.")]
        [Key(9)]
        public byte ClosedStatusId { get; set; }

        [Description("Date the case was closed (UTC).")]
        [Key(10)]
        public DateTimeOffset ClosedDate { get; set; }

        [Description("User who closed the case.")]
        [Key(11)]
        public string? ClosedUser { get; set; }

        [Description("Name of the payload key the case is raised against.")]
        [Key(12)]
        public string? CaseKey { get; set; }

        [Description("Whether the case is in diary.")]
        [Key(13)]
        public bool Diary { get; set; }

        [Description("User the case is diaried to.")]
        [Key(14)]
        public string? DiaryUser { get; set; }

        [Description("Rating (priority) of the case.")]
        [Key(15)]
        public int Rating { get; set; }

        [Description("Value of the payload key the case is raised against.")]
        [Key(16)]
        public string? CaseKeyValue { get; set; }

        [Description("Previous closed status of the case.")]
        [Key(17)]
        public int LastClosedStatus { get; set; }

        [Description("Date the closed status last migrated (UTC).")]
        [Key(18)]
        public DateTimeOffset ClosedStatusMigrationDate { get; set; }

        [Description("Foreground colour of the case workflow status.")]
        [Key(19)]
        public string? ForeColor { get; set; }

        [Description("Background colour of the case workflow status.")]
        [Key(20)]
        public string? BackColor { get; set; }

        [Description("Raw JSON payload of the case.")]
        [Key(21)]
        public string? Json { get; set; }

        [Description("Payload fields of the case, formatted per the case workflow XPath configuration.")]
        [Key(22)]
        public List<CaseBySessionCaseSearchCompileFieldEntryDto>? FormattedPayload { get; set; }

        [Description("Names of the visible activations on the case.")]
        [Key(23)]
        public List<CaseBySessionCaseSearchCompileActivationDto>? Activation { get; set; }

        [Description("Whether the case workflow has visualisation enabled.")]
        [Key(24)]
        public bool EnableVisualisation { get; set; }

        [Description("Guid of the visualisation registry entry for the case workflow.")]
        [Key(25)]
        public Guid VisualisationRegistryGuid { get; set; }

        [Description("True when the network graph is enabled for the workflow.")]
        [Key(26)]
        public bool EnableGraph { get; set; }

        [Description("Guid of the Graph Registry attached to the workflow, when the graph is enabled.")]
        [Key(27)]
        public Guid? GraphRegistryGuid { get; set; }

        [Description("Integer identifier of the entity analysis model the case belongs to.")]
        [Key(28)]
        public int EntityAnalysisModelId { get; set; }
    }
}
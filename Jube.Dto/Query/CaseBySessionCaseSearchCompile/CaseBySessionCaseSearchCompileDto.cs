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

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Query.CaseBySessionCaseSearchCompile
{
    public class CaseBySessionCaseSearchCompileDto
    {
        [Description("Integer identifier of the case.")]
        public int Id { get; set; }

        [Description("Guid of the entity analysis model instance entry the case was created from.")]
        public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }

        [Description("Diary date of the case (UTC).")]
        public DateTimeOffset DiaryDate { get; set; }

        [Description("Guid of the case workflow the case belongs to.")]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("Guid of the case workflow status the case is currently in.")]
        public Guid CaseWorkflowStatusGuid { get; set; }

        [Description("Date the case was created (UTC).")]
        public DateTimeOffset CreatedDate { get; set; }

        [Description("Whether the case is locked. Always true on a successful response, as retrieving the " +
                     "case locks it to the calling user.")]
        public bool Locked { get; set; }

        [Description("User the case is locked to. Always the calling user on a successful response.")]
        public string? LockedUser { get; set; }

        [Description("Date the case was locked (UTC), as held before this retrieval.")]
        public DateTimeOffset LockedDate { get; set; }

        [Description("Closed status identifier: 0 Open, 1 Suspend Open, 2 Suspend Closed, 3 Closed, 4 Suspend Bypass.")]
        public byte ClosedStatusId { get; set; }

        [Description("Date the case was closed (UTC).")]
        public DateTimeOffset ClosedDate { get; set; }

        [Description("User who closed the case.")]
        public string? ClosedUser { get; set; }

        [Description("Name of the payload key the case is raised against.")]
        public string? CaseKey { get; set; }

        [Description("Whether the case is in diary.")]
        public bool Diary { get; set; }

        [Description("User the case is diaried to.")]
        public string? DiaryUser { get; set; }

        [Description("Rating (priority) of the case.")]
        public int Rating { get; set; }

        [Description("Value of the payload key the case is raised against.")]
        public string? CaseKeyValue { get; set; }

        [Description("Previous closed status of the case.")]
        public int LastClosedStatus { get; set; }

        [Description("Date the closed status last migrated (UTC).")]
        public DateTimeOffset ClosedStatusMigrationDate { get; set; }

        [Description("Foreground colour of the case workflow status.")]
        public string? ForeColor { get; set; }

        [Description("Background colour of the case workflow status.")]
        public string? BackColor { get; set; }

        [Description("Raw JSON payload of the case.")]
        public string? Json { get; set; }

        [Description("Payload fields of the case, formatted per the case workflow XPath configuration.")]
        public List<CaseBySessionCaseSearchCompileFieldEntryDto>? FormattedPayload { get; set; }

        [Description("Names of the visible activations on the case.")]
        public List<CaseBySessionCaseSearchCompileActivationDto>? Activation { get; set; }

        [Description("Whether the case workflow has visualisation enabled.")]
        public bool EnableVisualisation { get; set; }

        [Description("Guid of the visualisation registry entry for the case workflow.")]
        public Guid VisualisationRegistryGuid { get; set; }

        [Description("Integer identifier of the entity analysis model the case belongs to.")]
        public int EntityAnalysisModelId { get; set; }
    }
}
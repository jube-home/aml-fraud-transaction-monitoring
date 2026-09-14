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

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Dto.CaseCreationWarning
{
    public sealed record CaseCreationWarningDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp the stage's warn threshold was crossed.")]
        DateTime OccurredDate,
        [property:
            Description(
                "Guid of the specific invocation (EntityAnalysisModelInstanceEntry) this case was created for.")]
        Guid EntityAnalysisModelInstanceEntryGuid,
        [property: Description("Guid of the case workflow this case belongs to.")]
        Guid CaseWorkflowGuid,
        [property: Description("The case key field name used to find/deduplicate this case.")]
        string CaseKey,
        [property: Description("The case key value for this case -- correlates back to the actual case record.")]
        string CaseKeyValue,
        [property:
            Description("Which case creation stage was slow -- see StageName for the human-readable form.")]
        int StageId,
        [property:
            Description(
                "Name of the case creation stage that was slow -- ExistingCasePriorityLookup, WorkflowStatusLookupAndPersist, Notification, or HttpEndpoint.")]
        string StageName,
        [property:
            Description(
                "The notification destination or HTTP endpoint URL involved, when StageName is Notification or HttpEndpoint; null for the other two stages.")]
        string Destination,
        [property: Description("How long this stage actually took, in microseconds.")]
        long DurationMicroseconds,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that captured this warning.")]
        string Instance);
}
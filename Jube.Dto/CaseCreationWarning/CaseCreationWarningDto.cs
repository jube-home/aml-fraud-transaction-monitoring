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
using Jube.Dto.Options;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.CaseCreationWarning
{
    [GridEndpoint("CaseCreationWarning", Title = "Case creation warning", DefaultSort = "occurredDate")]
    [GridFilter(Param = "stageId", Label = "Stage", Choices = ["1=ExistingCasePriorityLookup", "2=WorkflowStatusLookupAndPersist", "3=Notification", "4=HttpEndpoint"])]
    [GridSearch(Label = "Case key value contains")]
    public sealed record CaseCreationWarningDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("UTC timestamp the stage's warn threshold was crossed.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime OccurredDate,
        [property:
            Description(
                "Guid of the specific invocation (EntityAnalysisModelInstanceEntry) this case was created for.")]
        [property: ListColumn(Order = 80, Title = "Invocation Entry")]
        Guid EntityAnalysisModelInstanceEntryGuid,
        [property: Description("Guid of the case workflow this case belongs to.")]
        [property: ListColumn(Order = 50, Title = "Case Workflow")]
        Guid CaseWorkflowGuid,
        [property: Description("The case key field name used to find/deduplicate this case.")]
        [property: ListColumn(Order = 30)]
        string CaseKey,
        [property: Description("The case key value for this case -- correlates back to the actual case record.")]
        [property: ListColumn(Order = 40)]
        string CaseKeyValue,
        [property:
            Description("Which case creation stage was slow -- see StageName for the human-readable form.")]
        [property: ListColumn(Hidden = true)]
        int StageId,
        [property:
            Description(
                "Name of the case creation stage that was slow -- ExistingCasePriorityLookup, WorkflowStatusLookupAndPersist, Notification, or HttpEndpoint.")]
        [property: ListColumn(Order = 20, Title = "Stage")]
        string StageName,
        [property:
            Description(
                "The notification destination or HTTP endpoint URL involved, when StageName is Notification or HttpEndpoint; null for the other two stages.")]
        [property: ListColumn(Order = 60)]
        string Destination,
        [property: Description("How long this stage actually took, in microseconds.")]
        [property: ListColumn(Order = 70, Title = "Duration", Unit = "µs", Format = GridFormat.Integer)]
        long DurationMicroseconds,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        [property: ListColumn(Order = 100, Title = "Flushed Date")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that captured this warning.")]
        [property: ListColumn(Order = 90)]
        string Instance);
}
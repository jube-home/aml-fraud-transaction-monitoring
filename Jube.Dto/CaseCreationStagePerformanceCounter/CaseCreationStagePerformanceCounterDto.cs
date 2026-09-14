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

namespace Jube.Dto.CaseCreationStagePerformanceCounter
{
    public sealed record CaseCreationStagePerformanceCounterDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property:
            Description(
                "Which background case creation stage this row aggregates -- see StageName for the human-readable form.")]
        int StageId,
        [property:
            Description(
                "Name of the background case creation stage this row aggregates -- ExistingCasePriorityLookup, WorkflowStatusLookupAndPersist, Notification (only when case notifications are enabled), or HttpEndpoint (only when a case HTTP callback is enabled). Not scoped to any Model -- case creation is a single global queue shared by every model.")]
        string StageName,
        [property:
            Description("Total microseconds spent in this stage across every case counted in this interval.")]
        long TotalMicroseconds,
        [property: Description("The fastest single case of this stage in this interval, in microseconds.")]
        long MinMicroseconds,
        [property: Description("The slowest single case of this stage in this interval, in microseconds.")]
        long MaxMicroseconds,
        [property:
            Description(
                "Number of cases counted in this interval; divide TotalMicroseconds by this for the average cost.")]
        int InvokeCount,
        [property: Description("UTC timestamp this row's flush interval was written.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the node that wrote this row.")]
        string Instance);
}
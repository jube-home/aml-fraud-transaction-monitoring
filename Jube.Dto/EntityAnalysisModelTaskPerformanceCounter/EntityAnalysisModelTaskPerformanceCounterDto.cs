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

namespace Jube.Dto.EntityAnalysisModelTaskPerformanceCounter
{
    public sealed record EntityAnalysisModelTaskPerformanceCounterDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("GUID of the Model this per-minute rollup belongs to.")]
        Guid EntityAnalysisModelGuid,
        [property: Description("Name of the Model this per-minute rollup belongs to.")]
        string EntityAnalysisModelName,
        [property:
            Description(
                "Whether this task reads from or writes to cache/store -- see DirectionName for the human-readable form.")]
        int DirectionId,
        [property:
            Description("Name of the direction this task ran in during invocation (Read or Write).")]
        string DirectionName,
        [property:
            Description("Which async task this row aggregates -- see TaskName for the human-readable form.")]
        int TaskTypeId,
        [property:
            Description("Name of the async task this row aggregates (e.g. SanctionsAsync, CachePayloadUpsertAsync).")]
        string TaskName,
        [property:
            Description(
                "Total microseconds spent executing this task across every invocation counted in this interval; useful for cloud compute-cost attribution.")]
        long TotalMicroseconds,
        [property: Description("The fastest single invocation of this task in this interval, in microseconds.")]
        long MinMicroseconds,
        [property: Description("The slowest single invocation of this task in this interval, in microseconds.")]
        long MaxMicroseconds,
        [property:
            Description("Total bytes allocated executing this task across every invocation counted in this interval.")]
        long TotalAllocatedBytes,
        [property: Description("The smallest single invocation's allocation for this task in this interval, in bytes.")]
        long MinAllocatedBytes,
        [property: Description("The largest single invocation's allocation for this task in this interval, in bytes.")]
        long MaxAllocatedBytes,
        [property:
            Description(
                "Number of invocations counted in this interval; divide the totals by this for the average cost.")]
        int InvokeCount,
        [property: Description("UTC timestamp this row's flush interval was written.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the node that wrote this row.")]
        string Instance);
}
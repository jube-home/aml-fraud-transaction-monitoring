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

namespace Jube.Dto.EntityAnalysisModelResponseTimePipelineCounter
{
    public sealed record EntityAnalysisModelResponseTimePipelineCounterDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("GUID of the Model this per-minute rollup belongs to.")]
        Guid EntityAnalysisModelGuid,
        [property: Description("Name of the Model this per-minute rollup belongs to.")]
        string EntityAnalysisModelName,
        [property:
            Description(
                "Which main invocation pipeline checkpoint this row aggregates -- see StageName for the human-readable form.")]
        int StageId,
        [property:
            Description(
                "Name of the main invocation pipeline checkpoint this row aggregates (e.g. Parse, Gateway, WriteResponse).")]
        string StageName,
        [property:
            Description(
                "Fixed pipeline position of this checkpoint, letting a query reconstruct pipeline order despite the table being flat.")]
        int SequenceNumber,
        [property:
            Description(
                "Total microseconds spent up to and including this checkpoint across every invocation counted in this interval.")]
        long TotalMicroseconds,
        [property:
            Description(
                "The fastest single invocation's duration for this checkpoint in this interval, in microseconds.")]
        long MinMicroseconds,
        [property:
            Description(
                "The slowest single invocation's duration for this checkpoint in this interval, in microseconds.")]
        long MaxMicroseconds,
        [property:
            Description("Total bytes allocated for this checkpoint across every invocation counted in this interval.")]
        long TotalAllocatedBytes,
        [property:
            Description("The smallest single invocation's allocation for this checkpoint in this interval, in bytes.")]
        long MinAllocatedBytes,
        [property:
            Description("The largest single invocation's allocation for this checkpoint in this interval, in bytes.")]
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
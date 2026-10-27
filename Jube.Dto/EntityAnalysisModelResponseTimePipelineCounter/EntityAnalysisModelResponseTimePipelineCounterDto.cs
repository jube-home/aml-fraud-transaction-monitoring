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

namespace Jube.Dto.EntityAnalysisModelResponseTimePipelineCounter
{
    [GridEndpoint("EntityAnalysisModelResponseTimePipelineCounter", Title = "Model response time pipeline counter", DefaultSort = "createdDate")]
    [GridFilter(Param = "stageId", Label = "Stage", Choices = ["1=Parse", "2=CheckIntegrityAndUpsert", "3=InlineFunctions", "4=InlineScripts", "5=Gateway", "6=CacheDbStorage", "10=JoinReadTasks", "11=AbstractionRulesWithoutSearchKeys", "12=AbstractionCalculations", "13=ExhaustiveAdaptation", "14=HttpAdaptation", "15=Activation", "16=JoinWriteTasks", "17=WriteResponse", "18=BuildArchivePayload"])]
    [GridRatio("averageMicroseconds", nameof(TotalMicroseconds), nameof(InvokeCount), Title = "Average", Order = 80, Unit = "µs", Format = GridFormat.Integer)]
    [GridRatio("averageAllocatedBytes", nameof(TotalAllocatedBytes), nameof(InvokeCount), Title = "Average Allocated", Order = 120, Unit = "B", Format = GridFormat.Integer)]
    public sealed record EntityAnalysisModelResponseTimePipelineCounterDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("GUID of the Model this per-minute rollup belongs to.")]
        [property: ListColumn(Hidden = true)]
        Guid EntityAnalysisModelGuid,
        [property: Description("Name of the Model this per-minute rollup belongs to.")]
        [property: ListColumn(Order = 10, Title = "Entity Analysis Model")]
        string EntityAnalysisModelName,
        [property:
            Description(
                "Which main invocation pipeline checkpoint this row aggregates -- see StageName for the human-readable form.")]
        [property: ListColumn(Hidden = true)]
        int StageId,
        [property:
            Description(
                "Name of the main invocation pipeline checkpoint this row aggregates (e.g. Parse, Gateway, WriteResponse).")]
        [property: ListColumn(Order = 30, Title = "Stage")]
        string StageName,
        [property:
            Description(
                "Fixed pipeline position of this checkpoint, letting a query reconstruct pipeline order despite the table being flat.")]
        [property: ListColumn(Order = 20, Title = "Sequence", Format = GridFormat.Integer)]
        int SequenceNumber,
        [property:
            Description(
                "Total microseconds spent up to and including this checkpoint across every invocation counted in this interval.")]
        [property: ListColumn(Order = 50, Title = "Total", Unit = "µs", Format = GridFormat.Integer)]
        long TotalMicroseconds,
        [property:
            Description(
                "The fastest single invocation's duration for this checkpoint in this interval, in microseconds.")]
        [property: ListColumn(Order = 60, Title = "Min", Unit = "µs", Format = GridFormat.Integer)]
        long MinMicroseconds,
        [property:
            Description(
                "The slowest single invocation's duration for this checkpoint in this interval, in microseconds.")]
        [property: ListColumn(Order = 70, Title = "Max", Unit = "µs", Format = GridFormat.Integer)]
        long MaxMicroseconds,
        [property:
            Description("Total bytes allocated for this checkpoint across every invocation counted in this interval.")]
        [property: ListColumn(Order = 90, Title = "Total Allocated", Unit = "B", Format = GridFormat.Integer)]
        long TotalAllocatedBytes,
        [property:
            Description("The smallest single invocation's allocation for this checkpoint in this interval, in bytes.")]
        [property: ListColumn(Order = 100, Title = "Min Allocated", Unit = "B", Format = GridFormat.Integer)]
        long MinAllocatedBytes,
        [property:
            Description("The largest single invocation's allocation for this checkpoint in this interval, in bytes.")]
        [property: ListColumn(Order = 110, Title = "Max Allocated", Unit = "B", Format = GridFormat.Integer)]
        long MaxAllocatedBytes,
        [property:
            Description(
                "Number of invocations counted in this interval; divide the totals by this for the average cost.")]
        [property: ListColumn(Order = 130, Format = GridFormat.Integer)]
        int InvokeCount,
        [property: Description("UTC timestamp this row's flush interval was written.")]
        [property: ListColumn(Order = 40)]
        [property: GridTimeRange]
        DateTime CreatedDate,
        [property: Description("Hostname of the node that wrote this row.")]
        [property: ListColumn(Order = 140)]
        string Instance);
}
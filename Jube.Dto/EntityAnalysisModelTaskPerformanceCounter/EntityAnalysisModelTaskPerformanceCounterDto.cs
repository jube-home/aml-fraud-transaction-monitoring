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

namespace Jube.Dto.EntityAnalysisModelTaskPerformanceCounter
{
    [GridEndpoint("EntityAnalysisModelTaskPerformanceCounter", Title = "Model task performance counter", DefaultSort = "createdDate")]
    [GridFilter(Param = "directionId", Label = "Direction", Choices = ["1=Read", "2=Write"])]
    [GridFilter(Param = "taskTypeId", Label = "Task", Choices = ["1=SanctionsAsync", "2=TtlCountersAsync", "3=AbstractionRulesWithSearchKeysAsync", "4=CachePayloadLatestUpsertAsync", "5=CachePayloadUpsertAsync", "6=CachePayloadInsertAsync", "7=CacheTtlCounterEntryUpsertAsync", "8=CacheTtlCounterEntryIncrementAsync", "10=CacheSanctionInsertAsync", "28=UpsertReferenceDateAsync"])]
    [GridRatio("averageMicroseconds", nameof(TotalMicroseconds), nameof(InvokeCount), Title = "Average", Order = 80, Unit = "µs", Format = GridFormat.Integer)]
    [GridRatio("averageAllocatedBytes", nameof(TotalAllocatedBytes), nameof(InvokeCount), Title = "Average Allocated", Order = 120, Unit = "B", Format = GridFormat.Integer)]
    public sealed record EntityAnalysisModelTaskPerformanceCounterDto(
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
                "Whether this task reads from or writes to cache/store -- see DirectionName for the human-readable form.")]
        [property: ListColumn(Hidden = true)]
        int DirectionId,
        [property:
            Description("Name of the direction this task ran in during invocation (Read or Write).")]
        [property: ListColumn(Order = 20, Title = "Direction")]
        string DirectionName,
        [property:
            Description("Which async task this row aggregates -- see TaskName for the human-readable form.")]
        [property: ListColumn(Hidden = true)]
        int TaskTypeId,
        [property:
            Description("Name of the async task this row aggregates (e.g. SanctionsAsync, CachePayloadUpsertAsync).")]
        [property: ListColumn(Order = 30, Title = "Task")]
        string TaskName,
        [property:
            Description(
                "Total microseconds spent executing this task across every invocation counted in this interval; useful for cloud compute-cost attribution.")]
        [property: ListColumn(Order = 50, Title = "Total", Unit = "µs", Format = GridFormat.Integer)]
        long TotalMicroseconds,
        [property: Description("The fastest single invocation of this task in this interval, in microseconds.")]
        [property: ListColumn(Order = 60, Title = "Min", Unit = "µs", Format = GridFormat.Integer)]
        long MinMicroseconds,
        [property: Description("The slowest single invocation of this task in this interval, in microseconds.")]
        [property: ListColumn(Order = 70, Title = "Max", Unit = "µs", Format = GridFormat.Integer)]
        long MaxMicroseconds,
        [property:
            Description("Total bytes allocated executing this task across every invocation counted in this interval.")]
        [property: ListColumn(Order = 90, Title = "Total Allocated", Unit = "B", Format = GridFormat.Integer)]
        long TotalAllocatedBytes,
        [property: Description("The smallest single invocation's allocation for this task in this interval, in bytes.")]
        [property: ListColumn(Order = 100, Title = "Min Allocated", Unit = "B", Format = GridFormat.Integer)]
        long MinAllocatedBytes,
        [property: Description("The largest single invocation's allocation for this task in this interval, in bytes.")]
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
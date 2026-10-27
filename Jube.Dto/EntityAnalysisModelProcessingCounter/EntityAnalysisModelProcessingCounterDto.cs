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

namespace Jube.Dto.EntityAnalysisModelProcessingCounter
{
    [GridEndpoint("EntityAnalysisModelProcessingCounter", Title = "Model processing counter", DefaultSort = "createdDate")]
    [GridSearch(Label = "Model name contains")]
    [GridRatio("averageResponseTime", nameof(ModelTotalResponseTime), nameof(ModelInvoke), Title = "Average Response Time", Order = 100, Format = GridFormat.Integer)]
    public sealed record EntityAnalysisModelProcessingCounterDto(
        [property: Description("Name of the Entity Analysis Model this row's processing counters belong to.")]
        [property: ListColumn(Order = 10, Title = "Entity Analysis Model")]
        string Name,
        [property: Description(
            "Always 0 -- carried forward unchanged from the legacy DTO, whose read path never selected a " +
            "row identifier at all.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("UTC timestamp this row's flush interval was written.")]
        [property: ListColumn(Order = 30)]
        [property: GridTimeRange]
        DateTimeOffset? CreatedDate,
        [property: Description("Hostname of the node that wrote this row.")]
        [property: ListColumn(Order = 20)]
        string Instance,
        [property: Description("Count of synchronous Model invocations in this interval.")]
        [property: ListColumn(Order = 40, Title = "Model Invocation", Format = GridFormat.Integer)]
        int ModelInvoke,
        [property: Description("Count of Gateway Rule matches in this interval.")]
        [property: ListColumn(Order = 50, Title = "Gateway Rule Match", Format = GridFormat.Integer)]
        int GatewayMatch,
        [property: Description("Count of response-elevation triggers in this interval.")]
        [property: ListColumn(Order = 60, Format = GridFormat.Integer)]
        int ResponseElevation,
        [property: Description("Sum of response-elevation values across this interval's triggers.")]
        [property: ListColumn(Hidden = true)]
        double ResponseElevationSum,
        [property: Description("Count of activation-watcher triggers in this interval.")]
        [property: ListColumn(Order = 70, Format = GridFormat.Decimal)]
        double ActivationWatcher,
        [property: Description("The configured response-elevation trigger limit in effect for this interval.")]
        [property: ListColumn(Order = 80, Format = GridFormat.Integer)]
        int ResponseElevationLimit,
        [property: Description("Total Model response time (microseconds) summed across this interval's invocations.")]
        [property: ListColumn(Order = 90, Title = "Total Response Time", Format = GridFormat.Integer)]
        long ModelTotalResponseTime,
        [property: Description("Minimum single-invocation response time (microseconds) observed in this interval.")]
        [property: ListColumn(Order = 110, Title = "Min Response Time", Unit = "µs", Format = GridFormat.Integer)]
        long? MinResponseTimeMicroseconds,
        [property: Description("Maximum single-invocation response time (microseconds) observed in this interval.")]
        [property: ListColumn(Order = 120, Title = "Max Response Time", Unit = "µs", Format = GridFormat.Integer)]
        long? MaxResponseTimeMicroseconds,
        [property: Description("Count of archive write-ahead-log entries still pending at the end of this interval.")]
        [property: ListColumn(Order = 130, Format = GridFormat.Integer)]
        int? ArchiveWalPendingCount,
        [property: Description("Identifier of the Entity Analysis Model this row's processing counters belong to.")]
        [property: ListColumn(Hidden = true)]
        Guid EntityAnalysisModelGuid);
}
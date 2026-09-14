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

namespace Jube.Dto.EntityAnalysisModelProcessingCounter
{
    public sealed record EntityAnalysisModelProcessingCounterDto(
        [property: Description("Name of the Entity Analysis Model this row's processing counters belong to.")]
        string Name,
        [property: Description(
            "Always 0 -- carried forward unchanged from the legacy DTO, whose read path never selected a " +
            "row identifier at all.")]
        int Id,
        [property: Description("UTC timestamp this row's flush interval was written.")]
        DateTimeOffset? CreatedDate,
        [property: Description("Hostname of the node that wrote this row.")]
        string Instance,
        [property: Description("Count of synchronous Model invocations in this interval.")]
        int ModelInvoke,
        [property: Description("Count of Gateway Rule matches in this interval.")]
        int GatewayMatch,
        [property: Description("Count of response-elevation triggers in this interval.")]
        int ResponseElevation,
        [property: Description("Sum of response-elevation values across this interval's triggers.")]
        double ResponseElevationSum,
        [property: Description("Count of activation-watcher triggers in this interval.")]
        double ActivationWatcher,
        [property: Description("The configured response-elevation trigger limit in effect for this interval.")]
        int ResponseElevationLimit,
        [property: Description("Total Model response time (microseconds) summed across this interval's invocations.")]
        long ModelTotalResponseTime,
        [property: Description("Minimum single-invocation response time (microseconds) observed in this interval.")]
        long? MinResponseTimeMicroseconds,
        [property: Description("Maximum single-invocation response time (microseconds) observed in this interval.")]
        long? MaxResponseTimeMicroseconds,
        [property: Description("Count of archive write-ahead-log entries still pending at the end of this interval.")]
        int? ArchiveWalPendingCount,
        [property: Description("Identifier of the Entity Analysis Model this row's processing counters belong to.")]
        Guid EntityAnalysisModelGuid);
}
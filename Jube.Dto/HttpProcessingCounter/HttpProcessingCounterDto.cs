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

namespace Jube.Dto.HttpProcessingCounter
{
    public sealed record HttpProcessingCounterDto(
        [property: Description("Hostname of the node that wrote this row.")]
        string Instance,
        [property: Description("UTC timestamp this row's flush interval was written.")]
        DateTimeOffset? CreatedDate,
        [property: Description("Count of requests routed to synchronous Model invocation in this interval.")]
        int Model,
        [property: Description("Count of requests routed to asynchronous Model invocation in this interval.")]
        int AsynchronousModel,
        [property: Description("Count of requests routed to Model Tagging in this interval.")]
        int Tag,
        [property: Description("Count of requests that encountered an error in this interval.")]
        int Error,
        [property: Description("Count of requests routed to a direct Sanctions recall in this interval.")]
        int Sanction,
        [property: Description("Count of requests routed to a direct Exhaustive Adaptation recall in this interval.")]
        int Exhaustive,
        [property: Description("Count of every HTTP request received in this interval, across every route below.")]
        int All);
}
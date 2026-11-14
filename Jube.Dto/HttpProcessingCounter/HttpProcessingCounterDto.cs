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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.HttpProcessingCounter
{
    [GridEndpoint("HttpProcessingCounter", Title = "Http processing counter", DefaultSort = "createdDate")]
    [MessagePackObject]
    public sealed record HttpProcessingCounterDto(
        [property: Key(0)] [property: Description("Hostname of the node that wrote this row.")]
        [property: ListColumn(Order = 10)]
        string Instance,
        [property: Key(1)] [property: Description("UTC timestamp this row's flush interval was written.")]
        [property: ListColumn(Order = 20)]
        [property: GridTimeRange]
        DateTimeOffset? CreatedDate,
        [property: Key(2)] [property: Description("Count of requests routed to synchronous Model invocation in this interval.")]
        [property: ListColumn(Order = 30, Format = GridFormat.Integer)]
        int Model,
        [property: Key(3)] [property: Description("Count of requests routed to asynchronous Model invocation in this interval.")]
        [property: ListColumn(Order = 40, Format = GridFormat.Integer)]
        int AsynchronousModel,
        [property: Key(4)] [property: Description("Count of requests routed to Model Tagging in this interval.")]
        [property: ListColumn(Order = 50, Format = GridFormat.Integer)]
        int Tag,
        [property: Key(5)] [property: Description("Count of requests that encountered an error in this interval.")]
        [property: ListColumn(Order = 60, Format = GridFormat.Integer)]
        int Error,
        [property: Key(6)] [property: Description("Count of requests routed to a direct Sanctions recall in this interval.")]
        [property: ListColumn(Order = 70, Format = GridFormat.Integer)]
        int Sanction,
        [property: Key(7)] [property: Description("Count of requests routed to a direct Exhaustive Adaptation recall in this interval.")]
        [property: ListColumn(Order = 80, Format = GridFormat.Integer)]
        int Exhaustive,
        [property: Key(8)] [property: Description("Count of every HTTP request received in this interval, across every route below.")]
        [property: ListColumn(Order = 90, Format = GridFormat.Integer)]
        int All);
}
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

namespace Jube.Dto.RespCallCounter
{
    [GridEndpoint("RespCallCounter", Title = "RESP call counter", DefaultSort = "createdDate")]
    [GridSearch(Label = "Call contains")]
    [GridRatio("averageMicroseconds", nameof(TotalMicroseconds), nameof(Count), Title = "Average", Order = 70, Unit = "µs", Format = GridFormat.Integer)]
    [MessagePackObject]
    public sealed record RespCallCounterDto(
        [property: Key(0)] [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Key(1)] [property:
            Description(
                "Which Jube.Cache repository method issued the RESP calls counted in this row -- \"<Repository>.<Method>\", e.g. \"CachePayloadRepository.InsertAsync\". Never a RESP key or value.")]
        [property: ListColumn(Order = 10)]
        string Call,
        [property: Key(2)] [property: Description("How many calls to this Call label completed during this one-minute window.")]
        [property: ListColumn(Order = 30, Format = GridFormat.Integer)]
        long Count,
        [property: Key(3)] [property: Description("Sum of the elapsed time, in microseconds, of every call counted in this row.")]
        [property: ListColumn(Order = 40, Title = "Total", Unit = "µs", Format = GridFormat.Integer)]
        long TotalMicroseconds,
        [property: Key(4)] [property: Description("The fastest single call counted in this row, in microseconds.")]
        [property: ListColumn(Order = 50, Title = "Min", Unit = "µs", Format = GridFormat.Integer)]
        long MinMicroseconds,
        [property: Key(5)] [property: Description("The slowest single call counted in this row, in microseconds.")]
        [property: ListColumn(Order = 60, Title = "Max", Unit = "µs", Format = GridFormat.Integer)]
        long MaxMicroseconds,
        [property: Key(6)] [property:
            Description(
                "UTC timestamp this row was flushed to the database (the end of the one-minute window it covers).")]
        [property: ListColumn(Order = 20)]
        [property: GridTimeRange]
        DateTime CreatedDate,
        [property: Key(7)] [property: Description("Hostname of the Jube node that recorded this row.")]
        [property: ListColumn(Order = 80)]
        string Instance);
}
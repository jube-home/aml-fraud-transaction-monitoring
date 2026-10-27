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

namespace Jube.Dto.RedisCallCounter
{
    [GridEndpoint("RedisCallCounter", Title = "Redis call counter", DefaultSort = "createdDate")]
    [GridSearch(Label = "Call contains")]
    [GridRatio("averageMicroseconds", nameof(TotalMicroseconds), nameof(Count), Title = "Average", Order = 70, Unit = "µs", Format = GridFormat.Integer)]
    public sealed record RedisCallCounterDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property:
            Description(
                "Which Jube.Cache repository method issued the Redis calls counted in this row -- \"<Repository>.<Method>\", e.g. \"CachePayloadRepository.InsertAsync\". Never a Redis key or value.")]
        [property: ListColumn(Order = 10)]
        string Call,
        [property: Description("How many calls to this Call label completed during this one-minute window.")]
        [property: ListColumn(Order = 30, Format = GridFormat.Integer)]
        long Count,
        [property: Description("Sum of the elapsed time, in microseconds, of every call counted in this row.")]
        [property: ListColumn(Order = 40, Title = "Total", Unit = "µs", Format = GridFormat.Integer)]
        long TotalMicroseconds,
        [property: Description("The fastest single call counted in this row, in microseconds.")]
        [property: ListColumn(Order = 50, Title = "Min", Unit = "µs", Format = GridFormat.Integer)]
        long MinMicroseconds,
        [property: Description("The slowest single call counted in this row, in microseconds.")]
        [property: ListColumn(Order = 60, Title = "Max", Unit = "µs", Format = GridFormat.Integer)]
        long MaxMicroseconds,
        [property:
            Description(
                "UTC timestamp this row was flushed to the database (the end of the one-minute window it covers).")]
        [property: ListColumn(Order = 20)]
        [property: GridTimeRange]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that recorded this row.")]
        [property: ListColumn(Order = 80)]
        string Instance);
}
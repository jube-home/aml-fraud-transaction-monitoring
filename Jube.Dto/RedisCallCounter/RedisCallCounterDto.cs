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

namespace Jube.Dto.RedisCallCounter
{
    public sealed record RedisCallCounterDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property:
            Description(
                "Which Jube.Cache repository method issued the Redis calls counted in this row -- \"<Repository>.<Method>\", e.g. \"CachePayloadRepository.InsertAsync\". Never a Redis key or value.")]
        string Call,
        [property: Description("How many calls to this Call label completed during this one-minute window.")]
        long Count,
        [property: Description("Sum of the elapsed time, in microseconds, of every call counted in this row.")]
        long TotalMicroseconds,
        [property: Description("The fastest single call counted in this row, in microseconds.")]
        long MinMicroseconds,
        [property: Description("The slowest single call counted in this row, in microseconds.")]
        long MaxMicroseconds,
        [property:
            Description(
                "UTC timestamp this row was flushed to the database (the end of the one-minute window it covers).")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that recorded this row.")]
        string Instance);
}
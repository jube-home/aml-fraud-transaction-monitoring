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

namespace Jube.Extensions;

public static class DateTimeExtensions
{
    public static long ToUnixTimeMilliSeconds(this DateTime dateTime)
    {
        var dto = new DateTimeOffset(dateTime
            .ToUniversalTime());
        return dto.ToUnixTimeMilliseconds();
    }

    public static DateTime FromUnixTimeMilliSeconds(this long timestamp)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(timestamp)
            .DateTime.ToLocalTime();
    }

    public static DateTime Floor(this DateTime dateTime, TimeSpan interval)
    {
        return dateTime.AddTicks(-(dateTime.Ticks % interval.Ticks));
    }

    public static DateTime Ceiling(this DateTime dateTime, TimeSpan interval)
    {
        var overflow = dateTime.Ticks % interval.Ticks;

        return overflow == 0 ? dateTime : dateTime.AddTicks(interval.Ticks - overflow);
    }

    public static DateTime Round(this DateTime dateTime, TimeSpan interval)
    {
        var halfIntervalTicks = (interval.Ticks + 1) >> 1;

        return dateTime.AddTicks(halfIntervalTicks - (dateTime.Ticks + halfIntervalTicks) % interval.Ticks);
    }

    public static string ToUnixTime(this DateTime dateTime)
    {
        var dto = new DateTimeOffset(dateTime.ToUniversalTime());
        return dto.ToUnixTimeSeconds().ToString();
    }
}
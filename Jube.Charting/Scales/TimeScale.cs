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

namespace Jube.Charting.Scales
{
    public sealed class TimeScale(DateTime domainMin, DateTime domainMax, double rangeStart, double rangeEnd)
        : LinearScale(ToSeconds(domainMin), ToSeconds(domainMax), rangeStart, rangeEnd)
    {
        public static readonly DateTime Epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static double ToSeconds(DateTime value)
        {
            var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;

            return (utc - Epoch).TotalSeconds;
        }

        public static DateTime FromSeconds(double seconds)
        {
            return Epoch.AddSeconds(Math.Clamp(seconds, -62135596800d + 1, 253402300799d - 1));
        }

        public DateTime MapBack(double position)
        {
            return FromSeconds(Invert(position));
        }

        public double Map(DateTime value)
        {
            return Map(ToSeconds(value));
        }
    }
}
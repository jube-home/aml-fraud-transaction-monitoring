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

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static DateTime AddBusinessDays(this DateTime @this, int businessDays)
        {
            var date = @this;
            if (businessDays == 0)
            {
                return date;
            }

            var step = businessDays > 0 ? 1 : -1;
            var remaining = Math.Abs((long)businessDays);
            var weeks = (remaining - 1) / 5;
            date = date.AddDays(weeks * 7 * step);
            remaining -= weeks * 5;
            while (remaining > 0)
            {
                date = date.AddDays(step);
                if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                {
                    remaining--;
                }
            }

            return date;
        }
    }
}
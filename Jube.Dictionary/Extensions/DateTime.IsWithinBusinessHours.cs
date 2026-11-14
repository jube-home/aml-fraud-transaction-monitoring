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
    using DateTime = DateTime;

    public static partial class Extensions
    {
        [Description("Checks if the time falls within a given hour range (start inclusive, end exclusive).")]
        public static bool IsWithinBusinessHours(this DateTime @this,
            [Description("The first hour (0-23) considered within business hours.")] int startHour,
            [Description("The first hour (0-23) no longer considered within business hours.")] int endHour)
        {
            var hour = @this.Hour;

            return hour >= startHour && hour < endHour;
        }
    }
}

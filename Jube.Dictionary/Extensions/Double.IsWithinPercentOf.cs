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
        [Description("Checks if the value is within a percentage tolerance of a target value (e.g. an invoice amount matching a purchase order).")]
        public static bool IsWithinPercentOf(this double @this,
            [Description("The value to compare against.")] double target,
            [Description("The allowed tolerance, as a percentage of target.")] double percent)
        {
            if (target == 0)
            {
                return @this == 0;
            }

            return Math.Abs(@this.PercentageChangeFrom(target)) <= percent;
        }
    }
}

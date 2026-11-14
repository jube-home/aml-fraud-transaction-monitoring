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
        [Description("True when: a number found in the text is within the tolerance percentage of the target.")]
        public static bool NumberInTextWithinPercentOf(this string? @this,
            [Description("The number the numbers found in the text are compared with.")] double target,
            [Description("How far from the target still counts, as a percentage of it.")] double tolerancePercent)
        {
            if (string.IsNullOrEmpty(@this))
            {
                return false;
            }

            return @this.ExtractManyDouble().Any(number => number.IsWithinPercentOf(target, tolerancePercent));
        }
    }
}
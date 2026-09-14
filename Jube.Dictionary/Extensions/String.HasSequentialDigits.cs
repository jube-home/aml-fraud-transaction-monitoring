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
        public static bool HasSequentialDigits(this string? @this, int minRunLength)
        {
            var value = @this ?? "";
            var threshold = Math.Max(minRunLength, 2);

            var ascendingRun = 1;
            var descendingRun = 1;

            for (var i = 1; i < value.Length; i++)
            {
                var previousIsDigit = char.IsDigit(value[i - 1]);
                var currentIsDigit = char.IsDigit(value[i]);

                ascendingRun = previousIsDigit && currentIsDigit && value[i] - value[i - 1] == 1
                    ? ascendingRun + 1
                    : 1;

                descendingRun = previousIsDigit && currentIsDigit && value[i - 1] - value[i] == 1
                    ? descendingRun + 1
                    : 1;

                if (ascendingRun >= threshold || descendingRun >= threshold)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

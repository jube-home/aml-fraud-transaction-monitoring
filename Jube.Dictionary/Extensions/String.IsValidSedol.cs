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
        public static bool IsValidSedol(this string? @this)
        {
            var value = (@this ?? "").Trim().ToUpperInvariant();
            if (value.Length != 7 || !char.IsAsciiDigit(value[6]))
            {
                return false;
            }

            int[] weights = [1, 3, 1, 7, 3, 9];
            var sum = 0;
            for (var i = 0; i < 6; i++)
            {
                var c = value[i];
                if (char.IsAsciiDigit(c))
                {
                    sum += (c - '0') * weights[i];
                }
                else if (char.IsAsciiLetterUpper(c) && c is not ('A' or 'E' or 'I' or 'O' or 'U'))
                {
                    sum += (c - 'A' + 10) * weights[i];
                }
                else
                {
                    return false;
                }
            }

            return (10 - sum % 10) % 10 == value[6] - '0';
        }
    }
}
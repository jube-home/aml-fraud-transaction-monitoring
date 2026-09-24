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
        public static bool IsValidCusip(this string? @this)
        {
            var value = (@this ?? "").Trim().ToUpperInvariant();
            if (value.Length != 9 || !char.IsAsciiDigit(value[8]))
            {
                return false;
            }

            var sum = 0;
            for (var i = 0; i < 8; i++)
            {
                var c = value[i];
                int v;
                if (char.IsAsciiDigit(c))
                {
                    v = c - '0';
                }
                else if (char.IsAsciiLetterUpper(c))
                {
                    v = c - 'A' + 10;
                }
                else
                {
                    v = c switch
                    {
                        '*' => 36,
                        '@' => 37,
                        '#' => 38,
                        _ => -1
                    };
                }

                if (v < 0)
                {
                    return false;
                }

                if (i % 2 == 1)
                {
                    v *= 2;
                }

                sum += v / 10 + v % 10;
            }

            return (10 - sum % 10) % 10 == value[8] - '0';
        }
    }
}
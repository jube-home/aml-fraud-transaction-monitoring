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
        public static int FromRomanNumerals(this string? @this)
        {
            var text = (@this ?? "").Trim().ToUpperInvariant();
            if (text.Length is 0 or > 15)
            {
                return 0;
            }

            var total = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var current = RomanValue(text[i]);
                if (current == 0)
                {
                    return 0;
                }

                var next = i + 1 < text.Length ? RomanValue(text[i + 1]) : 0;
                total += current < next ? -current : current;
            }

            return total is >= 1 and <= 3999 && total.ToRomanNumerals() == text ? total : 0;

            static int RomanValue(char c)
            {
                return c switch
                {
                    'I' => 1,
                    'V' => 5,
                    'X' => 10,
                    'L' => 50,
                    'C' => 100,
                    'D' => 500,
                    'M' => 1000,
                    _ => 0
                };
            }
        }
    }
}
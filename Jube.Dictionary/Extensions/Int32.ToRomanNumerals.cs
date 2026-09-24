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
        public static string ToRomanNumerals(this int @this)
        {
            if (@this is < 1 or > 3999)
            {
                return "";
            }

            int[] values = [1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1];
            string[] symbols = ["M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I"];

            var remaining = @this;
            var builder = new System.Text.StringBuilder();
            for (var i = 0; i < values.Length; i++)
            {
                while (remaining >= values[i])
                {
                    builder.Append(symbols[i]);
                    remaining -= values[i];
                }
            }

            return builder.ToString();
        }
    }
}
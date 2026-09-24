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
        public static long FromRadixString(this string? @this, int radix)
        {
            var text = (@this ?? "").Trim().ToLowerInvariant();
            if (radix is < 2 or > 36 || text.Length == 0)
            {
                throw new FormatException("A radix from 2 to 36 and a non-empty digit string are required.");
            }

            var negative = text[0] == '-';
            if (negative)
            {
                text = text[1..];
            }

            if (text.Length == 0)
            {
                throw new FormatException("The digit string has no digits.");
            }

            long value = 0;
            foreach (var c in text)
            {
                var digit = c is >= '0' and <= '9' ? c - '0' : c is >= 'a' and <= 'z' ? c - 'a' + 10 : 99;
                if (digit >= radix)
                {
                    throw new FormatException($"'{c}' is not a digit in base {radix}.");
                }

                value = negative ? checked(value * radix - digit) : checked(value * radix + digit);
            }

            return value;
        }
    }
}
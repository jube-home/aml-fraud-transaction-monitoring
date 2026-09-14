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

using System.Numerics;
using System.Text;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static bool IsValidIban(this string? @this)
        {
            if (string.IsNullOrWhiteSpace(@this))
            {
                return false;
            }

            var cleaned = @this.Replace(" ", "").ToUpperInvariant();

            if (cleaned.Length is < 15 or > 34)
            {
                return false;
            }

            if (!char.IsLetter(cleaned[0]) || !char.IsLetter(cleaned[1]) ||
                !char.IsDigit(cleaned[2]) || !char.IsDigit(cleaned[3]))
            {
                return false;
            }

            foreach (var c in cleaned)
            {
                if (!char.IsLetterOrDigit(c))
                {
                    return false;
                }
            }

            var rearranged = cleaned.Substring(4) + cleaned.Substring(0, 4);

            var numeric = new StringBuilder();
            foreach (var c in rearranged)
            {
                numeric.Append(char.IsDigit(c) ? c.ToString() : (c - 'A' + 10).ToString());
            }

            var remainder = BigInteger.Parse(numeric.ToString()) % 97;

            return remainder == 1;
        }
    }
}

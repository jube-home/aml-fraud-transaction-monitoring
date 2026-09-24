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
        public static bool IsValidIsin(this string? @this)
        {
            var value = (@this ?? "").Trim().ToUpperInvariant();
            if (value.Length != 12 || !char.IsAsciiLetterUpper(value[0]) || !char.IsAsciiLetterUpper(value[1]) ||
                !char.IsAsciiDigit(value[11]) || !value.All(char.IsAsciiLetterOrDigit))
            {
                return false;
            }

            var expanded = new System.Text.StringBuilder();
            foreach (var c in value)
            {
                expanded.Append(char.IsAsciiDigit(c) ? (c - '0').ToString() : (c - 'A' + 10).ToString());
            }

            return expanded.ToString().IsValidLuhn();
        }
    }
}
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
        public static int CountOccurrences(this string? @this, string? value)
        {
            var text = @this ?? "";
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            var count = 0;
            var index = text.IndexOf(value, StringComparison.Ordinal);
            while (index >= 0)
            {
                count++;
                index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal);
            }

            return count;
        }
    }
}
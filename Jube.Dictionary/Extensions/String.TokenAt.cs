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
        [Description("The token at a zero-based position after splitting on a separator; negative positions count from the end.")]
        public static string TokenAt(this string? @this,
            [Description("The text that separates the tokens.")] string? separator,
            [Description("The zero-based position of the token.")] int index)
        {
            var tokens = (@this ?? "").Split(string.IsNullOrEmpty(separator) ? " " : separator);

            if (index < 0)
            {
                index += tokens.Length;
            }

            return index >= 0 && index < tokens.Length ? tokens[index] : "";
        }
    }
}
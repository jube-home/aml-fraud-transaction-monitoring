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
        [Description("The first n digits (1 to 11) of a card number, ignoring spaces and hyphens, otherwise empty.")]
        public static string CardIssuerPrefix(this string? @this,
            [Description("How many leading digits to take.")] int length)
        {
            var digits = new string((@this ?? "").Where(c => c is not (' ' or '-')).ToArray());

            if (length is < 1 or > 11 || digits.Length < length || !digits.All(char.IsAsciiDigit))
            {
                return "";
            }

            return digits.Substring(0, length);
        }
    }
}
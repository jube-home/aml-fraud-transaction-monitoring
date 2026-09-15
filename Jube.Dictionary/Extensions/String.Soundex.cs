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

using System.Text;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static string Soundex(this string? @this)
        {
            if (string.IsNullOrWhiteSpace(@this))
            {
                return "";
            }

            string Code(char c)
            {
                return c switch
                {
                    'B' or 'F' or 'P' or 'V' => "1",
                    'C' or 'G' or 'J' or 'K' or 'Q' or 'S' or 'X' or 'Z' => "2",
                    'D' or 'T' => "3",
                    'L' => "4",
                    'M' or 'N' => "5",
                    'R' => "6",
                    _ => ""
                };
            }

            var letters = @this.Where(char.IsLetter).Select(char.ToUpperInvariant).ToArray();
            if (letters.Length == 0)
            {
                return "";
            }

            var result = new StringBuilder();
            result.Append(letters[0]);

            var lastCode = Code(letters[0]);

            for (var i = 1; i < letters.Length && result.Length < 4; i++)
            {
                var code = Code(letters[i]);

                if (code != "" && code != lastCode)
                {
                    result.Append(code);
                }

                if (letters[i] != 'H' && letters[i] != 'W')
                {
                    lastCode = code;
                }
            }

            while (result.Length < 4)
            {
                result.Append('0');
            }

            return result.ToString();
        }
    }
}

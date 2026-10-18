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

using System.Text.Json;
using System.Text.RegularExpressions;

namespace Jube.Validations
{
    public static class JsonText
    {
        private const int MaxLength = 1_048_576;
        private static readonly Regex unicodeEscape = new(@"\\u([0-9a-fA-F]{4})", RegexOptions.Compiled);

        public static bool IsAcceptable(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            if (value.Length > MaxLength)
            {
                return false;
            }

            if (value.Contains("\\u0000", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (HasUnpairedSurrogateEscape(value))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(value);

                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool HasUnpairedSurrogateEscape(string value)
        {
            var matches = unicodeEscape.Matches(value);

            for (var i = 0; i < matches.Count; i++)
            {
                var codeUnit = Convert.ToInt32(matches[i].Groups[1].Value, 16);

                var isHighSurrogate = codeUnit is >= 0xD800 and <= 0xDBFF;
                var isLowSurrogate = codeUnit is >= 0xDC00 and <= 0xDFFF;

                if (!isHighSurrogate && !isLowSurrogate)
                {
                    continue;
                }

                var followedByMatchingLowSurrogate = isHighSurrogate
                                                     && i + 1 < matches.Count
                                                     && matches[i + 1].Index == matches[i].Index + matches[i].Length
                                                     && Convert.ToInt32(matches[i + 1].Groups[1].Value, 16) is >= 0xDC00
                                                         and <= 0xDFFF;

                var precededByMatchingHighSurrogate = isLowSurrogate
                                                      && i > 0
                                                      && matches[i - 1].Index + matches[i - 1].Length ==
                                                      matches[i].Index
                                                      && Convert.ToInt32(matches[i - 1].Groups[1].Value, 16) is
                                                          >= 0xD800 and <= 0xDBFF;

                if (!followedByMatchingLowSurrogate && !precededByMatchingHighSurrogate)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
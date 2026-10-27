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

namespace Jube.Case
{
    public static class HookTokens
    {
        private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static string Replace(string? template, IReadOnlyDictionary<string, string?> values,
            Func<string, string> encode)
        {
            if (string.IsNullOrEmpty(template))
            {
                return template ?? string.Empty;
            }

            var result = template;

            foreach (var token in Tokenisation.Tokenisation.ReturnTokens(template).Distinct())
            {
                var match = values.FirstOrDefault(f => string.Equals(f.Key, token, StringComparison.OrdinalIgnoreCase));

                if (match.Key == null || match.Value == null)
                {
                    continue;
                }

                result = result.Replace($"[@{token}@]", encode(match.Value));
            }

            return result;
        }

        public static string Url(string value)
        {
            return Uri.EscapeDataString(value);
        }

        public static string Json(string value)
        {
            return System.Text.Json.JsonSerializer.Serialize(value, JsonOptions)[1..^1];
        }
    }
}

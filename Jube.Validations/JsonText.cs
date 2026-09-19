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

namespace Jube.Validations
{
    public static class JsonText
    {
        private const int MaxLength = 1_048_576;

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
    }
}
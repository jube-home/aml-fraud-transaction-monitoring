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
using System.Text.Json.Nodes;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static bool JsonTextArrayAnyInList(this string? @this, string? path, IEnumerable<string>? list)
        {
            if (list is null)
            {
                return false;
            }

            var node = StructuredDataSupport.Navigate(StructuredDataSupport.Parse(@this), path);

            if (node is not JsonArray array)
            {
                return false;
            }

            var members = new HashSet<string>(list, StringComparer.Ordinal);

            return array.Any(element => element is JsonValue value &&
                                        value.GetValueKind() == JsonValueKind.String &&
                                        value.GetValue<string>() is { } text &&
                                        members.Contains(text));
        }
    }
}
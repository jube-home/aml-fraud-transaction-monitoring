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

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Jube.App.Wire
{
    public static class ColumnarJson
    {
        private static readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> Derived = new();

        public static JsonSerializerOptions OptionsFor(JsonSerializerOptions baseOptions)
        {
            return Derived.GetValue(baseOptions, source =>
            {
                var options = new JsonSerializerOptions(source);
                options.Converters.Insert(0, new ColumnarJsonConverterFactory());

                return options;
            });
        }

        public static byte[] Serialize(object value, System.Type type, JsonSerializerOptions baseOptions)
        {
            return JsonSerializer.SerializeToUtf8Bytes(value, type, OptionsFor(baseOptions));
        }
    }
}

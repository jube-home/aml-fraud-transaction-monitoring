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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Formatters;

namespace Jube.App.Wire
{
    public sealed class StringKeyedMapResolver : IFormatterResolver
    {
        public static readonly StringKeyedMapResolver Instance = new();

        private readonly ConcurrentDictionary<Type, object> formatters = new();

        private StringKeyedMapResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return (IMessagePackFormatter<T>)formatters.GetOrAdd(typeof(T), Create);
        }

        private static object Create(Type type)
        {
            if (!type.IsGenericType)
            {
                return null;
            }

            var definition = type.GetGenericTypeDefinition();

            if (definition != typeof(Dictionary<,>) && definition != typeof(IDictionary<,>)
                                                    && definition != typeof(IReadOnlyDictionary<,>))
            {
                return null;
            }

            var arguments = type.GetGenericArguments();

            if (arguments[0] == typeof(string) || !WireDictionaryKeys.IsSupported(arguments[0]))
            {
                return null;
            }

            return Activator.CreateInstance(
                typeof(StringKeyedMapFormatter<,,>).MakeGenericType(arguments[0], arguments[1], type));
        }
    }
}

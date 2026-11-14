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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jube.Dto.Wire;
using MessagePack;
using MessagePack.Formatters;

namespace Jube.App.Wire
{
    public sealed class StringKeyedMapFormatter<TKey, TValue, TMap> : IMessagePackFormatter<TMap>
        where TKey : notnull, IParsable<TKey>
        where TMap : class, IEnumerable<KeyValuePair<TKey, TValue>>
    {
        public void Serialize(ref MessagePackWriter writer, TMap value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();

                return;
            }

            var pairs = value as ICollection<KeyValuePair<TKey, TValue>> ?? value.ToList();
            var formatter = options.Resolver.GetFormatterWithVerify<TValue>();

            writer.WriteMapHeader(pairs.Count);

            foreach (var pair in pairs)
            {
                writer.Write(pair.Key is IFormattable formattable
                    ? formattable.ToString(null, CultureInfo.InvariantCulture)
                    : pair.Key.ToString());
                formatter.Serialize(ref writer, pair.Value, options);
            }
        }

        public TMap Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);

            try
            {
                var count = reader.ReadMapHeader();
                var formatter = options.Resolver.GetFormatterWithVerify<TValue>();
                var result = new Dictionary<TKey, TValue>(Math.Min(count, 1024));

                for (var i = 0; i < count; i++)
                {
                    var text = StrictUtf8String.Read(ref reader);

                    if (text == null || !TKey.TryParse(text, CultureInfo.InvariantCulture, out var key))
                    {
                        throw new MessagePackSerializationException("A map key is not a valid " + typeof(TKey).Name + ".");
                    }

                    result[key] = formatter.Deserialize(ref reader, options);
                }

                return (TMap)(object)result;
            }
            finally
            {
                reader.Depth--;
            }
        }
    }
}

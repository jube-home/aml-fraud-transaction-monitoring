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
using System.Globalization;
using System.Collections.Generic;
using Jube.Dto.Wire;
using MessagePack;
using MessagePack.Formatters;

namespace Jube.App.Wire
{
    public sealed class JsonShapedObjectFormatter : IMessagePackFormatter<object>
    {
        public void Serialize(ref MessagePackWriter writer, object value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();

                return;
            }

            switch (value)
            {
                case DBNull:
                    writer.WriteNil();

                    return;
                case byte[] bytes:
                    writer.Write(Convert.ToBase64String(bytes));

                    return;
                case TimeSpan span:
                    writer.Write(span.ToString("c", CultureInfo.InvariantCulture));

                    return;
                case DateOnly date:
                    writer.Write(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

                    return;
                case TimeOnly time:
                    writer.Write(time.ToTimeSpan().ToString("c", CultureInfo.InvariantCulture));

                    return;
            }

            var type = value.GetType();

            if (type == typeof(object))
            {
                writer.WriteMapHeader(0);

                return;
            }

            MessagePackSerializer.Serialize(type, ref writer, value, options);
        }

        public object Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            options.Security.DepthStep(ref reader);

            try
            {
                switch (reader.NextMessagePackType)
                {
                    case MessagePackType.Nil:
                        reader.ReadNil();

                        return null;
                    case MessagePackType.Boolean:
                        return reader.ReadBoolean();
                    case MessagePackType.Integer:
                        return reader.NextCode == MessagePackCode.UInt64
                            ? reader.ReadUInt64()
                            : reader.ReadInt64();
                    case MessagePackType.Float:
                        return reader.ReadDouble();
                    case MessagePackType.String:
                        return StrictUtf8String.Read(ref reader);
                    case MessagePackType.Extension:
                        return reader.ReadDateTime();
                    case MessagePackType.Array:
                    {
                        var length = reader.ReadArrayHeader();
                        var list = new List<object>(Math.Min(length, 1024));

                        for (var i = 0; i < length; i++)
                        {
                            list.Add(Deserialize(ref reader, options));
                        }

                        return list;
                    }
                    case MessagePackType.Map:
                    {
                        var length = reader.ReadMapHeader();
                        var map = new Dictionary<string, object>(Math.Min(length, 1024));

                        for (var i = 0; i < length; i++)
                        {
                            if (reader.NextMessagePackType != MessagePackType.String)
                            {
                                throw new MessagePackSerializationException("Map keys must be strings.");
                            }

                            var key = StrictUtf8String.Read(ref reader);
                            map[key] = Deserialize(ref reader, options);
                        }

                        return map;
                    }
                    default:
                        throw new MessagePackSerializationException("Unsupported MessagePack value.");
                }
            }
            finally
            {
                reader.Depth--;
            }
        }
    }
}

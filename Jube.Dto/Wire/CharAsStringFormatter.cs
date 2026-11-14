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

using MessagePack;
using MessagePack.Formatters;

namespace Jube.Dto.Wire
{
    public sealed class CharAsStringFormatter : IMessagePackFormatter<char>
    {
        public void Serialize(ref MessagePackWriter writer, char value, MessagePackSerializerOptions options)
        {
            writer.Write(value.ToString());
        }

        public char Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.NextMessagePackType == MessagePackType.Integer)
            {
                return (char)reader.ReadUInt16();
            }

            var text = StrictUtf8String.Read(ref reader);

            if (text is not { Length: 1 })
            {
                throw new MessagePackSerializationException("The value must be a single character.");
            }

            return text[0];
        }
    }
}

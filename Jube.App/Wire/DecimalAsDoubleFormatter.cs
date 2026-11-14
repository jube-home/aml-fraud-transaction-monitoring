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
using MessagePack;
using MessagePack.Formatters;

namespace Jube.App.Wire
{
    public sealed class DecimalAsDoubleFormatter : IMessagePackFormatter<decimal>
    {
        public void Serialize(ref MessagePackWriter writer, decimal value, MessagePackSerializerOptions options)
        {
            writer.Write((double)value);
        }

        public decimal Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var value = reader.ReadDouble();

            if (!double.IsFinite(value))
            {
                throw new MessagePackSerializationException("The value must be a finite number.");
            }

            try
            {
                return (decimal)value;
            }
            catch (OverflowException exception)
            {
                throw new MessagePackSerializationException("The value is outside the range of a decimal.", exception);
            }
        }
    }
}

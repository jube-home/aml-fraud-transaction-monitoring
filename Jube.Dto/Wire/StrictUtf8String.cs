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


using System.Text;
using MessagePack;

namespace Jube.Dto.Wire
{
    public static class StrictUtf8String
    {
        private static readonly UTF8Encoding Strict = new(false, true);

        public static string Read(ref MessagePackReader reader)
        {
            var sequence = reader.ReadStringSequence();

            if (sequence == null)
            {
                return null;
            }

            try
            {
                return Strict.GetString(sequence.Value);
            }
            catch (DecoderFallbackException)
            {
                throw new MessagePackSerializationException("The string is not valid UTF-8.");
            }
        }
    }
}

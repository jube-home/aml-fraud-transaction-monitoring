/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Text;

namespace Jube.Data.Reporting.Check
{
    internal sealed class SqlPositionMap(string original, string substituted,
        IReadOnlyList<SqlParameterReplacement> replacements)
    {
        private int[] byteToCharacter;

        public int? ToOriginalPosition(int byteOffset)
        {
            if (byteOffset < 0)
            {
                return null;
            }

            var table = ByteToCharacter();

            if (byteOffset >= table.Length)
            {
                return null;
            }

            var index = table[byteOffset];
            var delta = 0;

            foreach (var replacement in replacements)
            {
                var start = replacement.OriginalStart + delta;

                if (index < start)
                {
                    break;
                }

                if (index < start + replacement.NewLength)
                {
                    return replacement.OriginalStart + 1;
                }

                delta += replacement.NewLength - replacement.OriginalLength;
            }

            var position = index - delta + 1;

            return position >= 1 && position <= original.Length + 1 ? position : null;
        }

        private int[] ByteToCharacter()
        {
            if (byteToCharacter != null)
            {
                return byteToCharacter;
            }

            var bytes = Encoding.UTF8.GetByteCount(substituted);
            var table = new int[bytes + 1];
            var offset = 0;

            for (var i = 0; i < substituted.Length; i++)
            {
                var width = char.IsHighSurrogate(substituted[i]) && i + 1 < substituted.Length
                    ? 4
                    : char.IsLowSurrogate(substituted[i])
                        ? 0
                        : Encoding.UTF8.GetByteCount(substituted[i].ToString());

                for (var b = 0; b < width && offset + b < table.Length; b++)
                {
                    table[offset + b] = i;
                }

                offset += width;
            }

            if (offset < table.Length)
            {
                table[offset] = substituted.Length;
            }

            byteToCharacter = table;

            return table;
        }
    }
}

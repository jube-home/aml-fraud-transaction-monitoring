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

namespace Jube.Preservation
{
    using Exceptions;
    using MessagePack;

    internal static class PackageSizeGuard
    {
        private const sbyte Lz4BlockArrayTypeCode = 98;

        internal static void EnsureWithin(byte[] compressed, long maximumDecompressedBytes)
        {
            if (DeclaredBytes(compressed) > maximumDecompressedBytes)
            {
                throw new PackageTooLargeImportException();
            }
        }

        private static long DeclaredBytes(byte[] compressed)
        {
            try
            {
                var reader = new MessagePackReader(compressed);
                reader.ReadArrayHeader();
                var header = reader.ReadExtensionFormatHeader();

                if (header.TypeCode != Lz4BlockArrayTypeCode)
                {
                    return compressed.Length;
                }

                var lengths = new MessagePackReader(reader.ReadRaw(header.Length));
                long total = 0;

                while (!lengths.End)
                {
                    total += lengths.ReadInt64();
                }

                return total;
            }
            catch (Exception ex) when (ex is MessagePackSerializationException or EndOfStreamException
                                           or InvalidOperationException)
            {
                return 0;
            }
        }
    }
}

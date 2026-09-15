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

namespace Jube.OpenTelemetryListener
{
    public static class ProtobufStringScanner
    {
        private const int MaxDepth = 32;
        private const int MaxStrings = 200;
        private const int MinStringLength = 2;

        public static List<string> ExtractStrings(ReadOnlySpan<byte> data)
        {
            var strings = new List<string>();
            Walk(data, strings, 0);
            return strings;
        }

        private static void Walk(ReadOnlySpan<byte> data, List<string> strings, int depth)
        {
            if (depth > MaxDepth || strings.Count >= MaxStrings)
            {
                return;
            }

            var offset = 0;
            while (offset < data.Length && strings.Count < MaxStrings)
            {
                if (!TryReadVarint(data, ref offset, out var key))
                {
                    return;
                }

                var wireType = (int)(key & 0x7);

                switch (wireType)
                {
                    case 0:
                        if (!TryReadVarint(data, ref offset, out _))
                        {
                            return;
                        }

                        break;

                    case 1:
                        if (offset + 8 > data.Length)
                        {
                            return;
                        }

                        offset += 8;
                        break;

                    case 2:
                        if (!TryReadVarint(data, ref offset, out var length) || length > int.MaxValue ||
                            offset + (long)length > data.Length)
                        {
                            return;
                        }

                        var slice = data.Slice(offset, (int)length);
                        TryAddString(slice, strings);
                        Walk(slice, strings, depth + 1);
                        offset += (int)length;
                        break;

                    case 5:
                        if (offset + 4 > data.Length)
                        {
                            return;
                        }

                        offset += 4;
                        break;

                    default:
                        return;
                }
            }
        }

        private static void TryAddString(ReadOnlySpan<byte> slice, List<string> strings)
        {
            if (slice.Length < MinStringLength)
            {
                return;
            }

            string text;
            try
            {
                text = Encoding.UTF8.GetString(slice);
            }
            catch (DecoderFallbackException)
            {
                return;
            }

            if (IsMostlyPrintable(text))
            {
                strings.Add(text);
            }
        }

        private static bool IsMostlyPrintable(string text)
        {
            var printable = 0;
            foreach (var c in text)
            {
                if (!char.IsControl(c) || c is '\t' or '\n' or '\r')
                {
                    printable++;
                }
            }

            return printable == text.Length;
        }

        private static bool TryReadVarint(ReadOnlySpan<byte> data, ref int offset, out ulong value)
        {
            value = 0;
            var shift = 0;

            while (offset < data.Length)
            {
                var b = data[offset];
                offset++;
                value |= (ulong)(b & 0x7F) << shift;

                if ((b & 0x80) == 0)
                {
                    return true;
                }

                shift += 7;
                if (shift >= 64)
                {
                    return false;
                }
            }

            return false;
        }
    }
}
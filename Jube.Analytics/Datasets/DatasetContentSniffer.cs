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

namespace Jube.Analytics.Datasets
{
    public static class DatasetContentSniffer
    {
        private static readonly (byte[] Magic, string Description)[] binaryMagics =
        [
            ([0x50, 0x4B, 0x03, 0x04], "ZIP archive"),
            ([0x50, 0x4B, 0x05, 0x06], "ZIP archive"),
            ([0x1F, 0x8B], "gzip data"),
            ([0x42, 0x5A, 0x68], "bzip2 data"),
            ([0x37, 0x7A, 0xBC, 0xAF], "7z archive"),
            ([0x52, 0x61, 0x72, 0x21], "RAR archive"),
            ([0xFD, 0x37, 0x7A, 0x58, 0x5A], "xz data"),
            ([0x28, 0xB5, 0x2F, 0xFD], "zstd data"),
            ([0x25, 0x50, 0x44, 0x46], "PDF document"),
            ([0xD0, 0xCF, 0x11, 0xE0], "legacy Office document"),
            ([0x4D, 0x5A], "executable"),
            ([0x7F, 0x45, 0x4C, 0x46], "executable")
        ];

        public static string? DetectBinary(byte[] head, int length)
        {
            foreach (var (magic, description) in binaryMagics)
            {
                if (length >= magic.Length && head.AsSpan(0, magic.Length).SequenceEqual(magic))
                {
                    return description;
                }
            }

            if (length >= 2 && ((head[0] == 0xFF && head[1] == 0xFE) || (head[0] == 0xFE && head[1] == 0xFF)))
            {
                return "UTF-16 text, which is not accepted (UTF-8 only)";
            }

            return null;
        }

        public static bool LooksLikeJson(byte[] head, int length)
        {
            for (var i = 0; i < length; i++)
            {
                if (head[i] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
                {
                    continue;
                }

                return head[i] is (byte)'{' or (byte)'[';
            }

            return false;
        }

        public static bool HasUtf8Bom(byte[] head, int length)
        {
            return length >= 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF;
        }
    }
}
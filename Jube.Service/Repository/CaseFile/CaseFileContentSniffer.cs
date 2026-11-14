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

namespace Jube.Service.Repository.CaseFile
{
    public static class CaseFileContentSniffer
    {
        public const string Unknown = "application/octet-stream";
        public const string Text = "text/plain";
        public const string Html = "text/html";
        public const string Svg = "image/svg+xml";
        public const string Script = "text/x-script";

        private const int TextWindow = 8192;
        private const int MarkupWindow = 2048;

        private static readonly string[] htmlMarkers =
            ["<!doctype html", "<html", "<script", "<iframe", "<body", "<head", "<?php", "<object", "<embed"];

        public static string Sniff(byte[] content)
        {
            if (content.Length == 0)
            {
                return Unknown;
            }

            var binary = SniffBinary(content);

            return binary ?? SniffText(content);
        }

        private static string? SniffBinary(byte[] b)
        {
            if (Starts(b, 0x4D, 0x5A))
            {
                return "application/x-msdownload";
            }

            if (Starts(b, 0x7F, 0x45, 0x4C, 0x46))
            {
                return "application/x-elf";
            }

            if (Starts(b, 0xCA, 0xFE, 0xBA, 0xBE) || Starts(b, 0xFE, 0xED, 0xFA, 0xCE) ||
                Starts(b, 0xFE, 0xED, 0xFA, 0xCF) || Starts(b, 0xCE, 0xFA, 0xED, 0xFE) ||
                Starts(b, 0xCF, 0xFA, 0xED, 0xFE))
            {
                return "application/x-mach-binary";
            }

            if (Starts(b, 0x23, 0x21))
            {
                return Script;
            }

            if (Starts(b, 0x25, 0x50, 0x44, 0x46))
            {
                return "application/pdf";
            }

            if (Starts(b, 0x89, 0x50, 0x4E, 0x47))
            {
                return "image/png";
            }

            if (Starts(b, 0xFF, 0xD8, 0xFF))
            {
                return "image/jpeg";
            }

            if (Starts(b, 0x47, 0x49, 0x46, 0x38))
            {
                return "image/gif";
            }

            if (Starts(b, 0x52, 0x49, 0x46, 0x46) && b.Length >= 12)
            {
                var kind = Encoding.ASCII.GetString(b, 8, 4);

                return kind switch
                {
                    "WEBP" => "image/webp",
                    "WAVE" => "audio/wav",
                    _ => null
                };
            }

            if (b.Length >= 26 && Starts(b, 0x42, 0x4D) && b[6] == 0 && b[7] == 0 && b[8] == 0 && b[9] == 0)
            {
                return "image/bmp";
            }

            if (Starts(b, 0x49, 0x49, 0x2A, 0x00) || Starts(b, 0x4D, 0x4D, 0x00, 0x2A))
            {
                return "image/tiff";
            }

            if (Starts(b, 0x50, 0x4B, 0x03, 0x04) || Starts(b, 0x50, 0x4B, 0x05, 0x06))
            {
                return "application/zip";
            }

            if (Starts(b, 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1))
            {
                return "application/x-ole-storage";
            }

            if (Starts(b, 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C))
            {
                return "application/x-7z-compressed";
            }

            if (Starts(b, 0x1F, 0x8B))
            {
                return "application/gzip";
            }

            if (b.Length >= 262 && Encoding.ASCII.GetString(b, 257, 5) == "ustar")
            {
                return "application/x-tar";
            }

            if (Starts(b, 0x7B, 0x5C, 0x72, 0x74, 0x66))
            {
                return "application/rtf";
            }

            if (Starts(b, 0x49, 0x44, 0x33) || (b.Length >= 2 && b[0] == 0xFF && b[1] != 0xFE && (b[1] & 0xE0) == 0xE0))
            {
                return "audio/mpeg";
            }

            if (b.Length >= 12 && Encoding.ASCII.GetString(b, 4, 4) == "ftyp")
            {
                return "video/mp4";
            }

            return null;
        }

        private static string SniffText(byte[] content)
        {
            var utf16 = Starts(content, 0xFF, 0xFE) || Starts(content, 0xFE, 0xFF);

            if (!utf16 && Array.IndexOf(content, (byte)0, 0, Math.Min(content.Length, TextWindow)) >= 0)
            {
                return Unknown;
            }

            var length = Math.Min(content.Length, MarkupWindow);

            var head = (utf16
                    ? (content[0] == 0xFF ? Encoding.Unicode : Encoding.BigEndianUnicode).GetString(content, 2,
                        Math.Max(0, length - 2) / 2 * 2)
                    : Encoding.UTF8.GetString(content, 0, length))
                .TrimStart('\uFEFF').ToLowerInvariant();

            if (head.Contains("<svg", StringComparison.Ordinal))
            {
                return Svg;
            }

            return htmlMarkers.Any(marker => head.Contains(marker, StringComparison.Ordinal)) ? Html : Text;
        }

        private static bool Starts(byte[] content, params ReadOnlySpan<byte> prefix)
        {
            return content.Length >= prefix.Length && content.AsSpan(0, prefix.Length).SequenceEqual(prefix);
        }
    }
}

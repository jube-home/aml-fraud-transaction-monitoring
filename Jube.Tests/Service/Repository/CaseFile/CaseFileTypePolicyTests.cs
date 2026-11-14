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

using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Jube.Service.Repository.CaseFile;
using Jube.Service.Repository.CaseFile.Models;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Service.Repository.CaseFile
{
    [Collection("EvalExpressionRegistry")]
    [Trait("Category", "Unit")]
    public sealed class CaseFileTypePolicyTests
    {
        private static byte[] Bytes(params byte[] values) => values;

        private static byte[] Padded(byte[] head, int length = 64)
        {
            var result = new byte[length];
            head.CopyTo(result, 0);

            return result;
        }

        public static TheoryData<string, byte[], string> Samples() => new()
        {
            { "pdf", Encoding.ASCII.GetBytes("%PDF-1.7"), "application/pdf" },
            { "png", Padded(Bytes(0x89, 0x50, 0x4E, 0x47)), "image/png" },
            { "jpeg", Padded(Bytes(0xFF, 0xD8, 0xFF, 0xE0)), "image/jpeg" },
            { "gif", Encoding.ASCII.GetBytes("GIF89a"), "image/gif" },
            { "webp", Encoding.ASCII.GetBytes("RIFF....WEBPVP8 "), "image/webp" },
            { "wav", Encoding.ASCII.GetBytes("RIFF....WAVEfmt "), "audio/wav" },
            { "zip", Padded(Bytes(0x50, 0x4B, 0x03, 0x04)), "application/zip" },
            { "ole", Padded(Bytes(0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1)), "application/x-ole-storage" },
            { "7z", Padded(Bytes(0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C)), "application/x-7z-compressed" },
            { "gzip", Padded(Bytes(0x1F, 0x8B, 0x08)), "application/gzip" },
            { "rtf", Encoding.ASCII.GetBytes("{\\rtf1\\ansi"), "application/rtf" },
            { "mp4", Encoding.ASCII.GetBytes("....ftypisom...."), "video/mp4" },
            { "exe", Padded(Bytes(0x4D, 0x5A, 0x90)), "application/x-msdownload" },
            { "elf", Padded(Bytes(0x7F, 0x45, 0x4C, 0x46)), "application/x-elf" },
            { "macho", Padded(Bytes(0xCF, 0xFA, 0xED, 0xFE)), "application/x-mach-binary" },
            { "script", Encoding.ASCII.GetBytes("#!/bin/sh\necho"), "text/x-script" },
            { "html", Encoding.UTF8.GetBytes("  <!DOCTYPE HTML><p>x</p>"), "text/html" },
            { "htmlInText", Encoding.UTF8.GetBytes("notes\n<script>alert(1)</script>"), "text/html" },
            { "php", Encoding.UTF8.GetBytes("<?php echo 1;"), "text/html" },
            { "svg", Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><svg></svg>"), "image/svg+xml" },
            { "text", Encoding.UTF8.GetBytes("An ordinary note of evidence."), "text/plain" },
            { "arabicText", Encoding.UTF8.GetBytes("ملاحظة عادية"), "text/plain" },
            { "utf16Text", Bytes(0xFF, 0xFE, 0x41, 0x00, 0x42, 0x00), "text/plain" },
            { "utf16Html", [0xFF, 0xFE, .. Encoding.Unicode.GetBytes("<html>")], "text/html" },
            { "unknownBinary", Bytes(0x01, 0x02, 0x00, 0x04), "application/octet-stream" },
            { "empty", [], "application/octet-stream" }
        };

        [Theory]
        [MemberData(nameof(Samples))]
        public void TheSnifferNamesTheContentFromItsBytes(string label, byte[] content, string expected)
        {
            CaseFileContentSniffer.Sniff(content).Should().Be(expected, label);
        }

        [Fact]
        public void TheDefaultsRefuseExecutablesScriptsHtmlAndSvgByExtensionAndByContent()
        {
            var policy = CaseFileTypePolicy.FromEnvironment(null);

            foreach (var name in new[] { "a.exe", "a.dll", "a.js", "a.html", "a.htm", "a.svg", "a.sh", "a.bat", "a.php", "a" })
            {
                policy.Evaluate(name, Encoding.ASCII.GetBytes("text")).Should()
                    .Be(CaseFileTypeVerdict.ExtensionNotAllowed, name);
            }

            policy.Evaluate("a.pdf", Padded(Bytes(0x4D, 0x5A))).Should().Be(CaseFileTypeVerdict.ContentNotAllowed);
            policy.Evaluate("a.txt", Encoding.UTF8.GetBytes("<svg/>")).Should().Be(CaseFileTypeVerdict.ContentNotAllowed);
            policy.Evaluate("a.PDF", Encoding.ASCII.GetBytes("%PDF-1.4")).Should().Be(CaseFileTypeVerdict.Accepted);
            policy.Evaluate("a.docx", Padded(Bytes(0x50, 0x4B, 0x03, 0x04))).Should().Be(CaseFileTypeVerdict.Accepted);
            policy.Evaluate("a.msg", Padded(Bytes(0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1)))
                .Should().Be(CaseFileTypeVerdict.Accepted);
            policy.Evaluate("a.eml", Encoding.ASCII.GetBytes("From: a@b.example\r\n")).Should()
                .Be(CaseFileTypeVerdict.Accepted);
        }

        [Fact]
        public void TheCodeDefaultsMatchTheDynamicEnvironmentDefaults()
        {
            var environment = TestDynamicEnvironment.Create();

            environment.AppSettings(CaseFileTypePolicy.ExtensionsSetting).Should()
                .Be(CaseFileTypePolicy.DefaultExtensions);

            environment.AppSettings(CaseFileTypePolicy.ContentTypesSetting).Should()
                .Be(CaseFileTypePolicy.DefaultContentTypes);
        }

        [Fact]
        public void ASettingReplacesTheListAndAStarAcceptsAnything()
        {
            var custom = CaseFileTypePolicy.FromEnvironment(TestDynamicEnvironment.Create(
                new Dictionary<string, string>
                {
                    [CaseFileTypePolicy.ExtensionsSetting] = ".abc; XYZ", [CaseFileTypePolicy.ContentTypesSetting] = "text/plain"
                }));

            custom.Evaluate("a.xyz", Encoding.ASCII.GetBytes("t")).Should().Be(CaseFileTypeVerdict.Accepted);
            custom.Evaluate("a.pdf", Encoding.ASCII.GetBytes("t")).Should().Be(CaseFileTypeVerdict.ExtensionNotAllowed);

            var open = CaseFileTypePolicy.FromEnvironment(TestDynamicEnvironment.Create(
                new Dictionary<string, string>
                {
                    [CaseFileTypePolicy.ExtensionsSetting] = "*", [CaseFileTypePolicy.ContentTypesSetting] = "*"
                }));

            open.Evaluate("a.exe", Padded(Bytes(0x4D, 0x5A))).Should().Be(CaseFileTypeVerdict.Accepted);
        }
    }
}

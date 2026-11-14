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

using System.Linq;
using FluentAssertions;
using Jube.OpenTelemetryListener;
using Xunit;

namespace Jube.Test.Service.Observability
{
    [Trait("Category", "Unit")]
    public sealed class ListenerConsoleTextTests
    {
        [Theory]
        [InlineData("text/plain\r\n[2000-01-01T00:00:00Z] POST /v1/logs forged")]
        [InlineData("a\nb")]
        [InlineData("\u0000nul")]
        [InlineData("\u0085next")]
        [InlineData("\u2028sep\u2029para")]
        [InlineData("\u202Ereversed")]
        [InlineData("\u001b[31mred")]
        public void ControlSeparatorAndDirectionalCharactersNeverReachTheConsoleLiterally(string hostile)
        {
            var escaped = ConsoleText.Escape(hostile);

            escaped.Any(c => char.IsControl(c) || c is '\u2028' or '\u2029' or '\u202E').Should().BeFalse();
            escaped.Should().Contain("\\u");
        }

        [Fact]
        public void OrdinaryTextIncludingUnicodeLettersIsUnchanged()
        {
            const string text = "application/x-protobuf; Zażółć gęślą jaźń مرحبا 你好";

            ConsoleText.Escape(text).Should().Be(text);
        }

        [Fact]
        public void NullAndEmptyBecomeEmpty()
        {
            ConsoleText.Escape(null).Should().BeEmpty();
            ConsoleText.Escape(string.Empty).Should().BeEmpty();
        }

        [Fact]
        public void EscapedProtobufStringsKeepTheirValuesButLoseTheirLineBreaks()
        {
            var payload = new byte[] { 0x0A, 0x09, (byte)'l', (byte)'i', (byte)'n', (byte)'e', (byte)'1', 0x0D, 0x0A, (byte)'X', (byte)'Y' };

            var strings = ProtobufStringScanner.ExtractStrings(payload).Select(ConsoleText.Escape).ToList();

            strings.Should().Contain("line1\\u000d\\u000aXY");
        }
    }
}

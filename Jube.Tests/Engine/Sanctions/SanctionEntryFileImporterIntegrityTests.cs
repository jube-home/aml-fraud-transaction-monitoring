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
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.Sanctions;
using Jube.Test.Infrastructure;
using Microsoft.VisualBasic.FileIO;
using Xunit;

namespace Jube.Test.Engine.Sanctions
{
    [Trait("Category", "Unit")]
    public sealed class SanctionEntryFileImporterIntegrityTests
    {
        [Fact]
        public async Task AFileThatEndsInAReadFaultIsReportedAsNotCompletedAsync()
        {
            await using var stream = new FaultAfterFirstReadStream("1,Alpha Person,USA\n2,Bravo Person,USA\n"u8.ToArray());
            using var parser = new TextFieldParser(stream)
            {
                TextFieldType = FieldType.Delimited,
                Delimiters = [","]
            };

            var result = await SanctionEntryFileImporter.ImportAsync(parser, 1, "1", 0, 0,
                (_, _) => Task.CompletedTask, null, TestLog.NoOp);

            result.Completed.Should().BeFalse("a truncated download must never be allowed to delist the rest");
            result.Hashes.Should().NotBeEmpty("the entries read before the fault are still reported");
        }

        [Fact]
        public async Task AFileReadToTheEndIsReportedAsCompletedAsync()
        {
            using var parser = new TextFieldParser(new StringReader("1,Alpha Person,USA\n2,Bravo Person,USA\n"))
            {
                TextFieldType = FieldType.Delimited,
                Delimiters = [","]
            };

            var result = await SanctionEntryFileImporter.ImportAsync(parser, 1, "1", 0, 0,
                (_, _) => Task.CompletedTask, null, TestLog.NoOp);

            result.Completed.Should().BeTrue();
        }

        private sealed class FaultAfterFirstReadStream(byte[] content) : MemoryStream(content)
        {
            private bool served;

            public override int Read(byte[] buffer, int offset, int count)
            {
                ThrowWhenAlreadyServed();

                return base.Read(buffer, offset, count);
            }

            public override int Read(Span<byte> buffer)
            {
                ThrowWhenAlreadyServed();

                return base.Read(buffer);
            }

            private void ThrowWhenAlreadyServed()
            {
                if (served)
                {
                    throw new IOException("The connection was closed before the end of the list.");
                }

                served = true;
            }
        }
    }
}

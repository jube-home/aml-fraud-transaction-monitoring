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
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Jube.App.Wire;
using Xunit;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireGenerationTests
    {
        private static (int Code, List<string> Output, List<string> Errors) Run(params string[] args)
        {
            var output = new List<string>();
            var errors = new List<string>();

            return (WireGeneration.Run(args, output.Add, errors.Add), output, errors);
        }

        private static string Temporary()
        {
            return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        [Theory]
        [InlineData("--wire-generate", true)]
        [InlineData("--wire-check", true)]
        [InlineData("--urls", false)]
        public void OnlyTheTwoWireFlagsTakeTheApplicationOutOfServingMode(string flag, bool handled)
        {
            WireGeneration.Handles(["--environment", "Development", flag]).Should().Be(handled);
        }

        [Fact]
        public void GeneratingWritesBothFilesThenReportsThemCurrentAndLeavesThemUntouchedTheSecondTime()
        {
            var directory = Temporary();

            try
            {
                var first = Run("--wire-generate", directory);
                var stamp = File.GetLastWriteTimeUtc(Path.Combine(directory, WireTypeScriptGenerator.DecodersFile));

                first.Code.Should().Be(0);
                first.Output.Should().ContainSingle().Which.Should().Contain("wrote");
                File.Exists(Path.Combine(directory, WireTypeScriptGenerator.TypesFile)).Should().BeTrue();

                var second = Run("--wire-generate", directory);

                second.Code.Should().Be(0);
                second.Output.Should().ContainSingle().Which.Should().Contain("already current");
                File.GetLastWriteTimeUtc(Path.Combine(directory, WireTypeScriptGenerator.DecodersFile))
                    .Should().Be(stamp);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Fact]
        public void CheckingSucceedsOnCurrentFilesAndFailsWithTheStaleNamesWhenOneIsEdited()
        {
            var directory = Temporary();

            try
            {
                Run("--wire-generate", directory);

                Run("--wire-check", directory).Code.Should().Be(0);

                File.AppendAllText(Path.Combine(directory, WireTypeScriptGenerator.DecodersFile), "// edited\n");

                var stale = Run("--wire-check", directory);

                stale.Code.Should().Be(1);
                stale.Errors.Should().ContainSingle().Which.Should().Contain(WireTypeScriptGenerator.DecodersFile);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }
    }
}

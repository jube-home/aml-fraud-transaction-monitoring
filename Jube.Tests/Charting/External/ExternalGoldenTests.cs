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
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Jube.Test.Charting.External
{
    public class ExternalGoldenTests
    {
        private const string ResourcePrefix = "Jube.Test.Charting.External.Golden.";

        public static IEnumerable<object[]> Names()
        {
            return ExternalSamples.GoldenNames().Select(k => new object[] { k });
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void SnapshotMatchesTheApprovedSvg(string name)
        {
            var actual = ExternalSamples.RenderExternal(name);
            var directory = Environment.GetEnvironmentVariable(ChartStylesheetTests.GenerateEnvironmentVariable);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, name + ".svg"), actual);

                return;
            }

            using var stream =
                typeof(ExternalGoldenTests).Assembly.GetManifestResourceStream(ResourcePrefix + name + ".svg");

            stream.Should().NotBeNull("the approved snapshot " + name + ".svg must exist; regenerate with " +
                                      ChartStylesheetTests.GenerateEnvironmentVariable);

            using var reader = new StreamReader(stream!);
            actual.Should().Be(reader.ReadToEnd());
        }

        [Fact]
        public void EverySnapshotResourceHasASample()
        {
            var resources = typeof(ExternalGoldenTests).Assembly.GetManifestResourceNames()
                .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal) &&
                            n.EndsWith(".svg", StringComparison.Ordinal))
                .Select(n => n[ResourcePrefix.Length..^".svg".Length]);

            resources.Should().BeEquivalentTo(ExternalSamples.GoldenNames());
        }
    }
}
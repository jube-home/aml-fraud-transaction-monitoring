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

namespace Jube.Test.Charting.Diagrams
{
    public class DiagramGoldenTests
    {
        private const string GenerateEnvironmentVariable = "DIAGRAM_GOLDEN_GENERATE_DIR";
        private const string Prefix = "Jube.Test.Charting.Diagrams.Golden.";

        public static IEnumerable<object[]> Names()
        {
            return DiagramSamples.All().Keys.Select(k => new object[] { k });
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void SnapshotMatchesTheApprovedSvg(string name)
        {
            var actual = DiagramSamples.All()[name].Render();
            var directory = Environment.GetEnvironmentVariable(GenerateEnvironmentVariable);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, name + ".svg"), actual);

                return;
            }

            using var stream = typeof(DiagramGoldenTests).Assembly.GetManifestResourceStream(Prefix + name + ".svg");

            stream.Should().NotBeNull("the approved snapshot " + name + ".svg must exist; regenerate with " +
                                      GenerateEnvironmentVariable);

            using var reader = new StreamReader(stream!);
            actual.Should().Be(reader.ReadToEnd());
        }

        [Fact]
        public void EverySnapshotResourceHasASample()
        {
            var resources = typeof(DiagramGoldenTests).Assembly.GetManifestResourceNames()
                .Where(n => n.StartsWith(Prefix, StringComparison.Ordinal))
                .Select(n => n[Prefix.Length..^".svg".Length]);

            resources.Should().BeEquivalentTo(DiagramSamples.All().Keys);
        }
    }
}
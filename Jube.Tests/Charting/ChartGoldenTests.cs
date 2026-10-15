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

namespace Jube.Test.Charting
{
    public class ChartGoldenTests
    {
        private const string GenerateEnvironmentVariable = "CHARTING_GOLDEN_GENERATE_DIR";

        public static IEnumerable<object[]> Names()
        {
            return ChartSamples.All().Keys.Select(k => new object[] { k });
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void SnapshotMatchesTheApprovedSvg(string name)
        {
            var actual = ChartSamples.All()[name].Render();
            var directory = Environment.GetEnvironmentVariable(GenerateEnvironmentVariable);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, name + ".svg"), actual);

                return;
            }

            var resource = "Jube.Test.Charting.Golden." + name + ".svg";
            using var stream = typeof(ChartGoldenTests).Assembly.GetManifestResourceStream(resource);

            stream.Should().NotBeNull("the approved snapshot " + name + ".svg must exist; regenerate with " +
                                      GenerateEnvironmentVariable);

            using var reader = new StreamReader(stream!);
            var expected = reader.ReadToEnd();
            actual.Should().Be(expected);
        }

        [Fact]
        public void EverySnapshotResourceHasASample()
        {
            var resources = typeof(ChartGoldenTests).Assembly.GetManifestResourceNames()
                .Where(n => n.StartsWith("Jube.Test.Charting.Golden.", StringComparison.Ordinal))
                .Select(n => n["Jube.Test.Charting.Golden.".Length..^".svg".Length]);

            resources.Should().BeEquivalentTo(ChartSamples.All().Keys);
        }
    }
}
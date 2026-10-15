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
using Jube.Service.ModelDiagrams;
using Xunit;

namespace Jube.Test.ModelDiagrams
{
    public class ModelDiagramGoldenTests
    {
        private const string GenerateEnvironmentVariable = "MODEL_DIAGRAM_GOLDEN_GENERATE_DIR";
        private const string Prefix = "Jube.Test.ModelDiagrams.Golden.";

        private static readonly string[] names =
            ["amount", "velocity", "merchant_risk", "account_age", "geo_mismatch", "night_time"];

        private static readonly IReadOnlyDictionary<string, string> samples =
            new SortedDictionary<string, string>(
                ModelDiagramFixtures.TrainedSnapshotKinds.Select(ModelDiagramFixtures.SnapshotFixtureName)
                    .ToDictionary(n => n, n => n), StringComparer.Ordinal)
            {
                ["network"] = "topology:Sigmoid2",
                ["snapshot-logistic"] = "snapshot-logistic-pipeline-evidence",
                ["bayes-tan"] = "bayes-tan",
                ["bayes-kdb"] = "bayes-kdb",
                ["bayes-aode"] = "bayes-aode-weighted",
                ["bayes-hnb"] = "bayes-hnb",
                ["bayes-latent-tree"] = "bayes-latent-tree",
                ["naive-bayes-gaussian"] = "naive-bayes-gaussian",
                ["regression-logistic"] = "regression-logistic",
                ["regression-linear"] = "regression-linear",
                ["tree"] = "tree",
                ["forest"] = "forest",
                ["hmm"] = "hmm-discrete",
                ["kmeans"] = "kmeans",
                ["pca"] = "pca"
            };

        public static IEnumerable<object[]> SampleNames()
        {
            return samples.Keys.Select(k => new object[] { k });
        }

        public static string Render(string sample)
        {
            var fixture = samples[sample];
            var options = new ModelDiagramOptions();
            var stored = ModelDiagramFixtures.Stored.SingleOrDefault(c => c.Name == fixture);

            if (stored is not null && !fixture.StartsWith("snapshot", StringComparison.Ordinal) && sample != "hmm")
            {
                options = options with { VariableNames = [.. names.Take(stored.Variables)] };
            }

            if (sample == "hmm")
            {
                options = options with { VariableNames = ["Normal", "Watch", "Compromised"] };
            }

            return ModelDiagramRenderer.Render(ModelDiagramFixtures.Read(fixture), options);
        }

        [Theory]
        [MemberData(nameof(SampleNames))]
        public void SnapshotMatchesTheApprovedSvg(string name)
        {
            var actual = Render(name);
            var directory = Environment.GetEnvironmentVariable(GenerateEnvironmentVariable);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, name + ".svg"), actual);

                return;
            }

            using var stream =
                typeof(ModelDiagramGoldenTests).Assembly.GetManifestResourceStream(Prefix + name + ".svg");

            stream.Should().NotBeNull("the approved snapshot " + name + ".svg must exist; regenerate with " +
                                      GenerateEnvironmentVariable);

            using var reader = new StreamReader(stream!);
            actual.Should().Be(reader.ReadToEnd());
        }

        [Fact]
        public void EverySnapshotResourceHasASample()
        {
            var resources = typeof(ModelDiagramGoldenTests).Assembly.GetManifestResourceNames()
                .Where(n => n.StartsWith(Prefix, StringComparison.Ordinal))
                .Select(n => n[Prefix.Length..^".svg".Length]);

            resources.Should().BeEquivalentTo(samples.Keys);
        }

        [Fact]
        public void EveryAdapterHasASnapshotSample()
        {
            var adapters = samples.Values.Select(ModelDiagramFixtures.ExpectedAdapter).Distinct();

            adapters.Should().BeEquivalentTo(ModelDiagramAdapters.Default.Select(a => a.Name));
        }
    }
}
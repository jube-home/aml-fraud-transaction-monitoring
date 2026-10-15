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
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Test.ModelDiagrams
{
    public sealed record FixtureCase(string Name, string Adapter, int Variables);

    public static class ModelDiagramFixtures
    {
        private const string FixturePrefix = "Jube.Test.ModelDiagrams.Fixtures.";
        private const string TopologyResource = "Jube.Test.NetworkGolden.topologies.json";

        public static readonly string[] TopologyNames =
            ["Sigmoid2", "BipolarSigmoid1", "Gaussian", "RectifiedLinear", "Linear1", "Identity"];

        public static readonly string[] TrainedSnapshotKinds =
        [
            "AdaBoostClassifierPipeline", "BaggingClassifierPipeline", "BalancedKMeansClustererPipeline",
            "BinarySplitClustererPipeline", "DecisionTreeClassifierPipeline", "DecisionTreeRegressorPipeline",
            "GaussianMixtureClustererPipeline", "GaussianMixtureDensityPipeline",
            "GradientBoostingClassifierPipeline", "GradientBoostingRegressorPipeline",
            "IsolationForestAnomalyPipeline", "KernelRidgeRegressorPipeline",
            "KernelSupportVectorClassifierPipeline", "KMedoidsClustererPipeline", "KModesClustererPipeline",
            "KNearestNeighboursClassifierPipeline", "KNearestNeighboursRegressorPipeline",
            "LinearSupportVectorClassifierPipeline", "MahalanobisAnomalyPipeline", "MeanShiftClustererPipeline",
            "MiniBatchKMeansClustererPipeline", "MultipleLinearRegressionPipeline", "NaiveBayesClassifierPipeline",
            "NeuralNetworkClassifierPipeline", "NeuralNetworkRegressorPipeline",
            "OneClassSupportVectorAnomalyPipeline", "RandomForestClassifierPipeline",
            "RandomForestRegressorPipeline", "TreeAugmentedNaiveBayesClassifierPipeline"
        ];

        public static IReadOnlyList<FixtureCase> Stored { get; } =
        [
            .. TrainedSnapshotKinds.Select(k => new FixtureCase(SnapshotFixtureName(k), "SnapshotEnvelope", 4)),
            new("bayes-tan", "TreeAugmentedNaiveBayes", 4),
            new("bayes-kdb", "KDependenceBayes", 4),
            new("bayes-aode", "AveragedOneDependence", 4),
            new("bayes-aode-weighted", "AveragedOneDependence", 4),
            new("bayes-hnb", "HiddenNaiveBayes", 4),
            new("bayes-latent-tree", "LatentTree", 6),
            new("naive-bayes-gaussian", "GaussianNaiveBayes", 4),
            new("regression-linear", "LinearCoefficients", 4),
            new("regression-logistic", "LinearCoefficients", 4),
            new("tree", "DecisionTree", 4),
            new("forest", "RandomForest", 4),
            new("hmm-discrete", "HiddenMarkov", 3),
            new("hmm-normal", "HiddenMarkov", 2),
            new("kmeans", "KMeans", 4),
            new("pca", "PrincipalComponents", 4),
            new("snapshot-logistic-pipeline", "SnapshotEnvelope", 3),
            new("snapshot-logistic-pipeline-evidence", "SnapshotEnvelope", 3)
        ];

        public static IEnumerable<object[]> StoredData()
        {
            return Stored.Select(c => new object[] { c.Name });
        }

        public static IEnumerable<object[]> TopologyData()
        {
            return TopologyNames.Select(n => new object[] { n });
        }

        public static IEnumerable<object[]> EveryFixtureData()
        {
            return Stored.Select(c => new object[] { c.Name })
                .Concat(TopologyNames.Select(n => new object[] { "topology:" + n }));
        }

        public static string Read(string name)
        {
            if (name.StartsWith("topology:", StringComparison.Ordinal))
            {
                return Topology(name["topology:".Length..]);
            }

            using var stream =
                typeof(ModelDiagramFixtures).Assembly.GetManifestResourceStream(FixturePrefix + name + ".json");

            if (stream is null)
            {
                throw new InvalidOperationException("Missing fixture " + name);
            }

            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        public static string Topology(string name)
        {
            using var stream = typeof(ModelDiagramFixtures).Assembly.GetManifestResourceStream(TopologyResource);
            using var reader = new StreamReader(stream!);

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd())![name];
        }

        public static JObject Parsed(string name)
        {
            return JObject.Parse(Read(name));
        }

        public static string ExpectedAdapter(string name)
        {
            return name.StartsWith("topology:", StringComparison.Ordinal)
                ? "NetworkTopology"
                : Stored.Single(c => c.Name == name).Adapter;
        }

        public static void HostileRename(JToken token, string hostile)
        {
            foreach (var property in token.SelectTokens("$..*").OfType<JProperty>().ToArray())
            {
                if (property.Name == "name" && property.Value.Type == JTokenType.String)
                {
                    property.Value = hostile;
                }
                else if (property.Name is "variablesInOrder" or "classNames" && property.Value is JArray array)
                {
                    for (var i = 0; i < array.Count; i++)
                    {
                        array[i] = hostile + i;
                    }
                }
            }
        }

        public static string SnapshotFixtureName(string kind)
        {
            var stem = kind.EndsWith("Pipeline", StringComparison.Ordinal) ? kind[..^"Pipeline".Length] : kind;

            var words = Regex.Replace(stem,
                "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", "-");

            return "snapshot-" + words.ToLowerInvariant();
        }

        public static IReadOnlyList<string> Names(int count, string prefix = "v")
        {
            return [.. Enumerable.Range(1, count).Select(i => prefix + i)];
        }
    }
}
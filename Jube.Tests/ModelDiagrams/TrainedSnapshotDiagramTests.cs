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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Anomaly;
using Jube.Engine.AdvancedAnalytics.Classification;
using Jube.Engine.AdvancedAnalytics.Clustering;
using Jube.Engine.AdvancedAnalytics.Density;
using Jube.Engine.AdvancedAnalytics.DimensionReduction;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Service.ModelDiagrams;
using Jube.Service.ModelDiagrams.Adapters;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.ModelDiagrams
{
    public class TrainedSnapshotDiagramTests
    {
        private const string GenerateEnvironmentVariable = "MODEL_DIAGRAM_FIXTURE_GENERATE_DIR";
        private static readonly string[] names = ["amount", "velocity", "merchant_risk", "account_age"];
        private static readonly ConcurrentDictionary<string, string> cache = new();

        private static readonly IReadOnlyDictionary<string, Func<string>> trainers =
            new SortedDictionary<string, Func<string>>(StringComparer.Ordinal)
            {
                ["LogisticRegressionPipeline"] = () =>
                    LogisticRegressionTrainer.Train(Inputs(), Labels(), names).SnapshotJson,
                ["MultipleLinearRegressionPipeline"] = () =>
                    LinearRegressionTrainer.Train(Inputs(), Outputs(), names).SnapshotJson,
                ["LinearSupportVectorClassifierPipeline"] = () =>
                    LinearSupportVectorClassifierTrainer.Train(Inputs(), Labels(), names).SnapshotJson,
                ["MahalanobisAnomalyPipeline"] = () =>
                    MahalanobisAnomalyTrainer.Train(Inputs(), null, names).SnapshotJson,
                ["IsolationForestAnomalyPipeline"] = () => IsolationForestAnomalyTrainer.Train(Inputs(), null, names,
                    new IsolationForestAnomalyOptions { TreeCount = 8, SubsampleSize = 32 }).SnapshotJson,
                ["OneClassSupportVectorAnomalyPipeline"] = () =>
                    OneClassSupportVectorAnomalyTrainer.Train(Inputs(), null, names).SnapshotJson,
                ["NaiveBayesClassifierPipeline"] = () =>
                    NaiveBayesClassifierTrainer.Train(Inputs(), Labels(), names).SnapshotJson,
                ["TreeAugmentedNaiveBayesClassifierPipeline"] = () =>
                    TreeAugmentedNaiveBayesClassifierTrainer.Train(Inputs(), Labels(), names).SnapshotJson,
                ["DecisionTreeClassifierPipeline"] = () => DecisionTreeClassifierTrainer.Train(Inputs(), Labels(),
                    names, new DecisionTreeClassifierOptions { MaxDepth = 3 }).SnapshotJson,
                ["DecisionTreeRegressorPipeline"] = () => DecisionTreeRegressorTrainer.Train(Inputs(), Outputs(),
                    names, new DecisionTreeRegressorOptions { MaxDepth = 3 }).SnapshotJson,
                ["RandomForestClassifierPipeline"] = () => RandomForestClassifierTrainer.Train(Inputs(), Labels(),
                    names, new RandomForestClassifierOptions { TreeCount = 8, MaxDepth = 3 }).SnapshotJson,
                ["RandomForestRegressorPipeline"] = () => RandomForestRegressorTrainer.Train(Inputs(), Outputs(),
                    names, new RandomForestRegressorOptions { TreeCount = 8, MaxDepth = 3 }).SnapshotJson,
                ["GradientBoostingClassifierPipeline"] = () => GradientBoostingClassifierTrainer.Train(Inputs(),
                    Labels(), names, new GradientBoostingClassifierOptions { Rounds = 8, MaxDepth = 2 }).SnapshotJson,
                ["GradientBoostingRegressorPipeline"] = () => GradientBoostingRegressorTrainer.Train(Inputs(),
                    Outputs(), names, new GradientBoostingRegressorOptions { Rounds = 8, MaxDepth = 2 }).SnapshotJson,
                ["AdaBoostClassifierPipeline"] = () => AdaBoostClassifierTrainer.Train(Inputs(), Labels(), names,
                    new AdaBoostClassifierOptions { Rounds = 8, MaxDepth = 2 }).SnapshotJson,
                ["BaggingClassifierPipeline"] = () => BaggingClassifierTrainer.Train(Inputs(), Labels(), names,
                    new BaggingClassifierOptions { TreeCount = 8, MaxDepth = 3 }).SnapshotJson,
                ["KNearestNeighboursClassifierPipeline"] = () =>
                    KNearestNeighboursClassifierTrainer.Train(Inputs(), Labels(), names).SnapshotJson,
                ["KNearestNeighboursRegressorPipeline"] = () =>
                    KNearestNeighboursRegressorTrainer.Train(Inputs(), Outputs(), names).SnapshotJson,
                ["KernelSupportVectorClassifierPipeline"] = () =>
                    KernelSupportVectorClassifierTrainer.Train(Inputs(), Labels(), names).SnapshotJson,
                ["KernelRidgeRegressorPipeline"] = () =>
                    KernelRidgeRegressorTrainer.Train(Inputs(), Outputs(), names).SnapshotJson,
                ["NeuralNetworkClassifierPipeline"] = () => NeuralNetworkClassifierTrainer.Train(Inputs(), Labels(),
                    names, new NeuralNetworkClassifierOptions { HiddenUnits = 3, Epochs = 20 }).SnapshotJson,
                ["NeuralNetworkRegressorPipeline"] = () => NeuralNetworkRegressorTrainer.Train(Inputs(), Outputs(),
                    names, new NeuralNetworkRegressorOptions { HiddenUnits = 3, Epochs = 20 }).SnapshotJson,
                ["KMeansClustererPipeline"] = () =>
                    KMeansClustererTrainer.Train(Blobs(), names, new KMeansClustererOptions { Clusters = 3 })
                        .SnapshotJson,
                ["BalancedKMeansClustererPipeline"] = () => BalancedKMeansClustererTrainer.Train(Blobs(), names,
                    new BalancedKMeansClustererOptions { Clusters = 3 }).SnapshotJson,
                ["BinarySplitClustererPipeline"] = () => BinarySplitClustererTrainer.Train(Blobs(), names,
                    new BinarySplitClustererOptions { Clusters = 3 }).SnapshotJson,
                ["MiniBatchKMeansClustererPipeline"] = () => MiniBatchKMeansClustererTrainer.Train(Blobs(), names,
                    new MiniBatchKMeansClustererOptions { Clusters = 3 }).SnapshotJson,
                ["KMedoidsClustererPipeline"] = () => KMedoidsClustererTrainer.Train(Blobs(), names,
                    new KMedoidsClustererOptions { Clusters = 3 }).SnapshotJson,
                ["KModesClustererPipeline"] = () => KModesClustererTrainer.Train(Categorical(), names,
                    new KModesClustererOptions { Clusters = 3 }).SnapshotJson,
                ["MeanShiftClustererPipeline"] = () =>
                    MeanShiftClustererTrainer.Train(Blobs(), names, new MeanShiftClustererOptions { Bandwidth = 0.7 })
                        .SnapshotJson,
                ["GaussianMixtureClustererPipeline"] = () => GaussianMixtureClustererTrainer.Train(Blobs(), names,
                    new GaussianMixtureClustererOptions { Components = 3 }).SnapshotJson,
                ["GaussianMixtureDensityPipeline"] = () =>
                    GaussianMixtureDensityTrainer.Train(Blobs(), names).SnapshotJson,
                ["PrincipalComponentPipeline"] = () =>
                    PrincipalComponentTrainer.Train(Inputs(), names).SnapshotJson
            };

        private static IReadOnlyList<string> EngineKinds()
        {
            return
            [
                .. typeof(MahalanobisAnomalyRecall).Assembly.GetTypes()
                    .Select(t => t.GetField("ModelKind", BindingFlags.Public | BindingFlags.Static))
                    .Where(f => f is { IsLiteral: true } && f.FieldType == typeof(string))
                    .Select(f => (string)f!.GetRawConstantValue()!)
                    .Distinct().Order(StringComparer.Ordinal)
            ];
        }

        public static IEnumerable<object[]> TrainableKinds()
        {
            return trainers.Keys.Select(k => new object[] { k });
        }

        public static string Snapshot(string kind)
        {
            return cache.GetOrAdd(kind, k => trainers[k]());
        }

        [Fact]
        public void EveryEngineModelKindHasARegisteredDiagram()
        {
            var registered = SnapshotKindAdapters.Default.Select(a => a.Kind).ToHashSet(StringComparer.Ordinal);

            var kinds = EngineKinds();

            kinds.Should().HaveCountGreaterThan(30);
            kinds.Where(k => !registered.Contains(k)).Should().BeEmpty();
        }

        [Fact]
        public void EveryRegisteredKindIsUniqueAndEveryPipelineKindIsTrainedHere()
        {
            var registered = SnapshotKindAdapters.Default.Select(a => a.Kind).ToArray();

            registered.Should().OnlyHaveUniqueItems();

            EngineKinds().Where(k => k.EndsWith("Pipeline", StringComparison.Ordinal))
                .Should().BeEquivalentTo(trainers.Keys);
        }

        [Theory]
        [MemberData(nameof(TrainableKinds))]
        public void ARealTrainedSnapshotOfEveryKindRendersADiagram(string kind)
        {
            var json = Snapshot(kind);
            var directory = Environment.GetEnvironmentVariable(GenerateEnvironmentVariable);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);

                File.WriteAllText(Path.Combine(directory, ModelDiagramFixtures.SnapshotFixtureName(kind) + ".json"),
                    json);
            }

            JObject.Parse(json).Value<string>("kind").Should().Be(kind);
            ModelDiagramRenderer.DetectedAdapter(json).Should().Be("SnapshotEnvelope");

            var svg = ModelDiagramRenderer.Render(json);

            svg.Should().StartWith("<svg").And.Contain("</svg>");
            svg.Should().NotContain("NaN").And.NotContain("Infinity");
        }

        private static double[][] Inputs()
        {
            var random = new Random(17);

            return
            [
                .. Enumerable.Range(0, 240).Select(_ =>
                {
                    var a = Normal(random);
                    var b = 0.6 * a + 0.8 * Normal(random);
                    var c = Normal(random);
                    var d = 50 + 20 * Normal(random) - 5 * c;

                    return new[] { 100 + 40 * a, b, c, d };
                })
            ];
        }

        private static int[] Labels()
        {
            var random = new Random(23);

            return
            [
                .. Inputs().Select(r =>
                {
                    var z = 0.03 * (r[0] - 100) + 0.9 * r[1] - 0.7 * r[2] - 0.02 * (r[3] - 50);

                    return random.NextDouble() < 1 / (1 + Math.Exp(-z)) ? 1 : 0;
                })
            ];
        }

        private static double[] Outputs()
        {
            var random = new Random(29);

            return [.. Inputs().Select(r => 2 + 0.05 * r[0] + 3 * r[1] - 1.5 * r[2] + 0.5 * Normal(random))];
        }

        private static double[][] Blobs()
        {
            var random = new Random(31);
            double[][] centres = [[0, 0, 0, 0], [5, 5, 0, -3], [-4, 3, 6, 2]];

            return
            [
                .. Enumerable.Range(0, 240).Select(i =>
                    centres[i % 3].Select(c => c + 0.8 * Normal(random)).ToArray())
            ];
        }

        private static double[][] Categorical()
        {
            var random = new Random(37);

            return
            [
                .. Enumerable.Range(0, 180).Select(i =>
                {
                    var group = i % 3 + 1;

                    return Enumerable.Range(0, names.Length)
                        .Select(f => f == 2 ? group == 3 ? 2 : 1 : f == 3 ? 4 : group)
                        .Select(v => random.NextDouble() < 0.85 ? v : random.Next(1, 5)).Select(v => (double)v)
                        .ToArray();
                })
            ];
        }

        private static double Normal(Random random)
        {
            var u = 1 - random.NextDouble();
            var v = random.NextDouble();

            return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * v);
        }
    }
}
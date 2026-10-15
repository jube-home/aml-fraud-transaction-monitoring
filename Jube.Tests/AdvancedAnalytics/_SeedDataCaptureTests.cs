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
using System.Linq;
using Jube.Engine.AdvancedAnalytics.Anomaly;
using Jube.Engine.AdvancedAnalytics.Classification;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Regression;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test.AdvancedAnalytics
{
    public class SeedDataCaptureTests
    {
        private const string OutputDirectory =
            "/tmp/claude-1000/-home-richard-RiderProjects-aml-fraud-transaction-monitoring/" +
            "9f71858e-04f8-4145-9f78-ba12ca83c297/scratchpad/aa-seed";

        private static readonly string[] names = ["Amount", "RiskScore", "TenureDays", "AccountAge"];

        private static double Normal(Random random)
        {
            var u = 1 - random.NextDouble();
            var v = random.NextDouble();

            return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * v);
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

        private static void Capture<TDetails>(string kind, string snapshotJson, string resultKind, int resultSeed,
            string[] resultVariables, AdvancedAnalyticsSplitSizes splitSizes,
            AdvancedAnalyticsVariablePreprocessing[] preprocessing, AdvancedAnalyticsWarning[] warnings,
            TDetails details) where TDetails : IAdvancedAnalyticsEvidenceDetails
        {
            var evidence = new AdvancedAnalyticsEvidence<TDetails>
            {
                Kind = resultKind,
                Seed = resultSeed,
                Variables = resultVariables,
                SplitSizes = splitSizes,
                Preprocessing = preprocessing,
                Warnings = warnings,
                Details = details
            };

            var metricsJson = AdvancedAnalyticsEvidenceJson.Serialise(evidence);

            var variablesJson = JsonConvert.SerializeObject(
                resultVariables.Select(n => new { Name = n, ProcessingTypeId = 1 }));

            Directory.CreateDirectory(OutputDirectory);

            File.WriteAllText(Path.Combine(OutputDirectory, kind + ".json"), JsonConvert.SerializeObject(new
            {
                modelJson = snapshotJson,
                variablesJson,
                metricsJson
            }));
        }

        [Fact]
        public void CaptureDecisionTreeRegressor()
        {
            var r = DecisionTreeRegressorTrainer.Train(Inputs(), Outputs(), names,
                new DecisionTreeRegressorOptions { MaxDepth = 3 });

            Capture("DecisionTreeRegressorPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables, r.SplitSizes,
                r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureKNearestNeighboursRegressor()
        {
            var r = KNearestNeighboursRegressorTrainer.Train(Inputs(), Outputs(), names);

            Capture("KNearestNeighboursRegressorPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables, r.SplitSizes,
                r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureRandomForestRegressor()
        {
            var r = RandomForestRegressorTrainer.Train(Inputs(), Outputs(), names,
                new RandomForestRegressorOptions { TreeCount = 8, MaxDepth = 3 });

            Capture("RandomForestRegressorPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables, r.SplitSizes,
                r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureKernelRidgeRegressor()
        {
            var r = KernelRidgeRegressorTrainer.Train(Inputs(), Outputs(), names);

            Capture("KernelRidgeRegressorPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables, r.SplitSizes,
                r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureNeuralNetworkRegressor()
        {
            var r = NeuralNetworkRegressorTrainer.Train(Inputs(), Outputs(), names,
                new NeuralNetworkRegressorOptions { HiddenUnits = 3, Epochs = 20 });

            Capture("NeuralNetworkRegressorPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables, r.SplitSizes,
                r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureGradientBoostingRegressor()
        {
            var r = GradientBoostingRegressorTrainer.Train(Inputs(), Outputs(), names,
                new GradientBoostingRegressorOptions { Rounds = 8, MaxDepth = 2 });

            Capture("GradientBoostingRegressorPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables, r.SplitSizes,
                r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureNeuralNetworkClassifier()
        {
            var r = NeuralNetworkClassifierTrainer.Train(Inputs(), Labels(), names,
                new NeuralNetworkClassifierOptions { HiddenUnits = 3, Epochs = 20 });

            Capture("NeuralNetworkClassifierPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables, r.SplitSizes,
                r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureRandomForestClassifier()
        {
            var r = RandomForestClassifierTrainer.Train(Inputs(), Labels(), names,
                new RandomForestClassifierOptions { TreeCount = 8, MaxDepth = 3 });

            Capture("RandomForestClassifierPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables, r.SplitSizes,
                r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureDecisionTreeClassifier()
        {
            var r = DecisionTreeClassifierTrainer.Train(Inputs(), Labels(), names,
                new DecisionTreeClassifierOptions { MaxDepth = 3 });

            Capture("DecisionTreeClassifierPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables, r.SplitSizes,
                r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureNaiveBayesClassifier()
        {
            var r = NaiveBayesClassifierTrainer.Train(Inputs(), Labels(), names);

            Capture("NaiveBayesClassifierPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables, r.SplitSizes,
                r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureTreeAugmentedNaiveBayesClassifier()
        {
            var r = TreeAugmentedNaiveBayesClassifierTrainer.Train(Inputs(), Labels(), names);

            Capture("TreeAugmentedNaiveBayesClassifierPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables,
                r.SplitSizes, r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureKNearestNeighboursClassifier()
        {
            var r = KNearestNeighboursClassifierTrainer.Train(Inputs(), Labels(), names);

            Capture("KNearestNeighboursClassifierPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables,
                r.SplitSizes, r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureBaggingClassifier()
        {
            var r = BaggingClassifierTrainer.Train(Inputs(), Labels(), names,
                new BaggingClassifierOptions { TreeCount = 8, MaxDepth = 3 });

            Capture("BaggingClassifierPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables, r.SplitSizes,
                r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureAdaBoostClassifier()
        {
            var r = AdaBoostClassifierTrainer.Train(Inputs(), Labels(), names,
                new AdaBoostClassifierOptions { Rounds = 8, MaxDepth = 2 });

            Capture("AdaBoostClassifierPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables, r.SplitSizes,
                r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureGradientBoostingClassifier()
        {
            var r = GradientBoostingClassifierTrainer.Train(Inputs(), Labels(), names,
                new GradientBoostingClassifierOptions { Rounds = 8, MaxDepth = 2 });

            Capture("GradientBoostingClassifierPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables, r.SplitSizes,
                r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureLinearSupportVectorClassifier()
        {
            var r = LinearSupportVectorClassifierTrainer.Train(Inputs(), Labels(), names);

            Capture("LinearSupportVectorClassifierPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables,
                r.SplitSizes, r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureKernelSupportVectorClassifier()
        {
            var r = KernelSupportVectorClassifierTrainer.Train(Inputs(), Labels(), names);

            Capture("KernelSupportVectorClassifierPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables,
                r.SplitSizes, r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureIsolationForestAnomaly()
        {
            var r = IsolationForestAnomalyTrainer.Train(Inputs(), null, names,
                new IsolationForestAnomalyOptions { TreeCount = 8, SubsampleSize = 32 });

            Capture("IsolationForestAnomalyPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables, r.SplitSizes,
                r.Preprocessing, r.Warnings, r.Details);
        }

        [Fact]
        public void CaptureOneClassSupportVectorAnomaly()
        {
            var r = OneClassSupportVectorAnomalyTrainer.Train(Inputs(), null, names);

            Capture("OneClassSupportVectorAnomalyPipeline", r.SnapshotJson, r.Kind, r.Seed, r.Variables,
                r.SplitSizes, r.Preprocessing, r.Warnings, r.Details);
        }
    }
}
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Neuro;
using FluentAssertions;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Data.Poco;
using Jube.Data.Repository.Models;
using Jube.Engine.Exhaustive;
using Jube.Engine.Exhaustive.Algorithms;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Exhaustive.Calibration;
using Jube.Engine.Exhaustive.Calibration.Models;
using Jube.Engine.Exhaustive.Promotion;
using Jube.Engine.Helpers;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Unit")]
    public sealed class ExhaustiveCalibrationConceptTests(ITestOutputHelper output)
    {
        private const double BaseRate = 0.02;

        private static ExhaustiveTrainingSettings Settings()
        {
            return new ExhaustiveTrainingSettings
            {
                TrialsLimit = 8,
                MinVariableCount = 6,
                MaxVariableCount = 20,
                TrainingDataSamplePercentage = 0.6,
                CrossValidationDataSamplePercentage = 0.2,
                TestingDataSamplePercentage = 0.2,
                ValidationTestingActivationThreshold = 0.5,
                TopologySinceImprovementLimit = 2,
                LayerDepthLimit = 4,
                LayerWidthLimitInputLayerFactor = 4,
                TopologyComplexityLimit = 10000,
                ActivationFunctionExplorationEpochs = 8,
                TopologyExplorationEpochs = 4,
                TopologyFinalisationEpochs = 8,
                SimulationsCount = 10
            };
        }

        private static async Task<Run> TrainAsync(int seed)
        {
            Accord.Math.Random.Generator.ThreadSeed = seed;
            var (header, allRows) = TrainSupport.ReadMock();
            var random = new Random(seed);
            var rows = allRows.OrderBy(_ => random.Next()).ToList();
            const int variableCount = 26;

            var variables = new Dictionary<int, Variable>();

            for (var i = 0; i < variableCount; i++)
            {
                variables[i] = new Variable
                {
                    Name = header[i + 1], ProcessingTypeId = 1, ExhaustiveSearchInstanceVariableId = i + 1
                };
            }

            var data = rows.Select(r => r.Skip(1).Take(variableCount).ToArray()).ToArray();
            var outputs = rows.Select(r => r[0]).ToArray();

            var training = TrainSupport.NewTraining(seed);

            await training.CalculateStatisticsAsync(1, variables, data, StatisticsTarget.Population,
                new TrainRecordingRepository(), new TrainRecordingRepository(), CancellationToken.None);

            data = training.NormaliseData(variables, data);

            var half = data.Length / 2;
            var developmentData = data.Take(half).ToArray();
            var developmentOutputs = outputs.Take(half).ToArray();
            training.DatasetSymmetry(developmentData, developmentOutputs, out developmentData, out developmentOutputs);

            var recorder = new TrainRecordingRecorder();

            var supervised = new Supervised(Settings(), recorder, 1, new JsonSerializationHelper(),
                new Random(seed), variables, developmentData, developmentOutputs, TrainSupport.Log,
                new BaseRateResolution(BaseRate, BaseRateResolution.SourceOverride));

            await supervised.TrainAsync();

            var promoted = recorder.Promoted.Last();
            var helper = new JsonSerializationHelper();

            var network = JsonConvert.DeserializeObject<ActivationNetwork>(promoted.Json,
                helper.DeserializeTopologyNetworkJsonSerializerSettings)!;

            var calibration = ExhaustiveCalibration.FromColumns(
                promoted.CalibrationMethodId?.ToString().ToLowerInvariant(),
                promoted.CalibrationInputId?.ToString().ToLowerInvariant(), promoted.CalibrationSlope,
                promoted.CalibrationIntercept, promoted.CalibrationEpsilon, promoted.CalibrationPrior,
                promoted.BaseRate, promoted.BaseRateSourceId?.ToString());

            var columns = recorder.Prescriptions.Skip(recorder.Prescriptions.Count - network.InputsCount)
                .Select(p => recorder.TrialVariables
                    .Single(v => v.Id == p.ExhaustiveSearchInstanceTrialInstanceVariableId)
                    .ExhaustiveSearchInstanceVariableId!.Value - 1).ToArray();

            var pool = data.Skip(half).Zip(outputs.Skip(half), (d, o) => (d, o)).ToList();
            var negatives = pool.Where(p => p.o < 0.5).ToList();

            var positives = pool.Where(p => p.o >= 0.5)
                .Take((int)Math.Round(negatives.Count * BaseRate / (1 - BaseRate))).ToList();

            var population = negatives.Concat(positives).ToList();

            var perPromotion = recorder.PredictedActual.Count / recorder.Promoted.Count;
            var lastTest = recorder.PredictedActual.Skip(recorder.PredictedActual.Count - perPromotion).ToList();

            return new Run(seed, promoted, calibration,
                [.. population.Select(p => network.Compute([.. columns.Select(c => p.d[c])])[0])],
                [.. population.Select(p => p.o >= 0.5 ? 1 : 0)],
                [.. developmentData.Select(r => columns.Select(c => r[c]).ToArray())],
                [.. developmentOutputs.Select(o => new[] { o })], network,
                [.. lastTest.Select(r => r.Predicted!.Value)],
                [.. lastTest.Select(r => r.Actual!.Value >= 0.5 ? 1 : 0)]);
        }

        private static async Task<List<Run>> CalibratedRunsAsync(int wanted, ITestOutputHelper output)
        {
            var runs = new List<Run>();

            for (var seed = 1; seed <= 14 && runs.Count < wanted; seed++)
            {
                var run = await TrainAsync(seed);

                output.WriteLine(
                    $"seed {seed}: status {run.Promoted.CalibrationStatusId} input {run.Promoted.CalibrationInputId} reason {run.Promoted.CalibrationReason}");

                if (run.Calibration.IsCalibrated)
                {
                    runs.Add(run);
                }
            }

            return runs;
        }

        [Fact]
        public async Task AtATwoPercentBaseRate_TheCalibratedMeanMatchesAndTheRawMeanDoesNotAsync()
        {
            var runs = await CalibratedRunsAsync(3, output);
            runs.Should().HaveCountGreaterOrEqualTo(3);

            foreach (var run in runs)
            {
                var actualRate = run.PoolLabels.Average();
                var raw = run.PoolScores.Select(s => Math.Min(Math.Max(s, 0), 1)).ToArray();
                var calibrated = run.PoolScores.Select(s => run.Calibration.Apply(s)).ToArray();

                output.WriteLine(
                    $"seed {run.Seed}: pool rows {raw.Length}, rate {actualRate:0.0000}, raw mean {raw.Average():0.0000}, calibrated mean {calibrated.Average():0.0000}, slope {run.Promoted.CalibrationSlope:0.000}, prior {run.Promoted.CalibrationPrior:0.000}");

                Math.Abs(calibrated.Average() - actualRate).Should().BeLessThan(0.03);
                Math.Abs(raw.Average() - actualRate).Should().BeGreaterThan(0.12);
            }
        }

        [Fact]
        public async Task
            AtATwoPercentBaseRate_EceBrierAndLogLossImproveOnTheDeploymentPopulation_AndAucIsUnchangedAsync()
        {
            var runs = await CalibratedRunsAsync(3, output);
            runs.Should().HaveCountGreaterOrEqualTo(3);

            foreach (var run in runs)
            {
                var raw = run.PoolScores.Select(s => Math.Min(Math.Max(s, 0), 1)).ToArray();
                var calibrated = run.PoolScores.Select(s => run.Calibration.Apply(s)).ToArray();
                var labels = run.PoolLabels;

                var ece = (BinaryMetrics.ExpectedCalibrationError(raw, labels),
                    BinaryMetrics.ExpectedCalibrationError(calibrated, labels));
                var brier = (BinaryMetrics.BrierScore(raw, labels), BinaryMetrics.BrierScore(calibrated, labels));
                var logLoss = (BinaryMetrics.LogLoss(raw, labels), BinaryMetrics.LogLoss(calibrated, labels));
                var aucRaw = BinaryMetrics.RocAuc(run.PoolScores, labels);
                var aucCalibrated = BinaryMetrics.RocAuc(calibrated, labels);

                output.WriteLine(
                    $"seed {run.Seed}: ECE {ece.Item1:0.0000} -> {ece.Item2:0.0000}; Brier {brier.Item1:0.0000} -> {brier.Item2:0.0000}; log loss {logLoss.Item1:0.0000} -> {logLoss.Item2:0.0000}; AUC {aucRaw:0.0000} -> {aucCalibrated:0.0000}");

                ece.Item2.Should().BeLessThan(ece.Item1);
                brier.Item2.Should().BeLessThan(brier.Item1);
                logLoss.Item2.Should().BeLessThan(logLoss.Item1);
                var ordered = run.PoolScores.Zip(calibrated, (r, c) => (r, c)).OrderBy(p => p.r).ToArray();

                for (var i = 1; i < ordered.Length; i++)
                {
                    ordered[i].c.Should().BeGreaterThanOrEqualTo(ordered[i - 1].c);
                }

                aucCalibrated.Should().BeApproximately(aucRaw, 0.01);
            }
        }

        [Fact]
        public async Task TheStoredQuality_ShowsTheImprovementOnTheBalancedTestSplitAndAtTheBaseRateAsync()
        {
            var runs = await CalibratedRunsAsync(3, output);
            runs.Should().HaveCountGreaterOrEqualTo(3);

            var eceRaw = new List<double>();
            var eceCalibrated = new List<double>();
            var logLossRaw = new List<double>();
            var logLossCalibrated = new List<double>();

            foreach (var run in runs)
            {
                var quality = CalibrationQuality.FromJson(run.Promoted.CalibrationQualityJson)!;

                output.WriteLine(
                    $"seed {run.Seed} balanced test: ECE {quality.Test.Ece.Raw:0.0000} -> {quality.Test.Ece.Calibrated:0.0000}; Brier {quality.Test.Brier.Raw:0.0000} -> {quality.Test.Brier.Calibrated:0.0000}; log loss {quality.Test.LogLoss.Raw:0.0000} -> {quality.Test.LogLoss.Calibrated:0.0000}; validation Brier {quality.Validation.Brier.Raw:0.0000} -> {quality.Validation.Brier.Calibrated:0.0000}");

                output.WriteLine(
                    $"seed {run.Seed} test at base rate: Brier {quality.TestAtBaseRate.Brier.Raw:0.0000} -> {quality.TestAtBaseRate.Brier.Calibrated:0.0000}; log loss {quality.TestAtBaseRate.LogLoss.Raw:0.0000} -> {quality.TestAtBaseRate.LogLoss.Calibrated:0.0000}");

                quality.TestAtBaseRate.Brier.Calibrated.Should().BeLessThan(quality.TestAtBaseRate.Brier.Raw!.Value);
                quality.TestAtBaseRate.LogLoss.Calibrated.Should()
                    .BeLessThan(quality.TestAtBaseRate.LogLoss.Raw!.Value);

                quality.Validation.LogLoss.Calibrated.Should().BeLessThan(quality.Validation.LogLoss.Raw!.Value + 0.01);
                eceRaw.Add(quality.Test.Ece.Raw!.Value);
                eceCalibrated.Add(quality.Test.Ece.Calibrated!.Value);
                logLossRaw.Add(quality.Test.LogLoss.Raw!.Value);
                logLossCalibrated.Add(quality.Test.LogLoss.Calibrated!.Value);
            }

            logLossCalibrated.Average().Should().BeLessThan(logLossRaw.Average() + 0.01);
            eceCalibrated.Average().Should().BeLessThan(eceRaw.Average() + 0.01);
        }

        [Fact]
        public async Task
            TheOptimismOfCalibratingOnTheSelectionSplitIsSmallComparedWithACalibratorFittedOnTheTestSplitItselfAsync()
        {
            var runs = await CalibratedRunsAsync(3, output);
            runs.Should().HaveCountGreaterOrEqualTo(3);

            foreach (var run in runs)
            {
                var mode = run.Promoted.CalibrationInputId == ExhaustiveCalibrationInput.Logit
                    ? CalibrationInput.LogitOfBoundedScore
                    : CalibrationInput.RawScore;

                var oracle = PlattCalibration.Fit(run.TestScores, run.TestLabels, null, mode);

                var validationFitted = new Calibrator(run.Promoted.CalibrationSlope!.Value,
                    run.Promoted.CalibrationIntercept!.Value, mode, run.Promoted.CalibrationEpsilon!.Value);

                var brierValidation = BinaryMetrics.BrierScore(validationFitted.Apply(run.TestScores), run.TestLabels);
                var brierOracle = BinaryMetrics.BrierScore(oracle.Apply(run.TestScores), run.TestLabels);
                var logLossValidation = BinaryMetrics.LogLoss(validationFitted.Apply(run.TestScores), run.TestLabels);
                var logLossOracle = BinaryMetrics.LogLoss(oracle.Apply(run.TestScores), run.TestLabels);

                output.WriteLine(
                    $"seed {run.Seed}: validation slope {validationFitted.Slope:0.000} intercept {validationFitted.Intercept:0.000}; test-fitted slope {oracle.Slope:0.000} intercept {oracle.Intercept:0.000}; test Brier {brierValidation:0.0000} (validation fit) versus {brierOracle:0.0000} (test fit, best possible); log loss {logLossValidation:0.0000} versus {logLossOracle:0.0000}");

                (brierValidation - brierOracle).Should().BeLessThan(0.02);
                (logLossValidation - logLossOracle).Should().BeLessThan(0.08);
            }
        }

        [Fact]
        public async Task TheCalibratorIsFittedOnTheValidationSplit_NotOnTheTestLabelsAsync()
        {
            var runs = await CalibratedRunsAsync(1, output);
            runs.Should().HaveCountGreaterOrEqualTo(1);
            var run = runs[0];
            var validation = run.Development.Take(200).ToArray();
            var labels = run.DevelopmentOutputs.Take(200).ToArray();
            var testData = run.Development.Skip(200).Take(200).ToArray();
            var testLabels = run.DevelopmentOutputs.Skip(200).Take(200).ToArray();

            var promoter = TrainingPromoter();

            var inputs = new CalibrationInputs(validation, labels, run.DevelopmentOutputs,
                new BaseRateResolution(BaseRate, "Override"));

            var trial = new ExhaustiveSearchInstanceTrialInstance { Id = 1 };

            var baseline = promoter.FitCalibration(run.Network, inputs, trial, testData, testLabels);

            var flipped = promoter.FitCalibration(run.Network, inputs, trial, testData,
                [.. testLabels.Select(l => new[] { 1 - l[0] })]);

            var validationFlipped = promoter.FitCalibration(run.Network,
                new CalibrationInputs(validation, [.. labels.Select((l, i) => i < 60 ? [1 - l[0]] : l)],
                    run.DevelopmentOutputs, new BaseRateResolution(BaseRate, "Override")), trial, testData,
                testLabels);

            flipped.Slope.Should().Be(baseline.Slope);
            flipped.Intercept.Should().Be(baseline.Intercept);
            flipped.Status.Should().Be(baseline.Status);
            validationFlipped.Slope.Should().NotBe(baseline.Slope);
        }

        private static ModelPromoter TrainingPromoter()
        {
            var recorder = new TrainRecordingRecorder();
            var performance = new Performance();

            return new ModelPromoter(recorder, performance,
                new JsonSerializationHelper(), null!, TrainSupport.Log);
        }

        private sealed record Run(
            int Seed,
            ExhaustiveSearchInstancePromotedTrialInstance Promoted,
            ExhaustiveCalibration Calibration,
            double[] PoolScores,
            int[] PoolLabels,
            double[][] Development,
            double[][] DevelopmentOutputs,
            ActivationNetwork Network,
            double[] TestScores,
            int[] TestLabels);
    }
}
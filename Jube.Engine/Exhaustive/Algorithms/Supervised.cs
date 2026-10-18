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
using Accord.Math;
using Accord.Neuro;
using Jube.Data.Poco;
using Jube.Data.Repository.Models;
using Jube.Engine.Exhaustive.Activation;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Exhaustive.Calibration;
using Jube.Engine.Exhaustive.Calibration.Models;
using Jube.Engine.Exhaustive.Interfaces;
using Jube.Engine.Exhaustive.Optimisation;
using Jube.Engine.Exhaustive.Promotion;
using Jube.Engine.Exhaustive.Selection;
using Jube.Engine.Exhaustive.Topology;
using Jube.Engine.Helpers;
using log4net;

namespace Jube.Engine.Exhaustive.Algorithms
{
    public class Supervised
    {
        private readonly BaseRateResolution baseRate;
        private readonly string calibrationMethod;
        private readonly double? challengerModelBaseProbabilityOverride;
        private readonly Random challengerRandom;
        private readonly DataOptimiser dataOptimiser;
        private readonly DataSplitter dataSplitter;
        private readonly int exhaustiveSearchInstanceId;
        private readonly ILog log;
        private readonly Performance performance = new();
        private readonly ModelPromoter promoter;
        private readonly ITrialRecorder recorder;
        private readonly ExhaustiveTrainingSettings settings;
        private readonly TopologyEvolver topologyEvolver;

        public Supervised(
            ExhaustiveTrainingSettings settings,
            ITrialRecorder recorder,
            int exhaustiveSearchInstanceId,
            JsonSerializationHelper jsonSerializationHelper,
            Random seeded,
            Dictionary<int, Variable> variables,
            double[][] data,
            double[] output,
            ILog log,
            BaseRateResolution baseRate = null,
            string calibrationMethod = ExhaustiveCalibrationRequest.Automatic,
            double[][] syntheticData = null,
            double[] syntheticOutput = null,
            double? challengerModelBaseProbabilityOverride = null)
        {
            this.baseRate = baseRate;
            this.calibrationMethod = calibrationMethod;
            this.settings = settings;
            this.challengerModelBaseProbabilityOverride = challengerModelBaseProbabilityOverride;
            this.recorder = recorder;
            this.exhaustiveSearchInstanceId = exhaustiveSearchInstanceId;
            this.log = log;
            var jaggedOutput = output.ToJagged();
            var jaggedSyntheticOutput = syntheticOutput?.ToJagged();
            var splitSeed = seeded.Next();
            challengerRandom = new Random(seeded.Next());
            Selector = new VariableSelector(recorder, seeded, variables, data, log, syntheticData);
            var sensitivityAnalyser = new SensitivityAnalyser(recorder, performance, seeded, log);
            promoter = new ModelPromoter(recorder, performance, jsonSerializationHelper, sensitivityAnalyser, log);
            topologyEvolver = new TopologyEvolver(recorder, performance, log);
            var activationSearcher = new ActivationFunctionSearcher(recorder, performance, log);
            dataSplitter = new DataSplitter(splitSeed, log);

            dataOptimiser = new DataOptimiser(recorder, variables, jaggedOutput, Selector, sensitivityAnalyser,
                activationSearcher, dataSplitter, log, jaggedSyntheticOutput);
        }

        internal VariableSelector Selector { get; }

        public async Task<bool> TrainAsync(CancellationToken token = default)
        {
            var trialsLimit = settings.TrialsLimit;
            var minVariableCount = settings.MinVariableCount;
            var maxVariableCount = settings.MaxVariableCount;
            var trainingDataSamplePercentage = settings.TrainingDataSamplePercentage;
            var crossValidationDataSamplePercentage = settings.CrossValidationDataSamplePercentage;
            var testingDataSamplePercentage = settings.TestingDataSamplePercentage;
            var validationTestingActivationThreshold = settings.ValidationTestingActivationThreshold;
            var topologySinceImprovementLimit = settings.TopologySinceImprovementLimit;
            var layerDepthLimit = settings.LayerDepthLimit;
            var layerWidthLimitInputLayerFactor = settings.LayerWidthLimitInputLayerFactor;
            var topologyComplexityLimit = settings.TopologyComplexityLimit;
            var activationFunctionExplorationEpochs = settings.ActivationFunctionExplorationEpochs;
            var topologyExplorationEpochs = settings.TopologyExplorationEpochs;
            var topologyFinalisationEpochs = settings.TopologyFinalisationEpochs;
            var simulationsCount = settings.SimulationsCount;

            var challengerModelBaseProbability =
                challengerModelBaseProbabilityOverride ?? settings.ChallengerModelBaseProbability;

            LogTrainingParameters();

            var bestScore = 0d;
            var models = 0;
            var modelsSinceBest = 0;
            var bestTopologyComplexity = 0;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    "Exhaustive Training: Is going to start supervised learning with the parameters has created " +
                    "global variables and repositories for the training,  will now proceed to loop until trial limit of" +
                    $"{trialsLimit} has been reached.  Will check if stopped before next trial");
            }

            for (var i = 1; i < trialsLimit; i++)
            {
                try
                {
                    if (await IsStoppingOrStoppedAsync(token).ConfigureAwait(false))
                    {
                        return false;
                    }

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Is starting trial instance count {i}.  Recording the trial instance in the database.");
                    }

                    var exhaustiveSearchInstanceTrialInstance =
                        await InsertExhaustiveSearchInstanceTrialInstanceAsync(token).ConfigureAwait(false);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                            $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}.  " +
                            " Will create a random variable count for this trial and optimise the data based on sensitivity of" +
                            "those variables.");
                    }

                    var (activationFunction,
                        trialVariables,
                        dataTraining,
                        dataCrossValidation,
                        dataTesting,
                        outputsTraining,
                        outputsCrossValidation,
                        outputsTesting) = await dataOptimiser.OptimiseDataAsync(activationFunctionExplorationEpochs,
                        minVariableCount,
                        maxVariableCount,
                        exhaustiveSearchInstanceTrialInstance,
                        trainingDataSamplePercentage,
                        crossValidationDataSamplePercentage,
                        testingDataSamplePercentage,
                        validationTestingActivationThreshold,
                        token
                    ).ConfigureAwait(false);

                    var challengerKind = ChallengerModelSelector.SelectFamily(challengerRandom,
                        ChallengerModelSelector.ChallengerProbability(challengerModelBaseProbability,
                            bestTopologyComplexity, topologyComplexityLimit));

                    ActivationNetwork topologyNetwork = null;
                    var topologyComplexity = 0;
                    ChallengerTrainingResult challengerResult = null;

                    if (challengerKind == null)
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                                $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                                "starting the evolution of width and depth.");
                        }

                        (topologyNetwork, _, topologyComplexity) = await topologyEvolver.EvolveTopologyAsync(
                            topologySinceImprovementLimit,
                            topologyExplorationEpochs,
                            topologyFinalisationEpochs,
                            i,
                            exhaustiveSearchInstanceTrialInstance,
                            activationFunction,
                            trialVariables,
                            dataTraining,
                            outputsTraining,
                            dataCrossValidation,
                            outputsCrossValidation,
                            dataTesting,
                            outputsTesting,
                            validationTestingActivationThreshold,
                            layerWidthLimitInputLayerFactor, layerDepthLimit,
                            topologyComplexityLimit, token).ConfigureAwait(false);

                        LogTopology(exhaustiveSearchInstanceTrialInstance, topologyNetwork);
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                                $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                                $"training a challenger model of kind {challengerKind} instead of evolving a topology.");
                        }

                        var orderedVariables = trialVariables.Values.ToArray();
                        var names = orderedVariables.Select(v => v.Name).ToArray();

                        double[][] Raw(double[][] standardised)
                        {
                            return
                            [
                                .. standardised.Select(row =>
                                    row.Select((value, column) => orderedVariables[column].ReverseZScore(value))
                                        .ToArray())
                            ];
                        }

                        var challengerInputs = Raw(dataTraining).Concat(Raw(dataCrossValidation)).ToArray();

                        var challengerLabels = outputsTraining.Concat(outputsCrossValidation)
                            .Select(o => (int)Math.Round(o[0])).ToArray();

                        challengerResult = ChallengerModelTrainer.Train(challengerKind.Value, challengerRandom,
                            challengerInputs, challengerLabels, names);

                        performance.CalculatePerformance(
                            performance.CalculateScores(challengerResult.Recall.Predict, Raw(dataTesting)),
                            outputsTesting, validationTestingActivationThreshold);
                    }

                    models = await IncrementModelsAsync(models, token).ConfigureAwait(false);

                    var trialJson = challengerKind == null
                        ? promoter.SerializeTopologyNetwork(topologyNetwork)
                        : challengerResult.ModelJson;

                    var trialMetricsJson = challengerKind == null ? null : challengerResult.MetricsJson;

                    var trialVariablesJson = challengerKind == null
                        ? null
                        : ModelPromoter.SerializeChallengerVariables(challengerResult, trialVariables);

                    await recorder.UpdateTrialOutcomeAsync(exhaustiveSearchInstanceTrialInstance.Id,
                        challengerKind ?? AdvancedAnalyticsModelKind.ExhaustiveTopologyNetwork, performance.Score,
                        trialJson, trialMetricsJson, trialVariablesJson, token).ConfigureAwait(false);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                            $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                            $"The total number of models executed so far is {models} and the best score is {bestScore}.  " +
                            $"This score is {performance.Score} and evaluates {performance.Score > bestScore} for " +
                            "promotion.");
                    }

                    if (performance.Score > bestScore && performance.Score > 0)
                    {
                        bestScore = performance.Score;

                        if (challengerKind == null)
                        {
                            bestTopologyComplexity = topologyComplexity;

                            await promoter.RecordBestScoreAsync(i, exhaustiveSearchInstanceTrialInstance, bestScore,
                                modelsSinceBest, topologyComplexity, token).ConfigureAwait(false);

                            modelsSinceBest = 0;

                            await promoter.PromoteAndExplainAsync(i, exhaustiveSearchInstanceTrialInstance,
                                topologyNetwork, topologyComplexity, trialVariables, dataTesting, outputsTesting,
                                simulationsCount, token,
                                new CalibrationInputs(dataCrossValidation, outputsCrossValidation, outputsTraining,
                                    baseRate), calibrationMethod).ConfigureAwait(false);
                        }
                        else
                        {
                            await promoter.RecordBestScoreAsync(i, exhaustiveSearchInstanceTrialInstance, bestScore,
                                modelsSinceBest, 0, token).ConfigureAwait(false);

                            modelsSinceBest = 0;

                            await promoter.PromoteChallengerAsync(exhaustiveSearchInstanceTrialInstance,
                                challengerResult, trialVariables, token).ConfigureAwait(false);

                            await promoter.StoreRocAsync(outputsTesting, exhaustiveSearchInstanceTrialInstance, token)
                                .ConfigureAwait(false);

                            await promoter.StorePredictedVsActualAsync(exhaustiveSearchInstanceTrialInstance,
                                outputsTesting, trialVariables, dataTesting, token).ConfigureAwait(false);

                            var orderedChallengerVariables = trialVariables.Values.ToArray();

                            double ChallengerScore(double[] zScored)
                            {
                                var raw = zScored.Select((value, column) =>
                                    orderedChallengerVariables[column].ReverseZScore(value)).ToArray();

                                return challengerResult.Recall.Predict(raw)[0];
                            }

                            await promoter.PerformMonteCarloSimulationToUnderstandTopologyModelAsync(
                                simulationsCount, trialVariables, ChallengerScore, token).ConfigureAwait(false);
                        }

                        if (!(Math.Abs(bestScore - 1) < 0.0001))
                        {
                            continue;
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                                $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                                "Is breaking training for reasons of total fit (and probably over fit).");
                        }

                        break;
                    }

                    modelsSinceBest = await IncrementModelsSinceBestAsync(modelsSinceBest, token).ConfigureAwait(false);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                            $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                            $"Has not beaten the best model,  has incremented models since best to {modelsSinceBest}.");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.Error(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                        $"Trial {trialsLimit} has caused an error in training as {ex}. ");
                }
            }

            return true;
        }

        private void LogTrainingParameters()
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    "Exhaustive Training: Is going to start supervised learning with the parameters " +
                    $"Trials Limit:{settings.TrialsLimit}," +
                    $"Min Variable Count: {settings.MinVariableCount}," +
                    $"MaxVariableCount: {settings.MaxVariableCount}," +
                    $"Training Data Sample Percentage: {settings.TrainingDataSamplePercentage}, " +
                    $"Cross Validation Data Sample Percentage: {settings.CrossValidationDataSamplePercentage}, " +
                    $"Testing Data Sample Percentage: {settings.TestingDataSamplePercentage}, " +
                    $"Validation Testing Activation Threshold:{settings.ValidationTestingActivationThreshold}," +
                    $"Topology Since Improvement Limit: {settings.TopologySinceImprovementLimit}, " +
                    $"Layer Depth Limit: {settings.LayerDepthLimit}, " +
                    $"Layer Width Limit Input Layer Factor: {settings.LayerWidthLimitInputLayerFactor}, " +
                    $"Topology Complexity Limit: {settings.TopologyComplexityLimit}, " +
                    $"ActivationFunctionExplorationEpochs: {settings.ActivationFunctionExplorationEpochs}," +
                    $"Topology Exploration Epochs: {settings.TopologyExplorationEpochs}," +
                    $"Topology Finalisation Epochs: {settings.TopologyFinalisationEpochs}, " +
                    $"Simulations Count: {settings.SimulationsCount}.");
            }
        }

        private void LogTopology(ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            Network topologyNetwork)
        {
            var layersString =
                "Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                $" Has concluded the topology evolution.  The topology discovered is {topologyNetwork.Layers.Length} deep.  The " +
                " processing elements for each layer are as follows:";

            var first = true;
            var complexity = 0;

            foreach (var layer in topologyNetwork.Layers)
            {
                if (!first)
                {
                    layersString += ",";
                }
                else
                {
                    first = false;
                }

                layersString += layer.Neurons.Length;

                complexity += layer.Neurons.Length;
            }

            layersString += $". The topology complexity is {complexity}.";

            if (log.IsInfoEnabled)
            {
                log.Info(layersString);
            }
        }

        private async Task<int> IncrementModelsAsync(int models, CancellationToken token = default)
        {
            models += 1;
            await recorder.UpdateModelsAsync(models, token).ConfigureAwait(false);

            return models;
        }

        private async Task<int> IncrementModelsSinceBestAsync(int modelsSinceBest, CancellationToken token = default)
        {
            modelsSinceBest += 1;
            await recorder.UpdateModelsSinceBestAsync(modelsSinceBest, token).ConfigureAwait(false);

            return modelsSinceBest;
        }

        internal void SplitDataForTest(
            double training, double crossValidation, double testing,
            double[][] dataToSplit, double[][] outputToSplit,
            out double[][] dataTraining, out double[][] outputsTraining,
            out double[][] dataCrossValidation, out double[][] outputsCrossValidation,
            out double[][] dataTesting, out double[][] outputsTesting)
        {
            dataSplitter.SplitData(training, crossValidation, testing, new ExhaustiveSearchInstanceTrialInstance(),
                dataToSplit, outputToSplit, out dataTraining, out outputsTraining,
                out dataCrossValidation, out outputsCrossValidation, out dataTesting, out outputsTesting);
        }

        private Task<ExhaustiveSearchInstanceTrialInstance> InsertExhaustiveSearchInstanceTrialInstanceAsync(
            CancellationToken token = default)
        {
            var exhaustiveSearchInstanceTrialInstance = new ExhaustiveSearchInstanceTrialInstance
            {
                ExhaustiveSearchInstanceId = exhaustiveSearchInstanceId,
                CreatedDate = DateTime.UtcNow
            };

            return recorder.InsertTrialInstanceAsync(exhaustiveSearchInstanceTrialInstance, token);
        }

        private Task<bool> IsStoppingOrStoppedAsync(CancellationToken token = default)
        {
            return recorder.IsStopRequestedAsync(token);
        }
    }
}
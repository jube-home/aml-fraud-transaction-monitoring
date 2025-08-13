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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Accord.Math;
using Accord.Neuro;
using Accord.Neuro.ActivationFunctions;
using Accord.Neuro.Learning;
using Accord.Statistics;
using Accord.Statistics.Visualizations;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.Exhaustive.Variables;
using Jube.Engine.Helpers.Json;
using log4net;
using Newtonsoft.Json;

namespace Jube.Engine.Exhaustive.Algorithms;

public class Supervised
{
    private readonly double[][] _data;
    private readonly DbContext _dbContext;
    private readonly DynamicEnvironment.DynamicEnvironment _environment;
    private readonly int _exhaustiveSearchInstanceId;
    private readonly JsonSerializerSettings _jsonSerializerSettings;
    private readonly ILog _log;
    private readonly double[][] _output;
    private readonly Performance _performance;
    private readonly ExhaustiveSearchInstanceRepository _repositoryExhaustiveSearchInstance;
    private readonly Random _seeded;
    private readonly Dictionary<int, Variable> _variables;

    public Supervised(DynamicEnvironment.DynamicEnvironment environment,
        ExhaustiveSearchInstanceRepository repositoryExhaustiveSearchInstance,
        int exhaustiveSearchInstanceId, Random seeded, Dictionary<int, Variable> variables,
        double[][] data, double[] output, DbContext dbContext, ILog log)
    {
        _jsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ContractResolver = new DeepContractResolver()
        };

        _variables = variables;
        _exhaustiveSearchInstanceId = exhaustiveSearchInstanceId;
        _seeded = seeded;
        _data = data;
        _output = output.ToJagged();
        _dbContext = dbContext;
        _log = log;
        _performance = new Performance();
        _repositoryExhaustiveSearchInstance = repositoryExhaustiveSearchInstance;
        _environment = environment;

        _log.Info("Exhaustive Training: Has initialised supervised learning.");
    }

    public Task<bool> Train()
    {
        var trialsLimit = int.Parse(_environment.AppSettings("ExhaustiveTrialsLimit"));
        var minVariableCount = int.Parse(_environment.AppSettings("ExhaustiveMinVariableCount"));
        var maxVariableCount = int.Parse(_environment.AppSettings("ExhaustiveMaxVariableCount"));
        var trainingDataSamplePercentage =
            double.Parse(_environment.AppSettings("ExhaustiveTrainingDataSamplePercentage"));
        var crossValidationDataSamplePercentage =
            double.Parse(_environment.AppSettings("ExhaustiveCrossValidationDataSamplePercentage"));
        var testingDataSamplePercentage =
            double.Parse(_environment.AppSettings("ExhaustiveTestingDataSamplePercentage"));
        var validationTestingActivationThreshold =
            double.Parse(_environment.AppSettings("ExhaustiveValidationTestingActivationThreshold"));
        var topologySinceImprovementLimit =
            int.Parse(_environment.AppSettings("ExhaustiveTopologySinceImprovementLimit"));
        var layerDepthLimit = int.Parse(_environment.AppSettings("ExhaustiveLayerDepthLimit"));
        var layerWidthLimitInputLayerFactor =
            int.Parse(_environment.AppSettings("ExhaustiveLayerWidthLimitInputLayerFactor"));
        var topologyComplexityLimit = int.Parse(_environment.AppSettings("ExhaustiveTopologyComplexityLimit"));
        var activationFunctionExplorationEpochs =
            int.Parse(_environment.AppSettings("ExhaustiveActivationFunctionExplorationEpochs"));
        var topologyExplorationEpochs = int.Parse(_environment.AppSettings("ExhaustiveTopologyExplorationEpochs"));
        var topologyFinalisationEpochs = int.Parse(_environment.AppSettings("ExhaustiveTopologyFinalisationEpochs"));
        var simulationsCount = int.Parse(_environment.AppSettings("ExhaustiveSimulationsCount"));

        _log.Info(
            "Exhaustive Training: Is going to start supervised learning with the parameters " +
            $"Trials Limit:{trialsLimit}," +
            $"Min Variable Count: {minVariableCount}," +
            $"MaxVariableCount: {maxVariableCount}," +
            $"Training Data Sample Percentage: {trainingDataSamplePercentage}, " +
            $"Cross Validation Data Sample Percentage: {crossValidationDataSamplePercentage}, " +
            $"Testing Data Sample Percentage: {testingDataSamplePercentage}, " +
            $"Validation Testing Activation Threshold:{validationTestingActivationThreshold}," +
            $"Topology Since Improvement Limit: {topologySinceImprovementLimit}, " +
            $"Layer Depth Limit: {layerDepthLimit}, " +
            $"Layer Width Limit Input Layer Factor: {layerWidthLimitInputLayerFactor}, " +
            $"Topology Complexity Limit: {topologyComplexityLimit}, " +
            $"ActivationFunctionExplorationEpochs: {activationFunctionExplorationEpochs}," +
            $"Topology Exploration Epochs: {topologyExplorationEpochs}," +
            $"Topology Finalisation Epochs: {topologyFinalisationEpochs}, " +
            $"Simulations Count: {simulationsCount}.");

        var bestScore = 0d;
        var models = 0;
        var modelsSinceBest = 0;

        var exhaustiveSearchInstanceTrialInstanceRepository = new
            ExhaustiveSearchInstanceTrialInstanceRepository(_dbContext);

        var exhaustiveSearchInstancePromotedTrialInstanceRepository = new
            ExhaustiveSearchInstancePromotedTrialInstanceRepository(_dbContext);

        var exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository = new
            ExhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository(_dbContext);

        var exhaustiveSearchInstancePromotedTrialInstanceRocRepository =
            new ExhaustiveSearchInstancePromotedTrialInstanceRocRepository(_dbContext);

        var exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository =
            new ExhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository(_dbContext);

        var exhaustiveSearchInstancePromotedTrialInstancePrescriptionRepository =
            new ExhaustiveSearchInstancePromotedTrialInstanceVariableRepository(_dbContext);

        _log.Info(
            "Exhaustive Training: Is going to start supervised learning with the parameters has created " +
            "global variables and repositories for the training,  will now proceed to loop until trial limit of" +
            $"{trialsLimit} has been reached.  Will check if stopped before next trial");

        for (var i = 1; i < trialsLimit; i++)
            try
            {
                if (IsStoppingOrStopped(_exhaustiveSearchInstanceId)) return Task.FromResult(false);

                _log.Info(
                    $"Exhaustive Training: Is starting trial instance count {i}.  Recording the trial instance in the database.");

                var exhaustiveSearchInstanceTrialInstance =
                    InsertExhaustiveSearchInstanceTrialInstance(exhaustiveSearchInstanceTrialInstanceRepository,
                        _exhaustiveSearchInstanceId);

                _log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}.  " +
                    " Will create a random variable count for this trial and optimise the data based on sensitivity of" +
                    "those variables.");

                OptimiseData(_log, _dbContext,
                    activationFunctionExplorationEpochs,
                    minVariableCount,
                    maxVariableCount,
                    exhaustiveSearchInstanceTrialInstance,
                    trainingDataSamplePercentage,
                    crossValidationDataSamplePercentage,
                    testingDataSamplePercentage,
                    validationTestingActivationThreshold,
                    out var activationFunction,
                    out var trialVariables,
                    out var dataTraining,
                    out var dataCrossValidation,
                    out var dataTesting,
                    out var outputsTraining,
                    out var outputsCrossValidation,
                    out var outputsTesting
                );

                _log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    "starting the evolution of width and depth.");

                EvolveTopology(
                    _log,
                    _performance,
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
                    topologyComplexityLimit,
                    out var topologyNetwork,
                    out var performance,
                    out var topologyComplexity);

                LogTopology(exhaustiveSearchInstanceTrialInstance, topologyNetwork);

                models = IncrementModels(models);

                _log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    $"The total number of models executed so far is {models} and the best score is {bestScore}.  " +
                    $"This score is {performance.Score} and evaluates {performance.Score > bestScore} for " +
                    "promotion.");

                if (performance.Score > bestScore && performance.Score > 0)
                {
                    bestScore = performance.Score;

                    _log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                        $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                        $"Has promoted best score as {bestScore}.  There have been {modelsSinceBest},  which will be reset. " +
                        "Will promote model in the database.");

                    _repositoryExhaustiveSearchInstance.UpdateBestScore(_exhaustiveSearchInstanceId, bestScore,
                        topologyComplexity);

                    modelsSinceBest = 0;

                    PromoteTopologyNetwork(topologyNetwork, performance, topologyComplexity,
                        exhaustiveSearchInstanceTrialInstance,
                        exhaustiveSearchInstancePromotedTrialInstanceRepository);

                    _log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                        $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                        "Has promoted the model in the database,  will now store the ROC data.");

                    StoreRoc(performance,
                        outputsTesting,
                        exhaustiveSearchInstanceTrialInstance,
                        exhaustiveSearchInstancePromotedTrialInstanceRocRepository);

                    _log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                        $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                        "Has stored ROC data and will now store the predicted vs actual data (this will be sampled when" +
                        "presenting it in the user interface).");

                    StorePredictedVsActual(exhaustiveSearchInstanceTrialInstance, outputsTesting,
                        exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository);

                    _log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                        $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                        "Has stored the predicted vs. the actual in the database,  will now proceed to perform " +
                        "sensitivity analysis on the model and variables.");

                    PerformSensitivityAnalysisAndStoreForPromoted(
                        _log,
                        exhaustiveSearchInstanceTrialInstance.Id,
                        topologyNetwork, trialVariables,
                        dataCrossValidation, exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository);

                    _log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                        $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                        "Has performed sensitivity analysis on the model and variables and will now use Monte Carlo " +
                        "simulation to explain the model topology in practice.");

                    PerformMonteCarloSimulationToUnderstandTopologyModel(simulationsCount,
                        trialVariables,
                        topologyNetwork,
                        exhaustiveSearchInstancePromotedTrialInstancePrescriptionRepository);

                    _log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                        $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                        "Has performed Monte Carlo simulation on the model and variables and the promotion is concluded.");

                    if (!(Math.Abs(bestScore - 1) < 0.0001)) continue;

                    _log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                        $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                        "Is breaking training for reasons of total fit (and probably over fit).");

                    break;
                }

                modelsSinceBest = IncrementModelsSinceBest(modelsSinceBest);

                _log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    $"Has not beaten the best model,  has incremented models since best to {modelsSinceBest}.");
            }
            catch (Exception ex)
            {
                _log.Error(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial {trialsLimit} has caused an error in training as {ex}. ");
            }

        return Task.FromResult(true);
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
                layersString += ",";
            else
                first = false;

            layersString += layer.Neurons.Length;

            complexity += layer.Neurons.Length;
        }

        layersString += $". The topology complexity is {complexity}.";

        _log.Info(layersString);
    }

    private void PerformMonteCarloSimulationToUnderstandTopologyModel(int simulationsCount,
        Dictionary<int, TrialVariable> trialVariables, Network topologyNetwork,
        ExhaustiveSearchInstancePromotedTrialInstanceVariableRepository
            exhaustiveSearchInstancePromotedTrialInstanceVariableRepository)
    {
        var activations = new List<double[]>();
        for (var j = 0; j < simulationsCount; j++)
        {
            var simulationZScore = new double[trialVariables.Count];

            for (var k = 0; k < trialVariables.Count; k++)
                try
                {
                    simulationZScore[k] = trialVariables.ElementAt(k).Value.TriangularDistribution.Generate();
                    if (trialVariables.ElementAt(k).Value.NormalisationTypeId == 1)
                        simulationZScore[k] = simulationZScore[k] > 0.5 ? 1 : 0;
                }
                catch
                {
                    simulationZScore[k] = 0;
                }

            if (topologyNetwork.Compute(simulationZScore)[0] > 0.5) activations.Add(simulationZScore);
        }

        var activationsArray = activations.ToArray();

        if (activations.Count <= 0) return;
        {
            for (var j = 0; j < trialVariables.Count; j++)
            {
                var exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription =
                    new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                    {
                        ExhaustiveSearchInstanceTrialInstanceVariableId =
                            trialVariables.ElementAt(j).Value.ExhaustiveSearchInstanceTrialInstanceVariableId
                    };

                var outputs = activationsArray.GetColumn(j);
                for (var i = 0; i < outputs.Length; i++)
                    outputs[i] = trialVariables.ElementAt(j).Value.ReverseZScore(outputs[i]);

                exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Mean = outputs.Mean();
                exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Maximum = outputs.Max();
                exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Minimum = outputs.Min();
                exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.StandardDeviation =
                    outputs.StandardDeviation();

                outputs.Quartiles(out var q1, out var q3);
                var iqr = q1 - q3;

                exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Iqr =
                    trialVariables.ElementAt(j).Value.ReverseZScore(iqr);

                exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription =
                    exhaustiveSearchInstancePromotedTrialInstanceVariableRepository
                        .Insert(exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription);

                if (exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Maximum -
                    exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Minimum == 0) continue;

                var histogram = new Histogram();
                histogram.Compute(outputs);

                var binSequence = 0;
                foreach (var histogramBin in histogram.Bins)
                {
                    var exhaustiveSearchInstanceTrialInstanceVariablePrescriptionHistogram =
                        new ExhaustiveSearchInstanceTrialInstanceVariablePrescriptionHistogram
                        {
                            ExhaustiveSearchInstancePromotedTrialInstanceVariableId
                                = exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription
                                    .Id,
                            Frequency = histogramBin.Value,
                            BinIndex = binSequence,
                            BinRangeEnd = histogramBin.Range.Min,
                            BinRangeStart = histogramBin.Range.Max
                        };

                    exhaustiveSearchInstanceTrialInstanceVariablePrescriptionHistogram.BinRangeEnd =
                        histogramBin.Range.Max;

                    binSequence += 1;
                }
            }
        }
    }

    private void PerformSensitivityAnalysisAndStoreForPromoted(
        ILog log,
        int exhaustiveSearchInstanceTrialInstanceId,
        ActivationNetwork topologyNetwork,
        Dictionary<int, TrialVariable> trialVariables, double[][] dataTesting,
        ExhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository
            exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository)
    {
        var sensitivityAnalysis = PerformSensitivityAnalysis(
            log, exhaustiveSearchInstanceTrialInstanceId,
            topologyNetwork, trialVariables, dataTesting, _performance.Scores);

        foreach (var (key, value) in sensitivityAnalysis)
        {
            var exhaustiveSearchInstancePromotedTrialInstanceSensitivity =
                new ExhaustiveSearchInstancePromotedTrialInstanceSensitivity
                {
                    Sensitivity = value,
                    SensitivityRank = 1,
                    ExhaustiveSearchInstanceTrialInstanceVariableId =
                        trialVariables[key].ExhaustiveSearchInstanceTrialInstanceVariableId
                };

            exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository.Insert(
                exhaustiveSearchInstancePromotedTrialInstanceSensitivity);
        }
    }

    private void StoreRoc(Performance performance, double[][] outputsTesting,
        ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
        ExhaustiveSearchInstancePromotedTrialInstanceRocRepository
            exhaustiveSearchInstancePromotedTrialInstanceRocRepository)
    {
        var minPrediction = 0d;
        var maxPrediction = 1d;
        var rocStep = (maxPrediction - minPrediction) / 20d;
        var rocStepThreshold = 0.05d;
        for (var j = 0; j < 20; j++)
        {
            performance.CalculatePerformance(performance.Scores, outputsTesting, rocStepThreshold);

            var exhaustiveSearchInstancePromotedTrialInstanceRoc =
                new ExhaustiveSearchInstancePromotedTrialInstanceRoc
                {
                    ExhaustiveSearchInstanceTrialInstanceId =
                        exhaustiveSearchInstanceTrialInstance.Id,
                    Score = performance.Score,
                    FalsePositive = performance.Fp,
                    FalseNegative = performance.Fn,
                    TruePositive = performance.Tp,
                    TrueNegative = performance.Tn,
                    Threshold = rocStepThreshold
                };

            exhaustiveSearchInstancePromotedTrialInstanceRocRepository.Insert(
                exhaustiveSearchInstancePromotedTrialInstanceRoc);

            rocStepThreshold += rocStep;
        }
    }

    private void StorePredictedVsActual(ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
        IReadOnlyList<double[]> outputsTesting,
        ExhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository
            exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository)
    {
        for (var j = 0; j < _performance.Scores.Length; j++)
        {
            var exhaustiveSearchInstancePromotedTrialInstancePredictedActual
                = new ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
                {
                    ExhaustiveSearchInstanceTrialInstanceId =
                        exhaustiveSearchInstanceTrialInstance.Id,
                    Predicted = _performance.Scores[j],
                    Actual = outputsTesting[j][0]
                };

            exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository.Insert(
                exhaustiveSearchInstancePromotedTrialInstancePredictedActual);
        }
    }

    private void PromoteTopologyNetwork(Network topologyNetwork, Performance performance, int topologyComplexity,
        ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
        ExhaustiveSearchInstancePromotedTrialInstanceRepository exhaustiveSearchInstancePromotedTrialsRepository)
    {
        var json = JsonConvert.SerializeObject(topologyNetwork, _jsonSerializerSettings);

        var exhaustiveSearchInstancePromotedTrial = new ExhaustiveSearchInstancePromotedTrialInstance
        {
            Active = 1,
            Score = performance.Score,
            TopologyComplexity = topologyComplexity,
            TrueNegative = performance.Tn,
            TruePositive = performance.Tp,
            FalseNegative = performance.Fn,
            FalsePositive = performance.Fp,
            Json = json,
            ExhaustiveSearchInstanceTrialInstanceId = exhaustiveSearchInstanceTrialInstance
                .Id
        };

        exhaustiveSearchInstancePromotedTrialsRepository.Insert(exhaustiveSearchInstancePromotedTrial);
    }

    private int IncrementModels(int models)
    {
        models += 1;
        _repositoryExhaustiveSearchInstance.UpdateModels(_exhaustiveSearchInstanceId, models);
        return models;
    }

    private int IncrementModelsSinceBest(int modelsSinceBest)
    {
        modelsSinceBest += 1;
        _repositoryExhaustiveSearchInstance.UpdateModelsSinceBest(_exhaustiveSearchInstanceId, modelsSinceBest);
        return modelsSinceBest;
    }

    private void EvolveTopology(
        ILog log,
        Performance performance,
        int topologySinceImprovementLimit,
        int topologyExplorationEpochs,
        int topologyFinalEpochs,
        int i,
        ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
        IActivationFunction activationFunction,
        Dictionary<int, TrialVariable> trialVariables, double[][] dataTraining, double[][] outputsTraining,
        double[][] dataCrossValidation,
        double[][] outputsCrossValidation,
        double[][] dataTesting,
        double[][] outputsTesting,
        double validationTestingActivationThreshold,
        int layerWidthLimitInputLayerFactor,
        int layerDepthLimit, int topologyComplexityLimit,
        out ActivationNetwork bestTopologyNetwork,
        out Performance bestPerformance,
        out int bestTopologyComplexity)
    {
        bestTopologyNetwork = null;
        bestPerformance = null;
        bestTopologyComplexity = 0;

        var repositoryExhaustiveSearchInstanceTrialInstanceTopologyTrial
            = new ExhaustiveSearchInstanceTrialInstanceTopologyTrialRepository(_dbContext);

        var bestScore = 0d;
        var topologiesSinceNoImproveCounter = 0;
        var topologyTrials = 1;
        var layers = new List<int> { 1 };

        while (topologiesSinceNoImproveCounter < topologySinceImprovementLimit)
        {
            log.Info(
                $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. {topologiesSinceNoImproveCounter} topologies processed since last improvement.");

            var exhaustiveSearchInstanceTrialInstanceTopologyTrial =
                new ExhaustiveSearchInstanceTrialInstanceTopologyTrial
                {
                    ExhaustiveSearchInstanceTrialInstanceId = exhaustiveSearchInstanceTrialInstance
                        .Id,
                    TrialsSinceImprovement = topologiesSinceNoImproveCounter,
                    TopologyTrials = topologyTrials,
                    Layer = layers.Count,
                    Neurons = layers[^1],
                    Score = bestScore,
                    Finalisation = 0
                };

            exhaustiveSearchInstanceTrialInstanceTopologyTrial =
                repositoryExhaustiveSearchInstanceTrialInstanceTopologyTrial.Insert(
                    exhaustiveSearchInstanceTrialInstanceTopologyTrial);

            log.Info(
                $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                $" created entry in the database for {exhaustiveSearchInstanceTrialInstanceTopologyTrial.ExhaustiveSearchInstanceTrialInstanceId}.  Will construct topology network and trainer with {layers.Count} layers" +
                $" and activation function {activationFunction}.  Weights are randomised on the construction of the trainer.");

            var topologyNetwork = new ActivationNetwork(activationFunction, trialVariables.Count,
                MapTrialVariableToActivationNetworkAnnotations(trialVariables), LayersParamsArray(layers));

            var trainerExploration =
                new LevenbergMarquardtLearning(TopologyRandomise(topologyNetwork));

            log.Info(
                $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                $" created entry in the database for {exhaustiveSearchInstanceTrialInstanceTopologyTrial.ExhaustiveSearchInstanceTrialInstanceId}. Trainer is ready.");

            for (var k = 0; k < topologyExplorationEpochs; k++)
            {
                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} starting the training of EPOCH " +
                    k + ".");

                var sw = new Stopwatch();
                sw.Start();

                trainerExploration.RunEpoch(dataTraining, outputsTraining);

                sw.Stop();

                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. Trial instance count {i} the EPOCH {k} in {sw.ElapsedMilliseconds} ms.");

                sw.Reset();
            }

            log.Info(
                $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                " has finished topology training and will calculate the performance of the model.");

            var thisScore = _performance.CalculatePerformance(
                _performance.CalculateScores(topologyNetwork, dataCrossValidation),
                outputsCrossValidation,
                validationTestingActivationThreshold);

            var thisTopologyComplexity = topologyNetwork.Layers.Sum(layer => layer.Neurons.Length);

            log.Info(
                $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                $" has a score of {thisScore}.  The best score is {bestScore} and when tested against {thisScore} evaluates to {thisScore > bestScore}.  The topology complexity is {bestTopologyComplexity}.");

            if (thisScore > bestScore)
            {
                bestScore = thisScore;
                bestTopologyComplexity = thisTopologyComplexity;

                bestTopologyNetwork = (ActivationNetwork)topologyNetwork.DeepMemberwiseClone();
                topologiesSinceNoImproveCounter = 0;

                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    " has taken a deep copy of the best model and has reset the topologiesSinceNoImproveCounter to 0.");
            }

            if (Math.Abs(bestScore - 1) < 0.0001)
            {
                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} the topology has arrived at a perfect fit (too good in fact).  The training will terminate for this iteration.");

                break;
            }

            if (topologyNetwork.Layers[layers.Count - 1].Neurons.Length >
                trialVariables.Count * layerWidthLimitInputLayerFactor)
            {
                if (layers.Count < layerDepthLimit)
                {
                    log.Info($"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive " +
                             "Search Instance Trial Instance ID of " +
                             $"{exhaustiveSearchInstanceTrialInstance.Id}. " +
                             $"Has reached a layer depth limit of {layerDepthLimit}.");

                    break;
                }

                layers.Add(1);
                layers[^1] = 1;

                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive " +
                    $"Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    $"Trial instance count {i}. Has increased layers to " +
                    $" {layers.Count} to width of {layers[^1]}.");
            }
            else
            {
                layers[^1] += 1;

                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive " +
                    $"Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    $"Trial instance count {i}. Has kept layers at " +
                    $" {layers.Count} and increased width to {layers[^1]}.");
            }

            log.Info(
                $"Exhaustive Training: Trial instance count {i}. Has created " +
                "an Exhaustive Search Instance Trial Instance ID of " +
                $"{exhaustiveSearchInstanceTrialInstance.Id} " +
                $"topology complexity or weights count is {thisTopologyComplexity}.");

            if (thisTopologyComplexity > topologyComplexityLimit)
            {
                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search " +
                    "Instance Trial Instance ID of " +
                    $"{exhaustiveSearchInstanceTrialInstance.Id} " +
                    $"topology complexity or weights count of {thisTopologyComplexity} has exceeded limits.");

                break;
            }

            topologiesSinceNoImproveCounter += 1;

            log.Info(
                $"Exhaustive Training: Trial instance count {i}. " +
                "Has created an Exhaustive Search Instance Trial Instance ID of " +
                $"{exhaustiveSearchInstanceTrialInstance.Id} " +
                $"{topologiesSinceNoImproveCounter} trials since improvement in the evolution of the topology.");
        }

        var trainerFinalise = new LevenbergMarquardtLearning(bestTopologyNetwork);
        for (var j = 0; j < topologyFinalEpochs; j++)
        {
            log.Info(
                $"Exhaustive Training: Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} starting the training of finalise EPOCH " +
                j + ".");

            var sw = new Stopwatch();
            sw.Start();

            trainerFinalise.RunEpoch(dataTraining, outputsTraining);

            sw.Stop();

            log.Info(
                $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                $"Trial instance count {i} the finalise EPOCH {j} in {sw.ElapsedMilliseconds} ms.");

            sw.Reset();
        }

        log.Info(
            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
            $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
            "Finalisation done  Will calculate the final score from this topology evolution.");

        performance.CalculatePerformance(_performance.CalculateScores(bestTopologyNetwork, dataTesting),
            outputsTesting,
            validationTestingActivationThreshold);

        bestPerformance = performance;

        log.Info(
            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
            $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
            $"Finalisation has a score of{bestPerformance.Score}.  Will store the finalisation trial in the database.");

        var modelExhaustiveSearchInstanceTrialInstanceTopologyTrialFinalisation =
            new ExhaustiveSearchInstanceTrialInstanceTopologyTrial
            {
                ExhaustiveSearchInstanceTrialInstanceId = exhaustiveSearchInstanceTrialInstance
                    .Id,
                TrialsSinceImprovement = topologiesSinceNoImproveCounter,
                TopologyTrials = topologyTrials,
                Layer = layers.Count,
                Neurons = layers[^1],
                Score = bestScore,
                Finalisation = 1
            };

        repositoryExhaustiveSearchInstanceTrialInstanceTopologyTrial.Insert(
            modelExhaustiveSearchInstanceTrialInstanceTopologyTrialFinalisation);

        log.Info(
            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
            $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
            "Has finished evolving topology.  Returning best topology complexity {topologyComplexity}.");
    }

    private int[] LayersParamsArray(IReadOnlyList<int> layers)
    {
        var layersArray = new int[layers.Count + 1];
        for (var j = 0; j < layers.Count; j++) layersArray[j] = layers[j];

        layersArray[^1] = 1;
        return layersArray;
    }

    private void OptimiseData(
        ILog log,
        DbContext dbContext,
        int activationFunctionExplorationEpochs,
        int minVariableCount,
        int maxVariableCount,
        ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
        double trainingDataSamplePercentage,
        double crossValidationDataSamplePercentage,
        double testingDataSamplePercentage,
        double validationTestingActivationThreshold,
        out IActivationFunction bestActivationFunctionObject,
        out Dictionary<int, TrialVariable> bestTrialVariables,
        out double[][] bestDataTraining,
        out double[][] bestDataCrossValidation,
        out double[][] bestDataTesting,
        out double[][] bestOutputsTraining,
        out double[][] bestOutputsCrossValidation,
        out double[][] bestOutputsTesting
    )
    {
        var bestSensitivityAnalysisScore = 0d;
        Dictionary<int, TrialVariable> trialVariables = new();
        var removedVariablesCount = 0;
        Dictionary<int, TrialVariable> trialVariablesSnapshot = null;
        Dictionary<int, TrialVariable> originalVariables = null;
        IActivationFunction activationFunctionSnapshot = null;

        log.Info(
            $"Exhaustive Training: Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
            " will proceed to optimise the dataset in a perpetual loop.");

        while (true)
        {
            if (trialVariables.Count < minVariableCount)
            {
                log.Info(
                    $"Exhaustive Training: Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                    " does not have enough variables so is establishing a random set of trial variables.");

                var randomTrialVariableCount =
                    GetRandomVariableCount(_variables.Count, minVariableCount, maxVariableCount);

                log.Info(
                    $"Exhaustive Training: Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                    $" will create with {randomTrialVariableCount} random variables.  Will proceed to select them in random order.");

                trialVariables = SelectVariables(log, _dbContext,
                    exhaustiveSearchInstanceTrialInstance.Id,
                    randomTrialVariableCount);

                originalVariables = (Dictionary<int, TrialVariable>)trialVariables.DeepMemberwiseClone();

                removedVariablesCount = 0;

                trialVariablesSnapshot = new Dictionary<int, TrialVariable>();

                activationFunctionSnapshot = null;

                bestSensitivityAnalysisScore = 0;

                bestTrialVariables = null;

                log.Info(
                    $"Exhaustive Training: Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} will create a model with {randomTrialVariableCount} variables. Will now proceed to split dataset.");
            }

            var reducedDataToTestActivationFunction = ReduceDataForOnlyTrialVariables(trialVariables);

            log.Info(
                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                $"reduced array to height {reducedDataToTestActivationFunction.Length} by width {reducedDataToTestActivationFunction[0].Length}.  " +
                "Will now split for training, cross validation and testing data");

            SplitData(log, trainingDataSamplePercentage,
                crossValidationDataSamplePercentage,
                testingDataSamplePercentage,
                exhaustiveSearchInstanceTrialInstance,
                reducedDataToTestActivationFunction,
                _output,
                out var dataTraining,
                out var outputsTraining,
                out var dataCrossValidation,
                out var outputsCrossValidation,
                out var dataTesting,
                out _);

            log.Info(
                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} has split datasets.  " +
                $"Counts are Testing:{dataTraining.Length}, Cross Validation: {dataCrossValidation.Length} and Testing {dataTesting.Length}. " +
                "Will now select activation functions.");

            SearchForBestActivationFunction(_performance,
                trialVariables,
                activationFunctionExplorationEpochs,
                exhaustiveSearchInstanceTrialInstance,
                dataTraining, outputsTraining, dataCrossValidation,
                outputsCrossValidation, validationTestingActivationThreshold,
                out var bestTopologyNetwork,
                out var bestActivationFunction,
                out var bestActivationFunctionScore);

            log.Info(
                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                $" has found an activation function {bestActivationFunction}. Will proceed to perform sensitivity analysis.");

            var sensitivityAnalysis = PerformSensitivityAnalysisAndStoreForVariableSelection(log,
                exhaustiveSearchInstanceTrialInstance.Id,
                dbContext,
                bestTopologyNetwork, trialVariables, dataCrossValidation);

            log.Info(
                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                $" The best activation score is {bestActivationFunctionScore} and the best Sensitivity score is {bestSensitivityAnalysisScore}" +
                $" which will evaluate to {bestActivationFunctionScore > bestSensitivityAnalysisScore} to proceed to remove the least sensitive variable from the trial.");

            if (bestActivationFunctionScore > bestSensitivityAnalysisScore)
            {
                bestSensitivityAnalysisScore = bestActivationFunctionScore;

                activationFunctionSnapshot = (IActivationFunction)bestActivationFunction.DeepMemberwiseClone();
                trialVariablesSnapshot = (Dictionary<int, TrialVariable>)trialVariables.DeepMemberwiseClone();

                log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                    $" The best activation score is {bestActivationFunctionScore} is about to remove key {sensitivityAnalysis.ElementAt(removedVariablesCount).Key}.  Deep copies of the best " +
                    " so far has been taken to allow revert as there is no certainty that this next trial will be better.");

                trialVariables.Remove(sensitivityAnalysis.ElementAt(removedVariablesCount).Key);
                removedVariablesCount += 1;

                log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                    $" the removed variable account for sensitivity is {removedVariablesCount}.");
            }
            else
            {
                log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                    " no longer gets improvement from removing variables that are not sensitive.  " +
                    "Will revert to deep copy before last removed variable and store the final removed variables removed in the database.");

                bestTrialVariables = trialVariablesSnapshot;
                bestActivationFunctionObject = activationFunctionSnapshot;

                var repository = new ExhaustiveSearchInstanceTrialInstanceVariableRepository(_dbContext);

                if (originalVariables != null)
                    for (var j = 0; j < originalVariables.Count - 1; j++)
                        if (!bestTrialVariables.ContainsKey(originalVariables.ElementAt(j).Key))
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                                $" variable {originalVariables.ElementAt(j).Key} has been removed,  will mark as removed in the database.");

                            repository.UpdateAsRemovedByExhaustiveSearchInstanceVariableId(
                                _variables[originalVariables.ElementAt(j).Key].ExhaustiveSearchInstanceVariableId,
                                exhaustiveSearchInstanceTrialInstance.Id);

                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                                $" variable {originalVariables.ElementAt(j).Key}has been removed in the database for this trial. Will reduce data for the trial variables only.");
                        }
                        else
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                                $" variable {originalVariables.ElementAt(j).Key} has not been removed.  Will reduce data for the trial variables only.");
                        }

                var reducedDataAfterOptimisation = ReduceDataForOnlyTrialVariables(bestTrialVariables);

                log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                    $" the array size is height {reducedDataAfterOptimisation.Length} by width {reducedDataAfterOptimisation[0].Length}.");

                SplitData(log, trainingDataSamplePercentage,
                    crossValidationDataSamplePercentage,
                    testingDataSamplePercentage,
                    exhaustiveSearchInstanceTrialInstance,
                    reducedDataAfterOptimisation,
                    _output,
                    out bestDataTraining,
                    out bestOutputsTraining,
                    out bestDataCrossValidation,
                    out bestOutputsCrossValidation,
                    out bestDataTesting,
                    out bestOutputsTesting);

                log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} has split datasets.  " +
                    $"Counts are Testing:{dataTraining.Length}, Cross Validation: {dataCrossValidation.Length} and Testing {dataTesting.Length}. " +
                    "breaking search and returning method.");

                return;
            }
        }
    }

    private Dictionary<int, double> PerformSensitivityAnalysisAndStoreForVariableSelection(ILog log,
        int exhaustiveSearchInstanceTrialInstanceId,
        DbContext dbContext,
        ActivationNetwork bestTopologyNetwork,
        Dictionary<int, TrialVariable> trialVariables,
        double[][] dataCrossValidation)
    {
        log.Info(
            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstanceId} " +
            " is starting sensitivity analysis.");

        var sensitivityAnalysis = PerformSensitivityAnalysis(log,
            exhaustiveSearchInstanceTrialInstanceId,
            bestTopologyNetwork,
            trialVariables, dataCrossValidation,
            _performance.CalculateScores(bestTopologyNetwork, dataCrossValidation));

        log.Info(
            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstanceId} " +
            " has finished the sensitivity analysis.  Will store in the database.");

        StoreSensitivityForVariableSelection(dbContext, sensitivityAnalysis, trialVariables);

        log.Info(
            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstanceId} " +
            " has stored sensitivity.");

        return sensitivityAnalysis;
    }

    private void StoreSensitivityForVariableSelection(DbContext dbContext,
        Dictionary<int, double> sensitivityAnalysis,
        IReadOnlyDictionary<int, TrialVariable> trialVariables)
    {
        var repository = new ExhaustiveSearchInstanceTrialInstanceSensitivityRepository(dbContext);

        foreach (var (key, value) in sensitivityAnalysis)
        {
            var model =
                new ExhaustiveSearchInstanceTrialInstanceSensitivity
                {
                    Sensitivity = value,
                    ExhaustiveSearchInstanceVariableId = trialVariables[key].ExhaustiveSearchInstanceVariableId
                };

            repository.Insert(model);
        }
    }

    private Dictionary<int, double> PerformSensitivityAnalysis(
        ILog log,
        int exhaustiveSearchInstanceTrialInstanceId,
        ActivationNetwork bestTrialTopologyNetworkObject,
        Dictionary<int, TrialVariable> trialVariables, double[][] dataCrossValidationOrTesting,
        double[] baseline)
    {
        var sensitivityTrialVariables = new Dictionary<int, double>();

        for (var j = 0; j < trialVariables.Count; j++)
        {
            var outputSensitivityValues =
                _performance.Sensitivity(_seeded, bestTrialTopologyNetworkObject, dataCrossValidationOrTesting, j,
                    baseline);

            var sensitivity = outputSensitivityValues.Mean();

            sensitivityTrialVariables.Add(trialVariables.ElementAt(j).Key, sensitivity);
        }

        var sorted = from pair in sensitivityTrialVariables
            orderby Math.Abs(pair.Value)
            select pair;

        var sortedDictionary = sorted.ToDictionary(p => p.Key, p => p.Value);

        WriteSensitivityLogString(log, sortedDictionary, exhaustiveSearchInstanceTrialInstanceId);

        return sortedDictionary;
    }

    private void WriteSensitivityLogString(ILog log, Dictionary<int, double> sortedDictionary,
        int exhaustiveSearchInstanceTrialInstanceId)
    {
        var sensitivityLogString =
            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstanceId}. " +
            " has created sensitivity ranking as ";

        var first = true;
        foreach (var (key, value) in sortedDictionary)
        {
            if (first)
                first = false;
            else
                sensitivityLogString += ",";

            sensitivityLogString += $" key {key} sensitivity {value}";
        }

        sensitivityLogString += ".";
        log.Info(sensitivityLogString);
    }

    private void SearchForBestActivationFunction(Performance performance,
        Dictionary<int, TrialVariable> trialVariables,
        int activationFunctionExplorationEpochs,
        ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
        double[][] dataTraining, double[][] outputsTraining,
        double[][] dataCrossValidation, double[][] outputsCrossValidation,
        double validationTestingActivationThreshold,
        out ActivationNetwork bestTrialTopologyNetwork,
        out IActivationFunction bestActivationFunction,
        out double bestScore)
    {
        bestScore = 0;
        bestTrialTopologyNetwork = null;
        bestActivationFunction = null;

        var repositoryExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial =
            new ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository(_dbContext);

        IActivationFunction trialTopologyFunction = new GaussianFunction();

        var trialTopologyNetwork = DefaultFirstActivationFunction(trialVariables,
            exhaustiveSearchInstanceTrialInstance,
            trialTopologyFunction,
            out var exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial);

        for (var j = 1; j < 11; j++)
        {
            switch (j)
            {
                case 1:
                {
                    trialTopologyFunction = new BipolarSigmoidFunction(1);

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    _log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Bipolar Sigmoid Function 1.");

                    break;
                }

                case 2:
                {
                    trialTopologyFunction = new BipolarSigmoidFunction(2);

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    _log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Bipolar Sigmoid Function 2.");

                    break;
                }

                case 3:
                {
                    trialTopologyFunction = new ThresholdFunction();

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    _log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Threshold Function.");

                    break;
                }

                case 4:
                {
                    trialTopologyFunction = new SigmoidFunction(1);

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    _log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Sigmoid Function 1.");

                    break;
                }

                case 5:
                {
                    trialTopologyFunction = new SigmoidFunction(2);

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    _log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Sigmoid Function 2.");

                    break;
                }

                case 6:
                {
                    trialTopologyFunction = new LinearFunction(1);

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    _log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Linear Function 1.");

                    break;
                }

                case 7:
                {
                    trialTopologyFunction = new LinearFunction(2);

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    _log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Linear Function 2.");

                    break;
                }

                case 8:
                {
                    trialTopologyFunction = new RectifiedLinearFunction();

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    _log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Rectified Linear Function.");

                    break;
                }

                case 9:
                {
                    trialTopologyFunction = new IdentityFunction();

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    _log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Identity Linear Function.");

                    break;
                }

                case 10:
                {
                    trialTopologyFunction = new BernoulliFunction();

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    _log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Bernoulli Linear Function.");

                    break;
                }
            }

            exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial.ActivationFunctionId = j;
            exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial =
                repositoryExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial
                    .Insert(exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial);

            _log.Info(
                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}has randomised topology weights for testing the Activation Function.");

            var trainer =
                new LevenbergMarquardtLearning(TopologyRandomise(trialTopologyNetwork));

            for (var k = 1; k < activationFunctionExplorationEpochs; k++)
            {
                _log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is performing EPOCH {k}");

                var sw = new Stopwatch();
                sw.Start();

                trainer.RunEpoch(dataTraining, outputsTraining);

                sw.Stop();
                sw.Reset();

                _log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} has finished EPOCH {k} in {sw.ElapsedMilliseconds} ms.");

                var outputs = performance.CalculateScores(trialTopologyNetwork, dataCrossValidation);

                _log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is calculating performance for EPOCH {k} as a classification model.");

                var thisScore = performance.CalculatePerformance(outputs, outputsCrossValidation,
                    validationTestingActivationThreshold);

                _log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} and the score is {thisScore},  this will be tested against the best score so far which is {bestScore}.");

                if (thisScore > bestScore)
                {
                    bestTrialTopologyNetwork = (ActivationNetwork)trialTopologyNetwork.DeepMemberwiseClone();
                    bestActivationFunction = (IActivationFunction)trialTopologyFunction.DeepMemberwiseClone();
                    bestScore = thisScore;

                    _log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} calculated performance for EPOCH {k} and the score is {bestScore}. This is the best score so far in sensitivity analysis.  The model has been saved as best.");
                }
                else
                {
                    _log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} calculated performance for EPOCH {k} and the score is {bestScore}.  This is not the best score so far.");
                }
            }
        }
    }

    private ActivationNetwork DefaultFirstActivationFunction(Dictionary<int, TrialVariable> trialVariables,
        ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
        IActivationFunction trialTopologyFunction,
        out ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial
            exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial)
    {
        var trialTopologyNetwork = new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
            MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

        exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial =
            new ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial
            {
                ExhaustiveSearchInstanceTrialInstanceId =
                    exhaustiveSearchInstanceTrialInstance.Id
            };

        _log.Info(
            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Gaussian Function.");
        return trialTopologyNetwork;
    }

    private double[][] ReduceDataForOnlyTrialVariables(Dictionary<int, TrialVariable> trialVariables)
    {
        var reducedData = new double[_data.Length][];
        for (var j = 0; j < reducedData.Length; j++)
        {
            reducedData[j] = new double[trialVariables.Count];

            for (var k = 0; k < trialVariables.Count - 1; k++)
            {
                reducedData[j][k] = _data[j][trialVariables.ElementAt(k).Key];
                k += 1;
            }
        }

        return reducedData;
    }

    private ActivationNetwork TopologyRandomise(ActivationNetwork topologyNetwork)
    {
        var topologyRandomise = new NguyenWidrow(topologyNetwork);
        topologyRandomise.Randomize();
        return topologyNetwork;
    }

    private void SplitData(
        ILog log,
        double trainingDataSamplePercentage,
        double crossValidationDataSamplePercentage,
        double testingDataSamplePercentage,
        ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
        IReadOnlyList<double[]> data,
        IReadOnlyList<double[]> output,
        out double[][] dataTraining,
        out double[][] outputsTraining,
        out double[][] dataCrossValidation,
        out double[][] outputsCrossValidation,
        out double[][] dataTesting,
        out double[][] outputsTesting
    )
    {
        var trainingLength = (int)(output.Count * trainingDataSamplePercentage);
        var crossValidationLength = (int)(output.Count * crossValidationDataSamplePercentage);
        var testingLength = (int)(output.Count * testingDataSamplePercentage);

        log.Info(
            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID " +
            $"of {exhaustiveSearchInstanceTrialInstance.Id} is separating the dataset. " +
            $"There are {trainingLength} training records, {crossValidationLength} cross validation records " +
            $"and {testingLength} testing records.  Will split the dataset.");

        dataTraining = new double[trainingLength - 1][];
        dataCrossValidation = new double[crossValidationLength - 1][];
        dataTesting = new double[testingLength + crossValidationLength][];
        outputsTraining = new double[trainingLength - 1][];

        outputsCrossValidation = new double[crossValidationLength - 1][];
        outputsTesting = new double[testingLength + crossValidationLength - 1][];

        dataTraining = SplitArray(data, 0, trainingLength);
        outputsTraining = SplitArray(output, 0, trainingLength);

        dataCrossValidation = SplitArray(data, trainingLength, crossValidationLength);
        outputsCrossValidation = SplitArray(output, trainingLength, testingLength + crossValidationLength);

        dataTesting = SplitArray(data, crossValidationLength, testingLength);
        outputsTesting = SplitArray(output, crossValidationLength, testingLength + crossValidationLength);
    }

    private Dictionary<int, TrialVariable> SelectVariables(
        ILog log,
        DbContext dbContext,
        int exhaustiveSearchInstanceTrialInstanceId, int randomTrialVariableCount)
    {
        var repository = new ExhaustiveSearchInstanceTrialInstanceVariableRepository(dbContext);
        repository.DeleteAllByExhaustiveSearchInstanceTrialInstanceId(exhaustiveSearchInstanceTrialInstanceId);

        var trialVariables = new Dictionary<int, TrialVariable>();
        for (var j = 0; j < randomTrialVariableCount; j++)
        {
            var randomVariable = _seeded.Next(0, randomTrialVariableCount - 1);

            log.Info(
                "Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of " +
                $"{exhaustiveSearchInstanceTrialInstanceId} is separating the dataset.  Has selected {randomVariable} as" +
                " a random index.  Will check if it has already been added as a trial variable.");

            if (!trialVariables.ContainsKey(randomVariable))
            {
                var trialVariable = new TrialVariable
                {
                    Name = _variables[randomVariable].Name,
                    Mean = _variables[randomVariable].Mean,
                    Sd = _variables[randomVariable].Sd,
                    Mode = _variables[randomVariable].Mode,
                    Min = _variables[randomVariable].Min,
                    Max = _variables[randomVariable].Max,
                    ExhaustiveSearchInstanceVariableId =
                        _variables[randomVariable].ExhaustiveSearchInstanceVariableId,
                    TriangularDistribution = _variables[randomVariable].TriangularDistribution,
                    NormalisationTypeId = _variables[randomVariable].NormalisationType,
                    ProcessingTypeId = _variables[randomVariable].ProcessingTypeId
                };

                trialVariables.Add(randomVariable, trialVariable);

                log.Info(
                    "Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of " +
                    $"{exhaustiveSearchInstanceTrialInstanceId} is separating the dataset.  " +
                    $"Has added Exhaustive Search Instance Variable Id {_variables[randomVariable].ExhaustiveSearchInstanceVariableId})." +
                    "Will also store a record in the database.");

                var exhaustiveSearchInstanceTrialInstanceVariable =
                    new ExhaustiveSearchInstanceTrialInstanceVariable
                    {
                        VariableSequence = j,
                        ExhaustiveSearchInstanceVariableId =
                            _variables[randomVariable].ExhaustiveSearchInstanceVariableId,
                        ExhaustiveSearchInstanceTrialInstanceId = exhaustiveSearchInstanceTrialInstanceId
                    };

                trialVariables[randomVariable].ExhaustiveSearchInstanceTrialInstanceVariableId
                    = repository.Insert(exhaustiveSearchInstanceTrialInstanceVariable)
                        .Id;

                log.Info(
                    "Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of " +
                    $"{exhaustiveSearchInstanceTrialInstanceId} is separating the dataset.  " +
                    $"Has added Exhaustive Search Instance Variable Id {_variables[randomVariable].ExhaustiveSearchInstanceVariableId})." +
                    " has created Exhaustive Search Instance Trial Instance Variable Id " +
                    $"{trialVariables[randomVariable].ExhaustiveSearchInstanceTrialInstanceVariableId}.");
            }
            else
            {
                log.Info(
                    "Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of " +
                    $"{exhaustiveSearchInstanceTrialInstanceId} is separating the dataset.  " +
                    $"Has not added Exhaustive Search Instance Variable Id {_variables[randomVariable].ExhaustiveSearchInstanceVariableId})." +
                    " as it already exist.");
            }

            log.Info(
                "Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of " +
                $"{exhaustiveSearchInstanceTrialInstanceId} is separating the dataset.  " +
                $" Has finished adding trial variables and there are now {trialVariables.Count} available.  Returning method.");
        }

        return trialVariables;
    }

    private double[][] SplitArray(IReadOnlyList<double[]> inputs, int start, int finish)
    {
        var newArray = new double[finish][];
        var j = 0;
        var loopTo = start + finish - 1;
        for (var i = start; i <= loopTo; i++)
        {
            newArray[j] = inputs[i];
            j += 1;
        }

        return newArray;
    }

    private int GetRandomVariableCount(
        int availableVariableCount, int minVariableCount, int maxVariableCount)
    {
        if (availableVariableCount < minVariableCount) return availableVariableCount;

        var value = _seeded.Next(minVariableCount, availableVariableCount);

        if (value < minVariableCount) return minVariableCount;

        return value > maxVariableCount ? maxVariableCount : value;
    }

    private ExhaustiveSearchInstanceTrialInstance InsertExhaustiveSearchInstanceTrialInstance(
        ExhaustiveSearchInstanceTrialInstanceRepository repository,
        int exhaustiveSearchInstanceId)
    {
        var exhaustiveSearchInstanceTrialInstance = new ExhaustiveSearchInstanceTrialInstance
        {
            ExhaustiveSearchInstanceId = exhaustiveSearchInstanceId,
            CreatedDate = DateTime.Now
        };

        return repository.Insert(exhaustiveSearchInstanceTrialInstance);
    }

    private List<ActivationNetworkAnnotation> MapTrialVariableToActivationNetworkAnnotations(
        Dictionary<int, TrialVariable> trialVariables)
    {
        return trialVariables.Select(trialVariable => new ActivationNetworkAnnotation
            {
                Name = trialVariable.Value.Name,
                ExhaustiveSearchInstanceVariableId = trialVariable.Value.ExhaustiveSearchInstanceVariableId,
                Mean = trialVariable.Value.Mean,
                Sd = trialVariable.Value.Sd,
                Max = trialVariable.Value.Max,
                Min = trialVariable.Value.Min,
                Mode = trialVariable.Value.Mode,
                ExhaustiveSearchInstanceTrialInstanceVariableId =
                    trialVariable.Value.ExhaustiveSearchInstanceTrialInstanceVariableId,
                NormalisationTypeId = trialVariable.Value.NormalisationTypeId,
                ProcessingTypeId = trialVariable.Value.ProcessingTypeId
            })
            .ToList();
    }

    private bool IsStoppingOrStopped(int exhaustiveSearchInstanceId)
    {
        return !_repositoryExhaustiveSearchInstance.IsStoppingOrStopped(exhaustiveSearchInstanceId);
    }
}
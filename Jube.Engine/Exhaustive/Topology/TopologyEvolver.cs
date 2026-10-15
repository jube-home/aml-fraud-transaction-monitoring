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
using System.Threading;
using System.Threading.Tasks;
using Accord.Neuro;
using Accord.Neuro.Learning;
using Jube.Data.Poco;
using Jube.Engine.Exhaustive.Activation;
using Jube.Engine.Exhaustive.Algorithms;
using Jube.Engine.Exhaustive.Interfaces;
using Jube.Engine.Exhaustive.Algorithms.Models;
using log4net;

namespace Jube.Engine.Exhaustive.Topology
{
    internal class TopologyEvolver(
        ITrialRecorder recorder,
        Performance performance,
        ILog log)
    {
        internal async Task<(ActivationNetwork ActivationNetwork, Performance Performance, int BestTopologyComplexity)>
            EvolveTopologyAsync(
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
                int layerDepthLimit, int topologyComplexityLimit, CancellationToken token = default)
        {
            ActivationNetwork bestTopologyNetwork = null;
            var bestTopologyComplexity = 0;

            var bestScore = 0d;
            var topologiesSinceNoImproveCounter = 0;
            const int topologyTrials = 1;

            var layers = new List<int>
            {
                1
            };

            while (topologiesSinceNoImproveCounter < topologySinceImprovementLimit)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. {topologiesSinceNoImproveCounter} topologies processed since last improvement.");
                }

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
                    await recorder.InsertTopologyTrialAsync(
                        exhaustiveSearchInstanceTrialInstanceTopologyTrial, token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                        $" created entry in the database for {exhaustiveSearchInstanceTrialInstanceTopologyTrial.ExhaustiveSearchInstanceTrialInstanceId}.  Will construct topology network and trainer with {layers.Count} layers" +
                        $" and activation function {activationFunction}.  Weights are randomised on the construction of the trainer.");
                }

                var topologyNetwork = new ActivationNetwork(activationFunction, trialVariables.Count,
                    ActivationAnnotations.MapTrialVariableToActivationNetworkAnnotations(trialVariables),
                    TopologyLayers.LayersParamsArray(layers));

                var trainerExploration =
                    new LevenbergMarquardtLearning(TopologyLayers.TopologyRandomise(topologyNetwork));

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                        $" created entry in the database for {exhaustiveSearchInstanceTrialInstanceTopologyTrial.ExhaustiveSearchInstanceTrialInstanceId}. Trainer is ready.");
                }

                RunExplorationEpochs(trainerExploration, topologyExplorationEpochs, i,
                    exhaustiveSearchInstanceTrialInstance, dataTraining, outputsTraining);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                        " has finished topology training and will calculate the performance of the model.");
                }

                var thisScore = performance.CalculatePerformance(
                    performance.CalculateScores(topologyNetwork, dataCrossValidation),
                    outputsCrossValidation,
                    validationTestingActivationThreshold);

                var thisTopologyComplexity = (int)(topologyNetwork.Layers
                                                       .Sum(layer =>
                                                           (long)layer.Neurons.Length * (layer.InputsCount + 1))
                                                   + topologyNetwork.Layers.Length);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                        $" has a score of {thisScore}.  The best score is {bestScore} and when tested against {thisScore} evaluates to {thisScore > bestScore}.  The topology complexity is {bestTopologyComplexity}.");
                }

                if (thisScore > bestScore)
                {
                    bestScore = thisScore;
                    bestTopologyComplexity = thisTopologyComplexity;

                    bestTopologyNetwork = (ActivationNetwork)topologyNetwork.DeepMemberwiseClone();
                    topologiesSinceNoImproveCounter = 0;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                            " has taken a deep copy of the best model and has reset the topologiesSinceNoImproveCounter to 0.");
                    }
                }

                if (Math.Abs(bestScore - 1) < 0.0001)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} the topology has arrived at a perfect fit (too good in fact).  The training will terminate for this iteration.");
                    }

                    break;
                }

                if (!TopologyLayers.TryGrowLayers(layers, topologyNetwork.Layers[layers.Count - 1].Neurons.Length,
                        trialVariables.Count, layerWidthLimitInputLayerFactor, layerDepthLimit))
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive " +
                                 "Search Instance Trial Instance ID of " +
                                 $"{exhaustiveSearchInstanceTrialInstance.Id}. " +
                                 $"Has reached a layer depth limit of {layerDepthLimit}.");
                    }

                    break;
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created " +
                        "an Exhaustive Search Instance Trial Instance ID of " +
                        $"{exhaustiveSearchInstanceTrialInstance.Id} " +
                        $"topology complexity or weights count is {thisTopologyComplexity}.");
                }

                if (thisTopologyComplexity > topologyComplexityLimit)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search " +
                                 "Instance Trial Instance ID of " +
                                 $"{exhaustiveSearchInstanceTrialInstance.Id} " +
                                 $"topology complexity or weights count of {thisTopologyComplexity} has exceeded limits.");
                    }

                    break;
                }

                topologiesSinceNoImproveCounter += 1;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. " +
                        "Has created an Exhaustive Search Instance Trial Instance ID of " +
                        $"{exhaustiveSearchInstanceTrialInstance.Id} " +
                        $"{topologiesSinceNoImproveCounter} trials since improvement in the evolution of the topology.");
                }
            }

            RunFinaliseEpochs(bestTopologyNetwork, topologyFinalEpochs, i, exhaustiveSearchInstanceTrialInstance,
                dataTraining, outputsTraining);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    "Finalisation done  Will calculate the final score from this topology evolution.");
            }

            performance.CalculatePerformance(performance.CalculateScores(bestTopologyNetwork, dataTesting),
                outputsTesting,
                validationTestingActivationThreshold);

            var bestPerformance = performance;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    $"Finalisation has a score of{bestPerformance.Score}.  Will store the finalisation trial in the database.");
            }

            await RecordFinalisationAsync(exhaustiveSearchInstanceTrialInstance, topologiesSinceNoImproveCounter,
                topologyTrials, layers, bestScore, token).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    "Has finished evolving topology.  Returning best topology complexity {topologyComplexity}.");
            }

            return (bestTopologyNetwork, bestPerformance, bestTopologyComplexity);
        }

        private void RunExplorationEpochs(LevenbergMarquardtLearning trainerExploration, int topologyExplorationEpochs,
            int i, ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            double[][] dataTraining, double[][] outputsTraining)
        {
            for (var k = 0; k < topologyExplorationEpochs; k++)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} starting the training of EPOCH " +
                        k + ".");
                }

                var sw = new Stopwatch();
                sw.Start();

                trainerExploration.RunEpoch(dataTraining, outputsTraining);

                sw.Stop();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. Trial instance count {i} the EPOCH {k} in {sw.ElapsedMilliseconds} ms.");
                }

                sw.Reset();
            }
        }

        private void RunFinaliseEpochs(ActivationNetwork bestTopologyNetwork, int topologyFinalEpochs, int i,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            double[][] dataTraining, double[][] outputsTraining)
        {
            var trainerFinalise = new LevenbergMarquardtLearning(bestTopologyNetwork);

            for (var j = 0; j < topologyFinalEpochs; j++)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} starting the training of finalise EPOCH " +
                        j + ".");
                }

                var sw = new Stopwatch();
                sw.Start();

                trainerFinalise.RunEpoch(dataTraining, outputsTraining);

                sw.Stop();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                        $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                        $"Trial instance count {i} the finalise EPOCH {j} in {sw.ElapsedMilliseconds} ms.");
                }

                sw.Reset();
            }
        }

        private Task RecordFinalisationAsync(
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            int topologiesSinceNoImproveCounter, int topologyTrials, List<int> layers, double bestScore,
            CancellationToken token)
        {
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

            return recorder.InsertTopologyTrialAsync(
                modelExhaustiveSearchInstanceTrialInstanceTopologyTrialFinalisation, token);
        }
    }
}
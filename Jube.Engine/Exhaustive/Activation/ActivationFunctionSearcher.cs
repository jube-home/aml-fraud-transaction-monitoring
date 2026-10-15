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

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Accord.Neuro;
using Accord.Neuro.ActivationFunctions;
using Accord.Neuro.Learning;
using Jube.Data.Poco;
using Jube.Engine.Exhaustive.Algorithms;
using Jube.Engine.Exhaustive.Interfaces;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Exhaustive.Topology;
using log4net;

namespace Jube.Engine.Exhaustive.Activation
{
    internal class ActivationFunctionSearcher(
        ITrialRecorder recorder,
        Performance performance,
        ILog log)
    {
        internal async
            Task<(ActivationNetwork bestTrialTopologyNetwork, IActivationFunction bestActivationFunction, double
                bestScore)> SearchForBestActivationFunctionAsync(Dictionary<int, TrialVariable> trialVariables,
                int activationFunctionExplorationEpochs,
                ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
                double[][] dataTraining, double[][] outputsTraining,
                double[][] dataCrossValidation, double[][] outputsCrossValidation,
                double validationTestingActivationThreshold, CancellationToken token = default)
        {
            var bestScore = 0d;
            ActivationNetwork bestTrialTopologyNetwork = null;
            IActivationFunction bestActivationFunction = null;

            IActivationFunction trialTopologyFunction = new GaussianFunction();

            var trialTopologyNetwork = DefaultFirstActivationFunction(trialVariables,
                exhaustiveSearchInstanceTrialInstance,
                trialTopologyFunction,
                out var exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial);

            for (var j = 1; j < 11; j++)
            {
                SelectCandidate(j, trialVariables, exhaustiveSearchInstanceTrialInstance,
                    ref trialTopologyFunction, ref trialTopologyNetwork);

                exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial.ActivationFunctionId = j;

                exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial =
                    await recorder
                        .InsertActivationFunctionTrialAsync(
                            exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial, token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}has randomised topology weights for testing the Activation Function.");
                }

                var trainer =
                    new LevenbergMarquardtLearning(TopologyLayers.TopologyRandomise(trialTopologyNetwork));

                for (var k = 1; k < activationFunctionExplorationEpochs; k++)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is performing EPOCH {k}");
                    }

                    var sw = new Stopwatch();
                    sw.Start();

                    trainer.RunEpoch(dataTraining, outputsTraining);

                    sw.Stop();
                    sw.Reset();

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} has finished EPOCH {k} in {sw.ElapsedMilliseconds} ms.");
                    }

                    var outputs = performance.CalculateScores(trialTopologyNetwork, dataCrossValidation);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is calculating performance for EPOCH {k} as a classification model.");
                    }

                    var thisScore = performance.CalculatePerformance(outputs, outputsCrossValidation,
                        validationTestingActivationThreshold);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} and the score is {thisScore},  this will be tested against the best score so far which is {bestScore}.");
                    }

                    if (thisScore > bestScore)
                    {
                        bestTrialTopologyNetwork = (ActivationNetwork)trialTopologyNetwork.DeepMemberwiseClone();
                        bestActivationFunction = (IActivationFunction)trialTopologyFunction.DeepMemberwiseClone();
                        bestScore = thisScore;

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} calculated performance for EPOCH {k} and the score is {bestScore}. This is the best score so far in sensitivity analysis.  The model has been saved as best.");
                        }
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} calculated performance for EPOCH {k} and the score is {bestScore}.  This is not the best score so far.");
                        }
                    }
                }
            }

            return (bestTrialTopologyNetwork, bestActivationFunction, bestScore);
        }

        private ActivationNetwork DefaultFirstActivationFunction(Dictionary<int, TrialVariable> trialVariables,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            IActivationFunction trialTopologyFunction,
            out ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial
                exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial)
        {
            var trialTopologyNetwork = new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                ActivationAnnotations.MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

            exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial =
                new ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial
                {
                    ExhaustiveSearchInstanceTrialInstanceId =
                        exhaustiveSearchInstanceTrialInstance.Id
                };

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Gaussian Function.");
            }

            return trialTopologyNetwork;
        }

        private void SelectCandidate(int j, Dictionary<int, TrialVariable> trialVariables,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            ref IActivationFunction trialTopologyFunction, ref ActivationNetwork trialTopologyNetwork)
        {
            switch (j)
            {
                case 1:
                {
                    trialTopologyFunction = new BipolarSigmoidFunction(1);

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            ActivationAnnotations.MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Bipolar Sigmoid Function 1.");
                    }

                    break;
                }

                case 2:
                {
                    trialTopologyFunction = new BipolarSigmoidFunction(2);

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            ActivationAnnotations.MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Bipolar Sigmoid Function 2.");
                    }

                    break;
                }

                case 3:
                {
                    trialTopologyFunction = new ThresholdFunction();

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            ActivationAnnotations.MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Threshold Function.");
                    }

                    break;
                }

                case 4:
                {
                    trialTopologyFunction = new SigmoidFunction(1);

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            ActivationAnnotations.MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Sigmoid Function 1.");
                    }

                    break;
                }

                case 5:
                {
                    trialTopologyFunction = new SigmoidFunction(2);

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            ActivationAnnotations.MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Sigmoid Function 2.");
                    }

                    break;
                }

                case 6:
                {
                    trialTopologyFunction = new LinearFunction(1);

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            ActivationAnnotations.MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Linear Function 1.");
                    }

                    break;
                }

                case 7:
                {
                    trialTopologyFunction = new LinearFunction(2);

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            ActivationAnnotations.MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Linear Function 2.");
                    }

                    break;
                }

                case 8:
                {
                    trialTopologyFunction = new RectifiedLinearFunction();

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            ActivationAnnotations.MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Rectified Linear Function.");
                    }

                    break;
                }

                case 9:
                {
                    trialTopologyFunction = new IdentityFunction();

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            ActivationAnnotations.MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Identity Linear Function.");
                    }

                    break;
                }

                case 10:
                {
                    trialTopologyFunction = new BernoulliFunction();

                    trialTopologyNetwork =
                        new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                            ActivationAnnotations.MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Bernoulli Linear Function.");
                    }

                    break;
                }
            }
        }
    }
}
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Neuro;
using Jube.Data.Poco;
using Jube.Engine.Exhaustive.Activation;
using Jube.Engine.Exhaustive.Algorithms;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Exhaustive.Interfaces;
using Jube.Engine.Exhaustive.Selection;
using log4net;

namespace Jube.Engine.Exhaustive.Optimisation
{
    internal class DataOptimiser(
        ITrialRecorder recorder,
        Dictionary<int, Variable> variables,
        double[][] output,
        VariableSelector selector,
        SensitivityAnalyser sensitivityAnalyser,
        ActivationFunctionSearcher activationSearcher,
        DataSplitter dataSplitter,
        ILog log,
        double[][] syntheticOutput = null)
    {
        internal async Task<(IActivationFunction bestActivationFunctionObject, Dictionary<int, TrialVariable>
            bestTrialVariables,
            double[][] bestDataTraining,
            double[][] bestDataCrossValidation,
            double[][] bestDataTesting,
            double[][] bestOutputsTraining,
            double[][] bestOutputsCrossValidation,
            double[][] bestOutputsTesting)> OptimiseDataAsync(
            int activationFunctionExplorationEpochs,
            int minVariableCount,
            int maxVariableCount,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            double trainingDataSamplePercentage,
            double crossValidationDataSamplePercentage,
            double testingDataSamplePercentage,
            double validationTestingActivationThreshold,
            CancellationToken token
        )
        {
            var bestSensitivityAnalysisScore = 0d;
            var trialVariables = new Dictionary<int, TrialVariable>();
            var removedVariablesCount = 0;
            Dictionary<int, TrialVariable> trialVariablesSnapshot = null;
            Dictionary<int, TrialVariable> originalVariables = null;
            IActivationFunction activationFunctionSnapshot = null;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                    " will proceed to optimise the dataset in a perpetual loop.");
            }

            while (true)
            {
                token.ThrowIfCancellationRequested();

                if (trialVariables.Count < minVariableCount)
                {
                    int randomTrialVariableCount;

                    (trialVariables, randomTrialVariableCount) = await EstablishRandomTrialVariablesAsync(
                            exhaustiveSearchInstanceTrialInstance, minVariableCount, maxVariableCount, token)
                        .ConfigureAwait(false);

                    originalVariables = (Dictionary<int, TrialVariable>)trialVariables.DeepMemberwiseClone();

                    removedVariablesCount = 0;

                    trialVariablesSnapshot = new Dictionary<int, TrialVariable>();

                    activationFunctionSnapshot = null;

                    bestSensitivityAnalysisScore = 0;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} will create a model with {randomTrialVariableCount} variables. Will now proceed to split dataset.");
                    }
                }

                var reducedDataToTestActivationFunction = selector.ReduceDataForOnlyTrialVariables(trialVariables);
                var reducedSyntheticDataToTestActivationFunction =
                    selector.ReduceSyntheticDataForOnlyTrialVariables(trialVariables);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                        $"reduced array to height {reducedDataToTestActivationFunction.Length} by width {reducedDataToTestActivationFunction[0].Length}.  " +
                        "Will now split for training, cross validation and testing data");
                }

                dataSplitter.SplitData(trainingDataSamplePercentage,
                    crossValidationDataSamplePercentage,
                    testingDataSamplePercentage,
                    exhaustiveSearchInstanceTrialInstance,
                    reducedDataToTestActivationFunction,
                    output,
                    out var dataTraining,
                    out var outputsTraining,
                    out var dataCrossValidation,
                    out var outputsCrossValidation,
                    out var dataTesting,
                    out _,
                    reducedSyntheticDataToTestActivationFunction,
                    syntheticOutput);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} has split datasets.  " +
                        $"Counts are Testing:{dataTraining.Length}, Cross Validation: {dataCrossValidation.Length} and Testing {dataTesting.Length}. " +
                        "Will now select activation functions.");
                }

                var (bestTopologyNetwork,
                    bestActivationFunction,
                    bestActivationFunctionScore) = await activationSearcher.SearchForBestActivationFunctionAsync(
                    trialVariables,
                    activationFunctionExplorationEpochs,
                    exhaustiveSearchInstanceTrialInstance,
                    dataTraining, outputsTraining, dataCrossValidation,
                    outputsCrossValidation, validationTestingActivationThreshold, token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                        $" has found an activation function {bestActivationFunction}. Will proceed to perform sensitivity analysis.");
                }

                var sensitivityAnalysis = await sensitivityAnalyser
                    .PerformSensitivityAnalysisAndStoreForVariableSelectionAsync(
                        exhaustiveSearchInstanceTrialInstance.Id,
                        bestTopologyNetwork, trialVariables, dataCrossValidation, token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                        $" The best activation score is {bestActivationFunctionScore} and the best Sensitivity score is {bestSensitivityAnalysisScore}" +
                        $" which will evaluate to {bestActivationFunctionScore > bestSensitivityAnalysisScore} to proceed to remove the least sensitive variable from the trial.");
                }

                var improved = bestActivationFunctionScore > bestSensitivityAnalysisScore;

                if (improved)
                {
                    bestSensitivityAnalysisScore = bestActivationFunctionScore;

                    activationFunctionSnapshot = (IActivationFunction)bestActivationFunction.DeepMemberwiseClone();
                    trialVariablesSnapshot = (Dictionary<int, TrialVariable>)trialVariables.DeepMemberwiseClone();
                }

                if (improved && trialVariables.Count > minVariableCount)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                            $" The best activation score is {bestActivationFunctionScore} is about to remove key {VariableSelector.LeastSensitiveVariableKey(sensitivityAnalysis)}.  Deep copies of the best " +
                            " so far has been taken to allow revert as there is no certainty that this next trial will be better.");
                    }

                    trialVariables.Remove(VariableSelector.LeastSensitiveVariableKey(sensitivityAnalysis));
                    removedVariablesCount += 1;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                            $" the removed variable account for sensitivity is {removedVariablesCount}.");
                    }
                }
                else
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                            " no longer gets improvement from removing variables that are not sensitive.  " +
                            "Will revert to deep copy before last removed variable and store the final removed variables removed in the database.");
                    }

                    var bestTrialVariables = trialVariablesSnapshot;
                    var bestActivationFunctionObject = activationFunctionSnapshot;

                    await MarkRemovedVariablesAsync(originalVariables, bestTrialVariables,
                        exhaustiveSearchInstanceTrialInstance, token).ConfigureAwait(false);

                    var reducedDataAfterOptimisation = selector.ReduceDataForOnlyTrialVariables(bestTrialVariables);
                    var reducedSyntheticDataAfterOptimisation =
                        selector.ReduceSyntheticDataForOnlyTrialVariables(bestTrialVariables);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                            $" the array size is height {reducedDataAfterOptimisation.Length} by width {reducedDataAfterOptimisation[0].Length}.");
                    }

                    dataSplitter.SplitData(trainingDataSamplePercentage,
                        crossValidationDataSamplePercentage,
                        testingDataSamplePercentage,
                        exhaustiveSearchInstanceTrialInstance,
                        reducedDataAfterOptimisation,
                        output,
                        out var bestDataTraining,
                        out var bestOutputsTraining,
                        out var bestDataCrossValidation,
                        out var bestOutputsCrossValidation,
                        out var bestDataTesting,
                        out var bestOutputsTesting,
                        reducedSyntheticDataAfterOptimisation,
                        syntheticOutput);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} has split datasets.  " +
                            $"Counts are Testing:{dataTraining.Length}, Cross Validation: {dataCrossValidation.Length} and Testing {dataTesting.Length}. " +
                            "breaking search and returning method.");
                    }

                    return (bestActivationFunctionObject,
                        bestTrialVariables,
                        bestDataTraining,
                        bestDataCrossValidation,
                        bestDataTesting,
                        bestOutputsTraining,
                        bestOutputsCrossValidation,
                        bestOutputsTesting);
                }
            }
        }

        private async Task<(Dictionary<int, TrialVariable> TrialVariables, int RandomTrialVariableCount)>
            EstablishRandomTrialVariablesAsync(
                ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
                int minVariableCount, int maxVariableCount, CancellationToken token)
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                    " does not have enough variables so is establishing a random set of trial variables.");
            }

            var randomTrialVariableCount =
                selector.GetRandomVariableCount(variables.Count, minVariableCount, maxVariableCount);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                    $" will create with {randomTrialVariableCount} random variables.  Will proceed to select them in random order.");
            }

            var trialVariables = await selector.SelectVariablesAsync(exhaustiveSearchInstanceTrialInstance.Id,
                randomTrialVariableCount, token).ConfigureAwait(false);

            return (trialVariables, randomTrialVariableCount);
        }

        private async Task MarkRemovedVariablesAsync(Dictionary<int, TrialVariable> originalVariables,
            Dictionary<int, TrialVariable> bestTrialVariables,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance, CancellationToken token)
        {
            if (originalVariables != null)
            {
                for (var j = 0; j < originalVariables.Count; j++)
                {
                    if (!bestTrialVariables.ContainsKey(originalVariables.ElementAt(j).Key))
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                                $" variable {originalVariables.ElementAt(j).Key} has been removed,  will mark as removed in the database.");
                        }

                        await recorder.MarkTrialVariableRemovedAsync(
                            variables[originalVariables.ElementAt(j).Key].ExhaustiveSearchInstanceVariableId,
                            exhaustiveSearchInstanceTrialInstance.Id, token).ConfigureAwait(false);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                                $" variable {originalVariables.ElementAt(j).Key}has been removed in the database for this trial. Will reduce data for the trial variables only.");
                        }
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                                $" variable {originalVariables.ElementAt(j).Key} has not been removed.  Will reduce data for the trial variables only.");
                        }
                    }
                }
            }
        }
    }
}
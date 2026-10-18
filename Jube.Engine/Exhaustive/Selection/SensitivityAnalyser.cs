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
using Accord.Statistics;
using Jube.Data.Poco;
using Jube.Engine.Exhaustive.Algorithms;
using Jube.Engine.Exhaustive.Interfaces;
using Jube.Engine.Exhaustive.Algorithms.Models;
using log4net;

namespace Jube.Engine.Exhaustive.Selection
{
    internal class SensitivityAnalyser(
        ITrialRecorder recorder,
        Performance performance,
        Random seeded,
        ILog log)
    {
        internal async Task<Dictionary<int, double>> PerformSensitivityAnalysisAndStoreForVariableSelectionAsync(
            int exhaustiveSearchInstanceTrialInstanceId,
            ActivationNetwork bestTopologyNetwork,
            Dictionary<int, TrialVariable> trialVariables,
            double[][] dataCrossValidation, CancellationToken token = default)
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstanceId} " +
                    " is starting sensitivity analysis.");
            }

            var sensitivityAnalysis = PerformSensitivityAnalysis(exhaustiveSearchInstanceTrialInstanceId,
                bestTopologyNetwork,
                trialVariables, dataCrossValidation,
                performance.CalculateScores(bestTopologyNetwork, dataCrossValidation));

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstanceId} " +
                    " has finished the sensitivity analysis.  Will store in the database.");
            }

            await StoreSensitivityForVariableSelectionAsync(sensitivityAnalysis, trialVariables, token)
                .ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstanceId} " +
                    " has stored sensitivity.");
            }

            return sensitivityAnalysis;
        }

        private async Task StoreSensitivityForVariableSelectionAsync(Dictionary<int, double> sensitivityAnalysis,
            Dictionary<int, TrialVariable> trialVariables, CancellationToken token = default)
        {
            foreach (var (key, value) in sensitivityAnalysis)
            {
                var model =
                    new ExhaustiveSearchInstanceTrialInstanceSensitivity
                    {
                        Sensitivity = value,
                        ExhaustiveSearchInstanceVariableId = trialVariables[key].ExhaustiveSearchInstanceVariableId
                    };

                await recorder.InsertTrialSensitivityAsync(model, token).ConfigureAwait(false);
            }
        }

        internal Dictionary<int, double> PerformSensitivityAnalysis(
            int exhaustiveSearchInstanceTrialInstanceId,
            ActivationNetwork bestTrialTopologyNetworkObject,
            Dictionary<int, TrialVariable> trialVariables, double[][] dataCrossValidationOrTesting,
            double[] baseline)
        {
            var sensitivityTrialVariables = new Dictionary<int, double>();

            for (var j = 0; j < trialVariables.Count; j++)
            {
                var outputSensitivityValues =
                    Performance.Sensitivity(seeded, bestTrialTopologyNetworkObject, dataCrossValidationOrTesting, j,
                        baseline);

                var sensitivity = outputSensitivityValues.Mean();

                sensitivityTrialVariables.Add(trialVariables.ElementAt(j).Key, sensitivity);
            }

            var sorted = from pair in sensitivityTrialVariables
                orderby Math.Abs(pair.Value)
                select pair;

            var sortedDictionary = sorted.ToDictionary(p => p.Key, p => p.Value);

            WriteSensitivityLogString(sortedDictionary, exhaustiveSearchInstanceTrialInstanceId);

            return sortedDictionary;
        }

        private void WriteSensitivityLogString(Dictionary<int, double> sortedDictionary,
            int exhaustiveSearchInstanceTrialInstanceId)
        {
            var sensitivityLogString =
                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstanceId}. " +
                " has created sensitivity ranking as ";

            var first = true;

            foreach (var (key, value) in sortedDictionary)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sensitivityLogString += ",";
                }

                sensitivityLogString += $" key {key} sensitivity {value}";
            }

            sensitivityLogString += ".";

            if (log.IsInfoEnabled)
            {
                log.Info(sensitivityLogString);
            }
        }
    }
}
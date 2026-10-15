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
using Jube.Data.Poco;
using Jube.Engine.Exhaustive.Interfaces;
using Jube.Engine.Exhaustive.Algorithms.Models;
using log4net;

namespace Jube.Engine.Exhaustive.Selection
{
    internal class VariableSelector(
        ITrialRecorder recorder,
        Random seeded,
        Dictionary<int, Variable> variables,
        double[][] data,
        ILog log,
        double[][] syntheticData = null)
    {
        internal static int LeastSensitiveVariableKey(Dictionary<int, double> sensitivityAscending)
        {
            return sensitivityAscending.First().Key;
        }

        internal double[][] ReduceDataForOnlyTrialVariables(Dictionary<int, TrialVariable> trialVariables)
        {
            return Reduce(data, trialVariables);
        }

        internal double[][] ReduceSyntheticDataForOnlyTrialVariables(Dictionary<int, TrialVariable> trialVariables)
        {
            return syntheticData == null ? null : Reduce(syntheticData, trialVariables);
        }

        private static double[][] Reduce(double[][] source, Dictionary<int, TrialVariable> trialVariables)
        {
            var reducedData = new double[source.Length][];

            for (var j = 0; j < reducedData.Length; j++)
            {
                reducedData[j] = new double[trialVariables.Count];

                var k = 0;

                foreach (var trialVariable in trialVariables)
                {
                    reducedData[j][k] = source[j][trialVariable.Key];
                    k += 1;
                }
            }

            return reducedData;
        }

        internal async Task<Dictionary<int, TrialVariable>> SelectVariablesAsync(
            int exhaustiveSearchInstanceTrialInstanceId, int randomTrialVariableCount,
            CancellationToken token = default)
        {
            await recorder.DeleteTrialVariablesAsync(exhaustiveSearchInstanceTrialInstanceId, token)
                .ConfigureAwait(false);

            var trialVariables = new Dictionary<int, TrialVariable>();
            var drawnVariables = DrawVariableIndices(seeded, variables.Count, randomTrialVariableCount);

            for (var j = 0; j < drawnVariables.Length; j++)
            {
                var randomVariable = drawnVariables[j];

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        "Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of " +
                        $"{exhaustiveSearchInstanceTrialInstanceId} is separating the dataset.  Has selected {randomVariable} as" +
                        " a random index.  Will check if it has already been added as a trial variable.");
                }

                if (!trialVariables.ContainsKey(randomVariable))
                {
                    var trialVariable = new TrialVariable
                    {
                        Name = variables[randomVariable].Name,
                        Mean = variables[randomVariable].Mean,
                        Sd = variables[randomVariable].Sd,
                        Mode = variables[randomVariable].Mode,
                        Min = variables[randomVariable].Min,
                        Max = variables[randomVariable].Max,
                        ExhaustiveSearchInstanceVariableId =
                            variables[randomVariable].ExhaustiveSearchInstanceVariableId,
                        TriangularDistribution = variables[randomVariable].TriangularDistribution,
                        NormalisationTypeId = variables[randomVariable].NormalisationType,
                        ProcessingTypeId = variables[randomVariable].ProcessingTypeId
                    };

                    trialVariables.Add(randomVariable, trialVariable);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            "Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of " +
                            $"{exhaustiveSearchInstanceTrialInstanceId} is separating the dataset.  " +
                            $"Has added Exhaustive Search Instance Variable Id {variables[randomVariable].ExhaustiveSearchInstanceVariableId})." +
                            "Will also store a record in the database.");
                    }

                    var exhaustiveSearchInstanceTrialInstanceVariable =
                        new ExhaustiveSearchInstanceTrialInstanceVariable
                        {
                            VariableSequence = j,
                            ExhaustiveSearchInstanceVariableId =
                                variables[randomVariable].ExhaustiveSearchInstanceVariableId,
                            ExhaustiveSearchInstanceTrialInstanceId = exhaustiveSearchInstanceTrialInstanceId
                        };

                    trialVariables[randomVariable].ExhaustiveSearchInstanceTrialInstanceVariableId
                        = (await recorder.InsertTrialVariableAsync(exhaustiveSearchInstanceTrialInstanceVariable, token)
                            .ConfigureAwait(false)).Id;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            "Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of " +
                            $"{exhaustiveSearchInstanceTrialInstanceId} is separating the dataset.  " +
                            $"Has added Exhaustive Search Instance Variable Id {variables[randomVariable].ExhaustiveSearchInstanceVariableId})." +
                            " has created Exhaustive Search Instance Trial Instance Variable Id " +
                            $"{trialVariables[randomVariable].ExhaustiveSearchInstanceTrialInstanceVariableId}.");
                    }
                }
                else
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            "Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of " +
                            $"{exhaustiveSearchInstanceTrialInstanceId} is separating the dataset.  " +
                            $"Has not added Exhaustive Search Instance Variable Id {variables[randomVariable].ExhaustiveSearchInstanceVariableId})." +
                            " as it already exist.");
                    }
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        "Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of " +
                        $"{exhaustiveSearchInstanceTrialInstanceId} is separating the dataset.  " +
                        $" Has finished adding trial variables and there are now {trialVariables.Count} available.  Returning method.");
                }
            }

            return trialVariables;
        }

        internal static int[] DrawVariableIndices(Random random, int variableCount, int count)
        {
            count = Math.Clamp(count, 0, Math.Max(variableCount, 0));
            var pool = Enumerable.Range(0, Math.Max(variableCount, 0)).ToArray();

            for (var i = 0; i < count; i++)
            {
                var j = random.Next(i, pool.Length);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            return [.. pool.Take(count)];
        }

        internal int GetRandomVariableCount(
            int availableVariableCount, int minVariableCount, int maxVariableCount)
        {
            if (availableVariableCount < minVariableCount)
            {
                return availableVariableCount;
            }

            var value = seeded.Next(minVariableCount, availableVariableCount);

            if (value < minVariableCount)
            {
                return minVariableCount;
            }

            return value > maxVariableCount ? maxVariableCount : value;
        }
    }
}
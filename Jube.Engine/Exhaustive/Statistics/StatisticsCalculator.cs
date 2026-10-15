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
using System.Threading;
using System.Threading.Tasks;
using Accord.Math;
using Accord.Statistics.Visualizations;
using Jube.Data.Repository.Interfaces;
using Jube.Engine.Exhaustive.Algorithms.Models;
using log4net;

namespace Jube.Engine.Exhaustive.Statistics
{
    internal class StatisticsCalculator(ILog log)
    {
        internal Task CalculateStatisticsAsync(int exhaustiveSearchInstanceId, Dictionary<int, Variable> variables,
            double[][] data,
            IGenericRepository repositoryVariables,
            IGenericRepository repositoryHistogram,
            CancellationToken token
        )
        {
            return CalculateStatisticsAsync(exhaustiveSearchInstanceId, variables, data,
                StatisticsTargetResolver.ResolveStatisticsTarget(repositoryVariables), repositoryVariables,
                repositoryHistogram, token);
        }

        internal async Task CalculateStatisticsAsync(int exhaustiveSearchInstanceId,
            Dictionary<int, Variable> variables,
            double[][] data,
            StatisticsTarget target,
            IGenericRepository repositoryVariables,
            IGenericRepository repositoryHistogram,
            CancellationToken token
        )
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Is starting calculation of statistics for Exhaustive Search Instance {exhaustiveSearchInstanceId}.  There are {variables.Count} variables.");
            }

            for (var i = 0; i < variables.Count; i++)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Processing variable {i} for Exhaustive Search Instance {exhaustiveSearchInstanceId}.  There are {variables.Count} variables.");
                }

                var variableData = data.GetColumn(i);

                var modelExhaustiveSearchInstanceVariableClassification =
                    new VariableProfileBuilder(log).Build(variableData, data.Length);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id} inserted and is about to compute histogram with {modelExhaustiveSearchInstanceVariableClassification.Bins} bins.");
                }

                var histogram = new Histogram();
                histogram.Compute(variableData);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id} has computed histogram with {modelExhaustiveSearchInstanceVariableClassification.Bins} bins.  Will proceed to process bins.");
                }


                if (target == StatisticsTarget.Population)
                {
                    await new PopulationStatisticsWriter(log).WriteAsync(exhaustiveSearchInstanceId, i, variables,
                        modelExhaustiveSearchInstanceVariableClassification, histogram, repositoryVariables,
                        repositoryHistogram, token).ConfigureAwait(false);
                }
                else if (target == StatisticsTarget.Classification)
                {
                    await new ClassificationStatisticsWriter(log).WriteAsync(exhaustiveSearchInstanceId, i, variables,
                        modelExhaustiveSearchInstanceVariableClassification, histogram, repositoryVariables,
                        repositoryHistogram, token).ConfigureAwait(false);
                }
                else
                {
                    await new AnomalyStatisticsWriter(log).WriteAsync(exhaustiveSearchInstanceId, i, variables,
                        modelExhaustiveSearchInstanceVariableClassification, histogram, repositoryVariables,
                        repositoryHistogram, token).ConfigureAwait(false);
                }
            }
        }
    }
}
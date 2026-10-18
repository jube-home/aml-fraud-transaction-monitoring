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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.Exhaustive.Algorithms;
using Jube.Engine.Exhaustive.Algorithms.Models;
using log4net;

namespace Jube.Engine.Exhaustive.Statistics
{
    using Performance = Performance;

    internal class MulticollinearityCalculator(ILog log)
    {
        internal async Task<Dictionary<int, Variable>> CalculateMulticollinearityAsync(
            Dictionary<int, Variable> variables, double[][] data,
            ExhaustiveSearchInstanceVariableMultiCollinearityRepository repository, CancellationToken token = default)
        {
            for (var i = 0; i < variables.Count; i++)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Exhaustive Search Instance Variable ID {variables[i].ExhaustiveSearchInstanceVariableId} will evaluate for strong correlations to this variable.");
                }

                var crossCorrelations = new Dictionary<int, double>();

                for (var j = 0; j < variables.Count; j++)
                {
                    if (i != j)
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Exhaustive Search Instance Variable ID {variables[i].ExhaustiveSearchInstanceVariableId} will evaluate correlation against {variables[j].ExhaustiveSearchInstanceVariableId}.");
                        }

                        var correlation = Performance.SpearmansCoeff(data.GetColumn(i), data.GetColumn(j));
                        crossCorrelations.Add(j, correlation);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Exhaustive Search Instance Variable ID {variables[i].ExhaustiveSearchInstanceVariableId} has evaluated correlation against {variables[j].ExhaustiveSearchInstanceVariableId} and returned {correlation}.");
                        }
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Exhaustive Search Instance Variable ID {variables[i].ExhaustiveSearchInstanceVariableId} will not evaluate to itself.");
                        }
                    }
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Exhaustive Search Instance Variable ID {variables[i].ExhaustiveSearchInstanceVariableId} will sort correlations for ranking.");
                }

                var sortedCrossCorrelations = from pair in crossCorrelations
                    orderby Math.Abs(Math.Round(pair.Value, 2)) descending
                    select pair;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Exhaustive Search Instance Variable ID {variables[i].ExhaustiveSearchInstanceVariableId} has sorted correlations for ranking. Will insert these correlations into the database.");
                }

                var seqCrossCorrelation = 1;

                foreach (var (key, value) in sortedCrossCorrelations)
                {
                    var model = new ExhaustiveSearchInstanceVariableMultiCollinearity
                    {
                        ExhaustiveSearchInstanceVariableId = variables[i].ExhaustiveSearchInstanceVariableId,
                        TestExhaustiveSearchInstanceVariableId = variables[key].ExhaustiveSearchInstanceVariableId,
                        Correlation = value,
                        CorrelationAbsRank = seqCrossCorrelation
                    };

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Exhaustive Search Instance Variable ID {variables[i].ExhaustiveSearchInstanceVariableId} will insert correlation for test key {variables[key].ExhaustiveSearchInstanceVariableId}.");
                    }

                    await repository.InsertAsync(model, token).ConfigureAwait(false);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Exhaustive Search Instance Variable ID {variables[i].ExhaustiveSearchInstanceVariableId} has inserted correlation for test key {variables[key].ExhaustiveSearchInstanceVariableId} and will proceed to next test correlation.");
                    }

                    seqCrossCorrelation += 1;
                }
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    "Exhaustive Training: Has concluded calculation of correlation for each correlation,  have updated variables and will return.");
            }

            return variables;
        }
    }
}
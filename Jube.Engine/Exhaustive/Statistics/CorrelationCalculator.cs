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
using Jube.Data.Repository;
using Jube.Engine.Exhaustive.Algorithms;
using Jube.Engine.Exhaustive.Algorithms.Models;
using log4net;

namespace Jube.Engine.Exhaustive.Statistics
{
    using Performance = Performance;

    internal class CorrelationCalculator(ILog log)
    {
        internal async Task CalculateCorrelationsAsync(Dictionary<int, Variable> variables, double[][] data,
            double[] outputs, ExhaustiveSearchInstanceVariableRepository repository, CancellationToken token = default)
        {
            for (var i = 0; i < variables.Count; i++)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: About to calculate correlation for ExhaustiveSearch Instance Variable Id {variables[i].ExhaustiveSearchInstanceVariableId}.");
                }

                var independent = data.GetColumn(i);
                variables[i].Correlation = Performance.SpearmansCoeff(outputs, independent);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Calculated correlation as {variables[i].Correlation} for Exhaustive Search Instance Variable Id {variables[i].ExhaustiveSearchInstanceVariableId}.  Will proceed to rank them.");
                }
            }

            var sortedCorrelations = from c in variables
                orderby Math.Abs(Math.Round(c.Value.Correlation, 2)) descending
                select c;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    "Exhaustive Training: Ranked correlations.  Will store them alongside the existing variable record with ranking.");
            }

            var variableSequence = 0;

            foreach (var (_, value) in sortedCorrelations)
            {
                log.Info(
                    $"Exhaustive Training: Ranked correlations.  Will proceed to store the correlation for Exhaustive Search Instance Variable Id {value.ExhaustiveSearchInstanceVariableId}.");

                await repository.UpdateCorrelationAsync(value.ExhaustiveSearchInstanceVariableId,
                    value.Correlation, variableSequence, token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Ranked correlations. Has stored correlation for {value.ExhaustiveSearchInstanceVariableId}.");
                }

                variableSequence += 1;
            }
        }
    }
}
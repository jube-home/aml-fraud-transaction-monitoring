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
using Jube.Data.Repository.Interfaces;
using Jube.Engine.Exhaustive.Algorithms.Models;

namespace Jube.Engine.Exhaustive.Statistics
{
    internal static class ClassHistogramWriter
    {
        internal const int NegativeLabel = 0;
        internal const int PositiveLabel = 1;

        internal static async Task WriteAllAsync(Dictionary<int, Variable> variables,
            double[][] labelled, double[] labels,
            Func<int, CancellationToken, Task<(double[] Start, double[] End)>> populationBins,
            IGenericRepository repositoryHistogramClass, CancellationToken token)
        {
            for (var i = 0; i < variables.Count; i++)
            {
                var (start, end) = await populationBins(variables[i].ExhaustiveSearchInstanceVariableId, token)
                    .ConfigureAwait(false);

                var column = i;
                var result = ClassHistogramComputer.Compute(start, end, [.. labelled.Select(r => r[column])],
                    labels);

                if (result == null)
                {
                    continue;
                }

                await WriteAsync(variables[i].ExhaustiveSearchInstanceVariableId, result, repositoryHistogramClass,
                    token).ConfigureAwait(false);
            }
        }

        internal static async Task WriteAsync(int exhaustiveSearchInstanceVariableId,
            ClassHistogramResult result, IGenericRepository repositoryHistogramClass,
            CancellationToken token)
        {
            for (var label = NegativeLabel; label <= PositiveLabel; label++)
            {
                var counts = label == NegativeLabel ? result.Negative : result.Positive;

                for (var bin = 0; bin < counts.Length; bin++)
                {
                    await repositoryHistogramClass.InsertAsync(new ExhaustiveSearchInstanceVariableHistogramClass
                    {
                        ExhaustiveSearchInstanceVariableId = exhaustiveSearchInstanceVariableId,
                        ClassLabel = label,
                        BinSequence = bin,
                        BinRangeStart = result.BinStart[bin],
                        BinRangeEnd = result.BinEnd[bin],
                        Frequency = counts[bin]
                    }, token).ConfigureAwait(false);
                }
            }
        }
    }
}
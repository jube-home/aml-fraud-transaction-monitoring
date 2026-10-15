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
using Accord.Statistics.Visualizations;
using Jube.Data.Poco;
using Jube.Data.Repository.Interfaces;
using Jube.Engine.Exhaustive.Algorithms.Models;
using log4net;

namespace Jube.Engine.Exhaustive.Statistics
{
    internal class AnomalyStatisticsWriter(ILog log)
    {
        internal async Task WriteAsync(int exhaustiveSearchInstanceId, int i, Dictionary<int, Variable> variables,
            ExhaustiveSearchInstanceVariableClassification modelExhaustiveSearchInstanceVariableClassification,
            Histogram histogram, IGenericRepository repositoryVariables, IGenericRepository repositoryHistogram,
            CancellationToken token)
        {
            var binSequence = 0;

            var modelExhaustiveSearchInstanceVariableAnomaly = new ExhaustiveSearchInstanceVariableAnomaly
            {
                Mode = modelExhaustiveSearchInstanceVariableClassification.Mode,
                Mean = modelExhaustiveSearchInstanceVariableClassification.Mean,
                StandardDeviation = modelExhaustiveSearchInstanceVariableClassification.StandardDeviation,
                Kurtosis = modelExhaustiveSearchInstanceVariableClassification.Kurtosis,
                Skewness = modelExhaustiveSearchInstanceVariableClassification.Skewness,
                Maximum = modelExhaustiveSearchInstanceVariableClassification.Maximum,
                Minimum = modelExhaustiveSearchInstanceVariableClassification.Minimum,
                Iqr = modelExhaustiveSearchInstanceVariableClassification.Iqr,
                DistinctValues = modelExhaustiveSearchInstanceVariableClassification.DistinctValues,
                Bins = modelExhaustiveSearchInstanceVariableClassification.Bins,
                ExhaustiveSearchInstanceVariableId = variables[i].ExhaustiveSearchInstanceVariableId
            };

            var exhaustiveSearchInstanceVariableAnomalyId =
                await repositoryVariables.InsertAsync(modelExhaustiveSearchInstanceVariableAnomaly, token)
                    .ConfigureAwait(false);

            foreach (var histogramBin in histogram.Bins)
            {
                var modelExhaustiveSearchInstanceVariableHistogram =
                    new ExhaustiveSearchInstanceVariableHistogramAnomaly
                    {
                        BinSequence = binSequence,
                        BinRangeStart = histogramBin.Range.Min,
                        BinRangeEnd = histogramBin.Range.Max,
                        ExhaustiveSearchInstanceVariableAnomalyId = exhaustiveSearchInstanceVariableAnomalyId,
                        Frequency = histogramBin.Value
                    };

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id} bin {binSequence} has start: {modelExhaustiveSearchInstanceVariableHistogram.BinRangeStart}, end {modelExhaustiveSearchInstanceVariableHistogram.BinRangeEnd} and frequency of {modelExhaustiveSearchInstanceVariableHistogram.Frequency}.");
                }

                await repositoryHistogram.InsertAsync(modelExhaustiveSearchInstanceVariableHistogram, token)
                    .ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id} bin {binSequence} inserted will move next bin.");
                }

                binSequence += 1;
            }
        }
    }
}
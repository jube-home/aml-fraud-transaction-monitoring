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
using System.Threading;
using System.Threading.Tasks;
using Accord.Statistics.Visualizations;
using Jube.Data.Poco;
using Jube.Data.Repository.Interfaces;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Exhaustive.Preparation;
using log4net;

namespace Jube.Engine.Exhaustive.Statistics
{
    internal class PopulationStatisticsWriter(ILog log)
    {
        internal async Task WriteAsync(int exhaustiveSearchInstanceId, int i, Dictionary<int, Variable> variables,
            ExhaustiveSearchInstanceVariableClassification modelExhaustiveSearchInstanceVariableClassification,
            Histogram histogram, IGenericRepository repositoryVariables, IGenericRepository repositoryHistogram,
            CancellationToken token)
        {
            var binSequence = 0;

            if (modelExhaustiveSearchInstanceVariableClassification is not
                {
                    Mean: { } mean, Mode: { } mode, Maximum: { } max, Minimum: { } min,
                    Kurtosis: { } kurtosis, Skewness: { } skewness, Iqr: { } iqr,
                    StandardDeviation: { } sd, DistinctValues: { } distinctCount, Bins: { } bins
                })
            {
                throw new InvalidOperationException(
                    "VariableProfileBuilder always populates every statistic before WriteAsync runs.");
            }

            variables[i].Mean = mean;
            variables[i].Mode = mode;
            variables[i].Max = max;
            variables[i].Min = min;
            variables[i].Kurtosis = kurtosis;
            variables[i].Skewness = skewness;
            variables[i].Iqr = iqr;
            variables[i].Sd = sd;
            variables[i].DistinctCount = distinctCount;
            variables[i].Bins = bins;

            var modelExhaustiveSearchInstanceVariable = new ExhaustiveSearchInstanceVariable
            {
                ExhaustiveSearchInstanceId = exhaustiveSearchInstanceId,
                VariableSequence = i,
                Name = variables[i].Name,
                ProcessingTypeId = variables[i].ProcessingTypeId,
                Mode = modelExhaustiveSearchInstanceVariableClassification.Mode,
                Mean = modelExhaustiveSearchInstanceVariableClassification.Mean,
                StandardDeviation = modelExhaustiveSearchInstanceVariableClassification.StandardDeviation,
                Kurtosis = modelExhaustiveSearchInstanceVariableClassification.Kurtosis,
                Skewness = modelExhaustiveSearchInstanceVariableClassification.Skewness,
                Maximum = modelExhaustiveSearchInstanceVariableClassification.Maximum,
                Minimum = modelExhaustiveSearchInstanceVariableClassification.Minimum,
                Iqr = modelExhaustiveSearchInstanceVariableClassification.Iqr,
                DistinctValues = modelExhaustiveSearchInstanceVariableClassification.DistinctValues,
                Bins = modelExhaustiveSearchInstanceVariableClassification.Bins
            };

            if (variables[i].CategoricalEncoder != null)
            {
                var indicator = variables[i].CategoricalEncoder.Indicators[variables[i].CategoricalIndicatorIndex];
                modelExhaustiveSearchInstanceVariable.CategoricalFieldId = variables[i].CategoricalFieldId;
                modelExhaustiveSearchInstanceVariable.CategoricalKind = (byte)indicator.Kind;
                modelExhaustiveSearchInstanceVariable.CategoricalLevel = indicator.Label;

                modelExhaustiveSearchInstanceVariable.CategoricalIndicatorIndex =
                    variables[i].CategoricalIndicatorIndex;
            }

            if (variables[i].Max is 0)
            {
                variables[i].NormalisationType = 0;
            }
            else if (variables[i].Min == 0 && Math.Abs(variables[i].Max - 1) < 0.0001)
            {
                variables[i].NormalisationType = 1;
            }
            else
            {
                variables[i].NormalisationType = 2;
            }

            modelExhaustiveSearchInstanceVariable.NormalisationTypeId = variables[i].NormalisationType;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Variable Normalisation Type: {modelExhaustiveSearchInstanceVariable.NormalisationTypeId} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariable.Id}.");
            }

            var minNormalised = Normalisation.ZScore(variables[i].Min, variables[i].Mean, variables[i].Sd,
                variables[i].NormalisationType);

            var maxNormalised = Normalisation.ZScore(variables[i].Max, variables[i].Mean, variables[i].Sd,
                variables[i].NormalisationType);

            if (variables[i].Mode == 0)
            {
                var meanNormalised = Normalisation.ZScore(variables[i].Mean, variables[i].Mean, variables[i].Sd,
                    variables[i].NormalisationType);

                variables[i].TriangularDistribution = variables[i].Mean == 0
                    ? TriangularDistributionFactory.BuildTriangularDistribution(minNormalised, maxNormalised,
                        meanNormalised)
                    : TriangularDistributionFactory.BuildTriangularDistribution(minNormalised, maxNormalised, 0.01d);
            }
            else
            {
                var modeNormalised = Normalisation.ZScore(variables[i].Mode, variables[i].Mean, variables[i].Sd,
                    variables[i].NormalisationType);

                variables[i].TriangularDistribution =
                    TriangularDistributionFactory.BuildTriangularDistribution(minNormalised, maxNormalised,
                        modeNormalised);
            }

            variables[i].ExhaustiveSearchInstanceVariableId =
                await repositoryVariables.InsertAsync(modelExhaustiveSearchInstanceVariable, token)
                    .ConfigureAwait(false);

            foreach (var histogramBin in histogram.Bins)
            {
                var modelExhaustiveSearchInstanceVariableHistogram =
                    new ExhaustiveSearchInstanceVariableHistogram
                    {
                        BinSequence = binSequence,
                        BinRangeStart = histogramBin.Range.Min,
                        BinRangeEnd = histogramBin.Range.Max,
                        ExhaustiveSearchInstanceVariableId = variables[i].ExhaustiveSearchInstanceVariableId,
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
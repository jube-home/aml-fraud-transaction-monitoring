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
using Accord.Statistics;
using Jube.Data.Poco;
using Jube.Engine.Exhaustive.Preparation;
using log4net;

namespace Jube.Engine.Exhaustive.Statistics
{
    internal class VariableProfileBuilder(ILog log)
    {
        internal ExhaustiveSearchInstanceVariableClassification Build(double[] variableData, int dataLength)
        {
            var modelExhaustiveSearchInstanceVariableClassification =
                new ExhaustiveSearchInstanceVariableClassification();

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
            }

            modelExhaustiveSearchInstanceVariableClassification.Mean =
                NumericSanitiser.SwapNanInfinityToZero(variableData.Mean());

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Variable Mean: {modelExhaustiveSearchInstanceVariableClassification.Mean} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
            }

            modelExhaustiveSearchInstanceVariableClassification.Mode =
                NumericSanitiser.SwapNanInfinityToZero(variableData.Mode());

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Variable Mode: {modelExhaustiveSearchInstanceVariableClassification.Mean} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
            }

            modelExhaustiveSearchInstanceVariableClassification.Maximum =
                NumericSanitiser.SwapNanInfinityToZero(variableData.Max());

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Variable Maximum: {modelExhaustiveSearchInstanceVariableClassification.Maximum} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
            }

            modelExhaustiveSearchInstanceVariableClassification.Minimum =
                NumericSanitiser.SwapNanInfinityToZero(variableData.Min());

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Variable Minimum: {modelExhaustiveSearchInstanceVariableClassification.Minimum} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
            }

            modelExhaustiveSearchInstanceVariableClassification.Kurtosis =
                NumericSanitiser.SwapNanInfinityToZero(variableData.Kurtosis());

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Variable Kurtosis: {modelExhaustiveSearchInstanceVariableClassification.Kurtosis} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
            }

            modelExhaustiveSearchInstanceVariableClassification.Skewness =
                NumericSanitiser.SwapNanInfinityToZero(variableData.Skewness());

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Variable Skewness: {modelExhaustiveSearchInstanceVariableClassification.Skewness} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
            }

            variableData.Quartiles(out var q1, out var q3);
            modelExhaustiveSearchInstanceVariableClassification.Iqr = NumericSanitiser.SwapNanInfinityToZero(q3 - q1);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Variable Iqr: {modelExhaustiveSearchInstanceVariableClassification.Iqr} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
            }

            modelExhaustiveSearchInstanceVariableClassification.StandardDeviation =
                NumericSanitiser.SwapNanInfinityToZero(variableData.StandardDeviation());

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Variable Standard Deviation: {modelExhaustiveSearchInstanceVariableClassification.StandardDeviation} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
            }

            var distinctDoubles = new List<double>();

            foreach (var value in variableData)
            {
                if (!distinctDoubles.Contains(value))
                {
                    distinctDoubles.Add(value);
                }
            }

            modelExhaustiveSearchInstanceVariableClassification.DistinctValues = distinctDoubles.Count;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Variable Distinct Values: {modelExhaustiveSearchInstanceVariableClassification.DistinctValues} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
            }

            const int maxBins = 20;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Maximum number of bins is {maxBins} and will proceed to calculate the optimal number of bins for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
            }

            var numberOfBins = 0.0d;

            try
            {
                if ((Math.Abs(modelExhaustiveSearchInstanceVariableClassification.Maximum.Value - 1) < 0.0001) &
                    (modelExhaustiveSearchInstanceVariableClassification.Minimum.Value == 0) &
                    (modelExhaustiveSearchInstanceVariableClassification.DistinctValues.Value == 2))
                {
                    numberOfBins = 2;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Variable is inferred to be binary with 2 bins for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                    }
                }
                else if (numberOfBins > modelExhaustiveSearchInstanceVariableClassification.DistinctValues.Value)
                {
                    numberOfBins = modelExhaustiveSearchInstanceVariableClassification.DistinctValues.Value;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Variable is set to distinct values of {modelExhaustiveSearchInstanceVariableClassification.DistinctValues.Value} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                    }
                }
                else if (modelExhaustiveSearchInstanceVariableClassification.Iqr.Value == 0)
                {
                    numberOfBins = maxBins;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Variable is set to distinct values of {modelExhaustiveSearchInstanceVariableClassification.DistinctValues.Value} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                    }
                }
                else
                {
                    numberOfBins = Math.Round(1d + 3.3d * Math.Log(dataLength));

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Has used Sturges Rule to create {numberOfBins} bins for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                    }
                }

                if (numberOfBins > maxBins)
                {
                    numberOfBins = maxBins;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Has clipped to {numberOfBins} bins for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                numberOfBins = maxBins;

                log.Error(
                    $"Exhaustive Training: Has {numberOfBins} bins for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id} after exception of {ex}.");
            }

            modelExhaustiveSearchInstanceVariableClassification.Bins = (int)numberOfBins;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Number of Bins Values: {modelExhaustiveSearchInstanceVariableClassification.Bins} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
            }

            return modelExhaustiveSearchInstanceVariableClassification;
        }
    }
}
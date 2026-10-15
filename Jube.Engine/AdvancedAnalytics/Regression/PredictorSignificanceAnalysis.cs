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
using System.Linq;
using System.Threading;
using Accord.Math;
using Jube.Analytics;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public sealed record PredictorSignificance(
        string Variable,
        int RowsUsed,
        int MissingRows,
        double? Estimate,
        double? StandardError,
        double? WaldZ,
        double? PValue,
        double? AdjustedPValue,
        double? OddsRatio,
        double? OddsRatioLower95,
        double? OddsRatioUpper95,
        double? RocAuc,
        string Note);

    public static class PredictorSignificanceAnalysis
    {
        public static PredictorSignificance[] Assess(double[][] inputs, int[] labels, string[] variableNames,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(inputs);
            ArgumentNullException.ThrowIfNull(labels);
            ArgumentNullException.ThrowIfNull(variableNames);

            if (variableNames.Length == 0)
            {
                throw new ArgumentException("At least one variable is needed.");
            }

            if (variableNames.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Variable names must not be blank.");
            }

            if (variableNames.Distinct(StringComparer.Ordinal).Count() != variableNames.Length)
            {
                throw new ArgumentException("Variable names must be unique.");
            }

            if (inputs.Length != labels.Length)
            {
                throw new ArgumentException($"There are {inputs.Length} input rows but {labels.Length} labels.");
            }

            if (inputs.Length == 0)
            {
                throw new ArgumentException("There are no rows.");
            }

            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] == null)
                {
                    throw new ArgumentException($"Row {i} is null.");
                }

                if (inputs[i].Length != variableNames.Length)
                {
                    throw new ArgumentException(
                        $"Row {i} has {inputs[i].Length} values but there are {variableNames.Length} variable names (rows must all be the same length).");
                }
            }

            if (labels.Any(l => l != 0 && l != 1))
            {
                throw new ArgumentException("Labels must be 0 or 1.");
            }

            if (labels.Sum() == 0 || labels.Sum() == labels.Length)
            {
                throw new ArgumentException("Both classes must be present in the labels.");
            }

            var count = variableNames.Length;
            var results = new PredictorSignificance[count];
            var pValues = new double?[count];

            for (var j = 0; j < count; j++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var keep = Enumerable.Range(0, inputs.Length).Where(i => double.IsFinite(inputs[i][j])).ToArray();
                var x = keep.Select(i => inputs[i][j]).ToArray();
                var y = keep.Select(i => labels[i]).ToArray();
                var missing = inputs.Length - keep.Length;
                var positives = y.Sum();

                PredictorSignificance Undefined(string note)
                {
                    return new PredictorSignificance(variableNames[j], keep.Length, missing, null, null, null, null,
                        null, null, null, null,
                        null,
                        note);
                }

                if (positives < 2 || y.Length - positives < 2)
                {
                    results[j] = Undefined("Fewer than two rows of one class remain after dropping missing values.");
                    continue;
                }

                if (x.Distinct().Count() < 2)
                {
                    results[j] = Undefined("The variable is constant over the usable rows.");
                    continue;
                }

                var auc = BinaryMetrics.RocAuc(x, y);

                if (auc >= 1d || auc <= 0d)
                {
                    results[j] =
                        Undefined(
                                "Complete separation: the variable alone perfectly splits the classes, so its Wald statistics are not defined.")
                            with
                            {
                                RocAuc = auc
                            };

                    continue;
                }

                var fit = UnivariateLogistic.Fit(x, y);

                if (!fit.Success)
                {
                    results[j] = Undefined("The univariate fit did not converge.") with { RocAuc = auc };
                    continue;
                }

                var z = fit.Slope / fit.SlopeStandardError;
                var p = Special.Erfc(Math.Abs(z) / Math.Sqrt(2d));
                pValues[j] = p;

                results[j] = new PredictorSignificance(variableNames[j], keep.Length, missing, fit.Slope,
                    fit.SlopeStandardError, z, p, null, Math.Exp(fit.Slope),
                    Math.Exp(fit.Slope - LogisticRegressionTrainer.Z95 * fit.SlopeStandardError),
                    Math.Exp(fit.Slope + LogisticRegressionTrainer.Z95 * fit.SlopeStandardError), auc, null);
            }

            var adjusted = BenjaminiHochberg(pValues);

            for (var j = 0; j < count; j++)
            {
                if (adjusted[j].HasValue)
                {
                    results[j] = results[j] with { AdjustedPValue = adjusted[j] };
                }
            }

            return results;
        }

        public static double?[] BenjaminiHochberg(double?[] pValues)
        {
            ArgumentNullException.ThrowIfNull(pValues);

            var order = Enumerable.Range(0, pValues.Length).Where(i => pValues[i].HasValue)
                .OrderBy(i => pValues[i]!.Value).ToArray();

            var m = order.Length;
            var result = new double?[pValues.Length];
            var running = 1d;

            for (var rank = m; rank >= 1; rank--)
            {
                var index = order[rank - 1];
                running = Math.Min(running, pValues[index]!.Value * m / rank);
                result[index] = Math.Min(1d, Math.Max(running, pValues[index]!.Value));
            }

            return result;
        }
    }
}
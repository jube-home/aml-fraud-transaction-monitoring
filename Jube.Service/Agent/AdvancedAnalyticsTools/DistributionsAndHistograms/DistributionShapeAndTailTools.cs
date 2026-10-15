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

using System.ComponentModel;
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
{
    [AdvancedAnalyticsToolGroup("DistributionsAndHistograms")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class DistributionShapeAndTailTools
    {
        private const string ValuesText = "The observations, no missing values.";

        [AdvancedAnalyticsTool("NormalityShapeSummary")]
        [Description(
            "Checks whether the shape of a sample is compatible with a normal distribution using skewness and kurtosis. " +
            "It returns the bias corrected sample skewness (0 for symmetric data, positive when the upper tail is longer) and excess kurtosis (0 for a normal, positive for heavier tails than normal), each with its standard error and a z score (values beyond about 2 in size are evidence of non-normality), " +
            "the Jarque-Bera statistic and its p value when there are at least 8 values, and a plain language verdict. " +
            "This describes shape only; it is a quick screen and not a replacement for a formal normality test on small samples. All values identical is an error.")]
        public NormalityShapeResult NormalityShapeSummary(
            [Description("The observations, at least 4 numbers, no missing values.")]
            double[] values)
        {
            DistributionGuard.Values(values, nameof(values), 4);

            return ShapeAndTailEngine.Shape(values);
        }

        [AdvancedAnalyticsTool("TailExceedanceByQuantile")]
        [Description(
            "Summarises the tail of a sample beyond high (or low) quantile levels. " +
            "For each level such as 0.95 or 0.99 it finds the threshold value at that quantile and reports how many observations exceed it, the fraction that exceed, the mean excess (average distance beyond the threshold), the mean of the exceeding values themselves (the empirical expected shortfall or tail value at risk) and the largest excess. " +
            "Use direction Upper for large values and Lower for small ones. A null mean means nothing exceeded the threshold.")]
        public TailExceedanceResult TailExceedanceByQuantile(
            [Description(ValuesText)] double[] values,
            [Description(
                "Quantile levels strictly between 0 and 1 that define the thresholds, for example 0.9, 0.95, 0.99.")]
            double[] levels,
            [Description("Upper looks at values above each threshold (default); Lower looks at values below it.")]
            TailDirection direction = TailDirection.Upper,
            [Description("Sample quantile definition used to find the thresholds, Type1 to Type9. Default Type7.")]
            QuantileDefinition definition = QuantileDefinition.Type7)
        {
            DistributionGuard.Values(values, nameof(values));
            DistributionGuard.Probabilities(levels, nameof(levels), false);
            var thresholds = HistogramEngine.Quantiles(values, levels, definition);
            var rows = new TailExceedanceRow[levels.Length];

            for (var i = 0; i < levels.Length; i++)
            {
                rows[i] = ShapeAndTailEngine.Row(values, levels[i], thresholds[i], direction);
            }

            return new TailExceedanceResult(values.Length, rows, Array.Empty<string>());
        }

        [AdvancedAnalyticsTool("TailExceedanceByThreshold")]
        [Description(
            "Summarises the tail of a sample beyond thresholds you choose, for example fixed reporting limits. " +
            "For each threshold it reports how many observations are strictly beyond it, the fraction beyond, the mean excess over the threshold, the mean of the exceeding values and the largest excess. " +
            "The level field is the empirical fraction of values at or below the threshold (for Upper) so you can see which quantile the limit corresponds to. " +
            "Use direction Upper for values above each threshold and Lower for values below it.")]
        public TailExceedanceResult TailExceedanceByThreshold(
            [Description(ValuesText)] double[] values,
            [Description("The threshold values, any real numbers.")]
            double[] thresholds,
            [Description("Upper looks at values above each threshold (default); Lower looks at values below it.")]
            TailDirection direction = TailDirection.Upper)
        {
            DistributionGuard.Values(values, nameof(values));
            DistributionGuard.Values(thresholds, nameof(thresholds));
            var rows = new TailExceedanceRow[thresholds.Length];

            for (var i = 0; i < thresholds.Length; i++)
            {
                var atMost = values.Count(v => v <= thresholds[i]);

                var level = direction == TailDirection.Upper
                    ? (double)atMost / values.Length
                    : (double)values.Count(v => v < thresholds[i]) / values.Length;

                rows[i] = ShapeAndTailEngine.Row(values, level, thresholds[i], direction);
            }

            return new TailExceedanceResult(values.Length, rows, Array.Empty<string>());
        }

        [AdvancedAnalyticsTool("FitGeneralizedParetoTail")]
        [Description(
            "Models the upper tail of a sample with a generalized Pareto distribution, the standard peaks-over-threshold method of extreme value analysis. " +
            "It takes the values above the threshold at the chosen quantile, fits a generalized Pareto to their excesses by maximum likelihood and returns the scale, the shape (tail index: near 0 means an exponential tail, clearly positive means a heavy power tail with rare huge values, negative means a bounded tail) and the log-likelihood. " +
            "It also returns return levels: for each requested overall probability such as 0.999, the value that will be exceeded only with probability one minus that. " +
            "At least 30 values must exceed the threshold for a stable fit; fewer is an error.")]
        public TailModelResult FitGeneralizedParetoTail(
            [Description("The observations, at least 100 numbers, no missing values.")]
            double[] values,
            [Description("The quantile level that defines the threshold, strictly between 0 and 1. Default 0.9.")]
            double thresholdQuantile = 0.9,
            [Description(
                "Overall probabilities at which to compute return levels; each must be above thresholdQuantile and below 1, for example 0.99 and 0.999. Leave empty for none.")]
            double[]? returnProbabilities = null)
        {
            DistributionGuard.Values(values, nameof(values), 100);
            DistributionGuard.Probability(thresholdQuantile, nameof(thresholdQuantile), false);
            returnProbabilities ??= Array.Empty<double>();

            foreach (var p in returnProbabilities)
            {
                if (!(p > thresholdQuantile && p < 1))
                {
                    throw new ArgumentException(
                        $"Every return probability must be above thresholdQuantile ({thresholdQuantile}) and below 1; got {p}.",
                        nameof(returnProbabilities));
                }
            }

            var threshold = HistogramEngine.Quantile(values, thresholdQuantile);
            var excesses = values.Where(v => v > threshold).Select(v => v - threshold).ToArray();

            if (excesses.Length < 30)
            {
                throw new ArgumentException(
                    $"Only {excesses.Length} value(s) exceed the threshold {threshold}; at least 30 are needed. Lower thresholdQuantile or supply more values.",
                    nameof(thresholdQuantile));
            }

            var entry = DistributionCatalogue.Get(DistributionKind.GeneralizedPareto);
            var outcome = DistributionFitter.Fit(entry, excesses, new[] { 0.0, double.NaN, double.NaN });
            var fraction = (double)excesses.Length / values.Length;

            var levels = returnProbabilities
                .Select(p => outcome.Model.Quantile(1.0 - (1.0 - p) / fraction) + threshold)
                .ToArray();

            var shape = outcome.Parameters[2];

            var kind = Math.Abs(shape) < 0.05
                ? "Approximately exponential tail"
                : shape > 0
                    ? "Heavy tail (power law), extreme values are possible"
                    : "Bounded tail (finite upper limit)";

            return new TailModelResult(
                values.Length,
                threshold,
                thresholdQuantile,
                excesses.Length,
                outcome.Parameters[1],
                shape,
                outcome.LogLikelihood,
                returnProbabilities,
                DistributionGuard.N(levels),
                kind,
                new[]
                {
                    "The excesses over the threshold were fitted with the location held at 0.",
                    "The tail is judged reliable only if the threshold is high enough that excesses are independent and few enough to be a true tail."
                });
        }
    }
}
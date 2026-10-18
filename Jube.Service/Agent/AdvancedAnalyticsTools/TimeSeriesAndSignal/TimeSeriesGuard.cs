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

using Accord.Math;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.TimeSeries;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal
{
    internal static class TimeSeriesGuard
    {
        public static double[] Finite(double[]? values, string name, int minimumLength)
        {
            if (values == null)
            {
                throw new ArgumentException(
                    $"{name} is required. Supply the series as an array of numbers in time order.",
                    name);
            }

            if (values.Length < minimumLength)
            {
                throw new ArgumentException(
                    $"{name} has {values.Length} values but at least {minimumLength} are needed for this analysis. Supply a longer series.",
                    name);
            }

            for (var i = 0; i < values.Length; i++)
            {
                if (double.IsNaN(values[i]) || double.IsInfinity(values[i]))
                {
                    throw new ArgumentException(
                        $"{name} contains a missing or infinite value at position {i}. Fill or remove it first (for example with FillMissingValuesInSeries).",
                        name);
                }
            }

            return values;
        }

        public static double[] AllowMissing(double[]? values, string name, int minimumLength)
        {
            if (values == null)
            {
                throw new ArgumentException(
                    $"{name} is required. Supply the series as an array of numbers in time order.",
                    name);
            }

            if (values.Length < minimumLength)
            {
                throw new ArgumentException(
                    $"{name} has {values.Length} values but at least {minimumLength} are needed for this analysis. Supply a longer series.",
                    name);
            }

            return values;
        }

        public static void SameLength(double[] first, double[] second, string firstName, string secondName)
        {
            if (first.Length != second.Length)
            {
                throw new ArgumentException(
                    $"{secondName} has {second.Length} values but {firstName} has {first.Length}; they must be the same length and aligned in time.",
                    secondName);
            }
        }

        public static int AtLeast(int value, int minimum, string name)
        {
            if (value < minimum)
            {
                throw new ArgumentException($"{name} must be at least {minimum} but was {value}.", name);
            }

            return value;
        }

        public static double Positive(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                throw new ArgumentException($"{name} must be a finite number greater than 0 but was {value}.", name);
            }

            return value;
        }

        public static double Unit(double value, string name, bool includeZero, bool includeOne)
        {
            var low = includeZero ? value >= 0 : value > 0;
            var high = includeOne ? value <= 1 : value < 1;

            if (double.IsNaN(value) || !low || !high)
            {
                var left = includeZero ? "[" : "(";
                var right = includeOne ? "]" : ")";
                throw new ArgumentException($"{name} must be within {left}0, 1{right} but was {value}.", name);
            }

            return value;
        }

        public static double Confidence(double level, string name)
        {
            if (double.IsNaN(level) || level <= 0 || level >= 1)
            {
                throw new ArgumentException(
                    $"{name} must be strictly between 0 and 1, for example 0.95, but was {level}.",
                    name);
            }

            return NormalDistribution.Standard.InverseDistributionFunction(0.5 + level / 2);
        }

        public static double[] Epoch(double[]? epochSeconds, string name, int minimumLength, bool requireSorted = true)
        {
            var values = Finite(epochSeconds, name, minimumLength);

            if (requireSorted)
            {
                for (var i = 1; i < values.Length; i++)
                {
                    if (values[i] < values[i - 1])
                    {
                        throw new ArgumentException(
                            $"{name} is not in time order: position {i} ({values[i]}) is earlier than position {i - 1} ({values[i - 1]}). Sort the events by time, keeping any value array aligned, before calling.",
                            name);
                    }
                }
            }

            return values;
        }

        public static double Sum(IReadOnlyList<double> values)
        {
            var sum = 0.0;
            var compensation = 0.0;

            foreach (var v in values)
            {
                var y = v - compensation;
                var t = sum + y;
                compensation = t - sum - y;
                sum = t;
            }

            return sum;
        }

        public static double Mean(IReadOnlyList<double> values)
        {
            return values.Count == 0 ? double.NaN : Sum(values) / values.Count;
        }

        public static double SampleVariance(IReadOnlyList<double> values)
        {
            if (values.Count < 2)
            {
                return double.NaN;
            }

            var mean = Mean(values);
            var squares = new double[values.Count];

            for (var i = 0; i < squares.Length; i++)
            {
                var d = values[i] - mean;
                squares[i] = d * d;
            }

            return Sum(squares) / (values.Count - 1);
        }

        public static double SampleStandardDeviation(IReadOnlyList<double> values)
        {
            return Math.Sqrt(SampleVariance(values));
        }

        public static double Median(IReadOnlyList<double> values)
        {
            if (values.Count == 0)
            {
                return double.NaN;
            }

            var sorted = values.OrderBy(v => v).ToArray();
            var mid = sorted.Length / 2;

            return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
        }

        public static double MedianAbsoluteDeviation(IReadOnlyList<double> values)
        {
            if (values.Count == 0)
            {
                return double.NaN;
            }

            var median = Median(values);

            return Median(values.Select(v => Math.Abs(v - median)).ToArray());
        }

        public static bool IsConstant(double[] values)
        {
            for (var i = 1; i < values.Length; i++)
            {
                if (values[i] != values[0])
                {
                    return false;
                }
            }

            return true;
        }

        public static double TwoSidedNormalPValue(double z)
        {
            return double.IsNaN(z)
                ? double.NaN
                : Math.Min(1.0, 2 * NormalDistribution.Standard.ComplementaryDistributionFunction(Math.Abs(z)));
        }

        public static double ChiSquareUpperTail(double statistic, int degreesOfFreedom)
        {
            if (double.IsNaN(statistic) || degreesOfFreedom < 1)
            {
                return double.NaN;
            }

            if (double.IsPositiveInfinity(statistic))
            {
                return 0;
            }

            return new ChiSquareDistribution(degreesOfFreedom).ComplementaryDistributionFunction(statistic);
        }

        public static void LinearFit(double[] values, out double intercept, out double slope)
        {
            var n = values.Length;
            var meanT = (n - 1) / 2.0;
            var meanY = Mean(values);
            var sxy = 0.0;
            var sxx = 0.0;

            for (var i = 0; i < n; i++)
            {
                sxy += (i - meanT) * (values[i] - meanY);
                sxx += (i - meanT) * (i - meanT);
            }

            slope = sxx == 0 ? 0 : sxy / sxx;
            intercept = meanY - slope * meanT;
        }

        public static double[] Autocorrelations(double[] values, int maxLag)
        {
            var lags = Math.Min(maxLag, values.Length - 1);
            var result = new double[lags + 1];

            if (IsConstant(values))
            {
                result[0] = 1;

                for (var k = 1; k < result.Length; k++)
                {
                    result[k] = double.NaN;
                }

                return result;
            }

            var library = TimeSeriesTools.AutoCorrelationFunction(values, lags + 1);

            for (var k = 0; k < result.Length; k++)
            {
                result[k] = k < library.Length ? library[k] : double.NaN;
            }

            return result;
        }

        public static double[] Autocovariances(double[] values, int maxLag)
        {
            var lags = Math.Min(maxLag, values.Length - 1);
            var n = values.Length;
            var mean = Mean(values);
            var result = new double[lags + 1];

            for (var k = 0; k <= lags; k++)
            {
                var sum = 0.0;

                for (var t = k; t < n; t++)
                {
                    sum += (values[t] - mean) * (values[t - k] - mean);
                }

                result[k] = sum / n;
            }

            return result;
        }

        public static double[] LeastSquares(double[,] design, double[] target)
        {
            return Matrix.Solve(design, target, true);
        }

        public static double ResidualSumOfSquares(double[,] design, double[] target, double[] coefficients)
        {
            var rows = design.GetLength(0);
            var cols = design.GetLength(1);
            var rss = 0.0;

            for (var i = 0; i < rows; i++)
            {
                var fitted = 0.0;

                for (var j = 0; j < cols; j++)
                {
                    fitted += design[i, j] * coefficients[j];
                }

                var e = target[i] - fitted;
                rss += e * e;
            }

            return rss;
        }
    }
}
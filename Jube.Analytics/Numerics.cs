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

namespace Jube.Analytics
{
    internal static class Numerics
    {
        public static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        public static double Sigmoid(double z)
        {
            if (z >= 0)
            {
                return 1d / (1d + Math.Exp(-z));
            }

            var e = Math.Exp(z);

            return e / (1d + e);
        }

        public static double Log1P(double x)
        {
            var u = 1d + x;

            return Math.Abs(u - 1d) < 0.0001 ? x : u == 0d ? double.NegativeInfinity : Math.Log(u) * x / (u - 1d);
        }

        public static double Softplus(double z)
        {
            return z > 0 ? z + Log1P(Math.Exp(-z)) : Log1P(Math.Exp(z));
        }

        public static double[] Finite(double[] values)
        {
            return values.Where(IsFinite).ToArray();
        }

        public static double Median(double[] sorted)
        {
            var n = sorted.Length;

            return n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2d;
        }

        public static double Quantile(double[] sorted, double q)
        {
            var position = q * (sorted.Length - 1);
            var lower = (int)Math.Floor(position);
            var upper = Math.Min(lower + 1, sorted.Length - 1);

            return sorted[lower] + (position - lower) * (sorted[upper] - sorted[lower]);
        }

        public static double[] AverageRanks(double[] values)
        {
            var order = Enumerable.Range(0, values.Length).OrderBy(i => values[i]).ToArray();
            var ranks = new double[values.Length];
            var start = 0;

            while (start < order.Length)
            {
                var end = start;

                while (end + 1 < order.Length && values[order[end + 1]].Equals(values[order[start]]))
                {
                    end++;
                }

                var rank = (start + end) / 2d + 1d;

                for (var k = start; k <= end; k++)
                {
                    ranks[order[k]] = rank;
                }

                start = end + 1;
            }

            return ranks;
        }

        public static (double[] Scores, int[] Labels, double[] Weights) CleanPairs(double[] scores, int[] labels,
            double[]? weights)
        {
            ArgumentNullException.ThrowIfNull(scores);
            ArgumentNullException.ThrowIfNull(labels);

            if (scores.Length != labels.Length)
            {
                throw new ArgumentException("Scores and labels must have the same length.");
            }

            if (weights != null && weights.Length != scores.Length)
            {
                throw new ArgumentException("Weights and scores must have the same length.");
            }

            var keep = new List<int>();

            for (var i = 0; i < scores.Length; i++)
            {
                if (labels[i] != 0 && labels[i] != 1)
                {
                    throw new ArgumentException("Labels must be 0 or 1.");
                }

                if (weights != null && (!IsFinite(weights[i]) || weights[i] < 0))
                {
                    throw new ArgumentException("Weights must be finite and not negative.");
                }

                if (!double.IsNaN(scores[i]))
                {
                    keep.Add(i);
                }
            }

            if (keep.Count == 0)
            {
                throw new ArgumentException("There are no usable rows: the input is empty or every score is NaN.");
            }

            return (keep.Select(i => scores[i]).ToArray(), keep.Select(i => labels[i]).ToArray(),
                keep.Select(i => weights?[i] ?? 1d).ToArray());
        }

        public static (double[] Actual, double[] Predicted) CleanRegressionPairs(double[] actual, double[] predicted)
        {
            ArgumentNullException.ThrowIfNull(actual);
            ArgumentNullException.ThrowIfNull(predicted);

            if (actual.Length != predicted.Length)
            {
                throw new ArgumentException("Actual and predicted must have the same length.");
            }

            var keep = Enumerable.Range(0, actual.Length)
                .Where(i => !double.IsNaN(actual[i]) && !double.IsNaN(predicted[i])).ToArray();

            if (keep.Length == 0)
            {
                throw new ArgumentException("There are no usable rows: the input is empty or every pair contains NaN.");
            }

            return (keep.Select(i => actual[i]).ToArray(), keep.Select(i => predicted[i]).ToArray());
        }
    }
}
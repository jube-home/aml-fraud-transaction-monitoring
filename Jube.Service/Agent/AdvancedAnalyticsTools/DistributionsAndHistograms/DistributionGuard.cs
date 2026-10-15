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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
{
    internal static class DistributionGuard
    {
        public static double[] Values(double[]? values, string name, int minimum = 1)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException($"{name} is empty: supply at least {minimum} number(s).", name);
            }

            if (values.Length < minimum)
            {
                throw new ArgumentException(
                    $"{name} has {values.Length} value(s) but at least {minimum} are needed for this tool.", name);
            }

            for (var i = 0; i < values.Length; i++)
            {
                if (!double.IsFinite(values[i]))
                {
                    throw new ArgumentException(
                        $"{name}[{i}] is {values[i]}: remove missing (NaN) or infinite entries before calling this tool.",
                        name);
                }
            }

            return values;
        }

        public static double[] Points(double[]? points, string name)
        {
            if (points == null)
            {
                throw new ArgumentException($"{name} must be supplied (it may be empty to get an empty result).", name);
            }

            for (var i = 0; i < points.Length; i++)
            {
                if (double.IsNaN(points[i]))
                {
                    throw new ArgumentException($"{name}[{i}] is NaN: remove missing entries first.", name);
                }
            }

            return points;
        }

        public static int PositiveCount(int value, string name, int maximum = 100000)
        {
            if (value < 1 || value > maximum)
            {
                throw new ArgumentException($"{name} must be between 1 and {maximum}, got {value}.", name);
            }

            return value;
        }

        public static double Probability(double p, string name, bool inclusive = true)
        {
            var ok = inclusive ? p >= 0 && p <= 1 : p > 0 && p < 1;

            if (double.IsNaN(p) || !ok)
            {
                throw new ArgumentException(
                    inclusive
                        ? $"{name} must be a probability between 0 and 1, got {p}."
                        : $"{name} must be strictly between 0 and 1, got {p}.", name);
            }

            return p;
        }

        public static double[] Probabilities(double[]? probabilities, string name, bool inclusive = true)
        {
            if (probabilities == null || probabilities.Length == 0)
            {
                throw new ArgumentException($"{name} is empty: supply at least one probability between 0 and 1.", name);
            }

            foreach (var p in probabilities)
            {
                Probability(p, name, inclusive);
            }

            return probabilities;
        }

        public static double? N(double value)
        {
            return double.IsFinite(value) ? value : null;
        }

        public static double?[] N(double[] values)
        {
            var result = new double?[values.Length];

            for (var i = 0; i < values.Length; i++)
            {
                result[i] = N(values[i]);
            }

            return result;
        }
    }
}
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

using System.Globalization;
using Accord.Statistics;
using Jube.Service.Agent.AdvancedAnalyticsTools.DescriptiveStatistics.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DescriptiveStatistics
{
    public enum DescriptiveQuantileInterpolation
    {
        Weibull = 6,
        Linear = 7,
        InverseEmpiricalCdf = 1,
        AveragedInverseEmpiricalCdf = 2,
        NearestEven = 3,
        LinearEmpiricalCdf = 4,
        PiecewiseLinearMidpoint = 5,
        MedianUnbiased = 8,
        NormalUnbiased = 9
    }

    public enum DescriptiveRankTieMethod
    {
        Average = 0,
        Ordinal = 1,
        Minimum = 2,
        Maximum = 3,
        Dense = 4
    }

    public enum DescriptiveWindowStatistic
    {
        Mean = 0,
        Sum = 1,
        Median = 2,
        Minimum = 3,
        Maximum = 4,
        StandardDeviation = 5,
        Variance = 6,
        Range = 7,
        Skewness = 8,
        Count = 9
    }

    public enum DescriptiveStandardisationScale
    {
        StandardDeviation = 0,
        MedianAbsoluteDeviation = 1,
        InterquartileRange = 2
    }

    public enum DescriptiveAngleUnit
    {
        Radians = 0,
        Degrees = 1,
        Hours24 = 2,
        Days7 = 3,
        Months12 = 4
    }

    internal static class DescriptiveInput
    {
        public const int MaximumTableCells = 20_000_000;

        public static QuantileMethod Map(DescriptiveQuantileInterpolation interpolation)
        {
            if (!Enum.IsDefined(typeof(DescriptiveQuantileInterpolation), interpolation))
            {
                throw new ArgumentException(
                    "Unknown interpolation. Use Linear (the common spreadsheet default) or Weibull.",
                    nameof(interpolation));
            }

            return (QuantileMethod)(int)interpolation;
        }

        public static double[] Clean(double[] values, string name, bool ignoreMissing, int minimum, out int missing)
        {
            if (values == null)
            {
                throw new ArgumentException($"{name} is required. Supply an array of numbers.", name);
            }

            var kept = new List<double>(values.Length);
            missing = 0;

            for (var i = 0; i < values.Length; i++)
            {
                if (double.IsNaN(values[i]))
                {
                    if (!ignoreMissing)
                    {
                        throw new ArgumentException(
                            $"{name} contains a missing (NaN) value at index {i}. Set ignoreMissing to true to drop missing values, or remove them first.",
                            name);
                    }

                    missing++;
                }
                else
                {
                    kept.Add(values[i]);
                }
            }

            if (kept.Count < minimum)
            {
                throw new ArgumentException(
                    minimum <= 1
                        ? $"{name} has no usable values. Supply at least one non-missing number."
                        : $"{name} has {kept.Count} usable value(s) but at least {minimum} are needed for this statistic.",
                    name);
            }

            return kept.ToArray();
        }

        public static double[] Finite(double[] values, string name, bool ignoreMissing, int minimum, out int missing)
        {
            var cleaned = Clean(values, name, ignoreMissing, minimum, out missing);

            for (var i = 0; i < cleaned.Length; i++)
            {
                if (double.IsInfinity(cleaned[i]))
                {
                    throw new ArgumentException(
                        $"{name} contains an infinite value. Remove or cap infinite values before using this tool.",
                        name);
                }
            }

            return cleaned;
        }

        public static DescriptiveScalar Scalar(double value, int count, int missing)
        {
            return new DescriptiveScalar { Value = value, Count = count, MissingIgnored = missing };
        }

        public static void SameLength(double[] first, string firstName, double[] second, string secondName)
        {
            if (first == null)
            {
                throw new ArgumentException($"{firstName} is required.", firstName);
            }

            if (second == null)
            {
                throw new ArgumentException($"{secondName} is required.", secondName);
            }

            if (first.Length != second.Length)
            {
                throw new ArgumentException(
                    $"{firstName} has {first.Length} entries but {secondName} has {second.Length}. They must have the same length, one entry per observation.",
                    secondName);
            }
        }

        public static double[] Weights(double[] values, double[] weights, string name)
        {
            SameLength(values, "values", weights, name);

            for (var i = 0; i < weights.Length; i++)
            {
                if (double.IsNaN(weights[i]) || double.IsInfinity(weights[i]) || weights[i] < 0)
                {
                    throw new ArgumentException(
                        $"{name} must be finite and not negative; index {i} is {weights[i].ToString(CultureInfo.InvariantCulture)}.",
                        name);
                }
            }

            return weights;
        }

        public static void PairClean(double[] values, double[] weights, string weightsName, bool ignoreMissing,
            out double[] cleanValues, out double[] cleanWeights, out int missing)
        {
            Weights(values, weights, weightsName);
            var v = new List<double>();
            var w = new List<double>();
            missing = 0;

            for (var i = 0; i < values.Length; i++)
            {
                if (double.IsNaN(values[i]))
                {
                    if (!ignoreMissing)
                    {
                        throw new ArgumentException(
                            $"values contains a missing (NaN) value at index {i}. Set ignoreMissing to true to drop it together with its weight.",
                            "values");
                    }

                    missing++;
                    continue;
                }

                v.Add(values[i]);
                w.Add(weights[i]);
            }

            if (v.Count == 0)
            {
                throw new ArgumentException("values has no usable values. Supply at least one non-missing number.",
                    "values");
            }

            cleanValues = v.ToArray();
            cleanWeights = w.ToArray();
        }

        public static void RequireProbability(double p, string name)
        {
            if (double.IsNaN(p) || p < 0 || p > 1)
            {
                throw new ArgumentException(
                    $"{name} must be between 0 and 1 inclusive; got {p.ToString(CultureInfo.InvariantCulture)}.", name);
            }
        }

        public static double[] Sorted(double[] values)
        {
            var copy = (double[])values.Clone();
            Array.Sort(copy);

            return copy;
        }

        public static string[] ValidateTable(double[]?[]? rows, string[]? columnNames, out int columns)
        {
            if (rows == null || rows.Length == 0)
            {
                throw new ArgumentException(
                    "rows is empty. Supply one array of numbers per case, all the same length.", nameof(rows));
            }

            if (rows[0] == null)
            {
                throw new ArgumentException("rows contains a null row at index 0.", nameof(rows));
            }

            columns = rows[0]!.Length;

            if (columns == 0)
            {
                throw new ArgumentException("rows has zero columns. Each row must contain at least one number.",
                    nameof(rows));
            }

            if ((long)rows.Length * columns > MaximumTableCells)
            {
                throw new ArgumentException(
                    $"rows has more than {MaximumTableCells} cells; summarise a sample of the rows instead.",
                    nameof(rows));
            }

            for (var i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null || rows[i]!.Length != columns)
                {
                    throw new ArgumentException(
                        $"Row {i} has {(rows[i] == null ? 0 : rows[i]!.Length)} values but row 0 has {columns}. All rows must have the same length.",
                        nameof(rows));
                }
            }

            if (columnNames == null || columnNames.Length == 0)
            {
                return Enumerable.Range(0, columns).Select(i => "column" + i.ToString(CultureInfo.InvariantCulture))
                    .ToArray();
            }

            if (columnNames.Length != columns)
            {
                throw new ArgumentException(
                    $"columnNames has {columnNames.Length} names but rows have {columns} columns. Give one name per column in column order, or leave it empty for automatic names.",
                    nameof(columnNames));
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var name in columnNames)
            {
                if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                {
                    throw new ArgumentException("columnNames must be non-empty and unique.", nameof(columnNames));
                }
            }

            return columnNames;
        }

        public static double[] Column(double[][] rows, int column)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                result[i] = rows[i][column];
            }

            return result;
        }

        public static double[] NonMissing(double[] values)
        {
            var list = new List<double>(values.Length);

            foreach (var v in values)
            {
                if (!double.IsNaN(v))
                {
                    list.Add(v);
                }
            }

            return list.ToArray();
        }

        public static string[] CleanLabels(string?[] values, string name, bool ignoreMissing, out int missing)
        {
            if (values == null)
            {
                throw new ArgumentException($"{name} is required. Supply an array of text labels.", name);
            }

            var kept = new List<string>(values.Length);
            missing = 0;

            foreach (var v in values)
            {
                if (v == null)
                {
                    if (!ignoreMissing)
                    {
                        throw new ArgumentException(
                            $"{name} contains a missing (null) value. Set ignoreMissing to true to count and skip them.",
                            name);
                    }

                    missing++;
                }
                else
                {
                    kept.Add(v);
                }
            }

            if (kept.Count == 0)
            {
                throw new ArgumentException($"{name} has no usable values. Supply at least one label.", name);
            }

            return kept.ToArray();
        }
    }

    public enum DescriptiveWeightKind
    {
        Frequency = 0,
        Reliability = 1
    }

    public enum DescriptiveEntropyUnit
    {
        Bits = 0,
        Nats = 1,
        Hartleys = 2
    }

    public enum DescriptivePercentileRankKind
    {
        Weak = 0,
        Strict = 1,
        Mean = 2
    }

    public enum DescriptiveFrequencyOrder
    {
        ByCountDescending = 0,
        ByValueAscending = 1,
        ByFirstAppearance = 2
    }

    public enum DescriptiveValueTransform
    {
        NaturalLog = 0,
        Log10 = 1,
        Log2 = 2,
        Log1P = 3,
        SquareRoot = 4,
        CubeRoot = 5,
        Square = 6,
        Reciprocal = 7,
        Absolute = 8,
        Sign = 9,
        Negate = 10,
        SignedLog1P = 11
    }

    public enum DescriptiveCentre
    {
        Mean = 0,
        Median = 1
    }
}
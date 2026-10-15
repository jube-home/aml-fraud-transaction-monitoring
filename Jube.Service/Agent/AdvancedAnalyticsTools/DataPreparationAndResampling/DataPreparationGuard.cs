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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling
{
    internal static class DataPreparationGuard
    {
        public static bool IsMissing(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value);
        }

        public static int Table(double[]?[]? data, string parameterName)
        {
            if (data == null)
            {
                throw new ArgumentException("The table is missing. Supply one array of numbers per row.",
                    parameterName);
            }

            if (data.Length == 0)
            {
                throw new ArgumentException("The table has no rows. Supply at least one row.", parameterName);
            }

            if (data.Any(r => r == null))
            {
                throw new ArgumentException("A row is null. Every row must be an array of numbers.", parameterName);
            }

            var columns = data[0]!.Length;

            if (columns == 0)
            {
                throw new ArgumentException("The rows have no columns. Supply at least one column.", parameterName);
            }

            for (var i = 1; i < data.Length; i++)
            {
                if (data[i]!.Length != columns)
                {
                    throw new ArgumentException(
                        $"Row {i} has {data[i]!.Length} values but row 0 has {columns}. Every row must have the same number of columns.",
                        parameterName);
                }
            }

            return columns;
        }

        public static string[] Names(string[]? names, int columns, string parameterName, string prefix = "column_")
        {
            if (names == null || names.Length == 0)
            {
                return Enumerable.Range(0, columns).Select(i => prefix + i).ToArray();
            }

            if (names.Length != columns)
            {
                throw new ArgumentException(
                    $"There are {names.Length} names but {columns} columns. Give one name per column in column order, or leave the names empty.",
                    parameterName);
            }

            if (names.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("A column name is empty. Every name must be non-blank.", parameterName);
            }

            var duplicate = names.GroupBy(n => n, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);

            if (duplicate != null)
            {
                throw new ArgumentException(
                    $"The column name '{duplicate.Key}' appears more than once. Names must be unique.",
                    parameterName);
            }

            return names.ToArray();
        }

        public static double[] Column(double[][] data, int column)
        {
            var result = new double[data.Length];

            for (var i = 0; i < data.Length; i++)
            {
                result[i] = data[i][column];
            }

            return result;
        }

        public static double[] FiniteColumn(double[][] data, int column)
        {
            return Column(data, column).Where(v => !IsMissing(v)).ToArray();
        }

        public static double[][] Build(int rows, int columns, Func<int, int, double> cell)
        {
            var result = new double[rows][];

            for (var i = 0; i < rows; i++)
            {
                result[i] = new double[columns];

                for (var j = 0; j < columns; j++)
                {
                    result[i][j] = cell(i, j);
                }
            }

            return result;
        }

        public static double[][] Clone(double[][] data)
        {
            return data.Select(r => (double[])r.Clone()).ToArray();
        }

        public static void Vector(double[]? values, string parameterName, int minimum = 1)
        {
            if (values == null)
            {
                throw new ArgumentException("The values are missing. Supply an array of numbers.", parameterName);
            }

            if (values.Length < minimum)
            {
                throw new ArgumentException(
                    $"At least {minimum} value(s) are needed but {values.Length} were given.", parameterName);
            }
        }

        public static double[] FiniteVector(double[]? values, string parameterName, int minimum = 1)
        {
            Vector(values, parameterName, 0);
            var finite = values!.Where(v => !IsMissing(v)).ToArray();

            if (finite.Length < minimum)
            {
                throw new ArgumentException(
                    $"At least {minimum} finite value(s) are needed but only {finite.Length} were found. NaN and infinite values are ignored.",
                    parameterName);
            }

            return finite;
        }

        public static void SameLength(int expected, int actual, string parameterName, string other)
        {
            if (expected != actual)
            {
                throw new ArgumentException(
                    $"The length is {actual} but {other} has {expected}. They must line up row for row.",
                    parameterName);
            }
        }

        public static void Fraction(double value, string parameterName, bool inclusiveZero = true,
            bool inclusiveOne = true)
        {
            var low = inclusiveZero ? value < 0 : value <= 0;
            var high = inclusiveOne ? value > 1 : value >= 1;

            if (double.IsNaN(value) || low || high)
            {
                throw new ArgumentException($"The value {value} must be a fraction between 0 and 1.", parameterName);
            }
        }

        public static void Positive(int value, string parameterName)
        {
            if (value < 1)
            {
                throw new ArgumentException($"The value {value} must be at least 1.", parameterName);
            }
        }

        public static void Binary(int[]? labels, string parameterName)
        {
            if (labels == null || labels.Length == 0)
            {
                throw new ArgumentException("The labels are missing. Supply one 0 or 1 per row.", parameterName);
            }

            if (labels.Any(l => l != 0 && l != 1))
            {
                throw new ArgumentException("Labels must be 0 or 1 only. Recode the outcome so the event is 1.",
                    parameterName);
            }
        }

        public static void Labels(int[]? labels, string parameterName)
        {
            if (labels == null || labels.Length == 0)
            {
                throw new ArgumentException("The labels are missing. Supply one class label per row.", parameterName);
            }
        }

        public static int[] Range(int count)
        {
            return Enumerable.Range(0, count).ToArray();
        }

        public static int[] Complement(int[] indices, int count)
        {
            var held = new HashSet<int>(indices);

            return Enumerable.Range(0, count).Where(i => !held.Contains(i)).ToArray();
        }

        public static void Shuffle(int[] values, Random random)
        {
            for (var i = values.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }

        public static double Quantile(double[] sorted, double q)
        {
            var position = q * (sorted.Length - 1);
            var lower = (int)Math.Floor(position);
            var upper = Math.Min(lower + 1, sorted.Length - 1);

            return sorted[lower] + (position - lower) * (sorted[upper] - sorted[lower]);
        }

        public static int ColumnIndex(string[] names, string name, string parameterName)
        {
            var index = Array.IndexOf(names, name);

            if (index < 0)
            {
                throw new ArgumentException(
                    $"The column '{name}' is not one of the column names ({string.Join(", ", names)}).", parameterName);
            }

            return index;
        }

        public static FeatureTableResult SelectColumns(double[][] data, string[] names, string[]? keep,
            string parameterName)
        {
            if (keep == null || keep.Length == 0)
            {
                throw new ArgumentException("Name at least one column to keep.", parameterName);
            }

            var indexes = keep.Select(k => ColumnIndex(names, k, parameterName)).ToArray();

            return new FeatureTableResult(keep.ToArray(),
                data.Select(r => indexes.Select(j => r[j]).ToArray()).ToArray());
        }
    }
}
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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering
{
    internal static class MultivariateGuard
    {
        public static double[][] Matrix(double[][]? data, string name, int minimumRows, int maximumRows,
            int minimumColumns = 1, int maximumColumns = MultivariateLimits.MaximumColumns)
        {
            if (data == null)
            {
                throw new ArgumentException($"{name} is required: pass one array of numbers per row.", name);
            }

            if (data.Length < minimumRows)
            {
                throw new ArgumentException(
                    $"{name} has {data.Length} row(s) but at least {minimumRows} are needed for this analysis.", name);
            }

            if (data.Length > maximumRows)
            {
                throw new ArgumentException(
                    $"{name} has {data.Length} rows but this tool accepts at most {maximumRows}. Sample or aggregate the rows first.",
                    name);
            }

            var columns = -1;

            for (var row = 0; row < data.Length; row++)
            {
                var values = data[row];

                if (values == null)
                {
                    throw new ArgumentException($"{name} row {row} is null.", name);
                }

                if (columns < 0)
                {
                    columns = values.Length;

                    if (columns < minimumColumns)
                    {
                        throw new ArgumentException(
                            $"{name} rows have {columns} column(s) but at least {minimumColumns} are needed.", name);
                    }

                    if (columns > maximumColumns)
                    {
                        throw new ArgumentException(
                            $"{name} rows have {columns} columns but this tool accepts at most {maximumColumns}. Select fewer variables first.",
                            name);
                    }
                }
                else if (values.Length != columns)
                {
                    throw new ArgumentException(
                        $"{name} row {row} has {values.Length} values but row 0 has {columns}; every row must have the same length.",
                        name);
                }

                for (var column = 0; column < values.Length; column++)
                {
                    if (double.IsNaN(values[column]) || double.IsInfinity(values[column]))
                    {
                        throw new ArgumentException(
                            $"{name} row {row} column {column} is not a finite number. Remove or impute missing and infinite values before calling this tool.",
                            name);
                    }
                }
            }

            return data;
        }

        public static double[][] Copy(double[][] data)
        {
            var copy = new double[data.Length][];

            for (var i = 0; i < data.Length; i++)
            {
                copy[i] = (double[])data[i].Clone();
            }

            return copy;
        }

        public static double[] Vector(double[]? values, string name, int minimumLength, int maximumLength)
        {
            if (values == null)
            {
                throw new ArgumentException($"{name} is required: pass an array of numbers.", name);
            }

            if (values.Length < minimumLength)
            {
                throw new ArgumentException(
                    $"{name} has {values.Length} value(s) but at least {minimumLength} are needed.", name);
            }

            if (values.Length > maximumLength)
            {
                throw new ArgumentException(
                    $"{name} has {values.Length} values but at most {maximumLength} are accepted.",
                    name);
            }

            for (var i = 0; i < values.Length; i++)
            {
                if (double.IsNaN(values[i]) || double.IsInfinity(values[i]))
                {
                    throw new ArgumentException($"{name} value at index {i} is not a finite number.", name);
                }
            }

            return values;
        }

        public static int[] Labels(int[]? labels, string name, int expectedLength, string dataName)
        {
            if (labels == null)
            {
                throw new ArgumentException($"{name} is required: pass one integer label per row of {dataName}.", name);
            }

            if (labels.Length != expectedLength)
            {
                throw new ArgumentException(
                    $"{name} has {labels.Length} values but {dataName} has {expectedLength} rows; they must match one to one.",
                    name);
            }

            return labels;
        }

        public static int Bounded(int value, string name, int minimum, int maximum)
        {
            if (value < minimum || value > maximum)
            {
                throw new ArgumentException($"{name} must be between {minimum} and {maximum} but was {value}.", name);
            }

            return value;
        }

        public static double BoundedDouble(double value, string name, double minimum, double maximum)
        {
            if (double.IsNaN(value) || value < minimum || value > maximum)
            {
                throw new ArgumentException($"{name} must be between {minimum} and {maximum} but was {value}.", name);
            }

            return value;
        }

        public static double Positive(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                throw new ArgumentException($"{name} must be a finite number greater than zero but was {value}.", name);
            }

            return value;
        }

        public static void RequireVariation(double[][] data, string name)
        {
            var columns = data[0].Length;

            for (var column = 0; column < columns; column++)
            {
                var first = data[0][column];
                var varies = false;

                for (var row = 1; row < data.Length; row++)
                {
                    if (data[row][column] != first)
                    {
                        varies = true;
                        break;
                    }
                }

                if (!varies)
                {
                    throw new ArgumentException(
                        $"{name} column {column} is constant, which makes a standardised or correlation based analysis undefined. Drop that column or use the covariance method.",
                        name);
                }
            }
        }

        public static string[] Names(string[]? names, string name, int expected)
        {
            if (names == null || names.Length == 0)
            {
                return Enumerable.Range(0, expected).Select(i => "V" + i).ToArray();
            }

            if (names.Length != expected)
            {
                throw new ArgumentException(
                    $"{name} has {names.Length} names but the data has {expected} columns; give one name per column or leave it empty.",
                    name);
            }

            if (names.Any(string.IsNullOrWhiteSpace) || names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            {
                throw new ArgumentException($"{name} must contain unique, non empty names.", name);
            }

            return names;
        }
    }
}
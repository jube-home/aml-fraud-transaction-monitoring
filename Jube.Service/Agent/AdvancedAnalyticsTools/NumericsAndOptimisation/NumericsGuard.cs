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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.NumericsAndOptimisation
{
    public static class NumericsGuard
    {
        public const int MaxMatrixDimension = 300;
        public const int MaxVectorLength = 200000;
        public const int MaxPolynomialCoefficients = 40;
        public const int MaxVariables = 50;
        public const int MaxIterations = 100000;
        public const int MaxSamples = 5000000;

        public static void RequireVector(double[]? values, string name, int minimumLength = 1,
            int maximumLength = MaxVectorLength, bool finite = true)
        {
            if (values == null)
            {
                throw new ArgumentException($"'{name}' is required: supply an array of numbers.", name);
            }

            if (values.Length < minimumLength)
            {
                throw new ArgumentException(
                    $"'{name}' needs at least {minimumLength} value(s) but has {values.Length}.",
                    name);
            }

            if (values.Length > maximumLength)
            {
                throw new ArgumentException(
                    $"'{name}' has {values.Length} values but at most {maximumLength} are allowed; send a shorter array.",
                    name);
            }

            if (finite)
            {
                for (var i = 0; i < values.Length; i++)
                {
                    if (!double.IsFinite(values[i]))
                    {
                        throw new ArgumentException(
                            $"'{name}' contains a NaN or infinite value at index {i}; remove or replace it before calling.",
                            name);
                    }
                }
            }
        }

        public static void RequireMatrix(double[][]? matrix, string name, out int rows, out int columns,
            bool square = false, bool finite = true, bool symmetric = false)
        {
            if (matrix == null || matrix.Length == 0)
            {
                throw new ArgumentException($"'{name}' is required: supply a non-empty array of equal-length rows.",
                    name);
            }

            rows = matrix.Length;

            if (matrix[0] == null || matrix[0].Length == 0)
            {
                throw new ArgumentException($"'{name}' has an empty first row; every row needs at least one column.",
                    name);
            }

            columns = matrix[0].Length;

            if (rows > MaxMatrixDimension || columns > MaxMatrixDimension)
            {
                throw new ArgumentException(
                    $"'{name}' is {rows} by {columns} but at most {MaxMatrixDimension} rows and {MaxMatrixDimension} columns are allowed.",
                    name);
            }

            for (var i = 0; i < rows; i++)
            {
                if (matrix[i] == null || matrix[i].Length != columns)
                {
                    throw new ArgumentException(
                        $"'{name}' is ragged: row {i} does not have {columns} columns like row 0.",
                        name);
                }

                if (finite)
                {
                    for (var j = 0; j < columns; j++)
                    {
                        if (!double.IsFinite(matrix[i][j]))
                        {
                            throw new ArgumentException(
                                $"'{name}' contains a NaN or infinite value at row {i}, column {j}; remove or replace it before calling.",
                                name);
                        }
                    }
                }
            }

            if ((square || symmetric) && rows != columns)
            {
                throw new ArgumentException($"'{name}' must be square but is {rows} by {columns}.", name);
            }

            if (symmetric)
            {
                for (var i = 0; i < rows; i++)
                {
                    for (var j = i + 1; j < columns; j++)
                    {
                        var scale = Math.Max(1.0, Math.Max(Math.Abs(matrix[i][j]), Math.Abs(matrix[j][i])));

                        if (Math.Abs(matrix[i][j] - matrix[j][i]) > 1e-9 * scale)
                        {
                            throw new ArgumentException(
                                $"'{name}' must be symmetric but element ({i},{j}) differs from ({j},{i}).", name);
                        }
                    }
                }
            }
        }

        public static void RequireFinite(double value, string name)
        {
            if (!double.IsFinite(value))
            {
                throw new ArgumentException($"'{name}' must be a finite number but was {value}.", name);
            }
        }

        public static void RequireInRange(int value, string name, int minimum, int maximum)
        {
            if (value < minimum || value > maximum)
            {
                throw new ArgumentException($"'{name}' must be between {minimum} and {maximum} but was {value}.", name);
            }
        }

        public static void RequirePositive(double value, string name)
        {
            if (!(value > 0) || !double.IsFinite(value))
            {
                throw new ArgumentException($"'{name}' must be a finite number greater than 0 but was {value}.", name);
            }
        }

        public static double[][] Copy(double[][] matrix)
        {
            var result = new double[matrix.Length][];

            for (var i = 0; i < matrix.Length; i++)
            {
                result[i] = (double[])matrix[i].Clone();
            }

            return result;
        }

        public static double[,] ToRectangular(double[][] matrix)
        {
            var rows = matrix.Length;
            var columns = matrix[0].Length;
            var result = new double[rows, columns];

            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < columns; j++)
                {
                    result[i, j] = matrix[i][j];
                }
            }

            return result;
        }

        public static double[][] FromRectangular(double[,] matrix)
        {
            var rows = matrix.GetLength(0);
            var columns = matrix.GetLength(1);
            var result = new double[rows][];

            for (var i = 0; i < rows; i++)
            {
                result[i] = new double[columns];

                for (var j = 0; j < columns; j++)
                {
                    result[i][j] = matrix[i, j];
                }
            }

            return result;
        }

        public static NumericsVectorResult ToVectorResult(double[] values)
        {
            return new NumericsVectorResult
            {
                Values = values,
                Count = values.Length,
                NonFiniteCount = values.Count(v => !double.IsFinite(v))
            };
        }

        public static NumericsMatrixResult ToMatrixResult(double[][] matrix)
        {
            return new NumericsMatrixResult
            {
                Matrix = matrix,
                Rows = matrix.Length,
                Columns = matrix.Length == 0 ? 0 : matrix[0].Length
            };
        }
    }
}
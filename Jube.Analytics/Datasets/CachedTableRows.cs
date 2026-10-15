/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.Analytics.Datasets
{
    public static class CachedTableRows
    {
        public static (double[][] Inputs, int[] Labels) Split(double[] values, int columns, int rows,
            int labelColumn, int variableCount, CancellationToken token = default)
        {
            if (columns < 2 || rows < 1 || (long)rows * columns != values.Length)
            {
                throw new InvalidOperationException(
                    "The cached input is not a table with predictor columns and a label column.");
            }

            var resolvedLabelColumn = labelColumn < 0 ? columns - 1 : labelColumn;

            if (resolvedLabelColumn >= columns)
            {
                throw new InvalidOperationException("The label column lies outside the cached table.");
            }

            if (columns - 1 != variableCount)
            {
                throw new InvalidOperationException(
                    "The cached table has a different number of predictor columns than variable names.");
            }

            var inputs = new double[rows][];
            var labels = new int[rows];

            for (var r = 0; r < rows; r++)
            {
                token.ThrowIfCancellationRequested();
                var row = new double[columns - 1];
                var target = 0;

                for (var c = 0; c < columns; c++)
                {
                    var value = values[r * columns + c];

                    if (c == resolvedLabelColumn)
                    {
                        if (value != 0 && Math.Abs(value - 1) > 0.0001)
                        {
                            throw new InvalidOperationException(
                                "The label column of the cached table holds a value other than 0 or 1.");
                        }

                        labels[r] = (int)value;
                    }
                    else
                    {
                        row[target++] = value;
                    }
                }

                inputs[r] = row;
            }

            return (inputs, labels);
        }
    }
}
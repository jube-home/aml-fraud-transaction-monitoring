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
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    internal static class RegressionInput
    {
        public const int MaximumCells = 50_000_000;

        public static void RequireArray(double[]? values, string name, int minimum)
        {
            if (values == null)
            {
                throw new ArgumentException($"{name} is required. Supply an array of numbers.", name);
            }

            if (values.Length < minimum)
            {
                throw new ArgumentException(
                    $"{name} has {values.Length} value(s) but at least {minimum} are needed for this tool.", name);
            }
        }

        public static string[] ResolveNames(string[]? names, int count, string parameterName)
        {
            if (names == null || names.Length == 0)
            {
                return Enumerable.Range(1, count).Select(i => $"x{i.ToString(CultureInfo.InvariantCulture)}").ToArray();
            }

            if (names.Length != count)
            {
                throw new ArgumentException(
                    $"{parameterName} has {names.Length} name(s) but each input row has {count} column(s). Supply one name per column, or leave the names empty.",
                    parameterName);
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException($"{parameterName} contains an empty name. Give every column a name.",
                        parameterName);
                }

                if (!seen.Add(name))
                {
                    throw new ArgumentException($"{parameterName} repeats the name '{name}'. Names must be unique.",
                        parameterName);
                }
            }

            return names.ToArray();
        }

        public static int ColumnCount(double[][]? inputs, string name)
        {
            if (inputs == null || inputs.Length == 0)
            {
                throw new ArgumentException(
                    $"{name} is required: one array of numbers per case, all the same length.", name);
            }

            if (inputs[0] == null)
            {
                throw new ArgumentException($"{name} row 0 is missing.", name);
            }

            var columns = inputs[0].Length;

            if (columns == 0)
            {
                throw new ArgumentException($"{name} rows are empty. Supply at least one column.", name);
            }

            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] == null || inputs[i].Length != columns)
                {
                    throw new ArgumentException(
                        $"{name} row {i} has {inputs[i]?.Length ?? 0} column(s) but row 0 has {columns}. Every row must have the same number of columns.",
                        name);
                }
            }

            if ((long)columns * inputs.Length > MaximumCells)
            {
                throw new ArgumentException(
                    $"{name} has {(long)columns * inputs.Length} cells which is more than the {MaximumCells} this tool accepts. Sample the rows or drop columns first.",
                    name);
            }

            return columns;
        }

        public static PreparedRegressionData Prepare(double[][]? inputs, double[]? outputs, string[]? names,
            double[]? weights, bool dropIncompleteRows, int minimumRows, string inputsName = "inputs",
            string outputsName = "outputs")
        {
            var columns = ColumnCount(inputs, inputsName);
            RequireArray(outputs, outputsName, 1);

            if (inputs!.Length != outputs!.Length)
            {
                throw new ArgumentException(
                    $"{inputsName} has {inputs.Length} row(s) but {outputsName} has {outputs.Length} value(s). They must be the same length, one outcome per row.",
                    outputsName);
            }

            if (weights != null && weights.Length != outputs.Length)
            {
                throw new ArgumentException(
                    $"weights has {weights.Length} value(s) but there are {outputs.Length} row(s). Supply one weight per row.",
                    nameof(weights));
            }

            var resolved = ResolveNames(names, columns, "variableNames");
            var kept = new List<int>(inputs.Length);

            for (var i = 0; i < inputs.Length; i++)
            {
                var missing = double.IsNaN(outputs[i]) || (weights != null && double.IsNaN(weights[i]));

                for (var j = 0; j < columns && !missing; j++)
                {
                    missing = double.IsNaN(inputs[i][j]);
                }

                if (double.IsInfinity(outputs[i]) || (weights != null && double.IsInfinity(weights[i])))
                {
                    throw new ArgumentException(
                        $"Row {i} contains an infinite value. Remove or cap infinite values before fitting.",
                        outputsName);
                }

                for (var j = 0; j < columns; j++)
                {
                    if (double.IsInfinity(inputs[i][j]))
                    {
                        throw new ArgumentException(
                            $"{inputsName} row {i} column {j} is infinite. Remove or cap infinite values before fitting.",
                            inputsName);
                    }
                }

                if (missing)
                {
                    if (!dropIncompleteRows)
                    {
                        throw new ArgumentException(
                            $"Row {i} has a missing (NaN) value. Set dropIncompleteRows to true to fit on complete rows only, or fill the gap first.",
                            inputsName);
                    }

                    continue;
                }

                kept.Add(i);
            }

            if (weights != null)
            {
                foreach (var index in kept)
                {
                    if (!(weights[index] > 0))
                    {
                        throw new ArgumentException(
                            $"weights[{index}] is {weights[index]}; every weight must be greater than zero. Remove rows you want ignored instead.",
                            nameof(weights));
                    }
                }
            }

            if (kept.Count < minimumRows)
            {
                throw new ArgumentException(
                    $"Only {kept.Count} usable row(s) remain but at least {minimumRows} are needed for this tool.",
                    inputsName);
            }

            return new PreparedRegressionData
            {
                X = kept.Select(i => (double[])inputs[i].Clone()).ToArray(),
                Y = kept.Select(i => outputs[i]).ToArray(),
                Weights = weights == null ? null : kept.Select(i => weights[i]).ToArray(),
                Names = resolved,
                Dropped = inputs.Length - kept.Count,
                KeptRows = kept.ToArray()
            };
        }

        public static double[][] Columns(double[][] rows)
        {
            var columns = rows[0].Length;
            var result = new double[columns][];

            for (var j = 0; j < columns; j++)
            {
                result[j] = new double[rows.Length];

                for (var i = 0; i < rows.Length; i++)
                {
                    result[j][i] = rows[i][j];
                }
            }

            return result;
        }

        public static double[][] FromColumns(double[][] columns)
        {
            var rows = columns[0].Length;
            var result = new double[rows][];

            for (var i = 0; i < rows; i++)
            {
                result[i] = new double[columns.Length];

                for (var j = 0; j < columns.Length; j++)
                {
                    result[i][j] = columns[j][i];
                }
            }

            return result;
        }

        public static void RequireLevel(double level, string name)
        {
            if (!(level > 0 && level < 1))
            {
                throw new ArgumentException(
                    $"{name} must be strictly between 0 and 1, for example 0.95 for a 95 percent interval.", name);
            }
        }

        public static void RequireSameLength(double[]? a, double[]? b, string aName, string bName)
        {
            RequireArray(a, aName, 1);
            RequireArray(b, bName, 1);

            if (a!.Length != b!.Length)
            {
                throw new ArgumentException(
                    $"{aName} has {a.Length} value(s) but {bName} has {b.Length}. They are paired observations and must be the same length.",
                    bName);
            }
        }
    }
}
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

using System.Globalization;
using FluentValidation.Results;
using Jube.Analytics.Datasets;
using Jube.AskJooby.DataCache.Models;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Repository.AdvancedAnalyticsTrainingData.Models;

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingData
{
    public static class AdvancedAnalyticsTrainingMatrix
    {
        internal static List<string> Order(CachedTable table, string[] variableNames, string labelColumnName)
        {
            if (variableNames == null || variableNames.Length == 0)
            {
                throw Invalid("variableNames", "VariableNamesRequired", "Name at least one predictor column.");
            }

            if (string.IsNullOrWhiteSpace(labelColumnName))
            {
                throw Invalid("labelColumnName", "LabelColumnRequired", "Name the column holding the outcome.");
            }

            var ordered = new List<string>(variableNames.Length + 1);

            foreach (var name in variableNames)
            {
                if (ordered.Contains(name, StringComparer.Ordinal))
                {
                    throw Invalid("variableNames", "VariableNameRepeated",
                        $"The predictor column '{name}' is named more than once.");
                }

                ordered.Add(Resolve(table, "variableNames", name));
            }

            var label = Resolve(table, "labelColumnName", labelColumnName);

            if (ordered.Contains(label, StringComparer.Ordinal))
            {
                throw Invalid("labelColumnName", "LabelColumnIsAlsoAPredictor",
                    $"The column '{label}' is named both as a predictor and as the label.");
            }

            ordered.Add(label);

            return ordered;
        }

        private static string Resolve(CachedTable table, string property, string name)
        {
            var column = table.Columns.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.Ordinal))
                         ?? table.Columns.FirstOrDefault(c =>
                             string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

            if (column == null)
            {
                throw Invalid(property, "ColumnNotInTable",
                    $"The cached table holds no column called '{name}'. It holds: "
                    + string.Join(", ", table.Columns.Select(c => c.Name)) + ".");
            }

            return column.Name;
        }

        private static byte[] Flatten(CachedTable table, List<string> ordered, string labelColumnName,
            out int missing, out int positives)
        {
            var columns = ordered.Count;
            var values = new double[(long)table.Rows.Count * columns];
            var at = 0;
            missing = 0;
            positives = 0;

            for (var r = 0; r < table.Rows.Count; r++)
            {
                var row = table.Rows[r];

                for (var c = 0; c < columns; c++)
                {
                    var name = ordered[c];
                    var held = row.TryGetValue(name, out var value) ? value : null;
                    var isLabel = c == columns - 1;
                    var number = Number(held, name, r, isLabel);

                    if (isLabel)
                    {
                        if (number != 0 && Math.Abs(number - 1) > 0.0001)
                        {
                            throw Invalid("labelColumnName", "LabelNotBinary",
                                $"Row {r + 1} holds '{Text(held)}' in the label column '{labelColumnName}'. "
                                + "The label must be 0 or 1 in every row.");
                        }

                        positives += (int)number;
                    }
                    else if (double.IsNaN(number))
                    {
                        missing++;
                    }

                    values[at++] = number;
                }
            }

            return AdvancedAnalyticsPacking.Pack(values);
        }

        private static double Number(object? value, string column, int row, bool isLabel)
        {
            switch (value)
            {
                case null when isLabel:
                    throw Invalid("labelColumnName", "LabelMissing",
                        $"Row {row + 1} holds no value in the label column '{column}'.");
                case null:
                    return double.NaN;
                case bool flag:
                    return flag ? 1d : 0d;
                case double number:
                    return number;
                case float number:
                    return number;
                case long number:
                    return number;
                case int number:
                    return number;
                case decimal number:
                    return (double)number;
            }

            var text = value.ToString();

            if (string.IsNullOrWhiteSpace(text))
            {
                return isLabel
                    ? throw Invalid("labelColumnName", "LabelMissing",
                        $"Row {row + 1} holds no value in the label column '{column}'.")
                    : double.NaN;
            }

            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            throw Invalid(isLabel ? "labelColumnName" : "variableNames", "ColumnNotNumeric",
                $"Row {row + 1} holds '{text}' in column '{column}', which is not a number. Convert the column "
                + "in the query that produced the table, or train on a column that is already numeric.");
        }

        private static string Text(object? value)
        {
            return value?.ToString() ?? "null";
        }

        public static AdvancedAnalyticsTrainingMatrixResult Build(CachedTable table, string[] variableNames,
            string labelColumnName)
        {
            var ordered = Order(table, variableNames, labelColumnName);
            var packed = Flatten(table, ordered, labelColumnName, out var missing, out var positives);

            return new AdvancedAnalyticsTrainingMatrixResult
            {
                Packed = packed,
                Rows = table.Rows.Count,
                Columns = ordered.Count,
                MissingValues = missing,
                PositiveLabels = positives
            };
        }

        private static DtoValidationException Invalid(string property, string code, string message)
        {
            return new DtoValidationException(new ValidationResult(
            [
                new ValidationFailure(property, message) { ErrorCode = code }
            ]));
        }
    }
}
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

using System.ComponentModel;
using Jube.Analytics;
using Jube.Analytics.Models;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling
{
    [AdvancedAnalyticsToolGroup("DataPreparationAndResampling")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class MissingValueTools
    {
        [AdvancedAnalyticsTool("ProfileMissingValues")]
        [Description(
            "Profiles missing data in a numeric table before any imputation decision. A cell is missing when it is NaN or infinite. " +
            "Returns per column missing counts and fractions, the number of rows that are fully complete, and the most common missing patterns (which set of columns is missing together) so you can tell random gaps from structural ones. " +
            "Warnings name columns that are entirely missing or mostly missing. An empty table is rejected.")]
        public MissingValueProfile ProfileMissingValues(
            [Description(
                "Table rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("Maximum number of distinct missing patterns to return, most frequent first. Default 10.")]
            int maximumPatterns = 10)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            DataPreparationGuard.Positive(maximumPatterns, nameof(maximumPatterns));

            var profiles = new ColumnMissingness[columns];
            var warnings = new List<string>();

            for (var j = 0; j < columns; j++)
            {
                var missing = data.Count(r => DataPreparationGuard.IsMissing(r[j]));
                profiles[j] = new ColumnMissingness(names[j], missing, missing / (double)data.Length);

                if (missing == data.Length)
                {
                    warnings.Add($"Column '{names[j]}' is entirely missing and cannot be imputed from its own values.");
                }
                else if (missing / (double)data.Length > 0.5)
                {
                    warnings.Add($"Column '{names[j]}' is more than half missing; consider dropping it.");
                }
            }

            var patternCounts = data
                .Select(r => string.Join("|",
                    Enumerable.Range(0, columns).Where(j => DataPreparationGuard.IsMissing(r[j]))
                        .Select(j => names[j])))
                .GroupBy(p => p)
                .Select(g => new MissingPattern(
                    g.Key.Length == 0 ? Array.Empty<string>() : g.Key.Split('|'),
                    g.Count()))
                .OrderByDescending(p => p.Rows)
                .ThenBy(p => string.Join("|", p.MissingColumns), StringComparer.Ordinal)
                .Take(maximumPatterns)
                .ToArray();

            var totalMissing = profiles.Sum(p => p.MissingCount);
            var completeRows = data.Count(r => r.All(v => !DataPreparationGuard.IsMissing(v)));

            return new MissingValueProfile(data.Length, columns, totalMissing,
                totalMissing / (double)(data.Length * columns), completeRows, profiles, patternCounts,
                warnings.ToArray());
        }

        [AdvancedAnalyticsTool("ImputeMissingValues")]
        [Description(
            "Fills missing cells (NaN or infinite) column by column with the mean, median, mode or a constant learned from the supplied rows. " +
            "Returns the imputed table, the fill value used per column (keep these and pass them to ApplyImputer to treat new data identically), how many cells were replaced per column and the exact cells replaced. " +
            "Optionally appends a 0/1 missing indicator column for every column that had gaps, which preserves the information that a value was absent. " +
            "A column with no finite value cannot be learned from: use the Constant strategy for it.")]
        public ImputationResult ImputeMissingValues(
            [Description(
                "Table rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description(
                "Mean, Median, Mode (most frequent value, smallest on ties) or Constant. Default Median which resists outliers.")]
            ImputationStrategy strategy = ImputationStrategy.Median,
            [Description(
                "The value used by the Constant strategy for every column. Ignored by the other strategies. Default 0.")]
            double constant = 0,
            [Description(
                "When true, add a 0/1 indicator column named <column>_missing for each column that had at least one gap. Default false.")]
            bool addMissingIndicators = false)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));

            if (strategy == ImputationStrategy.Constant && !double.IsFinite(constant))
            {
                throw new ArgumentException("The constant must be a finite number.", nameof(constant));
            }

            var fills = new double[columns];

            for (var j = 0; j < columns; j++)
            {
                var finite = DataPreparationGuard.FiniteColumn(data, j);

                if (finite.Length == 0 && strategy != ImputationStrategy.Constant)
                {
                    throw new ArgumentException(
                        $"Column '{names[j]}' has no finite value to learn a {strategy} from. Use the Constant strategy or drop the column.",
                        nameof(data));
                }

                fills[j] = Imputer.Fit(finite, strategy, constant).FillValue;
            }

            var indicators = addMissingIndicators
                ? names.Where((_, j) => data.Any(r => DataPreparationGuard.IsMissing(r[j]))).ToArray()
                : Array.Empty<string>();

            return Fill(data, names, fills, indicators);
        }

        [AdvancedAnalyticsTool("ApplyImputer")]
        [Description(
            "Applies fill values learned earlier by ImputeMissingValues to a new table so training and scoring data are treated identically. " +
            "Each missing cell (NaN or infinite) takes its column's fill value; a fill value of NaN leaves that column unchanged. " +
            "Returns the same result shape as ImputeMissingValues: the table, the fill values, counts and cells replaced. Give the indicator source column names returned earlier to append the same 0/1 indicator columns.")]
        public ImputationResult ApplyImputer(
            [Description(
                "Table rows to fill: one array of numbers per case, in the same column order as when the fill values were learned.")]
            double[][] data,
            [Description(
                "The fill value for each column, in column order, as returned by ImputeMissingValues. NaN means leave that column as is.")]
            double[] fillValues,
            [Description("Optional column names in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description(
                "Names of the columns to add a 0/1 missing indicator for, as used when the imputer was learned. Leave empty for none.")]
            string[]? indicatorSourceColumns = null)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));

            if (fillValues == null || fillValues.Length != columns)
            {
                throw new ArgumentException(
                    $"Give one fill value per column: the table has {columns} columns.", nameof(fillValues));
            }

            var indicators = indicatorSourceColumns ?? Array.Empty<string>();

            foreach (var indicator in indicators)
            {
                DataPreparationGuard.ColumnIndex(names, indicator, nameof(indicatorSourceColumns));
            }

            return Fill(data, names, fillValues, indicators);
        }

        [AdvancedAnalyticsTool("ImputeForwardFill")]
        [Description(
            "Fills missing cells in rows that are in time or sequence order by carrying the last observed value of the same column forward. " +
            "Use it for state-like series (balances, statuses) where the previous value is the best guess. Rows must already be ordered. " +
            "Leading gaps have no previous value and stay NaN unless backfillLeading is true, in which case they take the first observed value. " +
            "Returns the table, the count replaced per column and the exact cells replaced.")]
        public ImputationResult ImputeForwardFill(
            [Description(
                "Table rows in chronological or sequence order: one array of numbers per case. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description(
                "When true, leading gaps before the first observation take that first observed value. Default false.")]
            bool backfillLeading = false)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            var table = DataPreparationGuard.Clone(data);
            var replaced = new List<ReplacedCell>();

            for (var j = 0; j < columns; j++)
            {
                var last = double.NaN;

                for (var i = 0; i < table.Length; i++)
                {
                    if (!DataPreparationGuard.IsMissing(table[i][j]))
                    {
                        last = table[i][j];
                    }
                    else if (!double.IsNaN(last))
                    {
                        table[i][j] = last;
                        replaced.Add(new ReplacedCell(i, names[j], last));
                    }
                }

                if (backfillLeading)
                {
                    var first = Enumerable.Range(0, table.Length)
                        .Where(i => !DataPreparationGuard.IsMissing(data[i][j]))
                        .Select(i => (int?)i)
                        .FirstOrDefault();

                    if (first.HasValue)
                    {
                        for (var i = 0; i < first.Value; i++)
                        {
                            table[i][j] = data[first.Value][j];
                            replaced.Add(new ReplacedCell(i, names[j], data[first.Value][j]));
                        }
                    }
                }
            }

            return Summarise(table, names, Enumerable.Repeat(double.NaN, columns).ToArray(), names, replaced);
        }

        [AdvancedAnalyticsTool("ImputeLinearInterpolation")]
        [Description(
            "Fills gaps in each column of rows that are in order by straight line interpolation between the nearest observed values before and after, treating row position as the x axis. " +
            "Use it for smooth measurements such as rates or exposures. Gaps at the very start or end cannot be interpolated: they stay NaN unless extrapolateEdges is true, which copies the nearest observed value. " +
            "Returns the table, the count replaced per column and the exact cells replaced.")]
        public ImputationResult ImputeLinearInterpolation(
            [Description(
                "Table rows in chronological or sequence order: one array of numbers per case. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description(
                "When true, edge gaps take the nearest observed value; when false they stay NaN. Default false.")]
            bool extrapolateEdges = false)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            var table = DataPreparationGuard.Clone(data);
            var replaced = new List<ReplacedCell>();

            for (var j = 0; j < columns; j++)
            {
                var observed = Enumerable.Range(0, data.Length).Where(i => !DataPreparationGuard.IsMissing(data[i][j]))
                    .ToArray();

                if (observed.Length == 0)
                {
                    continue;
                }

                for (var i = 0; i < data.Length; i++)
                {
                    if (!DataPreparationGuard.IsMissing(data[i][j]))
                    {
                        continue;
                    }

                    var before = observed.LastOrDefault(o => o < i, -1);
                    var after = observed.FirstOrDefault(o => o > i, -1);
                    double value;

                    if (before >= 0 && after >= 0)
                    {
                        var t = (i - before) / (double)(after - before);
                        value = data[before][j] + t * (data[after][j] - data[before][j]);
                    }
                    else if (!extrapolateEdges)
                    {
                        continue;
                    }
                    else
                    {
                        value = before >= 0 ? data[before][j] : data[after][j];
                    }

                    table[i][j] = value;
                    replaced.Add(new ReplacedCell(i, names[j], value));
                }
            }

            return Summarise(table, names, Enumerable.Repeat(double.NaN, columns).ToArray(), names, replaced);
        }

        [AdvancedAnalyticsTool("ImputeNearestNeighbours")]
        [Description(
            "Fills each missing cell with the average of that column over the k most similar rows, where similarity is the Euclidean distance over the columns both rows have observed (rescaled for how many columns overlap). " +
            "Use it when columns are correlated so the row's other values say something about the gap. Optionally standardises columns first so no column dominates the distance. " +
            "A cell with no usable neighbour falls back to the column mean. Returns the table, count replaced per column and the exact cells replaced. Cost grows with the square of the row count.")]
        public ImputationResult ImputeNearestNeighbours(
            [Description(
                "Table rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("Number of nearest rows to average, at least 1. Default 5.")]
            int neighbours = 5,
            [Description(
                "When true, distances are computed on columns scaled to unit standard deviation. Default true.")]
            bool standardiseForDistance = true)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            DataPreparationGuard.Positive(neighbours, nameof(neighbours));

            var means = new double[columns];
            var scales = new double[columns];

            for (var j = 0; j < columns; j++)
            {
                var finite = DataPreparationGuard.FiniteColumn(data, j);

                if (finite.Length == 0)
                {
                    throw new ArgumentException(
                        $"Column '{names[j]}' has no observed value, so neighbours cannot fill it. Drop it or use ImputeMissingValues with the Constant strategy.",
                        nameof(data));
                }

                var fit = Standardisation.Fit(finite);
                means[j] = fit.Mean;
                scales[j] = standardiseForDistance && fit.StandardDeviation > 0 ? fit.StandardDeviation : 1;
            }

            var table = DataPreparationGuard.Clone(data);
            var replaced = new List<ReplacedCell>();

            for (var i = 0; i < data.Length; i++)
            {
                for (var c = 0; c < columns; c++)
                {
                    if (!DataPreparationGuard.IsMissing(data[i][c]))
                    {
                        continue;
                    }

                    var candidates = new List<(double Distance, int Row)>();

                    for (var r = 0; r < data.Length; r++)
                    {
                        if (r == i || DataPreparationGuard.IsMissing(data[r][c]))
                        {
                            continue;
                        }

                        double sum = 0;
                        var overlap = 0;

                        for (var j = 0; j < columns; j++)
                        {
                            if (j == c || DataPreparationGuard.IsMissing(data[i][j]) ||
                                DataPreparationGuard.IsMissing(data[r][j]))
                            {
                                continue;
                            }

                            var d = (data[i][j] - data[r][j]) / scales[j];
                            sum += d * d;
                            overlap++;
                        }

                        if (overlap > 0)
                        {
                            candidates.Add((Math.Sqrt(sum * (columns - 1) / overlap), r));
                        }
                    }

                    var chosen = candidates.OrderBy(x => x.Distance).ThenBy(x => x.Row).Take(neighbours).ToArray();
                    var value = chosen.Length == 0 ? means[c] : chosen.Average(x => data[x.Row][c]);
                    table[i][c] = value;
                    replaced.Add(new ReplacedCell(i, names[c], value));
                }
            }

            return Summarise(table, names, Enumerable.Repeat(double.NaN, columns).ToArray(), names, replaced);
        }

        [AdvancedAnalyticsTool("DropSparseRowsAndColumns")]
        [Description(
            "Removes columns whose missing fraction exceeds a threshold, then rows whose remaining missing fraction exceeds a threshold. " +
            "Use it as listwise or pairwise cleaning before modelling when imputation is not appropriate. " +
            "Returns the reduced table, the names of kept and dropped columns and the original row indices kept and dropped so labels and other arrays can be filtered identically. " +
            "A threshold of 0 drops anything with any gap; a threshold of 1 keeps everything.")]
        public SparseDropResult DropSparseRowsAndColumns(
            [Description(
                "Table rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description(
                "Drop a column when its missing fraction is strictly above this value, between 0 and 1. Default 0.5.")]
            double maximumColumnMissingFraction = 0.5,
            [Description(
                "Drop a row when its missing fraction over the kept columns is strictly above this value, between 0 and 1. Default 0 (any gap drops the row).")]
            double maximumRowMissingFraction = 0)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            DataPreparationGuard.Fraction(maximumColumnMissingFraction, nameof(maximumColumnMissingFraction));
            DataPreparationGuard.Fraction(maximumRowMissingFraction, nameof(maximumRowMissingFraction));

            var keptColumns = Enumerable.Range(0, columns)
                .Where(j => data.Count(r => DataPreparationGuard.IsMissing(r[j])) / (double)data.Length <=
                            maximumColumnMissingFraction)
                .ToArray();

            var keptRows = keptColumns.Length == 0
                ? Array.Empty<int>()
                : Enumerable.Range(0, data.Length)
                    .Where(i => keptColumns.Count(j => DataPreparationGuard.IsMissing(data[i][j])) /
                        (double)keptColumns.Length <= maximumRowMissingFraction)
                    .ToArray();

            var table = keptRows.Select(i => keptColumns.Select(j => data[i][j]).ToArray()).ToArray();

            return new SparseDropResult(table, keptColumns.Select(j => names[j]).ToArray(),
                names.Where((_, j) => !keptColumns.Contains(j)).ToArray(), keptRows,
                DataPreparationGuard.Complement(keptRows, data.Length));
        }

        private static ImputationResult Fill(double[][] data, string[] names, double[] fills, string[] indicators)
        {
            var table = DataPreparationGuard.Clone(data);
            var replaced = new List<ReplacedCell>();

            for (var i = 0; i < table.Length; i++)
            {
                for (var j = 0; j < names.Length; j++)
                {
                    if (DataPreparationGuard.IsMissing(table[i][j]) && !double.IsNaN(fills[j]))
                    {
                        table[i][j] = fills[j];
                        replaced.Add(new ReplacedCell(i, names[j], fills[j]));
                    }
                }
            }

            return Summarise(table, names, fills, indicators, replaced, data);
        }

        private static ImputationResult Summarise(
            double[][] table,
            string[] names,
            double[] fills,
            string[] indicators,
            List<ReplacedCell> replaced,
            double[][]? original = null)
        {
            var resultNames = names.ToList();
            var output = table;

            if (original != null && indicators.Length > 0)
            {
                var sources = indicators.Select(n => Array.IndexOf(names, n)).ToArray();

                output = table.Select((row, i) => row
                    .Concat(sources.Select(s => DataPreparationGuard.IsMissing(original[i][s]) ? 1d : 0d))
                    .ToArray()).ToArray();

                resultNames.AddRange(indicators.Select(n => n + "_missing"));
            }

            var perColumn = names.Select(n => replaced.Count(r => r.Column == n)).ToArray();

            return new ImputationResult(output, resultNames.ToArray(), fills, perColumn, replaced.Count,
                replaced.ToArray(), original != null ? indicators : Array.Empty<string>());
        }
    }

    public sealed record ColumnMissingness(string Name, int MissingCount, double MissingFraction);

    public sealed record MissingPattern(string[] MissingColumns, int Rows);

    public sealed record MissingValueProfile(
        int Rows,
        int Columns,
        int TotalMissing,
        double MissingFraction,
        int CompleteRows,
        ColumnMissingness[] ColumnProfiles,
        MissingPattern[] Patterns,
        string[] Warnings);

    public sealed record ReplacedCell(int Row, string Column, double Value);

    public sealed record ImputationResult(
        double[][] Table,
        string[] ColumnNames,
        double[] FillValues,
        int[] ReplacedPerColumn,
        int TotalReplaced,
        ReplacedCell[] ReplacedCells,
        string[] IndicatorSourceColumns);

    public sealed record SparseDropResult(
        double[][] Table,
        string[] KeptColumns,
        string[] DroppedColumns,
        int[] KeptRows,
        int[] DroppedRows);
}
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
using System.Globalization;
using Accord.Math;
using Accord.Statistics;
using Jube.Service.Agent.AdvancedAnalyticsTools.DescriptiveStatistics.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DescriptiveStatistics
{
    [AdvancedAnalyticsToolGroup("DescriptiveStatistics")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class TableAndTransformTools
    {
        private const string IgnoreMissingText =
            "True drops missing (NaN) values before computing and reports how many were dropped; false (default) rejects input containing NaN.";

        private const string RowsText =
            "The table: one array of numbers per row (case), all the same length, in the same column order as columnNames. NaN marks a missing value.";

        private const string ColumnNamesText =
            "The name of each column, unique and in column order. Leave empty to get automatic names column0, column1, and so on.";

        private const string DropIncompleteText =
            "True drops any row that has a missing value in any column before computing (listwise deletion) and reports how many; false (default) rejects a table with missing values.";

        private const string TransformMissingText =
            "Missing (NaN) entries stay NaN in the output and are excluded from any statistic used.";

        private const double NormalConsistencyConstant = 1.482602218505602;

        [AdvancedAnalyticsTool("SummariseColumns")]
        [Description(
            "One summary row per column of a table: count, missing, distinct, sum, mean, median, standard deviation, variance, standard error, coefficient of variation, min, max, range, quartiles, IQR, MAD, skewness, excess kurtosis, geometric mean, mode and zeros. " +
            "Use it as the first pass over a dataset. Missing values are excluded per column and counted in Missing; statistics that cannot be computed (for example standard deviation with 1 value, skewness with fewer than 3) are NaN.")]
        public DescriptiveTableSummaryResult SummariseColumns(
            [Description(RowsText)] double[][] rows,
            [Description(ColumnNamesText)] string[]? columnNames = null)
        {
            var names = DescriptiveInput.ValidateTable(rows, columnNames, out var columns);

            return new DescriptiveTableSummaryResult
            {
                Columns = Enumerable.Range(0, columns)
                    .Select(c => SummariseColumn(names[c], DescriptiveInput.Column(rows, c))).ToArray(),
                RowCount = rows.Length,
                ColumnCount = columns
            };
        }

        [AdvancedAnalyticsTool("DescribeColumn")]
        [Description(
            "A rich one-call profile of a single numeric variable: the full summary (count, missing, mean, median, spread, quartiles, skewness, kurtosis), percentiles 1 to 99, harmonic and trimmed means, trimean, outlier counts by z-score, IQR fence and MAD rule, the Gini concentration if the values are non-negative, the most common values and a plain-language reading of the shape. " +
            "Use it when you meet an unfamiliar variable. Missing values are excluded and counted; NaN fields mean the statistic is undefined for the data supplied.")]
        public DescriptiveColumnDescription DescribeColumn(
            [Description("The numbers, which may include NaN for missing.")]
            double[] values,
            [Description("A name for the variable, echoed back in the summary. Default value.")]
            string name = "value")
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("values is empty. Supply an array of numbers.", "values");
            }

            var v = DescriptiveInput.NonMissing(values);

            if (v.Length == 0)
            {
                throw new ArgumentException("values has no usable values. Supply at least one non-missing number.",
                    "values");
            }

            var summary = SummariseColumn(name, values);
            var levels = new[] { 0.01, 0.05, 0.1, 0.25, 0.5, 0.75, 0.9, 0.95, 0.99 };

            var percentiles =
                v.Quantiles(levels, false, DescriptiveInput.Map(DescriptiveQuantileInterpolation.Linear));

            var fenceLow = summary.LowerQuartile - 1.5 * summary.InterquartileRange;
            var fenceHigh = summary.UpperQuartile + 1.5 * summary.InterquartileRange;
            var positive = v.All(x => x > 0 && !double.IsInfinity(x));
            var nonNegative = v.All(x => x >= 0 && !double.IsInfinity(x)) && v.Sum() > 0;

            var zOutliers = double.IsNaN(summary.StandardDeviation) || summary.StandardDeviation == 0
                ? 0
                : v.Count(x => Math.Abs((x - summary.Mean) / summary.StandardDeviation) > 3);

            var madScale = ModifiedScale(v, summary.Median);
            var madOutliers = madScale == 0 ? 0 : v.Count(x => Math.Abs((x - summary.Median) / madScale) > 3.5);
            var reciprocals = positive ? v.Select(x => 1.0 / x).ToArray() : null;

            var counts = v.GroupBy(x => x).Select(g => (Value: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count).ThenBy(g => g.Value).Take(5).ToArray();

            return new DescriptiveColumnDescription
            {
                Summary = summary,
                PercentileLevels = levels.Select(l => l * 100).ToArray(),
                Percentiles = percentiles,
                HarmonicMean = reciprocals != null ? 1.0 / reciprocals.Mean() : double.NaN,
                TrimmedMean = v.TruncatedMean(0.1),
                MeanAbsoluteDeviation = v.Select(x => Math.Abs(x - summary.Mean)).Average(),
                ExcessKurtosis = summary.ExcessKurtosis,
                Kurtosis = v.Length >= 1 ? v.Kurtosis(false) + 3.0 : double.NaN,
                Trimean = (summary.LowerQuartile + 2 * summary.Median + summary.UpperQuartile) / 4.0,
                ZScoreOutlierCount = zOutliers,
                IqrOutlierCount = v.Count(x => x < fenceLow || x > fenceHigh),
                MadOutlierCount = madOutliers,
                LowerInnerFence = fenceLow,
                UpperInnerFence = fenceHigh,
                Gini = nonNegative ? Gini(v) : double.NaN,
                TopValues = counts.Select(g => new DescriptiveFrequencyRow
                {
                    Value = g.Value.ToString("R", CultureInfo.InvariantCulture), Count = g.Count,
                    Proportion = (double)g.Count / v.Length, Percent = 100.0 * g.Count / v.Length
                }).ToArray(),
                ShapeReading = ReadShape(summary, values.Length)
            };
        }

        [AdvancedAnalyticsTool("DescribeTable")]
        [Description(
            "One-call overview of a whole numeric table: a summary row per column, a missing-value profile, the strongest correlations between columns, and lists of constant columns, skewed columns, columns with IQR-fence outliers and columns with missing values. " +
            "Use it to orient yourself on a new dataset before choosing analyses. Correlations use rows complete in the pair of columns; only pairs with an absolute correlation of at least 0.5 are listed, up to 10.")]
        public DescriptiveTableDescription DescribeTable(
            [Description(RowsText)] double[][] rows,
            [Description(ColumnNamesText)] string[]? columnNames = null)
        {
            var names = DescriptiveInput.ValidateTable(rows, columnNames, out var columns);
            var summary = SummariseColumns(rows, names);
            var pairs = new List<DescriptiveCorrelationPair>();

            for (var i = 0; i < columns; i++)
            {
                for (var j = i + 1; j < columns; j++)
                {
                    var pair = PairwiseCorrelation(rows, names, i, j);

                    if (pair != null && Math.Abs(pair.Correlation) >= 0.5)
                    {
                        pairs.Add(pair);
                    }
                }
            }

            return new DescriptiveTableDescription
            {
                Summary = summary,
                Missing = TableMissingnessProfile(rows, names),
                StrongestCorrelations = pairs.OrderByDescending(p => Math.Abs(p.Correlation)).Take(10).ToArray(),
                ConstantColumns = summary.Columns.Where(c => c.Count > 0 && c.Minimum == c.Maximum).Select(c => c.Name)
                    .ToArray(),
                SkewedColumns = summary.Columns.Where(c => Math.Abs(c.Skewness) > 1).Select(c => c.Name).ToArray(),
                ColumnsWithOutliers = summary.Columns.Where(c => c.Count > 0 && CountOutsideFences(rows, names, c) > 0)
                    .Select(c => c.Name).ToArray(),
                ColumnsWithMissingValues = summary.Columns.Where(c => c.Missing > 0).Select(c => c.Name).ToArray()
            };
        }

        [AdvancedAnalyticsTool("TableMissingnessProfile")]
        [Description(
            "How much of a table is missing: per column the present and missing counts, missing fraction and count of infinite values, plus how many rows are complete, how many rows have any gap, the distribution of missing cells per row and the first few incomplete row indices. " +
            "Use it before modelling to see whether gaps are concentrated in a few columns or spread across rows. MissingCountPerRowDistribution[k] is the number of rows with exactly k missing cells.")]
        public DescriptiveMissingProfileResult TableMissingnessProfile(
            [Description(RowsText)] double[][] rows,
            [Description(ColumnNamesText)] string[]? columnNames = null)
        {
            var names = DescriptiveInput.ValidateTable(rows, columnNames, out var columns);
            var perColumn = new int[columns];
            var infinite = new int[columns];
            var distribution = new int[columns + 1];
            var incomplete = new List<int>();

            for (var i = 0; i < rows.Length; i++)
            {
                var missingInRow = 0;

                for (var c = 0; c < columns; c++)
                {
                    if (double.IsNaN(rows[i][c]))
                    {
                        perColumn[c]++;
                        missingInRow++;
                    }
                    else if (double.IsInfinity(rows[i][c]))
                    {
                        infinite[c]++;
                    }
                }

                distribution[missingInRow]++;

                if (missingInRow > 0 && incomplete.Count < 20)
                {
                    incomplete.Add(i);
                }
            }

            return new DescriptiveMissingProfileResult
            {
                Columns = Enumerable.Range(0, columns).Select(c => new DescriptiveColumnMissingness
                {
                    Name = names[c], Present = rows.Length - perColumn[c], Missing = perColumn[c],
                    MissingFraction = (double)perColumn[c] / rows.Length, Infinite = infinite[c]
                }).ToArray(),
                RowCount = rows.Length,
                CompleteRows = distribution[0],
                RowsWithAnyMissing = rows.Length - distribution[0],
                MissingCountPerRowDistribution = distribution,
                FirstIncompleteRowIndices = incomplete.ToArray()
            };
        }

        [AdvancedAnalyticsTool("ColumnCovarianceMatrix")]
        [Description(
            "Sample covariance matrix of the table's columns: entry [i][j] is how column i and column j vary together in the product of their units, and the diagonal is each column's variance. " +
            "Use it as a descriptive summary of joint variability; for a unit-free view use ColumnCorrelationMatrix. Needs at least 2 rows. Rows with missing values are rejected unless dropIncompleteRows is true.")]
        public DescriptiveMatrixResult ColumnCovarianceMatrix(
            [Description(RowsText)] double[][] rows,
            [Description(ColumnNamesText)] string[]? columnNames = null,
            [Description(DropIncompleteText)] bool dropIncompleteRows = false)
        {
            var names = DescriptiveInput.ValidateTable(rows, columnNames, out _);
            var complete = CompleteRows(rows, dropIncompleteRows, 2, out var dropped);

            return new DescriptiveMatrixResult
            {
                Names = names, Values = complete.Covariance(), RowsUsed = complete.Length, RowsDropped = dropped
            };
        }

        [AdvancedAnalyticsTool("ColumnCorrelationMatrix")]
        [Description(
            "Pearson correlation matrix of the table's columns: each entry is between -1 and 1, the diagonal is 1, near +1 or -1 means two columns move together (or oppositely) in a linear fashion. " +
            "A constant column has undefined correlation and gives NaN in its row and column. Needs at least 2 rows. Rows with missing values are rejected unless dropIncompleteRows is true.")]
        public DescriptiveMatrixResult ColumnCorrelationMatrix(
            [Description(RowsText)] double[][] rows,
            [Description(ColumnNamesText)] string[]? columnNames = null,
            [Description(DropIncompleteText)] bool dropIncompleteRows = false)
        {
            var names = DescriptiveInput.ValidateTable(rows, columnNames, out var columns);
            var complete = CompleteRows(rows, dropIncompleteRows, 2, out var dropped);
            var result = complete.Correlation();

            for (var i = 0; i < columns; i++)
            {
                var column = DescriptiveInput.Column(complete, i);
                var constant = column.Min() == column.Max();

                for (var j = 0; j < columns; j++)
                {
                    var other = DescriptiveInput.Column(complete, j);

                    if (constant || other.Min() == other.Max())
                    {
                        result[i][j] = double.NaN;
                    }
                }

                if (!constant)
                {
                    result[i][i] = 1.0;
                }
            }

            return new DescriptiveMatrixResult
                { Names = names, Values = result, RowsUsed = complete.Length, RowsDropped = dropped };
        }

        [AdvancedAnalyticsTool("StrongestColumnCorrelations")]
        [Description(
            "Ranks pairs of columns by the strength of their Pearson correlation and returns the strongest ones with the correlation and covariance, so you can find redundant or related variables without scanning a full matrix. " +
            "Each pair uses every row where both columns are present (pairwise deletion). Only pairs with an absolute correlation of at least minimumAbsoluteCorrelation are returned, strongest first, up to maxPairs.")]
        public DescriptiveCorrelationPairsResult StrongestColumnCorrelations(
            [Description(RowsText)] double[][] rows,
            [Description(ColumnNamesText)] string[]? columnNames = null,
            [Description("Smallest absolute correlation to report, from 0 to 1. Default 0.3.")]
            double minimumAbsoluteCorrelation = 0.3,
            [Description("Maximum number of pairs to return, at least 1. Default 20.")]
            int maxPairs = 20)
        {
            if (double.IsNaN(minimumAbsoluteCorrelation) || minimumAbsoluteCorrelation < 0 ||
                minimumAbsoluteCorrelation > 1)
            {
                throw new ArgumentException("minimumAbsoluteCorrelation must be between 0 and 1.",
                    "minimumAbsoluteCorrelation");
            }

            if (maxPairs < 1)
            {
                throw new ArgumentException("maxPairs must be at least 1.", "maxPairs");
            }

            var names = DescriptiveInput.ValidateTable(rows, columnNames, out var columns);
            var pairs = new List<DescriptiveCorrelationPair>();

            for (var i = 0; i < columns; i++)
            {
                for (var j = i + 1; j < columns; j++)
                {
                    var pair = PairwiseCorrelation(rows, names, i, j);

                    if (pair != null && Math.Abs(pair.Correlation) >= minimumAbsoluteCorrelation)
                    {
                        pairs.Add(pair);
                    }
                }
            }

            return new DescriptiveCorrelationPairsResult
            {
                Pairs = pairs.OrderByDescending(p => Math.Abs(p.Correlation)).Take(maxPairs).ToArray(),
                RowsUsed = rows.Length,
                RowsDropped = 0
            };
        }

        [AdvancedAnalyticsTool("QuantilesByColumn")]
        [Description(
            "The same set of quantiles for every column of a table, for example the 5th, 50th and 95th percentile of each variable. Use it to compare distributions across columns in one call. " +
            "Values[c][p] is the quantile of column c at Probabilities[p]. Missing values are excluded per column and Counts reports how many values each column contributed; a column with no values gives NaN.")]
        public DescriptiveColumnQuantilesResult QuantilesByColumn(
            [Description(RowsText)] double[][] rows,
            [Description("Cumulative probabilities between 0 and 1, for example 0.05, 0.5, 0.95.")]
            double[] probabilities,
            [Description(ColumnNamesText)] string[]? columnNames = null,
            [Description("Quantile interpolation rule. Linear (default) matches common spreadsheets.")]
            DescriptiveQuantileInterpolation interpolation = DescriptiveQuantileInterpolation.Linear)
        {
            if (probabilities == null || probabilities.Length == 0)
            {
                throw new ArgumentException(
                    "probabilities is empty. Supply values between 0 and 1 such as 0.25, 0.5, 0.75.", "probabilities");
            }

            foreach (var p in probabilities)
            {
                DescriptiveInput.RequireProbability(p, "probabilities");
            }

            var names = DescriptiveInput.ValidateTable(rows, columnNames, out var columns);
            var values = new double[columns][];
            var counts = new int[columns];

            for (var c = 0; c < columns; c++)
            {
                var present = DescriptiveInput.NonMissing(DescriptiveInput.Column(rows, c));
                counts[c] = present.Length;

                values[c] = present.Length == 0
                    ? Enumerable.Repeat(double.NaN, probabilities.Length).ToArray()
                    : present.Quantiles(probabilities, false, DescriptiveInput.Map(interpolation));
            }

            return new DescriptiveColumnQuantilesResult
                { ColumnNames = names, Probabilities = probabilities, Values = values, Counts = counts };
        }

        [AdvancedAnalyticsTool("SummariseRows")]
        [Description(
            "Row-wise summary across the columns of a table: for each row the count of present values, sum, mean, minimum, maximum and standard deviation, ignoring missing cells. Use it for row-level aggregates such as total spend across product columns or the spread of scores per case. " +
            "Names lists the output columns and Rows holds one output row per input row; standard deviation is NaN when a row has fewer than 2 values.")]
        public DescriptiveTableResult SummariseRows(
            [Description(RowsText)] double[][] rows)
        {
            DescriptiveInput.ValidateTable(rows, null, out _);
            var output = new double[rows.Length][];

            for (var i = 0; i < rows.Length; i++)
            {
                var present = DescriptiveInput.NonMissing(rows[i]);

                output[i] = present.Length == 0
                    ? new[] { 0, 0, double.NaN, double.NaN, double.NaN, double.NaN }
                    : new[]
                    {
                        present.Length, present.Sum(), present.Mean(), present.Min(), present.Max(),
                        present.Length >= 2 ? present.StandardDeviation() : double.NaN
                    };
            }

            return new DescriptiveTableResult
                { Names = new[] { "Count", "Sum", "Mean", "Minimum", "Maximum", "StandardDeviation" }, Rows = output };
        }

        [AdvancedAnalyticsTool("SummariseByGroup")]
        [Description(
            "Descriptive statistics of one numeric variable split by a category: for each group the count, mean, median, standard deviation, minimum, maximum and sum, plus the pooled standard deviation and the grand mean. Use it to compare, for example, transaction amounts by channel or country. " +
            "groups must have one label per value; null labels form a group named (missing); NaN values are excluded and counted in MissingIgnored. Groups are ordered by label. Standard deviation is NaN for a group of one.")]
        public DescriptiveGroupedSummaryResult SummariseByGroup(
            [Description("The numeric values, one per observation.")]
            double[] values,
            [Description("The group label for each value, in the same order.")]
            string?[] groups,
            [Description(IgnoreMissingText)] bool ignoreMissing = true)
        {
            if (values == null || groups == null || values.Length != groups.Length)
            {
                throw new ArgumentException(
                    "values and groups must both be supplied and have the same length, one group label per value.",
                    "groups");
            }

            var missing = values.Count(double.IsNaN);

            if (missing > 0 && !ignoreMissing)
            {
                throw new ArgumentException(
                    "values contains missing (NaN) entries. Set ignoreMissing to true to exclude them.", "values");
            }

            var rows = new List<DescriptiveGroupedSummaryRow>();

            var pairs = values.Select((x, i) => (Value: x, Group: groups[i] ?? "(missing)"))
                .Where(p => !double.IsNaN(p.Value)).ToArray();

            if (pairs.Length == 0)
            {
                throw new ArgumentException("values has no usable values. Supply at least one non-missing number.",
                    "values");
            }

            foreach (var g in pairs.GroupBy(p => p.Group, StringComparer.Ordinal)
                         .OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                var x = g.Select(p => p.Value).ToArray();

                rows.Add(new DescriptiveGroupedSummaryRow
                {
                    Group = g.Key, Count = x.Length, Mean = x.Mean(), Median = x.Median(),
                    StandardDeviation = x.Length >= 2 ? x.StandardDeviation() : double.NaN,
                    Minimum = x.Min(), Maximum = x.Max(), Sum = x.Sum()
                });
            }

            var eligible = rows.All(r => r.Count >= 2) && rows.Count >= 2;

            return new DescriptiveGroupedSummaryResult
            {
                Groups = rows.ToArray(),
                PooledStandardDeviation = eligible
                    ? Measures.PooledStandardDeviation(true,
                        pairs.GroupBy(p => p.Group, StringComparer.Ordinal)
                            .Select(g => g.Select(p => p.Value).ToArray())
                            .ToArray())
                    : double.NaN,
                GrandMean = pairs.Select(p => p.Value).ToArray().Mean(),
                Count = pairs.Length,
                MissingIgnored = missing
            };
        }

        [AdvancedAnalyticsTool("SummariseCategories")]
        [Description(
            "Overview of a categorical variable: total, missing, distinct count, the mode(s) and their frequency, Shannon entropy in bits, normalised entropy (0 concentrated, 1 perfectly even), Gini impurity, Herfindahl index, the number of labels seen exactly once and the top 10 labels with counts. " +
            "Use it to profile identifiers and categories such as merchant, country or device: high distinct count with many singletons suggests near-unique values. Null labels are counted as missing and excluded.")]
        public DescriptiveCategoricalSummaryResult SummariseCategories(
            [Description("The category label for each observation; null counts as missing.")]
            string[] values)
        {
            var v = DescriptiveInput.CleanLabels(values, "values", true, out var missing);

            var counts = v.GroupBy(x => x, StringComparer.Ordinal).Select(g => (g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count).ThenBy(g => g.Key, StringComparer.Ordinal).ToArray();

            var probabilities = counts.Select(c => (double)c.Count / v.Length).ToArray();
            var nats = Math.Abs(probabilities.Entropy());
            var best = counts[0].Count;
            var herfindahl = probabilities.Sum(p => p * p);
            var running = 0;

            return new DescriptiveCategoricalSummaryResult
            {
                Total = v.Length,
                Missing = missing,
                Distinct = counts.Length,
                Modes = counts.Where(c => c.Count == best).Select(c => c.Key).ToArray(),
                ModeFrequency = best,
                ShannonEntropyBits = nats / Math.Log(2),
                NormalisedEntropy = counts.Length > 1 ? nats / Math.Log(counts.Length) : double.NaN,
                GiniImpurity = 1.0 - herfindahl,
                HerfindahlIndex = herfindahl,
                SingletonCount = counts.Count(c => c.Count == 1),
                TopValues = counts.Take(10).Select(c =>
                {
                    running += c.Count;

                    return new DescriptiveFrequencyRow
                    {
                        Value = c.Key, Count = c.Count, Proportion = (double)c.Count / v.Length,
                        Percent = 100.0 * c.Count / v.Length, CumulativeCount = running,
                        CumulativeProportion = (double)running / v.Length
                    };
                }).ToArray()
            };
        }

        [AdvancedAnalyticsTool("TopShareConcentration")]
        [Description(
            "Pareto-style concentration of non-negative amounts: the share of the total held by the top 1 percent, 10 percent or any fractions of holders you choose, and the number of holders needed to reach given shares of the total (for example how many customers make up 80 percent of value). " +
            "Use it for account and counterparty concentration. Fractions and share targets are between 0 and 1; values must be non-negative with a positive total.")]
        public DescriptiveConcentrationResult TopShareConcentration(
            [Description("Non-negative amounts, one per holder or account.")]
            double[] values,
            [Description(
                "Fractions of holders, the biggest first, whose share of the total is wanted. Default 0.01, 0.1, 0.2.")]
            double[]? topFractions = null,
            [Description("Shares of the total whose required number of holders is wanted. Default 0.5, 0.8, 0.9.")]
            double[]? shareTargets = null,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var fractions = topFractions == null || topFractions.Length == 0 ? new[] { 0.01, 0.1, 0.2 } : topFractions;
            var targets = shareTargets == null || shareTargets.Length == 0 ? new[] { 0.5, 0.8, 0.9 } : shareTargets;

            if (fractions.Any(f => double.IsNaN(f) || f <= 0 || f > 1))
            {
                throw new ArgumentException("topFractions must each be greater than 0 and at most 1.", "topFractions");
            }

            if (targets.Any(f => double.IsNaN(f) || f <= 0 || f > 1))
            {
                throw new ArgumentException("shareTargets must each be greater than 0 and at most 1.", "shareTargets");
            }

            var v = DescriptiveInput.Finite(values, "values", ignoreMissing, 1, out var missing);

            if (v.Any(x => x < 0))
            {
                throw new ArgumentException("values must not be negative for concentration measures.", "values");
            }

            var total = v.Sum();

            if (total <= 0)
            {
                throw new ArgumentException("values sum to zero, so shares are undefined.", "values");
            }

            var sorted = DescriptiveInput.Sorted(v);
            Array.Reverse(sorted);
            var cumulative = new double[sorted.Length];
            var running = 0.0;

            for (var i = 0; i < sorted.Length; i++)
            {
                running += sorted[i];
                cumulative[i] = running;
            }

            var included = fractions.Select(f => Math.Max(1, (int)Math.Ceiling(f * sorted.Length - 1e-9))).ToArray();

            var needed = targets.Select(t =>
            {
                var index = Array.FindIndex(cumulative, c => c >= t * total - 1e-9 * total);

                return index < 0 ? sorted.Length : index + 1;
            }).ToArray();

            return new DescriptiveConcentrationResult
            {
                Fractions = fractions,
                HoldersIncluded = included,
                Shares = included.Select(k => cumulative[k - 1] / total).ToArray(),
                ShareTargets = targets,
                HoldersNeeded = needed,
                HolderFractionNeeded = needed.Select(k => (double)k / sorted.Length).ToArray(),
                Total = total,
                Count = sorted.Length,
                MissingIgnored = missing
            };
        }

        [AdvancedAnalyticsTool("ZScores")]
        [Description(
            "Standardises each value to a z-score: (value - mean) / standard deviation, so 0 is average and 2 means two standard deviations above. Use it to put variables on a common scale or to spot unusual values. " +
            "Returns the transformed series plus the Centre (mean) and Scale (standard deviation) used. A constant series has no spread and is rejected." +
            TransformMissingText)]
        public DescriptiveTransformResult ZScores(
            [Description("The numbers; NaN stay NaN.")]
            double[] values,
            [Description("True (default) uses the sample standard deviation (n-1); false the population one (n).")]
            bool unbiased = true)
        {
            var v = Present(values, unbiased ? 2 : 1);
            var mean = v.Mean();
            var sd = v.StandardDeviation(mean, unbiased);
            RequireSpread(sd, nameof(values));

            return Transformed(values, x => (x - mean) / sd, mean, sd, v.Length);
        }

        [AdvancedAnalyticsTool("RobustZScores")]
        [Description(
            "Robust standardisation: (value - median) / scale, where scale is 1.4826 x MAD (the default), or the interquartile range divided by 1.349, so extreme values do not distort the centre and spread as they do for ordinary z-scores. Values beyond about 3 to 3.5 are usually unusual. " +
            "Returns the series plus the Centre (median) and Scale used. A series whose scale is 0 is rejected." +
            TransformMissingText)]
        public DescriptiveTransformResult RobustZScores(
            [Description("The numbers; NaN stay NaN.")]
            double[] values,
            [Description(
                "Spread estimate: MedianAbsoluteDeviation (default) or InterquartileRange. StandardDeviation is also accepted and centres on the mean.")]
            DescriptiveStandardisationScale scale = DescriptiveStandardisationScale.MedianAbsoluteDeviation)
        {
            var v = Present(values, 1);
            double centre;
            double spread;

            switch (scale)
            {
                case DescriptiveStandardisationScale.MedianAbsoluteDeviation:
                    centre = v.Median();
                    spread = SpreadAndShapeTools.Mad(v) * NormalConsistencyConstant;
                    break;
                case DescriptiveStandardisationScale.InterquartileRange:
                    var q = v.Quantiles(new[] { 0.25, 0.5, 0.75 }, false,
                        DescriptiveInput.Map(DescriptiveQuantileInterpolation.Linear));

                    centre = q[1];
                    spread = (q[2] - q[0]) / 1.3489795003921634;
                    break;
                case DescriptiveStandardisationScale.StandardDeviation:
                    centre = v.Mean();
                    spread = v.Length >= 2 ? v.StandardDeviation(centre) : 0;
                    break;
                default:
                    throw new ArgumentException(
                        "Unknown scale. Use MedianAbsoluteDeviation, InterquartileRange or StandardDeviation.",
                        "scale");
            }

            RequireSpread(spread, nameof(values));

            return Transformed(values, x => (x - centre) / spread, centre, spread, v.Length);
        }

        [AdvancedAnalyticsTool("CentreValues")]
        [Description(
            "Subtracts the mean (or median) from every value so the result is centred on zero, without rescaling. Use it to work with deviations from typical behaviour. " +
            "Returns the centred series and the Centre subtracted." + TransformMissingText)]
        public DescriptiveTransformResult CentreValues(
            [Description("The numbers; NaN stay NaN.")]
            double[] values,
            [Description("Centre to subtract: Mean (default) or Median.")]
            DescriptiveCentre centre = DescriptiveCentre.Mean)
        {
            var v = Present(values, 1);
            var c = centre == DescriptiveCentre.Mean ? v.Mean() : v.Median();

            return Transformed(values, x => x - c, c, 1.0, v.Length);
        }

        [AdvancedAnalyticsTool("TransformValues")]
        [Description(
            "Applies a common element-wise transform to every value: natural log, log base 10 or 2, log(1+x), square root, cube root, square, reciprocal, absolute value, sign, negation or signed log(1+|x|). Use log or square root to tame right-skewed amounts before analysis. " +
            "Domain errors are rejected with the index of the first offending value (log needs positive values, log1p at least -1 + tiny, square root non-negative, reciprocal non-zero)." +
            TransformMissingText)]
        public DescriptiveSeriesResult TransformValues(
            [Description("The numbers; NaN stay NaN.")]
            double[] values,
            [Description("The transform to apply.")]
            DescriptiveValueTransform transform)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("values is empty. Supply an array of numbers.", "values");
            }

            var output = new double[values.Length];

            for (var i = 0; i < values.Length; i++)
            {
                var x = values[i];

                if (double.IsNaN(x))
                {
                    output[i] = double.NaN;
                    continue;
                }

                var valid = transform switch
                {
                    DescriptiveValueTransform.NaturalLog or DescriptiveValueTransform.Log10
                        or DescriptiveValueTransform.Log2 => x > 0,
                    DescriptiveValueTransform.Log1P => x > -1,
                    DescriptiveValueTransform.SquareRoot => x >= 0,
                    DescriptiveValueTransform.Reciprocal => x != 0,
                    _ => true
                };

                if (!valid)
                {
                    throw new ArgumentException(
                        $"values[{i}] = {x.ToString(CultureInfo.InvariantCulture)} is outside the domain of {transform}. Filter or shift the data first.",
                        "values");
                }

                output[i] = transform switch
                {
                    DescriptiveValueTransform.NaturalLog => Math.Log(x),
                    DescriptiveValueTransform.Log10 => Math.Log10(x),
                    DescriptiveValueTransform.Log2 => Math.Log2(x),
                    DescriptiveValueTransform.Log1P => Math.Log(1 + x),
                    DescriptiveValueTransform.SquareRoot => Math.Sqrt(x),
                    DescriptiveValueTransform.CubeRoot => Math.Cbrt(x),
                    DescriptiveValueTransform.Square => x * x,
                    DescriptiveValueTransform.Reciprocal => 1.0 / x,
                    DescriptiveValueTransform.Absolute => Math.Abs(x),
                    DescriptiveValueTransform.Sign => Math.Sign(x),
                    DescriptiveValueTransform.Negate => -x,
                    DescriptiveValueTransform.SignedLog1P => Math.Sign(x) * Math.Log(1 + Math.Abs(x)),
                    _ => throw new ArgumentException("Unknown transform.", "transform")
                };
            }

            var missing = values.Count(double.IsNaN);

            return new DescriptiveSeriesResult
                { Values = output, Count = values.Length - missing, MissingCount = missing };
        }

        [AdvancedAnalyticsTool("ProportionsOfTotal")]
        [Description(
            "Converts amounts into shares of their total: each value divided by the sum, so the output sums to 1. Use it to see how a total splits across accounts, products or periods. " +
            "The total must not be zero. Returns the shares plus the Scale (the total) used." + TransformMissingText)]
        public DescriptiveTransformResult ProportionsOfTotal(
            [Description("The amounts; NaN stay NaN and are left out of the total.")]
            double[] values)
        {
            var v = Present(values, 1);
            var total = v.Sum();

            if (total == 0)
            {
                throw new ArgumentException("The values sum to zero, so shares are undefined.", "values");
            }

            return Transformed(values, x => x / total, 0, total, v.Length);
        }

        [AdvancedAnalyticsTool("WinsoriseValues")]
        [Description(
            "Caps extreme values at chosen percentiles: anything below the lower percentile is set to it and anything above the upper percentile is set to it, keeping every observation. Use it to limit outlier influence before computing means or fitting. " +
            "Percent levels are between 0 and 100 with lower below upper. Returns the capped series; Centre and Scale hold the lower and upper cap values." +
            TransformMissingText)]
        public DescriptiveTransformResult WinsoriseValues(
            [Description("The numbers; NaN stay NaN.")]
            double[] values,
            [Description("Lower percentile cap, 0 to 100. Default 1.")]
            double lowerPercent = 1,
            [Description("Upper percentile cap, 0 to 100 and above the lower. Default 99.")]
            double upperPercent = 99)
        {
            if (double.IsNaN(lowerPercent) || double.IsNaN(upperPercent) || lowerPercent < 0 || upperPercent > 100 ||
                lowerPercent >= upperPercent)
            {
                throw new ArgumentException(
                    "lowerPercent and upperPercent must satisfy 0 <= lowerPercent < upperPercent <= 100.",
                    "upperPercent");
            }

            var v = Present(values, 1);

            var q = v.Quantiles(new[] { lowerPercent / 100.0, upperPercent / 100.0 }, false,
                DescriptiveInput.Map(DescriptiveQuantileInterpolation.Linear));

            return Transformed(values, x => Math.Min(Math.Max(x, q[0]), q[1]), q[0], q[1], v.Length);
        }

        [AdvancedAnalyticsTool("FlagOutliersByZScore")]
        [Description(
            "Flags values whose z-score, (value - mean) / standard deviation, is beyond a threshold in either direction (default 3). Simple and widely understood, but the mean and standard deviation are themselves distorted by outliers, so on small or heavy-tailed data prefer FlagOutliersByMedianAbsoluteDeviation. " +
            "Returns IsOutlier per value in original order, the outlier indices and values, each value's z-score in Scores, and the LowerBound and UpperBound value limits. NaN are never flagged and are counted in MissingCount.")]
        public DescriptiveOutlierResult FlagOutliersByZScore(
            [Description("The numbers in original order; NaN are skipped.")]
            double[] values,
            [Description("Absolute z-score above which a value is an outlier; greater than 0. Default 3.")]
            double threshold = 3.0)
        {
            RequirePositive(threshold, "threshold");
            var v = Present(values, 2);
            var mean = v.Mean();
            var sd = v.StandardDeviation(mean);
            var scores = values.Select(x => double.IsNaN(x) ? double.NaN : sd == 0 ? 0 : (x - mean) / sd).ToArray();

            return Outliers("ZScore", values, scores, x => Math.Abs(x) > threshold, mean - threshold * sd,
                mean + threshold * sd, mean, sd);
        }

        [AdvancedAnalyticsTool("FlagOutliersByIqrFence")]
        [Description(
            "Flags values outside Tukey's fences: below Q1 - k x IQR or above Q3 + k x IQR, with k = 1.5 for ordinary outliers or 3 for extreme ones (the box-plot rule). It relies on quartiles, so it is robust and does not assume normality, but it can flag many points on strongly skewed data. " +
            "Returns IsOutlier per value, the outlier indices and values, Scores (the signed number of IQRs a value lies beyond the nearest quartile, 0 inside the box) and the LowerBound and UpperBound fences. NaN are never flagged.")]
        public DescriptiveOutlierResult FlagOutliersByIqrFence(
            [Description("The numbers in original order; NaN are skipped.")]
            double[] values,
            [Description("Fence multiplier k; greater than 0. Default 1.5.")]
            double fenceMultiplier = 1.5,
            [Description("Quantile interpolation rule used for the quartiles. Default Linear.")]
            DescriptiveQuantileInterpolation interpolation = DescriptiveQuantileInterpolation.Linear)
        {
            RequirePositive(fenceMultiplier, "fenceMultiplier");
            var v = Present(values, 1);
            var q = v.Quantiles(new[] { 0.25, 0.75 }, false, DescriptiveInput.Map(interpolation));
            var iqr = q[1] - q[0];
            var low = q[0] - fenceMultiplier * iqr;
            var high = q[1] + fenceMultiplier * iqr;

            var scores = values.Select(x => double.IsNaN(x)
                ? double.NaN
                : iqr == 0
                    ? 0
                    : x < q[0]
                        ? (x - q[0]) / iqr
                        : x > q[1]
                            ? (x - q[1]) / iqr
                            : 0).ToArray();

            return Outliers("IqrFence", values, scores, null, low, high, (q[0] + q[1]) / 2.0, iqr,
                x => x < low || x > high);
        }

        [AdvancedAnalyticsTool("FlagOutliersByMedianAbsoluteDeviation")]
        [Description(
            "Flags values whose modified z-score, 0.6745 x (value - median) / MAD, exceeds a threshold (default 3.5, the Iglewicz-Hoaglin recommendation). Being built on the median and the median absolute deviation it stays reliable when up to about half the data is contaminated, so it is the preferred rule for messy amounts. " +
            "If more than half the values are identical (MAD is 0) the mean absolute deviation is used instead and Scale reports the spread actually used. Returns IsOutlier per value, indices, values, Scores (modified z) and the LowerBound and UpperBound limits.")]
        public DescriptiveOutlierResult FlagOutliersByMedianAbsoluteDeviation(
            [Description("The numbers in original order; NaN are skipped.")]
            double[] values,
            [Description("Absolute modified z-score above which a value is an outlier; greater than 0. Default 3.5.")]
            double threshold = 3.5)
        {
            RequirePositive(threshold, "threshold");
            var v = Present(values, 1);
            var median = v.Median();
            var scale = ModifiedScale(v, median);

            var scores = values.Select(x => double.IsNaN(x) ? double.NaN : scale == 0 ? 0 : (x - median) / scale)
                .ToArray();

            return Outliers("MedianAbsoluteDeviation", values, scores, x => Math.Abs(x) > threshold,
                median - threshold * scale, median + threshold * scale, median, scale);
        }

        private static double[] Present(double[]? values, int minimum)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("values is empty. Supply an array of numbers.", "values");
            }

            var v = DescriptiveInput.NonMissing(values);

            if (v.Length < minimum)
            {
                throw new ArgumentException(
                    $"values has {v.Length} usable value(s) but at least {minimum} are needed.", "values");
            }

            return v;
        }

        private static void RequireSpread(double spread, string parameterName)
        {
            if (double.IsNaN(spread) || spread <= 0 || double.IsInfinity(spread))
            {
                throw new ArgumentException(
                    "The values have no spread (they are constant, or the spread estimate is zero), so they cannot be standardised. Use CentreValues instead.",
                    parameterName);
            }
        }

        private static void RequirePositive(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                throw new ArgumentException($"{name} must be a finite number greater than 0.", name);
            }
        }

        private static DescriptiveTransformResult Transformed(double[] values, Func<double, double> map, double centre,
            double scale, int count)
        {
            return new DescriptiveTransformResult
            {
                Values = values.Select(x => double.IsNaN(x) ? double.NaN : map(x)).ToArray(),
                Centre = centre,
                Scale = scale,
                Count = count,
                MissingCount = values.Length - count
            };
        }

        private static DescriptiveOutlierResult Outliers(string method, double[] values, double[] scores,
            Func<double, bool>? scoreRule,
            double low, double high, double centre, double scale, Func<double, bool>? valueRule = null)
        {
            var flags = new bool[values.Length];

            for (var i = 0; i < values.Length; i++)
            {
                if (double.IsNaN(values[i]))
                {
                    continue;
                }

                flags[i] = valueRule != null ? valueRule(values[i]) : scoreRule!(scores[i]);
            }

            var indices = Enumerable.Range(0, values.Length).Where(i => flags[i]).ToArray();

            return new DescriptiveOutlierResult
            {
                Method = method, IsOutlier = flags, OutlierIndices = indices,
                OutlierValues = indices.Select(i => values[i]).ToArray(),
                Scores = scores, LowerBound = low, UpperBound = high, Centre = centre, Scale = scale,
                OutlierCount = indices.Length, Count = values.Count(x => !double.IsNaN(x)),
                MissingCount = values.Count(double.IsNaN)
            };
        }

        private static double ModifiedScale(double[] values, double median)
        {
            var mad = SpreadAndShapeTools.Mad(values);

            if (mad > 0)
            {
                return mad / 0.6745;
            }

            return values.Select(x => Math.Abs(x - median)).Average() * 1.2533141373155001;
        }

        private static double Gini(double[] values)
        {
            var sorted = DescriptiveInput.Sorted(values);
            var n = sorted.Length;
            var total = sorted.Sum();
            var weighted = 0.0;

            for (var i = 0; i < n; i++)
            {
                weighted += (2.0 * (i + 1) - n - 1) * sorted[i];
            }

            return weighted / (n * total);
        }

        private static double Correlation(double[] x, double[] y)
        {
            if (x.Min() == x.Max() || y.Min() == y.Max())
            {
                return double.NaN;
            }

            var joined = new double[x.Length][];

            for (var i = 0; i < x.Length; i++)
            {
                joined[i] = new[] { x[i], y[i] };
            }

            return joined.Correlation()[0][1];
        }

        private static DescriptiveCorrelationPair? PairwiseCorrelation(double[][] rows, string[] names, int i, int j)
        {
            var x = new List<double>();
            var y = new List<double>();

            foreach (var row in rows)
            {
                if (!double.IsNaN(row[i]) && !double.IsNaN(row[j]))
                {
                    x.Add(row[i]);
                    y.Add(row[j]);
                }
            }

            if (x.Count < 2)
            {
                return null;
            }

            var xs = x.ToArray();
            var ys = y.ToArray();
            var correlation = Correlation(xs, ys);

            if (double.IsNaN(correlation))
            {
                return null;
            }

            return new DescriptiveCorrelationPair
            {
                First = names[i], Second = names[j], Correlation = correlation,
                Covariance = xs.Covariance(ys)
            };
        }

        private static double[][] CompleteRows(double[][] rows, bool drop, int minimum, out int dropped)
        {
            var complete = new List<double[]>(rows.Length);

            for (var i = 0; i < rows.Length; i++)
            {
                if (rows[i].Any(double.IsNaN))
                {
                    if (!drop)
                    {
                        throw new ArgumentException(
                            $"rows contains a missing (NaN) value in row {i}. Set dropIncompleteRows to true to delete incomplete rows, or impute them first.",
                            "rows");
                    }
                }
                else
                {
                    complete.Add(rows[i]);
                }
            }

            dropped = rows.Length - complete.Count;

            if (complete.Count < minimum)
            {
                throw new ArgumentException(
                    $"Only {complete.Count} complete row(s) remain but at least {minimum} are needed.", "rows");
            }

            return complete.ToArray();
        }

        private static int CountOutsideFences(double[][] rows, string[] names, DescriptiveColumnSummary summary)
        {
            var index = Array.IndexOf(names, summary.Name);
            var low = summary.LowerQuartile - 1.5 * summary.InterquartileRange;
            var high = summary.UpperQuartile + 1.5 * summary.InterquartileRange;

            return rows.Count(r => !double.IsNaN(r[index]) && (r[index] < low || r[index] > high));
        }

        private static DescriptiveColumnSummary SummariseColumn(string name, double[] raw)
        {
            var v = DescriptiveInput.NonMissing(raw);
            var n = v.Length;

            if (n == 0)
            {
                return new DescriptiveColumnSummary
                {
                    Name = name, Count = 0, Missing = raw.Length, Sum = 0, Mean = double.NaN, Median = double.NaN,
                    StandardDeviation = double.NaN, Variance = double.NaN, StandardError = double.NaN,
                    CoefficientOfVariation = double.NaN, Minimum = double.NaN, Maximum = double.NaN, Range = double.NaN,
                    LowerQuartile = double.NaN, UpperQuartile = double.NaN, InterquartileRange = double.NaN,
                    MedianAbsoluteDeviation = double.NaN, Skewness = double.NaN, ExcessKurtosis = double.NaN,
                    GeometricMean = double.NaN, Mode = double.NaN
                };
            }

            var mean = v.Mean();

            var q = v.Quantiles(new[] { 0.25, 0.5, 0.75 }, false,
                DescriptiveInput.Map(DescriptiveQuantileInterpolation.Linear));

            var variance = n >= 2 ? v.Variance(mean, true) : double.NaN;
            var sd = Math.Sqrt(variance);
            var min = v.Min();
            var max = v.Max();

            return new DescriptiveColumnSummary
            {
                Name = name,
                Count = n,
                Missing = raw.Length - n,
                Distinct = v.Distinct().Count(),
                Sum = v.Sum(),
                Mean = mean,
                Median = q[1],
                StandardDeviation = sd,
                Variance = variance,
                StandardError = n >= 2 ? sd / Math.Sqrt(n) : double.NaN,
                CoefficientOfVariation = n >= 2 ? sd / Math.Abs(mean) : double.NaN,
                Minimum = min,
                Maximum = max,
                Range = max - min,
                LowerQuartile = q[0],
                UpperQuartile = q[2],
                InterquartileRange = q[2] - q[0],
                MedianAbsoluteDeviation = SpreadAndShapeTools.Mad(v) * NormalConsistencyConstant,
                Skewness = n >= 3 ? v.Skewness(mean) : double.NaN,
                ExcessKurtosis = n >= 4 ? v.Kurtosis(mean) : double.NaN,
                GeometricMean = v.All(x => x > 0 && !double.IsInfinity(x)) ? v.GeometricMean() : double.NaN,
                Mode = v.Mode(),
                Zeros = v.Count(x => x == 0)
            };
        }

        private static string ReadShape(DescriptiveColumnSummary s, int total)
        {
            var parts = new List<string>();

            if (s.Missing > 0)
            {
                parts.Add($"{s.Missing} of {total} values are missing ({100.0 * s.Missing / total:0.#} percent)");
            }

            if (s.Count > 0 && s.Minimum == s.Maximum)
            {
                parts.Add("all values are identical (constant)");

                return string.Join("; ", parts) + ".";
            }

            if (!double.IsNaN(s.Skewness))
            {
                var magnitude = Math.Abs(s.Skewness) > 1 ? "strongly" :
                    Math.Abs(s.Skewness) > 0.5 ? "moderately" : null;

                parts.Add(magnitude == null
                    ? $"roughly symmetric (skewness {s.Skewness:0.##})"
                    : $"{magnitude} {(s.Skewness > 0 ? "right" : "left")}-skewed (skewness {s.Skewness:0.##})");
            }

            if (!double.IsNaN(s.ExcessKurtosis))
            {
                parts.Add(s.ExcessKurtosis > 1
                    ? $"heavy-tailed (excess kurtosis {s.ExcessKurtosis:0.##})"
                    : s.ExcessKurtosis < -1
                        ? $"light-tailed (excess kurtosis {s.ExcessKurtosis:0.##})"
                        : $"tails near normal (excess kurtosis {s.ExcessKurtosis:0.##})");
            }

            if (s.Median != 0 && !double.IsNaN(s.Mean) && Math.Abs(s.Mean - s.Median) > 0.1 * Math.Abs(s.Median))
            {
                parts.Add($"mean ({s.Mean:0.###}) differs from median ({s.Median:0.###}) by more than 10 percent");
            }

            return parts.Count == 0 ? "Too few values to describe the shape." : string.Join("; ", parts) + ".";
        }
    }
}
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
using Accord.Statistics;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling
{
    [AdvancedAnalyticsToolGroup("DataPreparationAndResampling")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class DataQualityTools
    {
        [AdvancedAnalyticsTool("DescribeDatasetShape")]
        [Description(
            "Summarises the shape and column types of a numeric table: rows, columns, and for every column its inferred type (AllMissing, Constant, Binary for two distinct values, Integer for whole numbers with more than two values, Continuous otherwise), distinct count, missing count, minimum, maximum and mean of the finite values. " +
            "Use it as the first look at a table before choosing preparation steps. Missing means NaN or infinite. Statistics of an all-missing column are NaN.")]
        public DatasetShape DescribeDatasetShape(
            [Description(
                "Table rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));

            return new DatasetShape(data.Length, columns, Enumerable.Range(0, columns)
                .Select(j => Summarise(names[j], data, j)).ToArray(), (long)data.Length * columns * sizeof(double));
        }

        [AdvancedAnalyticsTool("DescribeCategoricalTable")]
        [Description(
            "Profiles a categorical (text) table: for each column the number of distinct values, missing count (null or empty), the most frequent value and its count, the share of rows the distinct values represent (cardinality ratio, near 1 means identifier-like) and the top values with counts. " +
            "Use it before choosing an encoder: low cardinality suits one-hot, high cardinality suits frequency or target encoding. Entropy is in nats and 0 means a single value.")]
        public CategoricalProfile[] DescribeCategoricalTable(
            [Description(
                "Categorical table rows: one array of text values per case, all the same length. Null or empty means missing.")]
            string?[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("How many of the most frequent values to list per column, at least 1. Default 5.")]
            int topValues = 5)
        {
            var columns = Categorical(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            DataPreparationGuard.Positive(topValues, nameof(topValues));

            return Enumerable.Range(0, columns).Select(j => ProfileCategorical(names[j], data, j, topValues)).ToArray();
        }

        [AdvancedAnalyticsTool("FindDuplicateRows")]
        [Description(
            "Finds rows of a numeric table that are exactly identical to an earlier row (two missing cells count as equal). " +
            "Returns the number of duplicate rows, the groups (first occurrence and its repeats) and the row indices to keep if you drop repeats. Duplicates often mean double-loaded feeds and inflate apparent sample size and cross validation scores.")]
        public DuplicateRowsResult FindDuplicateRows(
            [Description("Table rows: one array of numbers per case, all the same length. NaN counts as equal to NaN.")]
            double[][] data)
        {
            DataPreparationGuard.Table(data, nameof(data));

            return Duplicates(data.Select(r => string.Join("|",
                    r.Select(v =>
                        double.IsNaN(v) ? "NaN" : v.ToString("R", CultureInfo.InvariantCulture))))
                .ToArray());
        }

        [AdvancedAnalyticsTool("FindDuplicateCategoricalRows")]
        [Description(
            "Finds rows of a categorical (text) table that are exactly identical to an earlier row (null and empty are treated as the same). " +
            "Returns the number of duplicate rows, the groups (first occurrence and its repeats) and the row indices to keep if you drop repeats.")]
        public DuplicateRowsResult FindDuplicateCategoricalRows(
            [Description("Categorical table rows: one array of text values per case, all the same length.")]
            string?[][] data)
        {
            Categorical(data, nameof(data));

            return Duplicates(data.Select(r => string.Join("\u001f", r.Select(v => v ?? string.Empty))).ToArray());
        }

        [AdvancedAnalyticsTool("CheckTargetBalance")]
        [Description(
            "Checks how balanced the classes of a target are: the count and share of each class, the imbalance ratio (largest class over smallest), the entropy of the class distribution relative to perfectly balanced (1 balanced, near 0 dominated by one class) and the smallest class share. " +
            "Warnings say when a class is so rare that accuracy is misleading or that stratified splitting and class weights are needed. Use it before choosing a metric or a resampling method.")]
        public TargetBalanceResult CheckTargetBalance(
            [Description("The class label for each row (any integers, for example 0/1), in row order.")]
            int[] labels)
        {
            DataPreparationGuard.Labels(labels, nameof(labels));

            return Balance(labels);
        }

        [AdvancedAnalyticsTool("DataQualityReport")]
        [Description(
            "Runs a battery of quality checks on a numeric table and returns a list of issues, each with a severity (Error, Warning, Info), the column, a message and the tool to fix it. " +
            "Checks: entirely or mostly missing columns, non-finite values, constant and near-constant columns (one value in 99 percent or more of rows), identifier-like columns, exact duplicate rows, heavily skewed columns, outlier share by the 1.5 IQR fence, very different column scales, too few rows for the number of columns, and, with a target, class balance and leakage suspects (a feature almost perfectly correlated with, or separating, the target, or whose missingness predicts it). " +
            "Also returns the per column shape summary and the duplicate row count. Give 0/1 or class labels for a classification target, or a numeric target for regression, not both.")]
        public DataQualityReportResult DataQualityReport(
            [Description(
                "Feature rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description(
                "Optional class label per row for a classification target (any integers). Leave empty for none.")]
            int[]? labels = null,
            [Description("Optional numeric target per row for a regression target. Leave empty for none.")]
            double[]? numericTarget = null,
            [Description(
                "Absolute correlation or AUC distance above which a feature is a leakage suspect, between 0.5 and 1. Default 0.95.")]
            double leakageThreshold = 0.95)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            var hasLabels = labels is { Length: > 0 };
            var hasNumeric = numericTarget is { Length: > 0 };

            if (hasLabels && hasNumeric)
            {
                throw new ArgumentException("Give either labels or numericTarget, not both.", nameof(numericTarget));
            }

            if (double.IsNaN(leakageThreshold) || leakageThreshold < 0.5 || leakageThreshold > 1)
            {
                throw new ArgumentException("leakageThreshold must be between 0.5 and 1.", nameof(leakageThreshold));
            }

            if (hasLabels)
            {
                DataPreparationGuard.SameLength(data.Length, labels!.Length, nameof(labels), "the table");
            }

            if (hasNumeric)
            {
                DataPreparationGuard.SameLength(data.Length, numericTarget!.Length, nameof(numericTarget), "the table");
            }

            var issues = new List<DataQualityIssue>();
            var shapes = Enumerable.Range(0, columns).Select(j => Summarise(names[j], data, j)).ToArray();
            var leakage = new List<LeakageSuspect>();
            var n = data.Length;

            for (var j = 0; j < columns; j++)
            {
                var shape = shapes[j];
                var column = DataPreparationGuard.Column(data, j);
                var finite = column.Where(v => !DataPreparationGuard.IsMissing(v)).ToArray();

                if (column.Any(double.IsInfinity))
                {
                    issues.Add(new DataQualityIssue("Warning", "NonFinite", names[j],
                        $"{column.Count(double.IsInfinity)} infinite value(s) are treated as missing by the preparation tools.",
                        "ApplyColumnFunction or ImputeMissingValues"));
                }

                if (shape.Type == "AllMissing")
                {
                    issues.Add(new DataQualityIssue("Error", "Missing", names[j], "Every value is missing.",
                        "DropSparseRowsAndColumns"));

                    continue;
                }

                var missingFraction = shape.Missing / (double)n;

                if (missingFraction > 0.5)
                {
                    issues.Add(new DataQualityIssue("Warning", "Missing", names[j],
                        $"{missingFraction:P0} of values are missing.", "DropSparseRowsAndColumns"));
                }
                else if (missingFraction > 0.2)
                {
                    issues.Add(new DataQualityIssue("Info", "Missing", names[j],
                        $"{missingFraction:P0} of values are missing.", "ImputeMissingValues"));
                }

                if (shape.Type == "Constant")
                {
                    issues.Add(new DataQualityIssue("Warning", "Constant", names[j], "The column has a single value.",
                        "SelectByVarianceThreshold"));

                    continue;
                }

                var dominant = finite.GroupBy(v => v).Max(g => g.Count()) / (double)finite.Length;

                if (dominant >= 0.99)
                {
                    issues.Add(new DataQualityIssue("Warning", "NearConstant", names[j],
                        $"One value fills {dominant:P1} of the finite rows.", "SelectByVarianceThreshold"));
                }

                if (finite.Length >= 20 && shape.Type == "Integer" && shape.Distinct == finite.Length)
                {
                    issues.Add(new DataQualityIssue("Warning", "IdentifierLike", names[j],
                        "Whole numbers, all distinct: this looks like a row identifier and would not generalise.",
                        "SelectTableColumns"));
                }

                if (shape.Type is "Integer" or "Continuous" && finite.Length >= 8)
                {
                    var skew = finite.Skewness();

                    if (Math.Abs(skew) > 2)
                    {
                        issues.Add(new DataQualityIssue("Info", "Skewed", names[j],
                            $"Skewness is {skew:F2}.", "FitPowerTransformer or ApplyColumnFunction"));
                    }

                    var sorted = finite.OrderBy(v => v).ToArray();
                    var q1 = DataPreparationGuard.Quantile(sorted, 0.25);
                    var q3 = DataPreparationGuard.Quantile(sorted, 0.75);
                    var spread = q3 - q1;

                    if (spread > 0)
                    {
                        var outside = finite.Count(v => v < q1 - 1.5 * spread || v > q3 + 1.5 * spread) /
                                      (double)finite.Length;

                        if (outside > 0.05)
                        {
                            issues.Add(new DataQualityIssue("Info", "Outliers", names[j],
                                $"{outside:P1} of values lie outside the 1.5 IQR fences.", "FitOutlierClipper"));
                        }
                    }
                }

                if (hasLabels || hasNumeric)
                {
                    var suspect = LeakageCheck(names[j], column,
                        hasLabels ? labels!.Select(l => (double)l).ToArray() : numericTarget!,
                        hasLabels && labels!.Distinct().Count() == 2, leakageThreshold);

                    if (suspect != null)
                    {
                        leakage.Add(suspect);

                        issues.Add(new DataQualityIssue("Error", "Leakage", names[j],
                            $"{suspect.Measure} is {suspect.Value:F3} against the target: check that this column is not derived from the outcome.",
                            "SelectTableColumns"));
                    }

                    if (hasLabels && labels!.Distinct().Count() == 2 && shape.Missing >= 5 && shape.Missing < n)
                    {
                        var indicator = column.Select(v => DataPreparationGuard.IsMissing(v) ? 1d : 0d).ToArray();
                        var r = SimpleCorrelation(indicator, labels!.Select(l => (double)l).ToArray());

                        if (!double.IsNaN(r) && Math.Abs(r) >= 0.5)
                        {
                            issues.Add(new DataQualityIssue("Warning", "InformativeMissingness", names[j],
                                $"Whether the value is missing correlates {r:F2} with the target: missingness may leak the outcome, or may be a useful signal.",
                                "ImputeMissingValues with missing indicators"));
                        }
                    }
                }
            }

            var sds = shapes.Where(s => s.Type == "Continuous" || s.Type == "Integer")
                .Select((s, _) => DataPreparationGuard.FiniteColumn(data, Array.IndexOf(names, s.Name)))
                .Where(c => c.Length > 1).Select(c => Math.Sqrt(c.Variance())).Where(sd => sd > 0).ToArray();

            if (sds.Length > 1 && sds.Max() / sds.Min() > 1000)
            {
                issues.Add(new DataQualityIssue("Info", "Scale", null,
                    $"Column standard deviations range over a factor of {sds.Max() / sds.Min():F0}; distance based and regularised models need scaling.",
                    "FitTableScaler"));
            }

            var duplicates = FindDuplicateRows(data);

            if (duplicates.DuplicateRows > 0)
            {
                issues.Add(new DataQualityIssue("Warning", "Duplicates", null,
                    $"{duplicates.DuplicateRows} row(s) exactly repeat an earlier row.", "FindDuplicateRows"));
            }

            if (n < 10 * columns)
            {
                issues.Add(new DataQualityIssue("Warning", "SampleSize", null,
                    $"{n} rows for {columns} columns is under 10 rows per column; expect unstable estimates.",
                    "SelectByVarianceThreshold or FilterCorrelatedFeatures"));
            }

            TargetBalanceResult? balance = null;

            if (hasLabels)
            {
                balance = Balance(labels!);

                foreach (var warning in balance.Warnings)
                {
                    issues.Add(new DataQualityIssue("Warning", "TargetBalance", null, warning, "ComputeClassWeights"));
                }
            }

            return new DataQualityReportResult(n, columns, shapes, duplicates.DuplicateRows, balance, leakage.ToArray(),
                issues.ToArray());
        }

        [AdvancedAnalyticsTool("DataQualityReportCategorical")]
        [Description(
            "Runs quality checks on a categorical (text) table and returns issues with severity (Error, Warning, Info), column, message and the tool to fix it. " +
            "Checks: entirely or mostly empty columns, single-value columns, high cardinality (over 50 distinct values covering more than half the rows, identifier-like), rare categories (more than 20 percent of distinct values occurring once), inconsistent formatting (values that differ only by letter case or surrounding spaces), exact duplicate rows and, with class labels, leakage suspects (Cramer's V with the target at or above the threshold). " +
            "Also returns the per column profile and the duplicate row count.")]
        public CategoricalQualityReportResult DataQualityReportCategorical(
            [Description(
                "Categorical table rows: one array of text values per case, all the same length. Null or empty means missing.")]
            string?[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("Optional class label per row for leakage checks (any integers). Leave empty for none.")]
            int[]? labels = null,
            [Description(
                "Cramer's V at or above which a column is a leakage suspect, between 0.5 and 1. Default 0.95.")]
            double leakageThreshold = 0.95)
        {
            var columns = Categorical(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));

            if (double.IsNaN(leakageThreshold) || leakageThreshold < 0.5 || leakageThreshold > 1)
            {
                throw new ArgumentException("leakageThreshold must be between 0.5 and 1.", nameof(leakageThreshold));
            }

            var hasLabels = labels is { Length: > 0 };

            if (hasLabels)
            {
                DataPreparationGuard.SameLength(data.Length, labels!.Length, nameof(labels), "the table");
            }

            var issues = new List<DataQualityIssue>();
            var profiles = new CategoricalProfile[columns];
            var n = data.Length;

            for (var j = 0; j < columns; j++)
            {
                profiles[j] = ProfileCategorical(names[j], data, j, 5);
                var profile = profiles[j];
                var values = data.Select(r => r[j]).Where(v => !string.IsNullOrEmpty(v)).ToArray();

                if (values.Length == 0)
                {
                    issues.Add(new DataQualityIssue("Error", "Missing", names[j], "Every value is missing.",
                        "DropSparseRowsAndColumns"));

                    continue;
                }

                if (profile.Missing / (double)n > 0.5)
                {
                    issues.Add(new DataQualityIssue("Warning", "Missing", names[j],
                        $"{profile.Missing / (double)n:P0} of values are missing.", "FitRareCategoryGrouping"));
                }

                if (profile.Distinct == 1)
                {
                    issues.Add(new DataQualityIssue("Warning", "Constant", names[j], "The column has a single value.",
                        "SelectTableColumns"));
                }

                if (profile.Distinct > 50 && profile.CardinalityRatio > 0.5)
                {
                    issues.Add(new DataQualityIssue("Warning", "HighCardinality", names[j],
                        $"{profile.Distinct} distinct values over {n} rows: identifier-like.",
                        "FitFrequencyEncoder or FitRareCategoryGrouping"));
                }

                var singles = values.GroupBy(v => v, StringComparer.Ordinal).Count(g => g.Count() == 1);

                if (profile.Distinct >= 5 && singles / (double)profile.Distinct > 0.2)
                {
                    issues.Add(new DataQualityIssue("Info", "RareCategories", names[j],
                        $"{singles} of {profile.Distinct} distinct values occur exactly once.",
                        "FitRareCategoryGrouping"));
                }

                var normalised = values.Distinct(StringComparer.Ordinal)
                    .GroupBy(v => v!.Trim().ToLowerInvariant()).Count(g => g.Count() > 1);

                if (normalised > 0)
                {
                    issues.Add(new DataQualityIssue("Warning", "InconsistentFormatting", names[j],
                        $"{normalised} value(s) differ only by letter case or surrounding spaces.",
                        "FitRareCategoryGrouping"));
                }

                if (hasLabels && profile.Distinct > 1 && labels!.Distinct().Count() > 1)
                {
                    var v = CramersV(data.Select(r => r[j] ?? string.Empty).ToArray(), labels!);

                    if (v >= leakageThreshold)
                    {
                        issues.Add(new DataQualityIssue("Error", "Leakage", names[j],
                            $"Cramer's V is {v:F3} against the target: check that this column is not derived from the outcome.",
                            "SelectTableColumns"));
                    }
                }
            }

            var duplicates = FindDuplicateCategoricalRows(data);

            if (duplicates.DuplicateRows > 0)
            {
                issues.Add(new DataQualityIssue("Warning", "Duplicates", null,
                    $"{duplicates.DuplicateRows} row(s) exactly repeat an earlier row.",
                    "FindDuplicateCategoricalRows"));
            }

            return new CategoricalQualityReportResult(n, columns, profiles, duplicates.DuplicateRows, issues.ToArray());
        }

        private static int Categorical(string?[]?[]? data, string parameterName)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("The table has no rows. Supply one array of text values per row.",
                    parameterName);
            }

            if (data.Any(r => r == null) || data[0]!.Length == 0 || data.Any(r => r!.Length != data[0]!.Length))
            {
                throw new ArgumentException("Every row must be a non-empty array of text values of the same length.",
                    parameterName);
            }

            return data[0]!.Length;
        }

        private static ColumnShape Summarise(string name, double[][] data, int j)
        {
            var column = DataPreparationGuard.Column(data, j);
            var finite = column.Where(v => !DataPreparationGuard.IsMissing(v)).ToArray();
            var distinct = finite.Distinct().Count();
            var missing = column.Length - finite.Length;

            if (finite.Length == 0)
            {
                return new ColumnShape(name, "AllMissing", 0, missing, double.NaN, double.NaN, double.NaN);
            }

            var type = distinct == 1
                ? "Constant"
                : distinct == 2
                    ? "Binary"
                    : finite.All(v => Math.Abs(v - Math.Floor(v)) < 0.0001)
                        ? "Integer"
                        : "Continuous";

            return new ColumnShape(name, type, distinct, missing, finite.Min(), finite.Max(), finite.Average());
        }

        private static CategoricalProfile ProfileCategorical(string name, string?[][] data, int j, int top)
        {
            var values = data.Select(r => r[j]).Where(v => !string.IsNullOrEmpty(v)).ToArray();

            var groups = values.GroupBy(v => v, StringComparer.Ordinal)
                .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).ToArray();

            var entropy =
                groups.Sum(g => -(g.Count() / (double)values.Length) * Math.Log(g.Count() / (double)values.Length));

            return new CategoricalProfile(name, groups.Length, data.Length - values.Length,
                groups.Length == 0 ? string.Empty : groups[0].Key!, groups.Length == 0 ? 0 : groups[0].Count(),
                groups.Length / (double)data.Length, values.Length == 0 ? double.NaN : entropy,
                groups.Take(top).Select(g => new ValueCount(g.Key!, g.Count())).ToArray());
        }

        private static DuplicateRowsResult Duplicates(string[] keys)
        {
            var first = new Dictionary<string, int>(StringComparer.Ordinal);
            var repeats = new Dictionary<int, List<int>>();
            var keep = new List<int>();

            for (var i = 0; i < keys.Length; i++)
            {
                if (first.TryGetValue(keys[i], out var f))
                {
                    if (!repeats.TryGetValue(f, out var list))
                    {
                        list = new List<int>();
                        repeats[f] = list;
                    }

                    list.Add(i);
                }
                else
                {
                    first[keys[i]] = i;
                    keep.Add(i);
                }
            }

            return new DuplicateRowsResult(repeats.Values.Sum(l => l.Count),
                repeats.OrderBy(r => r.Key).Select(r => new DuplicateGroup(r.Key, r.Value.ToArray())).ToArray(),
                keep.ToArray());
        }

        private static TargetBalanceResult Balance(int[] labels)
        {
            var groups = labels.GroupBy(l => l).OrderBy(g => g.Key).ToArray();
            var n = (double)labels.Length;
            var counts = groups.Select(g => new ClassBalanceRow(g.Key, g.Count(), g.Count() / n)).ToArray();
            var smallest = counts.Min(c => c.Count);
            var largest = counts.Max(c => c.Count);
            var entropy = counts.Sum(c => -c.Fraction * Math.Log(c.Fraction));
            var relative = counts.Length > 1 ? entropy / Math.Log(counts.Length) : 0;
            var warnings = new List<string>();

            if (counts.Length < 2)
            {
                warnings.Add("The target has a single class, so no model can be trained on it.");
            }
            else if (smallest / n < 0.01)
            {
                warnings.Add(
                    $"The rarest class is {smallest / n:P2} of rows: accuracy is misleading, use stratified splits, class weights and precision/recall or AUC.");
            }
            else if (smallest / n < 0.1)
            {
                warnings.Add(
                    $"The rarest class is {smallest / n:P1} of rows: use stratified splitting and consider class weights.");
            }

            if (smallest < 30 && counts.Length > 1)
            {
                warnings.Add(
                    $"The rarest class has only {smallest} row(s); estimates involving it will be very noisy.");
            }

            return new TargetBalanceResult(counts, largest / (double)smallest, relative, smallest / n,
                counts.Length == 2, warnings.ToArray());
        }

        private static LeakageSuspect? LeakageCheck(string name, double[] column, double[] target, bool binary,
            double threshold)
        {
            var idx = Enumerable.Range(0, column.Length)
                .Where(i => !DataPreparationGuard.IsMissing(column[i]) && !DataPreparationGuard.IsMissing(target[i]))
                .ToArray();

            if (idx.Length < 10)
            {
                return null;
            }

            var x = idx.Select(i => column[i]).ToArray();
            var y = idx.Select(i => target[i]).ToArray();
            var r = SimpleCorrelation(x, y);

            if (!double.IsNaN(r) && Math.Abs(r) >= threshold)
            {
                return new LeakageSuspect(name, "Correlation", r);
            }

            if (binary)
            {
                var lo = y.Min();
                var positives = idx.Count(i => Math.Abs(target[i] - lo) > 0.0001);
                var negatives = idx.Length - positives;

                if (positives > 0 && negatives > 0)
                {
                    var order = Enumerable.Range(0, x.Length).OrderBy(i => x[i]).ToArray();
                    var ranks = new double[x.Length];
                    var start = 0;

                    while (start < order.Length)
                    {
                        var end = start;

                        while (end + 1 < order.Length && Math.Abs(x[order[end + 1]] - x[order[start]]) < 0.0001)
                        {
                            end++;
                        }

                        for (var k = start; k <= end; k++)
                        {
                            ranks[order[k]] = (start + end) / 2d + 1;
                        }

                        start = end + 1;
                    }

                    var rankSum = Enumerable.Range(0, x.Length).Where(i => Math.Abs(y[i] - lo) > 0.0001)
                        .Sum(i => ranks[i]);
                    var auc = (rankSum - positives * (positives + 1) / 2d) / ((double)positives * negatives);

                    if (auc >= threshold || auc <= 1 - threshold)
                    {
                        return new LeakageSuspect(name, "Auc", auc);
                    }
                }
            }

            return null;
        }

        private static double SimpleCorrelation(double[] a, double[] b)
        {
            var idx = Enumerable.Range(0, a.Length)
                .Where(i => !DataPreparationGuard.IsMissing(a[i]) && !DataPreparationGuard.IsMissing(b[i])).ToArray();

            if (idx.Length < 3)
            {
                return double.NaN;
            }

            var x = idx.Select(i => a[i]).ToArray();
            var y = idx.Select(i => b[i]).ToArray();
            var mx = x.Average();
            var my = y.Average();
            double sxy = 0;
            double sxx = 0;
            double syy = 0;

            for (var i = 0; i < x.Length; i++)
            {
                sxy += (x[i] - mx) * (y[i] - my);
                sxx += (x[i] - mx) * (x[i] - mx);
                syy += (y[i] - my) * (y[i] - my);
            }

            return sxx > 0 && syy > 0 ? sxy / Math.Sqrt(sxx * syy) : double.NaN;
        }

        private static double CramersV(string[] feature, int[] labels)
        {
            var categories = feature.Distinct(StringComparer.Ordinal).ToArray();
            var classes = labels.Distinct().ToArray();
            var n = (double)feature.Length;
            double chi = 0;

            foreach (var category in categories)
            {
                var rowTotal = feature.Count(f => f == category);

                foreach (var c in classes)
                {
                    var observed = Enumerable.Range(0, feature.Length)
                        .Count(i => feature[i] == category && labels[i] == c);

                    var expected = rowTotal * (double)labels.Count(l => l == c) / n;
                    chi += (observed - expected) * (observed - expected) / expected;
                }
            }

            return Math.Sqrt(chi / (n * (Math.Min(categories.Length, classes.Length) - 1)));
        }
    }

    public sealed record ColumnShape(
        string Name,
        string Type,
        int Distinct,
        int Missing,
        double Minimum,
        double Maximum,
        double Mean);

    public sealed record DatasetShape(int Rows, int Columns, ColumnShape[] ColumnSummaries, long ApproximateBytes);

    // ReSharper disable once NotAccessedPositionalProperty.Global
    public sealed record ValueCount(string Value, int Count);

    public sealed record CategoricalProfile(
        string Name,
        int Distinct,
        int Missing,
        string MostFrequent,
        int MostFrequentCount,
        double CardinalityRatio,
        double Entropy,
        ValueCount[] TopValues);

    public sealed record DuplicateGroup(int FirstRow, int[] Repeats);

    public sealed record DuplicateRowsResult(int DuplicateRows, DuplicateGroup[] Groups, int[] RowsToKeep);

    // ReSharper disable once NotAccessedPositionalProperty.Global
    public sealed record ClassBalanceRow(int Label, int Count, double Fraction);

    public sealed record TargetBalanceResult(
        ClassBalanceRow[] Classes,
        double ImbalanceRatio,
        double RelativeEntropy,
        double SmallestClassFraction,
        bool IsBinary,
        string[] Warnings);

    public sealed record LeakageSuspect(string Column, string Measure, double Value);

    public sealed record DataQualityIssue(
        string Severity,
        string Category,
        string? Column,
        string Message,
        string SuggestedTool);

    public sealed record DataQualityReportResult(
        int Rows,
        int Columns,
        ColumnShape[] ColumnSummaries,
        int DuplicateRows,
        TargetBalanceResult? TargetBalance,
        LeakageSuspect[] LeakageSuspects,
        DataQualityIssue[] Issues);

    public sealed record CategoricalQualityReportResult(
        int Rows,
        // ReSharper disable once NotAccessedPositionalProperty.Global
        int Columns,
        CategoricalProfile[] ColumnProfiles,
        int DuplicateRows,
        DataQualityIssue[] Issues);
}
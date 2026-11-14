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
using Accord.Statistics.Filters;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling
{
    public enum CategoryOrder
    {
        Sorted = 0,
        FirstAppearance = 1,
        MostFrequentFirst = 2
    }

    [AdvancedAnalyticsToolGroup("DataPreparationAndResampling")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class EncodingTools
    {
        [AdvancedAnalyticsTool("FitLabelEncoder")]
        [Description(
            "Learns an integer code for every distinct category of each text column and returns the coded table plus the category lists (the position in a list is the code) for ApplyLabelEncoder. " +
            "Use it to turn categories into numbers for tree style models or for counting; the codes imply an order that linear models will wrongly read as magnitude, so prefer FitOneHotEncoder for those. " +
            "Sorted numbers categories alphabetically, FirstAppearance in order seen, MostFrequentFirst by descending count. Null or empty cells are treated as missing and coded -1 and are not counted as a category.")]
        public LabelEncodingResult FitLabelEncoder(
            [Description(
                "Categorical table rows: one array of text values per case, all the same length. Null or empty means missing.")]
            string[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("Sorted (alphabetical), FirstAppearance or MostFrequentFirst. Default Sorted.")]
            CategoryOrder order = CategoryOrder.Sorted)
        {
            var columns = CategoricalTable(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            var categories = new string[columns][];

            for (var j = 0; j < columns; j++)
            {
                var values = data.Select(r => r[j]).Where(v => !string.IsNullOrEmpty(v)).ToArray();

                categories[j] = order switch
                {
                    CategoryOrder.FirstAppearance => values.Distinct(StringComparer.Ordinal).ToArray(),
                    CategoryOrder.MostFrequentFirst => values.GroupBy(v => v, StringComparer.Ordinal)
                        .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
                        .Select(g => g.Key).ToArray(),
                    _ => values.Length == 0
                        ? Array.Empty<string>()
                        : new Codification<string>.Options(names[j])
                            .Learn(values.Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal)
                                .ToArray()).Values
                };
            }

            return new LabelEncodingResult(names, categories, LabelCodes(data, categories, -1));
        }

        [AdvancedAnalyticsTool("ApplyLabelEncoder")]
        [Description(
            "Codes a categorical table with fixed category lists: a value's code is its position in its column's list, and anything not in the list (or null or empty) gets the unknownCode. " +
            "Use it with lists from FitLabelEncoder to encode new rows identically, or supply your own lists in rank order to do an ordinal encoding of ordered categories such as Low, Medium, High.")]
        public double[][] ApplyLabelEncoder(
            [Description(
                "Categorical table rows: one array of text values per case, in the same column order as the category lists.")]
            string[][] data,
            [Description("One category list per column; a category's code is its position in the list, starting at 0.")]
            string[][] categories,
            [Description("The code given to values not found in the list, and to null or empty. Default -1.")]
            double unknownCode = -1)
        {
            var columns = CategoricalTable(data, nameof(data));
            ValidateCategories(columns, categories, nameof(categories));

            return LabelCodes(data, categories, unknownCode);
        }

        [AdvancedAnalyticsTool("FitOneHotEncoder")]
        [Description(
            "Expands each categorical column into one 0/1 column per category, named <column>=<category>, and returns the encoded numeric table with the category lists for ApplyOneHotEncoder. " +
            "Use it to feed categories to linear or distance based models without implying an order. Set dropFirst to remove the first category of each column when the model needs to avoid the dummy variable trap. " +
            "Null or empty cells are treated as missing and produce all zeros for that column. Very high cardinality columns explode the width: check cardinality first or group rare categories.")]
        public OneHotEncodingResult FitOneHotEncoder(
            [Description(
                "Categorical table rows: one array of text values per case, all the same length. Null or empty means missing.")]
            string[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("When true, omit the first (alphabetically) category of every column. Default false.")]
            bool dropFirst = false,
            [Description("Refuse to run if any column has more distinct categories than this. Default 200.")]
            int maximumCategoriesPerColumn = 200)
        {
            var columns = CategoricalTable(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            var categories = new string[columns][];

            for (var j = 0; j < columns; j++)
            {
                categories[j] = data.Select(r => r[j]).Where(v => !string.IsNullOrEmpty(v))
                    .Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToArray();

                if (categories[j].Length > maximumCategoriesPerColumn)
                {
                    throw new ArgumentException(
                        $"Column '{names[j]}' has {categories[j].Length} categories, above the limit of {maximumCategoriesPerColumn}. Group rare categories first or raise the limit.",
                        nameof(maximumCategoriesPerColumn));
                }
            }

            var featureNames = OneHotNames(names, categories, dropFirst);

            return new OneHotEncodingResult(names, categories, featureNames, OneHot(data, categories, dropFirst));
        }

        [AdvancedAnalyticsTool("ApplyOneHotEncoder")]
        [Description(
            "One-hot encodes a categorical table with fixed category lists, producing the same columns as FitOneHotEncoder. " +
            "A category not in the list (or null or empty) yields all zeros for that column, so unseen levels in new data never change the width.")]
        public double[][] ApplyOneHotEncoder(
            [Description(
                "Categorical table rows: one array of text values per case, in the same column order as the category lists.")]
            string[][] data,
            [Description("One category list per column, as returned by FitOneHotEncoder.")]
            string[][] categories,
            [Description(
                "Whether the first category of each column was dropped when fitting. Must match. Default false.")]
            bool dropFirst = false)
        {
            var columns = CategoricalTable(data, nameof(data));
            ValidateCategories(columns, categories, nameof(categories));

            return OneHot(data, categories, dropFirst);
        }

        [AdvancedAnalyticsTool("FitTargetMeanEncoder")]
        [Description(
            "Replaces each category by the smoothed event rate of a 0/1 target within that category: (n_c * rate_c + m * overallRate) / (n_c + m), where m is the smoothing strength. " +
            "Small categories are pulled towards the overall rate, which limits overfitting; larger m means more pull. Returns the encoded table and, per column, each category's count, events and encoded value plus the value used for unseen categories (the overall rate) for ApplyCategoryEncoder. " +
            "Fit on training rows only, because the target leaks into the encoding. Null or empty cells form their own category.")]
        public CategoryEncodingResult FitTargetMeanEncoder(
            [Description(
                "Categorical table rows: one array of text values per case, all the same length. Null or empty is its own category.")]
            string[][] data,
            [Description("The 0/1 target for each row (1 is the event), in row order.")]
            int[] labels,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("Smoothing strength m in pseudo-observations, 0 or more. Default 10.")]
            double smoothing = 10)
        {
            var context = TargetContext(data, labels, columnNames, smoothing, nameof(smoothing));
            var overall = context.Events / (double)context.Rows;

            return BuildEncoding(data, context.Names, labels, overall,
                (n, e, _) => (n * (e / (double)n) + smoothing * overall) / (n + smoothing));
        }

        [AdvancedAnalyticsTool("FitWeightOfEvidenceEncoder")]
        [Description(
            "Replaces each category by its weight of evidence for a 0/1 target: ln( share of all events in the category / share of all non-events in the category ), the standard credit scoring encoding. " +
            "Positive means the category holds relatively more events than non-events. A pseudo-count (smoothing) is added to the event and non-event counts of every category so empty cells give a finite value. " +
            "Returns the encoded table and, per column, each category's count, events, encoded value and the column's information value (predictive strength: under 0.02 useless, 0.1 to 0.3 medium, above 0.5 suspiciously strong). Unseen categories encode as 0. Fit on training rows only.")]
        public CategoryEncodingResult FitWeightOfEvidenceEncoder(
            [Description(
                "Categorical table rows: one array of text values per case, all the same length. Null or empty is its own category.")]
            string[][] data,
            [Description("The 0/1 target for each row (1 is the event), in row order. Both classes must be present.")]
            int[] labels,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description(
                "Pseudo-count added to the event and non-event counts of each category, 0 or more. Default 0.5.")]
            double smoothing = 0.5)
        {
            var context = TargetContext(data, labels, columnNames, smoothing, nameof(smoothing));

            if (context.Events == 0 || context.Events == context.Rows)
            {
                throw new ArgumentException(
                    "Weight of evidence needs both events and non-events in the labels. The target has only one class.",
                    nameof(labels));
            }

            var nonEvents = context.Rows - context.Events;

            return BuildEncoding(data, context.Names, labels, 0, null, (n, e, k) =>
            {
                var eventShare = (e + smoothing) / (context.Events + smoothing * k);
                var nonEventShare = (n - e + smoothing) / (nonEvents + smoothing * k);

                return Math.Log(eventShare / nonEventShare);
            });
        }

        [AdvancedAnalyticsTool("FitFrequencyEncoder")]
        [Description(
            "Replaces each category by how often it occurs in that column, as a count or as a fraction of rows, and returns the encoded table plus the mapping for ApplyCategoryEncoder. " +
            "Use it for high cardinality identifiers such as merchant or device where rarity itself is informative and no target is needed. Unseen categories encode as 0. Null or empty cells form their own category.")]
        public CategoryEncodingResult FitFrequencyEncoder(
            [Description(
                "Categorical table rows: one array of text values per case, all the same length. Null or empty is its own category.")]
            string[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description(
                "When true the encoded value is the fraction of rows (0 to 1); when false it is the row count. Default true.")]
            bool asFraction = true)
        {
            var columns = CategoricalTable(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));

            return BuildEncoding(data, names, null, 0, (n, _, _) => asFraction ? n / (double)data.Length : n);
        }

        [AdvancedAnalyticsTool("ApplyCategoryEncoder")]
        [Description(
            "Encodes a categorical table with a fixed category-to-number mapping, as produced by FitTargetMeanEncoder, FitWeightOfEvidenceEncoder or FitFrequencyEncoder. " +
            "Each cell becomes the value listed for its category in its column, or the column's default value when the category was never seen, so unseen levels are handled explicitly.")]
        public double[][] ApplyCategoryEncoder(
            [Description(
                "Categorical table rows: one array of text values per case, in the same column order as the mappings.")]
            string?[][] data,
            [Description("One category list per column, as returned in the encoding tables.")]
            string[][] categories,
            [Description("One array of encoded values per column, aligned with the category lists.")]
            double[][] values,
            [Description("The value to use for an unseen category, one per column.")]
            double[] defaultValues)
        {
            var columns = CategoricalTable(data, nameof(data));
            ValidateCategories(columns, categories, nameof(categories));

            if (values == null || values.Length != columns || defaultValues == null || defaultValues.Length != columns)
            {
                throw new ArgumentException(
                    $"Give one values array and one default value per column: the table has {columns} columns.",
                    nameof(values));
            }

            for (var j = 0; j < columns; j++)
            {
                if (values[j] == null || values[j].Length != categories[j].Length)
                {
                    throw new ArgumentException($"Column {j} needs one value per category.", nameof(values));
                }
            }

            var maps = categories.Select((c, j) => c.Select((cat, k) => (cat, k))
                .ToDictionary(x => x.cat, x => values[j][x.k], StringComparer.Ordinal)).ToArray();

            return DataPreparationGuard.Build(data.Length, columns, (i, j) =>
                maps[j].TryGetValue(data[i][j] ?? string.Empty, out var v) ? v : defaultValues[j]);
        }

        [AdvancedAnalyticsTool("FitRareCategoryGrouping")]
        [Description(
            "Finds, for each categorical column, which categories are common enough to keep, and rewrites the rest to a single 'other' label. " +
            "A category is kept when it occurs at least minimumCount times and at least minimumFraction of the rows. Use it before one-hot encoding to cap the width and stop noise categories. " +
            "Returns the rewritten table and the kept categories per column for ApplyRareCategoryGrouping. Null or empty cells form their own category.")]
        public RareGroupingResult FitRareCategoryGrouping(
            [Description(
                "Categorical table rows: one array of text values per case, all the same length. Null or empty is its own category.")]
            string[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("Keep a category only if it occurs at least this many times. Default 5.")]
            int minimumCount = 5,
            [Description(
                "Keep a category only if it covers at least this fraction of rows, between 0 and 1. Default 0.")]
            double minimumFraction = 0,
            [Description("The label that replaces rare categories. Default other.")]
            string otherLabel = "other")
        {
            var columns = CategoricalTable(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            DataPreparationGuard.Fraction(minimumFraction, nameof(minimumFraction));

            if (minimumCount < 0)
            {
                throw new ArgumentException("minimumCount must be 0 or more.", nameof(minimumCount));
            }

            var kept = new string[columns][];
            var grouped = new int[columns];

            for (var j = 0; j < columns; j++)
            {
                var counts = data.GroupBy(r => r[j], StringComparer.Ordinal).ToArray();

                kept[j] = counts.Where(g =>
                        g.Count() >= minimumCount && g.Count() / (double)data.Length >= minimumFraction)
                    .Select(g => g.Key).OrderBy(k => k, StringComparer.Ordinal).ToArray();

                grouped[j] = counts.Count(g => !kept[j].Contains(g.Key));
            }

            return new RareGroupingResult(names, kept, grouped, ApplyGrouping(data, kept, otherLabel));
        }

        [AdvancedAnalyticsTool("ApplyRareCategoryGrouping")]
        [Description(
            "Rewrites every category that is not in the kept list of its column to the other label, using kept lists learned by FitRareCategoryGrouping. " +
            "This maps categories first seen in new data to 'other' so downstream encoders see only known levels.")]
        public string[][] ApplyRareCategoryGrouping(
            [Description(
                "Categorical table rows: one array of text values per case, in the same column order as the kept lists.")]
            string[][] data,
            [Description("The kept categories per column, as returned by FitRareCategoryGrouping.")]
            string[][] keptCategories,
            [Description("The label that replaces categories not in the kept list. Default other.")]
            string otherLabel = "other")
        {
            var columns = CategoricalTable(data, nameof(data));
            ValidateCategories(columns, keptCategories, nameof(keptCategories));

            return ApplyGrouping(data, keptCategories, otherLabel);
        }

        private static int CategoricalTable(string?[]?[]? data, string parameterName)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("The table has no rows. Supply one array of text values per row.",
                    parameterName);
            }

            if (data.Any(r => r == null))
            {
                throw new ArgumentException("A row is null. Every row must be an array of text values.", parameterName);
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

        private static void ValidateCategories(int columns, string[]?[]? categories, string parameterName)
        {
            if (categories == null || categories.Length != columns || categories.Any(r => r == null))
            {
                throw new ArgumentException($"Give one category list per column: the table has {columns} columns.",
                    parameterName);
            }
        }

        private static double[][] LabelCodes(string[][] data, string[][] categories, double unknown)
        {
            var maps = categories.Select(c => c.Select((cat, k) => (cat, k))
                .ToDictionary(x => x.cat, x => (double)x.k, StringComparer.Ordinal)).ToArray();

            return DataPreparationGuard.Build(data.Length, categories.Length, (i, j) =>
                !string.IsNullOrEmpty(data[i][j]) && maps[j].TryGetValue(data[i][j], out var code) ? code : unknown);
        }

        private static string[] OneHotNames(string[] names, string[][] categories, bool dropFirst)
        {
            return names.SelectMany((n, j) => categories[j].Skip(dropFirst ? 1 : 0).Select(c => n + "=" + c)).ToArray();
        }

        private static double[][] OneHot(string[][] data, string[][] categories, bool dropFirst)
        {
            var width = categories.Sum(c => Math.Max(0, c.Length - (dropFirst ? 1 : 0)));
            var table = new double[data.Length][];

            for (var i = 0; i < data.Length; i++)
            {
                table[i] = new double[width];
                var offset = 0;

                for (var j = 0; j < categories.Length; j++)
                {
                    var start = dropFirst ? 1 : 0;
                    var position = Array.IndexOf(categories[j], data[i][j]);

                    if (!string.IsNullOrEmpty(data[i][j]) && position >= start)
                    {
                        table[i][offset + position - start] = 1;
                    }

                    offset += Math.Max(0, categories[j].Length - start);
                }
            }

            return table;
        }

        private static (string[] Names, int Rows, int Events) TargetContext(string[][] data, int[] labels,
            string[]? columnNames, double smoothing, string smoothingName)
        {
            var columns = CategoricalTable(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            DataPreparationGuard.Binary(labels, nameof(labels));
            DataPreparationGuard.SameLength(data.Length, labels.Length, nameof(labels), "the table");

            if (double.IsNaN(smoothing) || smoothing < 0)
            {
                throw new ArgumentException("Smoothing must be 0 or more.", smoothingName);
            }

            return (names, data.Length, labels.Sum());
        }

        private static CategoryEncodingResult BuildEncoding(
            string[][] data,
            string[] names,
            int[]? labels,
            double defaultValue,
            Func<int, int, int, double>? mean,
            Func<int, int, int, double>? evidence = null)
        {
            var columns = names.Length;
            var tables = new CategoryEncodingTable[columns];
            var totalEvents = labels?.Sum() ?? 0;
            var totalNon = labels == null ? 0 : labels.Length - totalEvents;

            for (var j = 0; j < columns; j++)
            {
                var groups = data.Select((r, i) => (Key: r[j], Row: i))
                    .GroupBy(x => x.Key, StringComparer.Ordinal)
                    .OrderBy(g => g.Key, StringComparer.Ordinal)
                    .ToArray();

                var k = groups.Length;
                var counts = groups.Select(g => g.Count()).ToArray();
                var events = groups.Select(g => labels == null ? 0 : g.Sum(x => labels[x.Row])).ToArray();
                var function = mean ?? evidence!;
                var values = counts.Select((n, c) => function(n, events[c], k)).ToArray();

                var informationValue = 0d;

                if (evidence != null)
                {
                    informationValue = counts.Select((n, c) =>
                    {
                        var eventShare = (events[c] + 0.0) / totalEvents;
                        var nonShare = (n - events[c] + 0.0) / totalNon;

                        return eventShare > 0 && nonShare > 0
                            ? (eventShare - nonShare) * Math.Log(eventShare / nonShare)
                            : 0;
                    }).Sum();
                }

                tables[j] = new CategoryEncodingTable(names[j], groups.Select(g => g.Key).ToArray(), counts, events,
                    values,
                    defaultValue, informationValue);
            }

            var maps = tables.Select(t => t.Categories.Select((c, k) => (c, k))
                .ToDictionary(x => x.c, x => t.Values[x.k], StringComparer.Ordinal)).ToArray();

            var table = DataPreparationGuard.Build(data.Length, columns, (i, j) => maps[j][data[i][j]]);

            return new CategoryEncodingResult(names, tables, table);
        }

        private static string[][] ApplyGrouping(string[][] data, string[][] kept, string otherLabel)
        {
            var sets = kept.Select(k => new HashSet<string>(k, StringComparer.Ordinal)).ToArray();

            return data.Select(r => r
                .Select((v, j) => sets[j].Contains(v) ? v : otherLabel)
                .ToArray()).ToArray();
        }
    }

    // ReSharper disable once NotAccessedPositionalProperty.Global
    public sealed record LabelEncodingResult(string[] ColumnNames, string[][] Categories, double[][] Table);

    public sealed record OneHotEncodingResult(
        // ReSharper disable once NotAccessedPositionalProperty.Global
        string[] ColumnNames,
        string[][] Categories,
        string[] FeatureNames,
        double[][] Table);

    public sealed record CategoryEncodingTable(
        // ReSharper disable once NotAccessedPositionalProperty.Global
        string Column,
        string[] Categories,
        int[] Counts,
        int[] Events,
        double[] Values,
        double DefaultValue,
        double InformationValue);

    // ReSharper disable once NotAccessedPositionalProperty.Global
    public sealed record CategoryEncodingResult(string[] ColumnNames, CategoryEncodingTable[] Tables, double[][] Table);

    public sealed record RareGroupingResult(
        // ReSharper disable once NotAccessedPositionalProperty.Global
        string[] ColumnNames,
        string[][] KeptCategories,
        int[] CategoriesGroupedPerColumn,
        string[][] Table);
}
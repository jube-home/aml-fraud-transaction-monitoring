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
using Accord.Statistics;
using Accord.Statistics.Models.Regression.Linear;
using Accord.Statistics.Testing;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling
{
    public enum CorrelationKind
    {
        Pearson = 0,
        Spearman = 1
    }

    [AdvancedAnalyticsToolGroup("DataPreparationAndResampling")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class FeatureSelectionTools
    {
        [AdvancedAnalyticsTool("SelectByVarianceThreshold")]
        [Description(
            "Drops numeric columns whose sample variance (over their finite values) is at or below a threshold. " +
            "With the default threshold of 0 it removes constant columns, which carry no information and break many models. Raise it to remove near-constant columns, remembering that variance depends on the units of the column, so scale first if columns differ in size. " +
            "Returns each column's variance and finite count and the names kept and dropped. A column with fewer than 2 finite values has NaN variance and is dropped.")]
        public VarianceSelectionResult SelectByVarianceThreshold(
            [Description(
                "Table rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("Drop columns whose variance is at or below this value, 0 or more. Default 0.")]
            double threshold = 0)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));

            if (double.IsNaN(threshold) || threshold < 0)
            {
                throw new ArgumentException("threshold must be 0 or more.", nameof(threshold));
            }

            var variances = new double[columns];
            var counts = new int[columns];

            for (var j = 0; j < columns; j++)
            {
                var finite = DataPreparationGuard.FiniteColumn(data, j);
                counts[j] = finite.Length;
                variances[j] = finite.Length < 2 ? double.NaN : finite.Variance();
            }

            var keep = Enumerable.Range(0, columns).Where(j => variances[j] > threshold).ToArray();

            return new VarianceSelectionResult(names, variances, counts, keep.Select(j => names[j]).ToArray(),
                Enumerable.Range(0, columns).Except(keep).Select(j => names[j]).ToArray());
        }

        [AdvancedAnalyticsTool("FilterCorrelatedFeatures")]
        [Description(
            "Finds pairs of numeric columns whose absolute correlation exceeds a threshold and drops one of each pair, to remove redundant, collinear features. " +
            "Correlation is Pearson (linear) or Spearman (rank, monotone) computed on the rows where both columns are finite. The algorithm repeatedly takes the most correlated remaining pair and drops the member with the larger average absolute correlation to the others; if a numeric target is given, it instead drops the member less correlated with the target. " +
            "Returns the pairs above the threshold with their correlation, the columns dropped with the partner and reason, and the columns kept.")]
        public CorrelationFilterResult FilterCorrelatedFeatures(
            [Description(
                "Table rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("Absolute correlation above which a pair counts as redundant, between 0 and 1. Default 0.9.")]
            double threshold = 0.9,
            [Description("Pearson or Spearman. Default Pearson.")]
            CorrelationKind kind = CorrelationKind.Pearson,
            [Description(
                "Optional numeric target per row (same length as the table); the member of a pair less correlated with it is dropped. Leave empty to use average correlation.")]
            double[]? target = null)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            DataPreparationGuard.Fraction(threshold, nameof(threshold));

            if (target is { Length: > 0 })
            {
                DataPreparationGuard.SameLength(data.Length, target.Length, nameof(target), "the table");
            }

            var cols = Enumerable.Range(0, columns).Select(j => DataPreparationGuard.Column(data, j)).ToArray();
            var corr = new double[columns, columns];

            for (var a = 0; a < columns; a++)
            {
                corr[a, a] = 1;

                for (var b = a + 1; b < columns; b++)
                {
                    corr[a, b] = corr[b, a] = PairCorrelation(cols[a], cols[b], kind);
                }
            }

            var targetCorr = target is { Length: > 0 }
                ? cols.Select(c => Math.Abs(PairCorrelation(c, target, kind))).ToArray()
                : null;

            var pairs = new List<CorrelatedPair>();

            for (var a = 0; a < columns; a++)
            {
                for (var b = a + 1; b < columns; b++)
                {
                    if (Math.Abs(corr[a, b]) > threshold)
                    {
                        pairs.Add(new CorrelatedPair(names[a], names[b], corr[a, b]));
                    }
                }
            }

            var alive = Enumerable.Range(0, columns).ToList();
            var dropped = new List<DroppedFeature>();

            while (true)
            {
                var best = (A: -1, B: -1, Value: threshold);

                foreach (var a in alive)
                {
                    foreach (var b in alive.Where(b => b > a))
                    {
                        var v = Math.Abs(corr[a, b]);

                        if (v > best.Value)
                        {
                            best = (a, b, v);
                        }
                    }
                }

                if (best.A < 0)
                {
                    break;
                }

                double Score(int c)
                {
                    return targetCorr != null
                        ? -(double.IsNaN(targetCorr[c]) ? 0 : targetCorr[c])
                        : alive.Where(o => o != c).Select(o => Math.Abs(corr[c, o])).Where(v => !double.IsNaN(v))
                            .DefaultIfEmpty(0).Average();
                }

                var drop = Score(best.A) >= Score(best.B) ? best.A : best.B;
                var partner = drop == best.A ? best.B : best.A;
                alive.Remove(drop);
                dropped.Add(new DroppedFeature(names[drop], names[partner], corr[drop, partner]));
            }

            return new CorrelationFilterResult(pairs.ToArray(), dropped.ToArray(),
                alive.Select(j => names[j]).ToArray());
        }

        [AdvancedAnalyticsTool("RankFeaturesByAnovaF")]
        [Description(
            "Ranks numeric features by how well they separate the classes of a categorical target, using a one-way ANOVA F test per feature (between-class variance over within-class variance). " +
            "A larger F and smaller p-value mean the class averages differ more than chance allows. It only sees differences in means, not shape, and assumes roughly normal features with similar spread per class. " +
            "Returns features best first with F, p-value, a Benjamini-Hochberg adjusted p-value for testing many features and the rank. Rows with a missing value are ignored for that feature only; a feature with no usable rows in two classes gets NaN and is ranked last.")]
        public FeatureRankingResult RankFeaturesByAnovaF(
            [Description(
                "Feature rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "The class label for each row (any integers, for example 0/1 or 0..k), same length as the rows.")]
            int[] labels,
            [Description(
                "Optional name for each feature column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            DataPreparationGuard.Labels(labels, nameof(labels));
            DataPreparationGuard.SameLength(data.Length, labels.Length, nameof(labels), "the table");

            if (labels.Distinct().Count() < 2)
            {
                throw new ArgumentException("The labels contain a single class, so no feature can separate classes.",
                    nameof(labels));
            }

            var scores = new double[columns];
            var pValues = new double[columns];

            for (var j = 0; j < columns; j++)
            {
                var groups = Enumerable.Range(0, data.Length)
                    .Where(i => !DataPreparationGuard.IsMissing(data[i][j]))
                    .GroupBy(i => labels[i])
                    .Select(g => g.Select(i => data[i][j]).ToArray())
                    .Where(g => g.Length > 0)
                    .ToArray();

                if (groups.Length < 2 || groups.Sum(g => g.Length) - groups.Length < 1)
                {
                    scores[j] = double.NaN;
                    pValues[j] = double.NaN;
                    continue;
                }

                try
                {
                    var anova = new OneWayAnova(groups);
                    scores[j] = anova.FTest.Statistic;
                    pValues[j] = anova.FTest.PValue;
                }
                catch (Exception)
                {
                    scores[j] = double.NaN;
                    pValues[j] = double.NaN;
                }
            }

            return Rank(names, scores, pValues);
        }

        [AdvancedAnalyticsTool("RankFeaturesByFRegression")]
        [Description(
            "Ranks numeric features by their linear relationship with a numeric target: for each feature the correlation r with the target gives F = r^2 / (1 - r^2) * (n - 2) on 1 and n - 2 degrees of freedom. " +
            "Use it to screen predictors for a continuous outcome such as amount. It sees only linear association, one feature at a time. " +
            "Returns features best first with F, p-value, a Benjamini-Hochberg adjusted p-value and the rank. Rows where the feature or target is missing are ignored; a feature with fewer than 3 usable rows or no variation gets NaN and is ranked last.")]
        public FeatureRankingResult RankFeaturesByFRegression(
            [Description(
                "Feature rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description("The numeric target for each row, same length as the rows.")]
            double[] target,
            [Description(
                "Optional name for each feature column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            DataPreparationGuard.Vector(target, nameof(target));
            DataPreparationGuard.SameLength(data.Length, target.Length, nameof(target), "the table");

            var scores = new double[columns];
            var pValues = new double[columns];

            for (var j = 0; j < columns; j++)
            {
                var pairs = Enumerable.Range(0, data.Length)
                    .Where(i => !DataPreparationGuard.IsMissing(data[i][j]) &&
                                !DataPreparationGuard.IsMissing(target[i]))
                    .ToArray();

                var r = pairs.Length < 3
                    ? double.NaN
                    : PairCorrelation(pairs.Select(i => data[i][j]).ToArray(), pairs.Select(i => target[i]).ToArray(),
                        CorrelationKind.Pearson);

                if (double.IsNaN(r))
                {
                    scores[j] = double.NaN;
                    pValues[j] = double.NaN;
                    continue;
                }

                var r2 = Math.Min(r * r, 1 - 1e-15);
                scores[j] = r2 / (1 - r2) * (pairs.Length - 2);
                pValues[j] = new FTest(scores[j], 1, pairs.Length - 2).PValue;
            }

            return Rank(names, scores, pValues);
        }

        [AdvancedAnalyticsTool("RankCountFeaturesByChiSquare")]
        [Description(
            "Ranks non-negative numeric features (counts, frequencies, amounts, one-hot flags) by a chi-square test of whether the feature's total is spread across the classes in proportion to class size. " +
            "Use it for count-like features against a categorical target. Negative values are rejected because the test needs non-negative mass. " +
            "Returns features best first with the chi-square statistic, its p-value on classes-1 degrees of freedom, a Benjamini-Hochberg adjusted p-value and the rank. A feature that is zero everywhere gets NaN and is ranked last. Missing cells count as 0.")]
        public FeatureRankingResult RankCountFeaturesByChiSquare(
            [Description(
                "Feature rows: one array of non-negative numbers per case, all the same length. NaN counts as 0.")]
            double[][] data,
            [Description("The class label for each row (any integers), same length as the rows.")]
            int[] labels,
            [Description(
                "Optional name for each feature column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            DataPreparationGuard.Labels(labels, nameof(labels));
            DataPreparationGuard.SameLength(data.Length, labels.Length, nameof(labels), "the table");

            if (data.Any(r => r.Any(v => !DataPreparationGuard.IsMissing(v) && v < 0)))
            {
                throw new ArgumentException(
                    "Chi-square on counts needs non-negative values. Shift, rescale to 0..1 or use RankFeaturesByAnovaF.",
                    nameof(data));
            }

            var classes = labels.Distinct().OrderBy(c => c).ToArray();

            if (classes.Length < 2)
            {
                throw new ArgumentException("The labels contain a single class, so no feature can separate classes.",
                    nameof(labels));
            }

            var scores = new double[columns];
            var pValues = new double[columns];

            for (var j = 0; j < columns; j++)
            {
                var total = data.Sum(r => DataPreparationGuard.IsMissing(r[j]) ? 0 : r[j]);

                if (total <= 0)
                {
                    scores[j] = double.NaN;
                    pValues[j] = double.NaN;
                    continue;
                }

                var statistic = 0d;

                foreach (var c in classes)
                {
                    var observed = Enumerable.Range(0, data.Length).Where(i => labels[i] == c)
                        .Sum(i => DataPreparationGuard.IsMissing(data[i][j]) ? 0 : data[i][j]);

                    var expected = total * labels.Count(l => l == c) / labels.Length;
                    statistic += (observed - expected) * (observed - expected) / expected;
                }

                scores[j] = statistic;
                pValues[j] = new ChiSquareTest(statistic, classes.Length - 1).PValue;
            }

            return Rank(names, scores, pValues);
        }

        [AdvancedAnalyticsTool("RankCategoricalFeaturesByChiSquare")]
        [Description(
            "Ranks categorical (text) features by Pearson's chi-square test of independence between the feature and a categorical target. " +
            "For each column it builds the category by class table and returns the chi-square statistic, degrees of freedom (categories-1)*(classes-1), p-value, a Benjamini-Hochberg adjusted p-value, Cramer's V (effect size from 0 to 1, comparable across features) and the rank; features are ordered by Cramer's V, best first. " +
            "Null or empty cells are their own category. The test is unreliable when many expected counts are below 5: group rare categories first. A column with a single category gets NaN and is ranked last.")]
        public FeatureRankingResult RankCategoricalFeaturesByChiSquare(
            [Description(
                "Categorical table rows: one array of text values per case, all the same length. Null or empty is its own category.")]
            string?[]?[]? data,
            [Description("The class label for each row (any integers), same length as the rows.")]
            int[] labels,
            [Description(
                "Optional name for each feature column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null)
        {
            if (data == null || data.Length == 0 || data.Any(r => r == null) || data[0]!.Length == 0 ||
                data.Any(r => r!.Length != data[0]!.Length))
            {
                throw new ArgumentException("Supply at least one row of text values, all rows the same length.",
                    nameof(data));
            }

            var rows = data.Select(r => r!.Select(c => c ?? string.Empty).ToArray()).ToArray();
            var columns = rows[0].Length;
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            DataPreparationGuard.Labels(labels, nameof(labels));
            DataPreparationGuard.SameLength(rows.Length, labels.Length, nameof(labels), "the table");

            var classes = labels.Distinct().OrderBy(c => c).ToArray();

            if (classes.Length < 2)
            {
                throw new ArgumentException("The labels contain a single class, so no feature can separate classes.",
                    nameof(labels));
            }

            var scores = new double[columns];
            var pValues = new double[columns];
            var effects = new double[columns];
            var degrees = new int[columns];

            for (var j = 0; j < columns; j++)
            {
                var categories = rows.Select(r => r[j]).Distinct(StringComparer.Ordinal).ToArray();

                if (categories.Length < 2)
                {
                    scores[j] = pValues[j] = effects[j] = double.NaN;
                    continue;
                }

                var statistic = 0d;

                foreach (var category in categories)
                {
                    var rowTotal = rows.Count(r => r[j] == category);

                    foreach (var c in classes)
                    {
                        var observed = Enumerable.Range(0, rows.Length)
                            .Count(i => labels[i] == c && rows[i][j] == category);

                        var expected = rowTotal * (double)labels.Count(l => l == c) / labels.Length;
                        statistic += (observed - expected) * (observed - expected) / expected;
                    }
                }

                degrees[j] = (categories.Length - 1) * (classes.Length - 1);
                scores[j] = statistic;
                pValues[j] = new ChiSquareTest(statistic, degrees[j]).PValue;
                effects[j] = Math.Sqrt(statistic / (rows.Length * (Math.Min(categories.Length, classes.Length) - 1)));
            }

            return Rank(names, scores, pValues, effects, degrees);
        }

        [AdvancedAnalyticsTool("RankFeaturesByMutualInformation")]
        [Description(
            "Ranks numeric features by mutual information with a categorical target: how many nats of uncertainty about the class the feature removes, after cutting the feature into equal-frequency bins. " +
            "Unlike correlation or ANOVA it detects any dependence, including non-linear and non-monotone. Scores are 0 for independence and grow with dependence; the normalised score divides by the target's entropy so 1 means the feature determines the class. " +
            "Missing values form their own bin, so informative missingness is captured. With small samples mutual information is biased upwards, more with more bins. Returns features best first with the score, the normalised score and the rank.")]
        public FeatureRankingResult RankFeaturesByMutualInformation(
            [Description(
                "Feature rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description("The class label for each row (any integers), same length as the rows.")]
            int[] labels,
            [Description(
                "Optional name for each feature column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("Number of equal-frequency bins per feature, at least 2. Default 10.")]
            int bins = 10)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            DataPreparationGuard.Labels(labels, nameof(labels));
            DataPreparationGuard.SameLength(data.Length, labels.Length, nameof(labels), "the table");

            if (bins < 2)
            {
                throw new ArgumentException("bins must be at least 2.", nameof(bins));
            }

            var n = (double)labels.Length;
            var classEntropy = -labels.GroupBy(l => l).Sum(g => g.Count() / n * Math.Log(g.Count() / n));
            var binner = new BinningTools();
            var scores = new double[columns];
            var effects = new double[columns];

            for (var j = 0; j < columns; j++)
            {
                var column = DataPreparationGuard.Column(data, j);

                if (column.All(DataPreparationGuard.IsMissing))
                {
                    scores[j] = effects[j] = double.NaN;
                    continue;
                }

                var assignments = binner.DiscretiseEqualFrequency(column, bins).Assignments;

                var joint = assignments.Select((a, i) => (a, labels[i])).GroupBy(x => x)
                    .ToDictionary(g => g.Key, g => g.Count());

                var marginalBin = assignments.GroupBy(a => a).ToDictionary(g => g.Key, g => g.Count());
                var marginalClass = labels.GroupBy(l => l).ToDictionary(g => g.Key, g => g.Count());

                scores[j] = joint.Sum(kv =>
                {
                    var pxy = kv.Value / n;

                    return pxy * Math.Log(pxy / (marginalBin[kv.Key.a] / n * (marginalClass[kv.Key.Item2] / n)));
                });

                scores[j] = Math.Max(0, scores[j]);
                effects[j] = classEntropy > 0 ? scores[j] / classEntropy : double.NaN;
            }

            return Rank(names, scores, null, effects);
        }

        [AdvancedAnalyticsTool("EliminateFeaturesRecursively")]
        [Description(
            "Recursive feature elimination with a linear model: standardise the columns, fit least squares to the numeric (or 0/1) target, drop the feature with the smallest absolute standardised coefficient, refit, and repeat until the requested number remain. " +
            "Because coefficients are refitted after each removal, it accounts for overlap between features that one-at-a-time rankings miss. Rows with any missing value are dropped. " +
            "Returns the selected feature names and, for every feature, its rank (1 for those kept, higher for those removed earlier, the last removed being 2) and its final standardised coefficient. Collinear features can make the fit fail: run FilterCorrelatedFeatures first.")]
        public RecursiveEliminationResult EliminateFeaturesRecursively(
            [Description(
                "Feature rows: one array of numbers per case, all the same length. Rows with NaN or infinity are dropped.")]
            double[][] data,
            [Description("The numeric or 0/1 target for each row, same length as the rows.")]
            double[] target,
            [Description("How many features to keep, at least 1 and below the number of columns.")]
            int featuresToKeep,
            [Description(
                "Optional name for each feature column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("How many features to remove per round, at least 1. Default 1.")]
            int step = 1)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            DataPreparationGuard.Vector(target, nameof(target));
            DataPreparationGuard.SameLength(data.Length, target.Length, nameof(target), "the table");

            if (featuresToKeep < 1 || featuresToKeep >= columns)
            {
                throw new ArgumentException($"featuresToKeep must be between 1 and {columns - 1}.",
                    nameof(featuresToKeep));
            }

            DataPreparationGuard.Positive(step, nameof(step));

            var rows = Enumerable.Range(0, data.Length)
                .Where(i => !DataPreparationGuard.IsMissing(target[i]) &&
                            data[i].All(v => !DataPreparationGuard.IsMissing(v)))
                .ToArray();

            if (rows.Length < columns + 2)
            {
                throw new ArgumentException(
                    $"Only {rows.Length} complete rows remain, which is too few to fit {columns} features. Impute or drop features first.",
                    nameof(data));
            }

            var standardised = new double[columns][];

            for (var j = 0; j < columns; j++)
            {
                var values = rows.Select(i => data[i][j]).ToArray();
                var mean = values.Average();
                var sd = Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / (values.Length - 1));
                standardised[j] = values.Select(v => sd > 0 ? (v - mean) / sd : 0).ToArray();
            }

            var y = rows.Select(i => target[i]).ToArray();
            var alive = Enumerable.Range(0, columns).ToList();
            var rank = new int[columns];
            var lastCoefficient = new double[columns];
            var removed = 0;

            while (alive.Count > featuresToKeep)
            {
                var coefficients = FitCoefficients(standardised, alive, y, nameof(data));

                for (var k = 0; k < alive.Count; k++)
                {
                    lastCoefficient[alive[k]] = coefficients[k];
                }

                var drop = Math.Min(step, alive.Count - featuresToKeep);

                var victims = Enumerable.Range(0, alive.Count).OrderBy(k => Math.Abs(coefficients[k]))
                    .ThenBy(k => alive[k])
                    .Take(drop).Select(k => alive[k]).ToArray();

                removed += victims.Length;

                foreach (var victim in victims)
                {
                    rank[victim] = -1;
                    alive.Remove(victim);
                }

                foreach (var victim in victims)
                {
                    rank[victim] = removed;
                }
            }

            var final = FitCoefficients(standardised, alive, y, nameof(data));

            for (var k = 0; k < alive.Count; k++)
            {
                lastCoefficient[alive[k]] = final[k];
                rank[alive[k]] = 1;
            }

            var eliminatedCount = removed;

            var rows2 = Enumerable.Range(0, columns).Select(j => new EliminationRow(names[j],
                alive.Contains(j) ? 1 : eliminatedCount - rank[j] + 2, lastCoefficient[j])).ToArray();

            return new RecursiveEliminationResult(alive.Select(j => names[j]).ToArray(), rows2);
        }

        private static double[] FitCoefficients(double[][] standardised, List<int> alive, double[] y,
            string parameterName)
        {
            var x = Enumerable.Range(0, y.Length).Select(i => alive.Select(j => standardised[j][i]).ToArray())
                .ToArray();

            try
            {
                var regression = new OrdinaryLeastSquares { UseIntercept = true }.Learn(x, y);
                var weights = regression.Weights;

                if (weights.Any(double.IsNaN))
                {
                    throw new InvalidOperationException("Not a number in the fit.");
                }

                return weights;
            }
            catch (Exception exception) when (exception is not ArgumentException)
            {
                throw new ArgumentException(
                    "The least squares fit failed, usually because features are perfectly collinear. Run FilterCorrelatedFeatures or SelectByVarianceThreshold first.",
                    parameterName,
                    exception);
            }
        }

        private static double PairCorrelation(double[] a, double[] b, CorrelationKind kind)
        {
            var idx = Enumerable.Range(0, a.Length)
                .Where(i => !DataPreparationGuard.IsMissing(a[i]) && !DataPreparationGuard.IsMissing(b[i])).ToArray();

            if (idx.Length < 3)
            {
                return double.NaN;
            }

            var x = idx.Select(i => a[i]).ToArray();
            var y = idx.Select(i => b[i]).ToArray();

            if (kind == CorrelationKind.Spearman)
            {
                x = AverageRanks(x);
                y = AverageRanks(y);
            }

            if (x.Distinct().Count() < 2 || y.Distinct().Count() < 2)
            {
                return double.NaN;
            }

            var matrix = idx.Select((_, k) => new[] { x[k], y[k] }).ToArray();

            return matrix.Correlation()[0][1];
        }

        private static double[] AverageRanks(double[] values)
        {
            var order = Enumerable.Range(0, values.Length).OrderBy(i => values[i]).ToArray();
            var ranks = new double[values.Length];
            var start = 0;

            while (start < order.Length)
            {
                var end = start;

                while (end + 1 < order.Length && Math.Abs(values[order[end + 1]] - values[order[start]]) < 0.0001)
                {
                    end++;
                }

                for (var k = start; k <= end; k++)
                {
                    ranks[order[k]] = (start + end) / 2d + 1;
                }

                start = end + 1;
            }

            return ranks;
        }

        private static FeatureRankingResult Rank(string[] names, double[] scores, double[]? pValues,
            double[]? effects = null, int[]? degrees = null)
        {
            var adjusted = pValues == null ? null : BenjaminiHochberg(pValues);
            var key = effects ?? scores;

            var ordered = Enumerable.Range(0, names.Length)
                .OrderBy(j => double.IsNaN(key[j]) ? 1 : 0)
                .ThenByDescending(j => double.IsNaN(key[j]) ? 0 : key[j])
                .ThenBy(j => j)
                .ToArray();

            var rows = ordered.Select((j, r) => new FeatureScore(names[j], scores[j],
                pValues?[j] ?? double.NaN, adjusted?[j] ?? double.NaN, effects?[j] ?? double.NaN,
                degrees?[j] ?? 0, r + 1)).ToArray();

            return new FeatureRankingResult(rows);
        }

        private static double[] BenjaminiHochberg(double[] pValues)
        {
            var order = Enumerable.Range(0, pValues.Length).Where(i => !double.IsNaN(pValues[i]))
                .OrderBy(i => pValues[i]).ToArray();

            var adjusted = Enumerable.Repeat(double.NaN, pValues.Length).ToArray();
            var m = order.Length;
            var running = 1d;

            for (var k = m - 1; k >= 0; k--)
            {
                running = Math.Min(running, pValues[order[k]] * m / (k + 1));
                adjusted[order[k]] = running;
            }

            return adjusted;
        }
    }

    public sealed record VarianceSelectionResult(
        // ReSharper disable once NotAccessedPositionalProperty.Global
        string[] ColumnNames,
        double[] Variances,
        int[] FiniteCounts,
        string[] Kept,
        string[] Dropped);

    public sealed record CorrelatedPair(string First, string Second, double Correlation);

    public sealed record DroppedFeature(string Name, string CorrelatedWith, double Correlation);

    public sealed record CorrelationFilterResult(CorrelatedPair[] Pairs, DroppedFeature[] Dropped, string[] Kept);

    public sealed record FeatureScore(
        string Name,
        double Score,
        double PValue,
        double AdjustedPValue,
        double EffectSize,
        int DegreesOfFreedom,
        int Rank);

    public sealed record FeatureRankingResult(FeatureScore[] Features);

    public sealed record EliminationRow(string Name, int Rank, double StandardisedCoefficient);

    public sealed record RecursiveEliminationResult(string[] Selected, EliminationRow[] Features);
}
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
using Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    [AdvancedAnalyticsToolGroup("CategoricalAndSurvival")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class ContingencyAssociationTools
    {
        private const int MaximumOrdinalCells = 10000;

        [AdvancedAnalyticsTool("CrossTabulateCategories")]
        [Description(
            "Builds a contingency table (cross-tabulation) from two parallel lists of category labels, one pair per row. " +
            "Returns the sorted row labels, sorted column labels, the count in every cell, both sets of margins and the number of pairs skipped because either label was empty. " +
            "Use it to turn raw categorical columns into the count table that the association tools expect.")]
        public CrossTabulationResult CrossTabulateCategories(
            [Description("Category of the row variable for each case; an empty or blank label skips that case.")]
            string[] rowCategories,
            [Description(
                "Category of the column variable for each case, in the same order and of the same length as rowCategories.")]
            string[] columnCategories)
        {
            CategoricalGuards.Required(rowCategories, nameof(rowCategories));
            CategoricalGuards.Required(columnCategories, nameof(columnCategories));

            CategoricalGuards.SameLength(rowCategories.Length, columnCategories.Length, nameof(rowCategories),
                nameof(columnCategories));

            var pairs = new List<(string Row, string Column)>();
            var skipped = 0;

            for (var i = 0; i < rowCategories.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(rowCategories[i]) || string.IsNullOrWhiteSpace(columnCategories[i]))
                {
                    skipped++;
                    continue;
                }

                pairs.Add((rowCategories[i], columnCategories[i]));
            }

            if (pairs.Count == 0)
            {
                throw new ArgumentException(
                    "There are no cases with both a row and a column category; check that the label lists are populated.",
                    nameof(rowCategories));
            }

            var rowLabels = pairs.Select(p => p.Row).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();
            var columnLabels = pairs.Select(p => p.Column).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();
            var counts = new int[rowLabels.Length][];

            for (var i = 0; i < counts.Length; i++)
            {
                counts[i] = new int[columnLabels.Length];
            }

            var rowIndex = rowLabels.Select((label, index) => (label, index)).ToDictionary(t => t.label, t => t.index);
            var columnIndex = columnLabels.Select((label, index) => (label, index))
                .ToDictionary(t => t.label, t => t.index);

            foreach (var pair in pairs)
            {
                counts[rowIndex[pair.Row]][columnIndex[pair.Column]]++;
            }

            return new CrossTabulationResult
            {
                RowLabels = rowLabels,
                ColumnLabels = columnLabels,
                Counts = counts,
                RowTotals = counts.Select(r => r.Sum()).ToArray(),
                ColumnTotals = Enumerable.Range(0, columnLabels.Length).Select(j => counts.Sum(r => r[j])).ToArray(),
                Total = pairs.Count,
                SkippedPairs = skipped
            };
        }

        [AdvancedAnalyticsTool("AnalyseContingencyTableAssociation")]
        [Description(
            "Tests and measures the association between two categorical variables from a table of counts (rows by columns, at least 2 by 2). " +
            "Returns Pearson chi-square with degrees of freedom and p-value (small p means the variables are not independent), the likelihood-ratio G statistic, a Yates continuity-corrected chi-square for 2 by 2 tables, the expected counts, Pearson and adjusted standardised residuals (cells with an absolute adjusted residual above about 2 drive the association) and the effect sizes Cramer V, bias-corrected Cramer V, phi, the contingency coefficient and Tschuprow T. " +
            "The warnings list says when expected counts are too small for the chi-square approximation. A row or column with no observations is rejected.")]
        public ContingencyAssociationResult AnalyseContingencyTableAssociation(
            [Description(
                "Counts with one array per row category; every row must have the same number of columns and counts must be finite and not negative.")]
            double[][] counts,
            [Description(
                "Optional names of the row categories, in row order; leave empty to label them Row1, Row2 and so on.")]
            string[]? rowLabels = null,
            [Description(
                "Optional names of the column categories, in column order; leave empty to label them Column1, Column2 and so on.")]
            string[]? columnLabels = null)
        {
            var table = CategoricalGuards.Table(counts, nameof(counts));

            return ContingencyMath.Analyse(table,
                CategoricalGuards.Names(rowLabels, table.Length, "Row", nameof(rowLabels)),
                CategoricalGuards.Names(columnLabels, table[0].Length, "Column", nameof(columnLabels)));
        }

        [AdvancedAnalyticsTool("AnalyseCategoricalAssociationFromLabels")]
        [Description(
            "Cross-tabulates two categorical columns given as parallel label lists and then runs the full contingency association analysis on the result: chi-square test, G test, residuals, Cramer V, phi, contingency coefficient and Tschuprow T. " +
            "Use it when you have raw labelled cases (for example channel and confirmed-fraud flag) rather than a prepared count table. Cases with an empty label on either side are dropped. " +
            "Read the adjusted residuals to see which specific category pairs occur more (positive) or less (negative) often than independence predicts.")]
        public ContingencyAssociationResult AnalyseCategoricalAssociationFromLabels(
            [Description("Category of the row variable for each case; blank labels skip the case.")]
            string[] rowCategories,
            [Description("Category of the column variable for each case, same order and length as rowCategories.")]
            string[] columnCategories)
        {
            var crossTab = CrossTabulateCategories(rowCategories, columnCategories);
            var table = crossTab.Counts.Select(r => r.Select(c => (double)c).ToArray()).ToArray();

            if (table.Length < 2 || table[0].Length < 2)
            {
                throw new ArgumentException(
                    "Association needs at least two distinct categories in each variable; one of the variables is constant after removing blank labels.",
                    nameof(rowCategories));
            }

            return ContingencyMath.Analyse(table, crossTab.RowLabels, crossTab.ColumnLabels);
        }

        [AdvancedAnalyticsTool("CalculateGoodmanKruskalLambda")]
        [Description(
            "Computes Goodman and Kruskal's lambda for a contingency table: the proportional reduction in prediction error for one categorical variable when the other is known. " +
            "LambdaColumnGivenRow is the improvement in guessing the column category by knowing the row category, LambdaRowGivenColumn is the reverse, and LambdaSymmetric averages them; 0 means no help and 1 means perfect prediction. " +
            "Standard errors are the asymptotic ones. NaN means the predicted variable is concentrated in a single category so there is no error to reduce. Lambda can be 0 even when the variables are associated, because it only credits changes to the most common category.")]
        public GoodmanKruskalLambdaResult CalculateGoodmanKruskalLambda(
            [Description("Counts with one array per row category; all rows the same length, finite and not negative.")]
            double[][] counts)
        {
            return ContingencyMath.Lambda(CategoricalGuards.Table(counts, nameof(counts)));
        }

        [AdvancedAnalyticsTool("CalculateOrdinalAssociationMeasures")]
        [Description(
            "Measures the strength and direction of association between two ordered categorical variables from a table of counts whose rows and columns are in increasing order. " +
            "Returns concordant, discordant and tied pair counts, Goodman-Kruskal gamma with asymptotic standard error and confidence interval, Kendall tau-b, Stuart tau-c and Somers d in both directions and symmetric. " +
            "Values run from -1 (perfectly reversed order) through 0 (no monotone association) to +1. Gamma ignores ties and so is the largest in magnitude; tau-b and Somers d penalise ties. Rows and columns must be supplied in their natural order, otherwise the sign and size are meaningless.")]
        public OrdinalAssociationResult CalculateOrdinalAssociationMeasures(
            [Description(
                "Counts with one array per ordered row category, and columns in ordered category order; finite and not negative.")]
            double[][] counts,
            [Description("Confidence level for the gamma interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            var table = CategoricalGuards.Table(counts, nameof(counts));

            if ((long)table.Length * table[0].Length > MaximumOrdinalCells)
            {
                throw new ArgumentException(
                    $"The table has more than {MaximumOrdinalCells} cells; collapse categories before measuring ordinal association.",
                    nameof(counts));
            }

            return ContingencyMath.Ordinal(table, confidenceLevel);
        }

        [AdvancedAnalyticsTool("CalculateCategoricalMutualInformation")]
        [Description(
            "Quantifies the dependence between two categorical variables with information theory from a table of counts. " +
            "Returns the entropy of each variable, the joint entropy, the mutual information (how much knowing one reduces uncertainty about the other, 0 when independent), a normalised version between 0 and 1, and Theil's uncertainty coefficients in each direction and symmetric. " +
            "Unlike chi-square based measures it does not assume any category order and it captures any kind of dependence. NaN coefficients mean a variable has a single category and therefore no entropy.")]
        public InformationTheoryAssociationResult CalculateCategoricalMutualInformation(
            [Description("Counts with one array per row category; all rows the same length, finite and not negative.")]
            double[][] counts,
            [Description("True to report entropies in bits (base 2, the default); false reports natural units (nats).")]
            bool useBits = true)
        {
            return ContingencyMath.InformationTheory(CategoricalGuards.Table(counts, nameof(counts)), useBits);
        }

        [AdvancedAnalyticsTool("RankCategoricalColumnsByAssociationWithOutcome")]
        [Description(
            "Screens many categorical columns at once for how strongly each is associated with a binary outcome, and ranks them from strongest to weakest. " +
            "For every column it cross-tabulates the categories against the outcome and returns the chi-square test with its p-value and a Benjamini-Hochberg adjusted p-value (controls false discoveries across the many columns), Cramer V and its bias-corrected form (effect size from 0 to 1), the information value (weight-of-evidence separation) and the mutual information in bits. " +
            "Rank primarily by CramersVBiasCorrected or InformationValue, and treat a column as real only if the adjusted p-value is small. A column that is constant, or an outcome with a single class, gets NaN statistics and an explanatory Note. Blank category values are skipped per column.")]
        public CategoricalColumnRankingResult RankCategoricalColumnsByAssociationWithOutcome(
            [Description(
                "One array of category labels per case, all the same length and in the same order as columnNames; a blank label means missing for that column.")]
            string[][] rows,
            [Description("The name of each categorical column, unique and in column order.")]
            string[] columnNames,
            [Description(
                "Outcome for each case in the same order as rows: 1 for the event of interest and 0 otherwise.")]
            int[] labels)
        {
            CategoricalGuards.Required(rows, nameof(rows));
            CategoricalGuards.Required(columnNames, nameof(columnNames));
            CategoricalGuards.BinaryLabels(labels, nameof(labels));
            CategoricalGuards.SameLength(rows.Length, labels.Length, nameof(rows), nameof(labels));

            if (columnNames.Length == 0 || columnNames.Distinct().Count() != columnNames.Length)
            {
                throw new ArgumentException("Give at least one column name and keep the names unique.",
                    nameof(columnNames));
            }

            for (var i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null || rows[i].Length != columnNames.Length)
                {
                    throw new ArgumentException(
                        $"Row {i} of rows must have {columnNames.Length} entries, one per column name.", nameof(rows));
                }
            }

            var results = new List<CategoricalColumnAssociationRow>();
            var pValues = new List<(int Index, double P)>();

            for (var c = 0; c < columnNames.Length; c++)
            {
                var usable = Enumerable.Range(0, rows.Length).Where(i => !string.IsNullOrWhiteSpace(rows[i][c]))
                    .ToArray();

                var categories = usable.Select(i => rows[i][c]).Distinct().OrderBy(v => v, StringComparer.Ordinal)
                    .ToArray();

                var eventCount = usable.Sum(i => labels[i]);

                if (categories.Length < 2 || eventCount == 0 || eventCount == usable.Length)
                {
                    results.Add(new CategoricalColumnAssociationRow
                    {
                        Column = columnNames[c],
                        Categories = categories.Length,
                        UsableRows = usable.Length,
                        ChiSquare = double.NaN,
                        PValue = double.NaN,
                        AdjustedPValue = double.NaN,
                        CramersV = double.NaN,
                        CramersVBiasCorrected = double.NaN,
                        InformationValue = double.NaN,
                        MutualInformationBits = double.NaN,
                        Note = categories.Length < 2
                            ? "the column has fewer than two categories among the usable rows"
                            : "the outcome has a single class among the usable rows"
                    });

                    continue;
                }

                var index = categories.Select((name, position) => (name, position))
                    .ToDictionary(t => t.name, t => t.position);

                var table = categories.Select(_ => new double[2]).ToArray();
                var eventCounts = new int[categories.Length];
                var nonEventCounts = new int[categories.Length];

                foreach (var i in usable)
                {
                    table[index[rows[i][c]]][labels[i]]++;

                    if (labels[i] == 1)
                    {
                        eventCounts[index[rows[i][c]]]++;
                    }
                    else
                    {
                        nonEventCounts[index[rows[i][c]]]++;
                    }
                }

                var association = ContingencyMath.Analyse(table, categories, ["NonEvent", "Event"]);

                var value = BinningMath.InformationValue(categories, categories.Select(_ => double.NaN).ToArray(),
                    categories.Select(_ => double.NaN).ToArray(), eventCounts, nonEventCounts, 0.5, 0);

                var information = ContingencyMath.InformationTheory(table, true);
                pValues.Add((results.Count, association.PValue));

                results.Add(new CategoricalColumnAssociationRow
                {
                    Column = columnNames[c],
                    Categories = categories.Length,
                    UsableRows = usable.Length,
                    ChiSquare = association.ChiSquare,
                    DegreesOfFreedom = association.DegreesOfFreedom,
                    PValue = association.PValue,
                    AdjustedPValue = double.NaN,
                    CramersV = association.CramersV,
                    CramersVBiasCorrected = association.CramersVBiasCorrected,
                    InformationValue = value.TotalInformationValue,
                    MutualInformationBits = information.MutualInformation,
                    Note = association.Warnings.Length > 0 ? association.Warnings[0] : string.Empty
                });
            }

            var ordered = pValues.OrderBy(p => p.P).ToArray();
            var adjusted = new double[results.Count];
            var running = 1d;

            for (var k = ordered.Length - 1; k >= 0; k--)
            {
                running = Math.Min(running, ordered[k].P * ordered.Length / (k + 1d));
                adjusted[ordered[k].Index] = running;
            }

            var final = results.Select((row, position) => new CategoricalColumnAssociationRow
                {
                    Column = row.Column,
                    Categories = row.Categories,
                    UsableRows = row.UsableRows,
                    ChiSquare = row.ChiSquare,
                    DegreesOfFreedom = row.DegreesOfFreedom,
                    PValue = row.PValue,
                    AdjustedPValue = double.IsNaN(row.PValue) ? double.NaN : adjusted[position],
                    CramersV = row.CramersV,
                    CramersVBiasCorrected = row.CramersVBiasCorrected,
                    InformationValue = row.InformationValue,
                    MutualInformationBits = row.MutualInformationBits,
                    Note = row.Note
                }).OrderByDescending(r => double.IsNaN(r.CramersV) ? -1d : r.CramersV)
                .ThenBy(r => r.Column, StringComparer.Ordinal).ToArray();

            return new CategoricalColumnRankingResult
            {
                Columns = final,
                TotalRows = rows.Length,
                TotalEvents = labels.Sum()
            };
        }
    }
}
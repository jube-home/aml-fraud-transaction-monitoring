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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling
{
    [AdvancedAnalyticsToolGroup("DataPreparationAndResampling")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class TableShapingTools
    {
        [AdvancedAnalyticsTool("FilterTableRows")]
        [Description(
            "Returns a table containing only the given row indices, in the order supplied, compacting the table down to those rows. " +
            "Use it after any tool that identifies rows to keep or drop (duplicate detection, sparse-row detection, sampling) to turn the identified indices into the smaller table itself. " +
            "Every other row-shaping tool in this family builds on this one.")]
        public FeatureTableResult FilterTableRows(
            [Description("Table rows: one array of numbers per case, all the same length.")]
            double[][] data,
            [Description("The name of every column of the table, in column order.")]
            string[] columnNames,
            [Description(
                "Zero based row indices to keep, in the desired output order. May reference a row more than once.")]
            int[] keepRowIndices)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));

            if (keepRowIndices == null || keepRowIndices.Length == 0)
            {
                throw new ArgumentException("Name at least one row index to keep.", nameof(keepRowIndices));
            }

            var rows = data.Length;

            foreach (var index in keepRowIndices)
            {
                if (index < 0 || index >= rows)
                {
                    throw new ArgumentException(
                        $"Row index {index} is outside the table, which has {rows} row(s).", nameof(keepRowIndices));
                }
            }

            return new FeatureTableResult(names, keepRowIndices.Select(i => data[i]).ToArray());
        }

        [AdvancedAnalyticsTool("DistinctTableRows")]
        [Description(
            "Drops rows that exactly repeat an earlier row (two missing cells count as equal), keeping the first occurrence of each. " +
            "Use it after FindDuplicateRows to actually remove the repeats rather than only report them.")]
        public FeatureTableResult DistinctTableRows(
            [Description("Table rows: one array of numbers per case, all the same length. NaN counts as equal to NaN.")]
            double[][] data,
            [Description("The name of every column of the table, in column order.")]
            string[] columnNames)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));

            var duplicates = new DataQualityTools().FindDuplicateRows(data);

            return FilterTableRows(data, names, duplicates.RowsToKeep);
        }

        [AdvancedAnalyticsTool("ArrangeTableRows")]
        [Description(
            "Sorts the whole table by one or more columns at once, each ascending or descending, using a stable sort so rows that tie on every sort column keep their original relative order. " +
            "Use it to order a table by a primary key and one or more tie-breaking columns before inspection or export.")]
        public FeatureTableResult ArrangeTableRows(
            [Description("Table rows: one array of numbers per case, all the same length.")]
            double[][] data,
            [Description("The name of every column of the table, in column order.")]
            string[] columnNames,
            [Description("Zero based column indices to sort by, most significant first.")]
            int[] sortColumnIndices,
            [Description("One direction per sort column, in the same order: true sorts that column largest first.")]
            bool[] descending)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));

            if (sortColumnIndices == null || sortColumnIndices.Length == 0)
            {
                throw new ArgumentException("Name at least one column to sort by.", nameof(sortColumnIndices));
            }

            if (descending == null || descending.Length != sortColumnIndices.Length)
            {
                throw new ArgumentException(
                    "descending must have exactly one value per entry in sortColumnIndices.", nameof(descending));
            }

            foreach (var index in sortColumnIndices)
            {
                if (index < 0 || index >= columns)
                {
                    throw new ArgumentException(
                        $"Column index {index} is outside the table, which has {columns} column(s).",
                        nameof(sortColumnIndices));
                }
            }

            var rows = Enumerable.Range(0, data.Length);
            var firstColumn = sortColumnIndices[0];

            var ordered = descending[0]
                ? rows.OrderByDescending(i => data[i][firstColumn])
                : rows.OrderBy(i => data[i][firstColumn]);

            for (var key = 1; key < sortColumnIndices.Length; key++)
            {
                var column = sortColumnIndices[key];

                ordered = descending[key]
                    ? ordered.ThenByDescending(i => data[i][column])
                    : ordered.ThenBy(i => data[i][column]);
            }

            return FilterTableRows(data, names, ordered.ToArray());
        }

        [AdvancedAnalyticsTool("RelocateTableColumns")]
        [Description(
            "Moves one or more named columns to a new position, right after a given column, or to the front when no target column is given, leaving every other column where it is and in its own relative order. " +
            "Use it to reorder columns without having to name every column the way SelectTableColumns requires.")]
        public FeatureTableResult RelocateTableColumns(
            [Description("Table rows: one array of numbers per case, all the same length.")]
            double[][] data,
            [Description("The name of every column of the table, in column order.")]
            string[] columnNames,
            [Description("The names of the columns to move, in the order they should appear at their new position.")]
            string[] columnsToMove,
            [Description(
                "The column the moved columns are placed immediately after. Leave empty to move them to the front.")]
            string? after = null)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));

            if (columnsToMove == null || columnsToMove.Length == 0)
            {
                throw new ArgumentException("Name at least one column to relocate.", nameof(columnsToMove));
            }

            var moveSet = new HashSet<string>(columnsToMove, StringComparer.Ordinal);

            if (moveSet.Count != columnsToMove.Length)
            {
                throw new ArgumentException("A column name is repeated in columnsToMove.", nameof(columnsToMove));
            }

            foreach (var name in columnsToMove)
            {
                DataPreparationGuard.ColumnIndex(names, name, nameof(columnsToMove));
            }

            var remaining = names.Where(n => !moveSet.Contains(n)).ToList();
            List<string> order;

            if (after == null)
            {
                order = columnsToMove.Concat(remaining).ToList();
            }
            else
            {
                if (moveSet.Contains(after))
                {
                    throw new ArgumentException(
                        "The 'after' column cannot also be one of the columns being relocated.", nameof(after));
                }

                DataPreparationGuard.ColumnIndex(names, after, nameof(after));
                var insertAt = remaining.IndexOf(after) + 1;
                order = remaining.Take(insertAt).Concat(columnsToMove).Concat(remaining.Skip(insertAt)).ToList();
            }

            return DataPreparationGuard.SelectColumns(data, names, order.ToArray(), nameof(columnsToMove));
        }
    }
}
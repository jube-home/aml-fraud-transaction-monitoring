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

using System;
using System.Linq;
using FluentAssertions;
using Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class TableShapingToolsTests
    {
        private const double Nan = double.NaN;
        private static readonly TableShapingTools shaping = new();
        private static readonly string[] names = ["a", "b", "c"];

        private static double[][] Table(params double[][] rows)
        {
            return rows;
        }

        [Fact]
        [Trait("Covers", "FilterTableRows")]
        public void FilterTableRowsKeepsGivenIndicesInGivenOrder()
        {
            var table = Table(
                [1.0, 10.0, 100.0],
                [2.0, 20.0, 200.0],
                [3.0, 30.0, 300.0]);

            var result = shaping.FilterTableRows(table, names, [2, 0]);

            result.ColumnNames.Should().Equal(names);
            result.Table.Should().HaveCount(2);
            result.Table[0].Should().Equal(3.0, 30.0, 300.0);
            result.Table[1].Should().Equal(1.0, 10.0, 100.0);
        }

        [Fact]
        [Trait("Covers", "FilterTableRows")]
        public void FilterTableRowsOnSingleRowTableKeepsThatRow()
        {
            var table = Table([1.0, 2.0, 3.0]);

            var result = shaping.FilterTableRows(table, names, [0]);

            result.Table.Should().HaveCount(1);
            result.Table[0].Should().Equal(1.0, 2.0, 3.0);
        }

        [Fact]
        [Trait("Covers", "FilterTableRows")]
        public void FilterTableRowsRejectsEmptyTable()
        {
            Action act = () => shaping.FilterTableRows([], names, [0]);
            act.Should().Throw<ArgumentException>().WithParameterName("data");
        }

        [Fact]
        [Trait("Covers", "FilterTableRows")]
        public void FilterTableRowsRejectsOutOfRangeIndex()
        {
            var table = Table([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]);

            Action act = () => shaping.FilterTableRows(table, names, [0, 5]);
            act.Should().Throw<ArgumentException>().WithParameterName("keepRowIndices");
        }

        [Fact]
        [Trait("Covers", "FilterTableRows")]
        public void FilterTableRowsRejectsNegativeIndex()
        {
            var table = Table([1.0, 2.0, 3.0]);

            Action act = () => shaping.FilterTableRows(table, names, [-1]);
            act.Should().Throw<ArgumentException>().WithParameterName("keepRowIndices");
        }

        [Fact]
        [Trait("Covers", "DistinctTableRows")]
        public void DistinctTableRowsKeepsAllRowsWhenAllDistinct()
        {
            var table = Table(
                [1.0, 2.0, 3.0],
                [4.0, 5.0, 6.0],
                [7.0, 8.0, 9.0]);

            var result = shaping.DistinctTableRows(table, names);

            result.Table.Should().HaveCount(3);
            result.Table.Should().Equal(table);
        }

        [Fact]
        [Trait("Covers", "DistinctTableRows")]
        public void DistinctTableRowsCollapsesAllDuplicatesToOneRow()
        {
            var table = Table(
                [1.0, 2.0, Nan],
                [1.0, 2.0, Nan],
                [1.0, 2.0, Nan]);

            var result = shaping.DistinctTableRows(table, names);

            result.Table.Should().HaveCount(1);
            result.Table[0][0].Should().Be(1.0);
            result.Table[0][1].Should().Be(2.0);
            double.IsNaN(result.Table[0][2]).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "DistinctTableRows")]
        public void DistinctTableRowsRejectsOutOfRangeTable()
        {
            Action act = () => shaping.DistinctTableRows([[1.0], [1.0, 2.0]], names);
            act.Should().Throw<ArgumentException>().WithParameterName("data");
        }

        [Fact]
        [Trait("Covers", "ArrangeTableRows")]
        public void ArrangeTableRowsSortsByMultipleKeysWithMixedDirections()
        {
            var table = Table(
                [1.0, 30.0],
                [2.0, 10.0],
                [1.0, 10.0],
                [2.0, 20.0]);

            var result = shaping.ArrangeTableRows(table, ["group", "value"],
                [0, 1], [false, true]);

            result.Table.Select(r => (r[0], r[1])).Should().Equal(
                (1.0, 30.0), (1.0, 10.0), (2.0, 20.0), (2.0, 10.0));
        }

        [Fact]
        [Trait("Covers", "ArrangeTableRows")]
        public void ArrangeTableRowsIsStableWhenEveryKeyTies()
        {
            var table = Table(
                [1.0, 100.0],
                [1.0, 200.0],
                [1.0, 300.0]);

            var result = shaping.ArrangeTableRows(table, ["group", "marker"],
                [0], [false]);

            result.Table.Select(r => r[1]).Should().Equal(100.0, 200.0, 300.0);
        }

        [Fact]
        [Trait("Covers", "ArrangeTableRows")]
        public void ArrangeTableRowsRejectsMismatchedDirectionsLength()
        {
            var table = Table([1.0, 2.0]);

            Action act = () => shaping.ArrangeTableRows(table, ["a", "b"], [0, 1], [true]);
            act.Should().Throw<ArgumentException>().WithParameterName("descending");
        }

        [Fact]
        [Trait("Covers", "ArrangeTableRows")]
        public void ArrangeTableRowsRejectsOutOfRangeColumnIndex()
        {
            var table = Table([1.0, 2.0]);

            Action act = () => shaping.ArrangeTableRows(table, ["a", "b"], [5], [false]);
            act.Should().Throw<ArgumentException>().WithParameterName("sortColumnIndices");
        }

        [Fact]
        [Trait("Covers", "RelocateTableColumns")]
        public void RelocateTableColumnsMovesToFrontWhenAfterIsNull()
        {
            var table = Table([1.0, 2.0, 3.0]);

            var result = shaping.RelocateTableColumns(table, names, ["c"]);

            result.ColumnNames.Should().Equal("c", "a", "b");
            result.Table[0].Should().Equal(3.0, 1.0, 2.0);
        }

        [Fact]
        [Trait("Covers", "RelocateTableColumns")]
        public void RelocateTableColumnsMovesToEndWhenAfterIsLastRemainingColumn()
        {
            var table = Table([1.0, 2.0, 3.0]);

            var result = shaping.RelocateTableColumns(table, names, ["a"], "c");

            result.ColumnNames.Should().Equal("b", "c", "a");
            result.Table[0].Should().Equal(2.0, 3.0, 1.0);
        }

        [Fact]
        [Trait("Covers", "RelocateTableColumns")]
        public void RelocateTableColumnsMovesIntoTheMiddle()
        {
            var table = Table([1.0, 2.0, 3.0, 4.0]);
            var names = new[] { "a", "b", "c", "d" };

            var result = shaping.RelocateTableColumns(table, names, ["d"], "b");

            result.ColumnNames.Should().Equal("a", "b", "d", "c");
            result.Table[0].Should().Equal(1.0, 2.0, 4.0, 3.0);
        }

        [Fact]
        [Trait("Covers", "RelocateTableColumns")]
        public void RelocateTableColumnsRejectsUnknownColumnName()
        {
            var table = Table([1.0, 2.0, 3.0]);

            Action act = () => shaping.RelocateTableColumns(table, names, ["z"]);
            act.Should().Throw<ArgumentException>().WithParameterName("columnsToMove");
        }

        [Fact]
        [Trait("Covers", "RelocateTableColumns")]
        public void RelocateTableColumnsRejectsAfterBeingOneOfTheMovedColumns()
        {
            var table = Table([1.0, 2.0, 3.0]);

            Action act = () => shaping.RelocateTableColumns(table, names, ["a"], "a");
            act.Should().Throw<ArgumentException>().WithParameterName("after");
        }
    }
}
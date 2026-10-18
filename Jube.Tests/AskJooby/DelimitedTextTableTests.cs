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

using System.Linq;
using FluentAssertions;
using Jube.AskJooby.DataCache;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class DelimitedTextTableTests
    {
        [Fact]
        public void AHeaderAndRowsBecomeColumnsAndRows()
        {
            DelimitedTextTable.TryParse("Account,Amount\nA1,100\nA2,250.5\n", out var table).Should().BeTrue();

            table.Columns.Select(column => column.Name).Should().Equal("Account", "Amount");
            table.Rows.Should().HaveCount(2);
            table.Rows[1]["Account"].Should().Be("A2");

            table.Rows[1]["Amount"].Should().Be(250.5,
                "a numeric column kept as text cannot be compared properly by the cache tools' filtering");
        }

        [Fact]
        public void ADelimiterInsideAQuotedFieldDoesNotStartANewColumn()
        {
            DelimitedTextTable.TryParse("Id,Description\n1,\"Cash, in branch\"\n", out var table)
                .Should().BeTrue();

            table.Rows[0]["Description"].Should().Be("Cash, in branch",
                "a naive split silently misaligns every row after the first with a comma in a description");
        }

        [Fact]
        public void ADoubledQuoteIsOneQuoteAndANewlineInsideQuotesStaysInTheField()
        {
            DelimitedTextTable.TryParse("Id,Note\n1,\"He said \"\"no\"\"\nthen left\"\n2,plain\n", out var table)
                .Should().BeTrue();

            table.Rows.Should().HaveCount(2, "the newline inside quotes is part of the field, not a row break");
            table.Rows[0]["Note"].Should().Be("He said \"no\"\nthen left");
        }

        [Fact]
        public void TabsAndSemicolonsAreRecognisedAsDelimitersToo()
        {
            DelimitedTextTable.TryParse("Id\tName\n1\tOne\n", out var tabbed).Should().BeTrue();
            tabbed.Columns.Select(column => column.Name).Should().Equal("Id", "Name");

            DelimitedTextTable.TryParse("Id;Name\n1;One\n", out var semi).Should().BeTrue();
            semi.Columns.Select(column => column.Name).Should().Equal("Id", "Name");
        }

        [Fact]
        public void AShortRowIsPaddedRatherThanRejected()
        {
            DelimitedTextTable.TryParse("A,B,C\n1,2\n", out var table).Should().BeTrue();

            table.Rows[0]["C"].Should().BeNull(
                "a trailing empty field is frequently omitted entirely by whatever produced the file");
        }

        [Fact]
        public void DuplicateHeaderNamesAreMadeUniqueSoNoColumnIsLost()
        {
            DelimitedTextTable.TryParse("Name,Name\nx,y\n", out var table).Should().BeTrue();

            table.Columns.Select(column => column.Name).Should().Equal("Name", "Name_2");
            table.Rows[0]["Name_2"].Should().Be("y");
        }

        [Fact]
        public void ProseIsNotMistakenForATable()
        {
            DelimitedTextTable.TryParse("This is a sentence, with a comma.", out _).Should().BeFalse(
                "a single record is a header with no data, and prose would otherwise parse as a table");

            DelimitedTextTable.TryParse("just one column\nand another line\n", out _).Should().BeFalse();
            DelimitedTextTable.TryParse("", out _).Should().BeFalse();
        }
    }
}
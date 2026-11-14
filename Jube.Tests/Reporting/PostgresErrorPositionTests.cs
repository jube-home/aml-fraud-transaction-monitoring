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


using FluentAssertions;
using Jube.Data.Reporting;
using Xunit;

namespace Jube.Test.Reporting
{
    [Trait("Category", "Unit")]
    public sealed class PostgresErrorPositionTests
    {
        [Fact]
        public void APositionInAnUnchangedStatementIsKept()
        {
            PostgresErrorPosition.InOriginal("select nope from t", "select nope from t", 8).Should().Be(8);
        }

        [Fact]
        public void APositionBeforeARewrittenTableIsKept()
        {
            PostgresErrorPosition.InOriginal("select nope from \"Cases\"", "select nope from \"public\".\"Cases\"", 8)
                .Should().Be(8);
        }

        [Fact]
        public void APositionAfterARewrittenTableIsShiftedBackByTheLengthTheRewriteAdded()
        {
            const string original = "select a from \"Cases\" where nope = 1";
            const string submitted = "select a from \"public\".\"Cases\" where nope = 1";

            var inSubmitted = submitted.IndexOf("nope", System.StringComparison.Ordinal) + 1;

            PostgresErrorPosition.InOriginal(original, submitted, inSubmitted).Should()
                .Be(original.IndexOf("nope", System.StringComparison.Ordinal) + 1);
        }

        [Fact]
        public void APositionInsideTheRewrittenTableIsNotGuessed()
        {
            PostgresErrorPosition.InOriginal("select a from \"Cases\"", "select a from \"public\".\"Cases\"", 17)
                .Should().BeNull();
        }

        [Theory]
        [InlineData(null, "x", 1)]
        [InlineData("x", null, 1)]
        [InlineData("x", "x", 0)]
        [InlineData("x", "x", 2)]
        public void NothingUsableGivesNoPosition(string? original, string? submitted, int position)
        {
            PostgresErrorPosition.InOriginal(original!, submitted!, position).Should().BeNull();
        }

        [Theory]
        [InlineData("select 1", 8, 1, 8)]
        [InlineData("select 1,\n  nope", 13, 2, 3)]
        [InlineData("a\nb\nc", 5, 3, 1)]
        public void ALineAndColumnAreCountedFromOne(string text, int position, int line, int column)
        {
            PostgresErrorPosition.LineAndColumn(text, position).Should().Be((line, column));
        }
    }
}

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
using System.Text.Json;
using FluentAssertions;
using Jube.Data.Reporting;
using Xunit;
using FieldQuery = Jube.Data.Query.GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery;

namespace Jube.Test.Service.Query.Completions
{
    [Trait("Category", "Unit")]
    public sealed class CompletionSqlPathEscapingTests
    {
        private const string HostileName = "x') is null; delete from \"Case\"; select ('";

        [Theory]
        [InlineData("Amount", "Amount")]
        [InlineData("O'Brien", "O''Brien")]
        [InlineData("''", "''''")]
        [InlineData("", "")]
        public void SqlLiteralDoublesSingleQuotes(string name, string expected)
        {
            FieldQuery.SqlLiteral(name).Should().Be(expected);
        }

        [Fact]
        public void SqlLiteralPassesNullThrough()
        {
            FieldQuery.SqlLiteral(null).Should().BeNull();
        }

        [Theory]
        [InlineData("Fraud")]
        [InlineData("O'Brien")]
        [InlineData("quote\"inside")]
        [InlineData("back\\slash")]
        public void SqlJsonStringLiteralRoundTripsToTheOriginalName(string name)
        {
            var literal = FieldQuery.SqlJsonStringLiteral(name);

            literal.Replace("''", string.Empty).Should().NotContain("'");
            JsonSerializer.Deserialize<string>(literal.Replace("''", "'")).Should().Be(name);
        }

        [Fact]
        public void AnUnescapedHostileNameBreaksOutOfTheLiteral()
        {
            var sql = $"select (\"Json\"-> 'payload' ->> '{HostileName}') from \"Archive\"";

            var act = () => PostgresSqlValidator.AssertSelectOnly(sql);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void AnEscapedHostileNameStaysOneSelectOverOneLiteral()
        {
            var sql = $"select (\"Json\"-> 'payload' ->> '{FieldQuery.SqlLiteral(HostileName)}') from \"Archive\"";

            var act = () => PostgresSqlValidator.AssertSelectOnly(sql);

            act.Should().NotThrow();
        }

        [Fact]
        public void AnEscapedHostileTagNameStaysOneSelectOverOneJsonLiteral()
        {
            var sql = "select (case when (\"Json\"-> 'tag') @> " +
                      $"'{FieldQuery.SqlJsonStringLiteral(HostileName)}'::jsonb then 'True' else 'False' end) " +
                      "from \"Archive\"";

            var act = () => PostgresSqlValidator.AssertSelectOnly(sql);

            act.Should().NotThrow();
        }
    }
}
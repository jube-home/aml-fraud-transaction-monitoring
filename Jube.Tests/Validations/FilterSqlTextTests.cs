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

using System;
using FluentAssertions;
using Jube.Data.Reporting;
using Jube.Validations;
using Xunit;

namespace Jube.Test.Validations
{
    [Trait("Category", "Unit")]
    public sealed class FilterSqlTextTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void EmptyFilterIsAcceptable(string? sql)
        {
            FilterSqlText.IsAcceptable(sql).Should().BeTrue();
        }

        [Theory]
        [InlineData("(\"Json\"->>'Amount')::float > (@1)")]
        [InlineData("\"Tag\".\"Fraud\" = 1")]
        [InlineData("NOT (a = (@1) or b = (@2))")]
        [InlineData("x = 'select from union; -- /* $1 pg_sleep'")]
        [InlineData("x = 'it''s fine'")]
        public void OrdinaryPredicatesAreAcceptable(string sql)
        {
            FilterSqlText.IsAcceptable(sql).Should().BeTrue();
        }

        [Theory]
        [InlineData("1=1; drop table \"Archive\"")]
        [InlineData("1=1 -- trailing")]
        [InlineData("1=1 /* c */")]
        [InlineData("1=1 */")]
        [InlineData("x in (select 1)")]
        [InlineData("x = 1 UNION ALL x = 2")]
        [InlineData("pg_sleep(10) is not null")]
        [InlineData("PG_READ_FILE('x') is not null")]
        [InlineData("lo_import('x') > 0")]
        [InlineData("current_setting('x') = 'y'")]
        [InlineData("version() = 'x'")]
        [InlineData("x::regclass is not null")]
        [InlineData("1=1) or (1=1")]
        [InlineData("(1=1")]
        [InlineData("x = 'unterminated")]
        [InlineData("x = \"unterminated")]
        [InlineData("x = E'\\'' or 1=1")]
        [InlineData("x = E'\\'' or pg_sleep(1) is null or x = E'\\''")]
        [InlineData("x = e'a'")]
        [InlineData("x = 1 and E'\\x27'")]
        [InlineData("x = $$a$$")]
        [InlineData("x = $1")]
        [InlineData("x = 1 order by 1")]
        [InlineData("x = 1 limit 1")]
        [InlineData("x = 1 offset 1")]
        [InlineData("x = 1 for update")]
        [InlineData("x = 1 into y")]
        public void HostilePredicatesAreRefused(string sql)
        {
            FilterSqlText.IsAcceptable(sql).Should().BeFalse();
        }

        [Theory]
        [InlineData("name_e = 'a'")]
        [InlineData("true = 'a'")]
        [InlineData("\"e\" = 'a'")]
        public void AnIdentifierEndingInEBeforeALiteralIsNotAnEscapeString(string sql)
        {
            FilterSqlText.IsAcceptable(sql).Should().BeTrue();
        }

        [Fact]
        public void ANulOrOtherControlCharacterIsRefused()
        {
            FilterSqlText.IsAcceptable("x = 1\0").Should().BeFalse();
            FilterSqlText.IsAcceptable("x = 1\u0007").Should().BeFalse();
            FilterSqlText.IsAcceptable("x = 1\t and\r\n y = 2").Should().BeTrue();
        }

        [Fact]
        public void APredicateOverTheMaximumLengthIsRefused()
        {
            FilterSqlText.IsAcceptable("x = 1" + new string(' ', 65536)).Should().BeFalse();
        }

        [Theory]
        [InlineData("\"pg_sleep\"(10) is not null")]
        [InlineData("\"repeat\"('a', 1000000000) is not null")]
        [InlineData("\"pg_read_file\"('/etc/passwd') is not null")]
        [InlineData("\"set_config\"('x', 'y', false) is not null")]
        [InlineData("\"query_to_xml\"('x', true, true, '') is not null")]
        public void AQuotedFunctionNameThatTheTextCheckCannotSeeIsStillRefusedByTheStatementGateAtExecution(
            string sql)
        {
            FilterSqlText.IsAcceptable(sql).Should().BeTrue();

            var executed = Postgres.BuildArchiveSampleSql(sql, 2, false);

            Assert.Throws<InvalidOperationException>(() => PostgresSqlValidator.AssertSelectOnly(executed, false));
        }

        [Fact]
        public void AFilterThatClosesTheWrapperParenthesisToEscapeTheModelKeyIsRefusedByTheTextCheck()
        {
            FilterSqlText.IsAcceptable("1=1) or (\"EntityAnalysisModelId\" <> 0").Should().BeFalse();
        }

        [Fact]
        public void AUnionSmuggledThroughTheWrapperIsRefusedByTheTextCheckAndByTheStatementGate()
        {
            const string sql = "1=1 or true) union (select \"Json\" from \"UserRegistry\"";

            FilterSqlText.IsAcceptable(sql).Should().BeFalse();

            var executed = Postgres.BuildArchiveSampleSql(sql, 2, false);

            Assert.Throws<InvalidOperationException>(() => PostgresSqlValidator.AssertSelectOnly(executed, true));
        }
    }
}

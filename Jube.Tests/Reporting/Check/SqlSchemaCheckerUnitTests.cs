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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Data.Query.GetPostgresSchemaSnapshotQuery.Models;
using Jube.Data.Reporting.Check;
using Xunit;

namespace Jube.Test.Reporting.Check
{
    [Trait("Category", "Unit")]
    public sealed class SqlSchemaCheckerUnitTests
    {
        private static readonly SqlSchemaSnapshot snapshot = new(new PostgresSchemaSnapshotRows(
            [
                new PostgresSchemaColumnRow("public", "orders", "id", "int4", "N"),
                new PostgresSchemaColumnRow("public", "orders", "status", "text", "S"),
                new PostgresSchemaColumnRow("public", "orders", "amount", "numeric", "N")
            ],
            [new PostgresSchemaPrimaryKeyRow("orders", "id")],
            [
                new PostgresSchemaFunctionRow("lower", 1, 0, false, "f"),
                new PostgresSchemaFunctionRow("count", 1, 0, false, "a"),
                new PostgresSchemaFunctionRow("count", 0, 0, false, "a")
            ],
            ["int4", "text", "numeric", "bool"]));

        private static IReadOnlyList<SqlFinding> Check(string sql, SqlCheckOptions? options = null)
        {
            return SqlSchemaChecker.Check(sql, snapshot, options ?? new SqlCheckOptions());
        }

        [Fact]
        public void AnUnknownColumnReportsItsLineAndColumn()
        {
            var finding = Check("select\n  nope from orders").Should().ContainSingle().Subject;

            finding.Code.Should().Be(SqlCheckMessages.UnknownColumn);
            finding.Line.Should().Be(2);
            finding.Column.Should().Be(3);
        }

        [Fact]
        public void APositionIsCorrectAfterNamedParametersAreSubstituted()
        {
            const string sql = "select @a, nope from orders where id = @a";

            var finding = Check(sql, new SqlCheckOptions { ParameterNames = new HashSet<string> { "a" } })
                .Should().ContainSingle().Subject;

            finding.Position.Should().Be(sql.IndexOf("nope", StringComparison.Ordinal) + 1);
        }

        [Fact]
        public void APositionIsCorrectAfterMultiByteCharacters()
        {
            const string sql = "select 'é€', nope from orders";

            Check(sql).Should().ContainSingle().Which.Position
                .Should().Be(sql.IndexOf("nope", StringComparison.Ordinal) + 1);
        }

        [Fact]
        public void AnUnsuppliedNamedParameterIsReportedWithItsName()
        {
            var findings = Check("select * from orders where id = @missing",
                new SqlCheckOptions { ParameterNames = new HashSet<string> { "other" } });

            findings.Should().ContainSingle().Which.Code.Should().Be(SqlCheckMessages.ParameterUnknown);
            findings[0].Arguments.Should().Contain("missing");
        }

        [Fact]
        public void ASuppliedNamedParameterOfTheWrongTypeIsReportedAgainstTheColumn()
        {
            var options = new SqlCheckOptions
            {
                ParameterNames = new HashSet<string> { "x" },
                ParameterCategories = new Dictionary<string, SqlTypeCategory> { ["x"] = SqlTypeCategory.Text }
            };

            Check("select * from orders where id = @x", options).Should().ContainSingle().Which.Code
                .Should().Be(SqlCheckMessages.ParameterType);
        }

        [Fact]
        public void ASuppliedNamedParameterOfTheRightTypeIsAccepted()
        {
            var options = new SqlCheckOptions
            {
                ParameterNames = new HashSet<string> { "x" },
                ParameterCategories = new Dictionary<string, SqlTypeCategory> { ["x"] = SqlTypeCategory.Numeric }
            };

            Check("select * from orders where id = @x", options).Should().BeEmpty();
        }

        [Fact]
        public void ASnapshotThatCouldNotBeLoadedLeavesTheStatementToTheLaterGates()
        {
            SqlPreflight.Check("select nope from orders", null, null).Should().BeEmpty();
        }

        [Fact]
        public void ANonSelectStatementIsLeftToTheSelectOnlyGate()
        {
            Check("delete from orders").Should().BeEmpty();
        }

        [Fact]
        public void EveryFindingCodeIsExercisedByTheCorpusOrAnExplicitTest()
        {
            var exercised = SqlCheckCorpus.Bad.Select(b => b.Code).ToHashSet();

            var untested = SqlCheckMessages.English.Keys
                .Where(code => !exercised.Contains(code) && !exercised.Contains(code + "Suggest") &&
                               !exercised.Contains(code.Replace("Suggest", string.Empty)))
                .Except([SqlCheckMessages.ParameterUnknown, SqlCheckMessages.ParameterType,
                    SqlCheckMessages.UnknownTableSuggest, SqlCheckMessages.UnknownAliasSuggest]);

            untested.Should().BeEmpty();
        }

        [Fact]
        public void EveryMessageHasItsOwnPlaceholdersInOrder()
        {
            foreach (var (code, text) in SqlCheckMessages.English)
            {
                var highest = Enumerable.Range(0, 4).LastOrDefault(i => text.Contains("{" + i + "}"), -1);

                for (var i = 0; i <= highest; i++)
                {
                    text.Should().Contain("{" + i + "}", code);
                }
            }
        }
    }
}

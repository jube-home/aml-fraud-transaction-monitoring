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
using Jube.Service.DynamicSql;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class DynamicSqlCommonTableExpressionTests
    {
        private const string Cache = "\"AskJoobyDataCache\"";

        [Fact]
        public void ACommonTableExpressionNameIsNotJudgedAgainstTheAllowList()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("Administrator", _ => true, 18);

            var rewritten = PostgresSqlValidator.RewriteTableReferences(
                $"WITH src AS (SELECT \"DataJson\" FROM {Cache}) SELECT \"DataJson\" FROM src", allowed);

            rewritten.Should().Contain("UserInTenant",
                "the real table inside the expression body still goes through its tenant template");
        }

        [Fact]
        public void TheExactStatementTheAgentWasRefusedNowRewrites()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("Administrator", _ => true, 18);

            var act = () => PostgresSqlValidator.RewriteTableReferences(
                """
                WITH src AS (
                  SELECT "DataJson"::jsonb AS j FROM "AskJoobyDataCache" WHERE "PointerKey" = 'tbl_x'
                ), long AS (
                  SELECT r.ord AS rid, kv.key AS col, NULLIF(kv.value #>> '{}', '')::float8 AS val
                  FROM src, jsonb_array_elements(src.j) WITH ORDINALITY AS r(obj, ord),
                       jsonb_each(r.obj) AS kv
                )
                SELECT a.col AS feature_a, b.col AS feature_b, corr(a.val, b.val)::float8 AS r
                FROM long a JOIN long b ON a.rid = b.rid AND a.col < b.col
                GROUP BY a.col, b.col
                """, allowed);

            act.Should().NotThrow(
                "this is the statement that failed with \"Table \\\"long\\\" is not permitted\"");
        }

        [Fact]
        public void OneExpressionCanBuildOnAnEarlierOne()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("Administrator", _ => true, 18);

            var act = () => PostgresSqlValidator.RewriteTableReferences(
                $"WITH a AS (SELECT 1 AS n FROM {Cache}), b AS (SELECT n FROM a) SELECT n FROM b", allowed);

            act.Should().NotThrow("a later expression may reference an earlier one");
        }

        [Fact]
        public void ARecursiveExpressionCanReferenceItself()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("Administrator", _ => true, 18);

            var act = () => PostgresSqlValidator.RewriteTableReferences(
                "WITH RECURSIVE counter(n) AS (SELECT 1 UNION ALL SELECT n + 1 FROM counter WHERE n < 5) " +
                "SELECT n FROM counter", allowed);

            act.Should().NotThrow("WITH RECURSIVE brings the name into scope inside its own body");
        }

        [Fact]
        public void ANonRecursiveExpressionCannotSeeItselfSoAnInnerReferenceIsStillTheRealTable()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("Administrator", _ => true, 18);

            var rewritten = PostgresSqlValidator.RewriteTableReferences(
                $"WITH scoped AS (SELECT \"Id\" FROM {Cache}) SELECT \"Id\" FROM scoped", allowed);

            rewritten.Should().Contain("UserInTenant");
        }

        [Fact]
        public void AnUnregisteredTableInsideAnExpressionBodyIsStillRejected()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("Administrator", _ => true, 18);

            var act = () => PostgresSqlValidator.RewriteTableReferences(
                "WITH src AS (SELECT * FROM \"SomeTableNobodyRegistered\") SELECT * FROM src", allowed);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*SomeTableNobodyRegistered*",
                    "the expression body is walked like any other part of the statement");
        }

        [Fact]
        public void AnExpressionMayNotTakeTheNameOfARegisteredTable()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("Administrator", _ => true, 18);

            var act = () => PostgresSqlValidator.RewriteTableReferences(
                "WITH \"Archive\" AS (SELECT 1 AS n) SELECT n FROM \"Archive\"", allowed);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Archive*",
                    "shadowing a real name would hand back a relation that never met its tenant template");
        }

        [Fact]
        public void CollectedTableReferencesExcludeExpressionNames()
        {
            var names = PostgresSqlValidator.CollectTableReferences(
                $"WITH src AS (SELECT \"Id\" FROM {Cache}) SELECT \"Id\" FROM src");

            names.Should().Contain("AskJoobyDataCache");

            names.Should().NotContain("src",
                "an expression name is not a relation anyone can be granted or denied");
        }
    }
}
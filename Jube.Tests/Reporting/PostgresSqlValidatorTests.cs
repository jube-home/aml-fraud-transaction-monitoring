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
using Jube.Data.Reporting;
using Xunit;

namespace Jube.Test.Reporting
{
    [Trait("Category", "Reporting")]
    public sealed class PostgresSqlValidatorTests
    {
        private const string CaseTemplate =
            "SELECT c.* FROM \"Case\" c JOIN \"CaseWorkflow\" cw ON cw.\"Guid\" = c.\"CaseWorkflowGuid\" " +
            "WHERE EXISTS (SELECT 1 FROM \"UserInTenant\" uit WHERE uit.\"User\" = 'alice')";

        [Fact]
        public void AssertSelectOnlyAllowsAPlainSelect()
        {
            var act = () => PostgresSqlValidator.AssertSelectOnly("SELECT 1");
            var exception = Record.Exception(act);
            Assert.Null(exception);
        }

        [Fact]
        public void AssertSelectOnlyRejectsAnInsert()
        {
            Assert.Throws<InvalidOperationException>(() =>
                PostgresSqlValidator.AssertSelectOnly("INSERT INTO \"Case\" (\"Id\") VALUES (1)"));
        }

        [Fact]
        public void AssertSelectOnlyRejectsADataModifyingCte()
        {
            Assert.Throws<InvalidOperationException>(() => PostgresSqlValidator.AssertSelectOnly(
                "WITH x AS (DELETE FROM \"Case\" RETURNING \"Id\") SELECT * FROM x"));
        }

        [Fact]
        public void EscapeStringLiteralWrapsInSingleQuotes()
        {
            Assert.Equal("'alice'", PostgresSqlValidator.EscapeStringLiteral("alice"));
        }

        [Fact]
        public void EscapeStringLiteralDoublesEmbeddedSingleQuotes()
        {
            Assert.Equal("'alice''s account'", PostgresSqlValidator.EscapeStringLiteral("alice's account"));
        }

        [Fact]
        public void EscapeStringLiteralNeutralisesAClassicInjectionAttempt()
        {
            var escaped = PostgresSqlValidator.EscapeStringLiteral("' OR '1'='1");
            var sql = $"SELECT 1 WHERE 'x' = {escaped}";

            var result = PgSqlParser.Parser.Parse(sql);
            Assert.True(result.IsSuccess);
            var whereClause = result.Value!.Stmts[0].Stmt!.SelectStmt!.WhereClause;
            Assert.NotNull(whereClause);
            Assert.NotNull(whereClause!.AExpr);
        }

        [Fact]
        public void RewriteTableReferencesSubstitutesATopLevelFromReference()
        {
            var allowed = new Dictionary<string, string> { ["Case"] = CaseTemplate };

            var rewritten = PostgresSqlValidator.RewriteTableReferences(
                "SELECT * FROM \"Case\" WHERE \"Rating\" > 5", allowed);

            Assert.Contains("UserInTenant", rewritten);
            Assert.Contains("\"Rating\" > 5", rewritten);
        }

        [Fact]
        public void RewriteTableReferencesFindsATableHiddenInsideAWhereClauseSubquery()
        {
            var allowed = new Dictionary<string, string>
            {
                ["Case"] = CaseTemplate,
                ["CaseWorkflow"] = CaseTemplate
            };

            var rewritten = PostgresSqlValidator.RewriteTableReferences(
                "SELECT * FROM \"Case\" c WHERE c.\"CaseWorkflowGuid\" IN " +
                "(SELECT \"Guid\" FROM \"CaseWorkflow\" WHERE \"Active\" = 1)", allowed);

            Assert.Equal(2, CountOccurrences(rewritten, "UserInTenant"));
        }

        [Fact]
        public void RewriteTableReferencesLeavesAnExplicitlySafeTableUntouched()
        {
            var allowed = new Dictionary<string, string> { ["SomeReferenceTable"] = null! };

            var rewritten = PostgresSqlValidator.RewriteTableReferences(
                "SELECT * FROM \"SomeReferenceTable\"", allowed);

            Assert.DoesNotContain("UserInTenant", rewritten);
            Assert.Contains("SomeReferenceTable", rewritten);
        }

        [Fact]
        public void RewriteTableReferencesThrowsForAnUnregisteredTopLevelTable()
        {
            var allowed = new Dictionary<string, string> { ["Case"] = CaseTemplate };

            var act = () => PostgresSqlValidator.RewriteTableReferences(
                "SELECT * FROM \"UserRegistry\"", allowed);

            var ex = Assert.Throws<InvalidOperationException>(act);
            Assert.Contains("UserRegistry", ex.Message);
        }

        [Fact]
        public void RewriteTableReferencesThrowsForAnUnregisteredTableHiddenInsideASubquery()
        {
            var allowed = new Dictionary<string, string> { ["Case"] = CaseTemplate };

            var act = () => PostgresSqlValidator.RewriteTableReferences(
                "SELECT * FROM \"Case\" WHERE \"Id\" IN (SELECT \"Id\" FROM \"UserRegistry\")", allowed);

            var ex = Assert.Throws<InvalidOperationException>(act);
            Assert.Contains("UserRegistry", ex.Message);
        }

        [Fact]
        public void RewriteTableReferencesThrowsForAnUnregisteredTableInAJoin()
        {
            var allowed = new Dictionary<string, string> { ["Case"] = CaseTemplate };

            var act = () => PostgresSqlValidator.RewriteTableReferences(
                "SELECT * FROM \"Case\" c JOIN \"CaseNote\" cn ON cn.\"CaseId\" = c.\"Id\"", allowed);

            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void RewriteTableReferencesThrowsForAnUnregisteredTableInACte()
        {
            var allowed = new Dictionary<string, string> { ["Case"] = CaseTemplate };

            var act = () => PostgresSqlValidator.RewriteTableReferences(
                "WITH x AS (SELECT * FROM \"UserRegistry\") SELECT * FROM \"Case\", x", allowed);

            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void RewriteTableReferencesPreservesAnExplicitAliasForColumnQualification()
        {
            var allowed = new Dictionary<string, string> { ["Case"] = CaseTemplate };

            var rewritten = PostgresSqlValidator.RewriteTableReferences(
                "SELECT c.\"Id\" FROM \"Case\" c WHERE c.\"Rating\" > 5", allowed);

            var reparsed = PgSqlParser.Parser.Parse(rewritten);
            Assert.True(reparsed.IsSuccess);
        }

        [Fact]
        public void RewriteTableReferencesRejectsANonSelectEvenIfEveryTableIsRegistered()
        {
            var allowed = new Dictionary<string, string> { ["Case"] = CaseTemplate };

            var act = () => PostgresSqlValidator.RewriteTableReferences(
                "DELETE FROM \"Case\" WHERE \"Id\" = 1", allowed);

            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void RewriteTableReferencesOutputIsItselfReparseableAsAPureSelect()
        {
            var allowed = new Dictionary<string, string> { ["Case"] = CaseTemplate };

            var rewritten = PostgresSqlValidator.RewriteTableReferences("SELECT * FROM \"Case\"", allowed);

            var reparsed = PgSqlParser.Parser.Parse(rewritten);
            Assert.True(reparsed.IsSuccess);
            Assert.NotNull(reparsed.Value!.Stmts[0].Stmt!.SelectStmt);
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            var count = 0;
            var index = 0;

            while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += needle.Length;
            }

            return count;
        }
    }
}
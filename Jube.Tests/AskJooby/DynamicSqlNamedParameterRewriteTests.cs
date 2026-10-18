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
using Jube.Data.Reporting;
using Jube.Service.DynamicSql;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class DynamicSqlNamedParameterRewriteTests
    {
        private static readonly IReadOnlyDictionary<string, string> allowed =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ExampleCustomerCaseManagement"] = "SELECT t0.* FROM \"ExampleCustomerCaseManagement\" t0"
            };

        [Fact]
        public void TheSeededPieChartCommandRewritesWithItsParameterIntact()
        {
            var rewritten = PostgresSqlValidator.RewriteTableReferencesPreservingNamedParameters(
                "select \"MCC\", \"Frequency\", \"Sum\" from \"ExampleCustomerCaseManagement\" " +
                "where \"AccountId\" = @AccountId order by \"Frequency\" desc", allowed);

            rewritten.Should().Contain("@AccountId");
            rewritten.Should().Contain("SELECT t0.* FROM \"ExampleCustomerCaseManagement\" t0");
        }

        [Fact]
        public void ParameterCaseAndUnderscoresSurviveTheRoundTrip()
        {
            var rewritten = PostgresSqlValidator.RewriteTableReferencesPreservingNamedParameters(
                "select \"Frequency\" from \"ExampleCustomerCaseManagement\" " +
                "where \"Frequency\" > @Frequency_Greater_Than " +
                "and \"Sum\" > @Percentage_Contribution_Greater_Than", allowed);

            rewritten.Should().Contain("@Frequency_Greater_Than");
            rewritten.Should().Contain("@Percentage_Contribution_Greater_Than");
            rewritten.Should().NotContain("$1");
        }

        [Fact]
        public void MoreThanNineParametersRestoreWithoutPrefixCollision()
        {
            var predicates = string.Join(" and ",
                Enumerable.Range(1, 12).Select(i => $"\"Frequency\" > @Parameter{i}"));

            var rewritten = PostgresSqlValidator.RewriteTableReferencesPreservingNamedParameters(
                $"select \"Frequency\" from \"ExampleCustomerCaseManagement\" where {predicates}", allowed);

            foreach (var i in Enumerable.Range(1, 12))
            {
                rewritten.Should().Contain($"@Parameter{i}");
            }

            rewritten.Should().NotContain("@Parameter112");
        }

        [Fact]
        public void AnAtSignInsideAStringLiteralIsRestoredVerbatim()
        {
            var rewritten = PostgresSqlValidator.RewriteTableReferencesPreservingNamedParameters(
                "select \"Frequency\" from \"ExampleCustomerCaseManagement\" " +
                "where \"AccountId\" = 'fraud@example.test' and \"Sum\" > @Threshold", allowed);

            rewritten.Should().Contain("fraud@example.test");
            rewritten.Should().Contain("@Threshold");
        }

        [Fact]
        public void PositionalParametersAreRefused()
        {
            var act = () => PostgresSqlValidator.RewriteTableReferencesPreservingNamedParameters(
                "select \"Frequency\" from \"ExampleCustomerCaseManagement\" where \"Frequency\" > $1", allowed);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Positional parameters are not permitted*");
        }

        [Fact]
        public void AnUnregisteredTableIsRefusedEvenWithParameters()
        {
            var act = () => PostgresSqlValidator.RewriteTableReferencesPreservingNamedParameters(
                "select * from \"UserRegistry\" where \"Name\" = @Name", allowed);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*is not permitted in this query*");
        }

        [Fact]
        public void TheRegistryTemplatesAllRewriteTheirOwnTable()
        {
            var tables = DynamicSqlTableAccess.BuildAllowedTables("Administrator", _ => true, 1);

            foreach (var (table, _) in tables)
            {
                var rewritten = PostgresSqlValidator.RewriteTableReferencesPreservingNamedParameters(
                    $"select * from \"{table}\" where 1 = @Parameter", tables);

                rewritten.Should().Contain("@Parameter", $"the parameter must survive the {table} template");
            }
        }
    }
}

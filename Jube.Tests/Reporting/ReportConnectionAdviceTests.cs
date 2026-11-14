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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Data.Reporting;
using Jube.Service.DynamicSql;
using Xunit;

namespace Jube.Test.Reporting
{
    [Trait("Category", "Unit")]
    public sealed class ReportConnectionAdviceTests
    {
        private const string Application = "Host=db;Database=jube;Username=jube_app;Password=a";

        [Theory]
        [InlineData(null, ReportConnectionAssessment.Unset)]
        [InlineData("", ReportConnectionAssessment.Unset)]
        [InlineData("   ", ReportConnectionAssessment.Unset)]
        [InlineData("Host=replica;Database=jube;Username=jube_app;Password=b", ReportConnectionAssessment.SameLogin)]
        [InlineData("Host=db;Database=jube;Username=JUBE_APP;Password=a", ReportConnectionAssessment.SameLogin)]
        [InlineData("Host=db;Database=jube;Username=jube_reporting;Password=a", ReportConnectionAssessment.Separate)]
        [InlineData("not a connection string", ReportConnectionAssessment.Separate)]
        public void TheReportConnectionIsAssessedAgainstTheApplicationLogin(string report,
            ReportConnectionAssessment expected)
        {
            ReportConnectionAdvice.Assess(report, Application).Should().Be(expected);
        }

        [Fact]
        public void WithNoApplicationConnectionASetReportConnectionIsLeftAlone()
        {
            ReportConnectionAdvice.Assess("Host=x;Username=u", null).Should().Be(ReportConnectionAssessment.Separate);
        }

        [Fact]
        public void TheWarningNamesTheRemedyAndNeverCarriesAnyConnectionDetail()
        {
            foreach (var assessment in new[] { ReportConnectionAssessment.Unset, ReportConnectionAssessment.SameLogin })
            {
                var text = ReportConnectionAdvice.Warning(assessment);

                text.Should().Contain("SELECT-only").And.Contain("ReportConnectionString");
                text.Should().NotContainAny("Password", "Host=", "jube_app", "Username");
            }

            ReportConnectionAdvice.Warning(ReportConnectionAssessment.Separate).Should().BeNull();
        }

        [Fact]
        public void TheDocumentedSelectOnlyRoleGrantsExactlyTheTablesTheAllowListCanReach()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "docs")))
            {
                directory = directory.Parent;
            }

            var text = File.ReadAllText(Path.Combine(directory!.FullName, "docs", "Configuration", "Visualisations",
                "SecurityPolicy", "index.md"));

            var start = text.IndexOf("<!-- reporting-role-grants:start -->", StringComparison.Ordinal);
            var end = text.IndexOf("<!-- reporting-role-grants:end -->", StringComparison.Ordinal);

            start.Should().BeGreaterThan(-1);
            end.Should().BeGreaterThan(start);

            var documented = Regex.Matches(text[start..end], "\"([A-Za-z0-9_]+)\"").Select(m => m.Groups[1].Value)
                .ToHashSet(StringComparer.Ordinal);

            var allowed = DynamicSqlTableAccess.BuildVisualisationAllowedTables("Administrator", _ => true);
            var reachable = new HashSet<string>(StringComparer.Ordinal);

            foreach (var (table, template) in allowed)
            {
                if (table.StartsWith("pg_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                reachable.Add(table);

                foreach (var inner in PostgresSqlValidator.CollectTableReferences(template))
                {
                    if (!inner.StartsWith("pg_", StringComparison.OrdinalIgnoreCase))
                    {
                        reachable.Add(inner);
                    }
                }
            }

            reachable.Except(documented).Should().BeEmpty("every reachable table needs its GRANT SELECT in the docs");
            documented.Except(reachable).Except(["Name", "RoleRegistryGuid"]).Should()
                .BeEmpty("the documented role must not grant tables the allow-list cannot reach");
        }
    }
}

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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Convention
{
    [Trait("Category", "Convention")]
    public sealed class AuthorSqlPreflightConventionTests
    {
        private static readonly Regex authorSqlEntryPoints = new(
            @"postgres\s*\.\s*(ExecuteLimitedByNamedParametersAsync|IntrospectAsync|IntrospectWithTableTokenSwitchAsync|"
            + @"ExecuteWithTableTokenSwitchByNamedParametersAsync|ExplainWithTableTokenSwitchAsync|"
            + @"ExecuteAlreadyTokenSwitchedAsync|ExecuteWithTableTokenSwitchAsync|ExecuteByNamedParametersAsync)\(",
            RegexOptions.Compiled);

        private static readonly string[] preflightMarkers =
            ["SqlPreflight.", "PreflightAsync(", "SqlAuthoringCheck.", "SqlSchemaChecker.Check("];

        private static string Root()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Jube.App", "Jube.App.csproj")))
            {
                directory = directory.Parent;
            }

            return directory.Required().FullName;
        }

        [Fact]
        public void EveryCodePathThatSendsAuthorSqlToTheDatabaseChecksItBeforeOpeningAConnection()
        {
            var separator = Path.DirectorySeparatorChar;
            var root = Root();

            var offenders = new[] { "Jube.Data", "Jube.Service", "Jube.Engine", "Jube.App" }
                .SelectMany(project => Directory.EnumerateFiles(Path.Combine(root, project), "*.cs",
                    SearchOption.AllDirectories))
                .Where(p => !p.Contains($"{separator}obj{separator}") && !p.Contains($"{separator}bin{separator}"))
                .Where(p => Path.GetFileName(p) != "Postgres.cs")
                .Select(p => (Path: p, Text: File.ReadAllText(p)))
                .Where(f => authorSqlEntryPoints.IsMatch(f.Text)
                            && !preflightMarkers.Any(m => f.Text.Contains(m, StringComparison.Ordinal)))
                .Select(f => Path.GetRelativePath(root, f.Path))
                .ToList();

            offenders.Should().BeEmpty(
                "a file that runs author supplied SQL must call the schema checker first (SqlPreflight, "
                + "SqlAuthoringCheck or the Ask Jooby preflight) so the first fault heard is ours, not the database's");
        }
    }
}

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
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Convention
{
    [Trait("Category", "Unit")]
    public sealed class DbContextLeakShapeConventionTests
    {
        private static readonly Regex chainedOnTheContext = new(@"GetDbContext\(\)\s*\.", RegexOptions.Compiled);

        private static readonly Regex connectionStringLeak =
            new(@"GetDbContext\(\)\s*\.\s*Connection\s*\.\s*ConnectionString", RegexOptions.Compiled);

        private static string TestProjectDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Jube.Tests", "Jube.Tests.csproj")))
            {
                directory = directory.Parent;
            }

            return Path.Combine(directory.Required().FullName, "Jube.Tests");
        }

        private static IEnumerable<string> SourceFiles()
        {
            var separator = Path.DirectorySeparatorChar;

            return Directory.EnumerateFiles(TestProjectDirectory(), "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{separator}obj{separator}") && !p.Contains($"{separator}bin{separator}"))
                .Where(p => Path.GetFileName(p) != nameof(DbContextLeakShapeConventionTests) + ".cs");
        }

        private static IEnumerable<string> Offenders(Regex pattern)
        {
            foreach (var path in SourceFiles())
            {
                var text = File.ReadAllText(path);

                foreach (var match in pattern.Matches(text).Cast<Match>())
                {
                    var lineStart = text.LastIndexOf('\n', match.Index) + 1;
                    var lineEnd = text.IndexOf('\n', match.Index);
                    var line = text[lineStart..(lineEnd < 0 ? text.Length : lineEnd)].TrimStart();

                    if (line.StartsWith("using ", StringComparison.Ordinal)
                        || line.StartsWith("await using ", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var lineNumber = text[..match.Index].Count(c => c == '\n') + 1;

                    yield return $"{Path.GetRelativePath(TestProjectDirectory(), path)}:{lineNumber}";
                }
            }
        }

        [Fact]
        public void NoTestReadsTheConnectionStringOffAContextThatIsNeverDisposed()
        {
            var offenders = Offenders(connectionStringLeak).OrderBy(o => o, StringComparer.Ordinal).ToList();

            offenders.Should().BeEmpty(
                "each of these rents a pooled connection through a context that nothing disposes, "
                + "so read the connection string from the fixture instead: " + string.Join(", ", offenders));
        }

        [Fact]
        public void NoTestChainsAMemberOffAContextItNeverDisposes()
        {
            var offenders = Offenders(chainedOnTheContext).OrderBy(o => o, StringComparer.Ordinal).ToList();

            offenders.Should().BeEmpty(
                "a context created and used in one expression is never disposed and keeps its pooled connection; "
                + "declare it with a using statement: " + string.Join(", ", offenders));
        }
    }
}

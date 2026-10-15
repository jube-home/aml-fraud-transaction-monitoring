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
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Documentation
{
    [Trait("Category", "Unit")]
    public sealed class TestDocumentationTests
    {
        private static readonly Regex classLine = new(@"^`(?<name>Jube\.Test\.[A-Za-z0-9_.+]+)` — ",
            RegexOptions.Compiled);

        private static readonly Regex rowLine = new(@"^\|\s*`(?<name>[A-Za-z0-9_]+)`\s*\|", RegexOptions.Compiled);

        private sealed record Section(string Page, HashSet<string> Methods);

        private static string TestingDocumentsDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "docs")))
            {
                directory = directory.Parent;
            }

            return Path.Combine(directory.Required().FullName, "docs", "Testing");
        }

        private static Dictionary<string, List<Section>> DocumentedSections()
        {
            var sections = new Dictionary<string, List<Section>>(StringComparer.Ordinal);

            foreach (var path in Directory.EnumerateFiles(TestingDocumentsDirectory(), "*.md")
                         .Where(p => Path.GetFileName(p) != "index.md").OrderBy(p => p, StringComparer.Ordinal))
            {
                var page = Path.GetFileName(path);
                Section? current = null;

                foreach (var line in File.ReadAllLines(path))
                {
                    var klass = classLine.Match(line);

                    if (klass.Success)
                    {
                        current = new Section(page, new HashSet<string>(StringComparer.Ordinal));

                        if (!sections.TryGetValue(klass.Groups["name"].Value, out var list))
                        {
                            sections[klass.Groups["name"].Value] = list = new List<Section>();
                        }

                        list.Add(current);

                        continue;
                    }

                    var row = rowLine.Match(line);

                    if (row.Success && current != null && row.Groups["name"].Value != "Test")
                    {
                        current.Methods.Add(row.Groups["name"].Value);
                    }
                }
            }

            return sections;
        }

        private static Dictionary<string, HashSet<string>> LiveTests()
        {
            var live = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            foreach (var type in typeof(TestDocumentationTests).Assembly.GetTypes()
                         .Where(t => t.IsClass && !t.IsAbstract && (t.IsPublic || t.IsNestedPublic)))
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Where(m => m.IsDefined(typeof(FactAttribute), true)).Select(m => m.Name)
                    .ToHashSet(StringComparer.Ordinal);

                if (methods.Count > 0)
                {
                    live[type.FullName.Required()] = methods;
                }
            }

            return live;
        }

        [Fact]
        public void EveryTestClassIsDocumentedExactlyOnce()
        {
            var documented = DocumentedSections();

            var missing = LiveTests().Keys.Where(name => !documented.ContainsKey(name))
                .OrderBy(name => name, StringComparer.Ordinal).ToList();

            missing.Should().BeEmpty(
                "every test class needs a section in docs/Testing, with the fully qualified name on the line "
                + "under its heading, and a table row per test: " + string.Join(", ", missing));

            var duplicated = documented.Where(pair => pair.Value.Count > 1)
                .Select(pair => $"{pair.Key} on {string.Join(" and ", pair.Value.Select(s => s.Page))}")
                .OrderBy(text => text, StringComparer.Ordinal).ToList();

            duplicated.Should().BeEmpty(
                "a test class documented on two pages drifts the moment one copy is edited: "
                + string.Join("; ", duplicated));
        }

        [Fact]
        public void EveryTestHasARowInItsClassSectionAndNoRowIsStale()
        {
            var documented = DocumentedSections();
            var differences = new List<string>();

            foreach (var (name, methods) in LiveTests().OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                if (!documented.TryGetValue(name, out var list))
                {
                    continue;
                }

                var rows = list[0].Methods;

                foreach (var method in methods.Where(m => !rows.Contains(m)).OrderBy(m => m, StringComparer.Ordinal))
                {
                    differences.Add($"{name}.{method} has no row on {list[0].Page}");
                }

                foreach (var row in rows.Where(r => !methods.Contains(r)).OrderBy(r => r, StringComparer.Ordinal))
                {
                    differences.Add($"{name}.{row} is documented on {list[0].Page} but no longer exists");
                }
            }

            differences.Should().BeEmpty(
                "the tables in docs/Testing must hold exactly one row per live test: "
                + string.Join("; ", differences));
        }

        [Fact]
        public void EveryDocumentedTestClassStillExists()
        {
            var live = LiveTests();

            var stale = DocumentedSections().Keys.Where(name => !live.ContainsKey(name))
                .OrderBy(name => name, StringComparer.Ordinal).ToList();

            stale.Should().BeEmpty(
                "a documented test class that no longer exists misleads the reader and the retrieval pipeline: "
                + string.Join(", ", stale));
        }
    }
}

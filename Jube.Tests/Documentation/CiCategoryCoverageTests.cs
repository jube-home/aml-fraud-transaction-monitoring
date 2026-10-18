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
using Xunit;

namespace Jube.Test.Documentation
{
    [Trait("Category", "Unit")]
    public sealed class CiCategoryCoverageTests
    {
        private static HashSet<string> GatedCategories()
        {
            var categories = new HashSet<string>(StringComparer.Ordinal);

            foreach (var workflow in Directory.GetFiles(
                         Path.Combine(Security.PenTest.PenTestPaths.RepositoryRoot, ".github", "workflows"), "*.yml"))
            {
                foreach (var line in File.ReadLines(workflow))
                {
                    var commentStart = line.IndexOf('#');
                    var live = commentStart < 0 ? line : line[..commentStart];

                    foreach (Match match in Regex.Matches(live, @"Category=([A-Za-z]+)"))
                    {
                        categories.Add(match.Groups[1].Value);
                    }
                }
            }

            return categories;
        }

        [Fact]
        public void EveryTestCategoryIsNamedByAWorkflowFilterSoEveryTestGates()
        {
            const string deliberatelyUngated =
                "AskJoobyLive needs a live inference backend and is run by hand, so it is the one category no "
                + "workflow filter names.";

            deliberatelyUngated.Should().NotBeNullOrWhiteSpace();

            var gated = GatedCategories();
            gated.Should().Contain(["Unit", "Service", "PenTest"]);

            var declared = typeof(CiCategoryCoverageTests).Assembly.GetTypes()
                .SelectMany(t => t.GetCustomAttributesData()
                    .Concat(t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        .SelectMany(m => m.GetCustomAttributesData()))
                    .Where(w => w.AttributeType.Name == "TraitAttribute"
                                && w.ConstructorArguments.Count == 2
                                && Equals(w.ConstructorArguments[0].Value, "Category"))
                    .Select(s => s.ConstructorArguments[1].Value as string))
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Select(s => s!)
                .ToHashSet(StringComparer.Ordinal);

            declared.Should().NotBeEmpty();

            declared.Remove("AskJoobyLive");

            declared.Except(gated).Should().BeEmpty(
                "a test class whose category no workflow filter names never runs in any gate; add the category to "
                + "the units-and-services slice filter in jube-build-end-to-end-tests.yml, or retire the category");
        }
    }
}
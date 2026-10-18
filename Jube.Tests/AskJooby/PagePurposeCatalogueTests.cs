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

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Service.Agent.PageAgentToolBinder;
using Jube.Test.Security.PenTest;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class PagePurposeCatalogueTests
    {
        private static readonly Regex pageContextCall =
            new(@"AskJoobySetPageContext\??\.?\(\s*""(?<pageName>[^""]+)""", RegexOptions.Compiled);

        private static string JavaScriptDirectory =>
            Path.Combine(PenTestPaths.JubeAppDirectory, "wwwroot", "js");

        private static string[] AllPageNamesSentByTheClient()
        {
            return
            [
                .. Directory.EnumerateFiles(JavaScriptDirectory, "*.js", SearchOption.AllDirectories)
                    .SelectMany(File.ReadLines)
                    .SelectMany(line => pageContextCall.Matches(line).Select(m => m.Groups["pageName"].Value))
                    .Distinct()
            ];
        }

        [Fact(Skip = "PagePurposeCatalogue is still a pilot covering a handful of representative pages; " +
                     "un-skip once every page name has an authored entry or an explicit exemption.")]
        public void EveryPageNameSentByTheClientIsCatalogedOrExplicitlyExempt()
        {
            var missing = AllPageNamesSentByTheClient()
                .Where(name => !PagePurposeCatalogue.KnownPageNames.Contains(name))
                .Where(name => !PagePurposeCatalogue.exemptPageNames.Contains(name))
                .OrderBy(name => name)
                .ToList();

            missing.Should().BeEmpty(
                "every pageName a page's own JS passes to AskJoobySetPageContext needs either a " +
                "PagePurposeCatalogue entry or an explicit exemption, so a newly added page is never " +
                "silently context-blind: " + string.Join(", ", missing));
        }

        [Fact]
        public void NoCataloguedPageNameIsAlsoListedAsExempt()
        {
            var overlap = PagePurposeCatalogue.KnownPageNames
                .Intersect(PagePurposeCatalogue.exemptPageNames)
                .OrderBy(name => name)
                .ToList();

            overlap.Should().BeEmpty(
                "a page name should be authored or exempt, never both: " + string.Join(", ", overlap));
        }

        [Fact]
        public void NoCataloguedPageNameHasGoneStaleAgainstTheClient()
        {
            var sentByClient = AllPageNamesSentByTheClient();

            var stale = PagePurposeCatalogue.KnownPageNames
                .Except(sentByClient)
                .OrderBy(name => name)
                .ToList();

            stale.Should().BeEmpty(
                "a catalogued page name that no JS file sends any more is dead weight, usually left " +
                "behind by a rename: " + string.Join(", ", stale));
        }
    }
}
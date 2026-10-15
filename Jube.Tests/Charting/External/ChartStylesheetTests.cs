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
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Jube.Charting;
using Jube.Test.Charting.Diagrams;
using Jube.Test.Charting.More;
using Xunit;

namespace Jube.Test.Charting.External
{
    public class ChartStylesheetTests
    {
        public const string GenerateEnvironmentVariable = "CHARTING_EXTERNAL_GOLDEN_GENERATE_DIR";
        private const string ResourcePrefix = "Jube.Test.Charting.External.Golden.";

        public static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Jube.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ??
                   throw new InvalidOperationException("Jube.sln was not found above the test output.");
        }

        public static string AppStylesheetPath()
        {
            return Path.Combine(RepositoryRoot(), "Jube.App", "wwwroot", "css", ChartStylesheet.FileName);
        }

        public static HashSet<string> SelectorClasses(string css)
        {
            var selectors = new List<string>();
            var depth = 0;
            var parentheses = 0;
            var current = new StringBuilder();

            foreach (var c in css)
            {
                switch (c)
                {
                    case '(':
                        parentheses++;
                        current.Append(c);
                        break;
                    case ')':
                        parentheses--;
                        current.Append(c);
                        break;
                    case '{' when parentheses == 0:
                        if (!current.ToString().TrimStart().StartsWith('@'))
                        {
                            selectors.Add(current.ToString());
                        }

                        current.Clear();
                        depth++;
                        break;
                    case '}' when parentheses == 0:
                        depth--;
                        current.Clear();
                        break;
                    default:
                        if (depth == 0 || depth == 1)
                        {
                            current.Append(c);
                        }

                        break;
                }
            }

            return
            [
                .. Regex.Matches(string.Join(' ', selectors), "\\.([A-Za-z][\\w-]*)")
                    .Select(m => m.Groups[1].Value)
            ];
        }

        [Fact]
        public void StylesheetMatchesTheApprovedGolden()
        {
            var actual = ChartStylesheet.Css();
            var directory = Environment.GetEnvironmentVariable(GenerateEnvironmentVariable);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, ChartStylesheet.FileName), actual);
                var app = AppStylesheetPath();
                Directory.CreateDirectory(Path.GetDirectoryName(app)!);
                File.WriteAllText(app, ChartStylesheet.Css(true));

                return;
            }

            using var stream =
                typeof(ChartStylesheetTests).Assembly.GetManifestResourceStream(ResourcePrefix +
                    ChartStylesheet.FileName);

            stream.Should()
                .NotBeNull("the approved stylesheet must exist; regenerate with " + GenerateEnvironmentVariable);

            using var reader = new StreamReader(stream!);
            actual.Should().Be(reader.ReadToEnd());
        }

        [Fact]
        public void TheCommittedAppStylesheetHasNotDriftedFromTheGenerator()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(GenerateEnvironmentVariable)))
            {
                return;
            }

            File.Exists(AppStylesheetPath()).Should()
                .BeTrue("the generated stylesheet must be committed to " + AppStylesheetPath());

            File.ReadAllText(AppStylesheetPath()).Should().Be(ChartStylesheet.Css(true),
                "regenerate with " + GenerateEnvironmentVariable + " set to the External Golden folder");
        }

        [Fact]
        public void TheLayoutIncludesTheStylesheetOnceWithTheVersion()
        {
            var layout =
                File.ReadAllText(Path.Combine(RepositoryRoot(), "Jube.App", "Pages", "Shared", "_Layout.cshtml"));

            Regex.Matches(layout, "jube-charting\\.css").Count.Should().Be(1);
            layout.Should().Contain("/css/jube-charting.css?v=@Jube.Charting.ChartStylesheet.Version");
        }

        [Fact]
        public void EveryClassUsedByEveryExternalSampleIsASelectorInTheStylesheet()
        {
            var known = SelectorClasses(ChartStylesheet.Css(true));
            var missing = new List<string>();

            foreach (var name in ExternalSamples.Names())
            {
                var document = XDocument.Parse(ExternalSamples.RenderExternal(name));

                foreach (var token in ExternalStyleTests.ClassTokens(document).Where(t => !known.Contains(t)))
                {
                    missing.Add(name + ": " + token);
                }
            }

            missing.Should().BeEmpty();
        }

        [Fact]
        public void EveryHostileTextChartOnlyUsesKnownClasses()
        {
            var known = SelectorClasses(ChartStylesheet.Css(true));

            var charts = InjectionTests.Charts("hostile <b> & text")
                .Concat(DiagramInjectionTests.Diagrams("hostile <b> & text"))
                .Concat(MoreInjectionTests.Charts("hostile <b> & text"));

            foreach (var chart in charts)
            {
                var document = XDocument.Parse(ExternalSamples.ToExternal(chart).Render());
                ExternalStyleTests.ClassTokens(document).Where(t => !known.Contains(t)).Should().BeEmpty();
            }
        }

        [Fact]
        public void VersionIsStableAndEightHexDigits()
        {
            ChartStylesheet.Version.Should().MatchRegex("^[0-9a-f]{8}$");
            ChartStylesheet.Version.Should().Be(ChartStylesheet.Version);
        }

        [Fact]
        public void MinifiedAndReadableVariantsHaveTheSameRules()
        {
            var compact = ChartStylesheet.Css(true);
            var readable = ChartStylesheet.Css();
            compact.Should().NotContain("\n");
            readable.Should().Contain("\n");
            compact.Count(c => c == '{').Should().Be(readable.Count(c => c == '{'));
            compact.Length.Should().BeLessThan(readable.Length);
            Regex.Replace(readable, "\\s+", string.Empty).Should().Be(Regex.Replace(compact, "\\s+", string.Empty));
        }

        [Fact]
        public void StylesheetIsDeterministic()
        {
            ChartStylesheet.Css().Should().Be(ChartStylesheet.Css());
            ChartStylesheet.Css(true).Should().Be(ChartStylesheet.Css(true));
        }

        [Fact]
        public void StylesheetContainsNoMarkupImportsOrUrls()
        {
            var css = ChartStylesheet.Css(true);
            css.Should().NotContain("<");
            css.Should().NotContain(">");
            css.Should().NotContain("@import");
            css.Should().NotContain("url(");
            css.Should().NotContain("expression(");
            css.Should().NotContain("javascript:");
        }

        [Fact]
        public void EveryCustomPropertyWithoutAFallbackIsDeclared()
        {
            var css = ChartStylesheet.Css(true);
            var declared = Regex.Matches(css, "(--[a-z0-9-]+):").Select(m => m.Groups[1].Value).ToHashSet();
            var used = Regex.Matches(css, "var\\((--[a-z0-9-]+)\\)").Select(m => m.Groups[1].Value).ToHashSet();
            used.Where(u => !declared.Contains(u) && u is not "--sc" and not "--dx" and not "--dy").Should().BeEmpty();
        }

        [Fact]
        public void ThemingHooksAreAllInTheSharedFile()
        {
            var css = ChartStylesheet.Css(true);
            css.Should().Contain(":root{");
            css.Should().Contain("@media (prefers-color-scheme:dark){:root:not([data-theme=\"light\"]){");
            css.Should().Contain(":root[data-theme=\"dark\"]{");
            css.Should().Contain(".jc[data-theme=\"dark\"]{");
            css.Should().Contain(".jc[data-theme=\"light\"]{");
            css.Should()
                .Contain(
                    "@media (prefers-reduced-motion:reduce){.jc,.jc *{transition:none!important;animation:none!important}}");
            css.Should().Contain(".jc:has(.mk:hover) .mk:not(:hover)");
            css.Should().Contain("--jc-surface:");
            css.Should().Contain("--jc-c0:");
        }

        [Fact]
        public void RulesNeverLeakBetweenChartsOnAPage()
        {
            var css = ChartStylesheet.Css(true);
            var rules = Regex.Matches(css, "([^{}@]+)\\{[^{}]*\\}").Select(m => m.Groups[1].Value);

            foreach (var selectors in rules)
            {
                foreach (var selector in TopLevel(selectors))
                {
                    (selector.StartsWith(".jc") || selector.StartsWith(":root")).Should().BeTrue(selector);
                }
            }
        }

        private static IEnumerable<string> TopLevel(string selectors)
        {
            var depth = 0;
            var current = new StringBuilder();

            foreach (var c in selectors)
            {
                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;
                }

                if (c == ',' && depth == 0)
                {
                    yield return current.ToString().Trim();
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            yield return current.ToString().Trim();
        }
    }
}
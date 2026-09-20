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

namespace Jube.Test.DynamicEnvironment
{
    [Collection("EvalExpressionRegistry")]
    public class EnvironmentVariablesDocumentationTests
    {
        private static readonly Regex row = new(@"^\|\s*(?<key>[A-Za-z][A-Za-z0-9_]*)\s*\|\s*(?<default>[^|]*?)\s*\|",
            RegexOptions.Compiled);

        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly HashSet<string> undocumentedKeys = new(StringComparer.OrdinalIgnoreCase);

        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly HashSet<string> defaultDiffers = new(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> environmentOnlyKeys =
            new(StringComparer.OrdinalIgnoreCase) { "CacheConnectionString" };

        private static readonly HashSet<string> suppliedByTheTestHost =
            new(StringComparer.OrdinalIgnoreCase) { "ConnectionString", "JWTKey", "PasswordHashingKey" };

        private static string DocumentPath(params string[] relative)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "docs")))
            {
                directory = directory.Parent;
            }

            var root = directory.Required().FullName;
            return Path.Combine([root, "docs", .. relative]);
        }

        private static Dictionary<string, string> CodeDefaults()
        {
            var environment = TestDynamicEnvironment.Create();
            var field = typeof(Jube.DynamicEnvironment.DynamicEnvironment).GetField("appSettings",
                BindingFlags.Instance | BindingFlags.NonPublic).Required();
            var settings = (Dictionary<string, string>)field.GetValue(environment).Required();

            return new Dictionary<string, string>(settings, StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, string> DocumentedDefaults()
        {
            var rows = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(DocumentPath("Concepts", "EnvironmentVariables", "index.md")))
            {
                var match = row.Match(line);
                if (!match.Success || match.Groups["key"].Value is "Variable" or "Name")
                {
                    continue;
                }

                rows[match.Groups["key"].Value] = match.Groups["default"].Value.Trim('`', ' ');
            }

            return rows;
        }

        private static bool Same(string documented, string actual)
        {
            var real = string.IsNullOrEmpty(actual) ? "null" : actual;
            return string.Equals(documented, real, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EveryConfigurationKeyInCodeIsDocumented()
        {
            var code = CodeDefaults();
            var documented = DocumentedDefaults();

            var missing = code.Keys.Where(key => !documented.ContainsKey(key) && !undocumentedKeys.Contains(key))
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToList();

            missing.Should().BeEmpty(
                "every key in DynamicEnvironment needs a row in docs/Concepts/EnvironmentVariables/index.md");
        }

        [Fact]
        public void EveryDocumentedKeyStillExistsInCode()
        {
            var code = CodeDefaults();

            var stale = DocumentedDefaults().Keys
                .Where(key => !code.ContainsKey(key) && !environmentOnlyKeys.Contains(key))
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToList();

            stale.Should().BeEmpty("a documented variable that the code no longer reads misleads operators");
        }

        [Fact]
        public void EveryDocumentedDefaultMatchesTheDefaultInCode()
        {
            var code = CodeDefaults();

            var differences = DocumentedDefaults()
                .Where(pair => code.TryGetValue(pair.Key, out var actual)
                               && !defaultDiffers.Contains(pair.Key) && !suppliedByTheTestHost.Contains(pair.Key)
                               && !Same(pair.Value, actual))
                .Select(pair => $"{pair.Key}: documented '{pair.Value}', code '{code[pair.Key]}'")
                .OrderBy(text => text, StringComparer.OrdinalIgnoreCase).ToList();

            differences.Should().BeEmpty();
        }

        [Fact]
        public void TheAllowListsHoldNoEntryThatIsNoLongerNeeded()
        {
            var code = CodeDefaults();
            var documented = DocumentedDefaults();

            undocumentedKeys.Where(key => documented.ContainsKey(key) || !code.ContainsKey(key)).Should().BeEmpty();
            defaultDiffers.Where(key => !documented.ContainsKey(key) || !code.ContainsKey(key) ||
                                        Same(documented[key], code[key])).Should().BeEmpty();
        }

        [Theory]
        [InlineData("EnableSanctionLoader", "False")]
        [InlineData("EnableSanctionLoaderChangePoll", "True")]
        [InlineData("SanctionLoaderChangePoll", "60000")]
        [InlineData("SanctionLoaderWait", "3600000")]
        public void TheSanctionLoaderKeysAreDocumentedWithTheirRealDefaults(string key, string expected)
        {
            CodeDefaults()[key].Should().Be(expected);
            DocumentedDefaults()[key].Should().Be(expected);
        }

        [Fact]
        public void TheSanctionsLoaderPageMentionsTheChangePollKeys()
        {
            var page = File.ReadAllText(DocumentPath("Configuration", "Sanctions", "SanctionsLoader", "index.md"));

            page.Should().Contain("EnableSanctionLoaderChangePoll").And.Contain("SanctionLoaderChangePoll");
        }
    }
}
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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Jube.Data.Reporting.Check;
using Jube.Service.DynamicSql;
using Xunit;

namespace Jube.Test.Reporting.Check
{
    [Trait("Category", "Unit")]
    public sealed class SqlCheckLocalisationTests
    {
        public static IEnumerable<object[]> Cultures()
        {
            return [["pl"], ["ar"], ["ka"], ["id"], ["sv"], ["ur"], ["hi"], ["fr"]];
        }

        private static Dictionary<string, string> Resx(string culture)
        {
            var directory = new System.IO.DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !System.IO.Directory.Exists(System.IO.Path.Combine(directory.FullName, "Jube.Resources")))
            {
                directory = directory.Parent;
            }

            return XDocument.Load(System.IO.Path.Combine(directory!.FullName, "Jube.Resources",
                    $"SqlCheckResources.{culture}.resx"))
                .Descendants("data")
                .ToDictionary(d => (string)d.Attribute("name")!, d => (string)d.Element("value")!);
        }

        [Theory]
        [MemberData(nameof(Cultures))]
        public void EveryCodeIsTranslatedWithTheSamePlaceholders(string culture)
        {
            var translations = Resx(culture);

            translations.Keys.Should().BeEquivalentTo(SqlCheckMessages.English.Keys);

            foreach (var (code, english) in SqlCheckMessages.English)
            {
                Placeholders(translations[code]).Should().BeEquivalentTo(Placeholders(english), $"{culture}/{code}");
                translations[code].Should().NotBe(english, $"{culture}/{code} must be translated");
            }
        }

        [Theory]
        [MemberData(nameof(Cultures))]
        public void TheRendererUsesTheTranslationForTheCurrentCulture(string culture)
        {
            var finding = new SqlFinding(SqlCheckMessages.UnknownColumn, ["abc"], null, null, null, null);

            var text = SqlFindingText.Render(finding, new CultureInfo(culture));

            text.Should().Contain("abc").And.NotBe(finding.English);
        }

        [Fact]
        public void TheRendererFallsBackToEnglishForAnUntranslatedCulture()
        {
            var finding = new SqlFinding(SqlCheckMessages.UnknownColumn, ["abc"], null, null, null, null);

            SqlFindingText.Render(finding, new CultureInfo("de")).Should().Be(finding.English);
        }

        private static IReadOnlyList<string> Placeholders(string text)
        {
            return Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Distinct().OrderBy(v => v).ToList();
        }
    }
}

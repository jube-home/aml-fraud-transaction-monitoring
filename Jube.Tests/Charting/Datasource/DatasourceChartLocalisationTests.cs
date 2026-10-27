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
using System.IO;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Jube.Data.Reporting;
using Jube.Data.Repository.Exceptions;
using Jube.Dto.Options;
using Jube.Resources;
using Jube.Service.Charts.Datasource;
using Jube.Service.Charts.Datasource.Models;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Charting.Datasource
{
    [Trait("Category", "Unit")]
    public sealed class DatasourceChartLocalisationTests
    {
        public static IEnumerable<object[]> Cultures()
        {
            return [["pl"], ["ar"]];
        }

        private static Dictionary<string, string> English()
        {
            var english = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var kind in DatasourceChartKinds.All)
            {
                english[$"Kind.{kind.Kind}.Name"] = kind.Name;
                english[$"Kind.{kind.Kind}.Description"] = kind.Description;
            }

            foreach (var role in Enum.GetValues<DatasourceChartRole>())
            {
                english[$"Role.{role}.Name"] = role.ToString();
                english[$"Role.{role}.Description"] = DatasourceChartText.RoleDescriptionEnglish(role);
            }

            foreach (var (code, text) in DatasourceMessages.English)
            {
                english[$"Message.{code}"] = text;
            }

            foreach (var term in DatasourceMessages.Terms)
            {
                english[term.Key] = term.English;
            }

            return english;
        }

        private static string ResourcesDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Jube.Resources")))
            {
                directory = directory.Parent;
            }

            return Path.Combine(directory!.FullName, "Jube.Resources");
        }

        private static Dictionary<string, string> Translations(string culture)
        {
            var set = new ResourceManager(typeof(DatasourceChartResources))
                .GetResourceSet(new CultureInfo(culture), true, false)!;

            return set.Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(e => (string)e.Key, e => (string)e.Value!, StringComparer.Ordinal);
        }

        private static Dictionary<string, string> Hashes(string culture)
        {
            return XDocument.Load(Path.Combine(ResourcesDirectory(), $"DatasourceChartResources.{culture}.resx"))
                .Descendants("data")
                .ToDictionary(d => (string)d.Attribute("name")!, d => (string?)d.Element("comment") ?? string.Empty,
                    StringComparer.Ordinal);
        }

        private static DatasourceChartText TextIn()
        {
            var factory = new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

            return new DatasourceChartText(factory.Create(typeof(DatasourceChartResources)));
        }

        private static T In<T>(string culture, Func<T> action)
        {
            var previous = CultureInfo.CurrentUICulture;

            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo(culture);

                return action();
            }
            finally
            {
                CultureInfo.CurrentUICulture = previous;
            }
        }

        [Fact]
        public void EveryKindRoleMessageAndTermKeyIsProducedByTheCode()
        {
            var english = English();

            english.Keys.Count(k => k.StartsWith("Kind.", StringComparison.Ordinal)).Should()
                .Be(DatasourceChartKinds.All.Count * 2);

            english.Keys.Count(k => k.StartsWith("Role.", StringComparison.Ordinal)).Should()
                .Be(Enum.GetValues<DatasourceChartRole>().Length * 2);

            english.Keys.Count(k => k.StartsWith("Message.", StringComparison.Ordinal)).Should()
                .Be(DatasourceMessages.English.Count);
        }

        [Theory]
        [MemberData(nameof(Cultures))]
        public void EveryTextTheCodeProducesHasATranslation(string culture)
        {
            var missing = English().Keys.Where(k => !Translations(culture).TryGetValue(k, out var v) ||
                                                    string.IsNullOrWhiteSpace(v)).ToList();

            missing.Should().BeEmpty(
                $"{culture} needs a resx entry (and its SourceHash comment) for each: {string.Join(", ", missing)}");
        }

        [Theory]
        [MemberData(nameof(Cultures))]
        public void NoTranslationIsLeftOverForTextThatNoLongerExists(string culture)
        {
            var orphans = Translations(culture).Keys.Except(English().Keys).ToList();

            orphans.Should().BeEmpty($"{culture} translates keys the code no longer produces: {string.Join(", ", orphans)}");
        }

        [Theory]
        [MemberData(nameof(Cultures))]
        public void EveryTranslationWasMadeFromTheEnglishItNowHas(string culture)
        {
            var hashes = Hashes(culture);

            var stale = English().Where(e => hashes.TryGetValue(e.Key, out var hash) &&
                                             hash != FormModelHash(e.Value)).Select(e => e.Key).ToList();

            stale.Should().BeEmpty(
                $"{culture} translated English that has since changed: re-read each, then set its resx comment to the new hash: {string.Join(", ", stale)}");
        }

        [Theory]
        [MemberData(nameof(Cultures))]
        public void TranslationsKeepEveryPlaceholderOfTheirEnglish(string culture)
        {
            var translations = Translations(culture);

            foreach (var (key, english) in English())
            {
                Placeholders(translations[key]).Should().Equal(Placeholders(english), key);
            }
        }

        [Theory]
        [InlineData("pl", "Wykres kolumnowy")]
        [InlineData("ar", "مخطط أعمدة")]
        public void AKindNameIsReturnedInThePageLanguage(string culture, string expected)
        {
            var kind = DatasourceChartKinds.Get(DatasourceDisplayKind.Column);

            In(culture, () => TextIn().KindName(kind)).Should().Be(expected);
        }

        [Fact]
        public void EnglishIsTheFallbackWhenThereIsNoTranslationForTheCulture()
        {
            var kind = DatasourceChartKinds.Get(DatasourceDisplayKind.Column);

            In("fr", () => TextIn().KindName(kind)).Should().Be("Column chart");
            DatasourceChartText.English.KindName(kind).Should().Be("Column chart");
        }

        [Fact]
        public void ARoleNameAndDescriptionAreTranslated()
        {
            In("pl", () => TextIn().RoleName(DatasourceChartRole.Latitude)).Should().Be("Szerokość geograficzna");
            In("pl", () => TextIn().RoleDescription(DatasourceChartRole.Latitude)).Should()
                .Be("Szerokość geograficzna w stopniach");
        }

        [Fact]
        public void AChartProblemIsDescribedInThePageLanguageWithItsTermsTranslatedToo()
        {
            var kind = DatasourceChartKinds.Get(DatasourceDisplayKind.Candlestick);
            var frame = new DatasourceFrame([new DatasourceColumn("Day", 1, "text", SeriesDataType.Text)], []);

            var thrown = Assert.Throws<DatasourceChartException>(() =>
                DatasourceChartMappingResolver.Resolve(kind, frame, null));

            var polish = In("pl", () => TextIn().Describe(thrown));

            polish.Should().Contain("Wykres świecowy").And.Contain("Otwarcie").And.Contain("kolumna liczbowa");
            polish.Should().NotContain("Candlestick").And.NotContain("numeric");
            thrown.Message.Should().Contain("Candlestick chart").And.Contain("numeric column");
        }

        [Fact]
        public void ADataLayerMessageIsDescribedInThePageLanguageToo()
        {
            var exception = SqlValidationFailedException.Coded(
                DatasourceMessages.ColumnDuplicate, "Amount");

            In("pl", () => TextIn().Describe(exception)).Should()
                .Be("Nazwa kolumny „Amount” występuje więcej niż raz. Nadaj każdej kolumnie własny alias.");

            exception.Message.Should().Contain("appears more than once");
        }

        [Fact]
        public void AParameterMessageTranslatesTheExpectedValueTermInsideIt()
        {
            var exception = ParameterValueInvalidException.Coded(
                DatasourceMessages.ParameterValue, "Threshold", DatasourceMessages.ExpectedWholeNumber);

            In("pl", () => TextIn().Describe(exception)).Should()
                .Be("Wartość parametru „Threshold” musi być liczbą całkowitą.");

            exception.Message.Should().Be("The value for \"Threshold\" must be a whole number.");
        }

        [Fact]
        public void AnUncodedExceptionKeepsItsOwnMessage()
        {
            DatasourceChartText.English.Describe(new InvalidOperationException("raw")).Should().Be("raw");
        }

        [Fact]
        public void EveryEnglishMessageTemplateFormatsWithItsArgumentCount()
        {
            foreach (var (code, template) in DatasourceMessages.English)
            {
                var count = Placeholders(template).Count == 0 ? 0 : Placeholders(template).Max(p => int.Parse(p[1..^1])) + 1;
                var arguments = Enumerable.Range(0, count).Select(i => (object)("v" + i)).ToList();

                DatasourceChartText.English.Message(code, arguments).Should().NotBeNullOrWhiteSpace(code);
            }
        }

        private static List<string> Placeholders(string text)
        {
            return Regex.Matches(text, @"\{\d\}").Select(m => m.Value).OrderBy(x => x, StringComparer.Ordinal).ToList();
        }

        private static string FormModelHash(string english)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(english.Trim()));

            return Convert.ToHexString(bytes, 0, 4).ToLowerInvariant();
        }
    }
}

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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Jube.CaseRisk.Journal;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseSearch;
using Jube.Resources;
using Jube.Service.Charts;
using Jube.Service.Query.CaseKeyJournal;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using static Jube.Test.CaseRisk.Journal.JournalEvents;

namespace Jube.Test.Charting
{
    [Trait("Category", "Unit")]
    public sealed class ChartResourceLocalisationTests
    {
        private static readonly string[] Cultures = ["fr", "pl", "sv", "id", "ka", "ar", "ur", "hi"];

        public static IEnumerable<object[]> Resources()
        {
            return ResourceTypes().Select(type => new object[] { type.Name });
        }

        public static IEnumerable<object[]> VelocityTitles()
        {
            return
            [
                ["fr", "Quand les transactions ont lieu"],
                ["pl", "Kiedy odbywają się transakcje"],
                ["sv", "När transaktioner sker"],
                ["id", "Kapan transaksi terjadi"],
                ["ka", "როდის ხდება ტრანზაქციები"],
                ["ar", "متى تحدث المعاملات"],
                ["ur", "لین دین کب ہوتے ہیں"],
                ["hi", "लेन-देन कब होते हैं"]
            ];
        }

        private static Type[] ResourceTypes()
        {
            return
            [
                typeof(StatisticsChartResources), typeof(CaseKeyJournalChartResources),
                typeof(CaseAnalyticsChartResources), typeof(BacktestChartResources),
                typeof(WatcherSummaryChartResources), typeof(ModelDependencyChartResources)
            ];
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

        private static Dictionary<string, string> Load(string name, string? culture)
        {
            var path = Path.Combine(ResourcesDirectory(), name + (culture == null ? "" : "." + culture) + ".resx");

            return XDocument.Load(path).Descendants("data")
                .ToDictionary(data => (string)data.Attribute("name")!, data => (string?)data.Element("value") ?? "");
        }

        private static List<string> Placeholders(string text)
        {
            return Regex.Matches(text, @"\{\d\}").Select(m => m.Value).OrderBy(x => x, StringComparer.Ordinal).ToList();
        }

        [Theory]
        [MemberData(nameof(Resources))]
        public void EveryConstantHasAnEnglishEntryAndEveryLocaleTranslatesExactlyTheSameKeys(string name)
        {
            var type = ResourceTypes().Single(candidate => candidate.Name == name);
            var constants = type.GetFields().Where(field => field.IsLiteral).Select(field => field.Name).ToList();
            var english = Load(name, null);

            english.Keys.Should().BeEquivalentTo(constants);

            foreach (var culture in Cultures)
            {
                var translated = Load(name, culture);

                translated.Keys.Should().BeEquivalentTo(english.Keys, culture);

                foreach (var (key, text) in translated)
                {
                    text.Should().NotBeNullOrWhiteSpace($"{culture} {key}");
                    Placeholders(text).Should().Equal(Placeholders(english[key]), $"{culture} {key}");
                }
            }
        }

        [Theory]
        [MemberData(nameof(VelocityTitles))]
        public void ACaseKeyJournalChartIsTitledInThePageLanguage(string culture, string expected)
        {
            var events = Enumerable.Range(0, 30).Select(i => Event(i + 1, Start.AddHours(i * 7), 100)).ToList();
            var report = new CaseKeyJournalAnalyticsDto { Velocity = VelocityCalculator.Compute(events) };
            var previous = CultureInfo.CurrentUICulture;

            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo(culture);
                var strings = new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                    NullLoggerFactory.Instance).Create(typeof(CaseKeyJournalChartResources));

                CaseKeyJournalChartRenderer.Render(CaseKeyJournalChart.Velocity, report, null, false, strings)
                    .Should().Contain(expected).And.NotContain("When transactions happen");
            }
            finally
            {
                CultureInfo.CurrentUICulture = previous;
            }
        }

        public static IEnumerable<object[]> RiskMatrixTitles()
        {
            return
            [
                ["fr", "Matrice des risques"],
                ["pl", "Macierz ryzyka"],
                ["sv", "Riskmatris"],
                ["id", "Matriks risiko"],
                ["ka", "რისკის მატრიცა"],
                ["ar", "مصفوفة المخاطر"],
                ["ur", "رسک میٹرکس"],
                ["hi", "जोखिम मैट्रिक्स"]
            ];
        }

        [Theory]
        [MemberData(nameof(RiskMatrixTitles))]
        public void ACaseAnalyticsRiskMatrixIsTitledInThePageLanguage(string culture, string expected)
        {
            var matrix = new CaseRiskMatrixDto
            {
                Total = 5,
                Cells = [new CaseRiskMatrixCellDto { LikelihoodBand = 4, Impact = 5, Cases = 2 },
                    new CaseRiskMatrixCellDto { LikelihoodBand = 0, Impact = 0, Cases = 3 }]
            };
            var previous = CultureInfo.CurrentUICulture;

            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo(culture);
                var strings = new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                    NullLoggerFactory.Instance).Create(typeof(CaseAnalyticsChartResources));

                CaseAnalyticsChartRenderer.RiskMatrix(matrix, false, strings).Should().Contain(expected)
                    .And.NotContain("Risk matrix").And.NotContain("not yet assessed");
            }
            finally
            {
                CultureInfo.CurrentUICulture = previous;
            }
        }

        [Fact]
        public void WithoutALocalizerTheEnglishTextIsUnchanged()
        {
            var events = Enumerable.Range(0, 30).Select(i => Event(i + 1, Start.AddHours(i * 7), 100)).ToList();
            var report = new CaseKeyJournalAnalyticsDto { Velocity = VelocityCalculator.Compute(events) };

            CaseKeyJournalChartRenderer.Render(CaseKeyJournalChart.Velocity, report, null, false)
                .Should().Contain("When transactions happen").And.Contain("Weekday").And.Contain("Mon");
        }
    }
}

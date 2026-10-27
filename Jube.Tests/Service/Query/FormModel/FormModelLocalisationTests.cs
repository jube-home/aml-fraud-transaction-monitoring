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
using System.Linq;
using System.IO;
using System.Resources;
using System.Xml.Linq;
using FluentAssertions;
using Jube.Dto.Forms;
using Jube.Dto.Query.FormModel;
using Jube.Resources;
using Jube.Service.Query.FormModel;
using Jube.Validations.EntityAnalysisModelTag;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.FormModel
{
    [Trait("Category", "Unit")]
    public sealed class FormModelLocalisationTests
    {
        private static readonly FormModelRegistry Registry =
            FormModelRegistry.Build(typeof(FormEndpointAttribute).Assembly,
                typeof(EntityAnalysisModelTagDtoValidator).Assembly);

        private static FormModelDto Model(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            return model!;
        }

        private static HashSet<string> ValidKeys()
        {
            var keys = new HashSet<string>();

            foreach (var area in Registry.Areas)
            {
                var model = Model(area);

                foreach (var group in model.Groups) keys.Add(FormModelKeys.Group(area, group.Path));
                foreach (var column in model.ListColumns) keys.Add(FormModelKeys.Column(area, column.Field));

                foreach (var field in model.Fields)
                {
                    keys.Add(FormModelKeys.Field(area, field.Name));

                    if (!string.IsNullOrWhiteSpace(field.Description))
                        keys.Add(FormModelKeys.Description(area, field.Name));

                    if (field.Help != null) keys.Add(FormModelKeys.Help(area, field.Name));
                    if (field.Placeholder != null) keys.Add(FormModelKeys.Placeholder(area, field.Name));

                    foreach (var option in field.Options) keys.Add(FormModelKeys.Option(option.Key));
                }
            }

            return keys;
        }

        private static IStringLocalizer RealLocalizer()
        {
            return new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance).Create(typeof(FormModelResources));
        }

        [Fact]
        public void ATranslatedLabelReplacesTheEnglishOneWhereverTheModelShowsText()
        {
            var tag = Model("EntityAnalysisModelTag");
            var strings = new DictionaryLocalizer(new Dictionary<string, string>
            {
                [FormModelKeys.Field("EntityAnalysisModelTag", "name")] = "Nazwa",
                [FormModelKeys.Group("EntityAnalysisModelTag", "Identity")] = "Tożsamość",
                [FormModelKeys.Column("EntityAnalysisModelTag", "name")] = "Nazwa kolumny"
            });

            var localised = FormModelLocaliser.Localise(tag, strings);

            localised.Fields.Single(field => field.Name == "name").Label.Should().Be("Nazwa");
            localised.Groups.Single(group => group.Path == "Identity").Label.Should().Be("Tożsamość");
            localised.ListColumns.Single(column => column.Field == "name").Title.Should().Be("Nazwa kolumny");
        }

        [Fact]
        public void AnOptionHelpAndPlaceholderAreTranslatedByTheirOwnKeys()
        {
            var model = Registry.Areas.Select(Model).First(candidate => candidate.Fields.Any(field => field.Options.Count > 0));
            var field = model.Fields.First(candidate => candidate.Options.Count > 0);
            var option = field.Options[0];
            var strings = new DictionaryLocalizer(new Dictionary<string, string>
            {
                [FormModelKeys.Option(option.Key)] = "Wybór"
            });

            var localised = FormModelLocaliser.Localise(model, strings);
            var translated = localised.Fields.Single(candidate => candidate.Name == field.Name);

            translated.Options[0].Label.Should().Be("Wybór");
            translated.Options[0].Value.Should().Be(option.Value);
            translated.Options.Skip(1).Select(candidate => candidate.Label)
                .Should().Equal(field.Options.Skip(1).Select(candidate => candidate.Label));
        }

        [Fact]
        public void AnUntranslatedLabelKeepsTheEnglishFromTheDto()
        {
            var tag = Model("EntityAnalysisModelTag");

            var localised = FormModelLocaliser.Localise(tag, new DictionaryLocalizer([]));

            localised.Fields.Select(field => field.Label).Should().Equal(tag.Fields.Select(field => field.Label));
            localised.Groups.Select(group => group.Label).Should().Equal(tag.Groups.Select(group => group.Label));
        }

        [Fact]
        public void LocalisingNeverChangesTheSharedEnglishModel()
        {
            var tag = Model("EntityAnalysisModelTag");
            var before = tag.Fields.Single(field => field.Name == "name").Label;

            FormModelLocaliser.Localise(tag, new DictionaryLocalizer(new Dictionary<string, string>
            {
                [FormModelKeys.Field("EntityAnalysisModelTag", "name")] = "Nazwa"
            }));

            tag.Fields.Single(field => field.Name == "name").Label.Should().Be(before);
        }

        [Fact]
        public void TheLocalisedModelIsBuiltOncePerCultureAndKeptApartFromOtherCultures()
        {
            var polish = new DictionaryLocalizer(new Dictionary<string, string>
            {
                [FormModelKeys.Field("EntityAnalysisModelTag", "name")] = "Nazwa"
            });

            Registry.TryGetLocalised("EntityAnalysisModelTag", "pl-test", polish, out var first).Should().BeTrue();
            Registry.TryGetLocalised("EntityAnalysisModelTag", "pl-test", new DictionaryLocalizer([]), out var again)
                .Should().BeTrue();
            Registry.TryGetLocalised("EntityAnalysisModelTag", "en-test", new DictionaryLocalizer([]), out var english)
                .Should().BeTrue();

            again.Should().BeSameAs(first);
            first!.Fields.Single(field => field.Name == "name").Label.Should().Be("Nazwa");
            english!.Fields.Single(field => field.Name == "name").Label.Should().Be("Name");
        }

        [Fact]
        public void AnUnknownAreaHasNoLocalisedModel()
        {
            Registry.TryGetLocalised("Nope", "pl", new DictionaryLocalizer([]), out var model).Should().BeFalse();

            model.Should().BeNull();
        }

        [Fact]
        public void EveryOptionKeyIsUniqueToItsSetAndMember()
        {
            var keys = Registry.Areas.Select(Model).SelectMany(model => model.Fields)
                .SelectMany(field => field.Options)
                .Select(option => option.Key)
                .Distinct()
                .ToList();

            keys.Should().NotBeEmpty();
            keys.Should().OnlyContain(key => key.Contains('.'));
        }

        [Theory]
        [InlineData("pl")]
        [InlineData("ar")]
        public void EveryTranslationKeyInTheResourcesNamesALabelTheModelsReallyHave(string culture)
        {
            var set = new ResourceManager(typeof(FormModelResources))
                .GetResourceSet(new CultureInfo(culture), true, false)!;
            var valid = ValidKeys();
            var entries = set.Cast<System.Collections.DictionaryEntry>().ToList();

            entries.Should().NotBeEmpty();
            entries.Select(entry => (string)entry.Key).Where(key => !valid.Contains(key))
                .Should().BeEmpty($"{culture} must not translate a label that no longer exists");
            entries.Select(entry => (string)entry.Value!).Where(string.IsNullOrWhiteSpace).Should().BeEmpty();
        }

        [Theory]
        [InlineData("pl", "Nazwa")]
        [InlineData("ar", "الاسم")]
        public void TheRealTranslationsReachTheTagModelThroughTheLocalizer(string culture, string expected)
        {
            var previous = CultureInfo.CurrentUICulture;

            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo(culture);

                var localised = FormModelLocaliser.Localise(Model("EntityAnalysisModelTag"), RealLocalizer());

                localised.Fields.Single(field => field.Name == "name").Label.Should().Be(expected);
                localised.ListColumns.Single(column => column.Field == "name").Title.Should().Be(expected);
                localised.Fields.Single(field => field.Name == "locked").Label.Should().NotBe("Locked");
            }
            finally
            {
                CultureInfo.CurrentUICulture = previous;
            }
        }

        [Fact]
        public void AnEnglishRequestGetsTheEnglishLabelsBecauseNoEnglishResourceOverridesThem()
        {
            var previous = CultureInfo.CurrentUICulture;

            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("en");

                var localised = FormModelLocaliser.Localise(Model("EntityAnalysisModelTag"), RealLocalizer());

                localised.Fields.Single(field => field.Name == "name").Label.Should().Be("Name");
            }
            finally
            {
                CultureInfo.CurrentUICulture = previous;
            }
        }

        private static string ResourcesDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Jube.Resources")))
                directory = directory.Parent;

            return Path.Combine(directory!.FullName, "Jube.Resources");
        }

        private static readonly string[] TranslatedKinds = ["description", "placeholder"];

        private static Dictionary<string, string> TextsInEnglish()
        {
            var english = new Dictionary<string, string>();

            foreach (var area in Registry.Areas)
            {
                foreach (var field in Model(area).Fields)
                {
                    if (!string.IsNullOrWhiteSpace(field.Description))
                        english[FormModelKeys.Description(area, field.Name)] = field.Description;

                    if (!string.IsNullOrWhiteSpace(field.Placeholder))
                        english[FormModelKeys.Placeholder(area, field.Name)] = field.Placeholder;
                }
            }

            return english;
        }

        private static Dictionary<string, string> TranslatedTexts(string culture)
        {
            return XDocument.Load(Path.Combine(ResourcesDirectory(), $"FormModelResources.{culture}.resx"))
                .Descendants("data")
                .Where(data => TranslatedKinds.Any(kind =>
                    ((string)data.Attribute("name")!).EndsWith("." + kind, StringComparison.Ordinal)))
                .ToDictionary(data => (string)data.Attribute("name")!, data => (string?)data.Element("comment") ?? "");
        }

        [Fact]
        public void ATranslatedPlaceholderReplacesTheEnglishOneAndAFieldWithNoneStaysWithNone()
        {
            var tag = Model("EntityAnalysisModelTag");
            var strings = new DictionaryLocalizer(new Dictionary<string, string>
            {
                [FormModelKeys.Placeholder("EntityAnalysisModelTag", "name")] = "np. Test"
            });

            var localised = FormModelLocaliser.Localise(tag, strings);

            localised.Fields.Single(field => field.Name == "name").Placeholder.Should().Be("np. Test");
            localised.Fields.Single(field => field.Name == "active").Placeholder.Should().BeNull();
        }

        [Fact]
        public void ATranslatedDescriptionReplacesTheEnglishOneAndAMissingOneKeepsIt()
        {
            var tag = Model("EntityAnalysisModelTag");
            var strings = new DictionaryLocalizer(new Dictionary<string, string>
            {
                [FormModelKeys.Description("EntityAnalysisModelTag", "name")] = "Opis"
            });

            var localised = FormModelLocaliser.Localise(tag, strings);

            localised.Fields.Single(field => field.Name == "name").Description.Should().Be("Opis");
            localised.Fields.Single(field => field.Name == "active").Description
                .Should().Be(tag.Fields.Single(field => field.Name == "active").Description);
        }

        [Theory]
        [InlineData("pl")]
        [InlineData("ar")]
        public void EveryTranslatedTextWasMadeFromTheEnglishItNowHas(string culture)
        {
            var english = TextsInEnglish();

            var stale = TranslatedTexts(culture)
                .Where(pair => english.TryGetValue(pair.Key, out var source) &&
                               pair.Value != FormModelKeys.SourceHash(source))
                .Select(pair => pair.Key)
                .ToList();

            stale.Should().BeEmpty(
                $"{culture} translated English text that has since changed: re-read each, then set its resx comment to the new FormModelKeys.SourceHash");
        }

        [Theory]
        [InlineData("pl")]
        [InlineData("ar")]
        public void TheUntranslatedTextsAreExactlyThoseInTheBaseline(string culture)
        {
            var translated = TranslatedTexts(culture).Keys.ToHashSet();
            var untranslated = TextsInEnglish().Keys.Where(key => !translated.Contains(key)).ToHashSet();
            var baseline = File.ReadAllLines(Path.Combine(ResourcesDirectory(), "FormModelTranslationBaseline.txt"))
                .Where(line => line.StartsWith(culture + " ", StringComparison.Ordinal))
                .Select(line => line[(culture.Length + 1)..])
                .ToHashSet();

            untranslated.Except(baseline).Should().BeEmpty(
                $"{culture} has new descriptions or placeholders with no translation: translate them (resx entry plus the SourceHash comment), or add them to FormModelTranslationBaseline.txt knowingly");
            baseline.Except(untranslated).Should().BeEmpty(
                $"{culture} translated these texts or they no longer exist: remove them from FormModelTranslationBaseline.txt");
        }
    }
}

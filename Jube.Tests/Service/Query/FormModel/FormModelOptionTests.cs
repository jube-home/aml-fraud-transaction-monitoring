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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using FluentAssertions;
using FluentValidation;
using Jube.Dto.Forms;
using Jube.Service.Query.FormModel;
using Jube.Validations.EntityAnalysisModelTag;
using Xunit;

namespace Jube.Test.Service.Query.FormModel
{
    [Trait("Category", "Unit")]
    public sealed class FormModelOptionTests
    {
        private static readonly string[] Vocabulary =
        [
            "default", "switch", "number", "date", "guid", "textarea", "select", "radio", "slider", "percent",
            "colour", "code"
        ];

        private static readonly FormModelRegistry Registry =
            FormModelRegistry.Build(typeof(FormEndpointAttribute).Assembly,
                typeof(EntityAnalysisModelTagDtoValidator).Assembly);

        private static readonly FormModelRegistry Fixture =
            FormModelRegistry.Build(typeof(FormModelOptionTests).Assembly, typeof(FormModelOptionTests).Assembly);

        public static IEnumerable<object[]> Areas()
        {
            return Registry.Areas.Order().Select(area => new object[] { area });
        }

        public enum Numbers
        {
            One = 1,
            Three = 3,
            TwentyOneItems = 21
        }

        public enum Letters
        {
            [Description("Alpha one"), EnumMember(Value = "a")]
            Alpha = 1,

            [EnumMember(Value = "b")] Beta = 2,

            Gamma = 3
        }

        [FormEndpoint("OptionFixture")]
        public sealed class OptionFixtureDto
        {
            [FormField(Widget = "select")]
            [Options(typeof(Numbers))]
            public int Number { get; set; }

            [FormField(Widget = "radio")]
            [Options(typeof(Numbers), Numbers.One, Numbers.Three)]
            public byte? Subset { get; set; }

            [FormField(Widget = "radio")]
            [Options(typeof(Letters), Letters.Alpha, Letters.Beta)]
            public string? Text { get; set; }

            [FormField(Widget = "radio")]
            [Options(typeof(Letters))]
            public char Character { get; set; }

            [FormField(Widget = "radio")]
            [Options(typeof(Numbers), "Nope")]
            public int Unknown { get; set; }
        }

        private static FormModelFixtureField Field(string name)
        {
            Fixture.TryGet("OptionFixture", out var model).Should().BeTrue();

            return new FormModelFixtureField(model!.Fields.Single(field => field.Name == name).Options
                .Select(option => (option.Value, option.Label)).ToList());
        }

        private sealed record FormModelFixtureField(List<(object Value, string Label)> Choices);

        [Fact]
        public void AnEnumGivesNumericChoicesInValueOrderLabelledByDescriptionOrTheHumanisedName()
        {
            Field("number").Choices.Should().Equal((1L, "One"), (3L, "Three"), (21L, "Twenty one items"));
        }

        [Fact]
        public void AnAllowedSubsetKeepsOnlyTheNamedMembers()
        {
            Field("subset").Choices.Select(choice => choice.Value).Should().Equal(1L, 3L);
        }

        [Fact]
        public void ATextPropertyTakesItsValuesFromEnumMemberAndItsLabelsFromDescription()
        {
            Field("text").Choices.Should().Equal(("a", "Alpha one"), ("b", "Beta"));
        }

        [Fact]
        public void AMemberWithNoStoredTextOnATextPropertyAndAnUnknownAllowedMemberAreRecordedNotThrown()
        {
            Fixture.Failures.Should().Contain(failure => failure.Contains("Gamma has no EnumMember value"));
            Fixture.Failures.Should().Contain(failure => failure.Contains("has no member Nope"));
            Field("character").Choices.Select(choice => choice.Value).Should().Equal("a", "b");
        }

        [Fact]
        public void TheRealOptionsKeepTheGapsAndTheStoredLettersTheOldPagesUsed()
        {
            Registry.TryGet("EntityAnalysisModelAbstractionRule", out var abstraction).Should().BeTrue();

            abstraction!.Fields.Single(field => field.Name == "searchFunctionTypeId").Options
                .Select(option => option.Value).Should().Equal(1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 11L, 12L, 13L, 14L, 15L, 16L);

            Registry.TryGet("EntityAnalysisModel", out var model).Should().BeTrue();

            model!.Fields.Single(field => field.Name == "referenceDatePayloadLocationTypeId").Options
                .Select(option => option.Value).Should().Equal(1L, 3L);
            model.Fields.Single(field => field.Name == "cacheTtlInterval").Options
                .Select(option => option.Value).Should().Equal("s", "n", "h", "d");

            Registry.TryGet("EntityAnalysisModelTtlCounter", out var counter).Should().BeTrue();

            counter!.Fields.Single(field => field.Name == "ttlCounterInterval").Options
                .Select(option => option.Value).Should().Equal("s", "n", "h", "d", "m", "y");
            counter.Fields.Single(field => field.Name == "resolutionInterval").Options
                .Select(option => option.Value).Should().Equal("n", "h", "d");

            Registry.TryGet("ExhaustiveSearchInstance", out var exhaustive).Should().BeTrue();

            exhaustive!.Fields.Single(field => field.Name == "calibrationMethod").Options
                .Select(option => option.Value).Should().Equal("automatic", "platt", "isotonic", "beta");
        }

        [Fact]
        public void EveryOptionSetRegistersWithoutAFailure()
        {
            Registry.Failures.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void EveryChoiceFieldHasOptionsOrALookup(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            model!.Fields.Where(field => field.Widget is "select" or "radio" && !field.ReadOnly)
                .Where(field => field.Options.Count == 0 && field.Lookup == null)
                .Select(field => field.Name)
                .Should().BeEmpty($"{area} choice fields must say where their choices come from");
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void EveryWidgetIsInTheClosedVocabulary(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            model!.Fields.Select(field => field.Widget).Where(widget => !Vocabulary.Contains(widget))
                .Should().BeEmpty($"{area} must use only the widgets the renderer knows");
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void OptionValuesAreUniqueAndEveryOptionIsLabelled(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            foreach (var field in model!.Fields.Where(field => field.Options.Count > 0))
            {
                field.Options.Select(option => option.Value.ToString()).Should().OnlyHaveUniqueItems(field.Name);
                field.Options.Where(option => string.IsNullOrWhiteSpace(option.Label)).Should().BeEmpty(field.Name);
                field.Widget.Should().BeOneOf("select", "radio");
            }
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void EveryNewDefaultOfAChoiceFieldIsOneOfItsOptions(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            var outside = model!.Fields.Where(field => field.Options.Count > 0 && field.NewDefault != null)
                .Where(field => field.Options.All(option => !string.Equals(option.Value.ToString(),
                    field.NewDefault!.ToString(), StringComparison.Ordinal)))
                .Select(field => $"{field.Name}={field.NewDefault}")
                .ToList();

            outside.Should().BeEmpty($"{area} defaults must be one of the offered choices");
        }

        private static readonly string[] DomainRules =
        [
            "PredicateValidator", "InclusiveBetweenValidator", "ExclusiveBetweenValidator", "GreaterThanValidator",
            "GreaterThanOrEqualValidator", "LessThanValidator", "LessThanOrEqualValidator", "EnumValidator",
            "RegularExpressionValidator"
        ];

        [Theory]
        [MemberData(nameof(Areas))]
        public void EveryOptionIsInsideTheValueDomainEveryValidatorRuleAllowsWhateverItsCondition(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            var dto = typeof(FormEndpointAttribute).Assembly.GetTypes()
                .Single(type => type.GetCustomAttribute<FormEndpointAttribute>()?.Area == area);
            var validators = typeof(EntityAnalysisModelTagDtoValidator).Assembly.GetTypes()
                .Where(type => type is { IsAbstract: false, IsClass: true, IsGenericTypeDefinition: false } &&
                               type.BaseType is { IsGenericType: true } baseType &&
                               baseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>) &&
                               baseType.GetGenericArguments()[0] == dto)
                .Select(ValidatorBuilder.Build)
                .ToList();
            var contextType = typeof(ValidationContext<>).MakeGenericType(dto);
            var rejected = new List<string>();

            foreach (var field in model!.Fields.Where(field => field.Options.Count > 0))
            {
                var name = char.ToUpperInvariant(field.Name[0]) + field.Name[1..];
                var property = dto.GetProperty(name)!;
                var underlying = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                foreach (var option in field.Options)
                {
                    var value = underlying == typeof(char)
                        ? ((string)option.Value)[0]
                        : underlying == typeof(string)
                            ? option.Value
                            : Convert.ChangeType(option.Value, underlying);

                    foreach (var validator in validators)
                    {
                        foreach (var rule in validator.CreateDescriptor().GetMembersWithValidators()
                                     .Where(member => member.Key == name)
                                     .SelectMany(member => member)
                                     .Select(pair => pair.Validator)
                                     .Where(candidate => DomainRules.Any(domain => candidate.Name == domain)))
                        {
                            var instance = Activator.CreateInstance(dto)!;

                            property.SetValue(instance, value);

                            var context = Activator.CreateInstance(contextType, instance)!;
                            var isValid = rule.GetType().GetMethod("IsValid");

                            if (isValid == null)
                            {
                                continue;
                            }

                            try
                            {
                                var accepted = (bool)isValid.Invoke(rule, [context, value])!;

                                if (!accepted)
                                {
                                    rejected.Add($"{name}={option.Value} refused by {rule.Name}");
                                }
                            }
                            catch (TargetInvocationException)
                            {
                            }
                        }
                    }
                }
            }

            rejected.Should().BeEmpty($"{area} validators must allow every option its DTO offers");
        }
    }
}

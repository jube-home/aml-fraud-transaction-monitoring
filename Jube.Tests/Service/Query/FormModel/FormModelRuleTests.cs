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
using System.Linq;
using System.Reflection;
using FluentAssertions;
using FluentValidation;
using Jube.Dto.Forms;
using Jube.Service.Query.FormModel;
using Jube.Validations.EntityAnalysisModelTag;
using Xunit;

namespace Jube.Test.Service.Query.FormModel
{
    [Trait("Category", "Unit")]
    public sealed class FormModelRuleTests
    {
        private static readonly FormModelRegistry Registry =
            FormModelRegistry.Build(typeof(FormEndpointAttribute).Assembly,
                typeof(EntityAnalysisModelTagDtoValidator).Assembly);

        public static IEnumerable<object[]> Areas()
        {
            return Registry.Areas.Order().Select(area => new object[] { area });
        }

        public sealed class Sample
        {
            public string? Value { get; set; }
            public int Count { get; set; }
            public bool Switch { get; set; }
        }

        public sealed class RuleBuilderHolder(AbstractValidator<Sample> validator)
        {
            public IRuleBuilderInitial<Sample, string?> Text => validator.RuleFor(p => p.Value);

            public IRuleBuilderInitial<Sample, int> Number => validator.RuleFor(p => p.Count);
        }

        private sealed class SampleValidator : AbstractValidator<Sample>
        {
            public SampleValidator(Action<RuleBuilderHolder> define)
            {
                define(new RuleBuilderHolder(this));
            }
        }

        private static MemberRules RulesOfNumber(Action<IRuleBuilderInitial<Sample, int>> define)
        {
            var validator = new SampleValidator(holder => define(holder.Number));

            return ValidatorRules.Analyse(validator)["Count"];
        }

        private static MemberRules RulesOfText(Action<IRuleBuilderInitial<Sample, string?>> define)
        {
            var validator = new SampleValidator(holder => define(holder.Text));

            return ValidatorRules.Analyse(validator)["Value"];
        }

        [Fact]
        public void AnUnconditionalNotEmptyRuleMakesAFieldRequired()
        {
            var rules = RulesOfText(rule => rule.NotEmpty());

            rules.Required.Should().BeTrue();
            rules.RequiredWhenConditional.Should().BeFalse();
        }

        [Fact]
        public void ANotEmptyRuleUnderAConditionIsNotRequiredButIsFlaggedConditional()
        {
            var rules = RulesOfText(rule => rule.NotEmpty().When(sample => sample.Switch));

            rules.Required.Should().BeFalse();
            rules.RequiredWhenConditional.Should().BeTrue();
        }

        [Fact]
        public void MaximumAndMinimumLengthAreCarriedAcrossEvenUnderACondition()
        {
            var rules = RulesOfText(rule => rule.MaximumLength(40).MinimumLength(2).When(sample => sample.Switch));

            rules.MaxLength.Should().Be(40);
            rules.MinLength.Should().Be(2);
        }

        [Fact]
        public void AnInclusiveBetweenRuleGivesBothLimits()
        {
            var rules = RulesOfNumber(rule => rule.InclusiveBetween(0, 1));

            (rules.Min, rules.Max).Should().Be((0d, 1d));
            (rules.ExclusiveMin, rules.ExclusiveMax).Should().Be((false, false));
        }

        [Fact]
        public void AnExclusiveBetweenRuleMarksBothLimitsExclusive()
        {
            var rules = RulesOfNumber(rule => rule.ExclusiveBetween(0, 10));

            (rules.ExclusiveMin, rules.ExclusiveMax).Should().Be((true, true));
        }

        [Fact]
        public void GreaterThanAndLessThanOrEqualGiveAMinimumAndAMaximum()
        {
            var rules = RulesOfNumber(rule => rule.GreaterThan(0).LessThanOrEqualTo(100));

            (rules.Min, rules.ExclusiveMin).Should().Be((0d, true));
            (rules.Max, rules.ExclusiveMax).Should().Be((100d, false));
        }

        [Fact]
        public void ARangeUnderAConditionIsStillCarriedBecauseItBoundsTheValueWhenUsed()
        {
            var rules = RulesOfNumber(rule => rule.InclusiveBetween(0, 1).When(sample => sample.Switch));

            (rules.Min, rules.Max).Should().Be((0d, 1d));
        }

        [Fact]
        public void TheTightestOfSeveralLimitsWins()
        {
            var rules = RulesOfNumber(rule => rule.GreaterThanOrEqualTo(0).GreaterThanOrEqualTo(5).LessThan(50)
                .LessThanOrEqualTo(20));

            rules.Min.Should().Be(5d);
            rules.Max.Should().Be(20d);
        }

        [Fact]
        public void MergingTwoValidatorsOfTheSameDtoKeepsTheTighterLimitAndAnyRequired()
        {
            var merged = ValidatorRules.Merge(
                new MemberRules(false, true, null, 100, 0, 10, false, false, ["^a$"]),
                new MemberRules(true, false, 2, 50, 1, 20, true, false, ["^a$", "^b$"]));

            merged.Required.Should().BeTrue();
            merged.RequiredWhenConditional.Should().BeTrue();
            (merged.MinLength, merged.MaxLength).Should().Be((2, 50));
            (merged.Min, merged.ExclusiveMin).Should().Be((1d, true));
            merged.Max.Should().Be(10d);
            merged.Patterns.Should().Equal("^a$", "^b$");
        }

        [Theory]
        [InlineData(@"^#[0-9A-Fa-f]{6}$", @"^#[0-9A-Fa-f]{6}$")]
        [InlineData(@"\A[A-Za-z0-9._-]+\z", @"^[A-Za-z0-9._-]+$")]
        [InlineData(@"^(?!.*\.\.)[A-Za-z0-9_.\- ]{0,256}$", @"^(?!.*\.\.)[A-Za-z0-9_.\- ]{0,256}$")]
        [InlineData(@"[A-Z]+", null)]
        [InlineData(@"^abc", null)]
        [InlineData(@"abc$", null)]
        [InlineData(@"^a\$", null)]
        [InlineData(@"(?i)^abc$", null)]
        [InlineData(@"^\p{L}+$", null)]
        [InlineData(@"^(?>a+)$", null)]
        [InlineData(@"^a++$", null)]
        public void OnlyAnAnchoredWholeValuePatternWithNoDotNetOnlySyntaxIsTranslated(string expression,
            string? expected)
        {
            WholeValuePattern.Translate(expression).Should().Be(expected);
        }

        [Fact]
        public void AMatchesRuleOnAFieldReachesTheModelAsAPattern()
        {
            var rules = RulesOfText(rule => rule.Matches("^#[0-9A-Fa-f]{6}$").When(sample => sample.Switch));

            rules.Patterns.Should().Equal("^#[0-9A-Fa-f]{6}$");
            RulesOfText(rule => rule.Matches("[A-Z]+")).Patterns.Should().BeEmpty();

            Registry.TryGet("CaseWorkflowStatus", out var model).Should().BeTrue();

            model!.Fields.Single(field => field.Name == "foreColor").Patterns.Should().Equal("^#[0-9A-Fa-f]{6}$");
        }

        [Fact]
        public void AnIntegerFieldGetsItsExclusiveMinimumAdjustedToTheFirstAllowedValue()
        {
            Registry.TryGet("EntityAnalysisModel", out var model).Should().BeTrue();

            var threshold = model!.Fields.Single(field => field.Name == "logsWarnThresholdMilliseconds");

            threshold.Min.Should().Be(1d);
            threshold.ExclusiveMin.Should().BeFalse();
            threshold.Max.Should().BeNull();
            threshold.Widget.Should().Be("number");
        }

        [Fact]
        public void ARangeFromTheValidatorsReachesTheSliderAndAnUnboundedFieldIsNotOne()
        {
            Registry.TryGet("EntityAnalysisModelActivationRule", out var model).Should().BeTrue();

            var sample = model!.Fields.Single(field => field.Name == "activationSample");

            (sample.Min, sample.Max).Should().Be((0d, 1d));
            sample.Widget.Should().Be("slider");

            Registry.TryGet("ExhaustiveSearchInstance", out var exhaustive).Should().BeTrue();

            var factor = exhaustive!.Fields.Single(field => field.Name == "bootstrapWeightFactor");

            (factor.Min, factor.Max).Should().Be((0d, 1d));
        }

        [Fact]
        public void TheTagNameIsRequiredAndLimitedToTheValidatorsLength()
        {
            Registry.TryGet("EntityAnalysisModelTag", out var model).Should().BeTrue();

            var name = model!.Fields.Single(field => field.Name == "name");

            name.Required.Should().BeTrue();
            name.MaxLength.Should().Be(256);
        }

        [Fact]
        public void EveryValidatorOfAFormDtoBuildsWithoutItsServices()
        {
            Registry.Failures.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void EverySliderAndPercentFieldHasARangeFromAValidator(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            model!.Fields.Where(field => field.Widget is "slider" or "percent")
                .Where(field => field.Min == null || field.Max == null)
                .Select(field => field.Name)
                .Should().BeEmpty($"{area} sliders must be bounded by a validator range");
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void EveryRegularExpressionRuleOnAFormFieldIsCarriedToTheModel(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            var dto = typeof(FormEndpointAttribute).Assembly.GetTypes()
                .Single(type => type.GetCustomAttribute<FormEndpointAttribute>()?.Area == area);
            var dropped = new List<string>();

            foreach (var validator in typeof(EntityAnalysisModelTagDtoValidator).Assembly.GetTypes()
                         .Where(type => type is { IsAbstract: false, IsClass: true, IsGenericTypeDefinition: false } &&
                                        type.BaseType is { IsGenericType: true } baseType &&
                                        baseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>) &&
                                        baseType.GetGenericArguments()[0] == dto))
            {
                var descriptor = ValidatorBuilder.Build(validator).CreateDescriptor();

                foreach (var member in descriptor.GetMembersWithValidators())
                {
                    if (string.IsNullOrEmpty(member.Key))
                    {
                        continue;
                    }

                    var field = model!.Fields.SingleOrDefault(candidate => string.Equals(candidate.Name,
                        char.ToLowerInvariant(member.Key[0]) + member.Key[1..], StringComparison.Ordinal));

                    if (field == null)
                    {
                        continue;
                    }

                    foreach (var regular in member.Select(pair => pair.Validator)
                                 .OfType<FluentValidation.Validators.IRegularExpressionValidator>())
                    {
                        var carried = WholeValuePattern.Translate(regular.Expression);

                        if (carried == null || !field.Patterns.Contains(carried))
                        {
                            dropped.Add($"{member.Key}: {regular.Expression}");
                        }
                    }
                }
            }

            dropped.Should().BeEmpty(
                $"{area} must write each regular expression as an anchored whole-value pattern the model can carry");
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void EveryConditionalRequiredRuleIsDeclaredAsARequiredWhenOnTheDto(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            var dto = typeof(FormEndpointAttribute).Assembly.GetTypes()
                .Single(type => type.GetCustomAttribute<FormEndpointAttribute>()?.Area == area);
            var validators = typeof(EntityAnalysisModelTagDtoValidator).Assembly.GetTypes()
                .Where(type => type is { IsAbstract: false, IsClass: true } &&
                               type.BaseType is { IsGenericType: true } baseType &&
                               baseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>) &&
                               baseType.GetGenericArguments()[0] == dto)
                .ToList();

            var undeclared = new List<string>();

            foreach (var validator in validators)
            {
                var instance = ValidatorBuilder.Build(validator);

                foreach (var (member, rules) in ValidatorRules.Analyse(instance))
                {
                    var field = model!.Fields.SingleOrDefault(candidate =>
                        string.Equals(candidate.Name, char.ToLowerInvariant(member[0]) + member[1..],
                            StringComparison.Ordinal));

                    if (field != null && rules.RequiredWhenConditional && !field.Required &&
                        field.RequiredWhen.Count == 0)
                    {
                        undeclared.Add(member);
                    }
                }
            }

            undeclared.Should().BeEmpty(
                $"{area} validators require these only under a condition, which the DTO must declare as RequiredWhen");
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void NoConditionCarriesAnOperatorAmongItsValues(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            var misplaced = model!.Fields
                .SelectMany(field => field.VisibleWhen.Concat(field.RequiredWhen).Concat(field.EnabledWhen)
                    .Where(condition => condition.Values.Any(value =>
                        value is ConditionOp || (value is string text &&
                                                 Enum.GetNames<ConditionOp>().Contains(text,
                                                     StringComparer.OrdinalIgnoreCase))))
                    .Select(condition => $"{field.Name} on {condition.Property}"))
                .ToList();

            misplaced.Should().BeEmpty(
                $"{area} must give an operator through Op = and never as a value, or the condition never holds");
        }
    }
}

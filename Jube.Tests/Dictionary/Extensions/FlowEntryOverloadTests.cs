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
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class FlowEntryOverloadTests
    {
        private static readonly Regex stepName = new("^(Match|Reject|Break|Require|Ensure)[A-Z]");

        private static readonly Type[] rawTypes = [typeof(double), typeof(int), typeof(string), typeof(DateTime), typeof(bool)];

        private static IEnumerable<MethodInfo> Extension()
        {
            return typeof(Jube.Dictionary.Extensions.Extensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false));
        }

        private static bool IsFlow(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Flow<>);
        }

        private static IEnumerable<MethodInfo> TypedFlowSteps()
        {
            return Extension().Where(m => !m.IsGenericMethodDefinition && stepName.IsMatch(m.Name) &&
                                          IsFlow(m.GetParameters()[0].ParameterType) &&
                                          rawTypes.Contains(m.GetParameters()[0].ParameterType.GetGenericArguments()[0]));
        }

        private static IEnumerable<MethodInfo> RawSteps()
        {
            return Extension().Where(m => !m.IsGenericMethodDefinition && stepName.IsMatch(m.Name) &&
                                          rawTypes.Contains(m.GetParameters()[0].ParameterType));
        }

        private static string Signature(MethodInfo method, Type receiver)
        {
            return method.Name + "(" + receiver.Name + "," +
                   string.Join(",", method.GetParameters().Skip(1).Select(p => p.ParameterType.Name)) + ")";
        }

        [Fact]
        public void TheGuardFindsAWorthwhileNumberOfSteps()
        {
            TypedFlowSteps().Count().Should().BeGreaterThan(900);
        }

        [Fact]
        public void EveryTypedFlowStepHasARawTypeOverloadWithTheSameShape()
        {
            var raw = RawSteps().ToDictionary(m => Signature(m, m.GetParameters()[0].ParameterType), m => m);
            var missing = new List<string>();
            var mismatched = new List<string>();

            foreach (var step in TypedFlowSteps())
            {
                var receiver = step.GetParameters()[0].ParameterType.GetGenericArguments()[0];
                var key = Signature(step, receiver);
                if (!raw.TryGetValue(key, out var overload))
                {
                    missing.Add(key);
                    continue;
                }

                var expectedParameters = step.GetParameters().Skip(1).Select(p => p.ParameterType);
                var actualParameters = overload.GetParameters().Skip(1).Select(p => p.ParameterType);
                if (!expectedParameters.SequenceEqual(actualParameters) || overload.ReturnType != step.ReturnType)
                {
                    mismatched.Add(key);
                }
            }

            missing.Should().BeEmpty();
            mismatched.Should().BeEmpty();
        }

        [Fact]
        public void EveryRawOverloadHasATypedFlowStepSoNoneIsOrphaned()
        {
            var flow = TypedFlowSteps().Select(m => Signature(m, m.GetParameters()[0].ParameterType.GetGenericArguments()[0]))
                .ToHashSet();

            var orphans = RawSteps().Select(m => Signature(m, m.GetParameters()[0].ParameterType))
                .Where(k => !flow.Contains(k)).ToList();

            orphans.Should().BeEmpty();
        }

        [Fact]
        public void TheRawAndFlowCountsAreEqual()
        {
            RawSteps().Count().Should().Be(TypedFlowSteps().Count());
        }

        [Fact]
        public void EveryRawOverloadReturnsAFlowOfItsOwnType()
        {
            foreach (var overload in RawSteps())
            {
                var receiver = overload.GetParameters()[0].ParameterType;
                overload.ReturnType.Should().Be(typeof(Flow<>).MakeGenericType(receiver), overload.Name);
            }
        }

        [Fact]
        public void NoRawOverloadNameCollidesWithAnInstanceOrStaticMemberOfItsReceiver()
        {
            var collisions = new List<string>();

            foreach (var overload in RawSteps())
            {
                var receiver = overload.GetParameters()[0].ParameterType;
                if (receiver.GetMember(overload.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                        .Length > 0)
                {
                    collisions.Add(receiver.Name + "." + overload.Name);
                }
            }

            collisions.Should().BeEmpty();
        }

        [Fact]
        public void NoRawOverloadNameCollidesWithAPlainExtensionOfTheSameReceiverAndShape()
        {
            var plain = Extension().Where(m => !IsFlow(m.GetParameters()[0].ParameterType))
                .Where(m => !stepName.IsMatch(m.Name) || !rawTypes.Contains(m.GetParameters()[0].ParameterType))
                .Select(m => m.Name + "/" + m.GetParameters()[0].ParameterType.Name)
                .ToHashSet();

            var clashes = RawSteps().Select(m => m.Name + "/" + m.GetParameters()[0].ParameterType.Name)
                .Where(plain.Contains).ToList();

            clashes.Should().BeEmpty();
        }

        [Theory]
        [InlineData("Lower", typeof(string))]
        [InlineData("Upper", typeof(string))]
        [InlineData("ParseDouble", typeof(string))]
        [InlineData("ParseInteger", typeof(string))]
        [InlineData("ParseDate", typeof(string))]
        [InlineData("ParseBoolean", typeof(string))]
        [InlineData("HourOfDay", typeof(DateTime))]
        [InlineData("ToZone", typeof(DateTime))]
        [InlineData("TextLength", typeof(string))]
        [InlineData("Trimmed", typeof(string))]
        public void EveryTransformerWithoutACollisionHasARawEntryOverloadReturningAFlow(string name, Type receiver)
        {
            var overload = Extension().SingleOrDefault(m => m.Name == name && m.GetParameters()[0].ParameterType == receiver);

            overload.Should().NotBeNull();
            IsFlow(overload!.ReturnType).Should().BeTrue();
        }

        [Theory]
        [InlineData("Plus")]
        [InlineData("Minus")]
        [InlineData("Times")]
        [InlineData("DividedBy")]
        public void PlainArithmeticOnADoubleReturnsANumberAndOnAFlowReturnsAFlow(string name)
        {
            var overloads = Extension().Where(m => m.Name == name).ToList();

            overloads.Single(m => m.GetParameters()[0].ParameterType == typeof(double)).ReturnType
                .Should().Be(typeof(double));
            IsFlow(overloads.Single(m => m.GetParameters()[0].ParameterType == typeof(Flow<double>)).ReturnType)
                .Should().BeTrue();
        }

        [Theory]
        [InlineData("Abs", typeof(double))]
        [InlineData("Round", typeof(double))]
        [InlineData("AgeInDays", typeof(DateTime))]
        [InlineData("FuzzyBestScore", typeof(string))]
        [InlineData("FuzzyMatchCount", typeof(string))]
        [InlineData("FuzzyBestMatch", typeof(string))]
        public void TransformersThatCollideWithAPlainMethodResolveToItAndReturnAPlainValue(string name, Type receiver)
        {
            var overloads = Extension().Where(m => m.Name == name && m.GetParameters()[0].ParameterType == receiver)
                .ToList();

            overloads.Should().NotBeEmpty();
            overloads.Should().OnlyContain(m => !IsFlow(m.ReturnType));
        }

        [Fact]
        public void DoubleRoundIsAStaticBclMemberWhichVbBindsBeforeAnExtension()
        {
            typeof(double).GetMember("Round", BindingFlags.Public | BindingFlags.Static).Should().NotBeEmpty();
        }

        [Fact]
        public void NoFlowMethodIsNamedAfterABareMemberOfStringSoTheRuleTokenAllowListStaysTight()
        {
            typeof(string).GetMember("Trim", BindingFlags.Public | BindingFlags.Instance).Should().NotBeEmpty();
            typeof(string).GetMember("Length", BindingFlags.Public | BindingFlags.Instance).Should().NotBeEmpty();
            Extension().Select(m => m.Name).Should().NotContain(["Trim", "Length"]);
            Extension().Select(m => m.Name).Should().Contain(["Trimmed", "TextLength"]);
        }

        private static readonly string[] ReviewedPreExistingCollisions =
        [
            "Abs",
            "Acos",
            "Asin",
            "Atan",
            "Atan2",
            "BigMul",
            "Ceiling",
            "Clamp",
            "CompareOrdinal",
            "Concat",
            "Contains",
            "Cos",
            "Cosh",
            "DaysInMonth",
            "DivRem",
            "Exp",
            "Floor",
            "Format",
            "FromOADate",
            "Intern",
            "IsDaylightSavingTime",
            "IsInfinity",
            "IsInterned",
            "IsLeapYear",
            "IsNaN",
            "IsNegativeInfinity",
            "IsNullOrEmpty",
            "IsNullOrWhiteSpace",
            "IsPositiveInfinity",
            "Join",
            "Lerp",
            "Log",
            "Log10",
            "Max",
            "Min",
            "Pow",
            "Replace",
            "Round",
            "Sign",
            "Sin",
            "Sinh",
            "Split",
            "Sqrt",
            "Tan",
            "Tanh",
            "ToBinary",
            "ToLongDateString",
            "ToLongTimeString",
            "ToLower",
            "ToLowerInvariant",
            "ToShortDateString",
            "ToShortTimeString",
            "ToString",
            "ToUpper",
            "ToUpperInvariant",
            "Truncate"
        ];

        [Fact]
        public void NoNewExtensionMethodNameCollidesWithAMemberOfAnyTypeARuleValueCanActuallyBe()
        {
            var ruleValueTypes = new[] { typeof(object), typeof(string), typeof(double), typeof(int), typeof(DateTime), typeof(bool) };
            var reachableMemberNames = ruleValueTypes
                .SelectMany(t => t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                .Select(m => m.Name)
                .ToHashSet();

            var collisions = Extension().Select(m => m.Name).Distinct()
                .Where(reachableMemberNames.Contains)
                .Except(ReviewedPreExistingCollisions)
                .ToList();

            collisions.Should().BeEmpty(
                "any such name becomes an allowed rule token for every value of that member's type, " +
                "not only the type the extension was written for, since the token allow-list is name-only -- " +
                "a genuinely new collision needs its own review, either a rename or an addition to " +
                nameof(ReviewedPreExistingCollisions));
        }

        [Fact]
        public void EveryReviewedPreExistingCollisionIsStillActuallyPresent()
        {
            var extensionNames = Extension().Select(m => m.Name).ToHashSet();

            extensionNames.Should().Contain(ReviewedPreExistingCollisions,
                "a name kept here after it stopped being an extension method name would hide a real narrowing " +
                "of " + nameof(NoNewExtensionMethodNameCollidesWithAMemberOfAnyTypeARuleValueCanActuallyBe));
        }

        [Fact]
        public void ScoreAndControlStepsAreDeliberatelyFlowOnly()
        {
            var flowOnly = new[]
            {
                "AddScore", "AddScoreWhen", "CapScore", "Against", "Label", "OtherwiseMatch", "OtherwiseReject",
                "Reset", "Fail", "MatchWhen", "RejectWhen", "BreakWhen", "RequireThat", "EnsureThat",
                "MatchIfScoreAtLeast", "RejectIfScoreBelow", "BreakIfScoreAtLeast", "RequireScoreAtLeast", "ToScore",
                "ToBoolean"
            };

            foreach (var name in flowOnly)
            {
                Extension().Where(m => m.Name == name)
                    .Should().OnlyContain(m => IsFlow(m.GetParameters()[0].ParameterType),
                        name + " must not take a raw receiver");
            }
        }

        [Fact]
        public void DoubleStartIsIdempotentAndKeepsState()
        {
            var once = 5d.Start().AddScore(2).Label("x");
            var twice = once.Start();

            twice.Value.Should().Be(once.Value);
            twice.Score.Should().Be(2d);
            twice.Label.Should().Be("x");
            twice.Outcome.Should().Be(once.Outcome);
        }

        [Fact]
        public void StartOnADecidedFlowDoesNotUndecideIt()
        {
            5d.Start().Match().Start().Outcome.Should().Be(FlowOutcome.Matched);
        }

        [Fact]
        public void StartOfAFlowIsTheSameTypeNotANestedFlow()
        {
            Flow<double> flow = 5d.Start().Start().Start();

            flow.Value.Should().Be(5d);
        }

        [Fact]
        public void EachRawTypeStartsAPipelineWithoutStart()
        {
            5d.MatchGreater(1).ToBoolean().Should().BeTrue();
            5.MatchGreater(1).ToBoolean().Should().BeTrue();
            "abc".MatchContains("b").ToBoolean().Should().BeTrue();
            new DateTime(2024, 3, 15).MatchIsWeekday().ToBoolean().Should().BeTrue();
            true.MatchIsTrue().ToBoolean().Should().BeTrue();
        }

        [Fact]
        public void EachPrefixWorksOnARawValue()
        {
            5d.MatchGreater(1).Outcome.Should().Be(FlowOutcome.Matched);
            5d.RejectGreater(1).Outcome.Should().Be(FlowOutcome.Rejected);
            5d.BreakGreater(1).Outcome.Should().Be(FlowOutcome.Broken);
            5d.RequireGreater(9).Outcome.Should().Be(FlowOutcome.Rejected);
            5d.EnsureGreater(9).Outcome.Should().Be(FlowOutcome.Broken);
            5d.RequireGreater(1).Outcome.Should().Be(FlowOutcome.Undecided);
        }

        [Fact]
        public void ARawEntryAndAnExplicitStartGiveIdenticalResults()
        {
            var implicitEntry = 5d.RequireInRange(1, 10).MatchGreater(3);
            var explicitEntry = 5d.Start().RequireInRange(1, 10).MatchGreater(3);

            implicitEntry.Outcome.Should().Be(explicitEntry.Outcome);
        }

        [Fact]
        public void AChainCanBeginWithATransformerOnARawValue()
        {
            "ABC".Lower().MatchEqual("abc").ToBoolean().Should().BeTrue();
            "  abc ".Trim().MatchEqual("abc").ToBoolean().Should().BeTrue();
            "abc".Start().TextLength().MatchEqual(3).ToBoolean().Should().BeTrue();
            "12".ParseDouble().MatchGreater(5).ToBoolean().Should().BeTrue();
            100d.Plus(5).MatchEqual(105).ToBoolean().Should().BeTrue();
            (-5d).Abs().MatchEqual(5).ToBoolean().Should().BeTrue();
            "abc".Length.MatchEqual(3).ToBoolean().Should().BeTrue();
            new DateTime(2024, 3, 15, 10, 0, 0).HourOfDay().MatchEqual(10).ToBoolean().Should().BeTrue();
            DateTime.Today.AddDays(-3).AgeInDays().MatchEqual(3).ToBoolean().Should().BeTrue();
        }

        [Fact]
        public void AgainstFollowsAnImplicitStart()
        {
            var flow = 5d.RequireGreater(1).Against("GB").MatchIn("GB", "IE");

            flow.Outcome.Should().Be(FlowOutcome.Matched);
        }

        [Fact]
        public void NullNaNAndBlankInputsFailClosedOnARawEntry()
        {
            double.NaN.MatchGreater(1).ToBoolean().Should().BeFalse();
            double.NaN.MatchLessOrEqual(1).ToBoolean().Should().BeFalse();
            ((string?)null).MatchEqual("a").ToBoolean().Should().BeFalse();
            ((string?)null).MatchContains("a").ToBoolean().Should().BeFalse();
            ((string?)null).RequireIsNotNullOrEmpty().Outcome.Should().Be(FlowOutcome.Rejected);
            "".RequireIsNotNullOrEmpty().Outcome.Should().Be(FlowOutcome.Rejected);
            ((string?)null).ParseDouble().Outcome.Should().Be(FlowOutcome.Errored);
            ((string?)null).Lower().Value.Should().BeNull();
        }

        [Fact]
        public void ARawEntryOnADecidedFlowIsNotPossibleSoAFlowReceiverUsesTheFlowStep()
        {
            var flow = 5d.MatchGreater(1);

            flow.MatchLess(0).Outcome.Should().Be(FlowOutcome.Matched);
        }
    }
}

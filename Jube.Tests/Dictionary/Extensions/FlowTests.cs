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
using FluentAssertions;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class FlowTests
    {
        private static Flow<double> InState(FlowOutcome outcome)
        {
            var flow = 1d.Start();
            return outcome == FlowOutcome.Undecided ? flow : flow.Decide(outcome);
        }

        [Fact]
        public void StartBeginsUndecidedWithNoScoreOrLabel()
        {
            var flow = 5d.Start();

            flow.Outcome.Should().Be(FlowOutcome.Undecided);
            flow.Value.Should().Be(5d);
            flow.Score.Should().Be(0d);
            flow.Label.Should().BeNull();
            flow.IsDecided.Should().BeFalse();
        }

        [Fact]
        public void StartWorksOnAnyType()
        {
            "text".Start().Value.Should().Be("text");
            true.Start().Value.Should().BeTrue();
            new DateTime(2024, 1, 1).Start().Value.Should().Be(new DateTime(2024, 1, 1));
            ((string?)null).Start().Value.Should().BeNull();
        }

        [Theory]
        [InlineData(FlowKind.Match, true, FlowOutcome.Matched)]
        [InlineData(FlowKind.Match, false, FlowOutcome.Undecided)]
        [InlineData(FlowKind.Reject, true, FlowOutcome.Rejected)]
        [InlineData(FlowKind.Reject, false, FlowOutcome.Undecided)]
        [InlineData(FlowKind.Break, true, FlowOutcome.Broken)]
        [InlineData(FlowKind.Break, false, FlowOutcome.Undecided)]
        [InlineData(FlowKind.Require, true, FlowOutcome.Undecided)]
        [InlineData(FlowKind.Require, false, FlowOutcome.Rejected)]
        [InlineData(FlowKind.Ensure, true, FlowOutcome.Undecided)]
        [InlineData(FlowKind.Ensure, false, FlowOutcome.Broken)]
        public void ApplyTransitionsAnUndecidedFlowAccordingToTheKindAndTheTest(FlowKind kind, bool test,
            FlowOutcome expected)
        {
            InState(FlowOutcome.Undecided).Apply(kind, test).Outcome.Should().Be(expected);
        }

        [Theory]
        [InlineData(FlowOutcome.Matched, FlowKind.Match, true)]
        [InlineData(FlowOutcome.Matched, FlowKind.Reject, true)]
        [InlineData(FlowOutcome.Matched, FlowKind.Break, true)]
        [InlineData(FlowOutcome.Matched, FlowKind.Require, false)]
        [InlineData(FlowOutcome.Matched, FlowKind.Ensure, false)]
        [InlineData(FlowOutcome.Rejected, FlowKind.Match, true)]
        [InlineData(FlowOutcome.Rejected, FlowKind.Break, true)]
        [InlineData(FlowOutcome.Rejected, FlowKind.Require, false)]
        [InlineData(FlowOutcome.Broken, FlowKind.Match, true)]
        [InlineData(FlowOutcome.Broken, FlowKind.Reject, true)]
        [InlineData(FlowOutcome.Broken, FlowKind.Ensure, false)]
        [InlineData(FlowOutcome.Errored, FlowKind.Match, true)]
        [InlineData(FlowOutcome.Errored, FlowKind.Require, false)]
        public void ADecidedFlowIsAbsorbingAndIgnoresEveryKindAndTest(FlowOutcome state, FlowKind kind, bool test)
        {
            InState(state).Apply(kind, test).Outcome.Should().Be(state);
        }

        [Theory]
        [InlineData(FlowOutcome.Matched)]
        [InlineData(FlowOutcome.Rejected)]
        [InlineData(FlowOutcome.Broken)]
        [InlineData(FlowOutcome.Errored)]
        public void ADecidedFlowSkipsEveryTypedStepWithoutEvaluatingItsOutcome(FlowOutcome state)
        {
            var flow = InState(state);

            flow.MatchGreater(0).Outcome.Should().Be(state);
            flow.RejectLess(100).Outcome.Should().Be(state);
            flow.BreakIsPositive().Outcome.Should().Be(state);
            flow.RequireEqual(999).Outcome.Should().Be(state);
            flow.EnsureIsZero().Outcome.Should().Be(state);
        }

        [Theory]
        [InlineData(FlowOutcome.Undecided, false)]
        [InlineData(FlowOutcome.Matched, true)]
        [InlineData(FlowOutcome.Rejected, false)]
        [InlineData(FlowOutcome.Broken, false)]
        [InlineData(FlowOutcome.Errored, false)]
        public void OnlyAMatchedFlowConvertsToTrue(FlowOutcome state, bool expected)
        {
            bool converted = InState(state);

            converted.Should().Be(expected);
            InState(state).ToBoolean().Should().Be(expected);
        }

        [Fact]
        public void ADecidedFlowStillConvertsFalseWhenReadAsAnIfCondition()
        {
            var rejected = InState(FlowOutcome.Rejected);

            (rejected ? "yes" : "no").Should().Be("no");
        }

        [Fact]
        public void TerminalStepsDecideAnUndecidedFlow()
        {
            1d.Start().Match().Outcome.Should().Be(FlowOutcome.Matched);
            1d.Start().Reject().Outcome.Should().Be(FlowOutcome.Rejected);
            1d.Start().Break().Outcome.Should().Be(FlowOutcome.Broken);
            1d.Start().Fail("boom").Outcome.Should().Be(FlowOutcome.Errored);
            1d.Start().Fail("boom").Label.Should().Be("boom");
        }

        [Fact]
        public void TerminalStepsDoNotOverwriteAnAlreadyDecidedFlow()
        {
            1d.Start().Match().Reject().Outcome.Should().Be(FlowOutcome.Matched);
            1d.Start().Reject().Match().Outcome.Should().Be(FlowOutcome.Rejected);
            1d.Start().Break().Match().Outcome.Should().Be(FlowOutcome.Broken);
            1d.Start().Match().Fail("late").Outcome.Should().Be(FlowOutcome.Matched);
            1d.Start().Match().Fail("late").Label.Should().BeNull();
        }

        [Fact]
        public void OtherwiseStepsOnlyActWhileTheFlowIsStillUndecided()
        {
            1d.Start().OtherwiseMatch().Outcome.Should().Be(FlowOutcome.Matched);
            1d.Start().OtherwiseReject().Outcome.Should().Be(FlowOutcome.Rejected);
            1d.Start().Reject().OtherwiseMatch().Outcome.Should().Be(FlowOutcome.Rejected);
            1d.Start().Match().OtherwiseReject().Outcome.Should().Be(FlowOutcome.Matched);
            1d.Start().Break().OtherwiseMatch().Outcome.Should().Be(FlowOutcome.Broken);
        }

        [Fact]
        public void ResetReturnsAnyDecidedFlowToUndecidedAndClearsScoreAndLabel()
        {
            var flow = 5d.Start().AddScore(3).Label("why").Reject().Reset();

            flow.Outcome.Should().Be(FlowOutcome.Undecided);
            flow.Score.Should().Be(0d);
            flow.Label.Should().BeNull();
            flow.Value.Should().Be(5d);
        }

        [Fact]
        public void ResetAllowsAnErroredFlowToBeDecidedAgain()
        {
            1d.Start().Fail("x").Reset().Match().Outcome.Should().Be(FlowOutcome.Matched);
        }

        [Fact]
        public void LabelIsCarriedThroughToTheDecision()
        {
            var flow = 5d.Start().Label("threshold").MatchGreater(1);

            flow.Outcome.Should().Be(FlowOutcome.Matched);
            flow.Label.Should().Be("threshold");
        }

        [Fact]
        public void AgainstSwitchesTheValueButKeepsOutcomeScoreAndLabel()
        {
            var flow = 5d.Start().AddScore(2).Label("l").Against("GB");

            flow.Value.Should().Be("GB");
            flow.Score.Should().Be(2d);
            flow.Label.Should().Be("l");
            flow.Outcome.Should().Be(FlowOutcome.Undecided);
        }

        [Fact]
        public void AgainstOnADecidedFlowStaysDecidedSoLaterStepsAreSkipped()
        {
            var flow = 5d.Start().RequireLess(1).Against("GB").MatchIn("GB");

            flow.Outcome.Should().Be(FlowOutcome.Rejected);
        }

        [Fact]
        public void AChainDropsOutAtTheFirstDecisionAndIgnoresEverythingAfter()
        {
            var flow = 150d.Start()
                .RequireGreater(100)
                .RequireLess(120)
                .MatchGreater(0);

            flow.Outcome.Should().Be(FlowOutcome.Rejected);
        }

        [Fact]
        public void ARequireChainFollowedByOtherwiseMatchMatchesOnlyWhenEveryGuardPasses()
        {
            150d.Start().RequireGreater(100).RequireLess(200).OtherwiseMatch().ToBoolean().Should().BeTrue();
            150d.Start().RequireGreater(100).RequireLess(120).OtherwiseMatch().ToBoolean().Should().BeFalse();
        }

        [Fact]
        public void ARuleWithNoTerminalStepFailsClosed()
        {
            150d.Start().RequireGreater(100).ToBoolean().Should().BeFalse();
        }

        [Theory]
        [InlineData(true, FlowOutcome.Matched)]
        [InlineData(false, FlowOutcome.Undecided)]
        public void MatchWhenAppliesAnyBooleanExpression(bool condition, FlowOutcome expected)
        {
            1d.Start().MatchWhen(condition).Outcome.Should().Be(expected);
        }

        [Theory]
        [InlineData(true, FlowOutcome.Rejected)]
        [InlineData(false, FlowOutcome.Undecided)]
        public void RejectWhenAppliesAnyBooleanExpression(bool condition, FlowOutcome expected)
        {
            1d.Start().RejectWhen(condition).Outcome.Should().Be(expected);
        }

        [Theory]
        [InlineData(true, FlowOutcome.Broken)]
        [InlineData(false, FlowOutcome.Undecided)]
        public void BreakWhenAppliesAnyBooleanExpression(bool condition, FlowOutcome expected)
        {
            1d.Start().BreakWhen(condition).Outcome.Should().Be(expected);
        }

        [Theory]
        [InlineData(true, FlowOutcome.Undecided)]
        [InlineData(false, FlowOutcome.Rejected)]
        public void RequireThatGuardsOnAnyBooleanExpression(bool condition, FlowOutcome expected)
        {
            1d.Start().RequireThat(condition).Outcome.Should().Be(expected);
        }

        [Theory]
        [InlineData(true, FlowOutcome.Undecided)]
        [InlineData(false, FlowOutcome.Broken)]
        public void EnsureThatGuardsOnAnyBooleanExpression(bool condition, FlowOutcome expected)
        {
            1d.Start().EnsureThat(condition).Outcome.Should().Be(expected);
        }

        [Fact]
        public void BooleanExpressionsInteroperateWithTheExistingExtensionLibrary()
        {
            var email = "a@example.com";

            email.Start().RequireThat(email.IsValidEmail()).MatchEndsWith("example.com").ToBoolean().Should().BeTrue();
            "nope".Start().RequireThat("nope".IsValidEmail()).MatchEndsWith("nope").ToBoolean().Should().BeFalse();
        }

        [Fact]
        public void AddScoreAccumulatesWhileUndecided()
        {
            1d.Start().AddScore(1.5).AddScore(2).Score.Should().Be(3.5d);
            1d.Start().AddScore(5).AddScore(-2).Score.Should().Be(3d);
        }

        [Fact]
        public void AddScoreIsIgnoredOnceDecided()
        {
            1d.Start().AddScore(1).Match().AddScore(10).Score.Should().Be(1d);
        }

        [Theory]
        [InlineData(true, 3d)]
        [InlineData(false, 0d)]
        public void AddScoreWhenOnlyAddsWhenTheConditionHolds(bool condition, double expected)
        {
            1d.Start().AddScoreWhen(condition, 3).Score.Should().Be(expected);
        }

        [Fact]
        public void AddScoreWhenIsIgnoredOnceDecided()
        {
            1d.Start().Reject().AddScoreWhen(true, 3).Score.Should().Be(0d);
        }

        [Theory]
        [InlineData(2, 5, 2)]
        [InlineData(7, 5, 5)]
        [InlineData(5, 5, 5)]
        public void CapScoreLimitsTheAccumulatedScore(double score, double maximum, double expected)
        {
            1d.Start().AddScore(score).CapScore(maximum).Score.Should().Be(expected);
        }

        [Theory]
        [InlineData(0.75, FlowOutcome.Matched)]
        [InlineData(1.0, FlowOutcome.Undecided)]
        public void MatchIfScoreAtLeastDecidesFromTheAccumulatedScore(double threshold, FlowOutcome expected)
        {
            1d.Start().AddScore(0.75).MatchIfScoreAtLeast(threshold).Outcome.Should().Be(expected);
        }

        [Theory]
        [InlineData(0.5, FlowOutcome.Undecided)]
        [InlineData(0.75, FlowOutcome.Undecided)]
        [InlineData(1.0, FlowOutcome.Rejected)]
        public void RejectIfScoreBelowDecidesFromTheAccumulatedScore(double threshold, FlowOutcome expected)
        {
            1d.Start().AddScore(0.75).RejectIfScoreBelow(threshold).Outcome.Should().Be(expected);
        }

        [Theory]
        [InlineData(0.75, FlowOutcome.Broken)]
        [InlineData(1.0, FlowOutcome.Undecided)]
        public void BreakIfScoreAtLeastDecidesFromTheAccumulatedScore(double threshold, FlowOutcome expected)
        {
            1d.Start().AddScore(0.75).BreakIfScoreAtLeast(threshold).Outcome.Should().Be(expected);
        }

        [Theory]
        [InlineData(0.75, FlowOutcome.Undecided)]
        [InlineData(1.0, FlowOutcome.Rejected)]
        public void RequireScoreAtLeastGuardsOnTheAccumulatedScore(double threshold, FlowOutcome expected)
        {
            1d.Start().AddScore(0.75).RequireScoreAtLeast(threshold).Outcome.Should().Be(expected);
        }

        [Fact]
        public void ToScoreExposesTheScoreForNumericRuleReturnTypes()
        {
            1d.Start().AddScore(2.5).ToScore().Should().Be(2.5d);
        }

        [Fact]
        public void StateProbesReflectTheOutcome()
        {
            1d.Start().IsUndecided().Should().BeTrue();
            1d.Start().Match().IsMatched().Should().BeTrue();
            1d.Start().Reject().IsRejected().Should().BeTrue();
            1d.Start().Break().IsBroken().Should().BeTrue();
            1d.Start().Fail("x").IsErrored().Should().BeTrue();

            1d.Start().Match().IsRejected().Should().BeFalse();
            1d.Start().Match().IsUndecided().Should().BeFalse();
            1d.Start().IsMatched().Should().BeFalse();
        }

        [Theory]
        [InlineData("12.5", 12.5)]
        [InlineData("-3", -3)]
        [InlineData("1e3", 1000)]
        [InlineData(" 7 ", 7)]
        public void ParseDoubleReadsInvariantNumbers(string text, double expected)
        {
            var flow = text.Start().ParseDouble();

            flow.Outcome.Should().Be(FlowOutcome.Undecided);
            flow.Value.Should().Be(expected);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("1,5")]
        public void ParseDoubleErrorsRatherThanThrowingOnBadInput(string? text)
        {
            var flow = text.Start().ParseDouble();

            flow.Outcome.Should().Be(FlowOutcome.Errored);
            flow.Label.Should().NotBeNullOrEmpty();
            flow.ToBoolean().Should().BeFalse();
        }

        [Fact]
        public void ParseDoubleIsCultureInvariant()
        {
            var original = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

                "1.5".Start().ParseDouble().Value.Should().Be(1.5d);
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void AnErroredParseStopsEveryFollowingStep()
        {
            var flow = "abc".Start().ParseDouble().MatchGreater(0).OtherwiseMatch();

            flow.Outcome.Should().Be(FlowOutcome.Errored);
            flow.ToBoolean().Should().BeFalse();
        }

        [Fact]
        public void AParseAfterADecisionStaysDecided()
        {
            var flow = "abc".Start().Reject().ParseDouble();

            flow.Outcome.Should().Be(FlowOutcome.Rejected);
        }

        [Theory]
        [InlineData("42", 42)]
        [InlineData("-1", -1)]
        public void ParseIntegerReadsWholeNumbers(string text, int expected)
        {
            text.Start().ParseInteger().Value.Should().Be(expected);
        }

        [Theory]
        [InlineData("4.2")]
        [InlineData("x")]
        [InlineData(null)]
        [InlineData("99999999999")]
        public void ParseIntegerErrorsOnAnythingThatIsNotAnInt(string? text)
        {
            text.Start().ParseInteger().Outcome.Should().Be(FlowOutcome.Errored);
        }

        [Fact]
        public void ParseDateReadsInvariantDates()
        {
            var flow = "2024-03-15T10:30:00".Start().ParseDate();

            flow.Outcome.Should().Be(FlowOutcome.Undecided);
            flow.Value.Should().Be(new DateTime(2024, 3, 15, 10, 30, 0));
        }

        [Theory]
        [InlineData("not a date")]
        [InlineData("")]
        [InlineData(null)]
        public void ParseDateErrorsOnAnythingThatIsNotADate(string? text)
        {
            text.Start().ParseDate().Outcome.Should().Be(FlowOutcome.Errored);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("False", false)]
        public void ParseBooleanReadsBooleans(string text, bool expected)
        {
            text.Start().ParseBoolean().Value.Should().Be(expected);
        }

        [Theory]
        [InlineData("yes")]
        [InlineData("1")]
        [InlineData(null)]
        public void ParseBooleanErrorsOnAnythingElse(string? text)
        {
            text.Start().ParseBoolean().Outcome.Should().Be(FlowOutcome.Errored);
        }

        [Fact]
        public void ParseThenTestReadsLeftToRight()
        {
            "150".Start().ParseDouble().RequireGreater(100).OtherwiseMatch().ToBoolean().Should().BeTrue();
            "50".Start().ParseDouble().RequireGreater(100).OtherwiseMatch().ToBoolean().Should().BeFalse();
        }

        [Fact]
        public void NumericTransformersChangeTheValueAndKeepTheState()
        {
            (-5d).Start().AddScore(1).Abs().Value.Should().Be(5d);
            (-5d).Start().AddScore(1).Abs().Score.Should().Be(1d);
            1.2345.Start().Round(2).Value.Should().Be(1.23d);
            1d.Start().Plus(2).Value.Should().Be(3d);
            5d.Start().Minus(2).Value.Should().Be(3d);
            5d.Start().Times(2).Value.Should().Be(10d);
            10d.Start().DividedBy(4).Value.Should().Be(2.5d);
        }

        [Fact]
        public void DividedByZeroErrorsRatherThanProducingInfinity()
        {
            var flow = 10d.Start().DividedBy(0);

            flow.Outcome.Should().Be(FlowOutcome.Errored);
            flow.ToBoolean().Should().BeFalse();
        }

        [Fact]
        public void DividedByOnADecidedFlowLeavesItUntouched()
        {
            10d.Start().Match().DividedBy(0).Outcome.Should().Be(FlowOutcome.Matched);
        }

        [Fact]
        public void TransformersOnADecidedFlowKeepItDecided()
        {
            (-5d).Start().Reject().Abs().Outcome.Should().Be(FlowOutcome.Rejected);
            "a".Start().Break().Upper().Outcome.Should().Be(FlowOutcome.Broken);
        }

        [Fact]
        public void StringTransformersAreNullSafeAndCultureInvariant()
        {
            "  Abc ".Start().Trim().Value.Should().Be("Abc");
            "Abc".Start().Lower().Value.Should().Be("abc");
            "Abc".Start().Upper().Value.Should().Be("ABC");
            "Abc".Start().Length().Value.Should().Be(3);
            ((string?)null).Start().Trim().Value.Should().BeNull();
            ((string?)null).Start().Lower().Value.Should().BeNull();
            ((string?)null).Start().Upper().Value.Should().BeNull();
            ((string?)null).Start().Length().Value.Should().Be(0);
        }

        [Fact]
        public void TransformerThenTestReadsLeftToRight()
        {
            " GB ".Start().Trim().Lower().MatchEqual("gb").ToBoolean().Should().BeTrue();
        }

        [Fact]
        public void DateTransformersProduceIntegers()
        {
            new DateTime(2024, 3, 15, 10, 30, 0).Start().HourOfDay().Value.Should().Be(10);
            DateTime.Today.AddDays(-3).Start().AgeInDays().Value.Should().Be(3);
        }

        [Fact]
        public void AFlowIsAValueTypeSoStepsNeverMutateAnEarlierFlow()
        {
            var start = 5d.Start();

            var matched = start.Match();

            start.Outcome.Should().Be(FlowOutcome.Undecided);
            matched.Outcome.Should().Be(FlowOutcome.Matched);
        }

        [Fact]
        public void TheFlowCanBeBranchedAndEachBranchIsIndependent()
        {
            var start = 5d.Start().AddScore(1);

            var high = start.AddScore(10);
            var low = start.AddScore(2);

            high.Score.Should().Be(11d);
            low.Score.Should().Be(3d);
            start.Score.Should().Be(1d);
        }

        [Fact]
        public void ApplyRejectsAnUnknownKind()
        {
            var act = () => 1d.Start().Apply((FlowKind)99, true);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}

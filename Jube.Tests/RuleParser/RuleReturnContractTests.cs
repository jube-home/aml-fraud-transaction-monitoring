using System;
using FluentAssertions;
using Jube.Parser;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class RuleReturnContractTests
    {
        [Theory]
        [InlineData(RuleParse.GatewayRule)]
        [InlineData(RuleParse.AbstractionRule)]
        [InlineData(RuleParse.InlineFunction)]
        public void BareExtensionEvaluationWithoutAssignmentIsRefusedForEveryRuleType(int ruleParseType)
        {
            var result = RuleParse.Execute("Payload.Amount.Start().MatchGreater(100)", ruleParseType,
                RuleParseTests.Environment(), TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeFalse();
            result.ClassName.Should().BeNull();

            result.ErrorSpans.Should().ContainSingle(e =>
                e.Line == 0 && e.Message.StartsWith("Line 1:") && e.Message.Contains("result is discarded"));
        }

        [Fact]
        public void TheRefusalPointsAtTheDiscardedExpressionWithItsOwnSpan()
        {
            var rule = "If (Payload.Amount > 0) Then\n   Payload.Amount.Start().MatchGreater(100)\nEnd If";

            var result = RuleParse.Execute(rule, RuleParse.GatewayRule, RuleParseTests.Environment(),
                TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeFalse();
            var span = result.ErrorSpans.Should().ContainSingle(e => e.Message.Contains("result is discarded")).Subject;
            span.Line.Should().Be(1);
            var wrapped = result.ParsedRuleText;
            var fragmentStart = wrapped.IndexOf("If (", StringComparison.Ordinal);
            span.Start.Should().BeGreaterThan(0);
            wrapped.Substring(fragmentStart + span.Start, span.Length).Should().EndWith("MatchGreater(100)");
        }

        [Fact]
        public void ARuleWithNoStatementsAtAllIsRefused()
        {
            var result = RuleParse.Execute(string.Empty, RuleParse.GatewayRule, RuleParseTests.Environment(),
                TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeFalse();
            result.ErrorSpans.Should().ContainSingle(e => e.Line == 0 && e.Message.Contains("never sets Matched"));
        }

        [Fact]
        public void ABareReturnCompiles()
        {
            var result = RuleParse.Execute("Return False", RuleParse.GatewayRule, RuleParseTests.Environment(),
                TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeTrue();
            result.ErrorSpans.Should().BeEmpty();
        }

        [Fact]
        public void APipelineAssignmentToMatchedCompiles()
        {
            var result = RuleParse.Execute("Matched = Payload.Amount.Start().MatchGreater(100)",
                RuleParse.GatewayRule, RuleParseTests.Environment(), TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeTrue();
            result.ErrorSpans.Should().BeEmpty();
        }

        [Fact]
        public void AConditionalPipelineAssignmentToMatchedCompiles()
        {
            var result = RuleParse.Execute("If (Payload.Amount > 100) Then\n   Matched = True\nEnd If",
                RuleParse.GatewayRule, RuleParseTests.Environment(), TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeTrue();
            result.ErrorSpans.Should().BeEmpty();
        }

        [Fact]
        public void AnAssignmentToAnotherNameDoesNotSatisfyTheContract()
        {
            var result = RuleParse.Execute("Dim Verdict As Boolean = Payload.Amount.Start().MatchGreater(100)",
                RuleParse.GatewayRule, RuleParseTests.Environment(), TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeFalse();
            result.ErrorSpans.Should().ContainSingle(e => e.Message.Contains("never sets Matched"));
        }

        [Fact]
        public void AReturnInsideAStringLiteralDoesNotSatisfyTheContract()
        {
            var result = RuleParse.Execute("Payload.Country.EqualsIgnoreCase(\"Return True\")",
                RuleParse.GatewayRule, RuleParseTests.Environment(), TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeFalse();
            result.ErrorSpans.Should().ContainSingle(e => e.Message.Contains("result is discarded"));
        }
    }
}
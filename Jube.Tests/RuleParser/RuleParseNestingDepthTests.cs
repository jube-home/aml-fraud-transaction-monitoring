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
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using Jube.Parser;
using Jube.Test.Infrastructure;
using Jube.Test.Security.Model;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class RuleParseNestingDepthTests
    {
        private static string ParenCalls(int depth)
        {
            return "If (" + string.Concat(Enumerable.Repeat("Abs(", depth)) + "1" + new string(')', depth) +
                   " > 0) Then\n   Return True\nEnd If";
        }

        private static string NotChain(int depth)
        {
            return "If (" + string.Concat(Enumerable.Repeat("Not ", depth)) + "True) Then\n   Return True\nEnd If";
        }

        private static string NotParenChain(int depth)
        {
            return "If (" + string.Concat(Enumerable.Repeat("Not(", depth)) + "True" + new string(')', depth) +
                   ") Then\n   Return True\nEnd If";
        }

        private static string MinusChain(int depth)
        {
            return "If (" + new string('-', depth) + "1 > 0) Then\n   Return True\nEnd If";
        }

        private static string Grouping(int depth)
        {
            return "If (" + new string('(', depth) + "1" + new string(')', depth) +
                   " > 0) Then\n   Return True\nEnd If";
        }

        public static IEnumerable<object[]> PathologicalDepths()
        {
            foreach (var depth in new[] { 5000, 10000 })
            {
                yield return ["paren-calls", ParenCalls(depth)];
                yield return ["not-chain", NotChain(depth)];
                yield return ["not-paren-chain", NotParenChain(depth)];
                yield return ["minus-chain", MinusChain(depth)];
                yield return ["grouping", Grouping(depth)];
            }
        }

        [Theory]
        [MemberData(nameof(PathologicalDepths))]
        public void ADeeplyNestedRuleIsRefusedWithoutReachingTheCompiler(string label, string ruleText)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = RuleParse.Execute(ruleText, RuleParse.GatewayRule, RuleParseTests.Environment(),
                TestLog.NoOp, RuleParse.DefaultReferences());
            stopwatch.Stop();

            result.Compiled.Should().BeFalse(label);
            result.Message.Should().Be("Error", label);
            result.ClassName.Should().BeNull(label);
            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2), label);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(8)]
        [InlineData(30)]
        public void AReasonablyNestedLegitimateRuleStillCompiles(int depth)
        {
            var ruleText = "If (" + string.Concat(Enumerable.Repeat("Not (", depth)) + "Payload.Amount > 0" +
                           new string(')', depth) + ") Then\n   Return True\nEnd If";

            var result = RuleParse.Execute(ruleText, RuleParse.GatewayRule, RuleParseTests.Environment(),
                TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeTrue(result.Message);
            result.ErrorSpans.Should().BeEmpty();
        }

        [Fact]
        public void ManyConsecutiveDashesInsideAStringLiteralAreNotCountedAsNesting()
        {
            var ruleText = "If (Payload.Country = \"" + new string('-', 5000) + "\") Then\n   Return True\nEnd If";

            var result = RuleParse.Execute(ruleText, RuleParse.GatewayRule, RuleParseTests.Environment(),
                TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeTrue(result.Message);
        }

        [Fact]
        public void ManyNotWordsInsideAStringLiteralAreNotCountedAsNesting()
        {
            var ruleText = "If (Payload.Country = \"" + string.Concat(Enumerable.Repeat("Not ", 5000)) +
                           "\") Then\n   Return True\nEnd If";

            var result = RuleParse.Execute(ruleText, RuleParse.GatewayRule, RuleParseTests.Environment(),
                TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeTrue(result.Message);
        }

        [Fact]
        public void ALongFlatArithmeticChainIsNotTreatedAsNesting()
        {
            var ruleText = "If (100" + string.Concat(Enumerable.Repeat("-1", 5000)) +
                           " < 200) Then\n   Return True\nEnd If";

            var result = RuleParse.Execute(ruleText, RuleParse.GatewayRule, RuleParseTests.Environment(),
                TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeTrue(result.Message);
        }

        public static IEnumerable<object[]> StructuredCorpusAgainstRuleTypes()
        {
            foreach (var payload in ModelPayloads.Structured)
            {
                yield return [payload, RuleParse.ActivationRule];
                yield return [payload, RuleParse.AbstractionCalculation];
            }
        }

        [Theory]
        [MemberData(nameof(StructuredCorpusAgainstRuleTypes))]
        public void TheStructuredFuzzCorpusNeverReachesTheCompilerOrHangs(string ruleText, int ruleParseType)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = RuleParse.Execute(ruleText, ruleParseType, RuleParseTests.Environment(),
                TestLog.NoOp, RuleParse.DefaultReferences());
            stopwatch.Stop();

            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(60));
            if (result.Compiled)
            {
                result.ClassName.Should().NotBeNull();
            }
        }

        [Fact]
        public void NotAsPartOfAnIdentifierIsNotCountedAsAUnaryOperator()
        {
            var ruleText = "If (" + string.Concat(Enumerable.Repeat("Notation ", 5000)) +
                           "Payload.Amount > 0) Then\n   Return True\nEnd If";

            var result = RuleParse.Execute(ruleText, RuleParse.GatewayRule, RuleParseTests.Environment(),
                TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeFalse();
            result.ErrorSpans.Should().NotContain(e => e.Message.Contains("nests expressions too deeply"));
        }
    }
}
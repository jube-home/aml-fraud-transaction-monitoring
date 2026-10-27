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

using FluentAssertions;
using Jube.Parser;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class RuleParseErrorColumnTests
    {
        private static Jube.Parser.Models.RuleParseResult Run(string text)
        {
            return RuleParse.Execute(text, RuleParse.GatewayRule, RuleParseTests.Environment(), TestLog.NoOp,
                RuleParse.DefaultReferences());
        }

        private static string Covered(string text, Jube.Parser.Models.ErrorSpan span)
        {
            var line = text.Split('\n')[span.Line];

            return line.Substring(span.Column!.Value, span.EndColumn!.Value - span.Column.Value);
        }

        [Fact]
        public void AnUnknownListIsLocatedOnTheNameAsTyped()
        {
            const string text = "If (Payload.Amount > 100 AND List.Nope.Contains(\"x\")) Then\n   Return True\nEnd If";

            var span = Run(text).ErrorSpans.Should().ContainSingle(e => e.Message.Contains("List does not exist"))
                .Subject;

            span.Line.Should().Be(0);
            Covered(text, span).Should().Be("List.Nope");
        }

        [Fact]
        public void ARestrictedTokenAfterARewrittenFieldIsStillLocatedOnTheTypedText()
        {
            const string text = "If (Payload.Amount > 100 AND Shutdown = True) Then\n   Return True\nEnd If";

            var span = Run(text).ErrorSpans.Should().Contain(e => e.Message.Contains("Security restricted")).Subject;

            span.Line.Should().Be(0);
            Covered(text, span).Should().Be("Shutdown");
        }

        [Fact]
        public void ARestrictedTokenOnALaterLineIsLocatedOnItsOwnLine()
        {
            const string text = "If (Payload.Amount > 100) Then\n   Shutdown = True\nEnd If";

            var span = Run(text).ErrorSpans.Should().Contain(e => e.Message.Contains("Security restricted")).Subject;

            span.Line.Should().Be(1);
            Covered(text, span).Should().Be("Shutdown");
        }

        [Fact]
        public void ACompilerErrorAfterARewrittenFieldCoversTheTypedMemberAccessNotTheTranslatedOne()
        {
            const string text = "If (Payload.Amount > 100 AND Payload.Country.Bogus()) Then\n   Return True\nEnd If";

            var result = Run(text);
            var compile = result.ErrorSpans.Should().ContainSingle(e => e.Message.Contains("is not a member"))
                .Subject;
            var soft = result.ErrorSpans.Should().ContainSingle(e => e.Message.Contains("Security restricted"))
                .Subject;

            result.Compiled.Should().BeFalse();
            compile.Line.Should().Be(0);
            Covered(text, compile).Should().Be("Payload.Country.Bogus");
            Covered(text, soft).Should().Be("Bogus");
        }

        [Fact]
        public void AnUnknownWordOnPlainTextIsLocatedOnTheExactToken()
        {
            const string text = "If (Payload.Amount > 100) Then\n   Return Tru\nEnd If";

            var span = Run(text).ErrorSpans.Should().Contain(e => e.Message.Contains("Security restricted")).Subject;

            span.Line.Should().Be(1);
            Covered(text, span).Should().Be("Tru");
        }

        [Fact]
        public void ACompilerErrorOnPlainTextIsLocatedOnTheOffendingToken()
        {
            const string text = "If (Payload.Amount > ) Then\n   Return True\nEnd If";

            var span = Run(text).ErrorSpans.Should().Contain(e => e.Column != null && !e.Message.Contains("Security"))
                .Subject;

            span.Line.Should().Be(0);
            span.EndColumn.Should().BeGreaterThan(span.Column!.Value);
        }

        [Fact]
        public void ADiscardedExpressionIsLocatedOnTheTypedStatement()
        {
            const string text = "If (Payload.Amount > 0) Then\n   Payload.Amount.Start().MatchGreater(100)\nEnd If";

            var span = Run(text).ErrorSpans.Should().ContainSingle(e => e.Message.Contains("result is discarded"))
                .Subject;

            span.Line.Should().Be(1);
            Covered(text, span).Should().Be("Payload.Amount.Start().MatchGreater(100)");
        }
    }
}

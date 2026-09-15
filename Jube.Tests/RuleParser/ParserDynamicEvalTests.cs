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
using Jube.Dictionary;
using Jube.Parser;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    [Collection("EvalExpressionRegistry")]
    public sealed class ParserDynamicEvalTests : IDisposable
    {
        public ParserDynamicEvalTests()
        {
            EvalExpressionRegistry.Clear();
            Environment.SetEnvironmentVariable("EnableDynamicEval", null);
        }

        public void Dispose()
        {
            EvalExpressionRegistry.Clear();
            Environment.SetEnvironmentVariable("EnableDynamicEval", null);
        }

        private static ParsedRule NewRule(string text)
        {
            return new ParsedRule
            {
                ErrorSpans = [],
                OriginalRuleText = text,
                ParsedRuleText = text
            };
        }

        [Fact]
        public void ARegisteredNameIsAcceptedAndRewrittenToTheGenericEvalCallWhenEnabled()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("IsCorporateEmail", "!value.EndsWith(\"gmail.com\")", 5);

            var parser = new Parser.Parser(TestLog.NoOp, []);
            var parsed = parser.Parse(NewRule("If Payload(\"Email\").IsCorporateEmail Then\r\nEnd If"));

            parsed.ErrorSpans.Should().BeEmpty();
            parsed.ParsedRuleText.Should().Contain(".Eval(Of Boolean)(\"IsCorporateEmail\")");
        }

        [Fact]
        public void ARegisteredNameIsRejectedWhenDisabled()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("IsCorporateEmail", "!value.EndsWith(\"gmail.com\")", 5);
            Environment.SetEnvironmentVariable("EnableDynamicEval", "False");

            var parser = new Parser.Parser(TestLog.NoOp, []);
            var parsed = parser.Parse(NewRule("If Payload(\"Email\").IsCorporateEmail Then\r\nEnd If"));

            parsed.ErrorSpans.Should().NotBeEmpty();
        }

        [Fact]
        public void CallingEvalDirectlyIsAlwaysRejectedRegardlessOfTheSwitch()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("IsCorporateEmail", "!value.EndsWith(\"gmail.com\")", 5);

            var parser = new Parser.Parser(TestLog.NoOp, []);
            var parsed = parser.Parse(NewRule(
                "If Payload(\"Email\").Eval(\"IsCorporateEmail\") Then\r\nEnd If"));

            parsed.ErrorSpans.Should().Contain(e => e.Message.Contains("'Eval'"));
        }

        [Fact]
        public void CallingEvalDirectlyIsRejectedEvenWithNoNamesRegisteredAtAll()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");

            var parser = new Parser.Parser(TestLog.NoOp, []);
            var parsed = parser.Parse(NewRule(
                "If Payload(\"Email\").Eval(\"Anything\") Then\r\nEnd If"));

            parsed.ErrorSpans.Should().Contain(e => e.Message.Contains("'Eval'"));
        }

        [Fact]
        public void MultipleRegisteredNamesAreEachIndependentlyAcceptedAndRewritten()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("IsCorporateEmail", "!value.EndsWith(\"gmail.com\")", 5);
            EvalExpressionRegistry.Register("Shout", "value.ToUpper()", 1);

            var parser = new Parser.Parser(TestLog.NoOp, []);
            var parsed = parser.Parse(NewRule(
                "If Payload(\"Email\").IsCorporateEmail Then\r\n" +
                "Return Payload(\"Name\").Shout\r\n" +
                "End If"));

            parsed.ErrorSpans.Should().BeEmpty();
            parsed.ParsedRuleText.Should().Contain(".Eval(Of Boolean)(\"IsCorporateEmail\")");
            parsed.ParsedRuleText.Should().Contain(".Eval(Of String)(\"Shout\")");
        }

        [Fact]
        public void ARegisteredNameWithExplicitEmptyParenthesesIsAlsoRewritten()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("IsCorporateEmail", "!value.EndsWith(\"gmail.com\")", 5);

            var parser = new Parser.Parser(TestLog.NoOp, []);
            var parsed = parser.Parse(NewRule("If Payload(\"Email\").IsCorporateEmail() Then\r\nEnd If"));

            parsed.ErrorSpans.Should().BeEmpty();
            parsed.ParsedRuleText.Should().Contain(".Eval(Of Boolean)(\"IsCorporateEmail\")");
            parsed.ParsedRuleText.Should().NotContain("IsCorporateEmail()");
        }

        [Fact]
        public void ARegisteredNameThatIsAPrefixOfAnotherIdentifierIsNotIncorrectlyRewritten()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("Is", "value.Length > 0", 5);
            EvalExpressionRegistry.Register("IsCorporate", "!value.EndsWith(\"gmail.com\")", 5);

            var parser = new Parser.Parser(TestLog.NoOp, []);
            var parsed = parser.Parse(NewRule("If Payload(\"Email\").IsCorporate Then\r\nEnd If"));

            parsed.ErrorSpans.Should().BeEmpty();
            parsed.ParsedRuleText.Should().Contain(".Eval(Of Boolean)(\"IsCorporate\")");
            parsed.ParsedRuleText.Should().NotContain("\"Is\"");
        }

        [Fact]
        public void AnUnregisteredBareIdentifierStillFailsSoftParseNormally()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("IsCorporateEmail", "!value.EndsWith(\"gmail.com\")", 5);

            var parser = new Parser.Parser(TestLog.NoOp, []);
            var parsed = parser.Parse(NewRule("If Payload(\"Email\").SomeTypo Then\r\nEnd If"));

            parsed.ErrorSpans.Should().Contain(e => e.Message.Contains("'SomeTypo'"));
        }

        [Fact]
        public void ARuleCombiningARealExtensionMethodAndACuratedNameBothCompile()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("IsCorporateEmail", "!value.EndsWith(\"gmail.com\")", 5);

            var parser = new Parser.Parser(TestLog.NoOp, []);
            var parsed = parser.Parse(NewRule(
                "If Payload(\"Name\").Contains(\"Richard\") AND Payload(\"Email\").IsCorporateEmail Then\r\nEnd If"));

            parsed.ErrorSpans.Should().BeEmpty();
            parsed.ParsedRuleText.Should().Contain(".Eval(Of Boolean)(\"IsCorporateEmail\")");
        }

        [Fact]
        public void ANameRegisteredWhileEnabledIsNotSeededAsATokenIntoAParserConstructedAfterDisabling()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("IsCorporateEmail", "!value.EndsWith(\"gmail.com\")", 5);
            Environment.SetEnvironmentVariable("EnableDynamicEval", "False");

            var parser = new Parser.Parser(TestLog.NoOp, []);
            var parsed = parser.Parse(NewRule("If Payload(\"Email\").IsCorporateEmail Then\r\nEnd If"));

            parsed.ErrorSpans.Should().Contain(e => e.Message.Contains("'IsCorporateEmail'"));
        }

        [Fact]
        public void ANameRegisteredAfterTheParserWasConstructedIsNotAvailableUntilANewParserIsBuilt()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("First", "value.Length > 0", 5);

            var parser = new Parser.Parser(TestLog.NoOp, []);

            EvalExpressionRegistry.Register("Second", "value.Length > 1", 5);

            var parsed = parser.Parse(NewRule("If Payload(\"Email\").Second Then\r\nEnd If"));

            parsed.ErrorSpans.Should().Contain(e => e.Message.Contains("'Second'"));
        }

        [Fact]
        public void ANameRegisteredBeforeConstructionIsAvailableOnANewlyBuiltParser()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("First", "value.Length > 0", 5);
            EvalExpressionRegistry.Register("Second", "value.Length > 1", 5);

            var parser = new Parser.Parser(TestLog.NoOp, []);
            var parsed = parser.Parse(NewRule("If Payload(\"Email\").Second Then\r\nEnd If"));

            parsed.ErrorSpans.Should().BeEmpty();
            parsed.ParsedRuleText.Should().Contain(".Eval(Of Boolean)(\"Second\")");
        }

        [Theory]
        [InlineData(5, "Boolean", "value.Length > 0")]
        [InlineData(1, "String", "value")]
        [InlineData(3, "Double", "value.Length / 1.0")]
        [InlineData(2, "Integer", "value.Length")]
        [InlineData(4, "DateTime", "DateTime.Parse(value)")]
        public void RewriteProducesTheCorrectVbGenericSyntaxForEveryResultTypeId(int resultTypeId, string vbKeyword,
            string expression)
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("Curated", expression, resultTypeId);

            var parser = new Parser.Parser(TestLog.NoOp, []);
            var parsed = parser.Parse(NewRule("If Payload(\"Field\").Curated Then\r\nEnd If"));

            parsed.ErrorSpans.Should().BeEmpty();
            parsed.ParsedRuleText.Should().Contain($".Eval(Of {vbKeyword})(\"Curated\")");
        }

        [Fact]
        public void ARuleWithNoCuratedNamesAtAllStillParsesNormallyWhenEnabled()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");

            var parser = new Parser.Parser(TestLog.NoOp, []);
            var parsed = parser.Parse(NewRule("If Payload(\"Name\").Contains(\"Richard\") Then\r\nEnd If"));

            parsed.ErrorSpans.Should().BeEmpty();
            parsed.ParsedRuleText.Should().NotContain(".Eval(");
        }
    }
}
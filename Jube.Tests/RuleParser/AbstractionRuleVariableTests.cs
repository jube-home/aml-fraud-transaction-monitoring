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
using FluentAssertions;
using Jube.Parser;
using Jube.Parser.Compiler;
using Jube.Parser.Models;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class AbstractionRuleVariableTests
    {
        private const string ThirdAndFifth =
            "Static declines As Integer\n" +
            "If Payload.Country <> \"GB\" Then Continue For\n" +
            "declines += 1\n" +
            "Matched = declines = 3 OR declines = 5\n" +
            "If declines >= 5 Then Exit For";

        private static RuleParseResult Execute(string rule, int ruleParseType = RuleParse.AbstractionRule,
            bool engineWrap = false, params string[] tokens)
        {
            var environment = RuleParseTests.Environment();

            environment.Tokens.AddRange(tokens);

            return RuleParse.Execute(rule, ruleParseType, environment, TestLog.NoOp, RuleParse.DefaultReferences(),
                engineWrap: engineWrap);
        }

        private static Parser.Parser NewParser(params string[] tokens)
        {
            return new Parser.Parser(TestLog.NoOp, ["Matched", .. tokens])
            {
                EntityAnalysisModelRequestXPaths = new Dictionary<string, EntityAnalysisModelRequestXPath>
                {
                    ["Amount"] = new() { DataTypeId = 3, Cache = true },
                    ["Country"] = new() { DataTypeId = 1, Cache = true }
                },
                EntityAnalysisModelInlineScriptProperties = [],
                EntityAnalysisModelsInlineFunctions = [],
                EntityAnalysisModelsLists = [],
                EntityAnalysisModelsDictionaries = []
            };
        }

        private static ParsedRule Parse(Parser.Parser parser, string rule, bool allowCursor)
        {
            var translated = parser.TranslateFromDotNotation(new ParsedRule
            {
                ErrorSpans = [],
                OriginalRuleText = rule,
                ParsedRuleText = rule
            }, true);

            return parser.Parse(translated, allowCursor);
        }

        private static bool Compiles(string classText)
        {
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location) &&
                            a.GetName().Name?.EndsWith(".resources", StringComparison.Ordinal) != true)
                .Select(a => a.Location)
                .ToArray();

            var compile = new Compile();

            compile.CompileCode(classText, TestLog.NoOp, references, Compile.Language.Vb, false);

            return compile.Success;
        }

        [Theory]
        [InlineData(ThirdAndFifth)]
        [InlineData("Static total As Double\ntotal += Payload.Amount\nMatched = True\nIf total >= 10000 Then Exit For")]
        [InlineData("Static previous As String = \"\"\nMatched = previous <> \"\" AND previous <> Payload.Country\nprevious = Payload.Country")]
        [InlineData("Static limit As Double = 10000.5\nStatic total As Double\ntotal = total + Payload.Amount\nMatched = total >= limit")]
        [InlineData("Static seen As Boolean = False\nMatched = Not (seen)\nseen = True")]
        [InlineData("Static seen As Boolean\nIf seen Then Matched = True\nseen = True")]
        [InlineData("Static n As Integer = -1\nn += 1\nSelect Case n\n  Case 3, 5\n    Matched = True\nEnd Select")]
        [InlineData("static N as integer\nn += 1\nMatched = N = 1")]
        public void AScanVariableCompilesAndMakesTheRuleDependOnTheScan(string rule)
        {
            foreach (var engineWrap in new[] { false, true })
            {
                var result = Execute(rule, engineWrap: engineWrap);

                result.Message.Should().Be("Compiled");
                result.UsesCursor.Should().BeTrue();
            }
        }

        [Theory]
        [InlineData("Dim doubled As Double = Payload.Amount * 2\nMatched = doubled > 100")]
        [InlineData("Dim big As Boolean\nbig = Payload.Amount > 100\nReturn big")]
        [InlineData("Dim amount As Double = Payload.Amount\nMatched = amount.Start().MatchGreater(100).ToBoolean()")]
        [InlineData("Dim country As String = Payload.Country\nMatched = country.StartsWith(\"G\") AND Payload.Country.Contains(country)")]
        [InlineData("Dim amount As Double = Payload.Amount\nMatched = If(amount > 1, True, False)")]
        [InlineData("If Payload.Amount > 1 Then\n  Dim inner As Integer = 2\n  Matched = inner = 2\nEnd If")]
        [InlineData("Dim value As Double = Payload.Amount\nMatched = value > 1")]
        [InlineData("Dim File As Integer = 3\nMatched = File = 3")]
        public void ARecordVariableCompilesAndLeavesTheRuleIndependentOfTheScan(string rule)
        {
            foreach (var engineWrap in new[] { false, true })
            {
                var result = Execute(rule, engineWrap: engineWrap);

                result.Message.Should().Be("Compiled");
                result.UsesCursor.Should().BeFalse();
            }
        }

        [Theory]
        [InlineData("Dim x\nMatched = True")]
        [InlineData("Dim x As Object\nMatched = True")]
        [InlineData("Dim x As Integer()\nMatched = True")]
        [InlineData("Dim x, y As Integer\nMatched = True")]
        [InlineData("Dim x_y As Integer\nMatched = True")]
        [InlineData("Dim 1x As Integer\nMatched = True")]
        [InlineData("Dim x As New Integer\nMatched = True")]
        [InlineData("Static x As Integer = Payload.Amount\nMatched = True")]
        [InlineData("Static x As Integer = 1.5\nMatched = True")]
        [InlineData("Static x As String = 5\nMatched = True")]
        [InlineData("Static x As Boolean = 1\nMatched = True")]
        [InlineData("Static x As DateTime = 1\nMatched = True")]
        [InlineData("Static x As String = \"a\" & Payload.Country\nMatched = True")]
        public void AMalformedDeclarationIsRefusedWithItsOwnCode(string rule)
        {
            var result = Execute(rule);

            result.Compiled.Should().BeFalse();
            result.ErrorSpans.Should().Contain(e => e.Code == RuleVariableDeclarations.DeclarationCode && e.Line == 0);
            result.ErrorSpans.Should().NotContain(e => e.Code == Parser.Parser.RestrictedTokenCode);
        }

        [Theory]
        [InlineData("Dim If As Integer\nMatched = True")]
        [InlineData("Dim Stop As Integer\nMatched = True")]
        [InlineData("Dim Me As Integer\nMatched = True")]
        [InlineData("Dim Cursor As Integer\nMatched = True")]
        [InlineData("Dim Data As Integer\nMatched = True")]
        [InlineData("Dim Log As Integer\nMatched = True")]
        [InlineData("Dim Matched As Boolean\nMatched = True")]
        [InlineData("Dim CursorPass As Integer\nMatched = True")]
        [InlineData("Static Scan As Integer\nMatched = True")]
        [InlineData("Static MatchRecord As Integer\nMatched = True")]
        [InlineData("Dim GetType As Integer\nMatched = True")]
        [InlineData("Dim ex As Integer\nMatched = True")]
        [InlineData("Dim total As Integer\nStatic TOTAL As Double\nMatched = True")]
        public void AReservedOrRepeatedNameIsRefusedWithItsOwnCode(string rule)
        {
            var result = Execute(rule);

            result.Compiled.Should().BeFalse();
            result.ErrorSpans.Should().Contain(e => e.Code == RuleVariableDeclarations.NameCode);
        }

        [Fact]
        public void ANameLongerThanTheLimitIsRefused()
        {
            var name = new string('a', RuleVariableDeclarations.MaximumNameLength + 1);

            Execute($"Dim {name} As Integer\nMatched = True").ErrorSpans.Should()
                .Contain(e => e.Code == RuleVariableDeclarations.NameCode);

            Execute($"Dim {name[1..]} As Integer\nMatched = True").Message.Should().Be("Compiled");
        }

        [Fact]
        public void MoreVariablesThanTheLimitAreRefused()
        {
            var declarations = string.Join("\n",
                Enumerable.Range(0, RuleVariableDeclarations.MaximumVariables + 1).Select(i => $"Dim v{i} As Integer"));

            Execute(declarations + "\nMatched = True").ErrorSpans.Should()
                .Contain(e => e.Code == RuleVariableDeclarations.NameCode && e.Message.Contains("at most"));
        }

        [Fact]
        public void ARefusedNameIsLocatedOnTheNameItself()
        {
            var span = Execute("Matched = True\n  Dim If As Integer").ErrorSpans.Should()
                .ContainSingle(e => e.Code == RuleVariableDeclarations.NameCode).Subject;

            (span.Line, span.Column, span.EndColumn).Should().Be((1, 6, 8));
        }

        [Theory]
        [InlineData("Dim Assembly As Integer\nMatched = Cursor.Assembly > 1")]
        [InlineData("Dim Assembly As Integer\nMatched = Cursor.\nAssembly > 1")]
        [InlineData("Dim Assembly As Integer\nMatched = Cursor. _\nAssembly > 1")]
        [InlineData("Dim Assembly As Integer\nMatched = Cursor .Assembly > 1")]
        [InlineData("Dim Assembly As Integer\nMatched = Payload.Country.Assembly = 1")]
        [InlineData("Dim Assembly As Integer\nMatched = .Assembly > 1")]
        [InlineData("Dim Assembly As Integer\nMatched = Cursor?.Assembly > 1")]
        [InlineData("Dim Begin As Integer\nCursor.Begin(0)\nMatched = True")]
        [InlineData("Dim State As Integer\nCursor.State = 1\nMatched = True")]
        [InlineData("Dim Stopped As Boolean\nMatched = Cursor.Stopped")]
        [InlineData("Static Assembly As Integer\nMatched = Cursor.Assembly > 1")]
        [InlineData("Static inner As Integer\nStatic outer As Integer\nMatched = outer.inner > 1")]
        public void ADeclaredNameCannotFollowADot(string rule)
        {
            var result = Execute(rule);

            result.Compiled.Should().BeFalse();

            result.ErrorSpans.Should().Contain(e =>
                e.Code == RuleVariableUsage.UseCode || e.Code == Parser.Parser.RestrictedTokenCode);
        }

        [Fact]
        public void ADeclaredNameAfterADotOnTheNextLineIsReportedOnThatLine()
        {
            var span = Execute("Dim Assembly As Integer\nMatched = Cursor.\n  Assembly > 1").ErrorSpans.Should()
                .ContainSingle(e => e.Code == RuleVariableUsage.UseCode).Subject;

            (span.Line, span.Column, span.EndColumn).Should().Be((2, 2, 10));
        }

        [Fact]
        public void AFullWidthDotDoesNotHideAMemberAccessFromTheGate()
        {
            var result = Execute("Dim Assembly As Integer\nMatched = Cursor．Assembly > 1");

            result.Compiled.Should().BeFalse();
            result.ErrorSpans.Should().Contain(e => e.Code == Parser.Parser.RestrictedTokenCode);
        }

        [Theory]
        [InlineData("Dim WebClient As Integer\nMatched = New WebClient() Is Nothing", "New", "Is", "Nothing")]
        [InlineData("Dim File As Integer\nMatched = GetType(File) Is Nothing", "GetType", "Is", "Nothing")]
        [InlineData("Dim File As Integer\nMatched = CType(Payload.Amount, File) > 1", "CType")]
        [InlineData("Dim File As Integer\nMatched = TypeOf Payload.Amount Is File", "TypeOf", "Is")]
        [InlineData("Dim File As Integer\nMatched = DirectCast(Payload.Country, File) Is Nothing", "DirectCast", "Is", "Nothing")]
        [InlineData("Dim File As Integer\nFor File = 1 To 2\nMatched = True\nNext", "For", "To", "Next")]
        public void ADeclaredNameCannotStandWhereATypeOrANameIsExpectedEvenWhenTheInstallationAllowsTheKeyword(
            string rule, params string[] tokens)
        {
            var result = Execute(rule, tokens: tokens);

            result.Compiled.Should().BeFalse();
            result.ErrorSpans.Should().Contain(e => e.Code == RuleVariableUsage.UseCode);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ADeclaredNameHidesATypeOfTheSameNameSoItsMembersCannotBeReached(bool scan)
        {
            const string body = "Matched = File.Exists(\"x\")";

            var variable = new RuleVariable("File", "Integer", scan ? RuleVariableKind.Scan : RuleVariableKind.Record,
                null, 0);

            Compiles(EngineRuleWrapper.AbstractionRule(body).Text).Should().BeTrue();
            Compiles(EngineRuleWrapper.AbstractionRule(body, true, [variable]).Text).Should().BeFalse();

            var gated = Execute("Dim File As Integer\n" + body);

            gated.Compiled.Should().BeFalse();
            gated.ParsedRuleText.Should().Contain("Dim File As Integer");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ADeclaredNameCannotBeUsedFromCodeThatLeavesTheWrappersOwnMethod(bool scan)
        {
            const string body = "Matched = File = 1\n" +
                                "Next\nCatch ex As Exception\nEnd Try\nReturn Matched\nEnd Function\n" +
                                "Public Shared Function Other() As Boolean\nDim CursorPass As Integer\nTry\n" +
                                "For CursorPass = 0 To 0\nReturn File.Exists(\"x\")";

            var variable = new RuleVariable("File", "Integer", scan ? RuleVariableKind.Scan : RuleVariableKind.Record,
                null, 0);

            var errors = RuleVariableUsage.Check(body, [variable],
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "File" });

            errors.Should().ContainSingle().Which.Line.Should().Be(10);
            errors[0].Code.Should().Be(RuleVariableUsage.UseCode);
        }

        [Theory]
        [InlineData(RuleParse.GatewayRule)]
        [InlineData(RuleParse.ActivationRule)]
        [InlineData(RuleParse.InlineFunction)]
        [InlineData(RuleParse.AbstractionCalculation)]
        public void VariablesAreRefusedOutsideAbstractionRules(int ruleParseType)
        {
            foreach (var keyword in new[] { "Dim", "Static" })
            {
                var result = Execute($"{keyword} total As Double\nMatched = True", ruleParseType);

                result.Compiled.Should().BeFalse();

                result.ErrorSpans.Select(e => e.Message).Should()
                    .Contain(m => m.Contains($"Security restricted token '{keyword}'"));
            }
        }

        [Theory]
        [InlineData("Dim total As Double\ntotal = Payload.Amount\nMatched = total > 1")]
        [InlineData("Static total As Double\ntotal = total + Payload.Amount\nMatched = total > 1")]
        public void ARuleAnInstallationAlreadyAllowsIsLeftExactlyAsItWas(string rule)
        {
            string[] tokens = ["Dim", "Static", "As", "total"];

            var before = Parse(NewParser(tokens), rule, false);
            var after = Parse(NewParser(tokens), rule, true);

            before.ErrorSpans.Should().BeEmpty();
            after.ErrorSpans.Should().BeEmpty();
            after.ParsedRuleText.Should().Be(before.ParsedRuleText);
            after.ParsedRuleText.Should().Contain(rule.Split('\n')[0]);
            after.Variables.Should().BeEmpty();
            after.UsesCursor.Should().BeFalse();
        }

        [Theory]
        [InlineData("Matched = Payload.Amount > 100")]
        [InlineData("If (Payload.Amount > 100 AND Payload.Country = \"GB\") Then\n  Return True\nEnd If")]
        [InlineData("Matched = Cursor.Position = 3\nIf Cursor.Position >= 3 Then Exit For")]
        public void ARuleWithoutDeclarationsParsesToTheSameTextAndCarriesNoVariables(string rule)
        {
            var parsed = Parse(NewParser(), rule, true);
            var legacy = NewParser().TranslateFromDotNotation(new ParsedRule
            {
                ErrorSpans = [],
                OriginalRuleText = rule,
                ParsedRuleText = rule
            }, true);

            parsed.ErrorSpans.Should().BeEmpty();
            parsed.ParsedRuleText.Should().Be(legacy.ParsedRuleText);
            parsed.Variables.Should().BeEmpty();

            EngineRuleWrapper.AbstractionRule(parsed.ParsedRuleText, true, parsed.Variables).Text.Should()
                .Be(EngineRuleWrapper.AbstractionRule(parsed.ParsedRuleText).Text);
        }

        [Fact]
        public void ARefusedRuleWithoutDeclarationsKeepsTheErrorsItAlwaysHad()
        {
            var parsed = Parse(NewParser(), "Matched = Forbidden.Thing > 1", true);
            var legacy = Parse(NewParser(), "Matched = Forbidden.Thing > 1", false);

            parsed.ErrorSpans.Select(e => e.Message).Should().Equal(legacy.ErrorSpans.Select(e => e.Message));
        }

        [Fact]
        public void ACompilerErrorInARuleWithVariablesIsReportedOnTheLineTheAuthorWrote()
        {
            foreach (var engineWrap in new[] { false, true })
            {
                var result = Execute("Static n As Integer\nDim m As Integer\nMatched = n.StartsWith(\"a\")",
                    engineWrap: engineWrap);

                result.Compiled.Should().BeFalse();
                result.ErrorSpans.Should().NotBeEmpty();
                result.ErrorSpans.Should().OnlyContain(e => e.Line == 2);
            }
        }

        [Fact]
        public void TheDeclarationsBecomeFieldsAndLocalsOfTheWrapperAndTheirLinesAreBlanked()
        {
            var parsed = Parse(NewParser(), "Static declines As Integer = 2\nDim doubled As Double = Payload.Amount * 2\nMatched = doubled > declines", true);

            parsed.ErrorSpans.Should().BeEmpty();

            parsed.Variables.Should().Equal(
                new RuleVariable("declines", "Integer", RuleVariableKind.Scan, "2", 0),
                new RuleVariable("doubled", "Double", RuleVariableKind.Record, null, 1));

            var lines = parsed.ParsedRuleText.Split('\n');

            lines[0].Should().Be(new string(' ', "Static declines As Integer = 2".Length));
            lines[1].Should().StartWith("    doubled           = Data(");
            lines[1].Should().NotContain("Dim").And.NotContain(" As ");
        }

        [Fact]
        public void RulesThatDifferOnlyInTheirDeclarationsDoNotShareAMatchSignature()
        {
            var whole = Parse(NewParser(), "Static n As Integer\nn += 1\nMatched = n = 1", true);
            var real = Parse(NewParser(), "Static n As Double \nn += 1\nMatched = n = 1", true);

            whole.ParsedRuleText.Should().Be(real.ParsedRuleText);

            RuleVariableEmitter.Signature(whole.Variables).Should()
                .NotBe(RuleVariableEmitter.Signature(real.Variables));

            RuleVariableEmitter.Signature([]).Should().BeEmpty();
        }
    }
}

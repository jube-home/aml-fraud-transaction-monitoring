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

using System.Linq;
using FluentAssertions;
using Jube.Parser;
using Jube.Parser.Models;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class AbstractionCursorTokenTests
    {
        private static RuleParseResult Execute(string rule, int ruleParseType)
        {
            return RuleParse.Execute(rule, ruleParseType, RuleParseTests.Environment(), TestLog.NoOp,
                RuleParse.DefaultReferences());
        }

        private static void ShouldRestrict(RuleParseResult result, string token)
        {
            result.Compiled.Should().BeFalse();

            result.ErrorSpans.Select(e => e.Message).Should()
                .Contain(m => m.Contains("Security restricted token '" + token + "'"));
        }

        [Theory]
        [InlineData("Matched = Cursor.Position = 3 OR Cursor.Position = 5")]
        [InlineData("Matched = Cursor.Index = 0 AND Cursor.Size > 1")]
        [InlineData("If Payload.Amount > 100 Then Exit For\nMatched = True")]
        [InlineData("If Payload.Amount > 100 Then Continue For\nMatched = True")]
        [InlineData("Matched = Cursor.IsFirst OR Cursor.IsLast OR Cursor.IsCurrent OR Cursor.Matches > 2")]
        [InlineData("Cursor.Result = Cursor.Result + Payload.Amount")]
        public void CursorVocabularyCompilesInAnAbstractionRule(string rule)
        {
            var result = Execute(rule, RuleParse.AbstractionRule);

            result.Message.Should().Be("Compiled");
            result.UsesCursor.Should().BeTrue();
        }

        [Theory]
        [InlineData(RuleParse.GatewayRule)]
        [InlineData(RuleParse.ActivationRule)]
        [InlineData(RuleParse.InlineFunction)]
        [InlineData(RuleParse.AbstractionCalculation)]
        public void TheCursorIsRefusedOutsideAbstractionRules(int ruleParseType)
        {
            ShouldRestrict(Execute("Matched = Cursor.Position = 3", ruleParseType), "Cursor");
        }

        [Theory]
        [InlineData(RuleParse.GatewayRule)]
        [InlineData(RuleParse.ActivationRule)]
        public void LoopControlIsRefusedOutsideAbstractionRules(int ruleParseType)
        {
            ShouldRestrict(Execute("If Payload.Amount > 100 Then Exit For\nMatched = True", ruleParseType), "Exit");
            ShouldRestrict(Execute("If Payload.Amount > 100 Then Continue For\nMatched = True", ruleParseType),
                "Continue");
        }

        [Theory]
        [InlineData("For I = 1 To 1000000\nMatched = True\nNext", "For")]
        [InlineData("While True\nMatched = True\nEnd While", "While")]
        [InlineData("Do\nMatched = True\nLoop", "Do")]
        [InlineData("Matched = True\nGoTo Again", "GoTo")]
        [InlineData("Matched = True\nExit Function", "Exit")]
        [InlineData("Matched = True\nExit Sub", "Exit")]
        [InlineData("Matched = True\nExit Try", "Exit")]
        [InlineData("Matched = True\nContinue While", "Continue")]
        [InlineData("Matched = True\nExit", "Exit")]
        public void AnAbstractionRuleStillCannotOpenOrLeaveLoopsOfItsOwn(string rule, string token)
        {
            ShouldRestrict(Execute(rule, RuleParse.AbstractionRule), token);
        }

        [Theory]
        [InlineData("Cursor.Stop()\nMatched = True", "Stop")]
        [InlineData("Cursor.Begin(0)\nMatched = True", "Begin")]
        [InlineData("Cursor.Accept()\nMatched = True", "Accept")]
        [InlineData("Cursor.MoveTo(0, True)\nMatched = True", "MoveTo")]
        [InlineData("Matched = Cursor.Stopped", "Stopped")]
        [InlineData("CursorPass = 0\nMatched = True", "CursorPass")]
        [InlineData("Cursor.State = 1\nMatched = True", "State")]
        [InlineData("Matched = Cursor.Add(\"Total\", Payload.Amount) > 1000", "Add")]
        public void TheEngineSideOfTheCursorIsNotInTheVocabulary(string rule, string token)
        {
            ShouldRestrict(Execute(rule, RuleParse.AbstractionRule), token);
        }

        [Fact]
        public void ARestrictedTokenCarriesItsCode()
        {
            var result = Execute("Matched = Forbidden", RuleParse.AbstractionRule);

            result.ErrorSpans.Should().Contain(e => e.Code == Jube.Parser.Parser.RestrictedTokenCode);
        }

        [Fact]
        public void ARestrictedTokenAfterLoopControlIsStillLocated()
        {
            var result = Execute("If Payload.Amount > 100 Then Exit For\nMatched = Forbidden", RuleParse.AbstractionRule);

            var span = result.ErrorSpans.Should()
                .ContainSingle(e => e.Message.Contains("Security restricted token 'Forbidden'")).Subject;

            span.Line.Should().Be(1);
            span.Column.Should().Be(10);
        }
    }
}

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
using Jube.Parser.Models;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class RuleVariableDeclarationsTests
    {
        [Theory]
        [InlineData("Dim x As Integer", true)]
        [InlineData("  static x As Integer", true)]
        [InlineData("\tDIM x", true)]
        [InlineData("Dimension = 1", false)]
        [InlineData("Matched = True ' Dim x As Integer", false)]
        [InlineData("Matched = Payload.Text = \"\nDim x As Integer\"", true)]
        [InlineData("Matched = Dim", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void ADeclarationIsALineThatOpensWithDimOrStatic(string? rule, bool expected)
        {
            RuleVariableDeclarations.Any(rule).Should().Be(expected);
        }

        [Fact]
        public void EveryTypeIsReadAndWrittenInItsCanonicalCasing()
        {
            var reading = RuleVariableDeclarations.Read(
                "Dim a As double\nDim b As INTEGER\nStatic c As string\nStatic d As boolean\nDim e As datetime");

            reading.Errors.Should().BeEmpty();
            reading.Variables.ConvertAll(v => v.Type).Should().Equal(RuleVariableDeclarations.Types);

            reading.Variables.ConvertAll(v => v.Kind).Should().Equal(RuleVariableKind.Record, RuleVariableKind.Record,
                RuleVariableKind.Scan, RuleVariableKind.Scan, RuleVariableKind.Record);
        }

        [Fact]
        public void BlankingKeepsEveryLineItsLengthAndItsLineEnding()
        {
            const string rule = "Static n As Integer = 5\r\n  Dim m As Double = n * 2\rDim k As String\nMatched = m > 1";

            var reading = RuleVariableDeclarations.Read(rule);

            reading.Errors.Should().BeEmpty();
            reading.Text.Length.Should().Be(rule.Length);

            reading.Text.Should().Be(
                "                       \r\n      m           = n * 2\r               \nMatched = m > 1");
        }

        [Theory]
        [InlineData("Static a As Integer = 12", "12")]
        [InlineData("Static a As Integer = -3", "-3")]
        [InlineData("Static a As Double = 0.25", "0.25")]
        [InlineData("Static a As Double = 7", "7")]
        [InlineData("Static a As String = \"say \"\"hi\"\"\"", "\"say \"\"hi\"\"\"")]
        [InlineData("Static a As Boolean = true", "true")]
        [InlineData("Static a As DateTime", null)]
        public void AScanVariableKeepsItsLiteralStartingValue(string rule, string? initialiser)
        {
            var reading = RuleVariableDeclarations.Read(rule);

            reading.Errors.Should().BeEmpty();
            reading.Variables.Should().ContainSingle().Which.Initialiser.Should().Be(initialiser);
        }

        [Theory]
        [InlineData("Static a As Integer = 12345678901")]
        [InlineData("Static a As Integer = 1e5")]
        [InlineData("Static a As Double = 1,5")]
        [InlineData("Static a As String = \"a\" \"b\"")]
        [InlineData("Static a As String = Nothing")]
        [InlineData("Static a As Integer = (1)")]
        [InlineData("Static a As Integer = 1 : Matched = True")]
        public void AScanVariableStartsOnlyFromOneLiteral(string rule)
        {
            var reading = RuleVariableDeclarations.Read(rule);

            reading.Variables.Should().BeEmpty();

            reading.Errors.Should().ContainSingle().Which.Code.Should()
                .Be(RuleVariableDeclarations.DeclarationCode);

            reading.Text.Should().Be(new string(' ', rule.Length));
        }

        [Fact]
        public void TheEmitterWritesFieldsLocalsAndTheEntryThatFindsOrMakesTheScanState()
        {
            var reading = RuleVariableDeclarations.Read("Static n As Integer = 5\nDim m As Double\nStatic s As String");

            RuleVariableEmitter.Fields(reading.Variables).Should()
                .Equal("Private n As Integer = 5", "Private s As String");

            RuleVariableEmitter.Locals(reading.Variables).Should().Equal("Dim m As Double");
            RuleVariableEmitter.HasScan(reading.Variables).Should().BeTrue();
            RuleVariableEmitter.HasScan([]).Should().BeFalse();
            RuleVariableEmitter.HasScan(null).Should().BeFalse();

            RuleVariableEmitter.Entry("AbstractionRule", "Public Shared Function Match(Data As X,Log as ILog) As Boolean")
                .Should().Equal(
                    "Public Shared Function Match(Data As X,Log as ILog) As Boolean",
                    "Dim Scan As AbstractionRule = Nothing",
                    "If Cursor IsNot Nothing Then Scan = TryCast(Cursor.State, AbstractionRule)",
                    "If Scan Is Nothing Then",
                    "Scan = New AbstractionRule()",
                    "If Cursor IsNot Nothing Then Cursor.State = Scan",
                    "End If",
                    "Return Scan.MatchRecord(Data,List,KVP,Svirfd,Cursor,Log)",
                    "End Function",
                    "Private Function MatchRecord(Data As X,Log as ILog) As Boolean");
        }
    }
}

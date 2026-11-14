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
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class RuleVocabularyTests
    {
        [Fact]
        public void EveryWordTheParserAllowsIsListedOnceExceptCompilerNames()
        {
            var words = RuleVocabulary.Build(["PadLeft"]).Select(w => w.Name.ToUpperInvariant()).ToList();

            var allowed = new Jube.Parser.Parser(null!, ["PadLeft"]).RuleScriptTokens
                .Where(t => !t.Contains('<') && !t.Contains('$') && !t.StartsWith("get_") && !t.StartsWith("set_"))
                .Select(t => t.ToUpperInvariant()).Distinct().ToList();

            words.GroupBy(w => w).Where(g => g.Count() > 1).Select(g => g.Key).Should().BeEmpty();
            words.Except(allowed).Should().BeEmpty("the vocabulary lists a word the parser does not allow");
            allowed.Except(words).Should().BeEmpty("the parser allows a token the vocabulary does not list");
        }

        [Fact]
        public void TheAbstractionRuleVocabularyIsTheGeneralOnePlusExactlyTheCursorAndVariableWords()
        {
            var general = RuleVocabulary.Build(["PadLeft"]).Select(w => w.Name.ToUpperInvariant()).ToList();

            var abstraction = RuleVocabulary.Build(["PadLeft"], RuleParse.AbstractionRule)
                .Select(w => w.Name.ToUpperInvariant()).ToList();

            abstraction.GroupBy(w => w).Where(g => g.Count() > 1).Select(g => g.Key).Should().BeEmpty();

            abstraction.Except(general).Should().BeEquivalentTo(Jube.Parser.Parser.CursorTokens
                .Concat(["Dim", "Static", "As"])
                .Concat(RuleVariableDeclarations.Types)
                .Select(t => t.ToUpperInvariant()).Except(general));

            general.Except(abstraction).Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(RuleParse.GatewayRule)]
        [InlineData(RuleParse.ActivationRule)]
        [InlineData(RuleParse.InlineFunction)]
        [InlineData(RuleParse.AbstractionCalculation)]
        public void TheCursorWordsAreOfferedOnlyForAbstractionRules(int? ruleParseType)
        {
            var words = RuleVocabulary.Build([], ruleParseType).Select(w => w.Name).ToList();

            words.Should().NotContain(["Cursor", "Exit For", "Continue For", "Position"]);
            words.Should().Equal(RuleVocabulary.Build([]).Select(w => w.Name));
        }

        [Fact]
        public void TheCursorWordsCarryTheirSignaturesAndOnlyTheRuleFacingMembers()
        {
            var words = RuleVocabulary.Build([], RuleParse.AbstractionRule).ToDictionary(w => w.Name);

            words["Cursor"].Overloads.Single().ReturnType.Should().Be("AbstractionCursor");
            words["Position"].Overloads.Single().Signature.Should().Be("Cursor.Position As Integer");
            words["IsCurrent"].Overloads.Single().ReturnType.Should().Be("Boolean");
            words["Result"].Overloads.Single().Signature.Should().Be("Cursor.Result As Double");
            words["Exit For"].Kind.Should().Be(RuleWordKind.Keyword);
            words["Continue For"].Kind.Should().Be(RuleWordKind.Keyword);

            words["Position"].Overloads.Single().ReceiverType.Should().Be("AbstractionCursor");
            words["Value"].Overloads.Should().NotContain(o => o.ReceiverType == "AbstractionCursor");

            words.Keys.Should().NotContain(["Add", "State", "Stop", "Begin", "MoveTo", "Accept", "Stopped"]);
        }

        [Fact]
        public void TheVariableWordsAreOfferedOnlyForAbstractionRulesAndSayWhatTheyMean()
        {
            var general = RuleVocabulary.Build([]).ToDictionary(w => w.Name);
            var words = RuleVocabulary.Build([], RuleParse.AbstractionRule).ToDictionary(w => w.Name);

            general.Keys.Should().NotContain(["Dim", "Static", "As", "Boolean"]);
            words["Dim"].Description.Should().Contain("starts afresh for the next");
            words["Static"].Description.Should().Contain("keeps its value from one transaction to the next");
            words["As"].Kind.Should().Be(RuleWordKind.Keyword);

            foreach (var type in RuleVariableDeclarations.Types)
            {
                words[type].Description.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public void EveryCursorWordSaysWhatItMeans()
        {
            var general = RuleVocabulary.Build([]).Select(w => w.Name).ToHashSet();

            var cursor = RuleVocabulary.Build([], RuleParse.AbstractionRule)
                .Where(w => !general.Contains(w.Name)).ToList();

            cursor.Should().NotBeEmpty();
            cursor.Should().OnlyContain(w => w.Description != null && w.Description.Length > 20);

            cursor.Single(w => w.Name == "Exit For").Description.Should().Contain("ends the walk");
            cursor.Single(w => w.Name == "Continue For").Description.Should().Contain("moves to the next");
        }

        [Fact]
        public void BuiltInWordsAreKeywordsAndInstallationWordsAreAllowedWords()
        {
            var words = RuleVocabulary.Build(["PadLeft"]).ToDictionary(w => w.Name);

            words["If"].Kind.Should().Be(RuleWordKind.Keyword);
            words["Then"].Kind.Should().Be(RuleWordKind.Keyword);
            words.Should().NotContainKey("if");
            words["Payload"].Kind.Should().Be(RuleWordKind.Keyword);
            words["PadLeft"].Kind.Should().Be(RuleWordKind.AllowedWord);
            RuleVocabulary.Build([]).Should().NotContain(w => w.Name == "PadLeft");
        }

        [Fact]
        public void ExtensionMethodsAreFunctionsWithVisualBasicSignatures()
        {
            var isMatch = RuleVocabulary.Build([]).Single(w => w.Name == "IsMatch");

            isMatch.Kind.Should().Be(RuleWordKind.Function);

            isMatch.Overloads.Should().Contain(o =>
                o.Signature == "<String>.IsMatch(pattern As String) As Boolean" && o.ReceiverType == "String" &&
                o.ReturnType == "Boolean" && o.Parameters.Count == 1);

            isMatch.Overloads.Should().HaveCountGreaterThan(1);
        }

        [Fact]
        public void EveryFunctionHasAReceiverAReturnTypeAndADescription()
        {
            var functions = RuleVocabulary.Build([]).Where(w => w.Kind == RuleWordKind.Function).ToList();

            functions.Should().HaveCountGreaterThan(100);

            functions.SelectMany(f => f.Overloads).Should().OnlyContain(o =>
                !string.IsNullOrEmpty(o.ReceiverType) && !string.IsNullOrEmpty(o.ReturnType) &&
                o.Signature.StartsWith('<') && !string.IsNullOrEmpty(o.Description));

            functions.Should().OnlyContain(f => !string.IsNullOrEmpty(f.Description));
        }

        [Fact]
        public void AFunctionCarriesTheDescriptionOfEachOverloadAndParameter()
        {
            var isMatch = RuleVocabulary.Build([]).Single(w => w.Name == "IsMatch");

            isMatch.Description.Should()
                .Be("Checks if the string matches a regular expression; matching is limited to 250 ms.");

            var withOptions = isMatch.Overloads.Single(o => o.Parameters.Count == 2);

            withOptions.Description.Should()
                .Be("Checks if the string matches a regex with options; matching is limited to 250 ms.");

            withOptions.Parameters[0].Description.Should().Be("The regular expression pattern to match.");
            withOptions.Parameters[1].Description.Should().StartWith("A bitwise combination");

            var matchGreater = RuleVocabulary.Build([]).Single(w => w.Name == "MatchGreater");

            matchGreater.Overloads.Select(o => o.Description).Should().OnlyContain(d =>
                d.Contains("True when: ") && d.Contains("Decides matched if so"));
        }

        [Fact]
        public void EvalIsNeverOffered()
        {
            RuleVocabulary.Build([]).Should().NotContain(w => w.Name == "Eval");
        }
    }
}
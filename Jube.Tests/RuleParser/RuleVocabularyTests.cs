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
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class RuleVocabularyTests
    {
        [Fact]
        public void EveryWordTheParserAllowsIsListedOnceExceptCompilerNames()
        {
            var words = RuleVocabulary.Build(["PadLeft"]);
            var allowed = new Jube.Parser.Parser(null!, ["PadLeft"]).RuleScriptTokens
                .Where(t => !t.Contains('<') && !t.Contains('$') && !t.StartsWith("get_") && !t.StartsWith("set_"))
                .Select(t => t.ToUpperInvariant()).Distinct().ToList();

            words.Select(w => w.Name.ToUpperInvariant()).Should().OnlyHaveUniqueItems();
            words.Select(w => w.Name.ToUpperInvariant()).Should().BeEquivalentTo(allowed);
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
        public void EveryFunctionHasAReceiverAndAReturnType()
        {
            var functions = RuleVocabulary.Build([]).Where(w => w.Kind == RuleWordKind.Function).ToList();

            functions.Should().HaveCountGreaterThan(100);
            functions.SelectMany(f => f.Overloads).Should().OnlyContain(o =>
                !string.IsNullOrEmpty(o.ReceiverType) && !string.IsNullOrEmpty(o.ReturnType) &&
                o.Signature.StartsWith("<"));
        }

        [Fact]
        public void EvalIsNeverOffered()
        {
            RuleVocabulary.Build([]).Should().NotContain(w => w.Name == "Eval");
        }
    }
}
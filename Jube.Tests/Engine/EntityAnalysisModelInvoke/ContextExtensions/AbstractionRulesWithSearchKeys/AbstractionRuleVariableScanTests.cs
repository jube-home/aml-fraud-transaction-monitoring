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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys;
using Jube.Parser;
using Jube.Parser.Compiler;
using Jube.Parser.Models;
using Jube.Test.Infrastructure;
using Xunit;
using EntityAnalysisModelAbstractionRule =
    Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelAbstractionRule;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke.ContextExtensions.AbstractionRulesWithSearchKeys
{
    [Trait("Category", "Unit")]
    public sealed class AbstractionRuleVariableScanTests
    {
        private const string FirstTwo = "Static seen As Integer\nseen += 1\nMatched = seen <= 2";

        private static EntityAnalysisModelAbstractionRule.Match Compile(string rule)
        {
            var parser = new Parser.Parser(TestLog.NoOp, ["Matched"])
            {
                EntityAnalysisModelRequestXPaths = new Dictionary<string, EntityAnalysisModelRequestXPath>
                {
                    ["Amount"] = new() { DataTypeId = 3, Cache = true }
                },
                EntityAnalysisModelInlineScriptProperties = [],
                EntityAnalysisModelsInlineFunctions = [],
                EntityAnalysisModelsLists = [],
                EntityAnalysisModelsDictionaries = []
            };

            var parsed = parser.Parse(parser.TranslateFromDotNotation(new ParsedRule
            {
                ErrorSpans = [],
                OriginalRuleText = rule,
                ParsedRuleText = rule
            }, true), true);

            parsed.ErrorSpans.Should().BeEmpty();

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location) &&
                            a.GetName().Name?.EndsWith(".resources", StringComparison.Ordinal) != true)
                .Select(a => a.Location)
                .ToArray();

            var compile = new Compile();

            compile.CompileCode(EngineRuleWrapper.AbstractionRule(parsed.ParsedRuleText, true, parsed.Variables).Text,
                TestLog.NoOp, references, Parser.Compiler.Compile.Language.Vb);

            compile.Success.Should().BeTrue(compile.ErrorsSummary);

            var method = compile.CompiledAssembly.GetType("AbstractionRule")!.GetMethod("Match")!;

            return (EntityAnalysisModelAbstractionRule.Match)Delegate.CreateDelegate(
                typeof(EntityAnalysisModelAbstractionRule.Match), method);
        }

        private static List<DictionaryNoBoxing<string>> Documents(params double[] amounts)
        {
            return
            [
                .. amounts.Select(amount =>
                {
                    var document = new DictionaryNoBoxing<string>();

                    document.TryAdd("Amount", amount);

                    return document;
                })
            ];
        }

        private static double[] Scan(EntityAnalysisModelAbstractionRule.Match match,
            List<DictionaryNoBoxing<string>> documents, bool descending = false)
        {
            return
            [
                .. AbstractionCursorRunner.Scan(match, documents, descending, documents[^1], [], null!, null!,
                    TestLog.NoOp).Select(d => d["Amount"].AsDouble())
            ];
        }

        [Fact]
        public void AScanVariableStartsAfreshForEveryScanOfTheSameCompiledRule()
        {
            var match = Compile(FirstTwo);
            var documents = Documents(10, 20, 30, 40);

            Scan(match, documents).Should().Equal(10, 20);
            Scan(match, documents).Should().Equal(10, 20);
            Scan(match, documents, true).Should().Equal(40, 30);
        }

        [Fact]
        public void ScansRunningAtTheSameTimeDoNotShareTheirVariables()
        {
            var match = Compile("Static total As Double\ntotal += Payload.Amount\nMatched = total >= 30");
            var documents = Documents(10, 20, 30, 40);
            var results = new double[64][];

            Parallel.For(0, results.Length, index => results[index] = Scan(match, documents));

            results.Should().AllSatisfy(result => result.Should().Equal(20, 30, 40));
        }

        [Fact]
        public void ARuleTestedOneTransactionAtATimeStartsItsScanVariablesAfreshEachTime()
        {
            var match = Compile("Static seen As Integer\nseen += 1\nMatched = seen = 1");
            var cursor = new AbstractionCursor();
            var document = Documents(10)[0];

            for (var call = 0; call < 3; call++)
            {
                AbstractionCursorRunner.Single(match, cursor, document, [], null!, null!, TestLog.NoOp).Should()
                    .BeTrue();
            }
        }

        [Fact]
        public void ARuleCalledWithoutACursorStillRunsAndKeepsNothingBetweenCalls()
        {
            var match = Compile("Static seen As Integer\nseen += 1\nMatched = seen = 1");
            var document = Documents(10)[0];

            match(document, [], null!, null!, null!, TestLog.NoOp).Should().BeTrue();
            match(document, [], null!, null!, null!, TestLog.NoOp).Should().BeTrue();
        }

        [Fact]
        public void TheScanStateIsHeldByTheCursorAndReleasedWhenTheNextScanBegins()
        {
            var match = Compile(FirstTwo);
            var cursor = new AbstractionCursor();
            var document = Documents(10)[0];

            cursor.Begin(1);
            match(document, [], null!, null!, cursor, TestLog.NoOp);

            cursor.State.Should().NotBeNull();
            cursor.State!.GetType().Name.Should().Be("AbstractionRule");

            cursor.Begin(1);

            cursor.State.Should().BeNull();
        }

        [Fact]
        public void ARuleWithOnlyRecordVariablesKeepsNoScanState()
        {
            var match = Compile("Dim doubled As Double = Payload.Amount * 2\nMatched = doubled >= 40");
            var cursor = new AbstractionCursor();

            cursor.Begin(1);
            match(Documents(20)[0], [], null!, null!, cursor, TestLog.NoOp).Should().BeTrue();

            cursor.State.Should().BeNull();
        }
    }
}

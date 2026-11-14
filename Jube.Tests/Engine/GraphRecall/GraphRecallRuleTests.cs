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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Query.Models;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation.Models;
using Jube.GraphRecallProtocol;
using Jube.Parser;
using Jube.Parser.Dependency;
using Jube.Parser.Dependency.Models;
using Jube.Parser.Models;
using Jube.Test.Infrastructure;
using Jube.Test.RuleParser;
using Xunit;

namespace Jube.Test.Engine.GraphRecall
{
    [Trait("Category", "Unit")]
    public sealed class GraphRecallRuleTests
    {
        private const string DistanceRule = "If Graph.AccountId.DistanceToConfirmed <= 2 Then\n   Return True\nEnd If";
        private const string TagRule = "If Graph.AccountId.Tag(\"Mule\").Distance <= 1 Then\n   Return True\nEnd If";

        private static RuleParseEnvironmentDto Environment()
        {
            var environment = RuleParseTests.Environment();
            environment.TtlCounters = [];
            environment.AbstractionRules = [];
            environment.Sanctions = [];
            environment.AbstractionCalculations = [];
            environment.HttpAdaptations = [];
            environment.ExhaustiveAdaptations = [];
            environment.AdvancedAnalyticsAdaptations = [];
            environment.ActivationRules = [];
            environment.Graph = ["AccountId", "ToAccountId"];

            return environment;
        }

        private static RuleParseResult Parse(string text)
        {
            return RuleParse.Execute(text, RuleParse.ActivationRule, Environment(), TestLog.NoOp,
                RuleParse.EngineReferences(RuleParse.ActivationRule), engineWrap: true);
        }

        private static RuleRunInputs Inputs(GraphEntity account)
        {
            return new RuleRunInputs { Graph = { ["AccountId"] = account } };
        }

        [Theory]
        [InlineData(2, true)]
        [InlineData(5, false)]
        public async Task AnActivationRuleReadsAScalarMemberOfTheEntityAsync(int distance, bool expected)
        {
            var parsed = Parse(DistanceRule);
            parsed.Compiled.Should().BeTrue(parsed.Message);
            parsed.ParsedRuleText.Should().Contain("(Graph(\"AccountId\")?.DistanceToConfirmed) <= 2");
            parsed.References.Should().ContainSingle(r => r.Namespace == RuleReference.Graph && r.Name == "AccountId");

            var result = await RuleRunner.RunAsync(parsed, RuleParse.ActivationRule, false,
                Inputs(new GraphEntity { DistanceToConfirmed = distance }));

            result.Error.Should().BeNull();
            result.Value.Should().Be(expected);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(3, false)]
        public async Task AnActivationRuleReadsATagProximityThroughTheMethodMemberAsync(int distance, bool expected)
        {
            var parsed = Parse(TagRule);
            parsed.Compiled.Should().BeTrue(parsed.Message);
            parsed.ParsedRuleText.Should().Contain("Graph(\"AccountId\")?.Tag(\"Mule\").Distance <= 1");

            var account = new GraphEntity();
            account.AddTag("Mule", new GraphTagProximity(5) { Distance = distance, Known = true });

            var result = await RuleRunner.RunAsync(parsed, RuleParse.ActivationRule, false, Inputs(account));

            result.Error.Should().BeNull();
            result.Value.Should().Be(expected);
        }

        [Fact]
        public async Task PatternMembershipEdgeFactsAndAnUnknownTagAllEvaluateAsync()
        {
            var parsed = Parse(
                "If Graph.AccountId.Pattern(\"FanOut\") And Graph.AccountId.Edge.Count > 2 And Graph.AccountId.Tag(\"Nope\").Distance = 5 Then\n   Return True\nEnd If");

            parsed.Compiled.Should().BeTrue(parsed.Message);
            parsed.ParsedRuleText.Should().Contain("Graph(\"AccountId\")?.Pattern(\"FanOut\")");
            parsed.ParsedRuleText.Should().Contain("(Graph(\"AccountId\")?.Edge.Count) > 2");

            var account = new GraphEntity();
            account.AddPattern("FanOut");
            account.Edge.Count = 3;

            (await RuleRunner.RunAsync(parsed, RuleParse.ActivationRule, false, Inputs(account))).Value.Should().Be(true);
            (await RuleRunner.RunAsync(parsed, RuleParse.ActivationRule, false, Inputs(new GraphEntity()))).Value.Should().Be(false);
        }

        [Fact]
        public async Task AnEntityTheModelHasButDidNotEvaluateReadsAsNothingAndTheRuleStaysFalseAsync()
        {
            var parsed = Parse("If Graph.ToAccountId.Hub Then\n   Return True\nEnd If");
            parsed.Compiled.Should().BeTrue(parsed.Message);

            var result = await RuleRunner.RunAsync(parsed, RuleParse.ActivationRule, false, Inputs(new GraphEntity()));

            result.Error.Should().BeNull();
            result.Value.Should().Be(false);
        }

        [Fact]
        public void TwoMembersOfTheSameEntityOnOneLineAreEachRewrittenWhole()
        {
            var parsed = Parse("If Graph.AccountId.Degree > 1 And Graph.AccountId.DegreeMoney > 2 Then\n   Return True\nEnd If");

            parsed.Compiled.Should().BeTrue(parsed.Message);
            parsed.ParsedRuleText.Should().Contain("(Graph(\"AccountId\")?.Degree) > 1");
            parsed.ParsedRuleText.Should().Contain("(Graph(\"AccountId\")?.DegreeMoney) > 2");
        }

        [Fact]
        public void AnEntityTheModelDoesNotHaveIsReportedAsAnErrorSpan()
        {
            var parsed = Parse("If Graph.Nope.DistanceToConfirmed > 1 Then\n   Return True\nEnd If");

            parsed.ErrorSpans.Should().Contain(e => e.Message.Contains("Graph entity does not exist for Nope") && e.Column == 3);
        }

        [Fact]
        public void AMemberTheEntityDoesNotHaveIsReportedAsAnErrorSpan()
        {
            var parsed = Parse("If Graph.AccountId.Nope > 1 Then\n   Return True\nEnd If");

            parsed.ErrorSpans.Should().Contain(e => e.Message.Contains("Graph member does not exist for Nope"));
            parsed.Compiled.Should().BeFalse();
        }

        [Fact]
        public void AGraphReferenceIsNeverDanglingInTheDependencyGraph()
        {
            var rule = new ModelEntity(ModelEntityKind.ActivationRule, 1, "Rule", true);

            var graph = new ModelDependencyGraph([rule],
            [
                new ModelReference(rule, ModelDependencyKind.RuleText, RuleReference.Graph, "AccountId", 0),
                new ModelReference(rule, ModelDependencyKind.RuleText, RuleReference.TtlCounter, "Gone", 1)
            ]);

            graph.Dangling().Should().ContainSingle(d => d.Namespace == RuleReference.TtlCounter);
        }

        [Fact]
        public void TheVocabularyDocumentsTheGraphObjectAndItsMethods()
        {
            var words = RuleVocabulary.Build([]);

            words.Should().Contain(w => w.Name == "Graph" && w.Kind == RuleWordKind.Keyword);

            var distance = words.Single(w => w.Name == "DistanceToConfirmed");
            distance.Kind.Should().Be(RuleWordKind.Keyword);
            distance.Overloads.Should().ContainSingle(o => o.Signature == "Graph.<Entity>.DistanceToConfirmed As Integer" && o.ReturnType == "Integer");

            var tag = words.Single(w => w.Name == "Tag");
            tag.Overloads.Should().ContainSingle(o => o.Signature == "Graph.<Entity>.Tag(name As String) As GraphTagProximity" && o.Parameters.Count == 1);

            words.Single(w => w.Name == "Pattern").Overloads.Should().ContainSingle(o => o.ReturnType == "Boolean");
            words.Single(w => w.Name == "Within2").Overloads.Should().ContainSingle(o => o.ReceiverType == "Graph.<Entity>.Tag(<Name>)");
            words.Single(w => w.Name == "AmountSum").Overloads.Should().ContainSingle(o => o.ReceiverType == "Graph.<Entity>.Edge");
            words.Should().NotContain(w => w.Name == "ReportTable" || w.Name == "Beyond");
        }
    }
}

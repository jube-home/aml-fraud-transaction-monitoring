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
using Jube.Data.Query.Models;
using Jube.Parser;
using Jube.Parser.Dependency;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class ModelDependencyGraphTests
    {
        private static readonly ModelEntity amount = new(ModelEntityKind.RequestXPath, 1, "Amount", true);
        private static readonly ModelEntity country = new(ModelEntityKind.RequestXPath, 2, "Country", false);
        private static readonly ModelEntity scriptAmount = new(ModelEntityKind.InlineScriptProperty, 3, "Amount", true);
        private static readonly ModelEntity function = new(ModelEntityKind.InlineFunction, 4, "Score", true);
        private static readonly ModelEntity highRisk = new(ModelEntityKind.List, 5, "HighRisk", true);
        private static readonly ModelEntity unusedList = new(ModelEntityKind.List, 6, "Unused", true);
        private static readonly ModelEntity counter = new(ModelEntityKind.TtlCounter, 7, "CountLastHour", true);
        private static readonly ModelEntity gateway = new(ModelEntityKind.GatewayRule, 8, "Gate", true);
        private static readonly ModelEntity activation = new(ModelEntityKind.ActivationRule, 9, "Alert", true);
        private static readonly ModelEntity inactiveRule = new(ModelEntityKind.ActivationRule, 10, "Old", false);
        private static readonly ModelEntity workflow = new(ModelEntityKind.CaseWorkflow, 11, "Fraud", true);

        private static ModelDependencyGraph Graph()
        {
            return new ModelDependencyGraph(
                [
                    amount, country, scriptAmount, function, highRisk, unusedList, counter, gateway, activation,
                    inactiveRule, workflow
                ],
                [
                    new ModelReference(gateway, ModelDependencyKind.RuleText, RuleReference.Payload, "Amount", 0),
                    new ModelReference(gateway, ModelDependencyKind.RuleText, RuleReference.List, "HighRisk", 1),
                    new ModelReference(activation, ModelDependencyKind.RuleText, RuleReference.Payload, "Score", 0),
                    new ModelReference(activation, ModelDependencyKind.RuleText, RuleReference.Payload, "Country", 0),
                    new ModelReference(activation, ModelDependencyKind.RuleText, RuleReference.TtlCounter, "Gone", 2),
                    new ModelReference(activation, ModelDependencyKind.TtlCounterIncrement, RuleReference.TtlCounter,
                        "CountLastHour"),
                    new ModelReference(activation, ModelDependencyKind.CaseWorkflow,
                        nameof(ModelEntityKind.CaseWorkflow),
                        "Fraud"),
                    new ModelReference(inactiveRule, ModelDependencyKind.RuleText, RuleReference.Payload, "Country", 0)
                ]);
        }

        [Fact]
        public void APayloadNameResolvesToTheRequestXPathBeforeAnInlineScriptProperty()
        {
            Graph().DependenciesOf(gateway).Should()
                .Contain(d => d.Name == "Amount" && d.Target == amount);
        }

        [Fact]
        public void APayloadNameThatIsOnlyAnInlineFunctionResolvesToTheFunction()
        {
            Graph().DependenciesOf(activation).Should().Contain(d => d.Name == "Score" && d.Target == function);
        }

        [Fact]
        public void AReferenceToAMissingNameIsDanglingWithItsLine()
        {
            Graph().Dangling().Should().ContainSingle()
                .Which.Should().Match<ModelDependency>(d =>
                    d.Dependent == activation && d.Namespace == RuleReference.TtlCounter && d.Name == "Gone" &&
                    d.Line == 2);
        }

        [Fact]
        public void OnlyActiveDependentsOfInactiveTargetsAreReported()
        {
            Graph().OnInactiveTargets().Should().ContainSingle()
                .Which.Dependent.Should().Be(activation);
        }

        [Fact]
        public void DependentsAreFoundByNameOrById()
        {
            var graph = Graph();

            graph.DependentsOf(ModelEntityKind.List, "HighRisk").Select(d => d.Dependent).Should().Equal(gateway);
            graph.DependentsOf(ModelEntityKind.TtlCounter, 7).Single().Kind.Should()
                .Be(ModelDependencyKind.TtlCounterIncrement);
            graph.DependentsOf(ModelEntityKind.CaseWorkflow, "Fraud").Single().Dependent.Should().Be(activation);
        }

        [Fact]
        public void EntitiesNothingReferencesAreUnreferenced()
        {
            Graph().Unreferenced(ModelEntityKind.List, ModelEntityKind.InlineScriptProperty).Should()
                .BeEquivalentTo([unusedList, scriptAmount]);
        }

        [Fact]
        public void NamesMatchCaseSensitivelyLikeTheParser()
        {
            var graph = new ModelDependencyGraph([amount],
                [new ModelReference(gateway, ModelDependencyKind.RuleText, RuleReference.Payload, "amount")]);

            graph.Dangling().Should().ContainSingle();
        }

        [Theory]
        [InlineData(ModelEntityKind.RequestXPath, "Payload")]
        [InlineData(ModelEntityKind.InlineFunction, "Payload")]
        [InlineData(ModelEntityKind.TtlCounter, "TTLCounter")]
        [InlineData(ModelEntityKind.HttpAdaptation, "HTTPAdaptation")]
        [InlineData(ModelEntityKind.GatewayRule, null)]
        public void NamespacesMatchTheCompletionGroups(ModelEntityKind kind, string? expected)
        {
            ModelDependencyGraph.NamespaceOf(kind).Should().Be(expected);
        }

        [Fact]
        public void TheParserRecordsEveryNamespacedReferenceItSeesIncludingUnknownNames()
        {
            var environment = RuleParseTests.Environment();
            environment.TtlCounters = ["CountLastHour"];

            var result = RuleParse.Execute(
                "If (Payload.Amount > 1 And TTLCounter.CountLastHour > 2) Then\n" +
                "   Matched = List.HighRiskTerms.Contains(Payload.Country) Or payload.Missing = \"x\"\n" +
                "End If", RuleParse.ActivationRule, environment, TestLog.NoOp, null);

            result.References.Select(r => (r.CompletionName, r.Line)).Should().BeEquivalentTo([
                ("Payload.Amount", 0), ("TTLCounter.CountLastHour", 0), ("List.HighRiskTerms", 1),
                ("Payload.Country", 1), ("Payload.Missing", 1)
            ]);
        }

        [Fact]
        public void TokensOutsideTheKnownNamespacesAreNotReferences()
        {
            var result = RuleParse.Execute("Matched = Payload.Amount.Start().MatchGreater(100)",
                RuleParse.GatewayRule, RuleParseTests.Environment(), TestLog.NoOp, null);

            result.References.Select(r => r.CompletionName).Should().Equal("Payload.Amount");
        }

        [Fact]
        public void TheBuilderJoinsRuleTextAndStructuredReferencesIntoOneGraph()
        {
            var inputs = new ModelDependencyInputsDto();
            inputs.Entities.AddRange([
                new ModelDependencyEntityDto { Kind = "RequestXPath", Id = 1, Name = "Amount", Active = true },
                new ModelDependencyEntityDto { Kind = "RequestXPath", Id = 2, Name = "Country", Active = true },
                new ModelDependencyEntityDto { Kind = "List", Id = 3, Name = "HighRiskTerms", Active = true },
                new ModelDependencyEntityDto { Kind = "GatewayRule", Id = 4, Name = "Gate", Active = true },
                new ModelDependencyEntityDto { Kind = "TtlCounter", Id = 5, Name = "PerCountry", Active = true }
            ]);
            inputs.RuleTexts.Add(new ModelDependencyRuleTextDto
            {
                Kind = "GatewayRule", Id = 4, RuleParseType = RuleParse.GatewayRule,
                Text = "If (Payload.Amount > 1 And List.HighRiskTerms.Contains(Payload.Missing)) Then\n" +
                       "   Return True\nEnd If"
            });
            inputs.References.Add(new ModelDependencyReferenceDto
            {
                DependentKind = "TtlCounter", DependentId = 5, Kind = "TtlCounterDataName", Namespace = "Payload",
                Name = "Country"
            });

            var graph = ModelDependencyGraphBuilder.Build(inputs, RuleParseTests.Environment(), TestLog.NoOp);

            graph.Dependencies.Select(d => (d.Dependent.Name, d.Kind, d.Namespace + "." + d.Name, d.Dangling))
                .Should().BeEquivalentTo([
                    ("Gate", ModelDependencyKind.RuleText, "Payload.Amount", false),
                    ("Gate", ModelDependencyKind.RuleText, "List.HighRiskTerms", false),
                    ("Gate", ModelDependencyKind.RuleText, "Payload.Missing", true),
                    ("PerCountry", ModelDependencyKind.TtlCounterDataName, "Payload.Country", false)
                ]);
        }
    }
}
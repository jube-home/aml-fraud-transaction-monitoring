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
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Engine.Integrity;
using RuleReference = Jube.Parser.RuleReference;
using Jube.Parser.Dependency;
using Xunit;
using EngineModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.Integrity
{
    [Trait("Category", "Unit")]
    public sealed class ModelIntegrityRulesTests
    {
        private static readonly DateTime now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

        private static readonly ModelEntity field = new(ModelEntityKind.RequestXPath, 1, "AccountId", true);
        private static readonly ModelEntity counter = new(ModelEntityKind.TtlCounter, 2, "PerAccount", true);
        private static readonly ModelEntity activation = new(ModelEntityKind.ActivationRule, 3, "Many", true);
        private static readonly ModelEntity inactiveList = new(ModelEntityKind.List, 4, "Old", false);
        private static readonly ModelEntity usedInactive = new(ModelEntityKind.ActivationRule, 5, "UsesOld", true);
        private static readonly ModelEntity unusedList = new(ModelEntityKind.List, 6, "Unused", true);
        private static readonly ModelEntity dormant = new(ModelEntityKind.GatewayRule, 7, "Dormant", false);

        private static ModelDependencyGraph Graph()
        {
            return new ModelDependencyGraph(
                [field, counter, activation, inactiveList, usedInactive, unusedList, dormant],
                [
                    new ModelReference(counter, ModelDependencyKind.TtlCounterDataName, RuleReference.Payload,
                        "AccountId"),
                    new ModelReference(activation, ModelDependencyKind.RuleText, RuleReference.TtlCounter, "PerAccount",
                        0),
                    new ModelReference(activation, ModelDependencyKind.RuleText, RuleReference.TtlCounter, "Missing",
                        1),
                    new ModelReference(usedInactive, ModelDependencyKind.RuleText, RuleReference.List, "Old", 0),
                    new ModelReference(dormant, ModelDependencyKind.RuleText, RuleReference.Payload, "Gone", 0)
                ]);
        }

        [Fact]
        public void DependencyFindingsSeparateBrokenActiveEntitiesFromInactiveOnes()
        {
            var checks = ModelIntegrityRules.DependencyChecks(Graph()).ToList();

            checks.Should().ContainSingle(c => c.Code == IntegrityCode.DependencyDangling &&
                                               c.Severity == IntegritySeverity.Error && c.EntityName == "Many" &&
                                               c.Message.Contains("TTLCounter.Missing"));
            checks.Should().ContainSingle(c => c.Code == IntegrityCode.DependencyDangling &&
                                               c.Severity == IntegritySeverity.Info && c.EntityName == "Dormant");
            checks.Should().ContainSingle(c => c.Code == IntegrityCode.DependencyOnInactive &&
                                               c.EntityName == "UsesOld" && c.Message.Contains("List Old"));
            checks.Should().ContainSingle(c => c.Code == IntegrityCode.EntityUnreferenced &&
                                               c.EntityName == "Unused" && c.Severity == IntegritySeverity.Info);
            checks.Should().NotContain(c => c.Code == IntegrityCode.EntityUnreferenced && c.EntityName == "Old");
        }

        [Fact]
        public void OnlyActiveEntitiesTheEngineFailedToCompileAreErrors()
        {
            var checks = ModelIntegrityRules.CompilationChecks(
            [
                new CompileStatus("GatewayRule", 1, "Broken", true, false, "BC30451: 'X' is not declared."),
                new CompileStatus("GatewayRule", 2, "BrokenButOff", false, false, "BC30451"),
                new CompileStatus("GatewayRule", 3, "Fine", true, true, null),
                new CompileStatus("GatewayRule", 4, "NeverSynchronised", true, null, null)
            ]).ToList();

            checks.Should().ContainSingle().Which.Should().Match<IntegrityCheck>(c =>
                c.Code == IntegrityCode.EngineCompileFailed && c.EntityId == 1 && c.Message.Contains("BC30451"));
        }

        [Fact]
        public void ConfigurationFindingsCoverTheModelTtlCountersAndSearchWindows()
        {
            var checks = ModelIntegrityRules.ConfigurationChecks(false, false,
                new ModelDependencyGraph([counter], []),
                [
                    new SearchWindowInput(10, "Shortened", "d", 7, "AccountId", "h", 12),
                    new SearchWindowInput(11, "Collapsed", "d", 1, "AccountId", "d", 0),
                    new SearchWindowInput(12, "Fine", "h", 1, "AccountId", "d", 1)
                ], now).ToList();

            checks.Select(c => c.Code).Should().BeEquivalentTo([
                IntegrityCode.ModelInactive, IntegrityCode.NoActiveActivationRules,
                IntegrityCode.TtlCountersDisabled, IntegrityCode.SearchKeyTtlShortensWindow,
                IntegrityCode.SearchKeyTtlCollapsesWindow
            ]);
            checks.Single(c => c.Code == IntegrityCode.SearchKeyTtlShortensWindow).EntityId.Should().Be(10);
            checks.Single(c => c.Code == IntegrityCode.SearchKeyTtlCollapsesWindow).EntityId.Should().Be(11);
        }

        [Fact]
        public void EveryCodeHasADescription()
        {
            Enum.GetValues<IntegrityCode>()
                .Should().OnlyContain(c => IntegrityCodeText.Describe(c) != c.ToString());
        }

        [Fact]
        public void WithoutAnEngineOnlyTheNodesAreChecked()
        {
            var checks = ModelIntegrityRules.EngineChecks(
                new EngineObservation(EngineStateSourceKind.Unavailable, [], []), true, Graph().Entities, now,
                TimeSpan.FromMinutes(10)).ToList();

            checks.Select(c => c.Code).Should().BeEquivalentTo([
                IntegrityCode.EngineStateUnavailable, IntegrityCode.EngineNoNodes
            ]);
        }

        [Fact]
        public void TheEnginesLoadedEntitiesAreComparedWithTheActiveOnes()
        {
            var state = new EngineModelState("node-a", 1, 1, false, now,
            [
                new EngineLoadedEntity("RequestXPath", 1, "AccountId"),
                new EngineLoadedEntity("TtlCounter", 2, "PerAccount"),
                new EngineLoadedEntity("ActivationRule", 99, "Deactivated"),
                new EngineLoadedEntity("InlineScript", 50, "NotCompared")
            ]);
            var observation = new EngineObservation(EngineStateSourceKind.Snapshot, [state],
            [
                new EngineNode("node-a", now.AddMinutes(-1), now.AddMinutes(-5)),
                new EngineNode("node-b", now.AddHours(-2), now.AddHours(-2))
            ]);

            var checks = ModelIntegrityRules.EngineChecks(observation, true, Graph().Entities, now,
                TimeSpan.FromMinutes(10)).ToList();

            checks.Should().ContainSingle(c => c.Code == IntegrityCode.EngineModelNotStarted &&
                                               c.EntityName == "node-a");
            checks.Where(c => c.Code == IntegrityCode.EngineEntityNotLoaded).Select(c => c.EntityName).Should()
                .BeEquivalentTo("Many", "UsesOld");
            checks.Single(c => c.EntityName == "Many").Message.Should().Contain("approved");
            checks.Should().ContainSingle(c => c.Code == IntegrityCode.EngineEntityStale &&
                                               c.EntityName == "Deactivated");
            checks.Should().ContainSingle(c => c.Code == IntegrityCode.EngineNodeStale && c.EntityName == "node-b");
            checks.Should().ContainSingle(c => c.Code == IntegrityCode.EngineModelNotLoaded &&
                                               c.EntityName == "node-b");
            checks.Should().NotContain(c => c.EntityName == "NotCompared" || c.EntityName == "Unused");
        }

        [Fact]
        public void AnInactiveModelIsNotExpectedToBeLoaded()
        {
            var observation = new EngineObservation(EngineStateSourceKind.InProcess, [],
                [new EngineNode("node-a", now, now)]);

            ModelIntegrityRules.EngineChecks(observation, false, Graph().Entities, now, TimeSpan.FromMinutes(10))
                .Should().BeEmpty();
        }

        [Fact]
        public void TheSnapshotRecordsWhatTheEngineModelHasLoadedAndWhetherItStarted()
        {
            var model = new EngineModel { Started = true };
            model.Instance.Id = 7;
            model.Instance.TenantRegistryId = 3;
            model.Collections.EntityAnalysisModelRequestXPaths.Add(new EntityAnalysisModelRequestXPath
                { Id = 1, Name = "AccountId" });
            model.Collections.ModelGatewayRules.Add(new EntityModelGatewayRule
                { EntityAnalysisModelGatewayRuleId = 4, Name = "Gate" });
            model.Collections.ModelActivationRules.Add(new EntityAnalysisModelActivationRule
                { Id = 5, Name = "Act" });

            var state = EngineModelStateBuilder.FromModel(model, "node-a", now);

            (state.EntityAnalysisModelId, state.TenantRegistryId, state.Started, state.Instance).Should()
                .Be((7, 3, true, "node-a"));
            state.Loaded.Should().BeEquivalentTo(new List<EngineLoadedEntity>
            {
                new("RequestXPath", 1, "AccountId"), new("GatewayRule", 4, "Gate"), new("ActivationRule", 5, "Act")
            });
        }

        [Fact]
        public void TheGraphDrawsUsesToTheUsedWithMissingNamesAsTheirOwnNodes()
        {
            var built = ModelGraphBuilder.Build(Graph());

            built.Nodes.Should().Contain(n => n.Id == "Missing:TTLCounter.Missing" && n.Kind == "Missing");
            built.Edges.Should().Contain(e => e.From == "ActivationRule:3" && e.To == "TtlCounter:2" &&
                                              e.Label == "TTLCounter.PerAccount" && !e.Dashed);
            built.Edges.Should().Contain(e => e.From == "TtlCounter:2" && e.To == "RequestXPath:1" &&
                                              e.Label == "TtlCounterDataName" && e.Dashed);
            built.Nodes.Single(n => n.Id == "List:4").Detail.Should().Be("Inactive");
            built.Truncated.Should().BeFalse();
        }

        [Fact]
        public void AFocusKeepsOnlyTheNeighbourhoodWithinTheRadius()
        {
            var one = ModelGraphBuilder.Build(Graph(), field, 1);
            var two = ModelGraphBuilder.Build(Graph(), field);

            one.Focus.Should().Be("RequestXPath:1");
            one.Nodes.Select(n => n.Id).Should().BeEquivalentTo("RequestXPath:1", "TtlCounter:2");
            two.Nodes.Select(n => n.Id).Should().BeEquivalentTo("RequestXPath:1", "TtlCounter:2", "ActivationRule:3");
        }

        [Fact]
        public void ABigGraphKeepsTheMostConnectedNodesAndSaysSo()
        {
            var built = ModelGraphBuilder.Build(Graph(), maxNodes: 3);

            built.Truncated.Should().BeTrue();
            built.Nodes.Should().HaveCount(3);
            built.Nodes.Select(n => n.Id).Should().Contain("ActivationRule:3");
            built.Edges.Should().OnlyContain(e => built.Nodes.Any(n => n.Id == e.From) &&
                                                  built.Nodes.Any(n => n.Id == e.To));
        }
    }
}
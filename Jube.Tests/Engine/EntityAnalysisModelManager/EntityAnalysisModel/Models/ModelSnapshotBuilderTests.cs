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
using FluentAssertions;
using FluentAssertions.Execution;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models
{
    [Trait("Category", "Unit")]
    public class ModelSnapshotBuilderTests
    {
        private static EntityAnalysisModelActivationRule ActivationRule(int id, long evaluations, long activations,
            DateTime counterDate = default)
        {
            return new EntityAnalysisModelActivationRule
            {
                Id = id,
                Name = $"Rule{id}",
                Counters = new RuleCounters
                {
                    EvaluationCounter = evaluations,
                    ActivationCounter = activations,
                    ActivationCounterDate = counterDate
                }
            };
        }

        private static EntityModelGatewayRule GatewayRule(int id, long evaluations, long activations,
            DateTime counterDate = default)
        {
            return new EntityModelGatewayRule
            {
                EntityAnalysisModelGatewayRuleId = id,
                Name = $"Gateway{id}",
                Counters = new RuleCounters
                {
                    EvaluationCounter = evaluations,
                    ActivationCounter = activations,
                    ActivationCounterDate = counterDate
                }
            };
        }

        [Fact]
        public void ABuilderOverNoPreviousSnapshotProducesTheFirstGeneration()
        {
            var snapshot = new ModelSnapshotBuilder(null).Build();

            using var scope = new AssertionScope();
            snapshot.Generation.Should().Be(1);
            snapshot.ModelActivationRules.Should().BeEmpty();
            snapshot.EntityAnalysisModelLists.Should().BeEmpty();
        }

        [Fact]
        public void EachBuildIncrementsTheGenerationFromThePreviousSnapshot()
        {
            var first = new ModelSnapshotBuilder(null).Build();
            var second = new ModelSnapshotBuilder(first).Build();
            var third = new ModelSnapshotBuilder(second).Build();

            using var scope = new AssertionScope();
            first.Generation.Should().Be(1);
            second.Generation.Should().Be(2);
            third.Generation.Should().Be(3);
        }

        [Fact]
        public void CollectionsThatTheCycleDoesNotReplaceAreCarriedForwardByReference()
        {
            var previous = new ModelSnapshotBuilder(null)
            {
                ModelActivationRules = [ActivationRule(1, 0, 0)],
                EntityAnalysisModelLists = new Dictionary<string, List<string>> { ["Deny"] = ["1.2.3.4"] }
            }.Build();

            var next = new ModelSnapshotBuilder(previous).Build();

            using var scope = new AssertionScope();
            next.ModelActivationRules.Should().BeSameAs(previous.ModelActivationRules);
            next.EntityAnalysisModelLists.Should().BeSameAs(previous.EntityAnalysisModelLists);
        }

        [Fact]
        public void ACollectionReplacedInTheCycleDoesNotAffectThePreviousSnapshot()
        {
            var previous = new ModelSnapshotBuilder(null)
            {
                ModelActivationRules = [ActivationRule(1, 0, 0)]
            }.Build();

            var builder = new ModelSnapshotBuilder(previous)
            {
                ModelActivationRules = [ActivationRule(2, 0, 0)]
            };
            var next = builder.Build();

            using var scope = new AssertionScope();
            previous.ModelActivationRules.Should().HaveCount(1);
            previous.ModelActivationRules[0].Id.Should().Be(1);
            next.ModelActivationRules.Should().HaveCount(1);
            next.ModelActivationRules[0].Id.Should().Be(2);
        }

        [Fact]
        public void TheSearchKeysDictionaryIsCopiedSoMidCycleAdditionsAreNotVisibleUntilPublished()
        {
            var previous = new ModelSnapshotBuilder(null)
            {
                DistinctSearchKeys = new Dictionary<string, DistinctSearchKey>
                {
                    ["AccountId"] = new() { SearchKey = "AccountId" }
                }
            }.Build();

            var builder = new ModelSnapshotBuilder(previous);
            builder.DistinctSearchKeys["CardNumber"] = new DistinctSearchKey { SearchKey = "CardNumber" };

            using var scope = new AssertionScope();
            previous.DistinctSearchKeys.Should().ContainKey("AccountId");
            previous.DistinctSearchKeys.Should().NotContainKey("CardNumber");
            builder.Build().DistinctSearchKeys.Should().ContainKeys("AccountId", "CardNumber");
        }

        [Fact]
        public void ActivationRuleCountersAreSharedWithTheReplacementRuleSoLateIncrementsAreKept()
        {
            var counterDate = new DateTime(2026, 9, 29, 10, 0, 0, DateTimeKind.Utc);
            var outgoing = ActivationRule(7, 41, 5, counterDate);
            var previous = new ModelSnapshotBuilder(null) { ModelActivationRules = [outgoing] }.Build();

            var replacement = ActivationRule(7, 0, 0);
            var next = new ModelSnapshotBuilder(previous) { ModelActivationRules = [replacement] }.Build();

            using var scope = new AssertionScope();
            next.ModelActivationRules[0].EvaluationCounter.Should().Be(41);
            next.ModelActivationRules[0].ActivationCounter.Should().Be(5);
            next.ModelActivationRules[0].ActivationCounterDate.Should().Be(counterDate);
            next.ModelActivationRules[0].Counters.Should().BeSameAs(outgoing.Counters);
            outgoing.Counters.EvaluationCounter++;
            next.ModelActivationRules[0].EvaluationCounter.Should().Be(42);
        }

        [Fact]
        public void GatewayRuleCountersAreSharedWithTheReplacementRuleSoLateIncrementsAreKept()
        {
            var counterDate = new DateTime(2026, 9, 29, 11, 0, 0, DateTimeKind.Utc);
            var outgoing = GatewayRule(3, 17, 2, counterDate);
            var previous = new ModelSnapshotBuilder(null) { ModelGatewayRules = [outgoing] }.Build();

            var replacement = GatewayRule(3, 0, 0);
            var next = new ModelSnapshotBuilder(previous) { ModelGatewayRules = [replacement] }.Build();

            using var scope = new AssertionScope();
            next.ModelGatewayRules[0].EvaluationCounter.Should().Be(17);
            next.ModelGatewayRules[0].ActivationCounter.Should().Be(2);
            next.ModelGatewayRules[0].ActivationCounterDate.Should().Be(counterDate);
            next.ModelGatewayRules[0].Counters.Should().BeSameAs(outgoing.Counters);
            outgoing.Counters.EvaluationCounter++;
            next.ModelGatewayRules[0].EvaluationCounter.Should().Be(18);
        }

        [Fact]
        public void ARuleThatIsNewInThisGenerationKeepsItsOwnCounters()
        {
            var previous = new ModelSnapshotBuilder(null) { ModelActivationRules = [ActivationRule(1, 9, 9)] }.Build();

            var next = new ModelSnapshotBuilder(previous) { ModelActivationRules = [ActivationRule(2, 0, 0)] }.Build();

            next.ModelActivationRules[0].EvaluationCounter.Should().Be(0);
        }

        [Fact]
        public void CountersAreNotDisturbedWhenTheCycleDidNotReplaceTheRules()
        {
            var rule = ActivationRule(1, 12, 3);
            var previous = new ModelSnapshotBuilder(null) { ModelActivationRules = [rule] }.Build();

            var next = new ModelSnapshotBuilder(previous).Build();

            using var scope = new AssertionScope();
            next.ModelActivationRules.Should().BeSameAs(previous.ModelActivationRules);
            rule.EvaluationCounter.Should().Be(12);
            rule.ActivationCounter.Should().Be(3);
        }

        [Fact]
        public void ARuleThatAppearsTwiceInTheOutgoingGenerationDoesNotFailTheCarryForward()
        {
            var previous = new ModelSnapshotBuilder(null)
            {
                ModelActivationRules = [ActivationRule(1, 4, 1), ActivationRule(1, 6, 2)]
            }.Build();

            var act = () => new ModelSnapshotBuilder(previous)
            {
                ModelActivationRules = [ActivationRule(1, 0, 0)]
            }.Build();

            act.Should().NotThrow();
        }

        [Fact]
        public void BuildCarriesEveryCollectionOntoTheSnapshot()
        {
            var builder = new ModelSnapshotBuilder(null)
            {
                Users = ["caller"],
                ModelAbstractionRules = [new EntityAnalysisModelAbstractionRule { Name = "Abstraction" }],
                ModelTtlCounters = [new EntityAnalysisModelTtlCounter { Name = "Counter" }],
                EntityAnalysisModelSanctions = [new EntityAnalysisModelSanction { Name = "Sanction" }],
                ModelActivationRules = [ActivationRule(1, 0, 0)],
                ModelGatewayRules = [GatewayRule(1, 0, 0)],
                EntityAnalysisModelAdaptations =
                    [new EntityAnalysisModelHttpAdaptation(1, false, 1000) { Name = "Adaptation" }],
                EntityAnalysisModelRequestXPaths = [new EntityAnalysisModelRequestXPath { Name = "Field" }],
                EntityAnalysisModelAbstractionCalculations =
                    [new EntityAnalysisModelAbstractionCalculation { Name = "Calculation" }],
                EntityAnalysisModelInlineFunctions = [new EntityAnalysisModelInlineFunction { Name = "Function" }],
                EntityAnalysisModelTags = [new EntityAnalysisModelTag { Name = "Tag" }],
                ParseIndexCache = new Dictionary<int, string> { [1] = "Field" },
                EntityAnalysisModelLists = new Dictionary<string, List<string>> { ["Deny"] = ["1.2.3.4"] },
                EntityAnalysisModelOverrides =
                    new Dictionary<string, Dictionary<string, EntityAnalysisModelOverride>>
                    {
                        ["Country"] = new() { ["IR"] = new EntityAnalysisModelOverride() }
                    }
            };

            var snapshot = builder.Build();

            using var scope = new AssertionScope();
            snapshot.Users.Should().BeSameAs(builder.Users);
            snapshot.ModelAbstractionRules.Should().BeSameAs(builder.ModelAbstractionRules);
            snapshot.ModelTtlCounters.Should().BeSameAs(builder.ModelTtlCounters);
            snapshot.EntityAnalysisModelSanctions.Should().BeSameAs(builder.EntityAnalysisModelSanctions);
            snapshot.ModelActivationRules.Should().BeSameAs(builder.ModelActivationRules);
            snapshot.ModelGatewayRules.Should().BeSameAs(builder.ModelGatewayRules);
            snapshot.EntityAnalysisModelAdaptations.Should().BeSameAs(builder.EntityAnalysisModelAdaptations);
            snapshot.EntityAnalysisModelRequestXPaths.Should().BeSameAs(builder.EntityAnalysisModelRequestXPaths);
            snapshot.EntityAnalysisModelAbstractionCalculations.Should()
                .BeSameAs(builder.EntityAnalysisModelAbstractionCalculations);
            snapshot.EntityAnalysisModelInlineFunctions.Should().BeSameAs(builder.EntityAnalysisModelInlineFunctions);
            snapshot.EntityAnalysisModelTags.Should().BeSameAs(builder.EntityAnalysisModelTags);
            snapshot.ParseIndexCache.Should().BeSameAs(builder.ParseIndexCache);
            snapshot.EntityAnalysisModelLists.Should().BeSameAs(builder.EntityAnalysisModelLists);
            snapshot.EntityAnalysisModelOverrides.Should().BeSameAs(builder.EntityAnalysisModelOverrides);
        }
    }
}
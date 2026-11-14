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
using Jube.Data.Poco;
using Jube.Dictionary;
using Jube.Engine.GraphRecall;
using Jube.GraphRecallProtocol;
using Xunit;

namespace Jube.Test.Engine.GraphRecall
{
    [Trait("Category", "Unit")]
    public sealed class GraphRecallEvaluatorTests
    {
        private static readonly Guid registry = Guid.NewGuid();
        private static readonly Guid otherRegistry = Guid.NewGuid();
        private static readonly DateTime reference = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

        private static EntityAnalysisModelGraphRecall Row(int id, string name, string field, string? nodeKey = null,
            string? counterpart = null, Guid? registryGuid = null, byte reportTable = 1, byte responsePayload = 0)
        {
            return new EntityAnalysisModelGraphRecall
            {
                Id = id, Name = name, RequestXPathName = field, NodeKey = nodeKey, CounterpartName = counterpart,
                GraphRegistryGuid = registryGuid, Active = 1, Deleted = 0, ReportTable = reportTable,
                ResponsePayload = responsePayload
            };
        }

        private static GraphRecallPlan Plan(Guid? registryGuid = null)
        {
            return GraphRecallPlan.Build(
            [
                Row(1, "AccountId", "AccountId", counterpart: "Beneficiary", registryGuid: registryGuid),
                Row(2, "Beneficiary", "Beneficiary Account Id", "AccountId", "AccountId", registryGuid),
                Row(3, "DeviceId", "DeviceId", responsePayload: 1, reportTable: 0)
            ], 4);
        }

        private static GraphRecallSnapshot Snapshot(bool hashKeys = false)
        {
            var builder = new GraphRecallSnapshotBuilder(1, hashKeys, 1000, 1000, 4);

            builder.TryAddNode("AccountId", "A1", new GraphRecallNode
            {
                Id = 1, DistanceToConfirmed = 2, CommunityId = 10, Degree = 4, ConfirmedWithin2 = 1,
                PropagationScore = 0.5f, CommunityConfirmedRate = 0.25f, CommunityTenantRateRatio = 1.5f,
                Tags = [new GraphRecallTag("Mule", 2, 0, 1, 1)]
            });

            builder.TryAddNode("AccountId", "A3", new GraphRecallNode
            {
                Id = 3, DistanceToConfirmed = 0, CommunityId = 10, Tags = [new GraphRecallTag("Mule", 0, 1, 1, 1)]
            });

            builder.TryAddNode("AccountId", "B1",
                new GraphRecallNode { Id = 2, DistanceToConfirmed = 1, CommunityId = 11, PatternKinds = 1 | (1 << 2) });

            builder.TryAddNode("AccountId", "H1", new GraphRecallNode { Id = 6, Hub = true, DistanceToConfirmed = 0 });
            builder.TryAddNode("DeviceId", "D1", new GraphRecallNode { Id = 4, Hub = true, Degree = 900 });
            builder.TryAddEdge(1, 2, registry, new GraphRecallEdge(3, 300, reference.AddDays(-10), reference.AddDays(-1), 2));
            builder.TryAddEdge(2, 1, otherRegistry, new GraphRecallEdge(1, 50, reference.AddDays(-20), reference.AddDays(-20), 1));
            builder.AddDefinition(new GraphRecallDefinition(registry, "AccountId", "AccountId"));
            builder.AddDefinition(new GraphRecallDefinition(otherRegistry, "AccountId", "AccountId"));
            builder.AddDefinition(new GraphRecallDefinition(registry, "AccountId", "DeviceId"));

            return builder.Build(TimeSpan.Zero, 0);
        }

        private static Dictionary<string, GraphEntity> Evaluate(GraphRecallPlan plan, GraphRecallSnapshot? snapshot,
            params string?[] values)
        {
            var target = new PooledDictionary<string, GraphEntity>();
            GraphRecallEvaluator.Evaluate(plan, snapshot, values, reference, target);

            return new Dictionary<string, GraphEntity>(target);
        }

        [Fact]
        public void EveryConfiguredEntityIsWrittenOnceWithItsConfiguredFlags()
        {
            var plan = Plan();
            var graph = Evaluate(plan, Snapshot(), "A1", "B1", "D1");

            graph.Keys.Should().BeEquivalentTo(plan.Names);
            graph["AccountId"].ReportTable.Should().BeTrue();
            graph["DeviceId"].ReportTable.Should().BeFalse();
            plan.Entities[2].ResponsePayload.Should().BeTrue();
            plan.Entities[1].Field.Should().Be("Beneficiary_Account_Id");
            plan.Entities[1].NodeKey.Should().Be("AccountId");
            plan.Entities[0].CounterpartIndex.Should().Be(1);
            plan.Entities[1].CounterpartIndex.Should().Be(0);
            plan.Entities[2].CounterpartIndex.Should().Be(-1);
        }

        [Fact]
        public void KnownNodesSurfaceTheirFeaturesTagsPatternsAndEdgeFacts()
        {
            var graph = Evaluate(Plan(), Snapshot(), "A1", "B1", "D1");
            var account = graph["AccountId"];
            var beneficiary = graph["Beneficiary"];
            var device = graph["DeviceId"];

            account.Known.Should().BeTrue();
            account.Value.Should().Be("A1");
            account.DistanceToConfirmed.Should().Be(2);
            account.RelaxedDistanceToConfirmed.Should().Be(2);
            account.Degree.Should().Be(4);
            account.ConfirmedWithin2.Should().Be(1);
            account.PropagationScore.Should().BeApproximately(0.5, 1e-6);
            account.CommunityConfirmedRate.Should().BeApproximately(0.25, 1e-6);
            account.CommunityRateRatio.Should().BeApproximately(1.5, 1e-6);
            account.CommunityId.Should().Be(10);
            account.Tag("Mule").Distance.Should().Be(2);
            account.Tag("Mule").Within2.Should().Be(1);
            account.Tag("Mule").Known.Should().BeTrue();
            account.NearestTag.Should().Be("Mule");
            account.NearestTagDistance.Should().Be(2);
            account.Tag("Fraud").Distance.Should().Be(5);
            account.BridgesCommunities.Should().BeTrue();
            account.BridgesToTagged.Should().BeFalse();
            account.Edge.Exists.Should().BeTrue();
            account.Edge.Count.Should().Be(4);
            account.Edge.AmountSum.Should().Be(350);
            account.Edge.FirstSeenDays.Should().BeApproximately(20, 1e-6);
            account.Edge.LastSeenDays.Should().BeApproximately(1, 1e-6);
            account.Edge.Kind.Should().Be("MoneyFlow");

            beneficiary.DistanceToConfirmed.Should().Be(1);
            beneficiary.Pattern("FanOut").Should().BeTrue();
            beneficiary.Pattern("GatherScatter").Should().BeTrue();
            beneficiary.Pattern("FanIn").Should().BeFalse();
            beneficiary.BridgesToTagged.Should().BeTrue();
            beneficiary.Edge.Count.Should().Be(4);

            device.Hub.Should().BeTrue();
            device.Degree.Should().Be(900);
            device.DistanceToConfirmed.Should().Be(5);
            device.BridgesToTagged.Should().BeTrue();
            device.Edge.Exists.Should().BeFalse();
            device.BridgesCommunities.Should().BeFalse();
        }

        [Fact]
        public void TheRegistryGuidScopesEdgeFactsAndRelaxation()
        {
            var graph = Evaluate(Plan(registry), Snapshot(), "A1", "B1", "D1");

            graph["AccountId"].Edge.Count.Should().Be(3);
            graph["AccountId"].Edge.AmountSum.Should().Be(300);
            graph["AccountId"].Edge.FirstSeenDays.Should().BeApproximately(10, 1e-6);
            graph["Beneficiary"].RelaxedDistanceToConfirmed.Should().Be(1);

            var unknownRegistry = Evaluate(Plan(Guid.NewGuid()), Snapshot(), "A1", "ZZ", "D1");

            unknownRegistry["AccountId"].Edge.Exists.Should().BeFalse();
            unknownRegistry["Beneficiary"].RelaxedDistanceToConfirmed.Should().Be(5);
        }

        [Fact]
        public void AnUnknownCounterpartInheritsTheKnownDistancePlusOneAndStaysUnknown()
        {
            var graph = Evaluate(Plan(), Snapshot(), "A1", "ZZ", null);
            var beneficiary = graph["Beneficiary"];

            beneficiary.Known.Should().BeFalse();
            beneficiary.Value.Should().Be("ZZ");
            beneficiary.DistanceToConfirmed.Should().Be(5);
            beneficiary.RelaxedDistanceToConfirmed.Should().Be(3);
            beneficiary.Degree.Should().Be(0);
            beneficiary.Edge.Exists.Should().BeFalse();
            beneficiary.BridgesCommunities.Should().BeFalse();
            beneficiary.BridgesToTagged.Should().BeTrue();
            graph["AccountId"].RelaxedDistanceToConfirmed.Should().Be(2);
            graph["DeviceId"].Value.Should().BeEmpty();
            graph["DeviceId"].BridgesToTagged.Should().BeFalse();
        }

        [Fact]
        public void RelaxationNeverPassesThroughAHubAndNeedsBothValuesPresent()
        {
            var throughHub = Evaluate(Plan(), Snapshot(), "ZZ", "H1", "D1");
            throughHub["AccountId"].RelaxedDistanceToConfirmed.Should().Be(5);
            throughHub["Beneficiary"].Hub.Should().BeTrue();

            var missing = Evaluate(Plan(), Snapshot(), "A1", null, null);
            missing["Beneficiary"].Known.Should().BeFalse();
            missing["Beneficiary"].RelaxedDistanceToConfirmed.Should().Be(5);
            missing["Beneficiary"].BridgesToTagged.Should().BeFalse();
            missing["AccountId"].RelaxedDistanceToConfirmed.Should().Be(2);
            missing["AccountId"].Edge.Exists.Should().BeFalse();
        }

        [Fact]
        public void WithoutASnapshotEveryEntityCarriesItsTypedDefaults()
        {
            var plan = Plan();
            var graph = Evaluate(plan, null, "A1", "B1", "D1");

            graph.Keys.Should().BeEquivalentTo(plan.Names);
            graph["AccountId"].Known.Should().BeFalse();
            graph["AccountId"].Value.Should().Be("A1");
            graph["AccountId"].DistanceToConfirmed.Should().Be(5);
            graph["AccountId"].RelaxedDistanceToConfirmed.Should().Be(5);
            graph["AccountId"].Tag("Mule").Distance.Should().Be(5);
            graph["AccountId"].Edge.Exists.Should().BeFalse();
            graph["AccountId"].BridgesCommunities.Should().BeFalse();
            graph["AccountId"].BridgesToTagged.Should().BeFalse();
            Evaluate(GraphRecallPlan.Empty, null).Should().BeEmpty();
        }

        [Fact]
        public void HashedKeysResolveTheSameNodesAsStringKeys()
        {
            var plain = Evaluate(Plan(), Snapshot(), "A1", "B1", "D1");
            var hashed = Evaluate(Plan(), Snapshot(true), "A1", "B1", "D1");

            hashed.Should().BeEquivalentTo(plain);
        }

        [Fact]
        public void ThePlanKeepsActiveNamedRowsOnceAndResolvesCounterpartsByName()
        {
            var plan = GraphRecallPlan.Build(
            [
                Row(5, "AccountId", "AccountId", counterpart: "accountid"),
                Row(6, "accountid", "Other"),
                Row(7, "Self", "Self", counterpart: "Self"),
                Row(8, "Lonely", "Lonely", counterpart: "Missing"),
                new EntityAnalysisModelGraphRecall { Id = 9, Name = "Off", RequestXPathName = "Off", Active = 0, Deleted = 0 },
                new EntityAnalysisModelGraphRecall { Id = 10, Name = "Gone", RequestXPathName = "Gone", Active = 1, Deleted = 1 },
                new EntityAnalysisModelGraphRecall { Id = 11, Name = " ", RequestXPathName = "Blank", Active = 1 }
            ], 3);

            plan.Any.Should().BeTrue();
            plan.DistanceCap.Should().Be(3);
            plan.Beyond.Should().Be(4);
            plan.Names.Should().Equal("AccountId", "Self", "Lonely");
            plan.Entities[0].CounterpartIndex.Should().Be(-1);
            plan.Entities[0].CounterpartName.Should().Be("accountid");
            plan.Entities[1].CounterpartIndex.Should().Be(-1);
            plan.Entities[2].CounterpartIndex.Should().Be(-1);
            plan.Entities[2].ReportTable.Should().BeTrue();
            GraphRecallPlan.Empty.Any.Should().BeFalse();
            GraphRecallPlan.ResolveDistanceCap(null).Should().Be(GraphRecallPlan.DefaultDistanceCap);
            GraphRecallPlan.ResolveDistanceCap("0").Should().Be(GraphRecallPlan.DefaultDistanceCap);
            GraphRecallPlan.ResolveDistanceCap("6").Should().Be(6);
        }
    }
}

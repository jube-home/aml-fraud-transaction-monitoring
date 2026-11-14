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
using Jube.Dto.Options;
using Jube.Engine.GraphRecall;
using Jube.GraphRecallProtocol;
using Xunit;

namespace Jube.Test.Engine.GraphRecall
{
    [Trait("Category", "Unit")]
    public sealed class GraphRecallEvaluatorEdgeCaseTests
    {
        private static readonly DateTime reference = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

        private static GraphRecallPlan TwoEntityPlan()
        {
            return GraphRecallPlan.Build(
            [
                new EntityAnalysisModelGraphRecall { Id = 1, Name = "A", RequestXPathName = "A", Active = 1, Deleted = 0 },
                new EntityAnalysisModelGraphRecall { Id = 2, Name = "B", RequestXPathName = "B", Active = 1, Deleted = 0 }
            ], 4);
        }

        private static Dictionary<string, GraphEntity> Evaluate(GraphRecallPlan plan, GraphRecallSnapshot? snapshot,
            params string?[] values)
        {
            var target = new PooledDictionary<string, GraphEntity>();
            GraphRecallEvaluator.Evaluate(plan, snapshot, values, reference, target);

            return new Dictionary<string, GraphEntity>(target);
        }

        private static GraphRecallSnapshot Snapshot(GraphRecallNode a, GraphRecallNode b)
        {
            var builder = new GraphRecallSnapshotBuilder(1, false, 100, 100, 4);
            builder.TryAddNode("A", "a1", a);
            builder.TryAddNode("B", "b1", b);

            return builder.Build(TimeSpan.Zero, 0);
        }

        [Fact]
        public void ATaggedEntityDoesNotBridgeToItselfButBridgesToTheOtherTaggedEntity()
        {
            var tagged = new GraphRecallNode { Id = 1, Tags = [new GraphRecallTag("Mule", 1, 1, 1, 1)] };
            var plain = new GraphRecallNode { Id = 2 };

            var onlyFirst = Evaluate(TwoEntityPlan(), Snapshot(tagged, plain), "a1", "b1");
            onlyFirst["A"].BridgesToTagged.Should().BeFalse();
            onlyFirst["B"].BridgesToTagged.Should().BeTrue();

            var both = Evaluate(TwoEntityPlan(), Snapshot(tagged, tagged with { Id = 2 }), "a1", "b1");
            both["A"].BridgesToTagged.Should().BeTrue();
            both["B"].BridgesToTagged.Should().BeTrue();
        }

        [Fact]
        public void ATagMoreThanTwoHopsAwayDoesNotCountAsTaggedNearby()
        {
            var far = new GraphRecallNode { Id = 1, Tags = [new GraphRecallTag("Mule", 3, 0, 0, 1)] };
            var plain = new GraphRecallNode { Id = 2 };

            var graph = Evaluate(TwoEntityPlan(), Snapshot(far, plain), "a1", "b1");

            graph["A"].BridgesToTagged.Should().BeFalse();
            graph["B"].BridgesToTagged.Should().BeFalse();
        }

        [Fact]
        public void AnAbsentValueNeverBridgesEvenWhenTheOtherEntityIsTagged()
        {
            var tagged = new GraphRecallNode { Id = 1, Tags = [new GraphRecallTag("Mule", 0, 1, 1, 1)] };

            var graph = Evaluate(TwoEntityPlan(), Snapshot(tagged, new GraphRecallNode { Id = 2 }), "a1", null);

            graph["B"].BridgesToTagged.Should().BeFalse();
            graph["B"].Known.Should().BeFalse();
        }

        [Fact]
        public void EveryPatternKindBitMapsToItsNamedMembershipAndOnlyThat()
        {
            foreach (var kind in Enum.GetValues<GraphPatternKind>())
            {
                var node = new GraphRecallNode { Id = 1, PatternKinds = 1 << ((int)kind - 1) };
                var graph = Evaluate(TwoEntityPlan(), Snapshot(node, new GraphRecallNode { Id = 2 }), "a1", "b1");

                graph["A"].Patterns.Should().Equal(kind.ToString());
                graph["B"].Patterns.Should().BeEmpty();
            }
        }

        [Fact]
        public void AnOutOfRangeStoredDistanceIsClampedToBeyond()
        {
            var node = new GraphRecallNode { Id = 1, DistanceToConfirmed = 99, DistanceToNotConfirmed = -1 };

            var graph = Evaluate(TwoEntityPlan(), Snapshot(node, new GraphRecallNode { Id = 2 }), "a1", "b1");

            graph["A"].DistanceToConfirmed.Should().Be(5);
            graph["A"].DistanceToNotConfirmed.Should().Be(5);
        }

        [Fact]
        public void ShortValueArraysLeaveTheMissingEntitiesAbsent()
        {
            var graph = Evaluate(TwoEntityPlan(), null, "a1");

            graph["A"].Value.Should().Be("a1");
            graph["B"].Value.Should().BeEmpty();
        }

        [Fact]
        public void TheCacheKeepsOneSnapshotPerTenantAndNeverFallsBackToAnother()
        {
            var cache = new GraphRecallCache();
            var one = new GraphRecallSnapshotBuilder(1, false, 10, 10, 4).Build(TimeSpan.Zero, 0);
            var two = new GraphRecallSnapshotBuilder(2, false, 10, 10, 4).Build(TimeSpan.Zero, 0);

            cache.Publish(one);
            cache.Publish(two);

            cache.TryGet(1, out var got1).Should().BeTrue();
            got1.Should().BeSameAs(one);
            cache.TryGet(2, out var got2).Should().BeTrue();
            got2.Should().BeSameAs(two);
            cache.TryGet(3, out var none).Should().BeFalse();
            none.Should().BeNull();

            cache.Remove(1).Should().BeTrue();
            cache.TryGet(1, out _).Should().BeFalse();
            cache.TryGet(2, out _).Should().BeTrue();
        }

        [Fact]
        public void ASnapshotNeverResolvesANodeAnotherTenantsBuilderHolds()
        {
            var tenantOne = new GraphRecallSnapshotBuilder(1, false, 10, 10, 4);
            var tenantTwo = new GraphRecallSnapshotBuilder(2, false, 10, 10, 4);
            tenantOne.TryAddNode("A", "shared", new GraphRecallNode { Id = 1 });
            tenantTwo.TryAddNode("A", "only-two", new GraphRecallNode { Id = 2 });
            tenantTwo.TryAddEdge(1, 2, Guid.NewGuid(), new GraphRecallEdge(1, 1, reference, reference, 1))
                .Should().BeFalse();

            var snapshotOne = tenantOne.Build(TimeSpan.Zero, 0);

            snapshotOne.TryGetNode("A", "only-two", out _).Should().BeFalse();
            tenantTwo.Build(TimeSpan.Zero, 0).TryGetNode("A", "shared", out _).Should().BeFalse();
        }
    }
}

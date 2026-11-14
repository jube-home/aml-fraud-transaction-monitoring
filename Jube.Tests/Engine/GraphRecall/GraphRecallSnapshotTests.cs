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
using System.Linq;
using FluentAssertions;
using Jube.Data.Query.GetGraphRecallSnapshotQuery;
using Jube.Engine.GraphRecall;
using Xunit;

namespace Jube.Test.Engine.GraphRecall
{
    [Trait("Category", "Unit")]
    public sealed class GraphRecallSnapshotTests
    {
        private static readonly Guid registry = Guid.NewGuid();
        private static readonly Guid otherRegistry = Guid.NewGuid();

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TheIndexFindsAddedValuesAndMissesEverythingElse(bool hashKeys)
        {
            var index = new GraphRecallNodeIndex(hashKeys);
            index.Add("A1", new GraphRecallNode { Id = 7 });

            index.HashKeys.Should().Be(hashKeys);
            index.Count.Should().Be(1);
            index.TryGet("A1", out var node).Should().BeTrue();
            node.Id.Should().Be(7);
            index.TryGet("A2", out _).Should().BeFalse();
            index.TryGet(string.Empty, out _).Should().BeFalse();
            index.TryGet(null!, out _).Should().BeFalse();
        }

        [Fact]
        public void TheHashIsDeterministicAndSeparatesNearbyValues()
        {
            GraphRecallHash.Compute("A1").Should().Be(GraphRecallHash.Compute("A1"));
            GraphRecallHash.Compute("A1").Should().NotBe(GraphRecallHash.Compute("A2"));
            GraphRecallHash.Compute("1A").Should().NotBe(GraphRecallHash.Compute("A1"));
        }

        [Fact]
        public void TheBuilderHonoursTheNodeCapAndOnlyCarriesEdgesBetweenLoadedNodes()
        {
            var builder = new GraphRecallSnapshotBuilder(1, false, 2, 2, 4);
            var now = DateTime.UtcNow;

            builder.TryAddNode("AccountId", "A1", new GraphRecallNode { Id = 1 }).Should().BeTrue();
            builder.TryAddNode("AccountId", "A2", new GraphRecallNode { Id = 2 }).Should().BeTrue();
            builder.Full.Should().BeTrue();
            builder.TryAddNode("AccountId", "A3", new GraphRecallNode { Id = 3 }).Should().BeFalse();
            builder.Truncated.Should().BeTrue();
            builder.TryAddEdge(1, 2, registry, new GraphRecallEdge(1, 10, now, now, 2)).Should().BeTrue();
            builder.TryAddEdge(1, 3, registry, new GraphRecallEdge(1, 10, now, now, 2)).Should().BeFalse();
            builder.TryAddEdge(2, 1, otherRegistry, new GraphRecallEdge(1, 5, now, now, 1)).Should().BeTrue();
            builder.TryAddEdge(2, 1, registry, new GraphRecallEdge(1, 5, now, now, 1)).Should().BeFalse();
            builder.TryAddEdge(1, 2, registry, new GraphRecallEdge(4, 40, now.AddDays(-3), now.AddDays(1), 1)).Should().BeTrue();
            builder.EdgeCount.Should().Be(2);

            var snapshot = builder.Build(TimeSpan.FromMilliseconds(5), 1000);

            snapshot.NodeCount.Should().Be(2);
            snapshot.EdgeCount.Should().Be(2);
            snapshot.Truncated.Should().BeTrue();
            snapshot.EdgesTruncated.Should().BeTrue();
            snapshot.TryGetEdge(2, 1, registry, out var merged).Should().BeTrue();
            merged.Count.Should().Be(5);
            merged.AmountSum.Should().Be(50);
            merged.FirstSeen.Should().Be(now.AddDays(-3));
            merged.LastSeen.Should().Be(now.AddDays(1));
            merged.KindId.Should().Be(1);
            snapshot.TryGetEdge(1, 2, otherRegistry, out var other).Should().BeTrue();
            other.Count.Should().Be(1);
            snapshot.TryGetEdge(1, 2, Guid.NewGuid(), out _).Should().BeFalse();
            snapshot.TryGetEdge(1, 2, null, out var all).Should().BeTrue();
            all.Count.Should().Be(6);
            all.AmountSum.Should().Be(55);
            snapshot.TryGetEdge(1, 3, null, out _).Should().BeFalse();
            snapshot.TryGetNode("AccountId", "A3", out _).Should().BeFalse();
            snapshot.TryGetNode("Other", "A1", out _).Should().BeFalse();
            snapshot.NodeKeys.Should().BeEquivalentTo("AccountId");
            GraphRecallSnapshotLoader.BytesPerNode(snapshot).Should().Be(500);
        }

        [Fact]
        public void TheEdgeCapIsIndependentOfTheNodeCapAndIsReportedSeparately()
        {
            var builder = new GraphRecallSnapshotBuilder(1, false, 10, 1, 4);
            var now = DateTime.UtcNow;

            builder.TryAddNode("AccountId", "A1", new GraphRecallNode { Id = 1 });
            builder.TryAddNode("AccountId", "A2", new GraphRecallNode { Id = 2 });
            builder.TryAddEdge(1, 2, registry, new GraphRecallEdge(1, 10, now, now, 2)).Should().BeTrue();
            builder.EdgesTruncated.Should().BeFalse();
            builder.TryAddEdge(2, 1, registry, new GraphRecallEdge(1, 10, now, now, 2)).Should().BeFalse();

            var snapshot = builder.Build(TimeSpan.Zero, 0);

            snapshot.EdgeCount.Should().Be(1);
            snapshot.EdgesTruncated.Should().BeTrue();
            snapshot.Truncated.Should().BeFalse();
        }

        [Fact]
        public void TheSnapshotKnowsWhichNodeKeysAnActiveDefinitionConnects()
        {
            var builder = new GraphRecallSnapshotBuilder(1, false, 10, 10, 4);
            builder.AddDefinition(new GraphRecallDefinition(registry, "AccountId", "BeneficiaryId"));
            builder.AddDefinition(new GraphRecallDefinition(otherRegistry, "AccountId", "DeviceId"));

            var snapshot = builder.Build(TimeSpan.Zero, 0);

            snapshot.Definitions.Should().HaveCount(2);
            snapshot.Connects("AccountId", "BeneficiaryId", null).Should().BeTrue();
            snapshot.Connects("BeneficiaryId", "AccountId", registry).Should().BeTrue();
            snapshot.Connects("AccountId", "BeneficiaryId", otherRegistry).Should().BeFalse();
            snapshot.Connects("AccountId", "DeviceId", null).Should().BeTrue();
            snapshot.Connects("AccountId", "AccountId", null).Should().BeFalse();
            snapshot.Connects("AccountId", "Nothing", null).Should().BeFalse();
        }

        [Fact]
        public void TheBuilderCollectsTheDistinctTagsOfItsNodes()
        {
            var builder = new GraphRecallSnapshotBuilder(1, false, 10, 10, 4);
            builder.TryAddNode("AccountId", "A1", new GraphRecallNode { Id = 1, Tags = [new GraphRecallTag("Mule", 0, 1, 1, 1)] });
            builder.TryAddNode("AccountId", "A2", new GraphRecallNode { Id = 2, Tags = [new GraphRecallTag("Mule", 1, 0, 1, 1), new GraphRecallTag("Bust", 2, 0, 0, 1)] });

            builder.Build(TimeSpan.Zero, 0).Tags.Should().Equal("Bust", "Mule");
        }

        [Fact]
        public void ARowMapsToANodeWithUnknownDistancesAndParsedTags()
        {
            var node = GraphRecallSnapshotLoader.ToNode(new GetGraphRecallSnapshotQueryDto
            {
                Id = 9, Hub = 1, Degree = 3, PageRank = 0.125, PatternKinds = 3,
                Tags = "Mule\u001f2\u001f0\u001f1\u001f1\u001eBust\u001f0\u001f1\u001f1\u001f1"
            });

            node.Id.Should().Be(9);
            node.Hub.Should().BeTrue();
            node.Degree.Should().Be(3);
            node.PageRank.Should().BeApproximately(0.125f, 1e-6f);
            node.DistanceToConfirmed.Should().Be(GraphRecallNode.Unknown);
            node.DistanceToNotConfirmed.Should().Be(GraphRecallNode.Unknown);
            node.CommunityId.Should().Be(GraphRecallNode.Unknown);
            node.InPattern(0).Should().BeTrue();
            node.InPattern(1).Should().BeTrue();
            node.InPattern(2).Should().BeFalse();
            node.Tags.Should().HaveCount(2);
            node.TryGetTag("Bust", out var bust).Should().BeTrue();
            bust.Distance.Should().Be(0);
            bust.Within1.Should().Be(1);
            node.TryGetTag("Other", out _).Should().BeFalse();
            GraphRecallSnapshotLoader.ParseTags(null).Should().BeNull();
            node.Tags.Select(t => t.Tag).Should().Equal("Mule", "Bust");
        }
    }
}

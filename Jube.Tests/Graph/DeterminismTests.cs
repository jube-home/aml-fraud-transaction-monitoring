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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Graph.Algorithms;
using Jube.Graph.Model;
using Jube.Graph.Motifs;
using Jube.Graph.Relevance;
using Xunit;

namespace Jube.Test.Graph
{
    [Trait("Category", "Unit")]
    public sealed class DeterminismTests
    {
        [Fact]
        public void EveryAlgorithmRunTwiceGivesIdenticalArrays()
        {
            var graph = KarateClub.Graph();
            var again = KarateClub.Graph();

            again.Neighbours.Should().Equal(graph.Neighbours);
            again.NodeIds.Should().Equal(graph.NodeIds);
            UnionFind.Components(again).Component.Should().Equal(UnionFind.Components(graph).Component);
            var first = Louvain.Communities(graph);
            var second = Louvain.Communities(again);
            second.Community.Should().Equal(first.Community);
            second.Modularity.Should().Be(first.Modularity);
            KCore.CoreNumbers(again).Should().Equal(KCore.CoreNumbers(graph));
            PageRank.Compute(again).Should().Equal(PageRank.Compute(graph));
            PageRank.Personalised(again, Seeds.Bad(0, 33)).Should().Equal(PageRank.Personalised(graph, Seeds.Bad(0, 33)));
            var hits = Hits.Compute(graph);
            var hitsAgain = Hits.Compute(again);
            hitsAgain.Hub.Should().Equal(hits.Hub);
            hitsAgain.Authority.Should().Equal(hits.Authority);
            var bfs = BoundedBfs.DistancesFrom(graph, [0, 33], 3);
            var bfsAgain = BoundedBfs.DistancesFrom(again, [0, 33], 3);
            bfsAgain.Distance.Should().Equal(bfs.Distance);
            bfsAgain.Within3.Should().Equal(bfs.Within3);
            Triangles.Count(again).Triangles.Should().Equal(Triangles.Count(graph).Triangles);
            JohnsonCycles.Enumerate(again, 4).Select(c => string.Join(",", c)).Should()
                .Equal(JohnsonCycles.Enumerate(graph, 4).Select(c => string.Join(",", c)));
            GreedyPeeling.DensestBlock(again).Nodes.Should().Equal(GreedyPeeling.DensestBlock(graph).Nodes);
            InterestPropagation.Compute(Egonet.FromSeed(again, 0), 0, null).Should()
                .Equal(InterestPropagation.Compute(Egonet.FromSeed(graph, 0), 0, null));
        }

        [Fact]
        public void EveryDetectorRunTwiceGivesIdenticalInstances()
        {
            var ledger = new List<GraphEvent>();
            ledger.AddRange(MotifFixtures.FanOut());
            ledger.AddRange(MotifFixtures.GatherScatter());
            ledger.AddRange(MotifFixtures.ScatterGather());
            ledger.AddRange(MotifFixtures.TemporalCycle());
            ledger.AddRange(MotifFixtures.PassThrough());
            ledger.AddRange(MotifFixtures.SmurfTypeOne());
            ledger.AddRange(MotifFixtures.SmurfTypeTwo());
            ledger.AddRange(MotifFixtures.Structuring());
            ledger.AddRange(MotifFixtures.DenseBlock());

            Same(FanDetector.Detect(ledger), FanDetector.Detect(ledger));
            Same(GatherScatterDetector.Detect(ledger), GatherScatterDetector.Detect(ledger));
            Same(ScatterGatherDetector.Detect(ledger), ScatterGatherDetector.Detect(ledger));
            Same(TemporalCycleDetector.Detect(ledger), TemporalCycleDetector.Detect(ledger));
            Same(PassThroughDetector.Detect(ledger), PassThroughDetector.Detect(ledger));
            Same(SmurfDetector.Detect(ledger), SmurfDetector.Detect(ledger));
            Same(StructuringWindowDetector.Detect(ledger), StructuringWindowDetector.Detect(ledger));
            Same(DenseBlockDetector.Detect(ledger), DenseBlockDetector.Detect(ledger));
        }

        private static void Same(IReadOnlyList<PatternInstance> first, IReadOnlyList<PatternInstance> second)
        {
            first.Should().NotBeEmpty();
            second.Select(Key).Should().Equal(first.Select(Key));
        }

        private static string Key(PatternInstance instance)
        {
            return $"{instance.Kind}|{instance.AnchorNode}|{string.Join(",", instance.Members.Select(m => $"{m.NodeId}:{m.Role}"))}|{instance.Window.From:O}|{instance.Score}|{string.Join(",", instance.ArchiveIds)}";
        }
    }
}

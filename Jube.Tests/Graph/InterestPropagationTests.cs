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
using Jube.Graph.Model;
using Jube.Graph.Relevance;
using Xunit;

namespace Jube.Test.Graph
{
    [Trait("Category", "Unit")]
    public sealed class InterestPropagationTests
    {
        private static Egonet RingWithALongTail()
        {
            var edges = new List<(long, long, float, byte)>
            {
                (1L, 2L, 1f, 2), (2L, 3L, 1f, 2), (3L, 4L, 1f, 2), (4L, 1L, 1f, 2),
                (1L, 10L, 1f, 1), (2L, 10L, 1f, 1), (3L, 10L, 1f, 1), (4L, 10L, 1f, 1)
            };

            for (long i = 20; i < 27; i++)
            {
                edges.Add((i == 20 ? 1L : i - 1, i, 1f, 2));
            }

            return Egonet.FromSeed(CompressedGraphBuilder.FromEdges(edges, true), 0);
        }

        [Fact]
        public void SeedIsOneNeighboursOutrankFarNodesAndTheTailEndIsDropped()
        {
            var egonet = RingWithALongTail();

            var kept = InterestPropagation.Compute(egonet, 0, null);

            kept[0].Index.Should().Be(0);
            kept[0].Interest.Should().Be(1d);
            var byIndex = kept.ToDictionary(k => k.Index, k => k.Interest);
            byIndex[egonet.Graph.IndexOf(2L)].Should().BeGreaterThan(byIndex[egonet.Graph.IndexOf(3L)]);
            byIndex.Should().NotContainKey(egonet.Graph.IndexOf(26L));
            byIndex.Should().ContainKey(egonet.Graph.IndexOf(20L));
        }

        [Fact]
        public void BoostKeepsAConfirmedFarNode()
        {
            var egonet = RingWithALongTail();
            var far = egonet.Graph.IndexOf(26L);

            var kept = InterestPropagation.Compute(egonet, 0, new Dictionary<int, double> { [far] = 0.5 });

            kept.Select(k => k.Index).Should().Contain(far);
            kept.Single(k => k.Index == far).Interest.Should().BeGreaterThanOrEqualTo(0.5);
        }

        [Fact]
        public void ZeroHopsKeepsOnlyTheSeed()
        {
            var egonet = RingWithALongTail();

            var kept = InterestPropagation.Compute(egonet, 0, null, 0);

            kept.Should().ContainSingle().Which.Index.Should().Be(0);
        }
    }
}

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
using Jube.Graph.Motifs;
using Xunit;

namespace Jube.Test.Graph
{
    [Trait("Category", "Unit")]
    public sealed class MotifDetectorTests
    {
        [Fact]
        public void ADenseBurstOfEventsCannotMakeTheCycleSearchRunWithoutBound()
        {
            var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var events = new System.Collections.Generic.List<GraphEvent>();
            var archive = 0L;

            for (var round = 0; round < 6; round++)
            {
                for (var from = 1L; from <= 30; from++)
                {
                    for (var to = 1L; to <= 30; to++)
                    {
                        if (from != to)
                        {
                            events.Add(new GraphEvent(from, to, start.AddMinutes(round * 30 + (from + to) % 7), 10, ++archive));
                        }
                    }
                }
            }

            var found = TemporalCycleDetector.Detect(events);

            found.Should().NotBeNull();
            found.Count.Should().BeLessThanOrEqualTo(TemporalCycleDetector.MaximumSteps);
        }

        [Fact]
        public void FanOutIsFoundAtTheSource()
        {
            var found = FanDetector.Detect(MotifFixtures.FanOut());

            found.Should().ContainSingle();
            found[0].Kind.Should().Be(PatternKind.FanOut);
            found[0].AnchorNode.Should().Be(1L);
            found[0].Members.Should().HaveCount(6);
            found[0].Members.Count(m => m.Role == "Destination").Should().Be(5);
            found[0].Score.Should().Be(5d);
            found[0].ArchiveIds.Should().HaveCount(5);
            found[0].Window.Duration.Should().Be(TimeSpan.FromMinutes(40));
        }

        [Fact]
        public void FanInIsFoundAtTheDestination()
        {
            var found = FanDetector.Detect(MotifFixtures.FanIn());

            found.Should().ContainSingle();
            found[0].Kind.Should().Be(PatternKind.FanIn);
            found[0].AnchorNode.Should().Be(1L);
            found[0].Members.Count(m => m.Role == "Source").Should().Be(5);
        }

        [Fact]
        public void FanNeedsTheCounterpartsInsideTheWindow()
        {
            FanDetector.Detect(MotifFixtures.FanOut(), new FanParameters(5, TimeSpan.FromMinutes(5)))
                .Should().BeEmpty();
            FanDetector.Detect(MotifFixtures.FanOut(), new FanParameters(6, TimeSpan.FromDays(1)))
                .Should().BeEmpty();
        }

        [Fact]
        public void GatherScatterIsFoundAtTheHub()
        {
            var found = GatherScatterDetector.Detect(MotifFixtures.GatherScatter());

            found.Should().ContainSingle();
            found[0].Kind.Should().Be(PatternKind.GatherScatter);
            found[0].AnchorNode.Should().Be(50L);
            found[0].Members.Select(m => m.Role).Should().BeEquivalentTo("Hub", "Source", "Source", "Destination",
                "Destination");
            found[0].ArchiveIds.Should().HaveCount(4);
        }

        [Fact]
        public void ScatterGatherIsFoundAtTheSource()
        {
            var found = ScatterGatherDetector.Detect(MotifFixtures.ScatterGather());

            found.Should().ContainSingle();
            found[0].Kind.Should().Be(PatternKind.ScatterGather);
            found[0].AnchorNode.Should().Be(60L);
            found[0].Members.Where(m => m.Role == "Intermediary").Select(m => m.NodeId).Should().Equal(61L, 62L);
            found[0].Members.Single(m => m.Role == "Sink").NodeId.Should().Be(63L);
            found[0].ArchiveIds.Should().HaveCount(4);
        }

        [Fact]
        public void TemporalCycleIsFoundOnce()
        {
            var found = TemporalCycleDetector.Detect(MotifFixtures.TemporalCycle());

            found.Should().ContainSingle();
            found[0].Kind.Should().Be(PatternKind.TemporalCycle);
            found[0].AnchorNode.Should().Be(70L);
            found[0].Members.Select(m => m.NodeId).Should().Equal(70L, 71L, 72L);
            found[0].Parameters["Length"].Should().Be(3d);
            found[0].Parameters["ReturnRatio"].Should().BeApproximately(0.98, 1e-12);
        }

        [Fact]
        public void TemporalCycleRespectsTheWindow()
        {
            TemporalCycleDetector.Detect(MotifFixtures.TemporalCycle(),
                new TemporalCycleParameters(6, TimeSpan.FromMinutes(90))).Should().BeEmpty();
            TemporalCycleDetector.Detect(MotifFixtures.TemporalCycle(),
                new TemporalCycleParameters(2, TimeSpan.FromDays(1))).Should().BeEmpty();
        }

        [Fact]
        public void PassThroughIsFoundAtTheConduit()
        {
            var found = PassThroughDetector.Detect(MotifFixtures.PassThrough());

            found.Should().ContainSingle();
            found[0].Kind.Should().Be(PatternKind.PassThrough);
            found[0].AnchorNode.Should().Be(80L);
            found[0].Score.Should().BeApproximately(1430d / 1500d, 1e-12);
            found[0].Parameters["MedianHoldSeconds"].Should().Be(9000d);
            found[0].ArchiveIds.Should().HaveCount(4);
        }

        [Fact]
        public void SmurfTypeOneIsFoundAtTheMiddle()
        {
            var found = SmurfDetector.Detect(MotifFixtures.SmurfTypeOne());

            found.Should().ContainSingle();
            found[0].Kind.Should().Be(PatternKind.SmurfTypeOne);
            found[0].AnchorNode.Should().Be(90L);
            found[0].Score.Should().Be(6d);
            found[0].Members.Count(m => m.Role == "Source").Should().Be(3);
            found[0].Members.Count(m => m.Role == "Target").Should().Be(2);
        }

        [Fact]
        public void SmurfTypeTwoIsFoundAtTheSource()
        {
            var found = SmurfDetector.Detect(MotifFixtures.SmurfTypeTwo());

            found.Should().ContainSingle();
            found[0].Kind.Should().Be(PatternKind.SmurfTypeTwo);
            found[0].AnchorNode.Should().Be(100L);
            found[0].Score.Should().Be(5d);
            found[0].Members.Where(m => m.Role == "Middle").Select(m => m.NodeId).Should().Equal(101L, 102L, 103L);
            found[0].Members.Single(m => m.Role == "Target").NodeId.Should().Be(104L);
        }

        [Fact]
        public void SmurfPreFiltersByAmount()
        {
            SmurfDetector.Detect(MotifFixtures.SmurfTypeOne(),
                SmurfParameters.Default with { MinimumAmount = 4000d }).Should().BeEmpty();
        }

        [Fact]
        public void StructuringWindowCountsDepositsJustUnderTheThreshold()
        {
            var found = StructuringWindowDetector.Detect(MotifFixtures.Structuring());

            found.Should().ContainSingle();
            found[0].Kind.Should().Be(PatternKind.StructuringWindow);
            found[0].AnchorNode.Should().Be(110L);
            found[0].Score.Should().Be(3d);
            found[0].ArchiveIds.Should().HaveCount(3);
            found[0].Parameters["Total"].Should().Be(28500d);
        }

        [Fact]
        public void DenseBlockIsFound()
        {
            var found = DenseBlockDetector.Detect(MotifFixtures.DenseBlock());

            found.Should().ContainSingle();
            found[0].Kind.Should().Be(PatternKind.DenseBlock);
            found[0].Members.Select(m => m.NodeId).Should().Equal(120L, 121L, 122L, 123L, 124L, 125L);
            found[0].AnchorNode.Should().Be(120L);
            found[0].ArchiveIds.Should().HaveCount(30);
            found[0].Score.Should().BeGreaterThan(1.5);
        }

        [Fact]
        public void BenignLedgerTripsNoDetector()
        {
            var benign = MotifFixtures.Benign();

            FanDetector.Detect(benign).Should().BeEmpty();
            GatherScatterDetector.Detect(benign).Should().BeEmpty();
            ScatterGatherDetector.Detect(benign).Should().BeEmpty();
            TemporalCycleDetector.Detect(benign).Should().BeEmpty();
            PassThroughDetector.Detect(benign).Should().BeEmpty();
            SmurfDetector.Detect(benign).Should().BeEmpty();
            StructuringWindowDetector.Detect(benign).Should().BeEmpty();
            DenseBlockDetector.Detect(benign).Should().BeEmpty();
        }
    }
}

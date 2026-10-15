/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Linq;
using FluentAssertions;
using Jube.AskJooby.Guidance;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class AgentGuidanceExchangeTests
    {
        [Fact]
        public void EveryJoobrikStillWorkingHearsItAndEachOfThemOnlyOnce()
        {
            var exchange = new AgentGuidanceExchange();

            exchange.Send("1", "look at the other account instead");

            exchange.Drain("1", "run-a").Should().ContainSingle()
                .Which.Should().Be("look at the other account instead");

            exchange.Drain("1", "run-b").Should().ContainSingle(
                "a fan out is several Joobriks and the caller is addressing the room");
            exchange.Drain("1", "run-a").Should().BeEmpty(
                "delivering it twice would have an agent act on the same instruction again");
        }

        [Fact]
        public void AJoobrikSpawnedAfterTheCallerSpokeStillHearsIt()
        {
            var exchange = new AgentGuidanceExchange();

            exchange.Send("1", "ignore the dormant accounts");

            exchange.Drain("1", "spawned-later").Should().ContainSingle();
        }

        [Fact]
        public void OneTurnsGuidanceNeverReachesAnother()
        {
            var exchange = new AgentGuidanceExchange();

            exchange.Send("1", "for conversation one");

            exchange.Drain("2", "run-a").Should().BeEmpty();
        }

        [Fact]
        public void ReleasingTheTurnStopsStaleGuidanceReachingTheNextOne()
        {
            var exchange = new AgentGuidanceExchange();

            exchange.Send("1", "said about work that has now finished");
            exchange.Release("1");

            exchange.Drain("1", "next-turns-first-run").Should().BeEmpty(
                "guidance aimed at a finished turn would otherwise be delivered to the first agent "
                + "of the next one, which was never asked to do that thing");
        }

        [Fact]
        public void TheOldestIsDroppedFirstBecauseTheNewestIsWhatTheyMostWantHeard()
        {
            var exchange = new AgentGuidanceExchange(3);

            foreach (var entry in new[] { "first", "second", "third", "fourth" })
            {
                exchange.Send("1", entry);
            }

            var delivered = exchange.Drain("1", "run-a");

            delivered.Should().HaveCount(3);
            delivered.Should().NotContain("first");

            delivered.Last().Should().Be("fourth",
                "someone steering a run that is going wrong means the most recent thing they said");
        }

        [Fact]
        public void NothingPendingIsTheCommonCaseAndCostsNothing()
        {
            var exchange = new AgentGuidanceExchange();

            exchange.Drain("1", "run-a").Should().BeEmpty();
            exchange.Drain("never-seen", "run-a").Should().BeEmpty();
        }

        [Fact]
        public void BlankGuidanceIsNotDelivered()
        {
            var exchange = new AgentGuidanceExchange();

            exchange.Send("1", "   ");
            exchange.Send("1", string.Empty);

            exchange.Drain("1", "run-a").Should().BeEmpty();
        }
    }
}
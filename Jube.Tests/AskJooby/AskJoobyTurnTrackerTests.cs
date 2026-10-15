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

using FluentAssertions;
using Jube.AskJooby.Guidance;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class AskJoobyTurnTrackerTests
    {
        [Fact]
        public void ABegunConversationIsRunningUntilItIsDisposed()
        {
            var tracker = new AskJoobyTurnTracker();

            tracker.IsRunning("1").Should().BeFalse();

            var scope = tracker.Begin("1");
            tracker.IsRunning("1").Should().BeTrue();

            scope.Dispose();
            tracker.IsRunning("1").Should().BeFalse();
        }

        [Fact]
        public void TwoOverlappingTurnsOnTheSameConversationBothHaveToEndBeforeItIsIdleAgain()
        {
            var tracker = new AskJoobyTurnTracker();

            var first = tracker.Begin("1");
            var second = tracker.Begin("1");

            first.Dispose();

            tracker.IsRunning("1").Should().BeTrue(
                "two tabs could both be mid-turn on the same conversation; the first one finishing " +
                "must not make the other tab's still-running turn look idle");

            second.Dispose();
            tracker.IsRunning("1").Should().BeFalse();
        }

        [Fact]
        public void DisposingTwiceIsHarmless()
        {
            var tracker = new AskJoobyTurnTracker();

            var scope = tracker.Begin("1");
            scope.Dispose();
            scope.Dispose();

            tracker.IsRunning("1").Should().BeFalse();
        }

        [Fact]
        public void OneConversationRunningNeverReportsAnotherAsBusy()
        {
            var tracker = new AskJoobyTurnTracker();

            using var scope = tracker.Begin("1");

            tracker.IsRunning("2").Should().BeFalse();
        }

        [Fact]
        public void AConversationNeverBegunIsNotRunning()
        {
            var tracker = new AskJoobyTurnTracker();

            tracker.IsRunning("never-begun").Should().BeFalse();
        }
    }
}
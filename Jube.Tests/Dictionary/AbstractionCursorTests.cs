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

using FluentAssertions;
using Jube.Dictionary;
using Xunit;

namespace Jube.Test.Dictionary
{
    [Trait("Category", "Unit")]
    public sealed class AbstractionCursorTests
    {
        [Fact]
        public void PositionIsOneAheadOfIndexAndFirstAndLastFollowTheSize()
        {
            var cursor = new AbstractionCursor();
            cursor.Begin(3);

            cursor.MoveTo(0, false);
            (cursor.Index, cursor.Position, cursor.IsFirst, cursor.IsLast).Should().Be((0, 1, true, false));

            cursor.MoveTo(2, true);
            (cursor.Index, cursor.Position, cursor.IsFirst, cursor.IsLast).Should().Be((2, 3, false, true));
            cursor.IsCurrent.Should().BeTrue();
            cursor.Size.Should().Be(3);
        }

        [Fact]
        public void TheStateSlotHoldsWhatTheCompiledRuleKeepsForTheScan()
        {
            var cursor = new AbstractionCursor();
            var state = new object();

            cursor.State.Should().BeNull();
            cursor.State = state;
            cursor.MoveTo(1, false);

            cursor.State.Should().BeSameAs(state);
        }

        [Fact]
        public void BeginClearsEverythingFromThePreviousScan()
        {
            var cursor = new AbstractionCursor();
            cursor.Begin(2);
            cursor.MoveTo(1, true);
            cursor.Accept();
            cursor.State = new object();
            cursor.Result = 9;
            cursor.Stop();

            cursor.Begin(4);

            (cursor.Index, cursor.Size, cursor.Matches).Should().Be((0, 4, 0));
            cursor.IsCurrent.Should().BeFalse();
            cursor.Stopped.Should().BeFalse();
            cursor.State.Should().BeNull();
            cursor.Result.Should().Be(0);
        }
    }
}

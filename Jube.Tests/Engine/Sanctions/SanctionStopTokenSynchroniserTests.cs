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

using System.Collections.Concurrent;
using System.Collections.Generic;
using FluentAssertions;
using Jube.Engine.Sanctions;
using Xunit;

namespace Jube.Test.Engine.Sanctions
{
    [Trait("Category", "Unit")]
    public sealed class SanctionStopTokenSynchroniserTests
    {
        [Fact]
        public void SynchroniseAddsNewTokensWithTheirCategory()
        {
            var target = new ConcurrentDictionary<string, byte>();

            SanctionStopTokenSynchroniser.Synchronise(target, [("ltd", 1), ("bank", 2)]);

            target.Should().BeEquivalentTo(new Dictionary<string, byte> { ["ltd"] = 1, ["bank"] = 2 });
        }

        [Fact]
        public void SynchroniseRemovesTokensThatAreNoLongerLive()
        {
            var target = new ConcurrentDictionary<string, byte>();
            target.TryAdd("ltd", 1);
            target.TryAdd("deleted", 1);

            SanctionStopTokenSynchroniser.Synchronise(target, [("ltd", 1)]);

            target.Keys.Should().Equal("ltd");
        }

        [Fact]
        public void SynchroniseUpdatesTheCategoryOfAnExistingToken()
        {
            var target = new ConcurrentDictionary<string, byte>();
            target.TryAdd("ltd", 1);

            SanctionStopTokenSynchroniser.Synchronise(target, [("ltd", 3)]);

            target["ltd"].Should().Be(3);
        }

        [Fact]
        public void SynchroniseWithNoLiveTokensEmptiesTheSet()
        {
            var target = new ConcurrentDictionary<string, byte>();
            target.TryAdd("ltd", 1);

            SanctionStopTokenSynchroniser.Synchronise(target, []);

            target.Should().BeEmpty();
        }

        [Fact]
        public void SynchroniseIgnoresBlankTokens()
        {
            var target = new ConcurrentDictionary<string, byte>();

            SanctionStopTokenSynchroniser.Synchronise(target, [(" ", 1), ("", 1), ("ltd", 1)]);

            target.Keys.Should().Equal("ltd");
        }
    }
}

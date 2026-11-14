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
using Jube.App.Code.Watcher;
using Xunit;

namespace Jube.Test.App.Code.Watcher
{
    [Trait("Category", "Unit")]
    public sealed class WatcherStreamFrameTests
    {
        [Fact]
        public void AJsonPayloadBecomesOneNamedEventFrame()
        {
            WatcherStreamFrame.Build("{\"Key\":\"AccountId\"}").Should()
                .Be("event: activation\ndata: {\"Key\":\"AccountId\"}\n\n");
        }

        [Fact]
        public void LineBreaksInThePayloadCannotInjectAnotherEventOrField()
        {
            var frame = WatcherStreamFrame.Build("{}\r\nevent: revoked\rdata: {}\nid: 1");

            frame.Should().Be("event: activation\ndata: {}\ndata: event: revoked\ndata: data: {}\ndata: id: 1\n\n");
            frame.Split("\n\n").Should().HaveCount(2);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AnEmptyPayloadIsDropped(string payload)
        {
            WatcherStreamFrame.Build(payload).Should().BeNull();
        }

        [Fact]
        public void AnOversizePayloadIsDroppedAndOneAtTheLimitIsKept()
        {
            WatcherStreamFrame.Build(new string('a', WatcherStreamFrame.MaximumPayloadCharacters + 1)).Should()
                .BeNull();

            WatcherStreamFrame.Build(new string('a', WatcherStreamFrame.MaximumPayloadCharacters)).Should()
                .NotBeNull();
        }
    }
}

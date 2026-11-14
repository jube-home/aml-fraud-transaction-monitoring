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

using System.Text;
using FluentAssertions;
using Jube.Engine.InboundQueue;
using Jube.Service.Invoke;
using Xunit;

namespace Jube.Test.InboundQueue
{
    [Trait("Category", "Unit")]
    public sealed class InboundQueueUnitTests
    {
        [Theory]
        [InlineData(1, 5)]
        [InlineData(2, 10)]
        [InlineData(3, 20)]
        [InlineData(7, 300)]
        [InlineData(40, 300)]
        [InlineData(0, 5)]
        public void TheBackoffDoublesFromFiveSecondsAndStopsAtFiveMinutes(int attempts, int expected)
        {
            InboundQueueConsumer.BackoffSeconds(attempts).Should().Be(expected);
        }

        [Theory]
        [InlineData("{\"a\":1}", true)]
        [InlineData("[1,2,3]", true)]
        [InlineData("\"text\"", true)]
        [InlineData("{\"a\":", false)]
        [InlineData("not json", false)]
        [InlineData("{\"a\":1}{\"b\":2}", false)]
        public void OnlyWellFormedJsonIsAccepted(string body, bool expected)
        {
            InboundQueueService.IsJson(Encoding.UTF8.GetBytes(body)).Should().Be(expected);
        }

        [Fact]
        public void DeeplyNestedJsonIsRefused()
        {
            var body = new string('[', 100) + new string(']', 100);

            InboundQueueService.IsJson(Encoding.UTF8.GetBytes(body)).Should().BeFalse();
        }
    }
}

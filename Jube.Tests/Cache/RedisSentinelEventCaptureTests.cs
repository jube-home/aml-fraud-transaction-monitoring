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
using FluentAssertions;
using Jube.Cache;
using Xunit;

namespace Jube.Test.Cache
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class RedisSentinelEventCaptureTests
    {
        private static RedisSentinelEventCaptureRecord NewRecord()
        {
            return new RedisSentinelEventCaptureRecord(DateTime.UtcNow, "+odown", "master mymaster 127.0.0.1 6379");
        }

        [Fact]
        public void EnqueueThenDrainAllReturnsRecordsInOrderAndEmptiesTheQueue()
        {
            RedisSentinelEventCapture.DrainAll();
            var first = NewRecord();
            var second = NewRecord();

            RedisSentinelEventCapture.Enqueue(first);
            RedisSentinelEventCapture.Enqueue(second);

            var drained = RedisSentinelEventCapture.DrainAll();

            drained.Should().Equal(first, second);
            RedisSentinelEventCapture.QueueDepth.Should().Be(0);
        }

        [Fact]
        public void TakeDroppedCountResetsToZeroAfterReading()
        {
            RedisSentinelEventCapture.DrainAll();
            RedisSentinelEventCapture.TakeDroppedCount();

            RedisSentinelEventCapture.AddDroppedCount(4);

            RedisSentinelEventCapture.TakeDroppedCount().Should().Be(4);
            RedisSentinelEventCapture.TakeDroppedCount().Should().Be(0);
        }

        [Fact]
        public void EnqueueBeyondTheCapDropsInsteadOfGrowingTheQueueUnbounded()
        {
            RedisSentinelEventCapture.DrainAll();
            RedisSentinelEventCapture.TakeDroppedCount();

            const int maxQueueLength = 20000;
            for (var i = 0; i < maxQueueLength; i++)
            {
                RedisSentinelEventCapture.Enqueue(NewRecord());
            }

            RedisSentinelEventCapture.QueueDepth.Should().Be(maxQueueLength);

            RedisSentinelEventCapture.Enqueue(NewRecord());

            RedisSentinelEventCapture.QueueDepth.Should().Be(maxQueueLength, "the queue is capped, not unbounded");
            RedisSentinelEventCapture.TakeDroppedCount().Should().Be(1);

            RedisSentinelEventCapture.DrainAll();
        }
    }
}
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
using Jube.Data.Repository;
using StackExchange.Redis;
using Xunit;

namespace Jube.Test.Cache
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class RedisConnectionEventCaptureTests
    {
        private static RedisConnectionEventCaptureRecord NewRecord()
        {
            return new RedisConnectionEventCaptureRecord(
                DateTime.UtcNow, RedisConnectionEventType.ConnectionFailed, "localhost:6379",
                ConnectionType.Interactive, ConnectionFailureType.SocketClosed, null, null, null);
        }

        [Fact]
        public void EnqueueThenDrainAllReturnsRecordsInOrderAndEmptiesTheQueue()
        {
            RedisConnectionEventCapture.DrainAll();
            var first = NewRecord();
            var second = NewRecord();

            RedisConnectionEventCapture.Enqueue(first);
            RedisConnectionEventCapture.Enqueue(second);

            var drained = RedisConnectionEventCapture.DrainAll();

            drained.Should().Equal(first, second);
            RedisConnectionEventCapture.QueueDepth.Should().Be(0);
        }

        [Fact]
        public void TakeDroppedCountResetsToZeroAfterReading()
        {
            RedisConnectionEventCapture.DrainAll();
            RedisConnectionEventCapture.TakeDroppedCount();

            RedisConnectionEventCapture.AddDroppedCount(3);

            RedisConnectionEventCapture.TakeDroppedCount().Should().Be(3);
            RedisConnectionEventCapture.TakeDroppedCount().Should().Be(0);
        }

        [Fact]
        public void EnqueueBeyondTheCapDropsInsteadOfGrowingTheQueueUnbounded()
        {
            RedisConnectionEventCapture.DrainAll();
            RedisConnectionEventCapture.TakeDroppedCount();

            const int maxQueueLength = 20000;
            for (var i = 0; i < maxQueueLength; i++)
            {
                RedisConnectionEventCapture.Enqueue(NewRecord());
            }

            RedisConnectionEventCapture.QueueDepth.Should().Be(maxQueueLength);

            RedisConnectionEventCapture.Enqueue(NewRecord());
            RedisConnectionEventCapture.Enqueue(NewRecord());

            RedisConnectionEventCapture.QueueDepth.Should().Be(maxQueueLength, "the queue is capped, not unbounded");
            RedisConnectionEventCapture.TakeDroppedCount().Should().Be(2);

            RedisConnectionEventCapture.DrainAll();
        }
    }
}
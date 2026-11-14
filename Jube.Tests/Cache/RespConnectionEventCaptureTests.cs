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

using Jube.Data.Repository.Models;
using System;
using FluentAssertions;
using Jube.Cache;
using Jube.Cache.Models;
using StackExchange.Redis;
using Xunit;

namespace Jube.Test.Cache
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class RespConnectionEventCaptureTests
    {
        private static RespConnectionEventCaptureRecord NewRecord()
        {
            return new RespConnectionEventCaptureRecord(
                DateTime.UtcNow, RespConnectionEventType.ConnectionFailed, "localhost:6379",
                ConnectionType.Interactive, ConnectionFailureType.SocketClosed, null, null, null);
        }

        [Fact]
        public void EnqueueThenDrainAllReturnsRecordsInOrderAndEmptiesTheQueue()
        {
            RespConnectionEventCapture.DrainAll();
            var first = NewRecord();
            var second = NewRecord();

            RespConnectionEventCapture.Enqueue(first);
            RespConnectionEventCapture.Enqueue(second);

            var drained = RespConnectionEventCapture.DrainAll();

            drained.Should().Equal(first, second);
            RespConnectionEventCapture.QueueDepth.Should().Be(0);
        }

        [Fact]
        public void TakeDroppedCountResetsToZeroAfterReading()
        {
            RespConnectionEventCapture.DrainAll();
            RespConnectionEventCapture.TakeDroppedCount();

            RespConnectionEventCapture.AddDroppedCount(3);

            RespConnectionEventCapture.TakeDroppedCount().Should().Be(3);
            RespConnectionEventCapture.TakeDroppedCount().Should().Be(0);
        }

        [Fact]
        public void EnqueueBeyondTheCapDropsInsteadOfGrowingTheQueueUnbounded()
        {
            RespConnectionEventCapture.DrainAll();
            RespConnectionEventCapture.TakeDroppedCount();

            const int maxQueueLength = 20000;

            for (var i = 0; i < maxQueueLength; i++)
            {
                RespConnectionEventCapture.Enqueue(NewRecord());
            }

            RespConnectionEventCapture.QueueDepth.Should().Be(maxQueueLength);

            RespConnectionEventCapture.Enqueue(NewRecord());
            RespConnectionEventCapture.Enqueue(NewRecord());

            RespConnectionEventCapture.QueueDepth.Should().Be(maxQueueLength, "the queue is capped, not unbounded");
            RespConnectionEventCapture.TakeDroppedCount().Should().Be(2);

            RespConnectionEventCapture.DrainAll();
        }
    }
}
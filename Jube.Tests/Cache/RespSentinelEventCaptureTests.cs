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
using Jube.Cache.Models;
using Xunit;

namespace Jube.Test.Cache
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class RespSentinelEventCaptureTests
    {
        private static RespSentinelEventCaptureRecord NewRecord()
        {
            return new RespSentinelEventCaptureRecord(DateTime.UtcNow, "+odown", "master mymaster 127.0.0.1 6379");
        }

        [Fact]
        public void EnqueueThenDrainAllReturnsRecordsInOrderAndEmptiesTheQueue()
        {
            RespSentinelEventCapture.DrainAll();
            var first = NewRecord();
            var second = NewRecord();

            RespSentinelEventCapture.Enqueue(first);
            RespSentinelEventCapture.Enqueue(second);

            var drained = RespSentinelEventCapture.DrainAll();

            drained.Should().Equal(first, second);
            RespSentinelEventCapture.QueueDepth.Should().Be(0);
        }

        [Fact]
        public void TakeDroppedCountResetsToZeroAfterReading()
        {
            RespSentinelEventCapture.DrainAll();
            RespSentinelEventCapture.TakeDroppedCount();

            RespSentinelEventCapture.AddDroppedCount(4);

            RespSentinelEventCapture.TakeDroppedCount().Should().Be(4);
            RespSentinelEventCapture.TakeDroppedCount().Should().Be(0);
        }

        [Fact]
        public void EnqueueBeyondTheCapDropsInsteadOfGrowingTheQueueUnbounded()
        {
            RespSentinelEventCapture.DrainAll();
            RespSentinelEventCapture.TakeDroppedCount();

            const int maxQueueLength = 20000;

            for (var i = 0; i < maxQueueLength; i++)
            {
                RespSentinelEventCapture.Enqueue(NewRecord());
            }

            RespSentinelEventCapture.QueueDepth.Should().Be(maxQueueLength);

            RespSentinelEventCapture.Enqueue(NewRecord());

            RespSentinelEventCapture.QueueDepth.Should().Be(maxQueueLength, "the queue is capped, not unbounded");
            RespSentinelEventCapture.TakeDroppedCount().Should().Be(1);

            RespSentinelEventCapture.DrainAll();
        }
    }
}
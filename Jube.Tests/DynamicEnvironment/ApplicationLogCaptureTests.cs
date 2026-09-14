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
using Jube.DynamicEnvironment.Logging;
using Xunit;

namespace Jube.Test.DynamicEnvironment
{
    [Trait("Category", "Unit")]
    [Collection("ApplicationLogCapture")]
    public sealed class ApplicationLogCaptureTests
    {
        public ApplicationLogCaptureTests()
        {
            ApplicationLogCapture.DrainAll();
        }

        private static ApplicationLogCaptureRecord MakeRecord(string message)
        {
            return new ApplicationLogCaptureRecord(DateTime.UtcNow, "WARN", "Some.Class", "1", message, null);
        }

        [Fact]
        public void DrainAllReturnsEveryEnqueuedRecordInOrderAndEmptiesTheQueue()
        {
            ApplicationLogCapture.Enqueue(MakeRecord("first"));
            ApplicationLogCapture.Enqueue(MakeRecord("second"));

            var drained = ApplicationLogCapture.DrainAll();

            drained.Should().HaveCount(2);
            drained[0].Message.Should().Be("first");
            drained[1].Message.Should().Be("second");

            ApplicationLogCapture.DrainAll().Should().BeEmpty();
        }

        [Fact]
        public void EnqueueBeyondMaxQueueLengthIncrementsDroppedCountInsteadOfGrowingUnbounded()
        {
            const int maxQueueLength = 20000;
            var droppedBefore = ApplicationLogCapture.DroppedCount;

            for (var i = 0; i < maxQueueLength + 5; i++)
            {
                ApplicationLogCapture.Enqueue(MakeRecord($"message-{i}"));
            }

            var drained = ApplicationLogCapture.DrainAll();

            drained.Should().HaveCount(maxQueueLength);
            (ApplicationLogCapture.DroppedCount - droppedBefore).Should().Be(5);
        }
    }
}
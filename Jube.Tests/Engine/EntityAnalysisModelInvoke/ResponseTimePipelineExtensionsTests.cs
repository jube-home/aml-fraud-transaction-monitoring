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
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using Jube.Engine.EntityAnalysisModelInvoke.Context;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class ResponseTimePipelineExtensionsTests
    {
        private static Context NewContext(bool logSampled = true)
        {
            return new Context
            {
                EntityAnalysisModel = new EntityAnalysisModel(),
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload(),
                Stopwatch = Stopwatch.StartNew(),
                LogSampled = logSampled
            };
        }

        [Fact]
        public void RecordResponseTimeWhenNotSampledDoesNotCreatePipelineOrEntries()
        {
            var context = NewContext(false);

            context.RecordResponseTime("Parse");

            context.EntityAnalysisModelInstanceEntryPayload.ResponseTimePipeline.Should().BeNull();
        }

        [Fact]
        public void RecordResponseTimeWhenSampledAddsOneEntryWithTheGivenStageName()
        {
            var context = NewContext();

            context.RecordResponseTime("Parse");

            var pipeline = context.EntityAnalysisModelInstanceEntryPayload.ResponseTimePipeline;
            pipeline.Should().NotBeNull();
            pipeline!.Entries.Should().ContainSingle();
            pipeline.Entries[0].Stage.Should().Be("Parse");
            pipeline.Entries[0].ThreadId.Should().Be(Environment.CurrentManagedThreadId);
        }

        [Fact]
        public void RecordResponseTimeAccumulatesOneEntryPerCallAcrossStages()
        {
            var context = NewContext();

            context.RecordResponseTime("Parse");
            context.RecordResponseTime("Gateway");
            context.RecordResponseTime("Activation");

            var entries = context.EntityAnalysisModelInstanceEntryPayload.ResponseTimePipeline!.Entries;
            entries.Should().HaveCount(3);
            entries.Select(e => e.Stage).Should().Equal("Parse", "Gateway", "Activation");
        }

        [Fact]
        public void RecordResponseTimeUpdatesLastResponseTimeElapsedMicrosecondsToTheNewEntry()
        {
            var context = NewContext();

            context.RecordResponseTime("Parse");
            var firstElapsed = context.EntityAnalysisModelInstanceEntryPayload.ResponseTimePipeline!.Entries[0]
                .ElapsedMicroseconds;

            context.LastResponseTimeElapsedMicroseconds.Should().Be(firstElapsed);
        }

        [Fact]
        public void RecordResponseTimeClampsAllocatedBytesToZeroWhenTheDeltaWouldBeNegative()
        {
            var context = NewContext();
            context.LastResponseTimeAllocatedBytes = long.MaxValue / 2;
            context.RecordResponseTime("Parse");

            var entry = context.EntityAnalysisModelInstanceEntryPayload.ResponseTimePipeline!.Entries[0];
            entry.AllocatedBytes.Should().Be(0);
        }

        [Fact]
        public void RecordResponseTimeUsesStartBytesUsedWhenNoPriorCheckpointExists()
        {
            var context = NewContext();
            context.StartBytesUsed.Should().BeNull();

            var act = () => context.RecordResponseTime("Parse");

            act.Should().NotThrow();
            context.EntityAnalysisModelInstanceEntryPayload.ResponseTimePipeline!.Entries.Should().ContainSingle();
        }

        [Fact]
        public void RecordResponseTimeNeverProducesANegativeDurationOnTheVeryFirstCall()
        {
            var context = NewContext();
            context.LastResponseTimeElapsedMicroseconds.Should().Be(0);

            context.RecordResponseTime("Parse");

            var entry = context.EntityAnalysisModelInstanceEntryPayload.ResponseTimePipeline!.Entries[0];
            entry.DurationMicroseconds.Should().BeGreaterOrEqualTo(0);
            entry.ElapsedMicroseconds.Should().BeGreaterOrEqualTo(0);
        }
    }
}
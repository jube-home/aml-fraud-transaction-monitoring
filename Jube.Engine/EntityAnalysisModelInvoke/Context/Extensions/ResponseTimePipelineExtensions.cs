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
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.ResponseTime;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    public static class ResponseTimePipelineExtensions
    {
        public static readonly string[] StageSequence =
        [
            "Parse", "CheckIntegrityAndUpsert", "InlineFunctions", "InlineScripts", "Gateway", "CacheDbStorage",
            "JoinReadTasks", "AbstractionRulesWithoutSearchKeys", "AbstractionCalculations", "ExhaustiveAdaptation",
            "HttpAdaptation", "Activation", "JoinWriteTasks", "WriteResponse", "BuildArchivePayload"
        ];

        public static void RecordResponseTime(this Context context, string stage)
        {
            if (!context.LogSampled)
            {
                return;
            }

            var elapsedMicroseconds = (long)(context.Stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));
            var durationMicroseconds = elapsedMicroseconds - context.LastResponseTimeElapsedMicroseconds;
            var currentAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();

            var previousAllocatedBytes =
                context.LastResponseTimeAllocatedBytes ?? context.StartBytesUsed ?? currentAllocatedBytes;

            var allocatedBytes = Math.Max(currentAllocatedBytes - previousAllocatedBytes, 0);

            var pipeline = context.EntityAnalysisModelInstanceEntryPayload.ResponseTimePipeline ??=
                new ResponseTimePipeline();

            pipeline.Entries.Add(new ResponseTimePipelineEntry
            {
                Stage = stage,
                ElapsedMicroseconds = elapsedMicroseconds,
                DurationMicroseconds = durationMicroseconds,
                AllocatedBytes = allocatedBytes,
                ThreadId = Environment.CurrentManagedThreadId
            });

            context.LastResponseTimeElapsedMicroseconds = elapsedMicroseconds;
            context.LastResponseTimeAllocatedBytes = currentAllocatedBytes;
        }
    }
}
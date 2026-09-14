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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Engine.EntityAnalysisModelInvoke.ImplicitAsync.Interfaces;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel;
using log4net;

namespace Jube.Engine.EntityAnalysisModelInvoke.ImplicitAsync
{
    using EntityAnalysisModel = EntityAnalysisModel;

    public class ImplicitAsyncInvocationTracker(ILog log) : IImplicitAsyncInvocationTracker
    {
        private readonly ConcurrentDictionary<Guid, (int EntityAnalysisModelId, DateTime StartedAt)> pending = new();

        public int PendingCount => pending.Count;

        public int PendingCountForModel(int entityAnalysisModelId)
        {
            return pending.Count(entry => entry.Value.EntityAnalysisModelId == entityAnalysisModelId);
        }

        public void TrackOverdueInvocation(Guid entityAnalysisModelInstanceEntryGuid,
            EntityAnalysisModel entityAnalysisModel, Task invocation)
        {
            pending[entityAnalysisModelInstanceEntryGuid] = (entityAnalysisModel.Instance.Id, DateTime.UtcNow);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Implicit Async: GUID {entityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Instance.Id} " +
                    "has exceeded its implicit async timeout and will continue running in the background. There are now " +
                    $"{PendingCount} implicit async invocations overdue.");
            }

            _ = invocation.ContinueWith(task =>
            {
                pending.TryRemove(entityAnalysisModelInstanceEntryGuid, out _);

                if (task.IsFaulted)
                {
                    Interlocked.Increment(ref entityAnalysisModel.Counters
                        .ModelImplicitAsyncFaultedAfterTimeoutCounter);

                    log.Error(
                        $"Implicit Async: GUID {entityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Instance.Id} " +
                        $"faulted after the caller had already timed out and moved on, as {task.Exception}.");
                }
                else
                {
                    Interlocked.Increment(ref entityAnalysisModel.Counters
                        .ModelImplicitAsyncCompletedAfterTimeoutCounter);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Implicit Async: GUID {entityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Instance.Id} " +
                            "completed successfully after the caller had already timed out and moved on.");
                    }
                }
            }, TaskScheduler.Default);
        }
    }
}
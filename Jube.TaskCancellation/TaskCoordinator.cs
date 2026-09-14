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

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using Jube.TaskCancellation.Interfaces;
using Jube.TaskCancellation.Models;
using log4net;

namespace Jube.TaskCancellation
{
    public class TaskCoordinator(ICancellationTokenProvider cancellationProvider, ILog? log = null) : ITaskCoordinator
    {
        private readonly ConcurrentBag<NamedTask> tasks =
        [
        ];

        public CancellationToken CancellationToken => cancellationProvider.Token;

        public Task RunAsync(string name, Func<CancellationToken, Task> work, CancellationToken? token = null)
        {
            var effectiveToken = token ?? cancellationProvider.Token;

            var task = Task.Run(async () =>
            {
                try
                {
                    await work(effectiveToken);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    var message = $"Task '{name}' threw an exception and will not be restarted: {ex}";
                    if (log != null)
                    {
                        log.Error(message);
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }
                }
            }, effectiveToken);

            tasks.Add(new NamedTask(name, task));

            return task;
        }

        public IEnumerator<NamedTask> GetEnumerator()
        {
            return tasks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Task WaitForAllAsync()
        {
            return Task.WhenAll(tasks.Select(t => t.Task));
        }

        public async Task StillRunningAsync()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (tasks.Any(t => t.Task is { IsCompleted: false, IsCanceled: false, IsFaulted: false }))
            {
                // ReSharper disable once MethodSupportsCancellation
                await Task.Delay(200);

                if (stopwatch.ElapsedMilliseconds < 1000)
                {
                    continue;
                }

                var blockingTasks = tasks
                    .Where(w => w.Task is { IsCompleted: false, IsCanceled: false, IsFaulted: false })
                    .ToList();

                if (blockingTasks.Any())
                {
                    foreach (var blockingTask in blockingTasks)
                    {
                        Console.WriteLine($"Blocking task: {blockingTask.Name}");
                    }
                }

                stopwatch.Restart();
            }
        }
    }
}
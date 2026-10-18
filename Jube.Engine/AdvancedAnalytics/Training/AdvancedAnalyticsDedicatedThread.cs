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
using System.Threading;
using System.Threading.Tasks;

namespace Jube.Engine.AdvancedAnalytics.Training
{
    internal static class AdvancedAnalyticsDedicatedThread
    {
        internal static Task<T> RunAsync<T>(Func<T> work, CancellationToken token)
        {
            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (token.IsCancellationRequested)
            {
                completion.SetCanceled(token);

                return completion.Task;
            }

            var thread = new Thread(() =>
            {
                try
                {
                    completion.SetResult(work());
                }
                catch (OperationCanceledException ex)
                {
                    completion.SetCanceled(ex.CancellationToken.IsCancellationRequested
                        ? ex.CancellationToken
                        : token);
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
            })
            {
                IsBackground = true,
                Name = "AdvancedAnalyticsTraining",
                Priority = ThreadPriority.BelowNormal
            };

            thread.Start();

            return completion.Task;
        }
    }
}
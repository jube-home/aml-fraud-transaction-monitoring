/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Runtime.CompilerServices;
using Jube.AskJooby.Agents.Models;

namespace Jube.AskJooby.Agents
{
    public static class AgentIdleWatchdog
    {
        public static async IAsyncEnumerable<AgentStreamItem> WithIdleTimeoutAsync(
            Func<CancellationToken, IAsyncEnumerable<AgentStreamItem>> source,
            TimeSpan idleTimeout,
            Action? onIdle = null,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (idleTimeout <= TimeSpan.Zero)
            {
                await foreach (var item in source(token).WithCancellation(token).ConfigureAwait(false))
                {
                    yield return item;
                }

                yield break;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var enumerator = source(cts.Token).GetAsyncEnumerator(cts.Token);
            Task<bool>? pending = null;

            try
            {
                while (true)
                {
                    pending = enumerator.MoveNextAsync().AsTask();

                    using var delayCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                    var delay = Task.Delay(idleTimeout, delayCts.Token);
                    var first = await Task.WhenAny(pending, delay).ConfigureAwait(false);

                    if (!ReferenceEquals(first, pending))
                    {
                        token.ThrowIfCancellationRequested();
                        await cts.CancelAsync().ConfigureAwait(false);
                        onIdle?.Invoke();
                        yield break;
                    }

                    await delayCts.CancelAsync().ConfigureAwait(false);

                    var moved = await pending.ConfigureAwait(false);
                    pending = null;

                    if (!moved)
                    {
                        yield break;
                    }

                    yield return enumerator.Current;
                }
            }
            finally
            {
                if (pending is not null)
                {
                    try
                    {
                        await pending.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception)
                    {
                        _ = pending.Exception;
                    }
                }

                try
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }
}
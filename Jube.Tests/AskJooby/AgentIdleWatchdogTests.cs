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

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Agents;
using Jube.AskJooby.Agents.Models;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AgentIdleWatchdogTests
    {
        private static AgentStreamItem Step(string text)
        {
            return new AgentStreamItem(AgentStreamItemKind.Step, text);
        }

        [Fact]
        public async Task AnAgentThatKeepsTalkingIsNeverCutOffHoweverLongItRunsAsync()
        {
            async IAsyncEnumerable<AgentStreamItem> ChattyAsync([EnumeratorCancellation] CancellationToken token)
            {
                for (var i = 0; i < 5; i++)
                {
                    await Task.Delay(60, token);
                    yield return Step("beat " + i);
                }
            }

            var idle = false;
            var seen = new List<AgentStreamItem>();

            await foreach (var item in AgentIdleWatchdog.WithIdleTimeoutAsync(
                               ChattyAsync, TimeSpan.FromMilliseconds(200), () => idle = true))
            {
                seen.Add(item);
            }

            seen.Should().HaveCount(5);
            idle.Should().BeFalse("nothing went quiet, so nothing should have been retired");
        }

        [Fact]
        public async Task AnAgentThatGoesSilentIsRetiredRatherThanWaitedOnForeverAsync()
        {
            async IAsyncEnumerable<AgentStreamItem> StallsAfterOneAsync(
                [EnumeratorCancellation] CancellationToken token)
            {
                yield return Step("got started");
                await Task.Delay(Timeout.Infinite, token);
                yield return Step("never reached");
            }

            var idle = false;
            var seen = new List<AgentStreamItem>();

            var enumeration = Task.Run(async () =>
            {
                await foreach (var item in AgentIdleWatchdog.WithIdleTimeoutAsync(
                                   StallsAfterOneAsync, TimeSpan.FromMilliseconds(150), () => idle = true))
                {
                    seen.Add(item);
                }
            });

            await enumeration.WaitAsync(TimeSpan.FromSeconds(5));

            idle.Should().BeTrue("an agent producing nothing at all is the failure this exists for");
            seen.Should().ContainSingle("what it managed before going quiet is kept, not discarded");
        }

        [Fact]
        public async Task TheStalledAgentIsActuallyCancelledAndNotLeftRunningAsync()
        {
            var cancelled = new TaskCompletionSource();

            async IAsyncEnumerable<AgentStreamItem> StallsAsync([EnumeratorCancellation] CancellationToken token)
            {
                await using var registration = token.Register(() => cancelled.TrySetResult())
                    .ConfigureAwait(false);

                yield return Step("one");
                await Task.Delay(Timeout.Infinite, token);
            }

            await foreach (var _ in AgentIdleWatchdog.WithIdleTimeoutAsync(
                               StallsAsync, TimeSpan.FromMilliseconds(120)))
            {
            }

            await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));

            cancelled.Task.IsCompletedSuccessfully.Should().BeTrue(
                "returning it to the woodlands has to actually stop it, or the tokens keep being "
                + "spent by something nobody is listening to any more");
        }

        [Fact]
        public async Task ANonPositiveTimeoutLetsEverythingThroughUnwatchedAsync()
        {
            async IAsyncEnumerable<AgentStreamItem> SlowAsync([EnumeratorCancellation] CancellationToken token)
            {
                await Task.Delay(120, token);
                yield return Step("late but fine");
            }

            var idle = false;
            var seen = new List<AgentStreamItem>();

            await foreach (var item in AgentIdleWatchdog.WithIdleTimeoutAsync(
                               SlowAsync, TimeSpan.Zero, () => idle = true))
            {
                seen.Add(item);
            }

            seen.Should().ContainSingle("zero disables the watchdog rather than retiring instantly");
            idle.Should().BeFalse();
        }

        [Fact]
        public async Task CallerCancellationStillPropagatesRatherThanBeingTreatedAsIdlenessAsync()
        {
            async IAsyncEnumerable<AgentStreamItem> StallsAsync([EnumeratorCancellation] CancellationToken token)
            {
                yield return Step("one");
                await Task.Delay(Timeout.Infinite, token);
            }

            var cts = new CancellationTokenSource();
            var idle = false;

            async Task ActAsync()
            {
                await foreach (var _ in AgentIdleWatchdog.WithIdleTimeoutAsync(
                                   StallsAsync, TimeSpan.FromSeconds(30), () => idle = true, cts.Token))
                {
                    await cts.CancelAsync();
                }
            }

            await FluentActions.Awaiting(ActAsync).Should().ThrowAsync<OperationCanceledException>();

            idle.Should().BeFalse(
                "a caller hanging up is not the agent dying, and must not be reported as one");
        }

        [Fact]
        public async Task AnEmptyRunCompletesWithoutBeingCalledIdleAsync()
        {
            async IAsyncEnumerable<AgentStreamItem> NothingAsync([EnumeratorCancellation] CancellationToken token)
            {
                await Task.CompletedTask;
                yield break;
            }

            var idle = false;

            var seen = new List<AgentStreamItem>();

            await foreach (var item in AgentIdleWatchdog.WithIdleTimeoutAsync(
                               NothingAsync, TimeSpan.FromMilliseconds(200), () => idle = true))
            {
                seen.Add(item);
            }

            seen.Should().BeEmpty();
            idle.Should().BeFalse("finishing quickly with nothing to say is not the same as hanging");
        }
    }
}
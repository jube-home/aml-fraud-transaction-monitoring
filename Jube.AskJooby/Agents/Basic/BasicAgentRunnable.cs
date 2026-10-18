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

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Jube.AskJooby.Agents.Interfaces;
using Jube.AskJooby.Agents.Models;
using Jube.AskJooby.Reasoning;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Jube.AskJooby.Agents.Basic
{
    public sealed class BasicAgentRunnable(AIAgent agent, string runId, RunContext context)
        : IRunnableAgent
    {
        public AIAgent Agent { get; } = agent;
        public string RunId { get; } = runId;

        public async Task<string> RunAsync(string task, CancellationToken token = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await Agent.RunAsync(task, cancellationToken: token);
                var result = response.FinalAnswer();
                await CompleteAsync("Completed", result, stopwatch.ElapsedMilliseconds, token);

                return result;
            }
            catch (Exception ex)
            {
                await CompleteAsync("Failed", ex.Message, stopwatch.ElapsedMilliseconds, token);
                throw;
            }
        }

        public IAsyncEnumerable<AgentStreamItem> RunStreamingAsync(string task, CancellationToken token = default)
        {
            return StreamAsync(Agent.RunStreamingAsync(task, cancellationToken: token), token);
        }

        public IAsyncEnumerable<AgentStreamItem> RunStreamingAsync(
            IReadOnlyList<ChatMessage> messages, CancellationToken token = default)
        {
            return StreamAsync(Agent.RunStreamingAsync(messages, cancellationToken: token), token);
        }

        private async IAsyncEnumerable<AgentStreamItem> StreamAsync(
            IAsyncEnumerable<AgentResponseUpdate> updates,
            [EnumeratorCancellation] CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();
            var replyText = new StringBuilder();
            var pendingCalls = new Dictionary<string, FunctionCallContent>();
            var narratedCalls = new HashSet<string>(StringComparer.Ordinal);

            await using var enumerator = updates.GetAsyncEnumerator(token);

            while (true)
            {
                AgentResponseUpdate? update = null;
                string? errorMessage = null;

                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }

                    update = enumerator.Current;
                }
                catch (Exception ex)
                {
                    var root = ex;

                    while (root.InnerException is not null)
                    {
                        root = root.InnerException;
                    }

                    errorMessage = root.Message;
                }

                if (errorMessage is not null)
                {
                    await CompleteAsync("Failed", errorMessage, stopwatch.ElapsedMilliseconds, token);
                    yield return new AgentStreamItem(AgentStreamItemKind.Error, errorMessage);
                    yield break;
                }

                foreach (var content in update!.Contents)
                {
                    switch (content)
                    {
                        case FunctionCallContent call:
                            pendingCalls[call.CallId] = call;

                            if (!ClaimNarration(call))
                            {
                                break;
                            }

                            narratedCalls.Add(call.CallId);
                            var start = AgentStreamNarrator.DescribeStart(call);

                            if (start is not null)
                            {
                                yield return start;
                            }

                            break;
                        case FunctionResultContent result when pendingCalls.Remove(result.CallId, out var call):
                            if (narratedCalls.Remove(result.CallId))
                            {
                                yield return AgentStreamNarrator.Describe(call, result);
                            }

                            break;
                    }
                }

                if (TryGetReasoningContent(update, out var reasoning))
                {
                    if (!context.EnableThinking)
                    {
                        context.TurnActivity?.NoteUnrequestedReasoning();
                    }
                }

                if (context.EnableThinking && TryGetReasoningContent(update, out reasoning))
                {
                    yield return new AgentStreamItem(AgentStreamItemKind.Thinking, reasoning);
                }

                if (!string.IsNullOrEmpty(update.Text))
                {
                    replyText.Append(update.Text);
                    yield return new AgentStreamItem(AgentStreamItemKind.Text, update.Text);
                }
            }

            await CompleteAsync("Completed", replyText.ToString(), stopwatch.ElapsedMilliseconds, token);
        }

        private bool ClaimNarration(FunctionCallContent call)
        {
            if (context.RepeatedTools is null)
            {
                return true;
            }

            try
            {
                return context.RepeatedTools.ClaimNarration(call.Name,
                    JsonSerializer.Serialize(call.Arguments ?? new Dictionary<string, object?>()));
            }
            catch (NotSupportedException)
            {
                return true;
            }
        }

        private static bool TryGetReasoningContent(AgentResponseUpdate update, out string reasoning)
        {
            reasoning = string.Empty;

            return update.RawRepresentation is ChatResponseUpdate inner
                   && ReasoningDelta.TryRead(inner, out reasoning);
        }

        private Task CompleteAsync(string status, string? result, long durationMs, CancellationToken token)
        {
            return context.Recorder?.CompleteRunAsync(RunId, status, result, durationMs, token) ?? Task.CompletedTask;
        }
    }
}
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
using System.Text;
using Jube.AskJooby.Reasoning;
using Microsoft.Extensions.AI;

namespace Jube.AskJooby.Observability
{
    public sealed class ProviderMarkupChatClient(IChatClient inner) : IChatClient
    {
        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var history = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();

            var response = await inner.GetResponseAsync(history, options, cancellationToken)
                .ConfigureAwait(false);

            var names = ToolNames(options);
            var text = string.Concat(response.Messages.Select(message => message.Text));

            var hadRealCall = response.Messages.SelectMany(message => message.Contents)
                .Any(content => content is FunctionCallContent);

            var fabricated = !hadRealCall && ProviderMarkup.FabricationIndex(text, names) >= 0;

            var truncated = !hadRealCall && !fabricated
                                         && response.FinishReason == ChatFinishReason.Stop
                                         && ProviderMarkup.LooksTruncated(text);

            if (!fabricated && !truncated)
            {
                return response;
            }

            var continuation = await inner
                .GetResponseAsync(Reask(history, text, names, truncated), options, cancellationToken)
                .ConfigureAwait(false);

            if (!truncated)
            {
                return continuation;
            }

            var combined = new List<ChatMessage>(continuation.Messages);

            if (combined.Count > 0)
            {
                combined[0] = new ChatMessage(combined[0].Role, text + combined[0].Text)
                {
                    AuthorName = combined[0].AuthorName,
                    MessageId = combined[0].MessageId,
                    AdditionalProperties = combined[0].AdditionalProperties
                };
            }
            else
            {
                combined.Add(new ChatMessage(ChatRole.Assistant, text));
            }

            return new ChatResponse(combined)
            {
                ResponseId = continuation.ResponseId,
                ConversationId = continuation.ConversationId,
                ModelId = continuation.ModelId,
                CreatedAt = continuation.CreatedAt,
                FinishReason = continuation.FinishReason,
                Usage = continuation.Usage,
                ContinuationToken = continuation.ContinuationToken,
                RawRepresentation = continuation.RawRepresentation,
                AdditionalProperties = continuation.AdditionalProperties
            };
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var history = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
            var names = ToolNames(options);

            var first = new StreamPass(names);

            await foreach (var update in Pass(history, options, first, cancellationToken)
                               .ConfigureAwait(false))
            {
                yield return update;
            }

            if (!first.NeedsReask)
            {
                yield break;
            }

            var second = new StreamPass(names);

            await foreach (var update in Pass(
                               Reask(history, first.Delivered, names, first.Truncated), options, second,
                               cancellationToken).ConfigureAwait(false))
            {
                yield return update;
            }

            if (second.NeedsReask)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant,
                    Unresolved(second.ToolName, second.Truncated));
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return inner.GetService(serviceType, serviceKey);
        }

        public void Dispose()
        {
            inner.Dispose();
        }

        private async IAsyncEnumerable<ChatResponseUpdate> Pass(
            IReadOnlyList<ChatMessage> messages, ChatOptions? options, StreamPass pass,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var update in inner.GetStreamingResponseAsync(messages, options, cancellationToken)
                               .ConfigureAwait(false))
            {
                if (update.Contents.Any(content => content is FunctionCallContent))
                {
                    pass.NoteRealCall();
                }

                if (update.FinishReason is not null)
                {
                    pass.NoteFinishReason(update.FinishReason);
                }

                if (string.IsNullOrEmpty(update.Text))
                {
                    yield return update;
                    continue;
                }

                var releasable = pass.Accept(update.Text);

                if (releasable.Length == 0)
                {
                    var carried = update.Contents.Where(content => content is not TextContent).ToList();

                    if (carried.Count > 0)
                    {
                        yield return new ChatResponseUpdate(update.Role, carried);
                    }

                    continue;
                }

                yield return new ChatResponseUpdate(update.Role, releasable);
            }

            var remainder = pass.Finish();

            if (remainder.Length > 0)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, remainder);
            }
        }

        private static IReadOnlyList<ChatMessage> Reask(
            IReadOnlyList<ChatMessage> messages, string said, IReadOnlyList<string> names, bool truncated)
        {
            var reask = new ChatMessage(ChatRole.User,
                truncated ? TruncatedReaskText() : FabricatedReaskText(said, names));

            var carried = new List<ChatMessage>(messages);
            var trimmed = said.Trim();

            if (trimmed.Length > 0)
            {
                carried.Add(new ChatMessage(ChatRole.Assistant, trimmed));
            }

            carried.Add(reask);

            return carried;
        }

        private static string FabricatedReaskText(string said, IReadOnlyList<string> names)
        {
            var intended = ProviderMarkup.FabricatedToolName(said, names)
                           ?? ProviderMarkup.IntendedToolName(said);

            var which = intended is null ? "A tool call you just made" : $"Your call to {intended}";

            return $"IMPORTANT -- {which} did not happen. You wrote it out as text in your reply instead "
                   + "of issuing it as a tool call, so nothing ran and nothing has been handed to anyone. "
                   + "The text has been withheld from the caller, who has seen only what you said before "
                   + "it. Issue that call now as a real tool call, using the tool exactly as it is named "
                   + "in the tools available to you, and do not describe it, write it out in angle "
                   + "brackets, or claim it has been done.";
        }

        private static string TruncatedReaskText()
        {
            return "IMPORTANT -- your reply above stopped partway through, with nothing further "
                   + "written after it. Finish it now, continuing from exactly where you left off "
                   + "rather than restarting the answer or repeating what you already said.";
        }

        private static string Unresolved(string? intended, bool truncated)
        {
            if (truncated)
            {
                return "\n\nI was not able to finish that reply cleanly even on a second attempt. "
                       + "Ask me again and I will retry it.";
            }

            var which = intended is null ? "the specialist" : intended;

            return $"\n\nI could not complete the handover to {which} -- the call did not go through, "
                   + "and nothing has been run on your behalf. Ask me again and I will retry it.";
        }

        private static IReadOnlyList<string> ToolNames(ChatOptions? options)
        {
            return options?.Tools is null
                ? []
                : options.Tools.OfType<AIFunction>().Select(tool => tool.Name).ToList();
        }

        private sealed class StreamPass(IReadOnlyList<string> names)
        {
            private readonly StringBuilder delivered = new();
            private string held = string.Empty;
            private bool realCall;
            private ChatFinishReason? finishReason;

            private void NoteIfTruncated()
            {
                if (realCall || NeedsReask || finishReason != ChatFinishReason.Stop)
                {
                    return;
                }

                if (!ProviderMarkup.LooksTruncated(delivered.ToString()))
                {
                    return;
                }

                NeedsReask = true;
                Truncated = true;
            }

            public string Delivered => delivered.ToString();
            public string? ToolName { get; private set; }
            public bool NeedsReask { get; private set; }
            public bool Truncated { get; private set; }

            public void NoteRealCall()
            {
                realCall = true;
            }

            public void NoteFinishReason(ChatFinishReason? reason)
            {
                finishReason = reason;
            }

            public string Accept(string text)
            {
                held += text;
                var safe = ProviderMarkup.SafePrefixLength(held, names);

                if (safe == 0)
                {
                    return string.Empty;
                }

                var releasable = held[..safe];
                held = held[safe..];
                delivered.Append(releasable);

                return releasable;
            }

            public string Finish()
            {
                if (held.Length == 0)
                {
                    NoteIfTruncated();

                    return string.Empty;
                }

                var cut = ProviderMarkup.FabricationIndex(held, names);

                if (cut < 0 || realCall)
                {
                    delivered.Append(held);
                    var all = held;
                    held = string.Empty;
                    NoteIfTruncated();

                    return all;
                }

                ToolName = ProviderMarkup.FabricatedToolName(held, names)
                           ?? ProviderMarkup.IntendedToolName(held);

                NeedsReask = true;

                var kept = held[..cut].TrimEnd();
                held = string.Empty;
                delivered.Append(kept);

                return kept;
            }
        }
    }
}
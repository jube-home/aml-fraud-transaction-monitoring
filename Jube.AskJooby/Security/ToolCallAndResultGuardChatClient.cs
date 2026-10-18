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
using System.Text.Json;
using AgentGuard.Core.Abstractions;
using AgentGuard.Core.Builders;
using AgentGuard.Core.Guardrails;
using AgentGuard.Core.Ledger;
using AgentGuard.Core.Rules.ToolCall;
using AgentGuard.Core.Rules.ToolResult;
using Jube.AskJooby.Observability;
using Jube.AskJooby.Security.Interfaces;
using Jube.AskJooby.Security.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jube.AskJooby.Security
{
    public sealed class ToolCallAndResultGuardChatClient(
        IChatClient innerClient,
        IAgentGuardRejectionLedger? guardRejectionLedger = null,
        bool isLandlord = false,
        IReadOnlyList<AgentGuardRuleSetting>? agentGuardRules = null)
        : DelegatingChatClient(innerClient)
    {
        private const string CallPipelineName = "askjooby-tool-call-guard";
        private const string ResultPipelineName = "askjooby-tool-result-guard";

        private static readonly GuardrailPipeline callPipeline = new(
            new GuardrailPolicyBuilder(CallPipelineName)
                .GuardToolCalls(new ToolCallGuardrailOptions
                {
                    Categories = ToolCallInjectionCategory.All,
                    PerToolAllowedArguments = AgentGuardToolProvenance.AllowedCallArguments()
                })
                .Build(),
            NullLogger<GuardrailPipeline>.Instance,
            new HashChainLedger(jsonlFilePath: null));

        private static readonly GuardrailPipeline resultPipeline = new(
            new GuardrailPolicyBuilder(ResultPipelineName)
                .GuardToolResults(new ToolResultGuardrailOptions
                {
                    Action = ToolResultAction.Block,
                    SkippedTools = AgentGuardToolProvenance.SkippedResultTools()
                })
                .Build(),
            NullLogger<GuardrailPipeline>.Instance,
            new HashChainLedger(jsonlFilePath: null));

        public override async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var sent = messages as IList<ChatMessage> ?? messages.ToList();

            var resultBlock = await CheckToolResultsAsync(sent, guardRejectionLedger, cancellationToken)
                .ConfigureAwait(false);

            if (resultBlock is not null)
            {
                return BlockedResponse(resultBlock);
            }

            var response = await base.GetResponseAsync(sent, options, cancellationToken)
                .ConfigureAwait(false);

            var callBlock = await CheckToolCallsAsync(
                    response.Messages.SelectMany(m => m.Contents), guardRejectionLedger, cancellationToken)
                .ConfigureAwait(false);

            if (callBlock is not null)
            {
                return BlockedResponse(callBlock);
            }

            var textBlock = await CheckOutputTextAsync(response.Text, guardRejectionLedger, isLandlord,
                agentGuardRules, cancellationToken).ConfigureAwait(false);

            return textBlock is null ? response : BlockedResponse(textBlock);
        }

        public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var sent = messages as IList<ChatMessage> ?? messages.ToList();

            var resultBlock = await CheckToolResultsAsync(sent, guardRejectionLedger, cancellationToken)
                .ConfigureAwait(false);

            if (resultBlock is not null)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, resultBlock);
                yield break;
            }

            var updates = new List<ChatResponseUpdate>();

            await foreach (var update in base.GetStreamingResponseAsync(sent, options, cancellationToken)
                               .ConfigureAwait(false))
            {
                updates.Add(update);
            }

            var callBlock = await CheckToolCallsAsync(
                updates.SelectMany(u => u.Contents), guardRejectionLedger, cancellationToken).ConfigureAwait(false);

            if (callBlock is not null)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, callBlock);
                yield break;
            }

            var combinedText = string.Concat(updates.Select(u => u.Text));

            var textBlock = await CheckOutputTextAsync(combinedText, guardRejectionLedger, isLandlord,
                agentGuardRules, cancellationToken).ConfigureAwait(false);

            if (textBlock is not null)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, textBlock);
                yield break;
            }

            foreach (var update in updates)
            {
                yield return update;
            }
        }

        private static async Task<string?> CheckToolCallsAsync(
            IEnumerable<AIContent> contents, IAgentGuardRejectionLedger? ledger, CancellationToken cancellationToken)
        {
            var calls = contents.OfType<FunctionCallContent>().Select(ToAgentToolCall).ToList();

            if (calls.Count == 0)
            {
                return null;
            }

            var context = new GuardrailContext
            {
                Text = string.Empty,
                Phase = GuardrailPhase.Output,
                Properties = new Dictionary<string, object>
                {
                    [ToolCallGuardrailRule.ToolCallsKey] = calls
                }
            };

            var result = await callPipeline.RunAsync(context, cancellationToken).ConfigureAwait(false);

            if (!result.IsBlocked)
            {
                return null;
            }

            await RecordRejectionAsync(ledger, CallPipelineName, result, cancellationToken).ConfigureAwait(false);

            return DescribeBlock("make that tool call", result);
        }

        private static async Task<string?> CheckToolResultsAsync(
            IList<ChatMessage> messages, IAgentGuardRejectionLedger? ledger, CancellationToken cancellationToken)
        {
            var allContent = messages.SelectMany(m => m.Contents).ToList();

            var toolNameByCallId = allContent.OfType<FunctionCallContent>()
                .ToDictionary(call => call.CallId, call => call.Name);

            var results = allContent.OfType<FunctionResultContent>()
                .Select(result => ToToolResultEntry(result, toolNameByCallId))
                .Where(entry => entry is not null)
                .Select(entry => entry!)
                .ToList();

            if (results.Count == 0)
            {
                return null;
            }

            var context = new GuardrailContext
            {
                Text = string.Empty,
                Phase = GuardrailPhase.Output,
                Properties = new Dictionary<string, object>
                {
                    [ToolResultGuardrailRule.ToolResultsKey] = results
                }
            };

            var result = await resultPipeline.RunAsync(context, cancellationToken).ConfigureAwait(false);

            if (!result.IsBlocked)
            {
                return null;
            }

            await RecordRejectionAsync(ledger, ResultPipelineName, result, cancellationToken).ConfigureAwait(false);

            return DescribeBlock("act on that tool result", result);
        }

        private static async Task<string?> CheckOutputTextAsync(
            string? text, IAgentGuardRejectionLedger? ledger, bool isLandlord,
            IReadOnlyList<AgentGuardRuleSetting>? agentGuardRules, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var finding = await AskJoobyInjectionGuard.CheckOutputAsync(text, isLandlord, agentGuardRules)
                .ConfigureAwait(false);

            if (finding is null)
            {
                return null;
            }

            await RecordRejectionAsync(ledger, finding, cancellationToken).ConfigureAwait(false);

            return DescribeOutputBlock(finding);
        }

        private static Task RecordRejectionAsync(IAgentGuardRejectionLedger? ledger, string pipelineName,
            GuardrailPipelineResult result, CancellationToken cancellationToken)
        {
            return ledger is null
                ? Task.CompletedTask
                : ledger.RecordAsync(
                    new AgentGuardRejection("Output", pipelineName, result.BlockingResult?.RuleName,
                        result.BlockingResult?.Reason, "Blocked"),
                    cancellationToken);
        }

        private static Task RecordRejectionAsync(
            IAgentGuardRejectionLedger? ledger, AskJoobyGuardFinding finding, CancellationToken cancellationToken)
        {
            return ledger is null
                ? Task.CompletedTask
                : ledger.RecordAsync(
                    new AgentGuardRejection(
                        finding.Phase, finding.PipelineName, finding.RuleName, finding.Reason, finding.Action),
                    cancellationToken);
        }

        private static string DescribeBlock(string action, GuardrailPipelineResult result)
        {
            var ruleName = result.BlockingResult?.RuleName ?? "agentguard-rule";
            var reason = result.BlockingResult?.Reason ?? "it matched a known injection pattern.";

            return $"I'm not going to {action} -- AgentGuard's '{ruleName}' rule caught it: {reason}";
        }

        private static string DescribeOutputBlock(AskJoobyGuardFinding finding)
        {
            var ruleName = finding.RuleName ?? "agentguard-rule";
            var reason = finding.Reason ?? "it matched a topic-boundary or content-safety rule.";

            return $"I'm not going to send that reply -- AgentGuard's '{ruleName}' rule caught it: {reason}";
        }

        private static AgentToolCall ToAgentToolCall(FunctionCallContent call)
        {
            var arguments = (call.Arguments ?? new Dictionary<string, object?>())
                .ToDictionary(kvp => kvp.Key, kvp => ArgumentToText(kvp.Value));

            return new AgentToolCall
            {
                ToolName = call.Name,
                Arguments = arguments
            };
        }

        private static string ArgumentToText(object? value)
        {
            if (value is null)
            {
                return string.Empty;
            }

            if (value is string text)
            {
                return text;
            }

            try
            {
                return JsonSerializer.Serialize(value);
            }
            catch (NotSupportedException)
            {
                return "[unserialisable argument value]";
            }
        }

        private static ToolResultEntry? ToToolResultEntry(
            FunctionResultContent result, IReadOnlyDictionary<string, string> toolNameByCallId)
        {
            var content = result.Result?.ToString();

            if (string.IsNullOrEmpty(content))
            {
                return null;
            }

            var toolName = toolNameByCallId.GetValueOrDefault(result.CallId, "unknown");

            return new ToolResultEntry { ToolName = toolName, Content = content };
        }

        private static ChatResponse BlockedResponse(string message)
        {
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, message));
        }
    }
}
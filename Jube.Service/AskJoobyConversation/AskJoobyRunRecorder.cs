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

using System.Globalization;
using Jube.AskJooby.Observability.Interfaces;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;

namespace Jube.Service.AskJoobyConversation
{
    internal sealed class AskJoobyRunRecorder(
        AskJoobyRunRepository runRepository,
        AskJoobyToolCallRepository toolCallRepository,
        AskJoobyModelCallRepository modelCallRepository,
        AskJoobyEmbeddingCallRepository embeddingCallRepository,
        AskJoobyRerankCallRepository rerankCallRepository,
        AskJoobyWebSearchCallRepository webSearchCallRepository,
        AskJoobyOpenCorporatesCallRepository openCorporatesCallRepository,
        int? askJoobyConversationId,
        int tenantRegistryId,
        string userName) : IRunRecorder
    {
        private int? askJoobyRoutingId;

        public void AttachRouting(int routingId)
        {
            askJoobyRoutingId = routingId;
        }

        public async Task<string> StartRunAsync(string agentType, int depth, string? parentRunId, string task,
            CancellationToken token = default)
        {
            var run = await runRepository.InsertAsync(new AskJoobyRun
            {
                AskJoobyConversationId = askJoobyConversationId,
                ParentAskJoobyRunId = ParseOrNull(parentRunId),
                TenantRegistryId = tenantRegistryId,
                AgentType = agentType,
                Depth = depth,
                Task = task,
                AskJoobyRoutingId = depth == 0 ? askJoobyRoutingId : null,
                CreatedUser = userName
            }, token).ConfigureAwait(false);

            return run.Id.ToString(CultureInfo.InvariantCulture);
        }

        public Task CompleteRunAsync(string runId, string status, string? result, long durationMs,
            CancellationToken token = default)
        {
            return runRepository.CompleteAsync(int.Parse(runId, CultureInfo.InvariantCulture),
                (byte)MapStatus(status), result, durationMs, token);
        }

        public Task RecordToolCallAsync(string runId, string toolName, string? argumentsJson, string? result,
            string status, long durationMs, CancellationToken token = default)
        {
            return toolCallRepository.InsertAsync(new AskJoobyToolCall
            {
                AskJoobyRunId = int.Parse(runId, CultureInfo.InvariantCulture),
                ToolName = toolName,
                Arguments = argumentsJson,
                Result = result,
                StatusId = (byte)MapToolCallStatus(status),
                DurationMs = durationMs
            }, token);
        }

        public Task RecordModelCallAsync(string runId, string model, int? inputTokens, int? outputTokens,
            long? cachedInputTokens, string? promptPrefixHash, string? usageCountsJson,
            long? promptDetailCachedTokens,
            string? finishReason, long durationMs, long? firstChunkMs, CancellationToken token = default)
        {
            return modelCallRepository.InsertAsync(new AskJoobyModelCall
            {
                AskJoobyRunId = int.Parse(runId, CultureInfo.InvariantCulture),
                ModelTypeId = (byte)MapModelType(model),
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                CachedInputTokens = (int?)cachedInputTokens,
                PromptPrefixHash = promptPrefixHash,
                UsageCountsJson = usageCountsJson,
                PromptDetailCachedTokens = (int?)promptDetailCachedTokens,
                FinishReasonId = (byte)MapFinishReason(finishReason),
                DurationMs = durationMs,
                FirstChunkMs = firstChunkMs
            }, token);
        }

        public Task RecordEmbeddingCallAsync(string runId, string model, int? inputTokens, long durationMs,
            CancellationToken token = default)
        {
            return embeddingCallRepository.InsertAsync(new AskJoobyEmbeddingCall
            {
                AskJoobyRunId = int.Parse(runId, CultureInfo.InvariantCulture),
                ModelTypeId = (byte)MapModelType(model),
                InputTokens = inputTokens,
                DurationMs = durationMs
            }, token);
        }

        public Task RecordRerankCallAsync(string runId, string model, int documentsCompared, int? tokensCompared,
            long durationMs, CancellationToken token = default)
        {
            return rerankCallRepository.InsertAsync(new AskJoobyRerankCall
            {
                AskJoobyRunId = int.Parse(runId, CultureInfo.InvariantCulture),
                ModelTypeId = (byte)MapModelType(model),
                DocumentsCompared = documentsCompared,
                TokensCompared = tokensCompared,
                DurationMs = durationMs
            }, token);
        }

        public Task RecordWebSearchCallAsync(string runId, string query, string? responseJson, long durationMs,
            CancellationToken token = default)
        {
            return webSearchCallRepository.InsertAsync(new AskJoobyWebSearchCall
            {
                AskJoobyRunId = int.Parse(runId, CultureInfo.InvariantCulture),
                Query = query,
                ResponseJson = responseJson,
                DurationMs = durationMs
            }, token);
        }

        public Task RecordOpenCorporatesCallAsync(string runId, string operation, string query,
            string? responseJson, long durationMs, CancellationToken token = default)
        {
            return openCorporatesCallRepository.InsertAsync(new AskJoobyOpenCorporatesCall
            {
                AskJoobyRunId = int.Parse(runId, CultureInfo.InvariantCulture),
                Operation = operation,
                Query = query,
                ResponseJson = responseJson,
                DurationMs = durationMs
            }, token);
        }

        private static AskJoobyModelType MapModelType(string? model)
        {
            if (string.IsNullOrWhiteSpace(model))
            {
                return AskJoobyModelType.Unknown;
            }

            var lowered = model.ToLowerInvariant();

            if (lowered.Contains("bge-reranker"))
            {
                return AskJoobyModelType.BgeRerankerV2M3;
            }

            if (lowered.Contains("bge"))
            {
                return AskJoobyModelType.BgeM3;
            }

            if (lowered.Contains("deepseek"))
            {
                return lowered.Contains("pro") ? AskJoobyModelType.DeepSeekV4Pro : AskJoobyModelType.DeepSeekV41Flash;
            }

            return AskJoobyModelType.Unknown;
        }

        private static AskJoobyRunStatus MapStatus(string? status)
        {
            return Enum.TryParse<AskJoobyRunStatus>(status, true, out var parsed)
                ? parsed
                : AskJoobyRunStatus.Completed;
        }

        private static AskJoobyToolCallStatus MapToolCallStatus(string? status)
        {
            return Enum.TryParse<AskJoobyToolCallStatus>(status, true, out var parsed)
                ? parsed
                : AskJoobyToolCallStatus.Completed;
        }

        private static AskJoobyFinishReason MapFinishReason(string? finishReason)
        {
            if (string.IsNullOrWhiteSpace(finishReason))
            {
                return AskJoobyFinishReason.Unknown;
            }

            return finishReason.ToLowerInvariant() switch
            {
                "stop" => AskJoobyFinishReason.Stop,
                "length" => AskJoobyFinishReason.Length,
                "tool_calls" => AskJoobyFinishReason.ToolCalls,
                "content_filter" => AskJoobyFinishReason.ContentFilter,
                _ => AskJoobyFinishReason.Unknown
            };
        }

        private static int? ParseOrNull(string? value)
        {
            return string.IsNullOrEmpty(value) ? null : int.Parse(value, CultureInfo.InvariantCulture);
        }
    }
}
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

namespace Jube.AskJooby.Observability.Interfaces
{
    public interface IRunRecorder
    {
        Task<string> StartRunAsync(string agentType, int depth, string? parentRunId, string task,
            CancellationToken token = default);

        Task CompleteRunAsync(string runId, string status, string? result, long durationMs,
            CancellationToken token = default);

        Task RecordToolCallAsync(string runId, string toolName, string? argumentsJson, string? result,
            string status, long durationMs, CancellationToken token = default);

        void AttachRouting(int askJoobyRoutingId)
        {
        }

        Task RecordModelCallAsync(string runId, string model, int? inputTokens, int? outputTokens,
            long? cachedInputTokens, string? promptPrefixHash, string? usageCountsJson,
            long? promptDetailCachedTokens,
            string? finishReason, long durationMs, long? firstChunkMs, CancellationToken token = default);

        Task RecordEmbeddingCallAsync(string runId, string model, int? inputTokens, long durationMs,
            CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        Task RecordRerankCallAsync(string runId, string model, int documentsCompared, int? tokensCompared,
            long durationMs, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        Task RecordWebSearchCallAsync(string runId, string query, string? responseJson, long durationMs,
            CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        Task RecordOpenCorporatesCallAsync(string runId, string operation, string query, string? responseJson,
            long durationMs, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }
    }
}
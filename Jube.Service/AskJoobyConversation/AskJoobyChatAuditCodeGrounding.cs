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

using System.Text;
using Jube.AskJooby.Rag.Models;
using Jube.AskJooby.Tools.Rag.Interfaces;
using Jube.Data.Query.GetAskJoobyRagChunkByQualifiedNameQuery;
using Jube.Data.Query.SearchAskJoobyRagChunkQuery;
using Jube.Data.Repository.Models;
using Jube.Service.AskJoobyRagChunk;

namespace Jube.Service.AskJoobyConversation
{
    internal static class AskJoobyChatAuditCodeGrounding
    {
        private static readonly string[] orchestrationQualifiedNames =
        [
            "Jube.App.Startup.AddSingletonForAskJooby",
            "Jube.AskJooby.Routing.AgentRouter",
            "Jube.AskJooby.Routing.Models.RoutingRules"
        ];

        private const int AgentCodeTopK = 4;
        private const string CodeCategory = nameof(AskJoobyRagChunkCategory.Code);

        internal static async Task<string?> BuildOrchestrationCodeAsync(
            GetAskJoobyRagChunkByQualifiedNameQuery query, int budgetCharacters, CancellationToken token)
        {
            var rows = await query.ExecuteAsync(orchestrationQualifiedNames, token).ConfigureAwait(false);

            var chunks = rows.Select(Map)
                .OrderBy(chunk => chunk.SourceFile, StringComparer.Ordinal)
                .ThenBy(chunk => chunk.QualifiedName, StringComparer.Ordinal);

            return Format("Core Orchestration Code", chunks, budgetCharacters);
        }

        internal static async Task<string?> BuildAgentCodeAsync(
            IRagClient ragClient, IEnumerable<string> agentTypes, int budgetCharacters, CancellationToken token)
        {
            var seenHashes = new HashSet<string>(StringComparer.Ordinal);
            var chunks = new List<RagChunk>();

            foreach (var agentType in agentTypes.Where(t => !string.IsNullOrWhiteSpace(t))
                         .Distinct(StringComparer.Ordinal))
            {
                var hits = await ragClient.SearchAsync(agentType, AgentCodeTopK, token: token).ConfigureAwait(false);

                foreach (var hit in hits.Where(hit => string.Equals(hit.Category, CodeCategory,
                             StringComparison.Ordinal)))
                {
                    if (seenHashes.Add(hit.Hash))
                    {
                        chunks.Add(hit);
                    }
                }
            }

            return Format("Agent Implementation Code", chunks, budgetCharacters);
        }

        private static string? Format(string heading, IEnumerable<RagChunk> chunks, int budgetCharacters)
        {
            var ordered = chunks.ToList();

            if (ordered.Count == 0)
            {
                return null;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## {heading}");
            sb.AppendLine();

            foreach (var chunk in ordered)
            {
                if (sb.Length >= budgetCharacters)
                {
                    break;
                }

                sb.AppendLine(chunk.Content);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static RagChunk Map(SearchAskJoobyRagChunkQueryDto row)
        {
            return new RagChunk(
                row.Hash, row.ParentHash, AskJoobyRagClient.CategoryName(row.CategoryId),
                AskJoobyRagClient.KindName(row.KindId), row.SourceFile, row.QualifiedName,
                row.Content ?? string.Empty, row.Score);
        }
    }
}
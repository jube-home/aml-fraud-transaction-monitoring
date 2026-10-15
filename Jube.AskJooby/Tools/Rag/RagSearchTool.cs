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

using System.ComponentModel;
using System.Text;
using Jube.AskJooby.Observability.Interfaces;
using Jube.AskJooby.Rag.Models;
using Jube.AskJooby.Tools.Rag.Interfaces;

namespace Jube.AskJooby.Tools.Rag
{
    public sealed class RagSearchTool(
        IRagClient rag,
        int topK = 5,
        double minScore = 0,
        IRetrievedBiteLedger? ledger = null,
        IRunRecorder? recorder = null,
        string? runId = null)
    {
        private const int MaxAllowedBites = 8;
        private const int MaxAllowedCandidates = 50;

        [Description(
            "Searches the Pasture -- Jube documentation, rule taxonomy, code, and database " +
            "schema, chunked into Bites -- and returns the most relevant Bites. Use this before " +
            "writing anything that depends on Jube-specific knowledge -- a token/extension name, " +
            "a rule structure, a table name -- rather than relying on what you already believe " +
            "about Jube. " +
            "Two searches run underneath, a keyword one and a meaning-based one, and their two " +
            "rankings are merged by reciprocal rank fusion before you see anything. That is worth " +
            "knowing when you read the order: a Bite near the top is one BOTH searches liked, " +
            "which is a stronger signal than a high score from either alone, and a Bite further " +
            "down may still be the one that answers you -- it may simply have been found by only " +
            "one of the two. Read what came back and judge it, rather than taking the first Bite " +
            "as the answer. " +
            "A reranker is also in place, and it changes how wide you should search. After the two " +
            "searches are merged, it reads your query against every candidate together and keeps the " +
            "best of them, so cast the net wide: set candidates (up to 50) for how many Bites the " +
            "search may consider, then set maxBites for how many you actually receive back (up to 8). " +
            "Only the Bites you receive enter your context, so a wide candidate pool costs you nothing " +
            "beyond the reranking itself. Do not hold back on candidates to save tokens; hold back on " +
            "maxBites. " +
            "A Bite already given to you earlier in this turn is named rather than " +
            "printed again; it has not changed, so scroll up for it instead of searching again.")]
        public async Task<string> SearchRag(
            [Description("The search query -- a natural-language description of what you need to know.")]
            string query,
            [Description(
                "How many Bites to return to you, after reranking. Leave unset to use the deployment " +
                "default. Use a lower number (1-2) for a narrow, specific lookup where extra Bites would " +
                "just be noise; use a higher number (up to 8) for a broad question, or when you're not " +
                "confident which Bite will actually answer it.")]
            int? maxBites = null,
            [Description(
                "How many candidate Bites the search may consider before the reranker chooses from them. " +
                "Set this wide (20-50) for any question the reranker should judge: it reads each " +
                "candidate against your query, and a wider pool gives it more to choose the best from. " +
                "Leave unset to use the deployment default.")]
            int? candidates = null,
            CancellationToken token = default)
        {
            if (ledger is not null && !ledger.TryStartSearch())
            {
                return "You have used this turn's allowance of Pasture searches. Answer from the Bites you "
                       + "already have, saying plainly what you could not confirm, or ask the caller a "
                       + "clarifying question -- do not keep searching.";
            }

            var effectiveTopK = maxBites.HasValue ? Math.Clamp(maxBites.Value, 1, MaxAllowedBites) : topK;
            var width = candidates.HasValue ? Math.Clamp(candidates.Value, 1, MaxAllowedCandidates) : 0;
            var hits = await rag.SearchAsync(query, effectiveTopK, minScore, width, recorder, runId, token);

            if (hits.Count == 0)
            {
                return "No matching Bites found.";
            }

            var fresh = new List<RagChunk>();
            var repeated = new List<string>();

            foreach (var hit in hits)
            {
                var key = $"{hit.Category}/{hit.Kind}/{hit.QualifiedName}/{hit.SourceFile}";

                if (ledger is null || ledger.Register(key))
                {
                    fresh.Add(hit);
                }
                else
                {
                    repeated.Add(hit.QualifiedName ?? hit.SourceFile ?? hit.Hash);
                }
            }

            var builder = new StringBuilder();

            builder.AppendLine($"Found {hits.Count} Bite{(hits.Count == 1 ? "" : "s")}"
                               + (repeated.Count > 0
                                   ? $", {fresh.Count} of them new."
                                   : "."));

            if (repeated.Count > 0)
            {
                builder.AppendLine();

                builder.AppendLine("Already given to you earlier this turn, unchanged, scroll up rather than "
                                   + "searching for them again: " + string.Join("; ", repeated));
            }

            if (fresh.Count == 0)
            {
                builder.AppendLine();

                builder.AppendLine("Nothing new -- every Bite that came back is one you already have. "
                                   + "Rewording this question will keep returning these same Bites, so "
                                   + "stop asking it. Either ask a materially DIFFERENT question (a "
                                   + "different mechanism, table, token or field by name), or use a "
                                   + "different tool: delegate it to the specialist Joobrik whose slice "
                                   + "owns it, query the data directly, or read the configuration off the "
                                   + "live platform. You have plenty of tools and this is not one of them "
                                   + "any more. If none of that will answer it, say plainly what the "
                                   + "Pasture does not cover rather than searching again.");

                return builder.ToString();
            }

            builder.AppendLine();
            builder.AppendLine("[CRITICAL CONTEXT START]");

            foreach (var hit in fresh)
            {
                builder.AppendLine($"[{hit.Category}/{hit.Kind}] {hit.QualifiedName} ({hit.SourceFile})");
                builder.AppendLine(hit.Content);
                builder.AppendLine();
            }

            builder.AppendLine("[CRITICAL CONTEXT END]");

            return builder.ToString();
        }
    }
}
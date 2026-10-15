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

namespace Jube.AskJooby.Validation
{
    public static class ResponseReviser
    {
        public static string BuildTask(
            string question, string docs, string draft, string feedback, string toolEvidence = "")
        {
            var docsSection = string.IsNullOrWhiteSpace(docs)
                ? "Bites already found while answering: none -- the original was written without a " +
                  "single Pasture search."
                : $"Bites already found while answering:\n[CRITICAL CONTEXT START]\n{docs}\n" +
                  "[CRITICAL CONTEXT END]";

            var toolEvidenceSection = string.IsNullOrWhiteSpace(toolEvidence)
                ? string.Empty
                : "\n\nTool calls actually executed this turn, against the live platform itself, " +
                  "whose own values are already confirmed and must not be changed or re-litigated " +
                  "against the Pasture:\n[CRITICAL CONTEXT START]\n" + toolEvidence +
                  "\n[CRITICAL CONTEXT END]";

            return "You already answered a question once and an independent review of your reply " +
                   "against the Pasture found problems with it. Fix those specific problems. " +
                   "You're only seeing this because the review judged the flagged claim as " +
                   "genuinely findable -- worth chasing down properly this time, not just " +
                   "re-running the same search that already missed it. " +
                   "Try a genuinely different angle: a different phrase or term than you searched " +
                   "before, the specific name or type the claim is actually about, or a related " +
                   "concept nearby it -- two or three real attempts, not one repeat of the original " +
                   "query. Each Bite is labelled with its own category -- if a Code Bite and a " +
                   "Document Bite genuinely disagree, trust the code: it's what the software " +
                   "actually does, and docs can lag behind a change that's already shipped. If you " +
                   "can confirm the claim with a real Bite this time, correct it to match exactly " +
                   "what the Bite says; if a real, deeper effort still finds nothing, remove the " +
                   "claim or say plainly the docs don't cover it, rather than defending or " +
                   "rewording your original guess. Keep the same tone, length, and directness as " +
                   "your original reply -- do not mention that it was reviewed or corrected. Output " +
                   "ONLY the corrected reply text, nothing else.\n\n" +
                   $"The question:\n\n{question}\n\nYour reply:\n{draft}\n\n" +
                   $"An independent review of your reply against the Pasture found:\n{feedback}\n\n" +
                   $"{docsSection}{toolEvidenceSection}\n\nOutput ONLY the corrected reply text, nothing else.";
        }
    }
}
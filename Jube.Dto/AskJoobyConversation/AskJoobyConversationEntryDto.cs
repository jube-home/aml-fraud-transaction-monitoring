/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.AskJoobyConversation
{
    [Description("One entry of a conversation's full transcript -- \"Message\" (a real user/assistant turn), " +
                 "\"Step\" (narrated tool activity), \"Thought\" (narrated reasoning) or \"Delegation\" " +
                 "(narrated Joobrik hand-off), replayed in the same shape the live stream used.")]
    public class AskJoobyConversationEntryDto
    {
        public string Kind { get; set; } = "Message";

        [Description("The underlying AskJoobyMessage Id -- for Kind \"Message\", Role \"Assistant\", usable to " +
                     "submit thumbs-up/down feedback against this reply.")]
        public int Id { get; set; }

        [Description("Who sent this -- \"User\" or \"Assistant\". Only populated for Kind \"Message\".")]
        public string? Role { get; set; }

        public string Text { get; set; } = string.Empty;
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Tokens sent to the model for this reply, summed across every model call this turn made " +
                     "(including any Joobrik delegations). Only populated for Kind \"Message\", Role \"Assistant\".")]
        public int? InputTokens { get; set; }

        [Description("Tokens the model generated for this reply, summed the same way as InputTokens.")]
        public int? OutputTokens { get; set; }

        [Description("How many of InputTokens the provider served from its prompt cache, summed the same " +
                     "way. Null where no model call on this reply reported a cache figure at all, which " +
                     "is not the same as zero: zero means every call reported a miss.")]
        public int? CachedInputTokens { get; set; }

        [Description("Wall-clock milliseconds this reply took to produce, start to finish.")]
        public long? DurationMs { get; set; }

        [Description("How many Joobriks this reply spawned, including the top-level Worker run itself. " +
                     "Only populated for Kind \"Message\", Role \"Assistant\", and only when " +
                     "EnableAskJoobyCostBanner is on.")]
        public int? JoobrikCount { get; set; }

        [Description("How many distinct Joobrik types (agent types) are among JoobrikCount's runs -- a " +
                     "Worker that spawned the same specialist three times still counts as one distinct " +
                     "Joobrik. Same gating as JoobrikCount.")]
        public int? DistinctJoobrikCount { get; set; }

        [Description("The reply's reconstituted all-up cost, in the same currency AskJoobyEndpointFee/" +
                     "AskJoobyRerankFee are priced in: every model call's tokens (cached input priced " +
                     "separately from the rest), every embedding call's tokens, and every reranker call's " +
                     "documents compared, each joined against its own rate and summed. Null when " +
                     "EnableAskJoobyCostBanner is off, or when a model/endpoint this reply used has no " +
                     "matching rate row -- a missing rate is silence about that call's cost, not a free one.")]
        public double? TotalCost { get; set; }

        [Description("Kind \"Step\" only: the AgentToolIcon id drawn beside the pill, the same one the live " +
                     "stream sent for this tool call. Null for a step recorded before icons existed.")]
        public int? IconId { get; set; }

        [Description("Kinds \"Step\", \"Delegation\", \"Handoff\", \"Guard\", \"Validation\" and \"Revision\" " +
                     "only: what was passed and what came back, already truncated, the same text the live " +
                     "stream sent behind the pill. Null for an entry recorded before this column existed.")]
        public string? Detail { get; set; }

        [Description("Files attached to this message, if any -- shown as a download link only, never as " +
                     "inline text (their content, when readable, is still folded into what Jooby sees).")]
        public List<AskJoobyMessageAttachmentDto> Attachments { get; set; } = [];
    }
}
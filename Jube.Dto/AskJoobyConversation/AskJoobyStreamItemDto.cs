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
    [Description("One item of a streamed AskJooby reply -- \"Text\" (a reply token), \"Step\" (narrated tool " +
                 "activity), \"Thought\" (narrated reasoning), \"Title\" (the conversation's auto-generated " +
                 "title, sent once, after the first reply completes), \"Stats\" (token/timing totals for the " +
                 "reply that just finished, sent once, last), \"Suggestions\" (2-3 short follow-up prompts " +
                 "for the reply that just finished, sent once, live only -- never reconstructed when replaying " +
                 "a conversation's history, since a suggestion is tied to a specific moment in the " +
                 "conversation, not something worth persisting) or \"Clarification\" (the reply that just " +
                 "finished was Jooby asking a multiple-choice clarifying question -- see Options/" +
                 "AllowFreeText -- sent once, live only, for the same reason as Suggestions; the question " +
                 "text itself is the ordinary \"Text\" reply and is persisted/replayed normally).")]
    public class AskJoobyStreamItemDto
    {
        public string Kind { get; set; } = "Text";
        public string Text { get; set; } = string.Empty;

        [Description("Kinds \"Thinking\", \"Step\", \"Thought\" and \"Delegation\" only: which Joobrik " +
                     "produced it, e.g. \"Model Risk (Zigneff)\", or null for the top-level Worker. Several " +
                     "Joobriks reason at once during a fan out, so a reasoning pass has to be attributable " +
                     "to one of them or their chunks interleave into a single unreadable block.")]
        public string? AgentLabel { get; set; }

        [Description("Kinds \"Step\", \"Delegation\", \"Handoff\", \"Guard\", \"Validation\" and " +
                     "\"Revision\" only: what was passed and what came back, already truncated, for the " +
                     "caller who wants to see behind the pill. Persisted on the underlying AskJoobyMessage " +
                     "and replayed unchanged from history; the complete, untruncated exchange is kept " +
                     "separately on AskJoobyToolCall and rendered in the chat audit, which is the real record.")]
        public string? Detail { get; set; }

        [Description("Kind \"Step\" only: the AgentToolIcon id of the icon drawn beside the pill for the tool " +
                     "it narrates, e.g. the id of Database for RunSqlQuery. Null when the step is not a tool call.")]
        public int? IconId { get; set; }

        [Description("Kind \"Suggestions\" only: the short follow-up prompts themselves.")]
        public List<string> Suggestions { get; set; } = [];

        [Description("Kind \"Clarification\" only: the selectable option labels.")]
        public List<string> Options { get; set; } = [];

        [Description("Kind \"Clarification\" only: whether the caller may also answer with free text " +
                     "instead of picking an option.")]
        public bool AllowFreeText { get; set; } = true;

        [Description("Kind \"Text\" for a completed Assistant reply only: the AskJoobyMessage Id this reply " +
                     "was saved as, for submitting thumbs-up/down feedback against it.")]
        public int? MessageId { get; set; }

        [Description("Kind \"Text\" for a completed Assistant reply only: files a tool created and attached " +
                     "to this reply (e.g. ExportCachedTableToCsv), if any -- shown as a download link, same " +
                     "as a message's Attachments in AskJoobyConversationEntryDto.")]
        public List<AskJoobyMessageAttachmentDto> Attachments { get; set; } = [];

        [Description("Kind \"Stats\" only: tokens sent to the model, summed across every model call the reply " +
                     "made (including Joobrik delegations).")]
        public int? InputTokens { get; set; }

        [Description("Kind \"Stats\" only: tokens the model generated, summed the same way as InputTokens.")]
        public int? OutputTokens { get; set; }

        [Description("Kind \"Stats\" only: how many of InputTokens the provider served from its prompt " +
                     "cache, summed the same way. Zero means every call reported a miss.")]
        public int? CachedInputTokens { get; set; }

        [Description("Kind \"Stats\" only: wall-clock milliseconds the reply took to produce, start to finish.")]
        public long? DurationMs { get; set; }

        [Description("Kind \"Stats\" only, and only when EnableAskJoobyCostBanner is on: how many Joobriks " +
                     "this reply spawned, including the top-level Worker run itself.")]
        public int? JoobrikCount { get; set; }

        [Description("Kind \"Stats\" only, same gating as JoobrikCount: how many distinct Joobrik types are " +
                     "among those runs.")]
        public int? DistinctJoobrikCount { get; set; }

        [Description("Kind \"Stats\" only, same gating as JoobrikCount: the reply's reconstituted all-up " +
                     "cost (model tokens, embedding tokens and reranker documents, each priced off " +
                     "AskJoobyEndpointFee/AskJoobyRerankFee and summed). Null when a rate is missing for " +
                     "something this reply used, not zero.")]
        public double? TotalCost { get; set; }
    }
}
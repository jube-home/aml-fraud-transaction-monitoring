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

using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Dto.AskJoobyConversation;

namespace Jube.Service.AskJoobyConversation
{
    using ConversationPoco = Data.Poco.AskJoobyConversation;
    using MessagePoco = AskJoobyMessage;

    internal readonly record struct RunStats(
        int InputTokens,
        int OutputTokens,
        long? DurationMs,
        int? CachedInputTokens = null,
        int? JoobrikCount = null,
        int? DistinctJoobrikCount = null,
        int? EmbeddingInputTokens = null,
        int? RerankDocumentsCompared = null,
        double? ChatCost = null,
        double? EmbeddingCost = null,
        double? RerankCost = null,
        double? TotalCost = null);

    internal static class AskJoobyConversationMapper
    {
        public static AskJoobyConversationDto? ToDto(ConversationPoco? conversation)
        {
            return conversation is null
                ? null
                : new AskJoobyConversationDto
                {
                    Id = conversation.Id,
                    Guid = conversation.Guid,
                    Title = conversation.Title,
                    CreatedUser = conversation.CreatedUser,
                    CreatedDate = ToOffset(conversation.CreatedDate),
                    UpdatedUser = conversation.UpdatedUser,
                    UpdatedDate = ToOffset(conversation.UpdatedDate)
                };
        }

        public static List<AskJoobyConversationDto> ToDto(IEnumerable<ConversationPoco>? source)
        {
            return (source ?? Enumerable.Empty<ConversationPoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static AskJoobyConversationEntryDto ToEntryDto(MessagePoco message,
            IReadOnlyDictionary<int, RunStats>? runStats = null,
            IReadOnlyDictionary<int, List<AskJoobyMessageAttachmentDto>>? attachmentsByMessageId = null)
        {
            var kind = message.EntryTypeId switch
            {
                AskJoobyMessageRepository.EntryTypeStep => "Step",
                AskJoobyMessageRepository.EntryTypeThought => "Thought",
                AskJoobyMessageRepository.EntryTypeThinking => "Thinking",
                AskJoobyMessageRepository.EntryTypeDelegation => "Delegation",
                AskJoobyMessageRepository.EntryTypeHandoff => "Handoff",
                AskJoobyMessageRepository.EntryTypeValidation => "Validation",
                AskJoobyMessageRepository.EntryTypeRevision => "Revision",
                _ => "Message"
            };

            var role = message.RoleId switch
            {
                (byte)AskJoobyMessageRole.User => "User",
                (byte)AskJoobyMessageRole.Assistant => "Assistant",
                (byte)AskJoobyMessageRole.System => "System",
                _ => null
            };

            var stats = message.AskJoobyRunId.HasValue && runStats is not null &&
                        runStats.TryGetValue(message.AskJoobyRunId.Value, out var found)
                ? found
                : (RunStats?)null;

            var attachments = attachmentsByMessageId is not null &&
                              attachmentsByMessageId.TryGetValue(message.Id, out var found2)
                ? found2
                : [];

            return new AskJoobyConversationEntryDto
            {
                Kind = kind,
                Id = message.Id,
                IconId = message.IconId,
                Detail = message.Detail,
                Role = kind == "Message" ? role : null,
                Text = message.Content,
                CreatedDate = ToOffset(message.CreatedDate),
                InputTokens = stats?.InputTokens,
                OutputTokens = stats?.OutputTokens,
                CachedInputTokens = stats?.CachedInputTokens,
                DurationMs = stats?.DurationMs,
                JoobrikCount = stats?.JoobrikCount,
                DistinctJoobrikCount = stats?.DistinctJoobrikCount,
                TotalCost = stats?.TotalCost,
                Attachments = attachments
            };
        }

        public static List<AskJoobyConversationEntryDto> ToEntryDto(IEnumerable<MessagePoco> source,
            IReadOnlyDictionary<int, RunStats>? runStats = null,
            IReadOnlyDictionary<int, List<AskJoobyMessageAttachmentDto>>? attachmentsByMessageId = null)
        {
            return source.Select(m => ToEntryDto(m, runStats, attachmentsByMessageId)).ToList();
        }

        private static DateTimeOffset? ToOffset(DateTime? value)
        {
            return value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
        }
    }
}
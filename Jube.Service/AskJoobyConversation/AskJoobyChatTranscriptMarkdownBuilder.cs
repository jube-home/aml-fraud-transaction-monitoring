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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;

namespace Jube.Service.AskJoobyConversation
{
    using ConversationPoco = Data.Poco.AskJoobyConversation;

    internal static class AskJoobyChatTranscriptMarkdownBuilder
    {
        private const string FileDownloadPathPrefix = "/api/AskJoobyConversation/Files/";

        public static string Build(
            ConversationPoco conversation,
            IReadOnlyList<AskJoobyMessage> entries,
            IReadOnlyList<AskJoobyFile> files,
            IReadOnlyList<AskJoobyCompaction> compactions,
            DateTime generatedAtUtc,
            string generatedByUser)
        {
            var sb = new StringBuilder();

            AppendHeader(sb, conversation, compactions, generatedAtUtc, generatedByUser);
            AppendTranscript(sb, entries, files);

            return sb.ToString();
        }

        private static void AppendHeader(StringBuilder sb, ConversationPoco conversation,
            IReadOnlyList<AskJoobyCompaction> compactions, DateTime generatedAtUtc, string generatedByUser)
        {
            sb.AppendLine($"# AskJooby Chat Transcript -- {NullToDash(conversation.Title)}");
            sb.AppendLine();

            sb.AppendLine("A clean transcript of this conversation -- what was asked, and what Jooby " +
                          "replied -- with none of the technical detail behind each reply.");

            sb.AppendLine();

            sb.AppendLine($"- **Started**: {FormatDate(conversation.CreatedDate)} by " +
                          $"{NullToDash(conversation.CreatedUser)}");

            sb.AppendLine($"- **Generated**: {FormatDate(generatedAtUtc)} by {NullToDash(generatedByUser)}");
            sb.AppendLine();

            sb.AppendLine(compactions.Count == 0
                ? "This conversation's history was never folded to fit the model's context window; " +
                  "every message below is the complete, original record."
                : $"This conversation's history was folded {compactions.Count} time(s) to fit the " +
                  "model's context window during later turns. Folding only ever shortened what later " +
                  "turns of Jooby could see internally -- it never removed or altered a message. " +
                  "Every message below is the complete, original record.");

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void AppendTranscript(StringBuilder sb, IReadOnlyList<AskJoobyMessage> entries,
            IReadOnlyList<AskJoobyFile> files)
        {
            var messages = entries.Where(e => e.EntryTypeId == AskJoobyMessageRepository.EntryTypeMessage).ToList();

            if (messages.Count == 0)
            {
                sb.AppendLine("_No messages recorded._");
                sb.AppendLine();

                return;
            }

            foreach (var entry in messages)
            {
                sb.AppendLine($"### {RoleName(entry.RoleId)} -- {FormatDate(entry.CreatedDate)}");
                sb.AppendLine();
                sb.AppendLine(string.IsNullOrEmpty(entry.Content) ? "_(empty)_" : entry.Content);
                sb.AppendLine();

                var attachments = files.Where(f => f.AskJoobyMessageId == entry.Id).ToList();

                if (attachments.Count > 0)
                {
                    sb.AppendLine("**Attachments:**");

                    foreach (var file in attachments)
                    {
                        sb.AppendLine($"- {FileLink(file)}");
                    }

                    sb.AppendLine();
                }
            }
        }

        private static string FileLink(AskJoobyFile file)
        {
            var name = string.IsNullOrEmpty(file.Name) ? $"file-{file.Id}" : file.Name;

            return $"[{name}]({FileDownloadPathPrefix}{file.Id})";
        }

        private static string FormatDate(DateTime? date)
        {
            return date.HasValue
                ? DateTime.SpecifyKind(date.Value, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                : "-";
        }

        private static string NullToDash(string? value)
        {
            return string.IsNullOrEmpty(value) ? "-" : value;
        }

        private static string RoleName(byte? role)
        {
            return role switch
            {
                (byte)AskJoobyMessageRole.User => "User",
                (byte)AskJoobyMessageRole.Assistant => "Assistant",
                (byte)AskJoobyMessageRole.System => "System",
                _ => "-"
            };
        }
    }
}
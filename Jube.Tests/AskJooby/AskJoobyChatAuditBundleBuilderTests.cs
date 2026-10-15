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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository.Models;
using Jube.Data.Repository.Models.AskJoobyDataCache;
using Jube.Service.AskJoobyConversation;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AskJoobyChatAuditBundleBuilderTests
    {
        private static readonly DateTime baseTime = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        private static readonly Guid conversationGuid = new("11111111-1111-1111-1111-111111111111");

        private static AskJoobyConversation Conversation()
        {
            return new AskJoobyConversation
            {
                Id = 1, Guid = conversationGuid, Title = "Test conversation", CreatedDate = baseTime,
                CreatedUser = "tester"
            };
        }

        private static string BuildMarkdownWithToolCall(AskJoobyToolCall toolCall)
        {
            var run = new AskJoobyRun
            {
                Id = 10, AskJoobyConversationId = 1, ParentAskJoobyRunId = null, AgentType = "worker", Depth = 0,
                StatusId = (byte)AskJoobyRunStatus.Completed, CreatedDate = baseTime, CreatedUser = "tester"
            };

            return AskJoobyChatAuditMarkdownBuilder.Build(
                Conversation(), entries: [], runs: [run], toolCalls: [toolCall], modelCalls: [], memories: [],
                dataCacheEntries: [], sqlLog: [], files: [],
                compactions: [], routings: [], workflowSteps: [], agentGuardRejections: [], webSearchCalls: [],
                openCorporatesCalls: [], generatedAtUtc: baseTime, generatedByUser: "tester");
        }

        [Fact]
        public void AToolCallsFullResultIsFiledNotInlinedButIsStillReachableFromTheLog()
        {
            var bigResult = string.Concat(Enumerable.Repeat("row-of-data;", 200));

            var toolCall = new AskJoobyToolCall
            {
                Id = 42, AskJoobyRunId = 10, ToolName = "run_sql_query", Arguments = "{\"sql\":\"select 1\"}",
                Result = bigResult, StatusId = (byte)AskJoobyToolCallStatus.Completed, DurationMs = 12,
                CreatedDate = baseTime
            };

            var markdown = BuildMarkdownWithToolCall(toolCall);

            var files = AskJoobyChatAuditBundleBuilder.BuildArtifactFiles(
                [toolCall], [], [], [], []);

            var expectedPath = "tool-calls/00042-run-sql-query.json";

            markdown.Should().Contain(expectedPath, "the log must point at exactly the file the bundle writes");
            markdown.Should().NotContain(bigResult, "the full payload belongs in the sidecar file, not the log");

            var file = files.Should().ContainSingle(f => f.RelativePath == expectedPath).Subject;
            var fileText = Encoding.UTF8.GetString(file.Content);

            fileText.Should().Contain(bigResult, "the sidecar file is where the full result actually lives");
        }

        [Fact]
        public void ADataCacheTableOverTheRowLimitIsPreviewedInTheLogButFullInTheCsv()
        {
            var columns = JsonSerializer.Serialize(new[] { new { Name = "Id" }, new { Name = "Amount" } });

            var rows = Enumerable.Range(1, 25)
                .Select(i => new Dictionary<string, object> { ["Id"] = i, ["Amount"] = i * 10 })
                .ToList();

            var dataJson = JsonSerializer.Serialize(rows);

            var entry = new AskJoobyDataCacheListItemJsonDto
            {
                Id = 7, PointerKey = "ptr-abc", Label = "Large Transfers", RowCount = rows.Count,
                ColumnsJson = columns, DataJson = dataJson, CreatedDate = baseTime
            };

            var markdown = AskJoobyChatAuditMarkdownBuilder.Build(
                Conversation(), entries: [], runs: [], toolCalls: [], modelCalls: [], memories: [],
                dataCacheEntries: [entry], sqlLog: [], files: [], compactions: [], routings: [], workflowSteps: [],
                agentGuardRejections: [], webSearchCalls: [], openCorporatesCalls: [], generatedAtUtc: baseTime,
                generatedByUser: "tester");

            var files = AskJoobyChatAuditBundleBuilder.BuildArtifactFiles(
                [], [entry], [], [], []);

            var expectedPath = "data-cache/00007-Large-Transfers.csv";

            markdown.Should().Contain(expectedPath);
            markdown.Should().Contain("showing first 10 of 25 rows");
            markdown.Should().NotContain("250", "row 25's amount must not have leaked into the truncated preview");

            var file = files.Should().ContainSingle(f => f.RelativePath == expectedPath).Subject;
            var csv = Encoding.UTF8.GetString(file.Content);
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            lines.Should().HaveCount(26, "header row plus all 25 data rows, not just the previewed 10");

            csv.Should().Contain("250", "the last row's full amount must be present in the CSV even though " +
                                        "it was trimmed from the log preview");
        }

        [Fact]
        public void TheManifestIndexesEveryArtifactByItsActualPath()
        {
            var toolCall = new AskJoobyToolCall
            {
                Id = 3, AskJoobyRunId = 10, ToolName = "search_rag", Arguments = "{}", Result = "ok",
                StatusId = (byte)AskJoobyToolCallStatus.Completed, DurationMs = 5, CreatedDate = baseTime
            };

            var manifestBytes = AskJoobyChatAuditBundleBuilder.BuildManifest(
                Conversation(), baseTime, "tester", [toolCall], [],
                [], [], []);

            using var document = JsonDocument.Parse(manifestBytes);
            var artifacts = document.RootElement.GetProperty("artifacts");

            artifacts.GetArrayLength().Should().Be(1);

            var artifact = artifacts[0];
            artifact.GetProperty("kind").GetString().Should().Be("tool-call");
            artifact.GetProperty("path").GetString().Should().Be("tool-calls/00003-search-rag.json");
            artifact.GetProperty("toolName").GetString().Should().Be("search_rag");
        }
    }
}
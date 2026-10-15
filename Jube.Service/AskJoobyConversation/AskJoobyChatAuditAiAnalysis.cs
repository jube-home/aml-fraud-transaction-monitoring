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
using Jube.AskJooby.Agents.Basic;
using Jube.Data.Poco;
using log4net;
using Microsoft.Extensions.AI;

namespace Jube.Service.AskJoobyConversation
{
    internal sealed record AiAnalysisOutcome(bool Succeeded, string? Text, string? FailureReason)
    {
        internal static AiAnalysisOutcome Ok(string text)
        {
            return new AiAnalysisOutcome(true, text, null);
        }

        internal static AiAnalysisOutcome Failed(string reason)
        {
            return new AiAnalysisOutcome(false, null, reason);
        }
    }

    internal static class AskJoobyChatAuditAiAnalysis
    {
        internal static string BuildPrompt(
            string conversationTitle,
            IReadOnlyList<AskJoobyRun> runs,
            byte[] healthJson,
            byte[] ragGapsJson,
            byte[] ragPerformanceJson,
            byte[] contextEfficiencyJson,
            string? orchestrationCode,
            string? agentCode)
        {
            var agentTypes = runs.Select(r => r.AgentType).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct();
            var grounding = AgentGroundingText.Collect(agentTypes);

            var sb = new StringBuilder();

            sb.AppendLine("You are reviewing AskJooby's own operational telemetry for one support " +
                          "conversation, already reduced to four rule-based reports below -- you are not " +
                          "being given the transcript, only what already fired. Diagnose what actually went " +
                          "wrong, tying each claim to a specific number or flagged item in the data; never " +
                          "invent a detail that isn't in front of you, and say plainly when the data doesn't " +
                          "support a conclusion either way. The real question in every case is what should " +
                          "have happened given each involved agent's own instructions and tools (below), " +
                          "contrasted with what the reports show actually happened. When the source code below " +
                          "covers the failure, name the actual file, method, or wiring responsible and propose " +
                          "a concrete fix grounded in that code -- a described mechanism beats a described " +
                          "symptom. Write for an engineer who will go fix the wiring, not for the person who " +
                          "had the conversation. Keep it to a few short paragraphs plus, if warranted, a short " +
                          "bulleted list of concrete recommendations. Do not quote any agent's instructions " +
                          "below verbatim -- they are grounding for your own judgement about what each agent " +
                          "was supposed to do, not something to repeat back. The code sections, by contrast, " +
                          "are there to be cited and quoted precisely -- they are the platform's real source, " +
                          "retrieved from its indexed corpus, not paraphrased documentation.");

            sb.AppendLine();
            sb.AppendLine($"Conversation: {conversationTitle}");
            sb.AppendLine();
            sb.AppendLine("## Health Signals (tool-calling and agent-hierarchy wiring)");
            sb.AppendLine(Encoding.UTF8.GetString(healthJson));
            sb.AppendLine();
            sb.AppendLine("## RAG & Tooling Gap Analysis (documentation/corpus coverage)");
            sb.AppendLine(Encoding.UTF8.GetString(ragGapsJson));
            sb.AppendLine();
            sb.AppendLine("## RAG Performance (response size and duration)");
            sb.AppendLine(Encoding.UTF8.GetString(ragPerformanceJson));
            sb.AppendLine();
            sb.AppendLine("## Context Efficiency (model-call and tool-result sizes, compaction)");
            sb.AppendLine(Encoding.UTF8.GetString(contextEfficiencyJson));
            sb.AppendLine();

            if (grounding.Count > 0)
            {
                sb.AppendLine("## What each agent that appeared in this conversation was told to do");
                sb.AppendLine();

                foreach (var (displayName, instructions) in grounding)
                {
                    sb.AppendLine($"### {displayName}");
                    sb.AppendLine(instructions);
                    sb.AppendLine();
                }
            }

            if (!string.IsNullOrWhiteSpace(orchestrationCode))
            {
                sb.AppendLine(orchestrationCode);
            }

            if (!string.IsNullOrWhiteSpace(agentCode))
            {
                sb.AppendLine(agentCode);
            }

            return sb.ToString();
        }

        internal static async Task<AiAnalysisOutcome> AnalyzeAsync(IChatClient chatClient, string prompt,
            int timeoutSeconds, ChatOptions? options, ILog log, CancellationToken token)
        {
            try
            {
                using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutSource.Token);

                var response = await chatClient
                    .GetResponseAsync([new ChatMessage(ChatRole.User, prompt)], options, linked.Token)
                    .ConfigureAwait(false);

                var text = response.Text.Trim();

                return string.IsNullOrWhiteSpace(text)
                    ? AiAnalysisOutcome.Failed("The model returned an empty response.")
                    : AiAnalysisOutcome.Ok(text);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"AskJoobyChatAudit: AI analysis timed out after {timeoutSeconds}s.");
                }

                return AiAnalysisOutcome.Failed(
                    $"Timed out after {timeoutSeconds}s waiting for a response from the model.");
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                log.Warn("AskJoobyChatAudit: AI analysis failed.", ex);

                return AiAnalysisOutcome.Failed($"{ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
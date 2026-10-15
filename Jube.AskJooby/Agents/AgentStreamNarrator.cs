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

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jube.AskJooby.Agents.Basic;
using Jube.AskJooby.Agents.Models;
using Jube.AskJooby.Observability;
using Jube.AskJooby.Observability.Models;
using Jube.AskJooby.Tools.Clarification;
using Microsoft.Extensions.AI;

namespace Jube.AskJooby.Agents
{
    public static class AgentStreamNarrator
    {
        private const int DetailArgumentCharacters = 600;
        private const int DetailResultCharacters = 1200;
        private const int DetailDelegationResultCharacters = 12000;
        private const int DelegationGistCharacters = 200;
        private const int FanOutGistCharacters = 90;
        private static readonly Regex searchRagCountPattern = new(@"^Found (\d+) Bite", RegexOptions.Compiled);

        private static readonly Regex recallHitPattern =
            new(@"^\[(User|System)\]", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex searchHistoryHitPattern =
            new(@"^\[", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex fanOutReportHeader =
            new(@"^\[(?<agentType>[A-Za-z0-9._-]+) #\d+\]", RegexOptions.Compiled | RegexOptions.Multiline);

        public static AgentStreamItem? DescribeStart(FunctionCallContent call)
        {
            if (call.Name is "AskClarifyingQuestion")
            {
                return null;
            }

            var args = call.Arguments ?? new Dictionary<string, object?>();

            string Arg(string name)
            {
                return args.TryGetValue(name, out var v) ? v?.ToString() ?? string.Empty : string.Empty;
            }

            if (call.Name == "SpawnAgent")
            {
                var agentType = Arg("agentType");
                var agent = DescribeAgent(agentType);

                return new AgentStreamItem(
                    AgentStreamItemKind.Delegation,
                    agent is null ? $"Calling in a Joobrik ({agentType})..." : $"Calling in {agent}...");
            }

            if (call.Name == "SpawnAgents")
            {
                var called = DescribeSpawnedAgents(args);

                return new AgentStreamItem(AgentStreamItemKind.Delegation,
                    called is null
                        ? "Herding the Joobriks and scaring a stampede..."
                        : $"Herding the Joobriks and scaring a stampede -- calling in {called}...");
            }

            var line = call.Name switch
            {
                "SearchRag" => $"Grazing the Pasture for \"{Arg("query")}\"...",
                "Remember" => "Slapping that down at the Salt Lick...",
                "Recall" => $"Enjoying the Salt Lick for \"{Arg("query")}\"...",
                "SearchHistory" => $"Tracing back Hoofprints for \"{Arg("query")}\"...",
                "SearchCompactedHistory" => $"Chewing the cud over \"{Arg("query")}\"...",
                _ => AgentToolNarration.DescribeStarting(call.Name) is { } starting
                    ? $"{starting}..."
                    : $"Calling {call.Name}..."
            };

            return new AgentStreamItem(AgentStreamItemKind.Step, line,
                AnswerPreserving: IsAnswerPreserving(call.Name), Icon: AgentToolIcons.Resolve(call.Name));
        }

        public static AgentStreamItem Describe(FunctionCallContent call, FunctionResultContent result)
        {
            var args = call.Arguments ?? new Dictionary<string, object?>();

            var surfaced = result.Result as ToolInvocationFailure;
            var failed = result.Exception is not null || surfaced is not null;
            var text = result.Result?.ToString() ?? string.Empty;

            string Arg(string name)
            {
                return args.TryGetValue(name, out var v) ? v?.ToString() ?? string.Empty : string.Empty;
            }

            var reason = result.Exception?.Message ?? surfaced?.Message ?? string.Empty;
            var detail = DescribeExchange(args, text, result, surfaced);

            if (call.Name == "AskClarifyingQuestion")
            {
                return DescribeAskClarifyingQuestion(failed, text, result);
            }

            if (call.Name == "SpawnAgent")
            {
                var agentType = Arg("agentType");
                var agent = DescribeAgent(agentType);
                var who = agent ?? $"The {agentType} Joobrik";

                detail = DescribeExchange(args, text, result, surfaced, DetailDelegationResultCharacters);

                return new AgentStreamItem(
                    AgentStreamItemKind.Delegation,
                    failed
                        ? $"{who} hit an error. ({reason})"
                        : DescribeDelegationReport(agentType, who, text),
                    Detail: detail);
            }

            if (call.Name == "SpawnAgents")
            {
                detail = DescribeExchange(args, text, result, surfaced, DetailDelegationResultCharacters);

                return new AgentStreamItem(
                    AgentStreamItemKind.Delegation,
                    failed
                        ? $"The Joobriks hit an error. ({reason})"
                        : DescribeFanOutReports(text),
                    Detail: detail);
            }

            var line = call.Name switch
            {
                "SearchRag" => DescribeSearchRag(Arg("query"), text),
                "Remember" => failed ? $"Couldn't remember that. ({reason})" : text,
                "Recall" => DescribeRecall(Arg("query"), text, failed, reason),
                "SearchHistory" => DescribeSearchHistory(Arg("query"), text, failed, reason),
                "SearchCompactedHistory" => failed
                    ? $"Could not chew the cud. ({reason})"
                    : text.StartsWith("Nothing compacted", StringComparison.Ordinal)
                        ? "Chewed the cud -- nothing compacted covers that."
                        : "Chewed the cud and brought the original back up.",
                _ => DescribeBoundTool(call.Name, failed, reason)
            };

            return new AgentStreamItem(AgentStreamItemKind.Step, line, Detail: detail,
                AnswerPreserving: IsAnswerPreserving(call.Name), Icon: AgentToolIcons.Resolve(call.Name));
        }

        public static string DescribeAgentLabel(string agentType)
        {
            if (LookUpDisplayName(agentType) is { } displayName)
            {
                return displayName;
            }

            return DescribeOwningDomain(agentType) ?? agentType;
        }

        public static string DescribeSpawnLabel(string agentType, string? spawnerAgentType, int? ordinal = null)
        {
            var label = DescribeAgentLabel(agentType);

            if (string.Equals(agentType, spawnerAgentType, StringComparison.Ordinal))
            {
                return $"{label} strand {(ordinal ?? 0) + 2}";
            }

            return ordinal is null ? label : $"{label} #{ordinal + 1}";
        }

        public static string DescribeRoutingAnnouncement(string agentType)
        {
            return $"Passing the yoke to {DescribeAgent(agentType) ?? DescribeAgentLabel(agentType)}...";
        }

        public static string DescribeRoutingDetail(string agentType, AgentRoutingStage stage, double? score,
            long durationMs)
        {
            var name = DescribeAgent(agentType) ?? DescribeAgentLabel(agentType);
            var builder = new StringBuilder();
            builder.Append("**").Append(name).Append("** (`").Append(agentType).AppendLine("`)").AppendLine();
            builder.Append("- Decided by: ").AppendLine(DescribeRoutingStage(stage));

            if (score is { } confidence)
            {
                builder.Append("- Confidence: ")
                    .AppendLine(confidence.ToString("0.00", CultureInfo.InvariantCulture));
            }

            builder.Append("- Took: ").Append(durationMs.ToString(CultureInfo.InvariantCulture))
                .AppendLine("ms");

            if (AgentDefinitions.All.TryGetValue(agentType, out var definition)
                && !string.IsNullOrWhiteSpace(definition.DelegationSummary))
            {
                builder.AppendLine().AppendLine(definition.DelegationSummary);
            }

            return builder.ToString().TrimEnd();
        }

        public static string DescribeSpawnAnnouncement(string agentType, string? spawnerAgentType,
            int? ordinal = null)
        {
            if (string.Equals(agentType, spawnerAgentType, StringComparison.Ordinal))
            {
                return $"{DescribeAgentLabel(agentType)} opening strand {(ordinal ?? 0) + 2} of its thinking...";
            }

            return $"{DescribeAgentLabel(agentType)} picking up the yoke...";
        }

        private static string? DescribeExchange(IDictionary<string, object?> args, string text,
            FunctionResultContent result, ToolInvocationFailure? surfaced,
            int resultCharacters = DetailResultCharacters)
        {
            var builder = new StringBuilder();

            if (args.Count > 0)
            {
                builder.Append("Passed: ");
                builder.Append(Shorten(string.Join(", ", args.Select(Pair)), DetailArgumentCharacters));
            }

            var broke = result.Exception is not null || surfaced is not null;
            var returned = result.Exception?.Message ?? surfaced?.Message ?? text;

            if (!string.IsNullOrWhiteSpace(returned))
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(broke ? "Failed: " : "Returned: ");
                builder.Append(Shorten(returned.Trim(), resultCharacters));
            }

            return builder.Length == 0 ? null : builder.ToString();
        }

        private static bool IsAnswerPreserving(string toolName)
        {
            return toolName == "Remember";
        }

        private static string Pair(KeyValuePair<string, object?> argument)
        {
            var value = argument.Value?.ToString() ?? "null";

            return $"{argument.Key}: {Shorten(value.Replace('\n', ' ').Trim(), 160)}";
        }

        private static string DescribeDelegationReport(string agentType, string who, string report)
        {
            if (LookUpCompletionWord(agentType) is { } word)
            {
                return $"{who} has completed: {word}.";
            }

            var gist = Gist(report, DelegationGistCharacters);

            return string.IsNullOrEmpty(gist) ? $"{who} reported back." : $"{who} reported back: {gist}";
        }

        private static string DescribeFanOutReports(string report)
        {
            var headers = fanOutReportHeader.Matches(report);

            if (headers.Count == 0)
            {
                var whole = Gist(report, DelegationGistCharacters);

                return string.IsNullOrEmpty(whole)
                    ? "The Joobriks reported back."
                    : $"The Joobriks reported back: {whole}";
            }

            var parts = new List<string>();

            for (var index = 0; index < headers.Count; index++)
            {
                var start = headers[index].Index + headers[index].Length;
                var end = index + 1 < headers.Count ? headers[index + 1].Index : report.Length;
                var agentType = headers[index].Groups["agentType"].Value;

                if (LookUpCompletionWord(agentType) is { } word)
                {
                    parts.Add($"{DescribeAgentLabel(agentType)}: {word}");
                    continue;
                }

                var gist = Gist(report[start..end], FanOutGistCharacters);

                parts.Add(string.IsNullOrEmpty(gist)
                    ? $"{DescribeAgentLabel(agentType)} said nothing"
                    : $"{DescribeAgentLabel(agentType)}: {gist}");
            }

            return "The Joobriks have completed -- " + string.Join(" / ", parts);
        }

        private static string Gist(string text, int limit)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim().TrimStart('#', '*', '-', '>', ' ').Trim();

                if (line.Length == 0 || line == "---")
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(line);

                if (builder.Length >= limit)
                {
                    break;
                }
            }

            return Shorten(builder.ToString(), limit);
        }

        private static string Shorten(string text, int limit)
        {
            if (text.Length <= limit)
            {
                return text;
            }

            var cut = text[..limit];
            var lastSpace = cut.LastIndexOf(' ');

            return (lastSpace > limit / 2 ? cut[..lastSpace] : cut).TrimEnd(',', ';', ':', '.', ' ') + "...";
        }

        private static string DescribeRoutingStage(AgentRoutingStage stage)
        {
            return stage switch
            {
                AgentRoutingStage.Pattern => "a wording rule, matched outright",
                AgentRoutingStage.Continuity => "continuity -- the workflow this conversation is in is unfinished, "
                                                + "so the turn stayed with the agent already carrying it",
                AgentRoutingStage.Embedding => "closest by meaning, against the routing exemplars",
                AgentRoutingStage.Model => "a model, asked to choose between the candidates",
                AgentRoutingStage.Default => "nothing matched, so the generalist kept it",
                AgentRoutingStage.NoCandidates => "no specialist was available to the caller's permissions",
                _ => stage.ToString()
            };
        }

        private static string? DescribeOwningDomain(string agentType)
        {
            return AgentDefinitions.HierarchyDomainOf.ContainsKey(agentType)
                ? PrettifyAgentType(agentType)
                : null;
        }

        private static string DescribeBoundTool(string toolName, bool failed, string reason)
        {
            if (failed)
            {
                return AgentToolNarration.DescribeStarting(toolName) is { } starting
                    ? $"{starting} hit an error. ({reason})"
                    : $"{toolName} hit an error. ({reason})";
            }

            return AgentToolNarration.DescribeFinished(toolName) is { } finished
                ? $"{finished}."
                : $"Called {toolName}.";
        }

        private static string? DescribeAgent(string agentType)
        {
            return LookUpDisplayName(agentType) is { } displayName
                ? $"{displayName} from {PrettifyAgentType(agentType)}"
                : DescribeOwningDomain(agentType);
        }

        private static string? LookUpDisplayName(string agentType)
        {
            return AgentDefinitions.All.TryGetValue(agentType, out var definition) ? definition.DisplayName : null;
        }

        private static string? LookUpCompletionWord(string agentType)
        {
            return AgentDefinitions.All.TryGetValue(agentType, out var definition) ? definition.CompletionWord : null;
        }

        private static string PrettifyAgentType(string agentType)
        {
            var builder = new StringBuilder(agentType.Length);
            var capitalise = true;

            foreach (var character in agentType)
            {
                if (character is '-' or '.' or '_')
                {
                    builder.Append(' ');
                    capitalise = true;
                    continue;
                }

                builder.Append(capitalise ? char.ToUpperInvariant(character) : character);
                capitalise = false;
            }

            return builder.ToString();
        }

        private static string DescribeSearchRag(string query, string text)
        {
            var match = searchRagCountPattern.Match(text);

            if (!match.Success)
            {
                return $"Grazed the Pasture for \"{query}\" -- no Bites found.";
            }

            var count = match.Groups[1].Value;

            return $"Grazed the Pasture for \"{query}\" -- returned {count} Bite{(count == "1" ? "" : "s")}.";
        }

        private static string DescribeRecall(string query, string text, bool failed, string reason)
        {
            if (failed)
            {
                return $"Couldn't enjoy the Salt Lick for \"{query}\". ({reason})";
            }

            var count = recallHitPattern.Matches(text).Count;

            return count == 0
                ? $"Enjoyed the Salt Lick for \"{query}\" -- nothing matched."
                : $"Enjoyed the Salt Lick for \"{query}\" -- found {count} " +
                  $"{(count == 1 ? "memory" : "memories")}.";
        }

        private static string? DescribeSpawnedAgents(IDictionary<string, object?> args)
        {
            if (!args.TryGetValue("subTasks", out var raw) || raw is null)
            {
                return null;
            }

            var types = new List<string>();

            foreach (var match in Regex.Matches(raw.ToString() ?? string.Empty,
                         "\"?[Aa]gentType\"?\\s*[:=]\\s*\"?([A-Za-z0-9._-]+)").Cast<Match>())
            {
                var described = DescribeAgent(match.Groups[1].Value) ?? match.Groups[1].Value;

                if (!types.Contains(described))
                {
                    types.Add(described);
                }
            }

            return types.Count == 0 ? null : string.Join(", ", types);
        }

        private static AgentStreamItem DescribeAskClarifyingQuestion(
            bool failed, string text, FunctionResultContent result)
        {
            if (failed)
            {
                return new AgentStreamItem(
                    AgentStreamItemKind.Step, $"AskClarifyingQuestion hit an error. ({result.Exception!.Message})",
                    Icon: AgentToolIcons.Resolve("AskClarifyingQuestion"));
            }

            AskClarifyingQuestionPayload? payload = null;

            try
            {
                payload = JsonSerializer.Deserialize<AskClarifyingQuestionPayload>(text);
            }
            catch (JsonException)
            {
            }

            if (payload is { Options.Count: > 0 } && !string.IsNullOrWhiteSpace(payload.Question))
            {
                return new AgentStreamItem(AgentStreamItemKind.Clarification, payload.Question,
                    Options: payload.Options, AllowFreeText: payload.AllowFreeText);
            }

            return new AgentStreamItem(AgentStreamItemKind.Step, "Called AskClarifyingQuestion.",
                Icon: AgentToolIcons.Resolve("AskClarifyingQuestion"));
        }

        private static string DescribeSearchHistory(string query, string text, bool failed, string reason)
        {
            if (failed)
            {
                return $"Couldn't trace back Hoofprints for \"{query}\". ({reason})";
            }

            var count = searchHistoryHitPattern.Matches(text).Count;

            return count == 0
                ? $"Traced back Hoofprints for \"{query}\" -- nothing matched."
                : $"Traced back Hoofprints for \"{query}\" -- found {count} " +
                  $"{(count == 1 ? "print" : "prints")}.";
        }
    }
}
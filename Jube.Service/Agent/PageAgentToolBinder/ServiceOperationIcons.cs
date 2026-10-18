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

using Jube.AskJooby.Agents;
using Jube.AskJooby.Agents.Models;

namespace Jube.Service.Agent.PageAgentToolBinder
{
    internal static class ServiceOperationIcons
    {
        private const string TreeChildrenPrefix = "TreeChildren";

        private static readonly (Func<string, bool> Matches, AgentToolIcon Icon)[] rules =
        [
            (name => name.Contains("Warning", StringComparison.Ordinal) || StartsWithAny(name, "WafAttack"),
                AgentToolIcon.TriangleAlert),
            (name => StartsWithAny(name, "OpenTelemetry", "Otlp", "ContainerLogEntry", "PostgresLogEntry",
                "ApplicationLogEntry", "ActivationWatcher"), AgentToolIcon.ScrollText),
            (name => name.EndsWith("Metric", StringComparison.Ordinal)
                     || name.Contains("PerformanceCounter", StringComparison.Ordinal)
                     || name.Contains("ProcessingCounter", StringComparison.Ordinal)
                     || name.Contains("ResponseTimePipelineCounter", StringComparison.Ordinal)
                     || name.Contains("AsynchronousQueueBalance", StringComparison.Ordinal)
                     || StartsWithAny(name, "RedisCallCounter", "HttpProcessingCounter"), AgentToolIcon.Gauge),
            (name => StartsWithAny(name, "Redis", "Postgres", "Patroni", "Etcd", "HAProxy", "Docker",
                "OverlayNetwork", "DotNetRuntime", "CaptureQueue", "HttpProcessing"), AgentToolIcon.Server),
            (name => StartsWithAny(name, "AskJooby"), AgentToolIcon.Bot),
            (name => StartsWithAny(name, "Case", "SessionCase"), AgentToolIcon.Briefcase),
            (name => StartsWithAny(name, "Preservation", "Archive"), AgentToolIcon.Archive),
            (name => StartsWithAny(name, "User", "Role", "Tenant", "PermissionSpecification"), AgentToolIcon.KeyRound),
            (name => StartsWithAny(name, "Visualisation"), AgentToolIcon.LayoutDashboard),
            (name => StartsWithAny(name, "AdvancedAnalytics", "Exhaustive")
                     || name.Contains("Backtest", StringComparison.Ordinal)
                     || name.Contains("HistorySimulation", StringComparison.Ordinal)
                     || name.Contains("InvocationContext", StringComparison.Ordinal)
                     || name.Contains("Integrity", StringComparison.Ordinal)
                     || name.Contains("Sample", StringComparison.Ordinal), AgentToolIcon.FlaskConical),
            (name => StartsWithAny(name, "RuleVocabulary", "QueryBuilderOperators", "ParserParse", "Icons",
                "Completions"), AgentToolIcon.BookOpenText),
            (name => StartsWithAny(name, "EntityAnalysis", "SanctionEntrySource", "Sanctions", "Adaptation",
                    "Dictionary", "InlineScript", "InlineFunction", "Reprocessing", "RequestXPath", "GatewayRule",
                    "AbstractionRule", "AbstractionCalculation", "ActivationRule", "Tag", "TTLCounter", "List"),
                AgentToolIcon.SlidersHorizontal),
            (name => name.Length == 0, AgentToolIcon.ListTree)
        ];

        internal static void Register(IEnumerable<string> serviceOperationNames)
        {
            foreach (var name in serviceOperationNames)
            {
                AgentToolIcons.Register(name, For(name));
            }
        }

        internal static AgentToolIcon For(string serviceOperationName)
        {
            var name = serviceOperationName.StartsWith(TreeChildrenPrefix, StringComparison.Ordinal)
                ? serviceOperationName[TreeChildrenPrefix.Length..]
                : serviceOperationName;

            foreach (var (matches, icon) in rules)
            {
                if (matches(name))
                {
                    return icon;
                }
            }

            return AgentToolIcons.Default;
        }

        private static bool StartsWithAny(string name, params string[] prefixes)
        {
            return prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));
        }
    }
}
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

using System.Threading.Channels;
using Jube.AskJooby.Agents.Basic.Models;
using Jube.AskJooby.Tools.Charting.Interfaces;
using Jube.AskJooby.Tools.Compaction.Interfaces;
using Jube.AskJooby.Tools.Delegation.Interfaces;
using Jube.AskJooby.Tools.DemoDataset.Interfaces;
using Jube.AskJooby.Tools.Export.Interfaces;
using Jube.AskJooby.Guidance.Interfaces;
using Jube.AskJooby.Observability.Interfaces;
using Jube.AskJooby.Prompts.Models;
using Jube.AskJooby.Security.Interfaces;
using Jube.AskJooby.Security.Models;
using Jube.AskJooby.Tools.OpenCorporates.Interfaces;
using Jube.AskJooby.Tools.Rag.Interfaces;
using Jube.AskJooby.Tools.WebSearch.Interfaces;
using Jube.AskJooby.Tools.WebSearch.Models;
using Jube.AskJooby.Tools.Workflows.Interfaces;
using Microsoft.Extensions.Logging;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.AskJooby.Agents.Models
{
    public sealed record RunContext(
        IRunRecorder? Recorder,
        string? ConversationId,
        string? ParentRunId,
        Func<int[], bool> HasPermission,
        bool EnableThinking = false,
        int MaxDepth = 3,
        int RagTopK = 5,
        double RagMinScore = 0,
        float? Temperature = null,
        int? ThinkingEffort = null,
        ChannelWriter<AgentStreamItem>? DelegationChannel = null,
        int MaxFanOut = 8,
        int AgentIdleTimeoutSeconds = 120,
        bool AllowDelegation = true,
        ITokenBudget? TokenBudget = null,
        long AgentTokenBudget = 0,
        ITurnActivityLedger? TurnActivity = null,
        IRepeatedToolLedger? RepeatedTools = null,
        IAgentGuidanceExchange? GuidanceExchange = null,
        bool DisableThinkingExplicitly = true,
        int? MaxOutputTokens = null,
        bool RecordPromptPrefixHash = false,
        IIsolatedRunScopeFactory? IsolatedRunScopeFactory = null,
        IExportSink? ExportSink = null,
        IHandBackSignal? HandBack = null,
        IRetrievedBiteLedger? RetrievedBiteLedger = null,
        ICompactedHistoryClient? CompactedHistory = null,
        IAutoSnapshot? AutoSnapshot = null,
        IReadOnlyDictionary<string, IReadOnlyList<AgentToolDefinition>>? ServiceBoundTools = null,
        AgentToolDefinition? PreservationSnapshotTool = null,
        bool IsLandlord = false,
        IChartRenderer? ChartRenderer = null,
        IDemoDatasetProvider? DemoDatasets = null,
        IWorkflowProgressClient? WorkflowProgress = null,
        IWebSearchClient? WebSearch = null,
        int WebSearchMaxResults = 5,
        WebSearchDepth WebSearchDepth = WebSearchDepth.Advanced,
        WebSearchAnswerMode WebSearchAnswerMode = WebSearchAnswerMode.Advanced,
        WebSearchRawContentMode WebSearchRawContentMode = WebSearchRawContentMode.Markdown,
        IOpenCorporatesClient? OpenCorporates = null,
        int OpenCorporatesMaxResults = 5,
        IReadOnlyList<AgentErratum>? Errata = null,
        int? MaxIterationsPerRequest = null,
        ILoggerFactory? LoggerFactory = null,
        IAgentGuardRejectionLedger? GuardRejectionLedger = null,
        IReadOnlyList<AgentGuardRuleSetting>? AgentGuardRules = null,
        PageContext? PageContext = null)
    {
        public static readonly RunContext None = new(null, null, null, _ => false);

        public RunContext WithParent(string newParentRunId)
        {
            return this with { ParentRunId = newParentRunId };
        }
    }
}
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

using Jube.AskJooby.Agents.Basic;
using Jube.AskJooby.Agents.Basic.Models;
using Jube.AskJooby.Agents.Exceptions;
using Jube.AskJooby.Agents.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.Tools.Export.Interfaces;
using Jube.AskJooby.Guidance;
using Jube.AskJooby.Observability;
using Jube.AskJooby.Prompts;
using Jube.AskJooby.Reasoning;
using Jube.AskJooby.Security;
using Jube.AskJooby.Tools.Cache;
using Jube.AskJooby.Tools.Delegation;
using Jube.AskJooby.Tools.History.Interfaces;
using Jube.AskJooby.Tools.Memory.Interfaces;
using Jube.AskJooby.Tools.Rag.Interfaces;
using Jube.AskJooby.Tools.ScriptExecution.Interfaces;
using Jube.AskJooby.Tools.SqlQuery.Interfaces;
using Jube.AskJooby.Tools.SystemContext;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Jube.AskJooby.Agents
{
    public static class AgentFactory
    {
        public const string DefaultModel = "deepseek-flash";

        public static async Task<(AIAgent Agent, string RunId)> CreateAsync(
            string agentType, IChatClient chatClient, IRagClient rag, IMemoryClient memory,
            IConversationHistoryClient history, string task, int depth, RunContext context,
            ISqlQueryClient? sqlQuery = null, IToolResultCache? cache = null, IFileExportClient? export = null,
            IScriptExecutionClient? scriptExecution = null, CancellationToken token = default)
        {
            if (!AgentDefinitions.All.TryGetValue(agentType, out var definition))
            {
                throw new ArgumentException(
                    $"Unknown agent type '{agentType}'. Known types: {string.Join(", ", AgentDefinitions.All.Keys)}");
            }

            if (!AgentDefinitions.IsPermitted(definition.RequiredPermissions, context.HasPermission,
                    definition.RequiresLandlord, context.IsLandlord))
            {
                throw new AgentPermissionDeniedException(agentType);
            }

            var runId = context.Recorder is null
                ? Guid.NewGuid().ToString()
                : await context.Recorder.StartRunAsync(agentType, depth, context.ParentRunId, task, token)
                    .ConfigureAwait(false);

            var effectiveBudget = context.TokenBudget is null || context.AgentTokenBudget <= 0
                ? context.TokenBudget
                : new CompositeTokenBudget(context.TokenBudget,
                    new TokenBudget(context.AgentTokenBudget));

            var budgetedChatClient = effectiveBudget is null
                ? chatClient
                : new TokenBudgetChatClient(chatClient, effectiveBudget);

            var loggedChatClient = context.Recorder is null
                ? budgetedChatClient
                : new PersistedChatClient(budgetedChatClient, context.Recorder, runId,
                    context.RecordPromptPrefixHash);

            loggedChatClient = new ProviderMarkupChatClient(loggedChatClient);

            if (context.GuidanceExchange is not null && context.ConversationId is not null)
            {
                loggedChatClient = new GuidanceInjectingChatClient(
                    loggedChatClient, context.GuidanceExchange, context.ConversationId, runId,
                    agentType, context.DelegationChannel);
            }

            var childContext = context.WithParent(runId);

            var toolArgs = new AgentToolFactoryArgs(
                rag, memory, history, chatClient, depth, childContext, sqlQuery, cache, export,
                agentType, context.ChartRenderer, context.DemoDatasets, context.WorkflowProgress,
                scriptExecution);

            var toolDefinitions = definition.ToolsFactory(toolArgs)
                .Where(tool => AgentDefinitions.IsPermitted(tool.RequiredPermissions, context.HasPermission,
                    tool.RequiresLandlord, context.IsLandlord))
                .ToList();

            var systemContextTool = new SystemContextTool(context.PageContext);

            toolDefinitions.Add(new AgentToolDefinition(
                AIFunctionFactory.Create(systemContextTool.DescribeSystemContext), []));

            loggedChatClient = new ToolCallAndResultGuardChatClient(
                loggedChatClient, context.GuardRejectionLedger, context.IsLandlord, context.AgentGuardRules);

            if (definition.RequiresPreservationSnapshot && context.PreservationSnapshotTool is { } snapshotTool
                                                        && AgentDefinitions.IsPermitted(
                                                            snapshotTool.RequiredPermissions, context.HasPermission,
                                                            snapshotTool.RequiresLandlord, context.IsLandlord)
                                                        && toolDefinitions.All(tool =>
                                                            tool.Function.Name != snapshotTool.Function.Name))
            {
                toolDefinitions.Add(snapshotTool);
            }

            var tools = context.AutoSnapshot is { } autoSnapshot
                ? toolDefinitions.Select(tool => tool.RequiresSnapshot
                        ? new SnapshotOnWriteAiFunction(tool.Function, autoSnapshot, context.DelegationChannel,
                            AgentStreamNarrator.DescribeAgentLabel(agentType))
                        : tool.Function)
                    .ToList()
                : toolDefinitions.Select(tool => tool.Function).ToList();

            var canDelegate = context.AllowDelegation && depth < context.MaxDepth
                                                      && definition.AllowedDelegates?.Count != 0;

            IReadOnlyDictionary<string, AgentDefinition> availableSpecialists =
                new Dictionary<string, AgentDefinition>();

            if (cache is not null)
            {
                var cacheAssembledTableTool = new CacheAssembledTableTool(cache);
                tools.Add(AIFunctionFactory.Create(cacheAssembledTableTool.CacheAssembledTable));
            }

            if (context.AllowDelegation)
            {
                if (depth == 0 && context.TurnActivity is { } turnActivity)
                {
                    tools.Add(AIFunctionFactory.Create(new PlanTool(turnActivity).WritePlan));
                }
                else if (depth >= 1 && context.TurnActivity is { } stableTurn)
                {
                    var ownerStable = AgentDefinitions.HierarchyDomainOf.GetValueOrDefault(agentType, agentType);
                    tools.Add(AIFunctionFactory.Create(new PlanTool(stableTurn, ownerStable).WritePlan));
                }
            }

            if (canDelegate)
            {
                var candidates = AgentDefinitions.Available(context.HasPermission, context.IsLandlord)
                    .Where(pair => pair.Key != agentType);

                if (definition.AllowedDelegates is not null)
                {
                    candidates = candidates.Where(pair => definition.AllowedDelegates.Contains(pair.Key));
                }

                availableSpecialists = candidates.ToDictionary(pair => pair.Key, pair => pair.Value);

                var spawnAgentTool = new SpawnAgentTool(
                    chatClient, rag, memory, history, depth, childContext, sqlQuery, cache, export,
                    scriptExecution, agentType);

                tools.Add(AIFunctionFactory.Create(spawnAgentTool.SpawnAgent));
                tools.Add(AIFunctionFactory.Create(spawnAgentTool.SpawnAgents));
            }

            var reachedSpawnDepthCeiling = context.AllowDelegation && depth >= context.MaxDepth
                                                                   && definition.AllowedDelegates?.Count != 0;

            if (context.Recorder is not null)
            {
                tools = tools.Select(tool => (AIFunction)new PersistedAiFunction(tool, context.Recorder, runId))
                    .ToList();
            }

            if (effectiveBudget is not null)
            {
                tools = tools.Select(tool =>
                        (AIFunction)new BudgetedAiFunction(tool, effectiveBudget, context.TurnActivity))
                    .ToList();
            }

            if (context.RepeatedTools is not null)
            {
                tools = tools.Select(tool =>
                        (AIFunction)new RepeatSuppressingAiFunction(tool, context.RepeatedTools))
                    .ToList();
            }

            tools = tools.Select(tool => (AIFunction)new SurfacedFailureAiFunction(tool)).ToList();

            tools = tools.Select(tool => (AIFunction)new ProviderMarkupAiFunction(tool)).ToList();

            var specialistDescriptions = availableSpecialists.ToDictionary(
                pair => pair.Key, pair => DescribeSpecialist(pair.Value));

            var errata = context.Errata?
                .Where(erratum => erratum.Affects is null || erratum.Affects == agentType)
                .Select(erratum => erratum.Body)
                .ToList();

            var instructions =
                InstructionsBuilder.Build(definition.Instructions, depth, specialistDescriptions, errata,
                    reachedSpawnDepthCeiling ? context.MaxDepth : null, context.PageContext);

            var options = new HarnessAgentOptions
            {
                Name = agentType,
                HarnessInstructions = instructions,
                ChatOptions = new ChatOptions
                {
                    Tools = [.. tools],
                    Temperature = context.Temperature,

                    MaxOutputTokens = context.MaxOutputTokens,

                    RawRepresentationFactory = context.EnableThinking
                        ? _ => ThinkingOptions.RawOptions(context.ThinkingEffort)
                        : context.DisableThinkingExplicitly
                            ? _ => ThinkingOptions.DisabledOptions()
                            : null
                },
                DisableWebSearch = true,
                DisableFileMemory = true,
                DisableTodoProvider = true,
                DisableAgentModeProvider = true,
                MaximumIterationsPerRequest = context.MaxIterationsPerRequest
            };

            return (loggedChatClient.AsHarnessAgent(options, context.LoggerFactory), runId);
        }

        public static string DescribeSpecialist(AgentDefinition definition)
        {
            var summary = definition.DelegationSummary ?? definition.Instructions;

            return definition.AllowedDelegates is { Count: > 0 } ownDelegates
                ? $"{summary} Its own specialists: {string.Join(", ", ownDelegates)}."
                : summary;
        }
    }
}
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

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Jube.AskJooby.Agents;
using Jube.AskJooby.Agents.Basic;
using Jube.AskJooby.Agents.Basic.Models;
using Jube.AskJooby.Agents.Interfaces;
using Jube.AskJooby.Agents.Models;
using Jube.AskJooby.DataCache;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.Guidance.Interfaces;
using Jube.AskJooby.Memory;
using Jube.AskJooby.Observability;
using Jube.AskJooby.Observability.Interfaces;
using Jube.AskJooby.Observability.Models;
using Jube.AskJooby.Prompts.Models;
using Jube.AskJooby.Rag;
using Jube.AskJooby.Reasoning;
using Jube.AskJooby.Routing;
using Jube.AskJooby.Routing.Interfaces;
using Jube.AskJooby.Security;
using Jube.AskJooby.Security.Interfaces;
using Jube.AskJooby.Security.Models;
using Jube.AskJooby.Tools.Charting.Interfaces;
using Jube.AskJooby.Tools.Compaction.Interfaces;
using Jube.AskJooby.Tools.Delegation.Interfaces;
using Jube.AskJooby.Tools.DemoDataset.Interfaces;
using Jube.AskJooby.Tools.Export.Interfaces;
using Jube.AskJooby.Tools.History.Interfaces;
using Jube.AskJooby.Tools.Memory.Interfaces;
using Jube.AskJooby.Tools.OpenCorporates.Interfaces;
using Jube.AskJooby.Tools.Rag.Interfaces;
using Jube.AskJooby.Tools.ScriptExecution.Interfaces;
using Jube.AskJooby.Tools.SqlQuery.Interfaces;
using Jube.AskJooby.Tools.SystemContext;
using Jube.AskJooby.Tools.WebSearch.Interfaces;
using Jube.AskJooby.Tools.WebSearch.Models;
using Jube.AskJooby.Tools.Workflows.Interfaces;
using Jube.AskJooby.Validation;
using Jube.AskJooby.Validation.Models;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.GetAskJoobyConversationRetrievedDocsQuery;
using Jube.Data.Query.GetAskJoobyRagChunkByHashQuery;
using Jube.Data.Query.GetAskJoobyRagChunkByParentHashQuery;
using Jube.Data.Query.GetAskJoobyStartupStatsQuery;
using Jube.Data.Query.GetAskJoobyWorkflowProgressQuery;
using Jube.Data.Query.SearchAskJoobyCompactionQuery;
using Jube.Data.Query.SearchAskJoobyConversationHistoryQuery;
using Jube.Data.Query.SearchAskJoobyMemoryQuery;
using Jube.Data.Query.SearchAskJoobyRagChunkQuery;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Dto.AskJoobyConversation;
using Jube.Engine.AdvancedAnalytics.RScript;
using Jube.Resources;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation;
using Jube.Service.Agent.Models;
using Jube.Service.Agent.PageAgentToolBinder;
using Jube.Service.Agent.PageAgentToolBinder.Models;
using Jube.Service.Agent.RuleExtensionTools;
using Jube.Service.AskJoobyCharting;
using Jube.Service.AskJoobyConversation.Models;
using Jube.Service.AskJoobyDataCache;
using Jube.Service.AskJoobyExport;
using Jube.Service.AskJoobyMemory;
using Jube.Service.AskJoobyRagChunk;
using Jube.Service.AskJoobyRagChunk.Interfaces;
using Jube.Service.AskJoobyWorkflow;
using Jube.Service.DemoDataset;
using Jube.Service.DynamicSql;
using Jube.Service.Exceptions.AskJoobyConversation;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CrossCutScriptExecution;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Jube.Service.AskJoobyConversation
{
    using RunContext = RunContext;

    public sealed class AskJoobyConversationService
    {
        private const int SubAgentTimeoutSeconds = 90;
        private const string ValidationNoteMarkerStart = "\uE001ASKJOOBY_VALIDATION_NOTE\uE001";
        private const string ValidationNoteMarkerEnd = "\uE001/ASKJOOBY_VALIDATION_NOTE\uE001";

        private const string UnverifiedNotRefuted =
            "\n\nThis is unverified, not refuted -- it may well be correct. Do not withdraw or "
            + "contradict it on the strength of this note alone; a search that found nothing is the "
            + "docs being silent, which is also exactly what a search that simply missed looks like. "
            + "Confirm it against the docs or the platform before telling anyone it was wrong.";

        private const int RoutingContextTurns = 3;
        private const int RoutingContextTurnLength = 300;
        private const int SuggestionReplyCharacters = 1200;
        private const int CompactionTriggerMessageCount = 60;
        private const int CompactionTriggerCharacterCount = 80000;
        private static readonly int[] permissions = [AskJoobyPermissions.Access];

        private static readonly string validatorLabel =
            AgentDefinitions.All.TryGetValue("validator", out var validatorDefinition)
            && validatorDefinition.DisplayName is { } validatorName
                ? validatorName
                : "validator";

        private static readonly string[] cannedGreetingTemplates =
        [
            "I'm Jooby. {0} Bites in the Pasture, {1} Trails behind me, {2} Saltlicks laid down, " +
            "{3} specialist Joobriks on call, and {4} Yokes in the harness. What can I help you get sorted?",
            "Jooby here. The Pasture's holding {0} Bites, I've walked {1} Trails, kept {2} Saltlicks, " +
            "{3} Joobriks standing by, and {4} Yokes ready to reach for. What's on your plate?",
            "Right, I'm Jooby -- {0} Bites in the Pasture, {1} Trails trodden, {2} Saltlicks to draw on, " +
            "{3} Joobriks I can call in, {4} Yokes at hand. What are we looking at today?",
            "I'm Jooby, and the numbers are steady: {0} Bites, {1} Trails, {2} Saltlicks, {3} Joobriks " +
            "on call, {4} Yokes to hand. Tell me what's in front of you and we'll get into it.",
            "Jooby speaking. {0} Bites sit in the Pasture, {1} Trails are behind me, {2} facts at the " +
            "Salt Lick, {3} Joobriks ready to be sent, {4} Yokes in reach. What do you need first?",
            "I'm Jooby -- fraud and AML, this platform, no one else. {0} Bites to search, {1} Trails " +
            "walked, {2} Saltlicks kept, {3} Joobriks I can call on, {4} Yokes to hand. What's the " +
            "question?",
            "Jooby, at your service. The Pasture's got {0} Bites, I've covered {1} Trails, {2} Saltlicks " +
            "are on file, {3} Joobriks are mine to send, and {4} Yokes are in the harness. Where do you " +
            "want to start?",
            "I'm Jooby. Quick tally before we start: {0} Bites in the Pasture, {1} Trails behind, {2} at " +
            "the Salt Lick, {3} Joobriks on standby, {4} Yokes ready. What's up?",
            "Jooby here, ready when you are. {0} Bites in the Pasture, {1} Trails to my name, {2} " +
            "Saltlicks kept, {3} Joobriks I can call on, {4} Yokes ready to reach for. What are you " +
            "working on?",
            "I'm Jooby -- this platform's fraud and AML assistant, not anyone standing in for me. {0} " +
            "Bites, {1} Trails, {2} Saltlicks, {3} Joobriks on call, {4} Yokes ready, and counting. What " +
            "brings you in?",
            "Jooby. The Pasture holds {0} Bites, {1} Trails sit behind me, {2} Saltlicks are laid down, " +
            "{3} Joobriks at battalion strength ready to stampede charge whatever this turns out to be, " +
            "and {4} Yokes in the harness. What needs doing?",
            "I'm Jooby, running steady: {0} Bites in the Pasture, {1} Trails walked, {2} Saltlicks kept " +
            "for later, {3} Joobriks standing ready, {4} Yokes in reach. What's first?",
            "Jooby here. {0} Bites ready in the Pasture, {1} Trails behind us, {2} at the Salt Lick, {3} " +
            "Joobriks I can send out, {4} Yokes ready to pull. What do you want to tackle?",
            "I'm Jooby -- {0} Bites in the Pasture, {1} Trails trodden, {2} Saltlicks banked, {3} " +
            "Joobriks on call, {4} Yokes ready when it counts -- and beyond that toolbox, I can run " +
            "real SQL against Jube's own database, parsed and rewritten to what you're allowed to " +
            "see, when that's not enough. What's on your mind?",
            "Right then, I'm Jooby. {0} Bites, {1} Trails, {2} Saltlicks, {3} Joobriks on call, {4} " +
            "Yokes in the harness -- all steady. What do you need looked at?",
            "I'm Jooby -- {0} Bites in the Pasture, {1} Trails trodden, {2} Saltlicks banked, {3} " +
            "Joobriks on call, {4} Yokes ready -- and when none of them quite fits, I can run a " +
            "little R program of my own, in a locked-down box with no reach past it. What's the " +
            "question?"
        ];

        private readonly long agentTokenBudget;
        private readonly AskJoobyAgentGuardRejectionRepository guardRejectionRepository;
        private readonly AskJoobyAgentGuardRuleRepository agentGuardRuleRepository;
        private readonly ILog auditLog;
        private readonly IChartRenderer chartRenderer = new AskJoobyChartRenderer();
        private readonly IChatClient chatClient;
        private readonly string? connectionString;
        private readonly AskJoobyConversationRepository conversationRepository;
        private readonly DbContext dbContext;
        private readonly IDemoDatasetProvider demoDatasets = new AskJoobyDemoDatasetProvider();
        private readonly DynamicEnvironment.DynamicEnvironment? dynamicEnvironment;
        private readonly bool embedAllMessages;
        private readonly IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator;
        private readonly bool enableSuggestions;
        private readonly bool enableValidation;
        private readonly AskJoobyErrataRepository errataRepository;
        private readonly AskJoobyFileRepository fileRepository;
        private readonly IAgentGuidanceExchange? guidanceExchange;
        private readonly IAskJoobyTurnTracker? turnTracker;
        private readonly SearchAskJoobyConversationHistoryQuery historyQuery;
        private readonly ILog log;
        private readonly ILoggerFactory loggerFactory = new Log4NetLoggerFactory();
        private readonly int maxConcurrentToolCalls;
        private readonly int maxFanOut;
        private readonly int maxSearchesPerTurn;
        private readonly int maxToolResultCharacters;
        private readonly double memoryDecayHalfLifeDays;
        private readonly SearchAskJoobyMemoryQuery memoryQuery;
        private readonly AskJoobyMemoryRepository memoryRepository;
        private readonly AskJoobyMessageRepository messageRepository;
        private readonly AskJoobyModelCallRepository modelCallRepository;
        private readonly AskJoobyEmbeddingCallRepository embeddingCallRepository;
        private readonly AskJoobyRerankCallRepository rerankCallRepository;
        private readonly AskJoobyWebSearchCallRepository webSearchCallRepository;
        private readonly AskJoobyOpenCorporatesCallRepository openCorporatesCallRepository;
        private readonly AskJoobyEndpointFeeRepository endpointFeeRepository;
        private readonly AskJoobyRerankFeeRepository rerankFeeRepository;
        private readonly AskJoobyTurnCostRepository turnCostRepository;
        private readonly PermissionValidation permissionValidation;
        private readonly int promotionBudgetCharacters;
        private readonly int ragCandidateCount;
        private readonly int ragMaxCandidateCharacters;
        private readonly double ragMinScore;
        private readonly SearchAskJoobyRagChunkQuery ragQuery;
        private readonly IRagReranker? ragReranker;
        private readonly int ragTopK;
        private readonly IRepeatedToolLedger repeatedTools = new RepeatedToolLedger();
        private readonly double rerankMinScore;
        private readonly GetAskJoobyConversationRetrievedDocsQuery retrievedDocsQuery;
        private readonly IAgentRoutingLedger? routingLedger;
        private readonly AskJoobyRoutingRepository routingRepository;
        private readonly AskJoobyRunRepository runRepository;
        private readonly Lock serviceBoundToolsGate = new();
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly bool serviceToolsReadOnly;
        private readonly GetAskJoobyStartupStatsQuery statsQuery;
        private readonly IStringLocalizerFactory stringLocalizerFactory;
        private readonly IStringLocalizer strings;
        private readonly float? temperature;
        private readonly int tenantRegistryId;
        private readonly ITokenBudget? tokenBudget;
        private readonly AskJoobyToolCallRepository toolCallRepository;
        private readonly ITurnActivityLedger turnActivity;
        private readonly string userName;
        private readonly IChatClient validationChatClient;
        private readonly double validationRagMinScore;
        private readonly float? validationTemperature;
        private readonly IWebSearchClient? webSearchClient;
        private readonly int webSearchMaxResults;
        private readonly WebSearchDepth webSearchDepth;
        private readonly WebSearchAnswerMode webSearchAnswerMode;
        private readonly WebSearchRawContentMode webSearchRawContentMode;
        private readonly IOpenCorporatesClient? openCorporatesClient;
        private readonly int openCorporatesMaxResults;
        private IReadOnlyDictionary<string, IReadOnlyList<AgentToolDefinition>>? serviceBoundTools;

        private AskJoobyConversationService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, IChatClient chatClient, IChatClient? validationChatClient,
            IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator,
            bool enableValidation, int maxToolResultCharacters,
            int maxSearchesPerTurn, bool enableSuggestions,
            bool embedAllMessages,
            int ragTopK, double ragMinScore,
            double validationRagMinScore, float? temperature, float? validationTemperature,
            int maxFanOut, string? connectionString, IStringLocalizerFactory stringLocalizerFactory,
            bool serviceToolsReadOnly, DynamicEnvironment.DynamicEnvironment? dynamicEnvironment,
            int maxConcurrentToolCalls, IRagReranker? ragReranker, double memoryDecayHalfLifeDays = 30,
            IAgentGuidanceExchange? guidanceExchange = null,
            IAskJoobyTurnTracker? turnTracker = null,
            IAgentRoutingLedger? routingLedger = null,
            IWebSearchClient? webSearchClient = null, int webSearchMaxResults = 5,
            WebSearchDepth webSearchDepth = WebSearchDepth.Advanced,
            WebSearchAnswerMode webSearchAnswerMode = WebSearchAnswerMode.Advanced,
            WebSearchRawContentMode webSearchRawContentMode = WebSearchRawContentMode.Markdown,
            IOpenCorporatesClient? openCorporatesClient = null, int openCorporatesMaxResults = 5)
        {
            this.webSearchClient = webSearchClient;
            this.webSearchMaxResults = webSearchMaxResults > 0 ? webSearchMaxResults : 5;
            this.webSearchDepth = webSearchDepth;
            this.webSearchAnswerMode = webSearchAnswerMode;
            this.webSearchRawContentMode = webSearchRawContentMode;
            this.openCorporatesClient = openCorporatesClient;
            this.openCorporatesMaxResults = openCorporatesMaxResults > 0 ? openCorporatesMaxResults : 5;
            this.dbContext = dbContext;
            this.stringLocalizerFactory = stringLocalizerFactory;
            this.serviceToolsReadOnly = serviceToolsReadOnly;
            this.dynamicEnvironment = dynamicEnvironment;

            var budget = long.TryParse(dynamicEnvironment?.AppSettings("AskJoobyTurnTokenBudget"),
                out var parsedBudget)
                ? parsedBudget
                : 0;

            var cachedWeight = double.TryParse(
                dynamicEnvironment?.AppSettings("AskJoobyCachedTokenBudgetWeight"),
                NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedWeight)
                ? parsedWeight
                : 1.0;

            tokenBudget = budget > 0 ? new TokenBudget(budget, cachedInputWeight: cachedWeight) : null;

            this.ragReranker = ragReranker;

            ragCandidateCount = int.TryParse(
                                    dynamicEnvironment?.AppSettings("AskJoobyRagCandidateCount"),
                                    out var parsedCandidates)
                                && parsedCandidates > 0
                ? parsedCandidates
                : 0;

            ragMaxCandidateCharacters = int.TryParse(
                                            dynamicEnvironment?.AppSettings("AskJoobyRagMaxCandidateCharacters"),
                                            out var parsedMaxCandidateCharacters)
                                        && parsedMaxCandidateCharacters > 0
                ? parsedMaxCandidateCharacters
                : 9000;

            promotionBudgetCharacters = int.TryParse(
                                            dynamicEnvironment?.AppSettings("AskJoobyRagPromotionBudgetCharacters"),
                                            out var parsedBudgetChars)
                                        && parsedBudgetChars > 0
                ? parsedBudgetChars
                : 0;

            rerankMinScore = double.TryParse(
                dynamicEnvironment?.AppSettings("AskJoobyRerankerMinScore"),
                NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRerankMinScore)
                ? parsedRerankMinScore
                : 0;

            agentTokenBudget = long.TryParse(dynamicEnvironment?.AppSettings("AskJoobyAgentTokenBudget"),
                out var parsedAgentBudget) && parsedAgentBudget > 0
                ? parsedAgentBudget
                : 0;

            turnActivity = new TurnActivityLedger(tokenBudget);
            this.maxConcurrentToolCalls = maxConcurrentToolCalls;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.chatClient = chatClient;
            this.validationChatClient = validationChatClient ?? chatClient;
            this.embeddingGenerator = embeddingGenerator;
            this.enableValidation = enableValidation;
            this.enableSuggestions = enableSuggestions;
            this.maxToolResultCharacters = maxToolResultCharacters;
            this.maxSearchesPerTurn = maxSearchesPerTurn;
            this.embedAllMessages = embedAllMessages;
            this.ragTopK = ragTopK > 0 ? ragTopK : 5;
            this.ragMinScore = ragMinScore;
            this.validationRagMinScore = validationRagMinScore;
            this.temperature = temperature;
            this.validationTemperature = validationTemperature;
            this.maxFanOut = maxFanOut > 0 ? maxFanOut : 8;
            this.connectionString = connectionString;
            this.memoryDecayHalfLifeDays = memoryDecayHalfLifeDays > 0 ? memoryDecayHalfLifeDays : 30;
            this.guidanceExchange = guidanceExchange;
            this.turnTracker = turnTracker;
            this.routingLedger = routingLedger;
            conversationRepository = new AskJoobyConversationRepository(dbContext, tenantRegistryId, userName);
            messageRepository = new AskJoobyMessageRepository(dbContext);
            errataRepository = new AskJoobyErrataRepository(dbContext);
            fileRepository = new AskJoobyFileRepository(dbContext, tenantRegistryId, userName);
            runRepository = new AskJoobyRunRepository(dbContext);
            routingRepository = new AskJoobyRoutingRepository(dbContext);
            guardRejectionRepository = new AskJoobyAgentGuardRejectionRepository(dbContext);
            agentGuardRuleRepository = new AskJoobyAgentGuardRuleRepository(dbContext);
            toolCallRepository = new AskJoobyToolCallRepository(dbContext);
            modelCallRepository = new AskJoobyModelCallRepository(dbContext);
            embeddingCallRepository = new AskJoobyEmbeddingCallRepository(dbContext);
            rerankCallRepository = new AskJoobyRerankCallRepository(dbContext);
            webSearchCallRepository = new AskJoobyWebSearchCallRepository(dbContext);
            openCorporatesCallRepository = new AskJoobyOpenCorporatesCallRepository(dbContext);
            endpointFeeRepository = new AskJoobyEndpointFeeRepository(dbContext);
            rerankFeeRepository = new AskJoobyRerankFeeRepository(dbContext);
            turnCostRepository = new AskJoobyTurnCostRepository(dbContext);
            ragQuery = new SearchAskJoobyRagChunkQuery(dbContext);
            retrievedDocsQuery = new GetAskJoobyConversationRetrievedDocsQuery(dbContext);
            memoryRepository = new AskJoobyMemoryRepository(dbContext, tenantRegistryId, userName);
            memoryQuery = new SearchAskJoobyMemoryQuery(dbContext, this.memoryDecayHalfLifeDays);
            historyQuery = new SearchAskJoobyConversationHistoryQuery(dbContext);
            statsQuery = new GetAskJoobyStartupStatsQuery(dbContext);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        private bool WorkerEnableThinking =>
            "True".Equals(dynamicEnvironment?.AppSettings("AskJoobyEnableThinking"),
                StringComparison.OrdinalIgnoreCase);

        private int WorkerThinkingEffort =>
            int.TryParse(dynamicEnvironment?.AppSettings("AskJoobyThinkingEffort"), out var effort)
            && effort is >= 0 and <= 3
                ? effort
                : 1;

        private bool ValidationEnableThinking =>
            "True".Equals(dynamicEnvironment?.AppSettings("AskJoobyValidationEnableThinking"),
                StringComparison.OrdinalIgnoreCase);

        private bool DisableThinkingExplicitly =>
            !"False".Equals(dynamicEnvironment?.AppSettings("AskJoobyThinkingDisableExplicitly"),
                StringComparison.OrdinalIgnoreCase);

        private int? MaxOutputTokens
        {
            get
            {
                var configured = int.TryParse(
                    dynamicEnvironment?.AppSettings("AskJoobyMaxOutputTokens"), out var parsed)
                    ? parsed
                    : 0;

                return configured > 0 ? configured : null;
            }
        }

        private bool ShowPromptCacheShare =>
            "True".Equals(dynamicEnvironment?.AppSettings("AskJoobyShowPromptCacheShare"),
                StringComparison.OrdinalIgnoreCase);

        private bool EnableAskJoobyCostBanner =>
            "True".Equals(dynamicEnvironment?.AppSettings("EnableAskJoobyCostBanner"),
                StringComparison.OrdinalIgnoreCase);

        private bool RecordPromptPrefixHash =>
            "True".Equals(dynamicEnvironment?.AppSettings("AskJoobyRecordPromptPrefixHash"),
                StringComparison.OrdinalIgnoreCase);

        private int AgentIdleTimeoutSeconds =>
            int.TryParse(dynamicEnvironment?.AppSettings("AskJoobyAgentIdleTimeoutSeconds"), out var seconds)
                ? seconds
                : 120;

        private int MaxIterationsPerRequest =>
            int.TryParse(dynamicEnvironment?.AppSettings("AskJoobyMaxIterationsPerRequest"), out var iterations)
            && iterations > 0
                ? iterations
                : 200;

        private int MaxSpawnDepth =>
            int.TryParse(dynamicEnvironment?.AppSettings("AskJoobyMaxSpawnDepth"), out var maxSpawnDepth)
            && maxSpawnDepth > 0
                ? maxSpawnDepth
                : 6;

        private int ValidationThinkingEffort =>
            int.TryParse(dynamicEnvironment?.AppSettings("AskJoobyValidationThinkingEffort"), out var effort)
            && effort is >= 0 and <= 3
                ? effort
                : 0;

        public static Task<AskJoobyConversationService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            IChatClient chatClient, IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator,
            bool enableValidation = false,
            int maxToolResultCharacters = 6000,
            int maxSearchesPerTurn = 64, bool enableSuggestions = true,
            IChatClient? validationChatClient = null, bool embedAllMessages = false, int ragTopK = 5,
            double ragMinScore = 0, double validationRagMinScore = 0, float? temperature = null,
            float? validationTemperature = null, int maxFanOut = 8,
            string? connectionString = null, bool serviceToolsReadOnly = false,
            DynamicEnvironment.DynamicEnvironment? dynamicEnvironment = null,
            int maxConcurrentToolCalls = 4, IRagReranker? ragReranker = null,
            double memoryDecayHalfLifeDays = 30, IAgentGuidanceExchange? guidanceExchange = null,
            IAskJoobyTurnTracker? turnTracker = null,
            IAgentRoutingLedger? routingLedger = null,
            IWebSearchClient? webSearchClient = null, int webSearchMaxResults = 5,
            WebSearchDepth webSearchDepth = WebSearchDepth.Advanced,
            WebSearchAnswerMode webSearchAnswerMode = WebSearchAnswerMode.Advanced,
            WebSearchRawContentMode webSearchRawContentMode = WebSearchRawContentMode.Markdown,
            IOpenCorporatesClient? openCorporatesClient = null, int openCorporatesMaxResults = 5,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, chatClient,
                embeddingGenerator, LogManager.GetLogger("Jube.Audit"), enableValidation,
                maxToolResultCharacters, maxSearchesPerTurn,
                enableSuggestions,
                validationChatClient, embedAllMessages, ragTopK,
                ragMinScore, validationRagMinScore, temperature, validationTemperature, maxFanOut,
                connectionString, serviceToolsReadOnly, dynamicEnvironment, maxConcurrentToolCalls,
                ragReranker, memoryDecayHalfLifeDays, guidanceExchange, turnTracker, routingLedger,
                webSearchClient, webSearchMaxResults, webSearchDepth, webSearchAnswerMode,
                webSearchRawContentMode, openCorporatesClient, openCorporatesMaxResults, token);
        }

        internal static async Task<AskJoobyConversationService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            IChatClient chatClient, IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator, ILog auditLog,
            bool enableValidation = false,
            int maxToolResultCharacters = 6000,
            int maxSearchesPerTurn = 64, bool enableSuggestions = true,
            IChatClient? validationChatClient = null, bool embedAllMessages = false, int ragTopK = 5,
            double ragMinScore = 0, double validationRagMinScore = 0, float? temperature = null,
            float? validationTemperature = null, int maxFanOut = 8,
            string? connectionString = null, bool serviceToolsReadOnly = false,
            DynamicEnvironment.DynamicEnvironment? dynamicEnvironment = null,
            int maxConcurrentToolCalls = 4, IRagReranker? ragReranker = null,
            double memoryDecayHalfLifeDays = 30, IAgentGuidanceExchange? guidanceExchange = null,
            IAskJoobyTurnTracker? turnTracker = null,
            IAgentRoutingLedger? routingLedger = null,
            IWebSearchClient? webSearchClient = null, int webSearchMaxResults = 5,
            WebSearchDepth webSearchDepth = WebSearchDepth.Advanced,
            WebSearchAnswerMode webSearchAnswerMode = WebSearchAnswerMode.Advanced,
            WebSearchRawContentMode webSearchRawContentMode = WebSearchRawContentMode.Markdown,
            IOpenCorporatesClient? openCorporatesClient = null, int openCorporatesMaxResults = 5,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(AskJoobyConversationResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("AskJoobyConversation.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[AskJoobyConversationResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"AskJoobyConversation.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[AskJoobyConversationResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new AskJoobyConversationService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, chatClient, validationChatClient,
                embeddingGenerator, enableValidation, maxToolResultCharacters,
                maxSearchesPerTurn, enableSuggestions,
                embedAllMessages, ragTopK,
                ragMinScore, validationRagMinScore, temperature, validationTemperature, maxFanOut,
                connectionString, stringLocalizerFactory, serviceToolsReadOnly, dynamicEnvironment,
                maxConcurrentToolCalls, ragReranker, memoryDecayHalfLifeDays, guidanceExchange, turnTracker,
                routingLedger, webSearchClient, webSearchMaxResults, webSearchDepth, webSearchAnswerMode,
                webSearchRawContentMode, openCorporatesClient, openCorporatesMaxResults);
        }

        private IReadOnlyDictionary<string, IReadOnlyList<AgentToolDefinition>> BuildServiceBoundTools(
            CancellationToken token, IToolResultCache? resultCache = null)
        {
            if (serviceBoundTools is not null)
            {
                return serviceBoundTools;
            }

            lock (serviceBoundToolsGate)
            {
                if (serviceBoundTools is not null)
                {
                    return serviceBoundTools;
                }

                var services = new ServiceAgentServiceProvider(log, stringLocalizerFactory, serviceChangeBus,
                    embeddingGenerator, dynamicEnvironment);

                var bindingContext = new ServiceAgentBindingContext(dbContext, userName, services, token,
                    serviceToolsReadOnly, connectionString, maxConcurrentToolCalls, resultCache,
                    maxToolResultCharacters, TenantRegistryId: tenantRegistryId);

                var bound = new Dictionary<string, IReadOnlyList<AgentToolDefinition>>();

                foreach (var (agentType, toolNames) in AgentDefinitions.HierarchyToolNames)
                {
                    var tools = PageAgentToolBinder.Bind(bindingContext, toolNames, log);

                    if (tools.Count > 0)
                    {
                        bound[agentType] = tools;
                    }
                }

                var pointerReader = new AdvancedAnalyticsTrainingDataPointerReader(connectionString!, tenantRegistryId,
                    userName, log);

                var pointerWriter = resultCache is not null
                    ? new AdvancedAnalyticsTrainingDataPointerWriter(connectionString!, tenantRegistryId, userName,
                        log, resultCache)
                    : null;

                foreach (var (agentType, toolNames) in AdvancedAnalyticsComputationAgentToolNames.All)
                {
                    var tools = AdvancedAnalyticsAgentToolBinder.BindNames(
                        toolNames, resultCache, maxToolResultCharacters, pointerReader, pointerWriter);

                    if (tools.Count > 0)
                    {
                        bound[agentType] = tools;
                    }
                }

                foreach (var (agentType, toolNames) in AdvancedAnalyticsModelEvaluationAgentToolNames.All)
                {
                    var tools = AdvancedAnalyticsAgentToolBinder.BindNames(
                        toolNames, resultCache, maxToolResultCharacters, pointerReader, pointerWriter);

                    if (tools.Count > 0)
                    {
                        bound[agentType] = tools;
                    }
                }

                foreach (var (agentType, toolNames) in AdvancedAnalyticsTrainingAgentToolNames.All)
                {
                    var tools = AdvancedAnalyticsTrainingToolBinder.Bind(bindingContext, toolNames, log);

                    if (tools.Count > 0)
                    {
                        bound[agentType] = tools;
                    }
                }

                foreach (var (agentType, toolNames) in RuleExtensionAgentToolNames.All)
                {
                    var tools = RuleExtensionToolBinder.Bind(toolNames, resultCache, maxToolResultCharacters);

                    if (tools.Count == 0)
                    {
                        continue;
                    }

                    bound[agentType] = bound.TryGetValue(agentType, out var existing)
                        ? [.. existing, .. tools]
                        : tools;
                }

                serviceBoundTools = bound;

                return serviceBoundTools;
            }
        }

        [Description("Lists this tenant's AskJooby conversations, most recently active first.")]
        [ServiceOperation("AskJoobyConversationList", OperationKind.Read, Idempotent = true)]
        public async Task<List<AskJoobyConversationDto>> ListConversationsAsync(
            [Description("Optional case-insensitive filter against the conversation title.")]
            string? search,
            [Description("Maximum rows to return.")]
            int take = 50,
            [Description("Return only rows with Id less than this, for paging. 0 for the first page.")]
            int afterId = 0,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("AskJoobyConversation", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"AskJoobyConversation.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("AskJoobyConversation.List");
                var boundedTake = Math.Clamp(take, 1, 200);

                var dtos = AskJoobyConversationMapper.ToDto(
                    await conversationRepository.ListAsync(search ?? string.Empty, boundedTake, afterId, token)
                        .ConfigureAwait(false));
                op.Rows(dtos.Count);

                return dtos;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"AskJoobyConversation.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Starts a new, empty, untitled AskJooby conversation. Not idempotent.")]
        [ServiceOperation("AskJoobyConversationCreate", OperationKind.Write, Idempotent = false)]
        public async Task<AskJoobyConversationDto> CreateConversationAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("AskJoobyConversation", "Create", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"AskJoobyConversation.Create: entry user={userName}");
            }

            try
            {
                EnsurePermitted("AskJoobyConversation.Create");

                var saved = await conversationRepository
                    .InsertAsync(new Data.Poco.AskJoobyConversation(), token).ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"AskJoobyConversation.Create: created Id={saved.Id} user={userName}");
                }

                return AskJoobyConversationMapper.ToDto(saved)!;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"AskJoobyConversation.Create: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the full transcript of one conversation, oldest first -- every message and every " +
                     "narrated Pasture/Joobrik activity, replayed in the same shape the live stream used.")]
        [ServiceOperation("AskJoobyConversationGetMessages", OperationKind.Read, Idempotent = true)]
        public async Task<List<AskJoobyConversationEntryDto>> GetMessagesAsync(
            [Description("The conversation's Id.")]
            int conversationId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("AskJoobyConversation", "GetMessages", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"AskJoobyConversation.GetMessages: entry id={conversationId} user={userName}");
            }

            try
            {
                EnsurePermitted("AskJoobyConversation.GetMessages");

                var conversation = await conversationRepository.GetByIdAsync(conversationId, token)
                    .ConfigureAwait(false);

                if (conversation is null)
                {
                    throw new NotFoundException(strings[AskJoobyConversationResources.NotFound]);
                }

                var entries = await messageRepository.ListEntriesByConversationAsync(conversationId, token)
                    .ConfigureAwait(false);

                var runStats = await BuildRunStatsAsync(conversationId, entries, token).ConfigureAwait(false);

                var attachmentsByMessageId = await BuildAttachmentsByMessageIdAsync(entries, token)
                    .ConfigureAwait(false);

                var dtos = AskJoobyConversationMapper.ToEntryDto(entries, runStats, attachmentsByMessageId);
                op.Entity(conversationId);
                op.Rows(dtos.Count);

                return dtos;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);

                log.Error($"AskJoobyConversation.GetMessages: unexpected failure id={conversationId} " +
                          $"user={userName}", ex);

                throw;
            }
        }

        [Description("Returns the same live Pasture/Trails/Salt Lick/Joobriks/Yokes numbers the opening " +
                     "greeting would weave in -- a plain read, no model call, so it works even with no " +
                     "inference backend configured.")]
        [ServiceOperation("AskJoobyConversationGetStartupStats", OperationKind.Read, Idempotent = true)]
        public async Task<GetAskJoobyStartupStatsQueryDto> GetStartupStatsAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("AskJoobyConversation", "GetStartupStats", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"AskJoobyConversation.GetStartupStats: entry user={userName}");
            }

            try
            {
                EnsurePermitted("AskJoobyConversation.GetStartupStats");

                var stats = await BuildStartupStatsAsync(token).ConfigureAwait(false);
                op.Entity(tenantRegistryId);

                return stats;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"AskJoobyConversation.GetStartupStats: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task<GetAskJoobyStartupStatsQueryDto> BuildStartupStatsAsync(CancellationToken token)
        {
            var stats = await statsQuery.ExecuteAsync(tenantRegistryId, userName, token).ConfigureAwait(false);

            stats.Yokes = AgentDefinitions.CountAvailableTools(
                permissionValidation.Validate, !string.IsNullOrWhiteSpace(connectionString),
                permissionValidation.Landlord, BuildServiceBoundTools(token));

            stats.Joobriks =
                AgentDefinitions.CountAvailableJoobriks(permissionValidation.Validate, permissionValidation.Landlord);

            return stats;
        }

        public async Task<string> SubmitFeedbackAsync(
            [Description("The conversation's Id.")]
            int conversationId,
            [Description("The AskJoobyMessage Id of the Assistant reply being rated.")]
            int messageId,
            [Description("True for thumbs-up (a correct example), false for thumbs-down (an incorrect one).")]
            bool isPositive,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("AskJoobyConversation", "SubmitFeedback", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"AskJoobyConversation.SubmitFeedback: entry id={conversationId} messageId={messageId} " +
                          $"positive={isPositive} user={userName}");
            }

            try
            {
                EnsurePermitted("AskJoobyConversation.SubmitFeedback");

                var conversation = await conversationRepository.GetByIdAsync(conversationId, token)
                    .ConfigureAwait(false);

                if (conversation is null)
                {
                    throw new NotFoundException(strings[AskJoobyConversationResources.NotFound]);
                }

                var message = await messageRepository.GetByIdAsync(messageId, conversationId, token)
                    .ConfigureAwait(false);

                if (message is null ||
                    message.RoleId != (byte)AskJoobyMessageRole.Assistant)
                {
                    throw new NotFoundException(strings[AskJoobyConversationResources.NotFound]);
                }

                var question = "(original question unavailable)";

                if (message.AskJoobyRunId.HasValue)
                {
                    var run = await runRepository.GetByIdAsync(message.AskJoobyRunId.Value, token)
                        .ConfigureAwait(false);

                    if (run is not null && !string.IsNullOrWhiteSpace(run.Task))
                    {
                        question = run.Task;
                    }
                }

                var content = $"Q: {question}\n\nA: {message.Content}";

                var memoryClient = new AskJoobyMemoryClient(
                    memoryRepository, memoryQuery, tenantRegistryId, userName, embeddingGenerator, conversationId,
                    guardRejectionRepository);

                MemoryEntry saved;

                try
                {
                    saved = await memoryClient.RememberAsync(content, "User", true,
                        isPositive ? (byte)1 : (byte)0, token: token).ConfigureAwait(false);
                }
                catch (ArgumentException ex)
                {
                    throw new DtoValidationException(new FluentValidation.Results.ValidationResult(
                        [new FluentValidation.Results.ValidationFailure("Content", ex.Message)]));
                }

                var example = isPositive ? "a correct example" : "an incorrect example";

                var confirmation = string.Equals(saved.Content, content, StringComparison.Ordinal)
                    ? $"Remembered at the Salt Lick as {example} (your own scope)."
                    : $"Remembered at the Salt Lick as {example} (your own scope, personal data redacted).";

                await messageRepository.InsertAsync(
                    new AskJoobyMessage
                    {
                        AskJoobyConversationId = conversationId, RoleId = null, Content = confirmation,
                        EntryTypeId = AskJoobyMessageRepository.EntryTypeStep
                    }, token).ConfigureAwait(false);

                op.Entity(conversationId);
                op.Updated();

                return confirmation;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (DtoValidationException)
            {
                op.Outcome("invalid");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);

                log.Error($"AskJoobyConversation.SubmitFeedback: unexpected failure id={conversationId} " +
                          $"messageId={messageId} user={userName}", ex);

                throw;
            }
        }

        [Description("Deletes an AskJooby conversation. Not idempotent.")]
        [ServiceOperation("AskJoobyConversationDelete", OperationKind.Delete, Idempotent = false, Destructive = true)]
        public async Task DeleteConversationAsync(
            [Description("The conversation's Id.")]
            int conversationId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("AskJoobyConversation", "Delete", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"AskJoobyConversation.Delete: entry id={conversationId} user={userName}");
            }

            try
            {
                EnsurePermitted("AskJoobyConversation.Delete");

                await conversationRepository.DeleteAsync(conversationId, token).ConfigureAwait(false);

                op.Entity(conversationId);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"AskJoobyConversation.Delete: deleted id={conversationId} user={userName}");
                }
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (KeyNotFoundException)
            {
                op.Outcome("notfound");
                throw new NotFoundException(strings[AskJoobyConversationResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"AskJoobyConversation.Delete: unexpected failure id={conversationId} user={userName}", ex);
                throw;
            }
        }

        [Description("Sends a message to Jooby in an existing conversation and streams the reply -- reply " +
                     "text, narrated Pasture/Joobrik activity, and (on a conversation's first reply only) an " +
                     "auto-generated title. Not idempotent.")]
        [ServiceOperation("AskJoobyConversationSendMessage", OperationKind.Write, Idempotent = false)]
        public async IAsyncEnumerable<AskJoobyStreamItemDto> SendMessageAsync(
            [Description("The conversation's Id.")]
            int conversationId,
            [Description("The message text to send.")]
            string text,
            [Description("A small JSON object describing the Jube page the caller is currently on, if sent " +
                         "from the floating widget (pageName, state, and optionally a record) -- silently " +
                         "included as context for Jooby, never shown or persisted verbatim.")]
            string? pageContext = null,
            [Description("Ids of files (already uploaded via AskJoobyFileUpload) to attach to this message -- " +
                         "plain-text files have their content included for Jooby; other types are noted only.")]
            int[]? attachmentFileIds = null,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            EnsurePermitted("AskJoobyConversation.SendMessage");

            var conversation = await conversationRepository.GetByIdAsync(conversationId, token)
                .ConfigureAwait(false);

            if (conversation is null)
            {
                throw new NotFoundException(strings[AskJoobyConversationResources.NotFound]);
            }

            var agentGuardRules = await LoadAgentGuardRulesAsync(token).ConfigureAwait(false);

            var injectionFinding = await AskJoobyInjectionGuard.CheckAsync(
                    text, permissionValidation.Landlord, agentGuardRules)
                .ConfigureAwait(false);

            if (injectionFinding is not null)
            {
                await guardRejectionRepository.InsertAsync(new AskJoobyAgentGuardRejection
                {
                    AskJoobyConversationId = conversationId,
                    TenantRegistryId = tenantRegistryId,
                    PhaseId = MapGuardPhase(injectionFinding.Phase),
                    PipelineName = injectionFinding.PipelineName,
                    RuleName = injectionFinding.RuleName,
                    Reason = injectionFinding.Reason,
                    ActionId = MapGuardAction(injectionFinding.Action),
                    CreatedUser = userName
                }, token).ConfigureAwait(false);

                yield return new AskJoobyStreamItemDto
                {
                    Kind = "Guard",
                    Text = $"AgentGuard refused this message before any agent saw it: {injectionFinding.Message}.",
                    Detail = $"Phase: {injectionFinding.Phase}\nPipeline: {injectionFinding.PipelineName}\n" +
                             $"Rule: {injectionFinding.RuleName ?? "(unnamed)"}\nAction: {injectionFinding.Action}\n" +
                             $"Reason: {injectionFinding.Reason ?? "(none given)"}"
                };

                yield break;
            }

            using var affinity = ModelAffinityScope.Begin(conversation.Guid.ToString());

            using var op = OperationScope.Start("AskJoobyConversation", "SendMessage", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            if (log.IsInfoEnabled)
            {
                log.Info($"AskJoobyConversation.SendMessage: entry id={conversationId} user={userName}");
            }

            var conversationKey = conversationId.ToString(CultureInfo.InvariantCulture);
            var isBusy = turnTracker?.IsRunning(conversationKey) ?? false;

            List<AskJoobyMessage> priorMessages = isBusy
                ? []
                : await messageRepository.ListMessagesByConversationAsync(conversationId, token)
                    .ConfigureAwait(false);

            var attachmentContent = isBusy
                ? string.Empty
                : await BuildAttachmentContextAsync(conversationId, attachmentFileIds, token)
                    .ConfigureAwait(false);

            var userMessage = await InsertMessageAsync(
                new AskJoobyMessage
                {
                    AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.User, Content = text,
                    IsNudge = isBusy ? true : null
                }, token).ConfigureAwait(false);

            if (attachmentFileIds is { Length: > 0 })
            {
                await fileRepository.AttachToMessageAsync(attachmentFileIds, userMessage.Id, token)
                    .ConfigureAwait(false);
            }

            if (isBusy)
            {
                PushGuidanceBestEffort(conversationKey, text);

                op.Entity(conversationId);
                op.Updated();

                yield break;
            }

            var liveText = text;

            if (attachmentContent.Length > 0)
            {
                liveText = $"{liveText}\n\n{attachmentContent}";
            }

            var parsedPageContext = ParsePageContext(pageContext);

            var effectiveMessages = ConversationWindow.Select(priorMessages);

            var chatHistory = effectiveMessages
                .Select(m => new ChatMessage(MapRole(m.RoleId), m.Content))
                .Append(new ChatMessage(ChatRole.User, liveText))
                .ToList();

            var isFirstReply = string.IsNullOrEmpty(conversation.Title);
            var titleTask = isFirstReply ? GenerateTitleAsync(text, token) : null;

            using var turnScope = turnTracker?.Begin(conversationKey);

            IRunnableAgent? runnable = null;
            string? routedAgentType = null;
            AgentRoutingDecision? routedDecision = null;
            AskJoobyRunRecorder? recorder = null;
            IIsolatedRunScope? topLevelScope = null;
            var delegationChannel = Channel.CreateUnbounded<AgentStreamItem>();
            var isolatedRunScopeFactory = BuildIsolatedRunScopeFactory(conversationId);
            var exportSink = new ExportSink();
            var retrievedBiteLedger = new RetrievedBiteLedger(maxSearchesPerTurn);
            Exception? setupFailure = null;

            try
            {
                recorder = new AskJoobyRunRecorder(runRepository, toolCallRepository, modelCallRepository,
                    embeddingCallRepository, rerankCallRepository, webSearchCallRepository,
                    openCorporatesCallRepository, conversationId, tenantRegistryId, userName);

                topLevelScope = isolatedRunScopeFactory is null
                    ? null
                    : await isolatedRunScopeFactory.CreateAsync(token).ConfigureAwait(false);

                var topLevelRecorder = topLevelScope?.Recorder ?? recorder;

                var ragClient = topLevelScope?.Rag ?? new AskJoobyRagClient(ragQuery, embeddingGenerator, ragReranker,
                    new GetAskJoobyRagChunkByHashQuery(dbContext),
                    new GetAskJoobyRagChunkByParentHashQuery(dbContext), ragCandidateCount, rerankMinScore,
                    promotionBudgetCharacters, ragMaxCandidateCharacters);

                var memoryClient = topLevelScope?.Memory ?? new AskJoobyMemoryClient(
                    memoryRepository, memoryQuery, tenantRegistryId, userName, embeddingGenerator, conversationId,
                    guardRejectionRepository);

                var historyClient = topLevelScope?.History ?? new AskJoobyConversationHistoryClient(
                    historyQuery, tenantRegistryId, userName, embeddingGenerator);

                var sqlQueryClient = topLevelScope?.SqlQuery;
                var cacheClient = topLevelScope?.Cache;
                var exportClient = topLevelScope?.Export;
                var workflowProgressClient = topLevelScope?.WorkflowProgress;
                var scriptExecutionClient = topLevelScope?.ScriptExecution;

                var runContext = new RunContext(
                    topLevelRecorder, conversationId.ToString(CultureInfo.InvariantCulture), null,
                    permissionValidation.Validate,
                    WorkerEnableThinking, ThinkingEffort: WorkerThinkingEffort,
                    Errata: await LoadErrataAsync(token),
                    AgentGuardRules: agentGuardRules,
                    RagTopK: ragTopK, RagMinScore: ragMinScore, Temperature: temperature,
                    DelegationChannel: delegationChannel.Writer,
                    MaxFanOut: maxFanOut, IsolatedRunScopeFactory: isolatedRunScopeFactory,
                    ExportSink: exportSink,
                    RetrievedBiteLedger: retrievedBiteLedger,
                    IsLandlord: permissionValidation.Landlord,
                    ChartRenderer: chartRenderer,
                    DemoDatasets: demoDatasets,
                    WebSearch: webSearchClient,
                    WebSearchMaxResults: webSearchMaxResults,
                    WebSearchDepth: webSearchDepth,
                    WebSearchAnswerMode: webSearchAnswerMode,
                    WebSearchRawContentMode: webSearchRawContentMode,
                    OpenCorporates: openCorporatesClient,
                    OpenCorporatesMaxResults: openCorporatesMaxResults,
                    WorkflowProgress: workflowProgressClient,
                    AgentIdleTimeoutSeconds: AgentIdleTimeoutSeconds,
                    TokenBudget: tokenBudget, AgentTokenBudget: agentTokenBudget,
                    TurnActivity: turnActivity,
                    RepeatedTools: repeatedTools,
                    GuidanceExchange: guidanceExchange,
                    MaxOutputTokens: MaxOutputTokens,
                    RecordPromptPrefixHash: RecordPromptPrefixHash,
                    DisableThinkingExplicitly: DisableThinkingExplicitly,
                    ServiceBoundTools: BuildServiceBoundTools(token, cacheClient),
                    AutoSnapshot: BuildAutoSnapshot(),
                    CompactedHistory: BuildCompactedHistoryClient(conversationId),
                    MaxDepth: MaxSpawnDepth,
                    MaxIterationsPerRequest: MaxIterationsPerRequest,
                    LoggerFactory: loggerFactory,
                    GuardRejectionLedger: topLevelScope?.GuardRejectionLedger
                                          ?? new AskJoobyPersistedGuardRejectionLedger(
                                              guardRejectionRepository, conversationId, tenantRegistryId, userName),
                    PageContext: parsedPageContext);

                var availableAgents = AgentDefinitions.Available(runContext.HasPermission, runContext.IsLandlord);

                var persistedRouting = new AskJoobyPersistedRoutingLedger(routingRepository,
                    conversationId, tenantRegistryId, userName, text);

                var agentType = await AgentRouter
                    .RouteAsync(text, chatClient, embeddingGenerator, availableAgents, token,
                        BuildRoutingContext(priorMessages), UtilityChatOptions(),
                        routingLedger is null
                            ? persistedRouting
                            : new CompositeAgentRoutingLedger([persistedRouting, routingLedger]),
                        new AskJoobyRoutingContinuityClient(
                            new GetAskJoobyWorkflowProgressQuery(dbContext, conversationId)))
                    .ConfigureAwait(false);

                routedAgentType = agentType;
                routedDecision = persistedRouting.Decision;

                if (persistedRouting.RoutingId is { } routingId)
                {
                    topLevelRecorder.AttachRouting(routingId);
                }

                runnable = await RunnableFactory
                    .CreateAsync(agentType, chatClient, ragClient, memoryClient, historyClient, text, 0, runContext,
                        sqlQueryClient, cacheClient, exportClient, scriptExecutionClient)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                setupFailure = ex;
            }

            if (setupFailure is not null)
            {
                if (topLevelScope is not null)
                {
                    await topLevelScope.DisposeAsync().ConfigureAwait(false);
                }

                op.Error(setupFailure);

                log.Error($"AskJoobyConversation.SendMessage: failed to start id={conversationId} user={userName}",
                    setupFailure);

                yield return new AskJoobyStreamItemDto
                {
                    Kind = "Error", Text = "Jooby hit a snag getting started -- unlucky, try again in a moment."
                };

                yield break;
            }

            if (routedAgentType is not null)
            {
                var routedStep = AgentStreamNarrator.DescribeRoutingAnnouncement(routedAgentType);

                var routedDetail = routedDecision is { } decision
                    ? AgentStreamNarrator.DescribeRoutingDetail(routedAgentType, decision.Stage, decision.Score,
                        decision.DurationMs)
                    : null;

                yield return new AskJoobyStreamItemDto
                {
                    Kind = "Handoff", Text = routedStep, Detail = routedDetail
                };

                await messageRepository.InsertAsync(
                    new AskJoobyMessage
                    {
                        AskJoobyConversationId = conversationId, RoleId = null, Content = routedStep,
                        EntryTypeId = AskJoobyMessageRepository.EntryTypeHandoff
                    }, token).ConfigureAwait(false);
            }

            if (titleTask is not null)
            {
                var title = await titleTask.ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(title))
                {
                    await conversationRepository.UpdateTitleAsync(conversationId, title, token).ConfigureAwait(false);

                    yield return new AskJoobyStreamItemDto { Kind = "Title", Text = title };
                }
            }

            await using var topLevelScopeDisposer = topLevelScope;
            var replyBuilder = new ReplyDraft();
            var thinkingBuilder = new ThinkingPassBuffer();
            List<string>? clarificationOptions = null;
            var clarificationAllowFreeText = true;

            await using var enumerator = MergeDelegatedStreamAsync(runnable!, chatHistory, delegationChannel, token)
                .GetAsyncEnumerator(token);

            while (true)
            {
                AgentStreamItem? item = null;
                Exception? streamFailure = null;

                try
                {
                    if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        break;
                    }

                    item = enumerator.Current;
                }
                catch (OperationCanceledException)
                {
                    op.Outcome("cancelled");

                    foreach (var pass in thinkingBuilder.CompletedPasses())
                    {
                        await messageRepository.InsertAsync(
                            new AskJoobyMessage
                            {
                                AskJoobyConversationId = conversationId, RoleId = null,
                                Content = pass,
                                EntryTypeId = AskJoobyMessageRepository.EntryTypeThinking
                            }, CancellationToken.None).ConfigureAwait(false);
                    }

                    if (replyBuilder.Length > 0)
                    {
                        await messageRepository.InsertAsync(
                            new AskJoobyMessage
                            {
                                AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.Assistant,
                                Content = replyBuilder.ToString()
                            }, CancellationToken.None).ConfigureAwait(false);

                        await conversationRepository.TouchAsync(conversationId, CancellationToken.None)
                            .ConfigureAwait(false);
                    }

                    throw;
                }
                catch (Exception ex)
                {
                    streamFailure = ex;
                }

                if (streamFailure is not null)
                {
                    op.Error(streamFailure);

                    log.Error(
                        $"AskJoobyConversation.SendMessage: streaming failed id={conversationId} user={userName}",
                        streamFailure);

                    yield return new AskJoobyStreamItemDto
                    {
                        Kind = "Error", Text = "Jooby hit a snag mid-reply -- unlucky, try again in a moment."
                    };

                    yield break;
                }

                if (item!.Kind == AgentStreamItemKind.Error)
                {
                    op.Error(new InvalidOperationException(item.Text));

                    log.Error($"AskJoobyConversation.SendMessage: run reported an error id={conversationId} " +
                              $"user={userName}: {item.Text}");

                    yield return new AskJoobyStreamItemDto
                    {
                        Kind = "Error", Text = "Jooby hit a snag mid-reply -- unlucky, try again in a moment."
                    };

                    yield break;
                }

                if (item.Kind == AgentStreamItemKind.Text)
                {
                    replyBuilder.Append(item.Text);

                    if (!enableValidation)
                    {
                        yield return new AskJoobyStreamItemDto { Kind = "Text", Text = item.Text };
                    }
                }
                else if (item.Kind == AgentStreamItemKind.Thinking)
                {
                    thinkingBuilder.Append(item.AgentLabel, item.Text);

                    yield return new AskJoobyStreamItemDto
                    {
                        Kind = "Thinking", Text = item.Text, AgentLabel = item.AgentLabel
                    };
                }
                else if (item.Kind == AgentStreamItemKind.Clarification)
                {
                    clarificationOptions = item.Options is null ? [] : [.. item.Options];
                    clarificationAllowFreeText = item.AllowFreeText;

                    replyBuilder.Append(item.Text);

                    if (clarificationOptions.Count > 0)
                    {
                        replyBuilder.Append("\n\n");

                        foreach (var option in clarificationOptions)
                        {
                            replyBuilder.Append($"- {option}\n");
                        }
                    }
                }
                else
                {
                    replyBuilder.Observe(item);

                    var displayText = item.AgentLabel is null ? item.Text : $"[{item.AgentLabel}] {item.Text}";

                    await messageRepository.InsertAsync(
                        new AskJoobyMessage
                        {
                            AskJoobyConversationId = conversationId,
                            RoleId = null,
                            Content = displayText,
                            EntryTypeId = ToEntryType(item.Kind),
                            IconId = (int?)item.Icon,
                            Detail = item.Detail
                        }, token).ConfigureAwait(false);

                    yield return new AskJoobyStreamItemDto
                    {
                        Kind = item.Kind.ToString(), Text = displayText, AgentLabel = item.AgentLabel,
                        Detail = item.Detail, IconId = (int?)item.Icon
                    };
                }
            }

            foreach (var pass in thinkingBuilder.CompletedPasses())
            {
                await messageRepository.InsertAsync(
                    new AskJoobyMessage
                    {
                        AskJoobyConversationId = conversationId, RoleId = null,
                        Content = pass,
                        EntryTypeId = AskJoobyMessageRepository.EntryTypeThinking
                    }, token).ConfigureAwait(false);
            }

            var replyText = replyBuilder.ToString();
            var rootRunId = int.Parse(runnable!.RunId, CultureInfo.InvariantCulture);

            var streamedLive = !enableValidation && clarificationOptions is null &&
                               !string.IsNullOrWhiteSpace(replyText);

            if (string.IsNullOrWhiteSpace(replyText))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"AskJoobyConversation.SendMessage: run completed with no reply text " +
                             $"id={conversationId} user={userName} runId={rootRunId}");
                }

                replyText = strings[AskJoobyConversationResources.EmptyReply];
            }
            else if (enableValidation)
            {
                var conversationDocs = await retrievedDocsQuery.ExecuteAsync(conversationId, token)
                    .ConfigureAwait(false);

                var docs = ValidationDocs.Compact(conversationDocs);

                var toolEvidence = await BuildToolEvidenceAsync(conversationId, rootRunId, token)
                    .ConfigureAwait(false);

                var systemContext = SystemContextTool.Describe(parsedPageContext);
                var validationRunId = rootRunId.ToString(CultureInfo.InvariantCulture);

                async Task PersistStepAsync(string stepText,
                    byte entryType = AskJoobyMessageRepository.EntryTypeStep)
                {
                    await messageRepository.InsertAsync(
                        new AskJoobyMessage
                        {
                            AskJoobyConversationId = conversationId, RoleId = null, Content = stepText,
                            EntryTypeId = entryType
                        }, token).ConfigureAwait(false);
                }

                var validationBanner = $"Passing this to {validatorLabel} to check my work.";

                yield return new AskJoobyStreamItemDto
                    { Kind = "Validation", Text = validationBanner, Detail = replyText };

                await messageRepository.InsertAsync(
                    new AskJoobyMessage
                    {
                        AskJoobyConversationId = conversationId, RoleId = null, Content = validationBanner,
                        EntryTypeId = AskJoobyMessageRepository.EntryTypeValidation
                    }, token).ConfigureAwait(false);

                var validateTask = ResponseValidator.BuildTask(text, docs, replyText, toolEvidence, systemContext);
                var validateOutcome = new SubAgentOutcome();

                await foreach (var dto in ForwardSubAgentStreamAsync(
                                   RunValidatorSubAgentStreamingAsync(recorder!, validationChatClient, conversationId,
                                       validationRunId, validateTask, validateOutcome, retrievedBiteLedger, token),
                                   conversationId, token).ConfigureAwait(false))
                {
                    yield return dto;
                }

                var (validateOutput, validateError) = (validateOutcome.Result, validateOutcome.Error);
                var validation = validateOutput is null ? null : ResponseValidator.ParseResult(validateOutput);

                string outcomeStep;

                if (validation is null)
                {
                    outcomeStep = $"{validatorLabel} never picked up this time -- going with " +
                                  $"the first draft. ({validateError})";
                }
                else if (validation.Approved)
                {
                    outcomeStep = $"{validatorLabel} checked it against the Pasture -- holds up.";
                }
                else if (validation.FailureKind == ValidationFailureKind.Omission)
                {
                    outcomeStep = $"{validatorLabel} found a gap the docs don't cover, noting it...";
                }
                else
                {
                    outcomeStep = $"{validatorLabel} found something to fix, sending it back to the agent " +
                                  "to revise...";
                }

                yield return new AskJoobyStreamItemDto
                    { Kind = "Validation", Text = outcomeStep, Detail = validation?.Feedback };

                await PersistStepAsync(outcomeStep, AskJoobyMessageRepository.EntryTypeValidation)
                    .ConfigureAwait(false);

                if (validation is { Approved: false, FailureKind: ValidationFailureKind.Omission })
                {
                    replyText = replyText + "\n\n" + ValidationNoteMarkerStart +
                                "A note on the above -- a check against the docs couldn't confirm " +
                                "everything here. This records what the docs are silent on, not a " +
                                "finding that anything above is wrong:\n\n" + validation.Feedback! +
                                UnverifiedNotRefuted + ValidationNoteMarkerEnd;

                    var omissionStep = "The docs genuinely don't cover part of this -- added a note " +
                                       "rather than guessing or re-searching the same ground.";

                    yield return new AskJoobyStreamItemDto { Kind = "Step", Text = omissionStep };
                    await PersistStepAsync(omissionStep).ConfigureAwait(false);
                }
                else if (validation is { Approved: false })
                {
                    var reviseHandoffStep = "Handing this over to the agent now...";

                    yield return new AskJoobyStreamItemDto
                        { Kind = "Revision", Text = reviseHandoffStep, Detail = validation.Feedback };

                    await PersistStepAsync(reviseHandoffStep, AskJoobyMessageRepository.EntryTypeRevision)
                        .ConfigureAwait(false);

                    var reviseTask = ResponseReviser.BuildTask(
                        text, docs, replyText, validation.Feedback!, toolEvidence);

                    var reviseOutcome = new SubAgentOutcome();

                    await foreach (var dto in ForwardSubAgentStreamAsync(
                                       RunWorkerReviseSubAgentStreamingAsync(recorder!, conversationId, validationRunId,
                                           reviseTask, exportSink, reviseOutcome, retrievedBiteLedger, token),
                                       conversationId, token).ConfigureAwait(false))
                    {
                        yield return dto;
                    }

                    var (revisedText, reviseError) = (reviseOutcome.Result, reviseOutcome.Error);

                    ValidationResult? revisedValidation = null;
                    string? revalidateError = null;

                    if (revisedText is not null)
                    {
                        var revalidateHandoffStep =
                            $"Handing the revision back to {validatorLabel} for a second check...";

                        yield return new AskJoobyStreamItemDto
                            { Kind = "Revision", Text = revalidateHandoffStep, Detail = revisedText };

                        await PersistStepAsync(revalidateHandoffStep, AskJoobyMessageRepository.EntryTypeRevision)
                            .ConfigureAwait(false);

                        var revalidateTask =
                            ResponseValidator.BuildTask(text, docs, revisedText, toolEvidence, systemContext);

                        var revalidateOutcome = new SubAgentOutcome();

                        await foreach (var dto in ForwardSubAgentStreamAsync(
                                           RunValidatorSubAgentStreamingAsync(recorder!, validationChatClient,
                                               conversationId,
                                               validationRunId, revalidateTask, revalidateOutcome,
                                               retrievedBiteLedger, token),
                                           conversationId, token).ConfigureAwait(false))
                        {
                            yield return dto;
                        }

                        var (revalidateOutput, error) = (revalidateOutcome.Result, revalidateOutcome.Error);
                        revalidateError = error;

                        revisedValidation = revalidateOutput is null
                            ? null
                            : ResponseValidator.ParseResult(revalidateOutput);
                    }

                    string revisionStep;

                    if (revisedText is null)
                    {
                        revisionStep = $"Couldn't get a revised draft -- going with the original. ({reviseError})";
                    }
                    else if (revisedValidation is { Approved: true })
                    {
                        replyText = revisedText;
                        revisionStep = "Revised and re-checked -- solid now.";
                    }
                    else
                    {
                        replyText = revisedText + "\n\n" + ValidationNoteMarkerStart +
                                    "A note on the above -- a second check against the docs still " +
                                    "couldn't confirm everything here:\n\n" +
                                    (revisedValidation?.Feedback ?? $"(a second check couldn't be completed: " +
                                        $"{revalidateError})") +
                                    UnverifiedNotRefuted + ValidationNoteMarkerEnd;

                        revisionStep = revisedValidation is null
                            ? $"Revised, but a second check couldn't be completed -- see the note below. " +
                              $"({revalidateError})"
                            : "Revised, but a second check still couldn't confirm everything -- see the note " +
                              "below.";
                    }

                    yield return new AskJoobyStreamItemDto { Kind = "Step", Text = revisionStep };
                    await PersistStepAsync(revisionStep).ConfigureAwait(false);
                }
            }

            var savedMessage = await InsertMessageAsync(
                new AskJoobyMessage
                {
                    AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.Assistant,
                    Content = replyText,
                    AskJoobyRunId = rootRunId
                }, token).ConfigureAwait(false);

            await conversationRepository.TouchAsync(conversationId, token).ConfigureAwait(false);

            var exportedFiles = exportSink.Files;

            var attachments = exportedFiles.Count == 0
                ? []
                : exportedFiles.Select(f => new AskJoobyMessageAttachmentDto { Id = f.Id, Name = f.Name }).ToList();

            if (exportedFiles.Count > 0)
            {
                await fileRepository.AttachToMessageAsync(
                        exportedFiles.Select(f => f.Id).ToList(), savedMessage.Id, token)
                    .ConfigureAwait(false);
            }

            yield return new AskJoobyStreamItemDto
            {
                Kind = "Text", Text = streamedLive ? string.Empty : replyText, MessageId = savedMessage.Id,
                Attachments = attachments
            };

            if (clarificationOptions is { Count: > 0 })
            {
                yield return new AskJoobyStreamItemDto
                {
                    Kind = "Clarification", Options = clarificationOptions, AllowFreeText = clarificationAllowFreeText
                };
            }

            guidanceExchange?.Release(conversationId.ToString(CultureInfo.InvariantCulture));

            var runStats = await BuildRunStatsAsync(conversationId, [rootRunId], token).ConfigureAwait(false);

            if (runStats.TryGetValue(rootRunId, out var replyStats))
            {
                yield return new AskJoobyStreamItemDto
                {
                    Kind = "Stats", InputTokens = replyStats.InputTokens, OutputTokens = replyStats.OutputTokens,
                    CachedInputTokens = replyStats.CachedInputTokens,
                    DurationMs = replyStats.DurationMs,
                    JoobrikCount = replyStats.JoobrikCount,
                    DistinctJoobrikCount = replyStats.DistinctJoobrikCount,
                    TotalCost = replyStats.TotalCost
                };

                await PersistTurnCostAsync(rootRunId, replyStats, token).ConfigureAwait(false);
            }

            if (enableSuggestions && clarificationOptions is null && !string.IsNullOrWhiteSpace(replyText))
            {
                var suggestions = await GenerateSuggestionsAsync(replyText, token).ConfigureAwait(false);

                if (suggestions.Count > 0)
                {
                    yield return new AskJoobyStreamItemDto { Kind = "Suggestions", Suggestions = suggestions };
                }
            }

            op.Entity(conversationId);
            op.Updated();

            await ScheduleCompactionAsync(conversationId, token).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info($"AskJoobyConversation.SendMessage: completed id={conversationId} user={userName}");
            }
        }

        [Description("Sends a message to Jooby while a turn is already running for this conversation -- " +
                     "persisted to the transcript exactly like any other message, with a best-effort, " +
                     "no-guarantee attempt to surface it to the running turn before it finishes. Not idempotent.")]
        [ServiceOperation("AskJoobyConversationSendNudge", OperationKind.Write, Idempotent = false)]
        public async Task SendNudgeAsync(
            [Description("The conversation's Id.")]
            int conversationId,
            [Description("The message text to send while Jooby is already replying.")]
            string text,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("AskJoobyConversation", "SendNudge", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            if (log.IsInfoEnabled)
            {
                log.Info($"AskJoobyConversation.SendNudge: entry id={conversationId} user={userName}");
            }

            try
            {
                EnsurePermitted("AskJoobyConversation.SendMessage");

                var conversation = await conversationRepository.GetByIdAsync(conversationId, token)
                    .ConfigureAwait(false);

                if (conversation is null)
                {
                    throw new NotFoundException(strings[AskJoobyConversationResources.NotFound]);
                }

                var agentGuardRules = await LoadAgentGuardRulesAsync(token).ConfigureAwait(false);

                var injectionFinding = await AskJoobyInjectionGuard.CheckAsync(
                        text, permissionValidation.Landlord, agentGuardRules)
                    .ConfigureAwait(false);

                if (injectionFinding is not null)
                {
                    await guardRejectionRepository.InsertAsync(new AskJoobyAgentGuardRejection
                    {
                        AskJoobyConversationId = conversationId,
                        TenantRegistryId = tenantRegistryId,
                        PhaseId = MapGuardPhase(injectionFinding.Phase),
                        PipelineName = injectionFinding.PipelineName,
                        RuleName = injectionFinding.RuleName,
                        Reason = injectionFinding.Reason,
                        ActionId = MapGuardAction(injectionFinding.Action),
                        CreatedUser = userName
                    }, token).ConfigureAwait(false);

                    op.Outcome("guard-rejected");

                    return;
                }

                await InsertMessageAsync(
                    new AskJoobyMessage
                    {
                        AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.User,
                        Content = text, IsNudge = true
                    }, token).ConfigureAwait(false);

                PushGuidanceBestEffort(conversationId.ToString(CultureInfo.InvariantCulture), text);

                op.Entity(conversationId);
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"AskJoobyConversation.SendNudge: completed id={conversationId} user={userName}");
                }
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);

                log.Error($"AskJoobyConversation.SendNudge: unexpected failure id={conversationId} " +
                          $"user={userName}", ex);

                throw;
            }
        }

        [Description("Opens a brand-new conversation with Jooby speaking first -- introducing itself and " +
                     "sharing live stats about the Pasture, Trails, Saltlicks, Joobriks, and Yokes. A no-op " +
                     "if the conversation already has any messages. Not idempotent.")]
        [ServiceOperation("AskJoobyConversationStart", OperationKind.Write, Idempotent = false)]
        public async IAsyncEnumerable<AskJoobyStreamItemDto> StartConversationAsync(
            [Description("The conversation's Id.")]
            int conversationId,
            [Description("A small JSON object describing the Jube page the caller is currently on, if " +
                         "opened from the floating widget (pageName, state, and optionally a record) -- " +
                         "silently included as context for Jooby, never shown or persisted verbatim.")]
            string? pageContext,
            [Description("Whether AskJooby has a usable inference backend configured. When false, Jooby " +
                         "opens with a static status message instead of attempting a live reply.")]
            bool hasInferenceBackend,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            EnsurePermitted("AskJoobyConversation.Start");

            var conversation = await conversationRepository.GetByIdAsync(conversationId, token)
                .ConfigureAwait(false);

            if (conversation is null)
            {
                throw new NotFoundException(strings[AskJoobyConversationResources.NotFound]);
            }

            using var affinity = ModelAffinityScope.Begin(conversation.Guid.ToString());

            using var op = OperationScope.Start("AskJoobyConversation", "Start", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            var existingEntries = await messageRepository.ListEntriesByConversationAsync(conversationId, token)
                .ConfigureAwait(false);

            if (existingEntries.Count > 0)
            {
                op.Outcome("already-started");
                yield break;
            }

            var stats = await BuildStartupStatsAsync(token).ConfigureAwait(false);

            if (!hasInferenceBackend)
            {
                var fallback = string.Format(strings[AskJoobyConversationResources.NoInferenceBackend],
                    stats.Bites.ToString("N0"), stats.Trails.ToString("N0"), stats.Saltlicks.ToString("N0"),
                    stats.Joobriks.ToString("N0"), stats.Yokes.ToString("N0"));

                await InsertMessageAsync(new AskJoobyMessage
                {
                    AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.Assistant,
                    Content = fallback
                }, token).ConfigureAwait(false);

                await conversationRepository.TouchAsync(conversationId, token).ConfigureAwait(false);
                yield return new AskJoobyStreamItemDto { Kind = "Text", Text = fallback };

                op.Entity(conversationId);
                op.Updated();
                yield break;
            }

            var parsedPageContext = ParsePageContext(pageContext);

            if (parsedPageContext is null)
            {
                var canned = BuildCannedGreeting(stats);

                await InsertMessageAsync(new AskJoobyMessage
                {
                    AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.Assistant,
                    Content = canned
                }, token).ConfigureAwait(false);

                await conversationRepository.TouchAsync(conversationId, token).ConfigureAwait(false);
                yield return new AskJoobyStreamItemDto { Kind = "Text", Text = canned };

                op.Entity(conversationId);
                op.Updated();
                yield break;
            }

            var instruction = BuildOpeningInstruction(stats, hasPageContext: true);

            using var turnScope = turnTracker?.Begin(conversationId.ToString(CultureInfo.InvariantCulture));

            IRunnableAgent? runnable = null;
            IIsolatedRunScope? topLevelScope = null;
            var delegationChannel = Channel.CreateUnbounded<AgentStreamItem>();
            var isolatedRunScopeFactory = BuildIsolatedRunScopeFactory(conversationId);
            var exportSink = new ExportSink();
            Exception? setupFailure = null;

            try
            {
                var recorder = new AskJoobyRunRecorder(runRepository, toolCallRepository, modelCallRepository,
                    embeddingCallRepository, rerankCallRepository, webSearchCallRepository,
                    openCorporatesCallRepository, conversationId, tenantRegistryId, userName);

                topLevelScope = isolatedRunScopeFactory is null
                    ? null
                    : await isolatedRunScopeFactory.CreateAsync(token).ConfigureAwait(false);

                var topLevelRecorder = topLevelScope?.Recorder ?? recorder;

                var ragClient = topLevelScope?.Rag ?? new AskJoobyRagClient(ragQuery, embeddingGenerator, ragReranker,
                    new GetAskJoobyRagChunkByHashQuery(dbContext),
                    new GetAskJoobyRagChunkByParentHashQuery(dbContext), ragCandidateCount, rerankMinScore,
                    promotionBudgetCharacters, ragMaxCandidateCharacters);

                var memoryClient = topLevelScope?.Memory ?? new AskJoobyMemoryClient(
                    memoryRepository, memoryQuery, tenantRegistryId, userName, embeddingGenerator, conversationId,
                    guardRejectionRepository);

                var historyClient = topLevelScope?.History ?? new AskJoobyConversationHistoryClient(
                    historyQuery, tenantRegistryId, userName, embeddingGenerator);

                var sqlQueryClient = topLevelScope?.SqlQuery;
                var cacheClient = topLevelScope?.Cache;
                var exportClient = topLevelScope?.Export;
                var scriptExecutionClient = topLevelScope?.ScriptExecution;

                var runContext = new RunContext(
                    topLevelRecorder, conversationId.ToString(CultureInfo.InvariantCulture), null,
                    permissionValidation.Validate, RagTopK: ragTopK, RagMinScore: ragMinScore,
                    Errata: await LoadErrataAsync(token),
                    AgentGuardRules: await LoadAgentGuardRulesAsync(token),
                    DelegationChannel: delegationChannel.Writer, MaxFanOut: maxFanOut,
                    IsolatedRunScopeFactory: isolatedRunScopeFactory, ExportSink: exportSink,
                    IsLandlord: permissionValidation.Landlord,
                    ChartRenderer: chartRenderer,
                    DemoDatasets: demoDatasets,
                    WebSearch: webSearchClient,
                    WebSearchMaxResults: webSearchMaxResults,
                    WebSearchDepth: webSearchDepth,
                    WebSearchAnswerMode: webSearchAnswerMode,
                    WebSearchRawContentMode: webSearchRawContentMode,
                    OpenCorporates: openCorporatesClient,
                    OpenCorporatesMaxResults: openCorporatesMaxResults,
                    AgentIdleTimeoutSeconds: AgentIdleTimeoutSeconds,
                    TokenBudget: tokenBudget, AgentTokenBudget: agentTokenBudget,
                    TurnActivity: turnActivity,
                    RepeatedTools: repeatedTools,
                    GuidanceExchange: guidanceExchange,
                    MaxOutputTokens: MaxOutputTokens,
                    RecordPromptPrefixHash: RecordPromptPrefixHash,
                    DisableThinkingExplicitly: DisableThinkingExplicitly,
                    ServiceBoundTools: BuildServiceBoundTools(token, cacheClient),
                    AutoSnapshot: BuildAutoSnapshot(),
                    CompactedHistory: BuildCompactedHistoryClient(conversationId),
                    MaxDepth: MaxSpawnDepth,
                    MaxIterationsPerRequest: MaxIterationsPerRequest,
                    LoggerFactory: loggerFactory,
                    GuardRejectionLedger: topLevelScope?.GuardRejectionLedger
                                          ?? new AskJoobyPersistedGuardRejectionLedger(
                                              guardRejectionRepository, conversationId, tenantRegistryId, userName),
                    PageContext: parsedPageContext);

                runnable = await RunnableFactory.CreateAsync(
                        "general", chatClient, ragClient, memoryClient, historyClient, instruction, 0, runContext,
                        sqlQueryClient, cacheClient, exportClient, scriptExecutionClient, token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                setupFailure = ex;
            }

            if (setupFailure is not null)
            {
                if (topLevelScope is not null)
                {
                    await topLevelScope.DisposeAsync().ConfigureAwait(false);
                }

                op.Error(setupFailure);

                log.Error($"AskJoobyConversation.Start: failed to start id={conversationId} user={userName}",
                    setupFailure);

                yield break;
            }

            await using var topLevelScopeDisposer = topLevelScope;
            var replyBuilder = new ReplyDraft();
            var thinkingBuilder = new ThinkingPassBuffer();

            await using var enumerator = MergeDelegatedStreamAsync(
                    runnable!, [new ChatMessage(ChatRole.User, instruction)], delegationChannel, token)
                .GetAsyncEnumerator(token);

            while (true)
            {
                AgentStreamItem? item = null;
                Exception? streamFailure = null;

                try
                {
                    if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        break;
                    }

                    item = enumerator.Current;
                }
                catch (OperationCanceledException)
                {
                    op.Outcome("cancelled");
                    throw;
                }
                catch (Exception ex)
                {
                    streamFailure = ex;
                }

                if (streamFailure is not null)
                {
                    op.Error(streamFailure);

                    log.Error(
                        $"AskJoobyConversation.Start: streaming failed id={conversationId} user={userName}",
                        streamFailure);

                    yield break;
                }

                if (item!.Kind == AgentStreamItemKind.Error)
                {
                    op.Error(new InvalidOperationException(item.Text));

                    log.Error($"AskJoobyConversation.Start: run reported an error id={conversationId} " +
                              $"user={userName}: {item.Text}");

                    yield break;
                }

                if (item.Kind == AgentStreamItemKind.Text)
                {
                    replyBuilder.Append(item.Text);
                }
                else if (item.Kind == AgentStreamItemKind.Thinking)
                {
                    thinkingBuilder.Append(item.AgentLabel, item.Text);
                }
                else
                {
                    replyBuilder.Observe(item);

                    var displayText = item.AgentLabel is null ? item.Text : $"[{item.AgentLabel}] {item.Text}";

                    await messageRepository.InsertAsync(
                        new AskJoobyMessage
                        {
                            AskJoobyConversationId = conversationId,
                            RoleId = null,
                            Content = displayText,
                            EntryTypeId = ToEntryType(item.Kind),
                            IconId = (int?)item.Icon,
                            Detail = item.Detail
                        }, token).ConfigureAwait(false);

                    yield return new AskJoobyStreamItemDto
                    {
                        Kind = item.Kind.ToString(), Text = displayText, AgentLabel = item.AgentLabel,
                        Detail = item.Detail, IconId = (int?)item.Icon
                    };

                    continue;
                }

                yield return new AskJoobyStreamItemDto { Kind = item.Kind.ToString(), Text = item.Text };
            }

            foreach (var pass in thinkingBuilder.CompletedPasses())
            {
                await messageRepository.InsertAsync(
                    new AskJoobyMessage
                    {
                        AskJoobyConversationId = conversationId, RoleId = null,
                        Content = pass,
                        EntryTypeId = AskJoobyMessageRepository.EntryTypeThinking
                    }, token).ConfigureAwait(false);
            }

            var replyText = replyBuilder.ToString();
            var rootRunId = int.Parse(runnable!.RunId, CultureInfo.InvariantCulture);
            var wasEmptyReply = string.IsNullOrWhiteSpace(replyText);

            if (wasEmptyReply)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"AskJoobyConversation.Start: run completed with no reply text " +
                             $"id={conversationId} user={userName} runId={rootRunId}");
                }

                replyText = strings[AskJoobyConversationResources.EmptyReply];
            }

            var savedMessage = await InsertMessageAsync(
                new AskJoobyMessage
                {
                    AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.Assistant,
                    Content = replyText,
                    AskJoobyRunId = rootRunId
                }, token).ConfigureAwait(false);

            if (exportSink.Files.Count > 0)
            {
                await fileRepository.AttachToMessageAsync(
                        exportSink.Files.Select(f => f.Id).ToList(), savedMessage.Id, token)
                    .ConfigureAwait(false);
            }

            if (wasEmptyReply)
            {
                yield return new AskJoobyStreamItemDto
                {
                    Kind = "Text", Text = replyText, MessageId = savedMessage.Id
                };
            }

            await conversationRepository.TouchAsync(conversationId, token).ConfigureAwait(false);

            var runStats = await BuildRunStatsAsync(conversationId, [rootRunId], token).ConfigureAwait(false);

            if (runStats.TryGetValue(rootRunId, out var replyStats))
            {
                yield return new AskJoobyStreamItemDto
                {
                    Kind = "Stats", InputTokens = replyStats.InputTokens, OutputTokens = replyStats.OutputTokens,
                    CachedInputTokens = replyStats.CachedInputTokens,
                    DurationMs = replyStats.DurationMs,
                    JoobrikCount = replyStats.JoobrikCount,
                    DistinctJoobrikCount = replyStats.DistinctJoobrikCount,
                    TotalCost = replyStats.TotalCost
                };

                await PersistTurnCostAsync(rootRunId, replyStats, token).ConfigureAwait(false);
            }

            op.Entity(conversationId);
            op.Updated();

            if (log.IsInfoEnabled)
            {
                log.Info($"AskJoobyConversation.Start: completed id={conversationId} user={userName}");
            }
        }

        private static async IAsyncEnumerable<AgentStreamItem> StreamSubAgentAsync(
            IRunnableAgent subRunnable, string task, SubAgentOutcome outcome, ILog log, string logContext,
            int subAgentTimeoutSeconds, [EnumeratorCancellation] CancellationToken token)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(subAgentTimeoutSeconds));

            var subChannel = Channel.CreateUnbounded<AgentStreamItem>();
            var replyBuilder = new ReplyDraft();

            await using var enumerator = MergeDelegatedStreamAsync(
                    subRunnable, [new ChatMessage(ChatRole.User, task)], subChannel, timeoutCts.Token)
                .GetAsyncEnumerator(timeoutCts.Token);

            while (true)
            {
                AgentStreamItem? item = null;
                Exception? failure = null;

                try
                {
                    if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        break;
                    }

                    item = enumerator.Current;
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"{logContext}: timed out after {subAgentTimeoutSeconds}s");
                    }

                    outcome.Error = $"timed out after {subAgentTimeoutSeconds}s";
                    Activity.Current?.SetStatus(ActivityStatusCode.Error, outcome.Error);
                    yield break;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failure = ex;
                }

                if (failure is not null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"{logContext}: call failed", failure);
                    }

                    outcome.Error = failure.Message;
                    Activity.Current?.SetStatus(ActivityStatusCode.Error, outcome.Error);
                    yield break;
                }

                switch (item!.Kind)
                {
                    case AgentStreamItemKind.Text:
                        replyBuilder.Append(item.Text);
                        break;
                    case AgentStreamItemKind.Error:
                        if (log.IsWarnEnabled)
                        {
                            log.Warn($"{logContext}: run reported an error: {item.Text}");
                        }

                        outcome.Error = item.Text;
                        Activity.Current?.SetStatus(ActivityStatusCode.Error, outcome.Error);
                        yield break;
                    default:
                        replyBuilder.Observe(item);
                        yield return item;
                        break;
                }
            }

            outcome.Result = replyBuilder.ToString();
        }

        private async IAsyncEnumerable<AgentStreamItem> RunValidatorSubAgentStreamingAsync(
            AskJoobyRunRecorder recorder, IChatClient validationClient, int conversationId, string parentRunId,
            string task, SubAgentOutcome outcome, IRetrievedBiteLedger? retrievedBiteLedger,
            [EnumeratorCancellation] CancellationToken token)
        {
            var isolatedRunScopeFactory = BuildIsolatedRunScopeFactory(conversationId);

            await using var subScope = isolatedRunScopeFactory is null
                ? null
                : await isolatedRunScopeFactory.CreateAsync(token).ConfigureAwait(false);

            IRunnableAgent subRunnable;

            try
            {
                var ragClient = subScope?.Rag ?? new AskJoobyRagClient(ragQuery, embeddingGenerator, ragReranker,
                    new GetAskJoobyRagChunkByHashQuery(dbContext),
                    new GetAskJoobyRagChunkByParentHashQuery(dbContext), ragCandidateCount, rerankMinScore,
                    promotionBudgetCharacters, ragMaxCandidateCharacters);

                var memoryClient = subScope?.Memory ?? new AskJoobyMemoryClient(
                    memoryRepository, memoryQuery, tenantRegistryId, userName, embeddingGenerator, conversationId,
                    guardRejectionRepository);

                var historyClient = subScope?.History ?? new AskJoobyConversationHistoryClient(
                    historyQuery, tenantRegistryId, userName, embeddingGenerator);

                var subContext = new RunContext(subScope?.Recorder ?? recorder,
                    conversationId.ToString(CultureInfo.InvariantCulture),
                    parentRunId, permissionValidation.Validate,
                    ValidationEnableThinking, RagTopK: ragTopK,
                    RagMinScore: validationRagMinScore, Temperature: validationTemperature,
                    Errata: await LoadErrataAsync(token),
                    AgentGuardRules: await LoadAgentGuardRulesAsync(token),
                    ThinkingEffort: ValidationThinkingEffort,
                    IsLandlord: permissionValidation.Landlord,
                    ChartRenderer: chartRenderer,
                    DemoDatasets: demoDatasets,
                    WebSearch: webSearchClient,
                    WebSearchMaxResults: webSearchMaxResults,
                    WebSearchDepth: webSearchDepth,
                    WebSearchAnswerMode: webSearchAnswerMode,
                    WebSearchRawContentMode: webSearchRawContentMode,
                    OpenCorporates: openCorporatesClient,
                    OpenCorporatesMaxResults: openCorporatesMaxResults,
                    AgentIdleTimeoutSeconds: AgentIdleTimeoutSeconds,
                    AllowDelegation: false,
                    TokenBudget: tokenBudget, AgentTokenBudget: agentTokenBudget,
                    TurnActivity: turnActivity,
                    RepeatedTools: repeatedTools,
                    GuidanceExchange: guidanceExchange,
                    RetrievedBiteLedger: retrievedBiteLedger,
                    MaxOutputTokens: MaxOutputTokens,
                    RecordPromptPrefixHash: RecordPromptPrefixHash,
                    DisableThinkingExplicitly: DisableThinkingExplicitly,
                    ServiceBoundTools: BuildServiceBoundTools(token),
                    AutoSnapshot: BuildAutoSnapshot(),
                    CompactedHistory: BuildCompactedHistoryClient(conversationId),
                    MaxDepth: MaxSpawnDepth,
                    MaxIterationsPerRequest: MaxIterationsPerRequest,
                    LoggerFactory: loggerFactory,
                    GuardRejectionLedger: subScope?.GuardRejectionLedger
                                          ?? new AskJoobyPersistedGuardRejectionLedger(
                                              guardRejectionRepository, conversationId, tenantRegistryId, userName));

                subRunnable = await RunnableFactory.CreateAsync(
                        "validator", validationClient, ragClient, memoryClient, historyClient, task,
                        0, subContext, token: token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"AskJoobyConversation.SendMessage: validator sub-agent setup failed " +
                             $"id={conversationId} user={userName}", ex);
                }

                outcome.Error = ex.Message;
                Activity.Current?.SetStatus(ActivityStatusCode.Error, outcome.Error);
                yield break;
            }

            await foreach (var item in StreamSubAgentAsync(subRunnable, task, outcome, log,
                               $"AskJoobyConversation.SendMessage: validator sub-agent id={conversationId} user={userName}",
                               SubAgentTimeoutSeconds, token).ConfigureAwait(false))
            {
                yield return item.AgentLabel is null ? item with { AgentLabel = validatorLabel } : item;
            }
        }

        private async Task<IReadOnlyList<AgentErratum>> LoadErrataAsync(CancellationToken token)
        {
            var rows = await errataRepository.GetActiveOrderByIdAsync(token).ConfigureAwait(false);

            return rows.Select(row => new AgentErratum(row.Affects, row.Body)).ToList();
        }

        private async Task<IReadOnlyList<AgentGuardRuleSetting>> LoadAgentGuardRulesAsync(CancellationToken token)
        {
            var rows = await agentGuardRuleRepository.GetAllAsync(token).ConfigureAwait(false);

            return rows
                .Where(row => row.RuleId is not null)
                .Select(row => new AgentGuardRuleSetting(row.RuleId!.Value.ToString(), row.Enabled,
                    row.GuidanceText))
                .ToList();
        }

        private ChatOptions? UtilityChatOptions()
        {
            if (!"True".Equals(dynamicEnvironment?.AppSettings("AskJoobyUtilityEnableThinking"),
                    StringComparison.OrdinalIgnoreCase))
            {
                return DisableThinkingExplicitly
                    ? new ChatOptions { RawRepresentationFactory = _ => ThinkingOptions.DisabledOptions() }
                    : null;
            }

            var effort = int.TryParse(dynamicEnvironment?.AppSettings("AskJoobyUtilityThinkingEffort"),
                out var parsed)
                ? parsed
                : 0;

            return new ChatOptions { RawRepresentationFactory = _ => ThinkingOptions.RawOptions(effort) };
        }

        private async IAsyncEnumerable<AgentStreamItem> RunWorkerReviseSubAgentStreamingAsync(
            AskJoobyRunRecorder recorder, int conversationId, string parentRunId, string task,
            IExportSink exportSink, SubAgentOutcome outcome, IRetrievedBiteLedger? retrievedBiteLedger,
            [EnumeratorCancellation] CancellationToken token)
        {
            var isolatedRunScopeFactory = BuildIsolatedRunScopeFactory(conversationId);

            await using var subScope = isolatedRunScopeFactory is null
                ? null
                : await isolatedRunScopeFactory.CreateAsync(token).ConfigureAwait(false);

            var ragClient = subScope?.Rag ?? new AskJoobyRagClient(ragQuery, embeddingGenerator, ragReranker,
                new GetAskJoobyRagChunkByHashQuery(dbContext),
                new GetAskJoobyRagChunkByParentHashQuery(dbContext), ragCandidateCount, rerankMinScore,
                promotionBudgetCharacters, ragMaxCandidateCharacters);

            var memoryClient = subScope?.Memory ?? new AskJoobyMemoryClient(
                memoryRepository, memoryQuery, tenantRegistryId, userName, embeddingGenerator, conversationId,
                guardRejectionRepository);

            var historyClient = subScope?.History ?? new AskJoobyConversationHistoryClient(
                historyQuery, tenantRegistryId, userName, embeddingGenerator);

            IRunnableAgent subRunnable;

            try
            {
                var subContext = new RunContext(subScope?.Recorder ?? recorder,
                    conversationId.ToString(CultureInfo.InvariantCulture),
                    parentRunId, permissionValidation.Validate, RagTopK: ragTopK,
                    RagMinScore: ragMinScore, Temperature: temperature,
                    Errata: await LoadErrataAsync(token),
                    AgentGuardRules: await LoadAgentGuardRulesAsync(token),
                    MaxFanOut: maxFanOut, IsolatedRunScopeFactory: isolatedRunScopeFactory,
                    ExportSink: exportSink, IsLandlord: permissionValidation.Landlord,
                    ChartRenderer: chartRenderer,
                    DemoDatasets: demoDatasets,
                    WebSearch: webSearchClient,
                    WebSearchMaxResults: webSearchMaxResults,
                    WebSearchDepth: webSearchDepth,
                    WebSearchAnswerMode: webSearchAnswerMode,
                    WebSearchRawContentMode: webSearchRawContentMode,
                    OpenCorporates: openCorporatesClient,
                    OpenCorporatesMaxResults: openCorporatesMaxResults,
                    WorkflowProgress: subScope?.WorkflowProgress,
                    AgentIdleTimeoutSeconds: AgentIdleTimeoutSeconds,
                    TokenBudget: tokenBudget, AgentTokenBudget: agentTokenBudget,
                    TurnActivity: turnActivity,
                    RepeatedTools: repeatedTools,
                    GuidanceExchange: guidanceExchange,
                    RetrievedBiteLedger: retrievedBiteLedger,
                    MaxOutputTokens: MaxOutputTokens,
                    RecordPromptPrefixHash: RecordPromptPrefixHash,
                    DisableThinkingExplicitly: DisableThinkingExplicitly,
                    ServiceBoundTools: BuildServiceBoundTools(token),
                    AutoSnapshot: BuildAutoSnapshot(),
                    CompactedHistory: BuildCompactedHistoryClient(conversationId),
                    MaxDepth: MaxSpawnDepth,
                    MaxIterationsPerRequest: MaxIterationsPerRequest,
                    LoggerFactory: loggerFactory,
                    GuardRejectionLedger: subScope?.GuardRejectionLedger
                                          ?? new AskJoobyPersistedGuardRejectionLedger(
                                              guardRejectionRepository, conversationId, tenantRegistryId, userName));

                subRunnable = await RunnableFactory.CreateAsync(
                        "general", chatClient, ragClient, memoryClient, historyClient, task, 0, subContext,
                        subScope?.SqlQuery, subScope?.Cache, subScope?.Export, subScope?.ScriptExecution, token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"AskJoobyConversation.SendMessage: worker revise sub-agent setup failed " +
                             $"id={conversationId} user={userName}", ex);
                }

                outcome.Error = ex.Message;
                Activity.Current?.SetStatus(ActivityStatusCode.Error, outcome.Error);
                yield break;
            }

            await foreach (var item in StreamSubAgentAsync(subRunnable, task, outcome, log,
                                   "AskJoobyConversation.SendMessage: worker revise sub-agent " +
                                   $"id={conversationId} user={userName}", SubAgentTimeoutSeconds, token)
                               .ConfigureAwait(false))
            {
                yield return item;
            }
        }

        private async IAsyncEnumerable<AskJoobyStreamItemDto> ForwardSubAgentStreamAsync(
            IAsyncEnumerable<AgentStreamItem> items, int conversationId,
            [EnumeratorCancellation] CancellationToken token)
        {
            var thinkingBuilder = new ThinkingPassBuffer();

            await foreach (var item in items.WithCancellation(token).ConfigureAwait(false))
            {
                if (item.Kind == AgentStreamItemKind.Thinking)
                {
                    thinkingBuilder.Append(item.AgentLabel, item.Text);

                    yield return new AskJoobyStreamItemDto
                    {
                        Kind = "Thinking", Text = item.Text, AgentLabel = item.AgentLabel
                    };

                    continue;
                }

                var displayText = item.AgentLabel is null ? item.Text : $"[{item.AgentLabel}] {item.Text}";

                await messageRepository.InsertAsync(
                    new AskJoobyMessage
                    {
                        AskJoobyConversationId = conversationId, RoleId = null, Content = displayText,
                        EntryTypeId = ToEntryType(item.Kind),
                        IconId = (int?)item.Icon,
                        Detail = item.Detail
                    }, token).ConfigureAwait(false);

                yield return new AskJoobyStreamItemDto
                {
                    Kind = item.Kind.ToString(), Text = displayText, AgentLabel = item.AgentLabel,
                    Detail = item.Detail, IconId = (int?)item.Icon
                };
            }

            foreach (var pass in thinkingBuilder.CompletedPasses())
            {
                await messageRepository.InsertAsync(
                    new AskJoobyMessage
                    {
                        AskJoobyConversationId = conversationId, RoleId = null,
                        Content = pass, EntryTypeId = AskJoobyMessageRepository.EntryTypeThinking
                    }, token).ConfigureAwait(false);
            }
        }

        private static string BuildCannedGreeting(GetAskJoobyStartupStatsQueryDto stats)
        {
            var template = cannedGreetingTemplates[Random.Shared.Next(cannedGreetingTemplates.Length)];

            return string.Format(template, stats.Bites.ToString("N0"), stats.Trails.ToString("N0"),
                stats.Saltlicks.ToString("N0"), stats.Joobriks.ToString("N0"), stats.Yokes.ToString("N0"));
        }

        private static PageContext? ParsePageContext(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            string? pageName = null;
            string? state = null;
            string? recordSummary = null;

            try
            {
                using var document = JsonDocument.Parse(raw);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("pageName", out var pageNameElement) &&
                        pageNameElement.ValueKind == JsonValueKind.String)
                    {
                        pageName = pageNameElement.GetString();
                    }

                    if (root.TryGetProperty("state", out var stateElement) &&
                        stateElement.ValueKind == JsonValueKind.String)
                    {
                        state = stateElement.GetString();
                    }

                    if (root.TryGetProperty("record", out var recordElement) &&
                        recordElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        recordSummary = recordElement.GetRawText();
                    }
                }
            }
            catch (JsonException)
            {
                pageName = raw.Trim();
            }

            if (string.IsNullOrWhiteSpace(pageName))
            {
                return null;
            }

            PagePurposeCatalogue.TryGet(pageName, out var entry);

            return new PageContext(pageName, state, entry?.Purpose, entry?.LikelyAsks,
                recordSummary is { Length: > 0 and <= 2000 } ? recordSummary : null);
        }

        private static string BuildOpeningInstruction(GetAskJoobyStartupStatsQueryDto stats, bool hasPageContext)
        {
            var instruction =
                "A brand-new conversation has just started -- there is no user message yet, and none is " +
                "coming until you speak first. Introduce yourself as yourself, Jooby, in the first " +
                "person -- never as \"Jooby's assistant\" or \"Jooby's helper\", as if you and Jooby " +
                "were different people. This is a greeting, not a correction, so keep it in your " +
                "professional, measured baseline register -- no gruffness, that's reserved for firmness " +
                "later, not an opener. Weave in these live numbers naturally rather than listing them " +
                $"mechanically: the Pasture holds {stats.Bites:N0} Bites; you've trodden {stats.Trails:N0} " +
                $"Trails in your lifetime; you have {stats.Saltlicks:N0} Saltlicks to draw on; " +
                $"{stats.Joobriks:N0} specialist Joobrik(s) are yours to call on for a Stampede the " +
                $"moment a job needs one; and {stats.Yokes:N0} Yokes -- your actual tools, Pasture " +
                "search through to reading the database directly -- are in the harness right now. " +
                "Joobriks/Yokes are what's available to reach for, not a tally of what you've already " +
                "done, so phrase them that way (\"on call\", \"in the harness\"), never as a usage count " +
                "(\"sent out\", \"called on so far\"). If it fits naturally in a sentence, make clear " +
                "you're not limited to chatting: you reach into this platform's own service layer the " +
                "same way its own screens do, and further -- when that alone doesn't cover it you can " +
                "write and run real SQL against the database directly, parsed and rewritten to what " +
                "the caller is actually allowed to see, and on rare occasions run a small R script of " +
                "your own in a locked-down sandbox when nothing else fits. Lay down what you find at " +
                "the Salt Lick for next time. Keep the whole thing brief -- a couple of sentences, not " +
                "a report -- and end ready to hear what they need.";

            if (hasPageContext)
            {
                instruction = "[Your system instructions above already describe the page the user is " +
                              "currently on -- background for you only, never visible to them. Let it " +
                              "inform your greeting only if it naturally suggests something worth a light " +
                              "touch; never name the page, quote it, or read anything from it aloud, and " +
                              "never let it add so much as a clause to the greeting's length. If nothing " +
                              "about it is worth weaving in, ignore it completely and greet exactly as you " +
                              $"otherwise would.]\n\n{instruction}";
            }

            return instruction;
        }

        private async Task<string> BuildAttachmentContextAsync(
            int conversationId, int[]? attachmentFileIds, CancellationToken token)
        {
            if (attachmentFileIds is null || attachmentFileIds.Length == 0)
            {
                return string.Empty;
            }

            var contentBuilder = new StringBuilder();
            var agentGuardRules = await LoadAgentGuardRulesAsync(token).ConfigureAwait(false);

            foreach (var fileId in attachmentFileIds)
            {
                var file = await fileRepository.GetByIdAsync(fileId, token).ConfigureAwait(false);

                if (file is null || file.AskJoobyConversationId != conversationId)
                {
                    continue;
                }

                var extension = file.Extension?.ToLowerInvariant() ?? string.Empty;

                if (file.Object is not null && AskJoobyFileService.TextExtensions.Contains(extension))
                {
                    var text = Encoding.UTF8.GetString(file.Object);

                    var attachmentFinding = await AskJoobyInjectionGuard.CheckAsync(text, rules: agentGuardRules)
                        .ConfigureAwait(false);

                    if (attachmentFinding is not null)
                    {
                        await guardRejectionRepository.InsertAsync(new AskJoobyAgentGuardRejection
                        {
                            AskJoobyConversationId = conversationId,
                            TenantRegistryId = tenantRegistryId,
                            PhaseId = MapGuardPhase(attachmentFinding.Phase),
                            PipelineName = attachmentFinding.PipelineName,
                            RuleName = attachmentFinding.RuleName,
                            Reason = attachmentFinding.Reason,
                            ActionId = MapGuardAction(attachmentFinding.Action),
                            CreatedUser = userName
                        }, token).ConfigureAwait(false);

                        contentBuilder.AppendLine(
                            $"[{file.Name} withheld: it matched prompt-injection screening.]");

                        contentBuilder.AppendLine();
                        continue;
                    }

                    if (text.Length <= maxToolResultCharacters || maxToolResultCharacters <= 0)
                    {
                        contentBuilder.AppendLine($"[Content of {file.Name}]");
                        contentBuilder.AppendLine(text);
                        contentBuilder.AppendLine();
                        continue;
                    }

                    contentBuilder.AppendLine(
                        await DescribeLargeAttachmentAsync(file.Name, text, token).ConfigureAwait(false));

                    contentBuilder.AppendLine();
                }
            }

            return contentBuilder.ToString();
        }

        private async Task<string> DescribeLargeAttachmentAsync(string? name, string text,
            CancellationToken token)
        {
            var label = string.IsNullOrWhiteSpace(name) ? "attachment" : name;

            try
            {
                var cache = new AskJoobyDataCacheClient(
                    new AskJoobyDataCacheRepository(dbContext, tenantRegistryId, userName), null,
                    advancedAnalyticsCache: new AdvancedAnalyticsCachedTableReader(dbContext, tenantRegistryId,
                        userName));

                CachedTable table;
                string shape;

                if (DelimitedTextTable.TryParse(text, out var parsed))
                {
                    table = parsed;

                    shape = $"{table.Rows.Count} row(s), {table.Columns.Count} column(s). "
                            + $"Columns: {string.Join(", ", table.Columns.Select(column => column.Name))}.";
                }
                else
                {
                    var lines = text.Split('\n');

                    var rows = lines.Select((line, index) => (IReadOnlyDictionary<string, object?>)
                            new Dictionary<string, object?>
                            {
                                ["Line"] = (double)(index + 1), ["Text"] = line.TrimEnd('\r')
                            })
                        .ToList();

                    table = new CachedTable(
                        [new CachedColumn("Line", "Double"), new CachedColumn("Text", "String")], rows);

                    shape = $"{rows.Count} line(s) of text, not delimited.";
                }

                var pointer = await cache.StoreAsync(table, $"Attachment {label}", token: token)
                    .ConfigureAwait(false);

                return $"[Attached {label}: {text.Length.ToString(CultureInfo.InvariantCulture)} characters, "
                       + $"too large to include here. {shape} data_pointer: \"{pointer}\". Use "
                       + "InspectDataSchema/PreviewDataRows/QueryCachedData with that data_pointer to read "
                       + "and filter it. Do not ask for all of it back -- filter to what answers the "
                       + "question.]";
            }
            catch (Exception ex)
            {
                log.Warn($"AskJoobyConversation.Attachment: could not cache '{label}'; including a head "
                         + "excerpt instead.", ex);

                var head = text.Length > 2000 ? text[..2000] : text;

                return $"[Attached {label}: {text.Length.ToString(CultureInfo.InvariantCulture)} characters, "
                       + $"too large to include and could not be cached. First part only:]\n{head}";
            }
        }

        private async Task<string?> GenerateTitleAsync(string firstMessage, CancellationToken token)
        {
            try
            {
                var prompt = "Summarise the following message as a short chat title of four words or fewer, " +
                             "plain text, no quotes, no trailing punctuation:\n\n" + firstMessage;

                var response = await chatClient
                    .GetResponseAsync([new ChatMessage(ChatRole.User, prompt)], UtilityChatOptions(), token)
                    .ConfigureAwait(false);

                var title = response.Text.Trim();

                return string.IsNullOrWhiteSpace(title) ? null : title;
            }
            catch (Exception ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("AskJoobyConversation.SendMessage: title generation failed", ex);
                }

                return null;
            }
        }

        private static string Tail(string text, int characters)
        {
            return text.Length <= characters
                ? text
                : "[...earlier part of the reply omitted]\n" + text[^characters..];
        }

        private async Task<List<string>> GenerateSuggestionsAsync(string replyText, CancellationToken token)
        {
            try
            {
                var prompt = "Below is the end of a reply just given on a fraud/AML rules platform. Suggest " +
                             "2-3 short, concrete follow-up questions or actions the reader might want next. " +
                             "Each under 8 words, plain text, one per line, no numbering, no leading dashes " +
                             "or bullets, no quotes. If nothing sensible suggests itself (the reply was a " +
                             "decline, a closing remark, or a plain acknowledgement), reply with nothing at " +
                             "all.\n\n" + Tail(replyText, SuggestionReplyCharacters);

                var response = await chatClient
                    .GetResponseAsync([new ChatMessage(ChatRole.User, prompt)], UtilityChatOptions(), token)
                    .ConfigureAwait(false);

                var text = response.Text.Trim();

                if (string.IsNullOrWhiteSpace(text))
                {
                    return [];
                }

                return text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Take(3)
                    .ToList();
            }
            catch (Exception ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("AskJoobyConversation.SendMessage: suggestion generation failed", ex);
                }

                return [];
            }
        }

        private static ChatRole MapRole(byte? role)
        {
            if (role == (byte)AskJoobyMessageRole.User)
            {
                return ChatRole.User;
            }

            return role == (byte)AskJoobyMessageRole.System
                ? ChatRole.System
                : ChatRole.Assistant;
        }

        private static string RoleName(byte? role)
        {
            return role switch
            {
                (byte)AskJoobyMessageRole.User => "User",
                (byte)AskJoobyMessageRole.Assistant => "Assistant",
                (byte)AskJoobyMessageRole.System => "System",
                _ => "Assistant"
            };
        }

        private static AskJoobyGuardPhase? MapGuardPhase(string? phase)
        {
            return Enum.TryParse<AskJoobyGuardPhase>(phase, true, out var parsed) ? parsed : null;
        }

        private static AskJoobyGuardAction? MapGuardAction(string? action)
        {
            return Enum.TryParse<AskJoobyGuardAction>(action, true, out var parsed) ? parsed : null;
        }

        private async Task<AskJoobyMessage> InsertMessageAsync(
            AskJoobyMessage model, CancellationToken token)
        {
            if (!embedAllMessages)
            {
                return await messageRepository.InsertAsync(model, token).ConfigureAwait(false);
            }

            var embeddingText = await EmbedAsync(model.Content ?? string.Empty, token).ConfigureAwait(false);

            return string.IsNullOrEmpty(embeddingText)
                ? await messageRepository.InsertAsync(model, token).ConfigureAwait(false)
                : await messageRepository.InsertWithEmbeddingAsync(model, embeddingText, token)
                    .ConfigureAwait(false);
        }

        private async Task<string> EmbedAsync(string text, CancellationToken token)
        {
            if (embeddingGenerator is null || string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var embedding = await embeddingGenerator.GenerateAsync(text, cancellationToken: token)
                .ConfigureAwait(false);

            return AskJoobyRagClient.ToPgVectorText(embedding.Vector.Span);
        }

        private void PushGuidanceBestEffort(string conversationKey, string text)
        {
            if (guidanceExchange is null)
            {
                return;
            }

            try
            {
                guidanceExchange.Send(conversationKey, text);
            }
            catch (Exception ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"AskJoobyConversation: best-effort guidance push failed id={conversationKey} " +
                             $"user={userName}", ex);
                }
            }
        }

        private async Task ScheduleCompactionAsync(int conversationId, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(connectionString) || embeddingGenerator is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("AskJoobyConversation.Compact: skipped, no connection string or embedding "
                             + $"generator configured id={conversationId} user={userName}");
                }

                return;
            }

            List<AskJoobyMessage> messages;

            try
            {
                messages = await messageRepository.ListMessagesByConversationAsync(conversationId, token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Warn($"AskJoobyConversation.Compact: could not read messages to decide compaction "
                         + $"id={conversationId} user={userName}", ex);

                return;
            }

            var lastCompact = messages.LastOrDefault(m => m.IsCompact == true);

            var sinceLastCompact = lastCompact is null
                ? messages
                : messages.Where(m => m.Id >= lastCompact.Id).ToList();

            if (sinceLastCompact.Count == 0
                || (sinceLastCompact.Count < CompactionTriggerMessageCount
                    && sinceLastCompact.Sum(m => m.Content?.Length ?? 0) < CompactionTriggerCharacterCount))
            {
                return;
            }

            var anchorMessageId = sinceLastCompact[^1].Id;
            var transcript = string.Join("\n\n", sinceLastCompact.Select(m => $"{RoleName(m.RoleId)}: {m.Content}"));
            var count = sinceLastCompact.Count;

            _ = Task.Run(async () =>
            {
                try
                {
                    await CompactAsync(conversationId, transcript, anchorMessageId, count)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"AskJoobyConversation.Compact: background compaction failed "
                              + $"id={conversationId} user={userName}", ex);
                }
            }, CancellationToken.None);
        }

        private async Task CompactAsync(int conversationId, string transcript, int anchorMessageId,
            int messageCount)
        {
            var token = CancellationToken.None;
            var stopwatch = Stopwatch.StartNew();

            await using var scopedDbContext = DataConnectionDbContext
                .GetResilientDbContextDataConnection(connectionString, log);

            var prompt = "Summarise the following conversation turns into a compact but complete brief " +
                         "that preserves every fact, decision, and open thread someone would need to " +
                         "continue this conversation -- plain prose, no preamble:\n\n" + transcript;

            var response = await chatClient
                .GetResponseAsync([new ChatMessage(ChatRole.User, prompt)], UtilityChatOptions(), token)
                .ConfigureAwait(false);

            var summary = response.Text.Trim();

            if (string.IsNullOrWhiteSpace(summary))
            {
                return;
            }

            var embeddingText = await EmbedAsync(summary, token).ConfigureAwait(false);
            var beforeEmbedding = await EmbedAsync(transcript, token).ConfigureAwait(false);

            await scopedDbContext.BeginTransactionAsync(token).ConfigureAwait(false);

            AskJoobyMessage compactMessage;

            try
            {
                compactMessage = await new AskJoobyMessageRepository(scopedDbContext).InsertWithEmbeddingAsync(
                    new AskJoobyMessage
                    {
                        AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.System,
                        Content = summary,
                        IsCompact = true, CompactedToAskJoobyMessageId = anchorMessageId
                    }, embeddingText, token).ConfigureAwait(false);

                await new AskJoobyCompactionRepository(scopedDbContext, tenantRegistryId, userName)
                    .InsertWithEmbeddingAsync(new AskJoobyCompaction
                    {
                        AskJoobyConversationId = conversationId,
                        AskJoobyMessageId = compactMessage.Id,
                        CompactedToAskJoobyMessageId = anchorMessageId,
                        MessageCount = messageCount,
                        BeforeCharacters = transcript.Length,
                        AfterCharacters = summary.Length,
                        Before = transcript,
                        After = summary,
                        DurationMs = stopwatch.ElapsedMilliseconds
                    }, beforeEmbedding, token).ConfigureAwait(false);

                await scopedDbContext.CommitTransactionAsync(token).ConfigureAwait(false);
            }
            catch
            {
                await scopedDbContext.RollbackTransactionAsync(token).ConfigureAwait(false);
                throw;
            }

            if (log.IsInfoEnabled)
            {
                log.Info($"AskJoobyConversation.Compact: compacted {messageCount} messages, "
                         + $"{transcript.Length} characters to {summary.Length}, anchor={anchorMessageId} "
                         + $"id={conversationId} user={userName}");
            }
        }

        private static string? BuildRoutingContext(List<AskJoobyMessage> priorMessages)
        {
            var questions = priorMessages
                .Where(message => message.RoleId == (byte)AskJoobyMessageRole.User)
                .TakeLast(RoutingContextTurns)
                .Select(message => message.Content)
                .Where(content => !string.IsNullOrWhiteSpace(content))
                .Select(content => content!.Length > RoutingContextTurnLength
                    ? content[..RoutingContextTurnLength]
                    : content)
                .ToList();

            return questions.Count == 0 ? null : string.Join("\n", questions.Select(q => $"- {q}"));
        }

        private ICompactedHistoryClient BuildCompactedHistoryClient(int conversationId)
        {
            return new AskJoobyCompactedHistoryClient(
                new SearchAskJoobyCompactionQuery(dbContext), tenantRegistryId, userName, conversationId,
                embeddingGenerator);
        }

        private IAutoSnapshot? BuildAutoSnapshot()
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            return new AskJoobyAutoSnapshot(connectionString, userName, log, stringLocalizerFactory,
                serviceChangeBus, dynamicEnvironment?.AppSettings("PreservationSalt"),
                "True".Equals(dynamicEnvironment?.AppSettings("JempFileLegacyEncryptionFallback"),
                    StringComparison.OrdinalIgnoreCase));
        }

        private RServerClient? BuildRServerClient()
        {
            if (dynamicEnvironment is null
                || "False".Equals(dynamicEnvironment.AppSettings("EnableAskJoobyR"),
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return RServerClient.FromSettings(dynamicEnvironment.AppSettings);
        }

        private IIsolatedRunScopeFactory? BuildIsolatedRunScopeFactory(int conversationId)
        {
            return string.IsNullOrWhiteSpace(connectionString)
                ? null
                : new AskJoobyIsolatedRunScopeFactory(connectionString, log, conversationId, tenantRegistryId,
                    userName, embeddingGenerator, permissionValidation.Validate,
                    memoryDecayHalfLifeDays, ragReranker, ragCandidateCount, rerankMinScore,
                    promotionBudgetCharacters, ragMaxCandidateCharacters, stringLocalizerFactory, serviceChangeBus,
                    BuildRServerClient());
        }

        private static async IAsyncEnumerable<AgentStreamItem> MergeDelegatedStreamAsync(
            IRunnableAgent runnable, IReadOnlyList<ChatMessage> chatHistory,
            Channel<AgentStreamItem> channel,
            [EnumeratorCancellation] CancellationToken token)
        {
            var pumpTask = Task.Run(async () =>
            {
                try
                {
                    await foreach (var item in runnable.RunStreamingAsync(chatHistory, token))
                    {
                        await channel.Writer.WriteAsync(item, token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    await channel.Writer.WriteAsync(
                            new AgentStreamItem(AgentStreamItemKind.Error, ex.Message), CancellationToken.None)
                        .ConfigureAwait(false);
                }
                finally
                {
                    channel.Writer.TryComplete();
                }
            }, token);

            await foreach (var item in channel.Reader.ReadAllAsync(token))
            {
                yield return item;
            }

            await pumpTask.ConfigureAwait(false);
        }

        private static byte ToEntryType(AgentStreamItemKind kind)
        {
            return kind switch
            {
                AgentStreamItemKind.Step => AskJoobyMessageRepository.EntryTypeStep,
                AgentStreamItemKind.Thought => AskJoobyMessageRepository.EntryTypeThought,
                AgentStreamItemKind.Thinking => AskJoobyMessageRepository.EntryTypeThinking,
                AgentStreamItemKind.Delegation => AskJoobyMessageRepository.EntryTypeDelegation,
                AgentStreamItemKind.Handoff => AskJoobyMessageRepository.EntryTypeHandoff,
                _ => AskJoobyMessageRepository.EntryTypeMessage
            };
        }

        private Task<Dictionary<int, RunStats>> BuildRunStatsAsync(int conversationId,
            List<AskJoobyMessage> entries, CancellationToken token)
        {
            var rootRunIds = entries.Where(w => w.AskJoobyRunId.HasValue).Select(w => w.AskJoobyRunId!.Value)
                .Distinct().ToList();

            return BuildRunStatsAsync(conversationId, rootRunIds, token);
        }

        private Task PersistTurnCostAsync(int rootRunId, RunStats stats, CancellationToken token)
        {
            if (!EnableAskJoobyCostBanner)
            {
                return Task.CompletedTask;
            }

            return turnCostRepository.InsertAsync(new AskJoobyTurnCost
            {
                AskJoobyRunId = rootRunId,
                InputTokens = stats.InputTokens,
                CachedInputTokens = stats.CachedInputTokens,
                OutputTokens = stats.OutputTokens,
                EmbeddingInputTokens = stats.EmbeddingInputTokens,
                RerankDocumentsCompared = stats.RerankDocumentsCompared,
                JoobrikCount = stats.JoobrikCount,
                DistinctJoobrikCount = stats.DistinctJoobrikCount,
                ChatCost = stats.ChatCost,
                EmbeddingCost = stats.EmbeddingCost,
                RerankCost = stats.RerankCost,
                TotalCost = stats.TotalCost
            }, token);
        }

        private async Task<Dictionary<int, RunStats>> BuildRunStatsAsync(int conversationId,
            IReadOnlyCollection<int> rootRunIds, CancellationToken token)
        {
            if (rootRunIds.Count == 0)
            {
                return new Dictionary<int, RunStats>();
            }

            var allRuns = await runRepository.ListByConversationAsync(conversationId, token).ConfigureAwait(false);
            var runById = allRuns.ToDictionary(r => r.Id);

            var childrenByParent = allRuns.Where(w => w.ParentAskJoobyRunId.HasValue)
                .GroupBy(g => g.ParentAskJoobyRunId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(s => s.Id).ToList());

            var showCost = EnableAskJoobyCostBanner;

            var endpointFeesByKind = showCost
                ? (await endpointFeeRepository.ListAsync(token).ConfigureAwait(false))
                .GroupBy(fee => fee.EndpointKindId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<AskJoobyEndpointFee>)g.ToList())
                : null;

            var rerankFeesByModelType = showCost
                ? (await rerankFeeRepository.ListAsync(token).ConfigureAwait(false))
                .ToDictionary(fee => fee.ModelTypeId)
                : null;

            var result = new Dictionary<int, RunStats>();

            foreach (var rootId in rootRunIds)
            {
                var treeIds = CollectRunTree(rootId, childrenByParent);

                var (inputTokens, outputTokens, cachedInputTokens) = await modelCallRepository
                    .GetTotalsByRunIdsAsync(treeIds, token).ConfigureAwait(false);

                var durationMs = runById.TryGetValue(rootId, out var rootRun) ? rootRun.DurationMs : null;

                int? joobrikCount = null;
                int? distinctJoobrikCount = null;
                int? embeddingInputTokens = null;
                int? rerankDocumentsCompared = null;
                double? chatCost = null;
                double? embeddingCost = null;
                double? rerankCost = null;
                double? totalCost = null;

                if (showCost)
                {
                    joobrikCount = treeIds.Count;

                    distinctJoobrikCount = treeIds
                        .Select(id => runById.TryGetValue(id, out var run) ? run.AgentType : null)
                        .Where(agentType => agentType is not null)
                        .Distinct(StringComparer.Ordinal)
                        .Count();

                    var modelCalls = await modelCallRepository.ListByRunIdsAsync(treeIds, token)
                        .ConfigureAwait(false);
                    var embeddingCalls = await embeddingCallRepository.ListByRunIdsAsync(treeIds, token)
                        .ConfigureAwait(false);
                    var rerankCalls = await rerankCallRepository.ListByRunIdsAsync(treeIds, token)
                        .ConfigureAwait(false);

                    embeddingInputTokens = embeddingCalls.Sum(call => call.InputTokens ?? 0);
                    rerankDocumentsCompared = rerankCalls.Sum(call => call.DocumentsCompared ?? 0);

                    var anyRateMissing = false;

                    if (modelCalls.Count > 0)
                    {
                        chatCost = PriceModelCalls(modelCalls, endpointFeesByKind!);
                        anyRateMissing |= chatCost is null;
                    }

                    if (embeddingCalls.Count > 0)
                    {
                        embeddingCost = PriceEmbeddingCalls(embeddingCalls, endpointFeesByKind!);
                        anyRateMissing |= embeddingCost is null;
                    }

                    if (rerankCalls.Count > 0)
                    {
                        rerankCost = PriceRerankCalls(rerankCalls, rerankFeesByModelType!);
                        anyRateMissing |= rerankCost is null;
                    }

                    totalCost = anyRateMissing
                        ? null
                        : (chatCost ?? 0) + (embeddingCost ?? 0) + (rerankCost ?? 0);
                }

                result[rootId] = new RunStats(inputTokens, outputTokens, durationMs,
                    ShowPromptCacheShare ? cachedInputTokens : null, joobrikCount, distinctJoobrikCount,
                    embeddingInputTokens, rerankDocumentsCompared, chatCost, embeddingCost, rerankCost, totalCost);
            }

            return result;
        }

        private static bool TryFindBand(
            IReadOnlyDictionary<byte, IReadOnlyList<AskJoobyEndpointFee>> feesByKind, AskJoobyEndpointKind kind,
            DateTime? callTime, [NotNullWhen(true)] out AskJoobyEndpointFee? band)
        {
            band = null;

            if (callTime is null || !feesByKind.TryGetValue((byte)kind, out var bands))
            {
                return false;
            }

            var time = callTime.Value.TimeOfDay;

            var dayMatches = new Func<AskJoobyEndpointFee, bool>(candidate => callTime.Value.DayOfWeek switch
            {
                DayOfWeek.Monday => candidate.Monday,
                DayOfWeek.Tuesday => candidate.Tuesday,
                DayOfWeek.Wednesday => candidate.Wednesday,
                DayOfWeek.Thursday => candidate.Thursday,
                DayOfWeek.Friday => candidate.Friday,
                DayOfWeek.Saturday => candidate.Saturday,
                DayOfWeek.Sunday => candidate.Sunday,
                _ => false
            });

            foreach (var candidate in bands)
            {
                if (!dayMatches(candidate))
                {
                    continue;
                }

                var inWindow = candidate.StartTimeOfDay <= candidate.EndTimeOfDay
                    ? time >= candidate.StartTimeOfDay && time <= candidate.EndTimeOfDay
                    : time >= candidate.StartTimeOfDay || time <= candidate.EndTimeOfDay;

                if (inWindow)
                {
                    band = candidate;

                    return true;
                }
            }

            return false;
        }

        internal static double? PriceModelCalls(
            IReadOnlyCollection<AskJoobyModelCall> calls,
            IReadOnlyDictionary<byte, IReadOnlyList<AskJoobyEndpointFee>> fees)
        {
            double total = 0;

            foreach (var call in calls)
            {
                if (!TryFindBand(fees, AskJoobyEndpointKind.Chat, call.CreatedDate, out var band))
                {
                    return null;
                }

                var cached = call.CachedInputTokens ?? 0;
                var uncached = (call.InputTokens ?? 0) - cached;
                var output = call.OutputTokens ?? 0;

                total += uncached / 1_000_000.0 * band.RateInputPerMillionTokens;

                total += cached / 1_000_000.0 *
                         (band.RateCachedInputPerMillionTokens ?? band.RateInputPerMillionTokens);

                total += output / 1_000_000.0 * (band.RateOutputPerMillionTokens ?? 0);
            }

            return total;
        }

        internal static double? PriceEmbeddingCalls(
            IReadOnlyCollection<AskJoobyEmbeddingCall> calls,
            IReadOnlyDictionary<byte, IReadOnlyList<AskJoobyEndpointFee>> fees)
        {
            double total = 0;

            foreach (var call in calls)
            {
                if (!TryFindBand(fees, AskJoobyEndpointKind.Embedding, call.CreatedDate, out var band))
                {
                    return null;
                }

                total += (call.InputTokens ?? 0) / 1_000_000.0 * band.RateInputPerMillionTokens;
            }

            return total;
        }

        internal static double? PriceRerankCalls(
            IReadOnlyCollection<AskJoobyRerankCall> calls, IReadOnlyDictionary<byte, AskJoobyRerankFee> fees)
        {
            double total = 0;

            foreach (var call in calls)
            {
                if (call.ModelTypeId is not { } modelTypeId || !fees.TryGetValue(modelTypeId, out var fee))
                {
                    return null;
                }

                if (call.TokensCompared is { } tokens && fee.RatePerMillionTokensCompared is { } tokenRate)
                {
                    total += tokens / 1_000_000.0 * tokenRate;
                }
                else if (call.DocumentsCompared is { } documents && fee.RatePerDocumentCompared is { } documentRate)
                {
                    total += documents * documentRate;
                }
                else
                {
                    return null;
                }
            }

            return total;
        }

        private static List<int> CollectRunTree(int rootId, IReadOnlyDictionary<int, List<int>> childrenByParent)
        {
            var result = new List<int> { rootId };
            var stack = new Stack<int>();
            stack.Push(rootId);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (!childrenByParent.TryGetValue(current, out var children))
                {
                    continue;
                }

                foreach (var child in children)
                {
                    result.Add(child);
                    stack.Push(child);
                }
            }

            return result;
        }

        private async Task<string> BuildToolEvidenceAsync(int conversationId, int rootRunId, CancellationToken token)
        {
            var allRuns = await runRepository.ListByConversationAsync(conversationId, token).ConfigureAwait(false);

            var childrenByParent = allRuns.Where(w => w.ParentAskJoobyRunId.HasValue)
                .GroupBy(g => g.ParentAskJoobyRunId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(s => s.Id).ToList());

            var treeIds = CollectRunTree(rootRunId, childrenByParent);

            var toolCalls = await toolCallRepository.ListByRunIdsAsync(treeIds, token).ConfigureAwait(false);

            var evidence = toolCalls.Select(call =>
                new ToolCallEvidence(call.ToolName, call.Arguments, call.Result));

            return ToolEvidence.Compact(evidence);
        }

        private async Task<Dictionary<int, List<AskJoobyMessageAttachmentDto>>> BuildAttachmentsByMessageIdAsync(
            List<AskJoobyMessage> entries, CancellationToken token)
        {
            var messageIds = entries.Select(e => e.Id).ToList();
            var files = await fileRepository.ListByMessageIdsAsync(messageIds, token).ConfigureAwait(false);

            return files.Where(f => f.AskJoobyMessageId.HasValue)
                .GroupBy(f => f.AskJoobyMessageId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(f => new AskJoobyMessageAttachmentDto
                {
                    Id = f.Id, Name = f.Name ?? "file"
                }).ToList());
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[AskJoobyConversationResources.PermissionDenied], permissions);
        }

        private sealed class SubAgentOutcome
        {
            public string? Result { get; set; }
            public string? Error { get; set; }
        }

        private sealed class AskJoobyIsolatedRunScopeFactory(
            string connectionString,
            ILog log,
            int? conversationId,
            int tenantRegistryId,
            string userName,
            IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator,
            Func<int[], bool> hasPermission,
            double memoryDecayHalfLifeDays = 30,
            IRagReranker? ragReranker = null,
            int ragCandidateCount = 0,
            double rerankMinScore = 0,
            int promotionBudgetCharacters = 0,
            int ragMaxCandidateCharacters = 9000,
            IStringLocalizerFactory? stringLocalizerFactory = null,
            IServiceChangeBus? serviceChangeBus = null,
            RServerClient? rServer = null) : IIsolatedRunScopeFactory
        {
            public async Task<IIsolatedRunScope> CreateAsync(CancellationToken token = default)
            {
                var scopedDbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                    connectionString, log);

                var scopedGuardRejectionRepository = new AskJoobyAgentGuardRejectionRepository(scopedDbContext);

                var ragClient = new AskJoobyRagClient(
                    new SearchAskJoobyRagChunkQuery(scopedDbContext), embeddingGenerator, ragReranker,
                    new GetAskJoobyRagChunkByHashQuery(scopedDbContext),
                    new GetAskJoobyRagChunkByParentHashQuery(scopedDbContext), ragCandidateCount, rerankMinScore,
                    promotionBudgetCharacters, ragMaxCandidateCharacters);

                var memoryClient = new AskJoobyMemoryClient(
                    new AskJoobyMemoryRepository(scopedDbContext, tenantRegistryId, userName),
                    new SearchAskJoobyMemoryQuery(scopedDbContext, memoryDecayHalfLifeDays), tenantRegistryId,
                    userName, embeddingGenerator, conversationId, scopedGuardRejectionRepository);

                var historyClient = new AskJoobyConversationHistoryClient(
                    new SearchAskJoobyConversationHistoryQuery(scopedDbContext), tenantRegistryId, userName,
                    embeddingGenerator);

                var recorder = new AskJoobyRunRecorder(
                    new AskJoobyRunRepository(scopedDbContext), new AskJoobyToolCallRepository(scopedDbContext),
                    new AskJoobyModelCallRepository(scopedDbContext),
                    new AskJoobyEmbeddingCallRepository(scopedDbContext),
                    new AskJoobyRerankCallRepository(scopedDbContext),
                    new AskJoobyWebSearchCallRepository(scopedDbContext),
                    new AskJoobyOpenCorporatesCallRepository(scopedDbContext), conversationId, tenantRegistryId,
                    userName);

                var sqlQueryClient = new AskJoobySqlQueryClient(
                    connectionString, scopedDbContext, log, userName, hasPermission, conversationId);

                var cacheClient = new AskJoobyDataCacheClient(
                    new AskJoobyDataCacheRepository(scopedDbContext, tenantRegistryId, userName), conversationId,
                    advancedAnalyticsCache: new AdvancedAnalyticsCachedTableReader(scopedDbContext, tenantRegistryId,
                        userName));

                var exportClient = new AskJoobyFileExportClient(
                    new AskJoobyFileRepository(scopedDbContext, tenantRegistryId, userName), conversationId);

                var workflowProgressClient = conversationId is { } scopedConversationId
                    ? new AskJoobyWorkflowProgressClient(
                        new GetAskJoobyWorkflowProgressQuery(scopedDbContext, scopedConversationId),
                        new AskJoobyWorkflowStepRepository(scopedDbContext, tenantRegistryId, userName),
                        scopedConversationId)
                    : null;

                var guardRejectionLedger = new AskJoobyPersistedGuardRejectionLedger(
                    scopedGuardRejectionRepository, conversationId, tenantRegistryId, userName);

                IScriptExecutionClient? scriptExecutionClient = null;

                if (rServer is not null && stringLocalizerFactory is not null && serviceChangeBus is not null)
                {
                    var scriptExecutionService = await ScriptExecutionService.CreateAsync(scopedDbContext, userName,
                            log, stringLocalizerFactory, serviceChangeBus, rServer, cacheClient, token)
                        .ConfigureAwait(false);

                    scriptExecutionClient =
                        new AskJoobyScriptExecutionClient(new ScriptExecutionGateway(scriptExecutionService));
                }

                return new AskJoobyIsolatedRunScope(scopedDbContext, ragClient, memoryClient, historyClient,
                    recorder, sqlQueryClient, cacheClient, exportClient, workflowProgressClient,
                    scriptExecutionClient, guardRejectionLedger);
            }
        }

        private sealed class AskJoobyIsolatedRunScope(
            DbContext dbContext,
            IRagClient rag,
            IMemoryClient memory,
            IConversationHistoryClient history,
            IRunRecorder recorder,
            ISqlQueryClient sqlQuery,
            IToolResultCache cache,
            IFileExportClient export,
            IWorkflowProgressClient? workflowProgress,
            IScriptExecutionClient? scriptExecution,
            IAgentGuardRejectionLedger guardRejectionLedger) : IIsolatedRunScope
        {
            public IRagClient Rag { get; } = rag;
            public IMemoryClient Memory { get; } = memory;
            public IConversationHistoryClient History { get; } = history;
            public IRunRecorder Recorder { get; } = recorder;
            public ISqlQueryClient SqlQuery { get; } = sqlQuery;
            public IToolResultCache Cache { get; } = cache;
            public IFileExportClient Export { get; } = export;
            public IWorkflowProgressClient? WorkflowProgress { get; } = workflowProgress;
            public IScriptExecutionClient? ScriptExecution { get; } = scriptExecution;
            public IAgentGuardRejectionLedger GuardRejectionLedger { get; } = guardRejectionLedger;

            public ValueTask DisposeAsync()
            {
                return dbContext.DisposeAsync();
            }
        }
    }
}
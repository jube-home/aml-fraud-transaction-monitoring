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
using System.IO.Compression;
using System.Text;
using Jube.AskJooby.Reasoning;
using Jube.Data.Context;
using Jube.Data.Query.GetAskJoobyRagChunkByQualifiedNameQuery;
using Jube.Data.Query.GetAskJoobyWorkflowProgressQuery;
using Jube.Data.Query.SearchAskJoobyRagChunkQuery;
using Jube.Data.Repository;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.AskJoobyRagChunk;
using Jube.Service.AskJoobyRagChunk.Interfaces;
using Jube.Service.Exceptions.AskJoobyConversation;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;

namespace Jube.Service.AskJoobyConversation
{
    public sealed class AskJoobyChatAuditService
    {
        private readonly IChatClient? analysisChatClient;
        private readonly int analysisCodeBudgetCharacters;
        private readonly bool analysisEnabled;
        private readonly int analysisThinkingEffort;
        private readonly bool analysisThinkingEnabled;
        private readonly int analysisTimeoutSeconds;
        private readonly ILog auditLog;
        private readonly AskJoobyCompactionRepository compactionRepository;
        private readonly AskJoobyConversationRepository conversationRepository;
        private readonly AskJoobyDataCacheRepository dataCacheRepository;
        private readonly DbContext dbContext;
        private readonly DynamicSqlQueryLogRepository dynamicSqlQueryLogRepository;
        private readonly IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator;
        private readonly AskJoobyFileRepository fileRepository;
        private readonly AskJoobyAgentGuardRejectionRepository guardRejectionRepository;
        private readonly ILog log;
        private readonly AskJoobyMemoryRepository memoryRepository;
        private readonly AskJoobyMessageRepository messageRepository;
        private readonly AskJoobyModelCallRepository modelCallRepository;
        private readonly AskJoobyOpenCorporatesCallRepository openCorporatesCallRepository;
        private readonly PermissionValidation permissionValidation;
        private readonly IRagReranker? ragReranker;
        private readonly AskJoobyRoutingRepository routingRepository;
        private readonly AskJoobyRunRepository runRepository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly AskJoobyToolCallRepository toolCallRepository;
        private readonly string userName;
        private readonly AskJoobyWebSearchCallRepository webSearchCallRepository;

        private AskJoobyChatAuditService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, IChatClient? analysisChatClient,
            IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator, IRagReranker? ragReranker,
            DynamicEnvironment.DynamicEnvironment? dynamicEnvironment)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.analysisChatClient = analysisChatClient;
            this.embeddingGenerator = embeddingGenerator;
            this.ragReranker = ragReranker;

            analysisEnabled = !"False".Equals(dynamicEnvironment?.AppSettings("AskJoobyEnableAiAnalysis"),
                StringComparison.OrdinalIgnoreCase);

            analysisTimeoutSeconds = int.TryParse(
                                         dynamicEnvironment?.AppSettings("AskJoobyAiAnalysisTimeoutSeconds"),
                                         out var parsedTimeoutSeconds)
                                     && parsedTimeoutSeconds > 0
                ? parsedTimeoutSeconds
                : 60;

            analysisThinkingEnabled = "True".Equals(
                dynamicEnvironment?.AppSettings("AskJoobyAiAnalysisEnableThinking"),
                StringComparison.OrdinalIgnoreCase);

            analysisThinkingEffort = int.TryParse(
                dynamicEnvironment?.AppSettings("AskJoobyAiAnalysisThinkingEffort"), out var parsedThinkingEffort)
                ? parsedThinkingEffort
                : 0;

            analysisCodeBudgetCharacters = int.TryParse(
                dynamicEnvironment?.AppSettings("AskJoobyAiAnalysisCodeBudgetCharacters"),
                out var parsedCodeBudgetCharacters) && parsedCodeBudgetCharacters > 0
                ? parsedCodeBudgetCharacters
                : 16000;

            conversationRepository = new AskJoobyConversationRepository(dbContext, tenantRegistryId, userName);
            messageRepository = new AskJoobyMessageRepository(dbContext);
            routingRepository = new AskJoobyRoutingRepository(dbContext);
            runRepository = new AskJoobyRunRepository(dbContext);
            toolCallRepository = new AskJoobyToolCallRepository(dbContext);
            modelCallRepository = new AskJoobyModelCallRepository(dbContext);
            memoryRepository = new AskJoobyMemoryRepository(dbContext, tenantRegistryId, userName);
            dataCacheRepository = new AskJoobyDataCacheRepository(dbContext, tenantRegistryId, userName);
            compactionRepository = new AskJoobyCompactionRepository(dbContext, tenantRegistryId, userName);
            dynamicSqlQueryLogRepository = new DynamicSqlQueryLogRepository(dbContext);
            fileRepository = new AskJoobyFileRepository(dbContext, tenantRegistryId, userName);
            guardRejectionRepository = new AskJoobyAgentGuardRejectionRepository(dbContext);
            webSearchCallRepository = new AskJoobyWebSearchCallRepository(dbContext);
            openCorporatesCallRepository = new AskJoobyOpenCorporatesCallRepository(dbContext);
        }

        private ChatOptions AnalysisChatOptions()
        {
            return analysisThinkingEnabled
                ? new ChatOptions { RawRepresentationFactory = _ => ThinkingOptions.RawOptions(analysisThinkingEffort) }
                : new ChatOptions { RawRepresentationFactory = _ => ThinkingOptions.DisabledOptions() };
        }

        private static string BuildFileName(Data.Poco.AskJoobyConversation conversation, DateTime generatedAtUtc)
        {
            var guidPart = (conversation.Guid ?? Guid.NewGuid()).ToString("N");
            var descriptionPart = SanitizeForFileName(conversation.Title);
            var datePart = generatedAtUtc.ToString("yyyyMMddTHHmmssZ");

            return string.IsNullOrEmpty(descriptionPart)
                ? $"{guidPart}-ChatAudit-{datePart}.zip"
                : $"{guidPart}-{descriptionPart}-{datePart}.zip";
        }

        private static byte[] BuildZip(string markdown, byte[] manifest, byte[] health, byte[] ragGaps,
            byte[] ragPerformance, byte[] contextEfficiency, string? aiAnalysis,
            IReadOnlyList<AuditArtifactFile> files)
        {
            using var zipStream = new MemoryStream();

            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                WriteZipEntry(archive, "technical-log.md", Encoding.UTF8.GetBytes(markdown));
                WriteZipEntry(archive, "manifest.json", manifest);
                WriteZipEntry(archive, "health.json", health);
                WriteZipEntry(archive, "rag-gaps.json", ragGaps);
                WriteZipEntry(archive, "rag-performance.json", ragPerformance);
                WriteZipEntry(archive, "context-efficiency.json", contextEfficiency);

                if (aiAnalysis is not null)
                {
                    var withHeader = "# AI Analysis\n\n_Generated automatically by an LLM from the rule-based " +
                                     "reports above -- it can be wrong; verify before acting on it._\n\n" +
                                     aiAnalysis;

                    WriteZipEntry(archive, "ai-analysis.md", Encoding.UTF8.GetBytes(withHeader));
                }

                foreach (var file in files)
                {
                    WriteZipEntry(archive, file.RelativePath, file.Content);
                }
            }

            return zipStream.ToArray();
        }

        private static void WriteZipEntry(ZipArchive archive, string relativePath, byte[] content)
        {
            var entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(content, 0, content.Length);
        }

        private static string SanitizeForFileName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            foreach (var c in value.Trim())
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else if (sb.Length > 0 && sb[^1] != '-')
                {
                    sb.Append('-');
                }
            }

            var result = sb.ToString().Trim('-');

            return result.Length > 60 ? result[..60] : result;
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Landlord)
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied (landlord only) user={userName} landlord="
                         + $"{permissionValidation.Landlord}");
            }

            throw new ForbiddenException(strings[AskJoobyConversationResources.PermissionDenied], []);
        }

        public static Task<AskJoobyChatAuditService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default, IChatClient? analysisChatClient = null,
            IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator = null,
            IRagReranker? ragReranker = null, DynamicEnvironment.DynamicEnvironment? dynamicEnvironment = null)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token, analysisChatClient, embeddingGenerator, ragReranker,
                dynamicEnvironment);
        }

        internal static async Task<AskJoobyChatAuditService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default, IChatClient? analysisChatClient = null,
            IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator = null,
            IRagReranker? ragReranker = null, DynamicEnvironment.DynamicEnvironment? dynamicEnvironment = null)
        {
            var strings = stringLocalizerFactory.Create(typeof(AskJoobyConversationResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("AskJoobyChatAudit.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[AskJoobyConversationResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"AskJoobyChatAudit.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[AskJoobyConversationResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new AskJoobyChatAuditService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, analysisChatClient,
                embeddingGenerator, ragReranker, dynamicEnvironment);
        }

        [Description("Renders one AskJooby conversation's complete background-table footprint (transcript, " +
                     "runs, tool calls, model calls, SQL log, memories, cached results, files) as a zip " +
                     "bundle -- a lean technical-log.md (with no-model-call Health Signals, RAG & Tooling " +
                     "Gap Analysis, RAG Performance, and Context Efficiency sections), a manifest.json, a " +
                     "health.json, a rag-gaps.json, a rag-performance.json, a context-efficiency.json, an " +
                     "optional ai-analysis.md (one bounded LLM call over the reports above, grounded in the " +
                     "involved agents' own instructions and tools, omitted if that call fails or is " +
                     "disabled), and per-artifact files for tool-call payloads, cached query results, " +
                     "compactions, and external-API responses -- and returns it with a ready-to-use " +
                     "download file name.")]
        [ServiceOperation("AskJoobyChatAuditGenerate", OperationKind.Read, Idempotent = true)]
        public async Task<(byte[] Zip, string FileName)> GenerateAsync(
            [Description("The conversation's Id.")]
            int conversationId, CancellationToken token = default)
        {
            using var op = OperationScope.Start("AskJoobyChatAudit", "Generate", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("AskJoobyChatAudit.Generate");

                var conversation = await conversationRepository.GetByIdAsync(conversationId, token)
                    .ConfigureAwait(false);

                if (conversation is null)
                {
                    throw new NotFoundException(strings[AskJoobyConversationResources.NotFound]);
                }

                if (!string.Equals(conversation.CreatedUser, userName, StringComparison.OrdinalIgnoreCase))
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"AskJoobyChatAudit.Generate: 403 (not the creator) id={conversationId} " +
                                 $"user={userName} createdUser={conversation.CreatedUser}");
                    }

                    throw new ForbiddenException(strings[AskJoobyConversationResources.PermissionDenied], []);
                }

                var entries = await messageRepository.ListEntriesByConversationAsync(conversationId, token)
                    .ConfigureAwait(false);
                var runs = await runRepository.ListByConversationAsync(conversationId, token).ConfigureAwait(false);
                var runIds = runs.Select(r => r.Id).ToList();
                var toolCalls = await toolCallRepository.ListByRunIdsAsync(runIds, token).ConfigureAwait(false);
                var modelCalls = await modelCallRepository.ListByRunIdsAsync(runIds, token).ConfigureAwait(false);
                var memories = await memoryRepository.ListByConversationAsync(conversationId, token)
                    .ConfigureAwait(false);
                var dataCacheEntries = await dataCacheRepository.ListByConversationAsync(conversationId, token)
                    .ConfigureAwait(false);
                var sqlLog = await dynamicSqlQueryLogRepository.ListByConversationAsync(conversationId, token)
                    .ConfigureAwait(false);
                var files = await fileRepository.ListByConversationAsync(conversationId, token)
                    .ConfigureAwait(false);
                var compactions = await compactionRepository.ListByConversationAsync(conversationId, token)
                    .ConfigureAwait(false);
                var routings = await routingRepository.ListByConversationAsync(conversationId, token)
                    .ConfigureAwait(false);

                var workflowSteps = await new GetAskJoobyWorkflowProgressQuery(dbContext, conversationId)
                    .ExecuteAuditAsync(token).ConfigureAwait(false);

                var agentGuardRejections = await guardRejectionRepository
                    .ListByConversationAsync(conversationId, token).ConfigureAwait(false);

                var webSearchCalls = await webSearchCallRepository.ListByRunIdsAsync(runIds, token)
                    .ConfigureAwait(false);
                var openCorporatesCalls = await openCorporatesCallRepository.ListByRunIdsAsync(runIds, token)
                    .ConfigureAwait(false);

                var generatedAtUtc = DateTime.UtcNow;

                var markdown = AskJoobyChatAuditMarkdownBuilder.Build(conversation, entries, runs, toolCalls,
                    modelCalls, memories, dataCacheEntries, sqlLog, files, compactions, routings, workflowSteps,
                    agentGuardRejections, webSearchCalls, openCorporatesCalls, generatedAtUtc, userName);

                var artifactFiles = AskJoobyChatAuditBundleBuilder.BuildArtifactFiles(toolCalls, dataCacheEntries,
                    compactions, webSearchCalls, openCorporatesCalls);

                var manifest = AskJoobyChatAuditBundleBuilder.BuildManifest(conversation, generatedAtUtc, userName,
                    toolCalls, dataCacheEntries, compactions, webSearchCalls, openCorporatesCalls);

                var health = AskJoobyChatAuditHealthSignals.BuildHealthJson(runs, toolCalls, modelCalls, routings,
                    agentGuardRejections);

                var ragGaps = AskJoobyChatAuditRagGapSignals.BuildRagGapJson(entries, toolCalls);
                var ragPerformance = AskJoobyChatAuditRagPerformance.BuildRagPerformanceJson(toolCalls);

                var contextEfficiency = AskJoobyChatAuditContextEfficiency.BuildContextEfficiencyJson(runs,
                    toolCalls, modelCalls, compactions);

                string? aiAnalysis = null;

                if (analysisEnabled && analysisChatClient is not null)
                {
                    var agentTypes = runs.Select(r => r.AgentType).Where(t => !string.IsNullOrWhiteSpace(t))
                        .Distinct();

                    var orchestrationCode = await AskJoobyChatAuditCodeGrounding.BuildOrchestrationCodeAsync(
                            new GetAskJoobyRagChunkByQualifiedNameQuery(dbContext), analysisCodeBudgetCharacters, token)
                        .ConfigureAwait(false);

                    var ragClient = new AskJoobyRagClient(
                        new SearchAskJoobyRagChunkQuery(dbContext), embeddingGenerator, ragReranker);

                    var agentCode = await AskJoobyChatAuditCodeGrounding.BuildAgentCodeAsync(
                        ragClient, agentTypes, analysisCodeBudgetCharacters, token).ConfigureAwait(false);

                    var prompt = AskJoobyChatAuditAiAnalysis.BuildPrompt(conversation.Title ?? "(untitled)", runs,
                        health, ragGaps, ragPerformance, contextEfficiency, orchestrationCode, agentCode);

                    var outcome = await AskJoobyChatAuditAiAnalysis
                        .AnalyzeAsync(analysisChatClient, prompt, analysisTimeoutSeconds, AnalysisChatOptions(), log,
                            token)
                        .ConfigureAwait(false);

                    aiAnalysis = outcome.Succeeded
                        ? outcome.Text
                        : $"**AI analysis could not be completed.**\n\n{outcome.FailureReason}\n\nCheck " +
                          "`AskJoobyAiAnalysisModel`, `AskJoobyAiAnalysisApiEndpoint`, and " +
                          "`AskJoobyAiAnalysisApiKey` if this persists.";
                }

                var zip = BuildZip(markdown, manifest, health, ragGaps, ragPerformance, contextEfficiency,
                    aiAnalysis, artifactFiles);

                var fileName = BuildFileName(conversation, generatedAtUtc);

                op.Entity(conversationId);
                op.Rows(entries.Count);

                return (zip, fileName);
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
                log.Error($"AskJoobyChatAudit.Generate: unexpected failure id={conversationId} user={userName}", ex);
                throw;
            }
        }
    }
}
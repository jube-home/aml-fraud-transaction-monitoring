/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Jube.DynamicEnvironment.Logging;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace Jube.DynamicEnvironment
{
    public class DynamicEnvironment
    {
        private readonly Dictionary<string, string> appSettings;

        public DynamicEnvironment()
        {
            appSettings = new Dictionary<string, string>
            {
                {
                    "ModelSynchronisationWait", "10000"
                },
                {
                    "EnableNotification", "True"
                },
                {
                    "EnableTtlCounter", "True"
                },
                {
                    "ConnectionString", null
                },
                {
                    "ReportConnectionString", null
                },
                {
                    "MigrationConnectionString", null
                },
                {
                    "PurgeConnectionString", null
                },
                {
                    "PgPoolClearDebounceMilliseconds", "5000"
                },
                {
                    "PgMaxConnectionRetries", "10"
                },
                {
                    "EnableSearchKeyCache", "True"
                },
                {
                    "EnableCasesAutomation", "True"
                },
                {
                    "CasesAutomationWait", "60000"
                },
                {
                    "EnableCaseEscalation", "True"
                },
                {
                    "CaseEscalationWait", "30000"
                },
                {
                    "CaseEscalationBatchSize", "200"
                },
                {
                    "EnableCaseRisk", "True"
                },
                {
                    "CaseRiskWait", "30000"
                },
                {
                    "CaseRiskSweepMinutes", "15"
                },
                {
                    "CaseRiskBatchSize", "200"
                },
                {
                    "CaseRiskKappa", "0.7"
                },
                {
                    "CaseRiskRho", "0.3"
                },
                {
                    "CaseRiskPriorStrength", "20"
                },
                {
                    "CaseRiskBaseRatePrior", "0.08"
                },
                {
                    "CaseRiskDeadlineLaneDays", "2"
                },
                {
                    "CaseRiskKeyHistoryHalfLifeDays", "180"
                },
                {
                    "CaseRiskImpactWeights", "1,3,10,30,100"
                },
                {
                    "CaseRiskMoneyBands", "5000,100000,1000000,10000000"
                },
                {
                    "CaseRiskQaSampleRate", "0.05"
                },
                {
                    "CaseRiskQaMaxPriority", "25"
                },
                {
                    "EnableEntityModel", "True"
                },
                {
                    "ArchiverPersistThreads", "4"
                },
                {
                    "ModelInvokeAsynchronousThreads", "1"
                },
                {
                    "BulkCopyThreshold", "1000"
                },
                {
                    "CaseCreationWarnThresholdMilliseconds", "10000"
                },
                {
                    "ArchiverWarnThresholdMilliseconds", "10000"
                },
                {
                    "ActivationWatcherBulkCopyThreshold", "100"
                },
                {
                    "ReprocessingThreads", "1"
                },
                {
                    "ThreadPoolManualControl", "False"
                },
                {
                    "MinThreadPoolThreads", "30"
                },
                {
                    "MaxThreadPoolThreads", "1000"
                },
                {
                    "MaximumModelInvokeAsyncQueue", "10000"
                },
                {
                    "SMTPHost", null
                },
                {
                    "SMTPPort", "587"
                },
                {
                    "SMTPUser", null
                },
                {
                    "SMTPPassword", null
                },
                {
                    "SMTPFrom", null
                },
                {
                    "ClickatellAPIKey", null
                },
                {
                    "HttpAdaptationUrl", "http://localhost:5001"
                },
                {
                    "HttpAdaptationTimeout", "1000"
                },
                {
                    "HttpAdaptationValidateSsl", "False"
                },
                {
                    "ReprocessingBulkLimit", "10000"
                },
                {
                    "EnableMigration", "True"
                },
                {
                    "EnableSanction", "True"
                },
                {
                    "NegotiateAuthentication", "False"
                },
                {
                    "EnableExhaustiveTraining", "True"
                },
                {
                    "EnableAdvancedAnalyticsTraining", "True"
                },
                {
                    "AdvancedAnalyticsTrainingPollSeconds", "5"
                },
                {
                    "AdvancedAnalyticsTrainingMaxConcurrentRuns", "2"
                },
                {
                    "AdvancedAnalyticsTrainingMaxConcurrentRunsPerTenant", "1"
                },
                {
                    "AdvancedAnalyticsTrainingMaxRunSeconds", "1800"
                },
                {
                    "AdvancedAnalyticsTrainingHeartbeatSeconds", "10"
                },
                {
                    "AdvancedAnalyticsTrainingStaleSeconds", "120"
                },
                {
                    "AdvancedAnalyticsTrainingInlineMaxRows", "20000"
                },
                {
                    "AdvancedAnalyticsTrainingInlineMaxBytes", "16777216"
                },
                {
                    "AdvancedAnalyticsTrainingOnlineMaxRows", "2000"
                },
                {
                    "EnableReprocessing", "True"
                },
                {
                    "ExhaustiveDatasetUploadMaxBytes", "52428800"
                },
                {
                    "ExhaustiveDatasetUploadMaxRows", "1000000"
                },
                {
                    "ExhaustiveDatasetUploadMaxColumns", "500"
                },
                {
                    "ExhaustiveBatchRecallMaxRows", "2000"
                },
                {
                    "SanctionLoaderWait", "3600000"
                },
                {
                    "EnableSanctionLoaderChangePoll", "True"
                },
                {
                    "SanctionLoaderChangePoll", "60000"
                },
                {
                    "EnableSanctionLoader", "False"
                },
                {
                    "EnableDynamicEval", "False"
                },
                {
                    "EnableInlineScriptExecution", "False"
                },
                {
                    "EnableMockEndpoints", "True"
                },
                {
                    "EnableVisualisationDatasourceTableAllowList", "True"
                },
                {
                    "ApprovalsRequired", "1"
                },
                {
                    "EnableBacktest", "True"
                },
                {
                    "BacktestThreads", "1"
                },
                {
                    "BacktestMaxRows", "1000000"
                },
                {
                    "BacktestOnlineMaxRows", "10000"
                },
                {
                    "BacktestPageSize", "5000"
                },
                {
                    "BacktestMaxRunInterval", "s"
                },
                {
                    "BacktestMaxRunIntervalValue", "3600"
                },
                {
                    "BacktestStaleInterval", "s"
                },
                {
                    "BacktestStaleIntervalValue", "120"
                },
                {
                    "BacktestMaxConcurrentRunsPerTenant", "1"
                },
                {
                    "ActivationWatcherAllowPersist", "True"
                },
                {
                    "ActivationWatcherPersistThreads", "1"
                },
                {
                    "AMQP", "False"
                },
                {
                    "AMQPUri", null
                },
                {
                    "JWTValidAudience", "http://localhost:5001"
                },
                {
                    "JWTValidIssuer", "http://localhost:5001"
                },
                {
                    "JWTKey", null
                },
                {
                    "PasswordHashingKey", null
                },
                {
                    "EnablePublicInvokeController", "True"
                },
                {
                    "EnableEngine", "True"
                },
                {
                    "ExhaustiveTrialsLimit", "1000"
                },
                {
                    "ExhaustiveMinVariableCount", "5"
                },
                {
                    "ExhaustiveMaxVariableCount", "30"
                },
                {
                    "ExhaustiveTrainingDataSamplePercentage", "0.6"
                },
                {
                    "ExhaustiveCrossValidationDataSamplePercentage", "0.2"
                },
                {
                    "ExhaustiveTestingDataSamplePercentage", "0.2"
                },
                {
                    "ExhaustiveValidationTestingActivationThreshold", "0.5"
                },
                {
                    "ExhaustiveTopologySinceImprovementLimit", "10"
                },
                {
                    "ExhaustiveLayerDepthLimit", "4"
                },
                {
                    "ExhaustiveLayerWidthLimitInputLayerFactor", "4"
                },
                {
                    "ExhaustiveTopologyComplexityLimit", "10000"
                },
                {
                    "ExhaustiveActivationFunctionExplorationEpochs", "3"
                },
                {
                    "ExhaustiveTopologyExplorationEpochs", "3"
                },
                {
                    "ExhaustiveTopologyFinalisationEpochs", "20"
                },
                {
                    "ExhaustiveSimulationsCount", "100"
                },
                {
                    "ExhaustiveChallengerModelBaseProbability", "0.1"
                },
                {
                    "ExhaustiveAnomalyAutoBudgetSeconds", "120"
                },
                {
                    "ExhaustiveRecallTimeout", "1000"
                },
                {
                    "AdvancedAnalyticsStageTimeout", "1000"
                },
                {
                    "AdvancedAnalyticsCacheTtlSeconds", "3600"
                },
                {
                    "AdvancedAnalyticsCacheMaxEntryBytes", "67108864"
                },
                {
                    "AdvancedAnalyticsCacheMaxTenantBytes", "1073741824"
                },
                {
                    "EnableCallback", "True"
                },
                {
                    "CallbackTimeout", "1000"
                },
                {
                    "StreamingActivationWatcher", "True"
                },
                {
                    "WatcherStreamHeartbeatIntervalSeconds", "15"
                },
                {
                    "WaitPollFromActivationWatcherTable", "5000"
                },
                {
                    "WaitTtlCounterDecrement", "1000"
                },
                {
                    "RedisConnectionString", "localhost"
                },
                {
                    "RedisSentinelConnectTimeout", "2000"
                },
                {
                    "RedisSentinelSyncTimeout", "2000"
                },
                {
                    "RedisSentinelConnectRetry", "1"
                },
                {
                    "RedisReconnectRetryBaseDelay", "100"
                },
                {
                    "RedisReconnectRetryMaxDelay", "3000"
                },
                {
                    "RedisBacklogFailFast", "False"
                },
                {
                    "RedisCommandFlag", "0"
                },
                {
                    "CachePruneServer", "True"
                },
                {
                    "WaitCachePrune", "10000"
                },
                {
                    "WaitHashCacheAssemblyObservability", "60000"
                },
                {
                    "Landlord", "True"
                },
                {
                    "Log4NetConfigFileLocationName", null
                },
                {
                    "Log4NetLogPath", null
                },
                {
                    "Log4NetLogMaximumFileSize", "500MB"
                },
                {
                    "Log4NetLogMaxSizeRollBackups", "100"
                },
                {
                    "Log4NetLogLevel", "WARN"
                },
                {
                    "LocalCache", "True"
                },
                {
                    "LocalCacheFill", "True"
                },
                {
                    "LocalCacheBytes", "1073741824"
                },
                {
                    "RedisMessagePackCompression", "True"
                },
                {
                    "RedisStorePayloadCountsAndBytes", "False"
                },
                {
                    "RedisPublishSubscribeEvents", "False"
                },
                {
                    "MaxInvokeControllerRequestBytes", "4000"
                },
                {
                    "SearchKeyCacheServerIntervalType", "h"
                },
                {
                    "SearchKeyCacheServerIntervalValue", "1"
                },
                {
                    "AMQPHeartbeatInterval", "s"
                },
                {
                    "AMQPHeartbeatIntervalValue", "30"
                },
                {
                    "CaseCreationThreads", "4"
                },
                {
                    "PartialResponseMessageSerialisation", "True"
                },
                {
                    "RedisBackplane", "True"
                },
                {
                    "DataProtectionRedisBackplane", "True"
                },
                {
                    "SecretsPath", ""
                },
                {
                    "ApiHmacKey", null
                },
                {
                    "RedisHsetOffloadToPostgres", "False"
                },
                {
                    "SecureHttpCookie", "False"
                },
                {
                    "StrictCookie", "True"
                },
                {
                    "SessionAbsoluteLifetimeInterval", "n"
                },
                {
                    "SessionAbsoluteLifetimeIntervalValue", "720"
                },
                {
                    "MaxConcurrentConnections", "10000"
                },
                {
                    "RevokedSessionCheckInterval", "s"
                },
                {
                    "RevokedSessionCheckIntervalValue", "30"
                },
                {
                    "WafEnabled", "True"
                },
                {
                    "WafRefreshInterval", "s"
                },
                {
                    "WafRefreshIntervalValue", "30"
                },
                {
                    "WafFlushInterval", "s"
                },
                {
                    "WafFlushIntervalValue", "10"
                },
                {
                    "WafMaxInspectBytes", "262144"
                },
                {
                    "WafMaxRegexTimeoutMilliseconds", "100"
                },
                {
                    "WafMaxInspectionMilliseconds", "200"
                },
                {
                    "CsrfOriginCheck", "True"
                },
                {
                    "ElementSymmetricEncryptionKey", "SuperSecretEncryptionKeyGoesHere"
                },
                {
                    "EnableMultifactorAuthentication", "False"
                },
                {
                    "MultifactorAuthenticationEndpoint", "http://localhost:5001/api/mfa"
                },
                {
                    "MultifactorAuthenticationApplicationId", "MyApplicationGuidOrSomeSuch"
                },
                {
                    "MultifactorAuthenticationClientKey", "SomethingSecretForTheClientKeyHeader"
                },
                {
                    "PasswordAttempts", "3"
                },
                {
                    "EnablePasswordLockoutReset", "True"
                },
                {
                    "PasswordLockoutInterval", "n"
                },
                {
                    "PasswordLockoutIntervalValue", "30"
                },
                {
                    "LoginIpRateLimitAttempts", "1000"
                },
                {
                    "LoginIpRateLimitInterval", "s"
                },
                {
                    "LoginIpRateLimitIntervalValue", "60"
                },
                {
                    "EnableGlobalHttpRateLimit", "False"
                },
                {
                    "GlobalHttpRateLimitAttempts", "2000"
                },
                {
                    "GlobalHttpRateLimitInterval", "s"
                },
                {
                    "GlobalHttpRateLimitIntervalValue", "60"
                },
                {
                    "RateLimitBackplane", "True"
                },
                {
                    "CacheTtlDeleteLimit", "1000"
                },
                {
                    "TtlCounterEntryDeleteLimit", "200"
                },
                {
                    "TtlCounterAdministrationMaxConcurrency", "25"
                },
                {
                    "PasswordAsymmetricEncryption", "False"
                },
                {
                    "PasswordAsymmetricEncryptionPrivateKey", null
                },
                {
                    "PasswordAsymmetricEncryptionPublicKey", null
                },
                {
                    "SessionCookie", "True"
                },
                {
                    "AssumeLocalDateInPayloadExtraction", "True"
                },
                {
                    "ActivationRuleIdempotency", "False"
                },
                {
                    "LruJournalMaxAgeInterval", "h"
                },
                {
                    "LruJournalMaxAgeValue", "1"
                },
                {
                    "WaitLruJournalPrune", "10000"
                },
                {
                    "SMTPUseDefaultCredentials", "False"
                },
                {
                    "SMTPEnableSsl", "True"
                },
                {
                    "OutboundHttpRsaAmCertificateBypass", "False"
                },
                {
                    "OutboundHttpRsaAmCertificateThumbprint", null
                },
                {
                    "OAuthAuthentication", "False"
                },
                {
                    "OAuthAuthority", null
                },
                {
                    "OAuthClientId", null
                },
                {
                    "OAuthClientSecret", null
                },
                {
                    "OAuthForceRedirect", null
                },
                {
                    "OAuthForceGet", "False"
                },
                {
                    "OAuthSignInInterstitial", "False"
                },
                {
                    "OAuthPrompt", null
                },
                {
                    "SanctionsLevenshteinMaxDistanceRatio", "0.3"
                },
                {
                    "SanctionsLevenshteinMaxCoverageRatio", "2.0"
                },
                {
                    "UseForwardedHeaders", "True"
                },
                {
                    "EnableOpenTelemetry", "False"
                },
                {
                    "OpenTelemetryBackendEndpoint", null
                },
                {
                    "EnableServiceChangeStream", "False"
                },
                {
                    "ServiceChangeStreamHeartbeatIntervalSeconds", "15"
                },
                {
                    "EtcdEndpoints", null
                },
                {
                    "PatroniEndpoints", null
                },
                {
                    "EtcdDiscoveryPrefix", "etcd"
                },
                {
                    "PatroniDiscoveryPrefix", "patroni"
                },
                {
                    "EtcdPatroniDiscoveryMaxNodes", "5"
                },
                {
                    "EtcdClientPort", "2379"
                },
                {
                    "EtcdClientUsername", null
                },
                {
                    "EtcdClientPassword", null
                },
                {
                    "PatroniApiPort", "8008"
                },
                {
                    "PatroniDcsNamespace", "/service/"
                },
                {
                    "EnableCaseKeyJournalPeers", "True"
                },
                {
                    "WaitCaseKeyJournalPeers", "86400000"
                },
                {
                    "CaseKeyJournalPeerWindowDays", "90"
                },
                {
                    "CaseKeyJournalPeerMaximumRows", "200000"
                },
                {
                    "EnableCaseKeyJournalIndexing", "False"
                },
                {
                    "WaitCaseKeyJournalIndexing", "3600000"
                },
                {
                    "CaseKeyJournalIndexMinimumRows", "100000"
                },
                {
                    "CaseKeyJournalIndexMaximum", "16"
                },
                {
                    "EnableCaseKeyJournalTagJobs", "True"
                },
                {
                    "WaitCaseKeyJournalTagJobs", "2000"
                },
                {
                    "EnableInfrastructureHealthMetricsPurge", "True"
                },
                {
                    "DockerMonitoringProxyUrl", null
                },
                {
                    "WaitInfrastructureHealthMetricsPurge", "60000"
                },
                {
                    "InfrastructureHealthMetricsPurgeIntervalType", "d"
                },
                {
                    "InfrastructureHealthMetricsPurgeIntervalValue", "7"
                },
                {
                    "InfrastructureHealthMetricsPurgeDeleteLimit", "1000"
                },
                {
                    "OpenTelemetryMetricCaptureSamplePercentage", "100"
                },
                {
                    "EnableAskJooby", "False"
                },
                {
                    "AskJoobyApiEndpoint", "https://inference.do-ai.run/v1"
                },
                {
                    "AskJoobyApiKey", null
                },
                {
                    "AskJoobyModel", "deepseek-v4.1-flash"
                },
                {
                    "AskJoobyEmbeddingModel", "bge-m3"
                },
                {
                    "AskJoobyEmbeddingApiEndpoint", "https://inference.do-ai.run/v1"
                },
                {
                    "AskJoobyEmbeddingApiKey", null
                },
                {
                    "AskJoobyRerankerModel", "bge-reranker-v2-m3"
                },
                {
                    "AskJoobyRerankerApiEndpoint", "https://inference.do-ai.run/v1"
                },
                {
                    "AskJoobyRerankerApiKey", null
                },
                {
                    "AskJoobyRagCandidateCount", "40"
                },
                {
                    "AskJoobyRagMaxCandidateCharacters", "3500"
                },
                {
                    "AskJoobyRagPromotionBudgetCharacters", "6000"
                },
                {
                    "AskJoobyRerankerMinScore", "-100"
                },
                {
                    "AskJoobyEnableReranking", "True"
                },
                {
                    "AskJoobyThinkingDisableExplicitly", "True"
                },
                {
                    "AskJoobyTemperature", null
                },
                {
                    "AskJoobyMaxFanOut", "8"
                },
                {
                    "AskJoobyMaxSpawnDepth", "6"
                },
                {
                    "AskJoobyMemoryDecayHalfLifeDays", "30"
                },
                {
                    "AskJoobyEnableValidation", "True"
                },
                {
                    "AskJoobyMaxToolResultCharacters", "6000"
                },
                {
                    "AskJoobyMaxSearchesPerTurn", "64"
                },
                {
                    "AskJoobyEnableSuggestions", "True"
                },
                {
                    "AskJoobyValidationModel", "deepseek-v4.1-flash"
                },
                {
                    "AskJoobyValidationApiEndpoint", null
                },
                {
                    "AskJoobyValidationApiKey", null
                },
                {
                    "AskJoobyEnableAiAnalysis", "True"
                },
                {
                    "AskJoobyAiAnalysisModel", "deepseek-v4.1-flash"
                },
                {
                    "AskJoobyAiAnalysisApiEndpoint", null
                },
                {
                    "AskJoobyAiAnalysisApiKey", null
                },
                {
                    "AskJoobyAiAnalysisTimeoutSeconds", "60"
                },
                {
                    "AskJoobyAiAnalysisEnableThinking", "False"
                },
                {
                    "AskJoobyAiAnalysisThinkingEffort", "0"
                },
                {
                    "AskJoobyAiAnalysisCodeBudgetCharacters", "16000"
                },
                {
                    "AskJoobyEnableThinking", "False"
                },
                {
                    "AskJoobyThinkingEffort", "1"
                },
                {
                    "AskJoobyValidationEnableThinking", "False"
                },
                {
                    "AskJoobyValidationThinkingEffort", "0"
                },
                {
                    "AskJoobyValidationTemperature", "0.0"
                },
                {
                    "AskJoobyRagTopK", "3"
                },
                {
                    "AskJoobyRagMinScore", "0.013"
                },
                {
                    "AskJoobyValidationRagMinScore", "0.02"
                },
                {
                    "AskJoobyEmbedAllMessages", "True"
                },
                {
                    "AskJoobyReadOnly", "False"
                },
                {
                    "AskJoobyChatInputPromptInjectionMainThreshold", "0.97"
                },
                {
                    "AskJoobyEnableChatInputTopicBoundary", "True"
                },
                {
                    "AskJoobyUtilityEnableThinking", "False"
                },
                {
                    "AskJoobyUtilityThinkingEffort", "0"
                },
                {
                    "AskJoobyMaxConcurrentToolCalls", "4"
                },
                {
                    "AskJoobyAgentIdleTimeoutSeconds", "360"
                },
                {
                    "AskJoobyMaxIterationsPerRequest", "200"
                },
                {
                    "AskJoobyTurnTokenBudget", "10000000"
                },
                {
                    "AskJoobyAgentTokenBudget", "2000000"
                },
                {
                    "AskJoobyCachedTokenBudgetWeight", "0.1"
                },
                {
                    "AskJoobyMaxOutputTokens", "12288"
                },
                {
                    "AskJoobyRecordPromptPrefixHash", "False"
                },
                {
                    "AskJoobyShowPromptCacheShare", "True"
                },
                {
                    "EnableAskJoobyCostBanner", "True"
                },
                {
                    "RServerUrl", "http://localhost:8080"
                },
                {
                    "RServerHttpTimeout", "330000"
                },
                {
                    "RServerMaxConnections", "10"
                },
                {
                    "RServerValidateSsl", "True"
                },
                {
                    "EnableAskJoobyR", "True"
                },
                {
                    "EnableWebSearch", "False"
                },
                {
                    "AskJoobyWebSearchProvider", "1"
                },
                {
                    "AskJoobyWebSearchApiKey", null
                },
                {
                    "AskJoobyWebSearchApiEndpoint", "https://api.tavily.com/search"
                },
                {
                    "AskJoobyWebSearchMaxResults", "5"
                },
                {
                    "AskJoobyWebSearchDepth", "2"
                },
                {
                    "AskJoobyWebSearchAnswerMode", "2"
                },
                {
                    "AskJoobyWebSearchRawContentMode", "2"
                },
                {
                    "EnableOpenCorporates", "False"
                },
                {
                    "AskJoobyOpenCorporatesApiKey", null
                },
                {
                    "AskJoobyOpenCorporatesApiEndpoint", "https://api.opencorporates.com/v0.4"
                },
                {
                    "AskJoobyOpenCorporatesMaxResults", "5"
                },
                {
                    "JUBE_HOOK_ALLOWED_HOSTS", ""
                },
                {
                    "JUBE_HOOK_ALLOWED_CIDRS", ""
                }
            };

            var secretsPath = Environment.GetEnvironmentVariable("SecretsPath") ?? string.Empty;

            if (string.IsNullOrEmpty(secretsPath))
            {
                secretsPath = appSettings["SecretsPath"] ?? string.Empty;

                Console.WriteLine(string.IsNullOrEmpty(secretsPath)
                    ? "Environment Variable SecretsPath not available.  Will resolve to working directory."
                    : $"Environment Variable SecretsPath not available.  Set to default prefix of {secretsPath}.");
            }

            foreach (var environmentVariable in from DictionaryEntry environmentVariable in
                         Environment.GetEnvironmentVariables()
                     where appSettings.ContainsKey(Convert.ToString(environmentVariable.Key) ?? string.Empty)
                     where Convert.ToString(environmentVariable.Value) !=
                           appSettings[environmentVariable.Key.ToString() ?? string.Empty]
                     select environmentVariable)
            {
                var replaced = Convert.ToString(environmentVariable.Value);

                if (string.IsNullOrEmpty(replaced))
                {
                    continue;
                }

                foreach (var token in Tokenisation.Tokenisation.ReturnTokens(replaced))
                {
                    var path = Path.Combine(secretsPath, token);

                    if (!File.Exists(path))
                    {
                        Console.WriteLine(
                            $@"For environment variable {environmentVariable.Key} could not find {path} in container.");

                        continue;
                    }

                    string secret;

                    try
                    {
                        secret = File.ReadAllText(path).Trim();
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine(
                            $@"For environment variable {environmentVariable.Key} could not read {path}: {ex.Message}");

                        continue;
                    }

                    if (string.IsNullOrEmpty(secret))
                    {
                        Console.WriteLine(
                            $@"For environment variable {environmentVariable.Key} path {path} in container but it is empty.");

                        continue;
                    }

                    replaced = replaced.Replace($"[@{token}@]", secret);
                }

                appSettings[environmentVariable.Key.ToString() ?? string.Empty] = replaced;
            }

            InstantiateLog4Net();
            ValidateConnectionString();
            ValidateJwtKey();
            ValidatePasswordHashingKey();
            ValidatePasswordAsymmetricEncryptionKeys();
        }

        public ILog Log { get; set; }

        private void InstantiateLog4Net()
        {
            Console.WriteLine(@"Console logging for instantiation of logging library:");
            Console.WriteLine();

            Console.WriteLine(
                $@"Log4NetConfigFileLocationName:{Environment.GetEnvironmentVariable("Log4NetConfigFileLocationName")}");
            Console.WriteLine($@"Log4NetLogPath:{Environment.GetEnvironmentVariable("Log4NetLogPath")}");
            Console.WriteLine(
                $@"Log4NetLogMaximumFileSize:{Environment.GetEnvironmentVariable("Log4NetLogMaximumFileSize")}");
            Console.WriteLine(
                $@"Log4NetLogMaxSizeRollBackups:{Environment.GetEnvironmentVariable("Log4NetLogMaxSizeRollBackups")}");
            Console.WriteLine($@"Log4NetLogLevel:{Environment.GetEnvironmentVariable("Log4NetLogLevel")}");

            try
            {
                appSettings.TryGetValue("Log4NetConfigFileLocation", out var path);

                if (path != null)
                {
                    Console.WriteLine(
                        $@"Checking log4net XML serialisation file path {path} exists.");

                    var fileInfo = new FileInfo(path);

                    if (fileInfo.Exists)
                    {
                        try
                        {
                            Console.WriteLine(
                                $@"Configuring log4net XML serialisation from file path {path}.");

                            XmlConfigurator.Configure(fileInfo);

                            Console.WriteLine(
                                $@"Configured log4net XML serialisation from file path {path}.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(
                                $@"A log4net configuration file path: {path} was provided but it failed to parse with error {ex}. Falling back to default logger configuration.");

                            ConfigureLoggerFromEnvironmentVariables();
                        }
                    }
                    else
                    {
                        Console.WriteLine(
                            $@"A log4net configuration file path: {path} was provided but no file exists. Falling back to default logger configuration.");

                        ConfigureLoggerFromEnvironmentVariables();
                    }
                }
                else
                {
                    ConfigureLoggerFromEnvironmentVariables();
                }

                AttachApplicationLogCaptureAppender();

                Log = LogManager.GetLogger(typeof(ILog));
            }
            catch (Exception ex)
            {
                Console.WriteLine();

                Console.WriteLine(
                    $@"Failed to instantiate the logger with error {ex}.  No logging exists for the instance.");
            }

            Console.WriteLine(@"Logging instantiated.  Swapping to log4net instantiation for further logs.");
            Console.WriteLine();
        }

        private static void AttachApplicationLogCaptureAppender()
        {
            var applicationLogCaptureAppender = new ApplicationLogCaptureAppender
            {
                Threshold = Level.Warn
            };
            applicationLogCaptureAppender.ActivateOptions();

            ((Hierarchy)LogManager.GetRepository()).Root.AddAppender(applicationLogCaptureAppender);
        }

        private void ConfigureLoggerFromEnvironmentVariables()
        {
            var patternLayout = PatternLayout();
            var log4NetLevel = Log4NetLogLevel();
            ConfigureConsole(patternLayout, log4NetLevel);
            ConfigureRollingFileAppender(patternLayout, log4NetLevel);
            ((Hierarchy)LogManager.GetRepository()).Root.Level = Log4NetLogLevel();
        }

        private void ConfigureRollingFileAppender(PatternLayout patternLayout, Level log4NetLevel)
        {
            var log4NetLogFile = Log4NetLogFile();

            if (log4NetLogFile != null)
            {
                Console.WriteLine(@"Configuring software instantiation of log4net RollingFileAppender:");

                var rollingFileAppender = new RollingFileAppender
                {
                    File = log4NetLogFile,
                    Encoding = Encoding.UTF8,
                    Layout = patternLayout,
                    MaximumFileSize = Log4NetMaximumFileSize(),
                    MaxSizeRollBackups = Log4NetMaxSizeRollBackupsInt(),
                    StaticLogFileName = true,
                    RollingStyle = RollingFileAppender.RollingMode.Size,
                    AppendToFile = true,
                    Threshold = log4NetLevel
                };

                rollingFileAppender.ActivateOptions();

                BasicConfigurator.Configure(rollingFileAppender);

                Console.WriteLine(@"Configured software instantiation of log4net RollingFileAppender.");
            }
            else
            {
                Console.WriteLine(@"Log4NetLogPath not specified for RollingFileAppender.");
            }
        }

        private static void ConfigureConsole(PatternLayout patternLayout, Level log4NetLevel)
        {
            var consoleAppender = new ConsoleAppender
            {
                Layout = patternLayout,
                Threshold = log4NetLevel
            };
            consoleAppender.ActivateOptions();
            BasicConfigurator.Configure(consoleAppender);
        }

        private Level Log4NetLogLevel()
        {
            appSettings.TryGetValue("Log4NetLogLevel", out var log4NetLogLevel);

            return log4NetLogLevel switch
            {
                "ERROR" => Level.Error,
                "WARN" => Level.Warn,
                "INFO" => Level.Info,
                "DEBUG" => Level.Debug,
                _ => Level.Error
            };
        }

        private PatternLayout PatternLayout()
        {
            appSettings.TryGetValue("Log4NetPatternLayout", out var log4NetPatternLayout);

            var layout = new PatternLayout(log4NetPatternLayout ??
                                           "%date:%-5level:[%thread]:[%logger::%method]:%line:%message%newline");
            layout.ActivateOptions();

            return layout;
        }

        private string Log4NetMaximumFileSize()
        {
            appSettings.TryGetValue("Log4NetMaximumFileSize", out var log4NetMaximumFileSize);
            log4NetMaximumFileSize ??= "100MB";

            return log4NetMaximumFileSize;
        }

        private int Log4NetMaxSizeRollBackupsInt()
        {
            appSettings.TryGetValue("Log4NetMaxSizeRollBackups", out var log4NetMaxSizeRollBackups);

            if (!int.TryParse(log4NetMaxSizeRollBackups, out var log4NetMaxSizeRollBackupsInt))
            {
                log4NetMaxSizeRollBackupsInt = 100;
            }

            return log4NetMaxSizeRollBackupsInt;
        }

        private string Log4NetLogFile()
        {
            appSettings.TryGetValue("Log4NetLogPath", out var log4NetLogPath);

            return log4NetLogPath == null ? null : Path.Combine(log4NetLogPath, $"{Dns.GetHostName()}.log");
        }

        private void ValidateConnectionString()
        {
            if (string.IsNullOrEmpty(appSettings["ConnectionString"]))
            {
                throw new Exception("Missing ConnectionString in Environment Variables.");
            }
        }

        private void ValidatePasswordHashingKey()
        {
            if (string.IsNullOrEmpty(appSettings["PasswordHashingKey"]))
            {
                throw new Exception("Missing PasswordHashingKey in Environment Variables.");
            }
        }

        private void ValidateJwtKey()
        {
            if (string.IsNullOrEmpty(appSettings["JWTKey"]))
            {
                throw new Exception("Missing JWTKey in Environment Variables.");
            }
        }

        private void ValidatePasswordAsymmetricEncryptionKeys()
        {
            if (!appSettings["PasswordAsymmetricEncryption"].Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.IsNullOrEmpty(appSettings["PasswordAsymmetricEncryptionPrivateKey"]) ||
                string.IsNullOrEmpty(appSettings["PasswordAsymmetricEncryptionPublicKey"]))
            {
                throw new Exception(
                    "PasswordAsymmetricEncryption is True but PasswordAsymmetricEncryptionPrivateKey and/or " +
                    "PasswordAsymmetricEncryptionPublicKey are not set in Environment Variables. Generate a " +
                    "fresh RSA keypair for this environment and set both - do not reuse a keypair from " +
                    "documentation or another environment.");
            }
        }

        public string AppSettings(string[] keys)
        {
            return keys.Select(AppSettings).FirstOrDefault(value => value != null);
        }

        public string AppSettings(string key)
        {
            string value = null;

            if (appSettings.TryGetValue(key, out var setting))
            {
                value = setting;
            }

            return value;
        }
    }
}
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

using Jube.Data.Repository.Models;
using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentMigrator.Runner;
using Jube.App.Code;
using Jube.App.Code.AskJooby;
using Jube.App.Code.Interfaces;
using Jube.App.Code.Models;
using Jube.App.Code.ServiceChange;
using Jube.App.Code.Waf;
using Jube.App.Code.Watcher;
using Jube.App.Endpoints;
using Jube.App.Endpoints.AdvancedAnalytics;
using Jube.App.Endpoints.Mocks;
using Jube.App.Endpoints.Query;
using Jube.App.Endpoints.Repository;
using Jube.App.Endpoints.VersionHistory;
using Jube.App.Middlewares;
using Jube.App.Middlewares.Extensions;
using Jube.App.Middlewares.Models;
using Jube.AskJooby.Agents;
using Jube.AskJooby.Guidance;
using Jube.AskJooby.Guidance.Interfaces;
using Jube.AskJooby.Observability;
using Jube.AskJooby.Routing.Interfaces;
using Jube.AskJooby.Security;
using Jube.AskJooby.Security.Models;
using Jube.Cache;
using Jube.Cache.Observability;
using Jube.Cache.Redis.Callback;
using Jube.Data.Context;
using Jube.Data.Observability;
using Jube.Data.Observability.NpgsqlEventCounterBridge;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.BackgroundTasks.TaskStarters.Metrics.OpenTelemetry;
using Jube.Engine.EntityAnalysisModelInvoke.ImplicitAsync;
using Jube.Engine.EntityAnalysisModelInvoke.ImplicitAsync.Interfaces;
using Jube.Engine.Helpers;
using Jube.Engine.Observability;
using Jube.HttpHeaders;
using Jube.Migrations.Baseline;
using Jube.ResilientNpgsqlConnection;
using Jube.AskJooby.Tools.OpenCorporates.Interfaces;
using Jube.AskJooby.Tools.WebSearch.Interfaces;
using Jube.AskJooby.Tools.WebSearch.Models;
using Jube.Service.AskJoobyOpenCorporates;
using Jube.Service.AskJoobyRagChunk;
using Jube.Service.AskJoobyRagChunk.Interfaces;
using Jube.Service.AskJoobyWebSearch;
using Jube.Service.Authentication;
using Jube.Service.Observability;
using Jube.Service.Observability.OtlpDispatchCounters;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.TaskCancellation;
using Jube.TaskCancellation.Interfaces;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Npgsql;
using OpenAI;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using StackExchange.Redis;
using log4net;

namespace Jube.App
{
    public class Startup
    {
        private const int OtlpExporterTimeoutMilliseconds = 5000;
        private const int BatchMaxQueueSize = 2048;
        private const int BatchScheduledDelayMilliseconds = 5000;
        private const int BatchExporterTimeoutMilliseconds = 10000;
        private const int MetricExportIntervalMilliseconds = 60000;

        public void ConfigureServices(IServiceCollection services)
        {
            var dynamicEnvironment = AddSingletonForDynamicEnvironmentAndLogging(services, out var log);
            var cancellationTokenProvider = AddSingletonForCancellationToken(services);
            var taskCoordinator = AddSingletonForTaskCoordinator(services, cancellationTokenProvider, log);
            var contractResolver = AddSingletonForJsonSerializationHelper(services);

            ConfigureThreadPool(dynamicEnvironment, log);
            ValidateConnectionToPostgres(dynamicEnvironment.AppSettings("ConnectionString"), log);

            var callbacks = AddSingletonForCallbacks(services);
            var watcherStreamRegistry = AddSingletonForWatcherStreamRegistry(services, log);
            var serviceChangeStreamRegistry = AddSingletonForServiceChangeStreamRegistry(services, log);

            var cacheService = AddSingletonForCacheService(services, callbacks,
                int.Parse(dynamicEnvironment.AppSettings("CallbackTimeout") ?? "10000"),
                taskCoordinator, dynamicEnvironment, log, watcherStreamRegistry);

            AddSingletonForTokensCache(services, log, dynamicEnvironment, cacheService, taskCoordinator);

            var rabbitMqConnection = AddSingletonForRabbitMqConnection(services, dynamicEnvironment, log);
            var implicitAsyncInvocationTracker = AddSingletonForImplicitAsyncInvocationTracker(services, log);

            var openTelemetryExcludeCache =
                AddSingletonForOpenTelemetryExcludeCache(services, log, dynamicEnvironment, taskCoordinator);

            var logCounterRuleCache = AddSingletonForLogCounterRuleCache(services, log, dynamicEnvironment,
                taskCoordinator, openTelemetryExcludeCache);

            AddSingletonForEngine(services, dynamicEnvironment, log, rabbitMqConnection, cacheService, contractResolver,
                taskCoordinator, implicitAsyncInvocationTracker, openTelemetryExcludeCache, logCounterRuleCache);

            AddSingletonForIdentity(services);
            AddSingletonForLoginSourceIpThrottle(services);
            AddSingletonForGlobalHttpRequestSourceIpThrottle(services);
            ConfigureAuthentication(services, dynamicEnvironment, log);
            AddGenericServicesRequired(services, watcherStreamRegistry, serviceChangeStreamRegistry);
            AddDataProtection(services, dynamicEnvironment);
            AddSwagger(services);
            AddSingletonForServiceChangeBus(services, dynamicEnvironment, cacheService, serviceChangeStreamRegistry);
            AddSingletonForAskJoobyAgentGuidanceExchange(services);
            AddSingletonForAskJoobyTurnTracker(services);
            AddSingletonForAskJooby(services, dynamicEnvironment, log);
            AddSingletonForAskJoobyWebSearch(services, dynamicEnvironment, log);
            AddSingletonForAskJoobyOpenCorporates(services, dynamicEnvironment, log);

            AddOpenTelemetry(services, dynamicEnvironment, cacheService, openTelemetryExcludeCache, log,
                taskCoordinator);

            var openTelemetryMetricCaptureSamplePercentage = double.Parse(
                dynamicEnvironment.AppSettings("OpenTelemetryMetricCaptureSamplePercentage"),
                CultureInfo.InvariantCulture);

            OpenTelemetryMetricCapture.Start(openTelemetryMetricCaptureSamplePercentage, ServiceDiagnostics.Name,
                EngineDiagnostics.Name, DataDiagnostics.Name, CacheDiagnostics.Name, "System.Runtime",
                "System.Net.Http",
                "Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel",
                "Microsoft.AspNetCore.Authentication", "Microsoft.AspNetCore.Authorization",
                "OpenTelemetry.Instrumentation.Process");

            RedisCommandMetricBridge.Start();
            DataDiagnostics.Start();
            NpgsqlEventCounterBridge.Start();

            GetHttpHeadersFromDatabaseAndCreateSingleton(services, dynamicEnvironment, log, taskCoordinator);
            WriteWelcomeMessageToConsole();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log)
        {
            try
            {
                if (dynamicEnvironment.AppSettings("UseForwardedHeaders")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    var startupOptions = new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                    };

                    startupOptions.KnownIPNetworks.Clear();
                    startupOptions.KnownProxies.Clear();

                    app.UseForwardedHeaders(startupOptions);
                }

                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseExceptionHandler("/Error")
                );

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder =>
                        appBuilder
                            .UseHsts()
                );

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseHttpsRedirection()
                );

                app.UseMiddleware<RequestHardeningMiddleware>();
                app.UseRouting();

                app.UseRequestLocalization(new RequestLocalizationOptions()
                    .SetDefaultCulture("en")
                    .AddSupportedCultures("en")
                    .AddSupportedUICultures("en"));

                app.UseAuthentication();
                app.UseAuthorization();
                app.UseMiddleware<EmptyBodyGuardMiddleware>();
                app.UseMiddleware<WafMiddleware>();

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseMiddleware<GlobalHttpRateLimitMiddleware>()
                );

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseMiddleware<TokenRefreshMiddleware>()
                );

                var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

                app.UseWhen(context => context.Request.Path.StartsWithSegments("/api") &&
                                       lifetime.ApplicationStopping.IsCancellationRequested,
                    branch => branch.UseConditionalConnectionCloseWhenStopping());

                app.UseWhen(context => context.Request.Path.StartsWithSegments("/Account"), appBuilder =>
                {
                    appBuilder.Use(async (context, next) =>
                    {
                        context.Response.OnStarting(() =>
                        {
                            context.Response.Headers["Cache-Control"] = "no-store";
                            context.Response.Headers["Referrer-Policy"] = "no-referrer";

                            return Task.CompletedTask;
                        });

                        await next.Invoke();
                    });
                });

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseStatusCodePages(context =>

                    {
                        var request = context.HttpContext.Request;
                        var response = context.HttpContext.Response;

                        if (response.StatusCode != (int)HttpStatusCode.Unauthorized)
                        {
                            return Task.CompletedTask;
                        }

                        if (!request.Path.StartsWithSegments("/api"))
                        {
                            response.Redirect(
                                $"/Account/Login?RedirectUrl={Uri.EscapeDataString(request.Path + request.QueryString)}");
                        }

                        return Task.CompletedTask;
                    })
                );

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseResponseHttpHeadersMiddleware()
                );

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.RequestTrackingMiddleware()
                );

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseStaticFiles()
                );

                app.UseWhen(
                    httpContext => httpContext.Request.Path.StartsWithSegments("/swagger",
                        StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseMiddleware<SwaggerAuthenticationMiddleware>()
                );

                app.UseWhen(
                    httpContext => httpContext.Request.Path.Equals("/swagger/swagger-initializer.js",
                        StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.Run(context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;

                        return Task.CompletedTask;
                    })
                );

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseSwagger()
                );

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jube API v1");
                    })
                );

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapRazorPages();
                    endpoints.MapControllers();
                    endpoints.MapEntityAnalysisModelEndpoints();
                    endpoints.MapEntityAnalysisModelRequestXPathEndpoints();
                    endpoints.MapEntityAnalysisModelInlineFunctionEndpoints();
                    endpoints.MapEntityAnalysisModelInlineScriptEndpoints();
                    endpoints.MapEntityAnalysisModelGatewayRuleEndpoints();
                    endpoints.MapEntityAnalysisModelSanctionEndpoints();
                    endpoints.MapEntityAnalysisModelTagEndpoints();
                    endpoints.MapEntityApprovalEndpoints();
                    endpoints.MapVersionHistoryEndpoints();
                    endpoints.MapEntityAnalysisModelStagePerformanceCounterEndpoints();
                    endpoints.MapEntityAnalysisModelIntegrityEndpoints();
                    endpoints.MapQueryBuilderEndpoints();
                    endpoints.MapEntityAnalysisModelEngineStateEndpoints();
                    endpoints.MapEntityAnalysisModelDependencyGraphEndpoints();
                    endpoints.MapEntityAnalysisModelDependencyEndpoints();
                    endpoints.MapEntityAnalysisModelBacktestEndpoints();
                    endpoints.MapEntityAnalysisModelResponseTimePipelineCounterEndpoints();
                    endpoints.MapEntityAnalysisModelTaskPerformanceCounterEndpoints();
                    endpoints.MapApplicationLogEntryEndpoints();
                    endpoints.MapWafAttackEndpoints();
                    endpoints.MapDotNetRuntimeMetricEndpoints();
                    endpoints.MapPostgresMetricEndpoints();
                    endpoints.MapRedisMetricEndpoints();
                    endpoints.MapRedisSlowOperationEndpoints();
                    endpoints.MapRedisConnectionMultiplexerMetricEndpoints();
                    endpoints.MapPostgresTableStatisticsEndpoints();
                    endpoints.MapPostgresIndexStatisticsEndpoints();
                    endpoints.MapPostgresReplicationStatusEndpoints();
                    endpoints.MapPostgresLogEntryEndpoints();
                    endpoints.MapContainerLogEntryEndpoints();
                    endpoints.MapDockerEventEndpoints();
                    endpoints.MapRedisSentinelStatusEndpoints();
                    endpoints.MapRedisSentinelEventEndpoints();
                    endpoints.MapModelInvokeWarningEndpoints();
                    endpoints.MapArchiverStagePerformanceCounterEndpoints();
                    endpoints.MapCaseCreationStagePerformanceCounterEndpoints();
                    endpoints.MapCaseCreationWarningEndpoints();
                    endpoints.MapArchiverWarningEndpoints();
                    endpoints.MapCaptureQueueHealthEndpoints();
                    endpoints.MapRedisConnectionEventEndpoints();
                    endpoints.MapRedisCallCounterEndpoints();
                    endpoints.MapEtcdMemberStatusEndpoints();
                    endpoints.MapPatroniMemberStatusEndpoints();
                    endpoints.MapEtcdClusterEventEndpoints();
                    endpoints.MapPatroniClusterEventEndpoints();
                    endpoints.MapDockerContainerMetricEndpoints();
                    endpoints.MapDockerHostMetricEndpoints();
                    endpoints.MapHaProxyServerStatusEndpoints();
                    endpoints.MapHaProxyReachabilityProbeEndpoints();
                    endpoints.MapOverlayNetworkTaskDriftEndpoints();
                    endpoints.MapOpenTelemetryMetricEndpoints();
                    endpoints.MapOpenTelemetryLogCounterEndpoints();
                    endpoints.MapOpenTelemetryExcludeEndpoints();
                    endpoints.MapOtlpDispatchCounterEndpoints();
                    endpoints.MapUserLoginEndpoints();
                    endpoints.MapUserLogoutEndpoints();
                    endpoints.MapActivationWatcherEndpoints();
                    endpoints.MapWatcherStreamEndpoints();
                    endpoints.MapServiceChangeStreamEndpoints();
                    endpoints.MapPostgresActivityEndpoints();
                    endpoints.MapPostgresStatementStatisticsEndpoints();
                    endpoints.MapEntityAnalysisModelAbstractionRuleEndpoints();
                    endpoints.MapEntityAnalysisModelAbstractionCalculationEndpoints();
                    endpoints.MapEntityAnalysisModelHttpAdaptationEndpoints();
                    endpoints.MapExhaustiveSearchInstanceEndpoints();
                    endpoints.MapExhaustiveInstanceDatasetEndpoints();
                    endpoints.MapAdvancedAnalyticsInstanceEndpoints();
                    endpoints.MapAdvancedAnalyticsModelEndpoints();
                    endpoints.MapChartsEndpoints();
                    endpoints.MapEntityAnalysisModelActivationRuleEndpoints();
                    endpoints.MapEntityAnalysisInlineScriptEndpoints();
                    endpoints.MapEntityAnalysisModelTtlCounterEndpoints();
                    endpoints.MapEntityAnalysisModelListEndpoints();
                    endpoints.MapEntityAnalysisModelListValueEndpoints();
                    endpoints.MapEntityAnalysisModelDictionaryEndpoints();
                    endpoints.MapEntityAnalysisModelDictionaryKvpEndpoints();
                    endpoints.MapEntityAnalysisModelOverrideEndpoints();
                    endpoints.MapEntityAnalysisModelActivationRuleOverrideEndpoints();
                    endpoints.MapEntityAnalysisModelReprocessingRuleEndpoints();
                    endpoints.MapEntityAnalysisModelReprocessingRuleInstanceEndpoints();
                    endpoints.MapHttpProcessingCounterEndpoints();
                    endpoints.MapEntityAnalysisAsynchronousQueueBalanceEndpoints();
                    endpoints.MapEntityAnalysisModelAsynchronousQueueBalanceEndpoints();
                    endpoints.MapEntityAnalysisModelProcessingCounterEndpoints();
                    endpoints.MapArchiveEndpoints();
                    endpoints.MapCaseEndpoints();
                    endpoints.MapCaseFileEndpoints();
                    endpoints.MapCaseNoteEndpoints();
                    endpoints.MapCaseWorkflowEndpoints();
                    endpoints.MapCaseWorkflowActionEndpoints();
                    endpoints.MapCaseWorkflowActionRoleEndpoints();
                    endpoints.MapCaseWorkflowDisplayEndpoints();
                    endpoints.MapCaseWorkflowDisplayRoleEndpoints();
                    endpoints.MapCaseWorkflowFilterEndpoints();
                    endpoints.MapCaseWorkflowFilterRoleEndpoints();
                    endpoints.MapCaseWorkflowFormEndpoints();
                    endpoints.MapCaseWorkflowFormEntryEndpoints();
                    endpoints.MapCaseWorkflowFormEntryValueEndpoints();
                    endpoints.MapCaseWorkflowFormRoleEndpoints();
                    endpoints.MapCaseWorkflowMacroEndpoints();
                    endpoints.MapCaseWorkflowMacroRoleEndpoints();
                    endpoints.MapCaseWorkflowPriorityEndpoints();
                    endpoints.MapCaseWorkflowRoleEndpoints();
                    endpoints.MapCaseWorkflowStatusEndpoints();
                    endpoints.MapCaseWorkflowStatusRoleEndpoints();
                    endpoints.MapCaseWorkflowXPathEndpoints();
                    endpoints.MapCaseWorkflowXPathRoleEndpoints();
                    endpoints.MapEntityAnalysisModelRoleEndpoints();
                    endpoints.MapEntityAnalysisModelSynchronisationScheduleEndpoints();
                    endpoints.MapExhaustiveSearchInstancePromotedTrialInstanceEndpoints();
                    endpoints.MapPermissionSpecificationEndpoints();
                    endpoints.MapRoleRegistryEndpoints();
                    endpoints.MapRoleRegistryPermissionEndpoints();
                    endpoints.MapSanctionEntrySourceEndpoints();
                    endpoints.MapTenantRegistryEndpoints();
                    endpoints.MapUserInTenantEndpoints();
                    endpoints.MapUserRegistryEndpoints();
                    endpoints.MapUserRegistryApiKeyEndpoints();
                    endpoints.MapVisualisationRegistryEndpoints();
                    endpoints.MapVisualisationRegistryDatasourceEndpoints();
                    endpoints.MapVisualisationRegistryDatasourceRoleEndpoints();
                    endpoints.MapVisualisationRegistryDatasourceSeriesEndpoints();
                    endpoints.MapVisualisationRegistryParameterEndpoints();
                    endpoints.MapVisualisationRegistryParameterRoleEndpoints();
                    endpoints.MapVisualisationRegistryRoleEndpoints();
                    endpoints.MapCaseByCaseKeyValueEndpoints();
                    endpoints.MapCaseByIdEndpoints();
                    endpoints.MapCaseBySessionCaseSearchCompileEndpoints();
                    endpoints.MapCaseEventByCaseKeyValueEndpoints();
                    endpoints.MapCaseJournalEndpoints();
                    endpoints.MapCaseNoteByCaseKeyValueEndpoints();
                    endpoints.MapCaseWorkflowFormEntryByCaseKeyValueEndpoints();
                    endpoints.MapEntityAnalysisModelActivationRuleOverrideQueryEndpoints();
                    endpoints.MapEntityAnalysisModelSampleEndpoints();
                    endpoints.MapEntityAnalysisModelOverrideQueryEndpoints();
                    endpoints.MapEntityAnalysisModelSynchronisationNodeStatusEntriesEndpoints();
                    endpoints.MapEntityAnalysisPotentialMultiPartStringNamesEndpoints();
                    endpoints.MapEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeEndpoints();
                    endpoints.MapExhaustiveSearchInstancePromotedTrialInstanceConfusionEndpoints();
                    endpoints.MapExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramEndpoints();
                    endpoints.MapExhaustiveSearchInstancePromotedTrialInstanceLearningCurveEndpoints();
                    endpoints.MapExhaustiveSearchInstancePromotedTrialInstancePredictedActualEndpoints();
                    endpoints.MapExhaustiveSearchInstancePromotedTrialInstanceQueryEndpoints();
                    endpoints.MapExhaustiveSearchInstancePromotionHistoryEndpoints();
                    endpoints.MapExhaustiveSearchInstancePromotedTrialInstanceRocEndpoints();
                    endpoints.MapExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionEndpoints();
                    endpoints.MapExhaustiveSearchInstanceTrialInstanceListQueryEndpoints();
                    endpoints.MapExhaustiveSearchInstanceTrialInstanceVariableEndpoints();
                    endpoints.MapExhaustiveSearchInstanceTrialInstanceVariableVarianceEndpoints();
                    endpoints.MapExhaustiveSearchInstanceVariableEndpoints();
                    endpoints.MapVisualisationRegistryDatasourceCommandExecutionEndpoints();
                    endpoints.MapTreeChildrenEndpoints();
                    endpoints.MapSessionCaseJournalEndpoints();
                    endpoints.MapSessionCaseSearchCompiledSqlEndpoints();
                    endpoints.MapPreservationEndpoints();
                    endpoints.MapPreservationSnapshotEndpoints();
                    endpoints.MapReadyEndpoints();
                    endpoints.MapAuthenticationEndpoints();

                    if (!"False".Equals(dynamicEnvironment.AppSettings("EnableMockEndpoints"),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        endpoints.MapMockHttpAdaptationEndpoints();
                        endpoints.MapMockRsaMfaEndpoints();
                    }

                    endpoints.MapInvokeEndpoints();
                    endpoints.MapCompletionsEndpoints();
                    endpoints.MapIconsEndpoints();
                    endpoints.MapParserEndpoints();
                    endpoints.MapCaseWorkflowDisplayExecutionEndpoints();
                    endpoints.MapCaseWorkflowMacroExecutionEndpoints();
                    endpoints.MapAskJoobyConversationEndpoints();
                    endpoints.MapAskJoobyStreamEndpoints();
                    endpoints.MapAskJoobyFileEndpoints();
                    endpoints.MapSessionEndpoints();
                });

                _ = StartEngineInBackgroundAsync(app, log);
            }
            catch (Exception ex)
            {
                log.Error($"Error in App Configure as {ex}");
            }
        }

        private static void AddSingletonForServiceChangeBus(IServiceCollection services,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CacheService cacheService,
            ServiceChangeStreamRegistry serviceChangeStreamRegistry)
        {
            if (!dynamicEnvironment.AppSettings("EnableServiceChangeStream")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton<IServiceChangeBus, NullServiceChangeBus>();

                return;
            }

            IServiceChangeBus serviceChangeBus =
                dynamicEnvironment.AppSettings("RedisBackplane").Equals("True", StringComparison.OrdinalIgnoreCase)
                    ? new RedisServiceChangeBus(cacheService.ConnectionMultiplexer)
                    : new InProcessServiceChangeBus();

            serviceChangeBus.Subscribe(change =>
            {
                var payload = JsonSerializer.Serialize(change);
                serviceChangeStreamRegistry.Dispatch(change.TenantRegistryId, payload);

                return Task.CompletedTask;
            });

            services.AddSingleton(serviceChangeBus);
        }

        private static void AddSingletonForAskJooby(IServiceCollection services,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log)
        {
            var endpoint = dynamicEnvironment.AppSettings("AskJoobyApiEndpoint");
            var apiKey = dynamicEnvironment.AppSettings("AskJoobyApiKey");
            var model = dynamicEnvironment.AppSettings("AskJoobyModel");
            var embeddingModel = dynamicEnvironment.AppSettings("AskJoobyEmbeddingModel");

            if (string.IsNullOrWhiteSpace(endpoint) && string.IsNullOrWhiteSpace(model)
                                                    && string.IsNullOrWhiteSpace(embeddingModel))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("AskJooby: no AskJoobyApiEndpoint/AskJoobyModel/AskJoobyEmbeddingModel " +
                             "configured -- no backend inference engine has been specified for AskJooby. " +
                             "Falling back to a local Ollama OpenAI-compatible default " +
                             "(http://localhost:11434/v1) that this deployment almost certainly does not " +
                             "have. Set these AppSettings to point AskJooby at a real OpenAI-compatible " +
                             "inference endpoint (e.g. OpenRouter, OVHcloud AI Endpoints).");
                }
            }

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                endpoint = "http://localhost:11434/v1";
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                model = AgentFactory.DefaultModel;
            }

            if (string.IsNullOrWhiteSpace(embeddingModel))
            {
                embeddingModel = "baai/bge-m3";
            }

            var openAiClient = BuildOpenAiClient(endpoint, apiKey);

            var mainChatClient = new UserQueryGuardChatClient(openAiClient.GetChatClient(model).AsIChatClient());
            services.AddSingleton<IChatClient>(mainChatClient);

            var validationModel = dynamicEnvironment.AppSettings("AskJoobyValidationModel");

            var (validationEndpoint, validationApiKey) = EmbeddingApiBackendVariables(dynamicEnvironment,
                "AskJoobyValidationApiEndpoint", "AskJoobyValidationApiKey", endpoint, apiKey);

            var validationSharesTheChatBackend =
                SameBackendTest(validationEndpoint, validationApiKey, endpoint, apiKey);

            IChatClient validationChatClient;

            if (validationSharesTheChatBackend
                && (string.IsNullOrWhiteSpace(validationModel)
                    || validationModel.Equals(model, StringComparison.OrdinalIgnoreCase)))
            {
                validationChatClient = mainChatClient;

                services.AddKeyedSingleton(
                    "validation", (sp, _) => sp.GetRequiredService<IChatClient>());
            }
            else
            {
                var validationClient = validationSharesTheChatBackend
                    ? openAiClient
                    : BuildOpenAiClient(validationEndpoint, validationApiKey);

                validationChatClient = new UserQueryGuardChatClient(
                    validationClient.GetChatClient(
                            string.IsNullOrWhiteSpace(validationModel) ? model : validationModel)
                        .AsIChatClient());

                services.AddKeyedSingleton("validation", validationChatClient);
            }

            var analysisEnabled = !"False".Equals(dynamicEnvironment.AppSettings("AskJoobyEnableAiAnalysis"),
                StringComparison.OrdinalIgnoreCase);

            if (!analysisEnabled)
            {
                services.AddKeyedSingleton(
                    "analysis", (sp, _) => sp.GetRequiredService<IChatClient>());
            }
            else
            {
                var analysisModel = dynamicEnvironment.AppSettings("AskJoobyAiAnalysisModel");

                var (analysisEndpoint, analysisApiKey) = EmbeddingApiBackendVariables(dynamicEnvironment,
                    "AskJoobyAiAnalysisApiEndpoint", "AskJoobyAiAnalysisApiKey", endpoint, apiKey);

                var analysisSharesTheChatBackend =
                    SameBackendTest(analysisEndpoint, analysisApiKey, endpoint, apiKey);

                if (analysisSharesTheChatBackend
                    && (string.IsNullOrWhiteSpace(analysisModel)
                        || analysisModel.Equals(model, StringComparison.OrdinalIgnoreCase)))
                {
                    services.AddKeyedSingleton(
                        "analysis", (sp, _) => sp.GetRequiredService<IChatClient>());
                }
                else
                {
                    var analysisClient = analysisSharesTheChatBackend
                        ? openAiClient
                        : BuildOpenAiClient(analysisEndpoint, analysisApiKey);

                    IChatClient analysisChatClient = new UserQueryGuardChatClient(
                        analysisClient.GetChatClient(
                                string.IsNullOrWhiteSpace(analysisModel) ? model : analysisModel)
                            .AsIChatClient());

                    services.AddKeyedSingleton("analysis", analysisChatClient);
                }

                WarnOnRouteInEndpoint(log, "AskJoobyAiAnalysisApiEndpoint", analysisEndpoint, "chat/completions");
            }

            var chatInputPromptInjectionMainThreshold = float.TryParse(
                dynamicEnvironment.AppSettings("AskJoobyChatInputPromptInjectionMainThreshold"),
                NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedThreshold)
                ? parsedThreshold
                : 0.97f;

            var chatInputTopicBoundaryEnabled = "True".Equals(
                dynamicEnvironment.AppSettings("AskJoobyEnableChatInputTopicBoundary"),
                StringComparison.OrdinalIgnoreCase);

            AskJoobyInjectionGuard.Configure(new AskJoobyInjectionGuardSettings(
                validationChatClient, chatInputPromptInjectionMainThreshold, chatInputTopicBoundaryEnabled));

            var (embeddingEndpoint, embeddingApiKey) = EmbeddingApiBackendVariables(dynamicEnvironment,
                "AskJoobyEmbeddingApiEndpoint", "AskJoobyEmbeddingApiKey", endpoint, apiKey);

            WarnOnInheritedKey(log, "AskJoobyEmbeddingApiKey", "AskJoobyEmbeddingApiEndpoint",
                embeddingEndpoint, embeddingApiKey, endpoint, apiKey);

            WarnOnRouteInEndpoint(log, "AskJoobyApiEndpoint", endpoint, "chat/completions");
            WarnOnRouteInEndpoint(log, "AskJoobyEmbeddingApiEndpoint", embeddingEndpoint, "embeddings");
            WarnOnRouteInEndpoint(log, "AskJoobyValidationApiEndpoint", validationEndpoint, "chat/completions");

            var embeddingClient = SameBackendTest(embeddingEndpoint, embeddingApiKey, endpoint, apiKey)
                ? openAiClient
                : BuildOpenAiClient(embeddingEndpoint, embeddingApiKey);

            services.AddSingleton(
                embeddingClient.GetEmbeddingClient(embeddingModel).AsIEmbeddingGenerator());

            var rerankerEndpoint = dynamicEnvironment.AppSettings("AskJoobyRerankerApiEndpoint");
            var rerankerModel = dynamicEnvironment.AppSettings("AskJoobyRerankerModel");

            if (!string.IsNullOrWhiteSpace(rerankerEndpoint) && !string.IsNullOrWhiteSpace(rerankerModel))
            {
                var rerankerApiKey = dynamicEnvironment.AppSettings("AskJoobyRerankerApiKey");

                WarnOnInheritedKey(log, "AskJoobyRerankerApiKey", "AskJoobyRerankerApiEndpoint",
                    rerankerEndpoint, string.IsNullOrWhiteSpace(rerankerApiKey) ? apiKey : rerankerApiKey,
                    endpoint, apiKey);

                services.AddSingleton<IRagReranker>(new BgeReranker(
                    new HttpClient(), rerankerEndpoint,
                    string.IsNullOrWhiteSpace(rerankerApiKey) ? apiKey : rerankerApiKey,
                    rerankerModel, log));
            }
            else if (log.IsInfoEnabled)
            {
                log.Info("AskJooby: no AskJoobyRerankerApiEndpoint is configured, so retrieval runs in one "
                         + "stage. A cross encoder reranker matters more now that queries are embedded "
                         + "without a task instruction, because it is the stage that reads the query as "
                         + "written against the passage as written.");
            }

            services.AddSingleton<IAgentRoutingLedger,
                LoggingAgentRoutingLedger>();

            services.AddHostedService<RoutingExemplarWarmupService>();
        }

        private static void AddSingletonForAskJoobyWebSearch(IServiceCollection services,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log)
        {
            if (!AskJoobyConversationServiceFactory.Flag(dynamicEnvironment, "EnableWebSearch"))
            {
                return;
            }

            var apiKey = dynamicEnvironment.AppSettings("AskJoobyWebSearchApiKey");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("AskJooby: EnableWebSearch is True but AskJoobyWebSearchApiKey is not set, so "
                             + "the web search tool is not registered.");
                }

                return;
            }

            var providerSetting = dynamicEnvironment.AppSettings("AskJoobyWebSearchProvider");

            var provider = Enum.TryParse<WebSearchProviderType>(providerSetting, true, out var parsedProvider)
                ? parsedProvider
                : WebSearchProviderType.Tavily;

            switch (provider)
            {
                case WebSearchProviderType.Tavily:
                    var endpoint = dynamicEnvironment.AppSettings("AskJoobyWebSearchApiEndpoint");

                    services.AddSingleton<IWebSearchClient>(
                        new TavilyWebSearchClient(new HttpClient(), endpoint, apiKey));

                    break;
                default:
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"AskJooby: AskJoobyWebSearchProvider '{providerSetting}' is not a known "
                                 + "WebSearchProviderType, so the web search tool is not registered.");
                    }

                    break;
            }
        }

        private static void AddSingletonForAskJoobyOpenCorporates(IServiceCollection services,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log)
        {
            if (!AskJoobyConversationServiceFactory.Flag(dynamicEnvironment, "EnableOpenCorporates"))
            {
                return;
            }

            var apiKey = dynamicEnvironment.AppSettings("AskJoobyOpenCorporatesApiKey");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("AskJooby: EnableOpenCorporates is True but AskJoobyOpenCorporatesApiKey is not "
                             + "set, so the OpenCorporates tool is not registered.");
                }

                return;
            }

            var endpoint = dynamicEnvironment.AppSettings("AskJoobyOpenCorporatesApiEndpoint");

            services.AddSingleton<IOpenCorporatesClient>(new OpenCorporatesClient(new HttpClient(), endpoint, apiKey));
        }

        private static (string Endpoint, string ApiKey) EmbeddingApiBackendVariables(
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, string endpointKey, string apiKeyKey,
            string defaultEndpoint, string defaultApiKey)
        {
            var roleEndpoint = dynamicEnvironment.AppSettings(endpointKey);
            var roleApiKey = dynamicEnvironment.AppSettings(apiKeyKey);

            return (string.IsNullOrWhiteSpace(roleEndpoint) ? defaultEndpoint : roleEndpoint,
                string.IsNullOrWhiteSpace(roleApiKey) ? defaultApiKey : roleApiKey);
        }

        private static void WarnOnRouteInEndpoint(ILog log, string key, string endpoint, string route)
        {
            var trimmed = endpoint?.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(trimmed)
                || !trimmed.EndsWith("/" + route, StringComparison.OrdinalIgnoreCase)
                || !log.IsWarnEnabled)
            {
                return;
            }

            log.Warn($"AskJooby: {key} is set to {endpoint}, which already carries the /{route} route. These "
                     + $"settings are base addresses and the client appends /{route} itself, so every call "
                     + $"will be sent to {trimmed}/{route} and answered with an HTTP 400 that says nothing "
                     + $"about the doubled path. Set {key} to {trimmed[..^(route.Length + 1)]} instead.");
        }

        private static void WarnOnInheritedKey(ILog log, string apiKeyKey, string endpointKey,
            string roleEndpoint, string roleApiKey, string chatEndpoint, string chatApiKey)
        {
            if (string.Equals(Host(roleEndpoint), Host(chatEndpoint), StringComparison.OrdinalIgnoreCase)
                || !string.Equals(roleApiKey, chatApiKey, StringComparison.Ordinal)
                || !log.IsWarnEnabled)
            {
                return;
            }

            log.Warn($"AskJooby: {endpointKey} is {roleEndpoint}, which is a different provider from the "
                     + $"chat endpoint {chatEndpoint}, but no {apiKeyKey} is set so the chat provider's key "
                     + $"will be sent there. Expect an HTTP 401 on the first call. Set {apiKeyKey} to a "
                     + $"credential for {roleEndpoint}.");
        }

        private static string Host(string endpoint)
        {
            return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ? uri.Host : endpoint ?? string.Empty;
        }

        private static bool SameBackendTest(string endpoint, string apiKey, string otherEndpoint, string otherApiKey)
        {
            return string.Equals(endpoint, otherEndpoint, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(apiKey, otherApiKey, StringComparison.Ordinal);
        }

        private static OpenAIClient BuildOpenAiClient(string endpoint, string apiKey)
        {
            var credential = new ApiKeyCredential(
                string.IsNullOrWhiteSpace(apiKey) ? "not-needed" : apiKey);

            var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };

            options.AddPolicy(new ModelAffinityPolicy(),
                PipelinePosition.PerCall);

            return new OpenAIClient(credential, options);
        }

        private static Uri BuildOtlpEndpoint(string backendEndpoint, string path)
        {
            return string.IsNullOrEmpty(backendEndpoint)
                ? null
                : new Uri($"{backendEndpoint.TrimEnd('/')}/{path}");
        }

        private static OtlpExporterOptions BuildOtlpExporterOptions(string backendEndpoint, string path)
        {
            var options = new OtlpExporterOptions
            {
                TimeoutMilliseconds = OtlpExporterTimeoutMilliseconds
            };

            var endpoint = BuildOtlpEndpoint(backendEndpoint, path);

            if (endpoint == null)
            {
                return options;
            }

            options.Protocol = OtlpExportProtocol.HttpProtobuf;
            options.Endpoint = endpoint;

            return options;
        }

        private static void AddOpenTelemetry(IServiceCollection services,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CacheService cacheService,
            OpenTelemetryExcludeCache openTelemetryExcludeCache, ILog log, ITaskCoordinator taskCoordinator)
        {
            if (!dynamicEnvironment.AppSettings("EnableOpenTelemetry")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var backendEndpoint = dynamicEnvironment.AppSettings("OpenTelemetryBackendEndpoint");

            var dropListener = new OtlpSelfDiagnosticsDropListener();
            services.AddSingleton(dropListener);

            _ = taskCoordinator.RunAsync("OtlpDispatchCounterFlushTask",
                token => FlushOtlpDispatchCountersAsync(dynamicEnvironment, log, token));

            var tracingExporter = new DispatchTrackingExporter<Activity>(
                new OtlpTraceExporter(BuildOtlpExporterOptions(backendEndpoint, "v1/traces")), "traces", log);

            var tracingProcessor = new BatchActivityExportProcessor(tracingExporter, BatchMaxQueueSize,
                BatchScheduledDelayMilliseconds, BatchExporterTimeoutMilliseconds);

            var metricExporter = new DispatchTrackingExporter<Metric>(
                new OtlpMetricExporter(BuildOtlpExporterOptions(backendEndpoint, "v1/metrics")), "metrics", log);

            var metricReader = new PeriodicExportingMetricReader(metricExporter, MetricExportIntervalMilliseconds,
                OtlpExporterTimeoutMilliseconds);

            var logExporter = new DispatchTrackingExporter<LogRecord>(
                new OtlpLogExporter(BuildOtlpExporterOptions(backendEndpoint, "v1/logs")), "logs", log);

            var logProcessor = new BatchLogRecordExportProcessor(logExporter, BatchMaxQueueSize,
                BatchScheduledDelayMilliseconds, BatchExporterTimeoutMilliseconds);

            services.AddOpenTelemetry()
                .ConfigureResource(r => r.AddService("Jube.App",
                    serviceVersion: typeof(Startup).Assembly.GetName().Version?.ToString()))
                .WithTracing(t =>
                {
                    t.AddSource(ServiceDiagnostics.Name)
                        .AddSource(EngineDiagnostics.Name)
                        .AddAspNetCoreInstrumentation(o => o.Filter = ctx =>
                            !ctx.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase))
                        .AddHttpClientInstrumentation();

                    if (cacheService.ConnectionMultiplexer != null)
                    {
                        t.AddRedisInstrumentation(cacheService.ConnectionMultiplexer);
                    }

                    if (cacheService.SentinelMultiplexer != null)
                    {
                        t.AddRedisInstrumentation(cacheService.SentinelMultiplexer);
                    }

                    t.AddProcessor(tracingProcessor);
                })
                .WithMetrics(m => m
                    .AddMeter(ServiceDiagnostics.Name)
                    .AddMeter(EngineDiagnostics.Name)
                    .AddMeter(DataDiagnostics.Name)
                    .AddMeter(CacheDiagnostics.Name)
                    .AddMeter("Microsoft.AspNetCore.Authentication")
                    .AddMeter("Microsoft.AspNetCore.Authorization")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddView(instrument => openTelemetryExcludeCache.IsExcluded(instrument.Name)
                        ? MetricStreamConfiguration.Drop
                        : null)
                    .AddReader(metricReader))
                .WithLogging(l => l.AddProcessor(logProcessor));
        }

        private static async Task FlushOtlpDispatchCountersAsync(
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            ILog log, CancellationToken token)
        {
            const int flushIntervalMilliseconds = 60000;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(flushIntervalMilliseconds, token).ConfigureAwait(false);

                    var snapshot = OtlpDispatchCounters.TakeSnapshot();

                    if (snapshot.Count == 0)
                    {
                        continue;
                    }

                    var instance = Dns.GetHostName();
                    var createdDate = DateTime.UtcNow;

                    var models = snapshot.Select(s => new OtlpDispatchCounter
                    {
                        SignalId = (int)ParseSignal(s.Signal),
                        Count = s.Count,
                        SuccessCount = s.SuccessCount,
                        FailureCount = s.FailureCount,
                        ItemCount = s.ItemCount,
                        DroppedCount = s.DroppedCount,
                        TotalMicroseconds = s.TotalMicroseconds,
                        MinMicroseconds = s.MinMicroseconds,
                        MaxMicroseconds = s.MaxMicroseconds,
                        CreatedDate = createdDate,
                        Instance = instance
                    }).ToList();

                    await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                        dynamicEnvironment.AppSettings("ConnectionString"), log);

                    var repository = new OtlpDispatchCounterRepository(dbContext);
                    await repository.BulkCopyAsync(models, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    log.Error(
                        $"FlushOtlpDispatchCountersAsync: An error flushing OTLP dispatch counters has been observed as {ex}.");
                }
            }
        }

        private static OtlpSignal ParseSignal(string signal)
        {
            return signal switch
            {
                "traces" => OtlpSignal.Traces,
                "metrics" => OtlpSignal.Metrics,
                "logs" => OtlpSignal.Logs,
                _ => OtlpSignal.Unknown
            };
        }

        private static void GetHttpHeadersFromDatabaseAndCreateSingleton(IServiceCollection services,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log, TaskCoordinator taskCoordinator)
        {
            services.AddSingleton(new HttpHeadersFromDatabase(dynamicEnvironment.AppSettings("ConnectionString"), log,
                taskCoordinator.CancellationToken));
        }

        private static void AddSingletonForTokensCache(IServiceCollection services, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CacheService cacheService,
            TaskCoordinator taskCoordinator)
        {
            var apiTokensCache = new ApiTokensCache.ApiTokensCache(log, dynamicEnvironment, cacheService);
            _ = taskCoordinator.RunAsync("InstantiateApiTokensCache", _ => apiTokensCache.StartAsync(taskCoordinator));
            services.AddSingleton(apiTokensCache);
        }

        private static TaskCoordinator AddSingletonForTaskCoordinator(IServiceCollection services,
            ICancellationTokenProvider cancellationTokenProvider, ILog log)
        {
            var taskCoordinator = new TaskCoordinator(cancellationTokenProvider, log);
            services.AddSingleton(taskCoordinator);

            return taskCoordinator;
        }

        private static ICancellationTokenProvider AddSingletonForCancellationToken(IServiceCollection services)
        {
            ICancellationTokenProvider cancellationTokenProvider = new CancellationTokenProvider();
            services.AddSingleton(cancellationTokenProvider);

            return cancellationTokenProvider;
        }

        private static void WriteWelcomeMessageToConsole()
        {
            Console.WriteLine(@"Copyright (C) 2022-present Jube Holdings Limited.");
            Console.WriteLine(@"");
            Console.WriteLine(@"This software is Jube.  Welcome.");
            Console.WriteLine(@"");
            Console.Write(
                @"Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.");
            Console.WriteLine(
                @"Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.");
            Console.WriteLine(@"");
            Console.WriteLine(
                @"You should have received a copy of the GNU Affero General Public License along with Jube™. If not, see <https://www.gnu.org/licenses/>.");

            Console.WriteLine(@"");
            Console.WriteLine(
                @"If you are seeing this message it means that database migrations have completed and the database is fully configured with required Tables, Indexes and Constraints.");
            Console.WriteLine(@"");
            Console.WriteLine(
                @"Comprehensive documentation is available via https://github.com/jube-home/aml-transaction-monitoring.");
            Console.WriteLine(@"");
            Console.WriteLine(
                @"Use a web browser (e.g. Chrome) to navigate to the user interface via default endpoint https://<ASPNETCORE_URLS Environment Variable>/ (for example https://127.0.0.1:5001/ given ASPNETCORE_URLS=https://127.0.0.1:5001/). The default user name \ password is 'Administrator' \ 'Administrator' but will need to be changed on first use.  Availability of the user interface may be a few moments after this messages as the Kestrel web server starts and endpoint routing is established.");
            Console.WriteLine(@"");
            Console.WriteLine(
                @"The default endpoint for posting example transaction payload is https://<ASPNETCORE_URLS Environment Variable>/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc/.Example JSON payload is available in the documentation via at https://jube-home.github.io/aml-transaction-monitoring/Configuration/Models/Models/.");

            Console.WriteLine();
        }

        private static void AddGenericServicesRequired(IServiceCollection services,
            WatcherStreamRegistry watcherStreamRegistry, ServiceChangeStreamRegistry serviceChangeStreamRegistry)
        {
            services.AddHsts(options =>
            {
                options.MaxAge = TimeSpan.FromDays(365);
                options.IncludeSubDomains = true;
            });

            services.AddAuthorization();
            services.AddLocalization();
            services.AddRazorPages();
            services.AddSingleton<IRevocableConnectionRegistry>(watcherStreamRegistry);
            services.AddSingleton<IRevocableConnectionRegistry>(serviceChangeStreamRegistry);
            services.AddHostedService<RevokedSessionSweeper>();
            services.AddSingleton<WafRegistry>();
            services.AddSingleton<WafInspector>();
            services.AddHostedService<WafRegistryRefreshService>();
            services.AddHostedService<WafAttackFlushService>();
            services.AddHttpContextAccessor();

            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                };
            });

            services.AddMvc();

            services.AddEndpointsApiExplorer();
        }

        private static void AddDataProtection(IServiceCollection services,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            if (!dynamicEnvironment.AppSettings("DataProtectionRedisBackplane")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var redisConnection =
                ConnectionMultiplexer.Connect(dynamicEnvironment.AppSettings("RedisConnectionString"));

            services.AddDataProtection()
                .PersistKeysToStackExchangeRedis(redisConnection, "Jube:DataProtection:Keys")
                .SetApplicationName("Jube");
        }

        private static void AddSingletonForIdentity(IServiceCollection services)
        {
            services.AddTransient<IUserStore<ApplicationUser>, UserStore>();
            services.AddTransient<IRoleStore<ApplicationRole>, RoleStore>();
            services.AddIdentity<ApplicationUser, ApplicationRole>().AddDefaultTokenProviders();
        }

        private static void AddSingletonForLoginSourceIpThrottle(IServiceCollection services)
        {
            services.AddSingleton<LoginSourceIpThrottle>();
        }

        private static void AddSingletonForGlobalHttpRequestSourceIpThrottle(IServiceCollection services)
        {
            services.AddSingleton<GlobalHttpRequestSourceIpThrottle>();
        }

        private static TimeSpan ComputeWindow(string interval, string intervalValue)
        {
            var parsedValue = double.TryParse(intervalValue, out var value) ? value : 60;

            return interval switch
            {
                "s" => TimeSpan.FromSeconds(parsedValue),
                "n" => TimeSpan.FromMinutes(parsedValue),
                "h" => TimeSpan.FromHours(parsedValue),
                _ => TimeSpan.FromDays(parsedValue)
            };
        }

        private static void AddSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Jube.App.Api",
                    Version = "v1"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token after \"Bearer\", e.g. \"Bearer eyJhbGci...\""
                });

                c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Name = "x-api-key",
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Description =
                        "Enter your API key.  Obtain this by creating an API Key under a user in the application."
                });

                c.CustomSchemaIds(type => type.FullName);
                c.OperationFilter<AuthorizationHeaderParameterOperationFilter>();
            });
        }

        private static void ConfigureAuthentication(IServiceCollection services,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log)
        {
            var jwtValidAudience = dynamicEnvironment.AppSettings("JWTValidAudience");
            var jwtValidIssuer = dynamicEnvironment.AppSettings("JWTValidIssuer");
            var jwtKey = dynamicEnvironment.AppSettings("JWTKey");

            var authBuilder = services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Hybrid";
                    options.DefaultChallengeScheme = "Hybrid";
                    options.DefaultScheme = "Hybrid";
                })
                .AddScheme<HybridAuthOptions, HybridAuthHandler>("Hybrid", options =>
                {
                    options.JwtKey = jwtKey;
                    options.JwtValidIssuer = jwtValidIssuer;
                    options.JwtValidAudience = jwtValidAudience;
                });

            if (dynamicEnvironment.AppSettings("NegotiateAuthentication")
                .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                authBuilder.AddNegotiate();
            }

            if (dynamicEnvironment.AppSettings("OAuthAuthentication")
                .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                log.Info("Initializing OpenID Connect / OAuth authentication handlers.");

                authBuilder.AddOpenIdConnect("OAuth", options =>
                {
                    options.Authority = dynamicEnvironment.AppSettings("OAuthAuthority");
                    options.ClientId = dynamicEnvironment.AppSettings("OAuthClientId");
                    options.ClientSecret = dynamicEnvironment.AppSettings("OAuthClientSecret");
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.UsePkce = true;
                    options.SaveTokens = false;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.CorrelationCookie.Path = "/";
                    options.NonceCookie.Path = "/";
                    options.CallbackPath = new PathString("/signin-oidc");
                    options.CorrelationCookie.SameSite = SameSiteMode.None;
                    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.NonceCookie.SameSite = SameSiteMode.None;
                    options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;

                    if (dynamicEnvironment.AppSettings("OAuthForceGet")
                        .Equals("True", StringComparison.OrdinalIgnoreCase))
                    {
                        options.ResponseMode = OpenIdConnectResponseMode.Query;
                        log.Info("OIDC ResponseMode explicitly forced to HTTP GET (Query).");
                    }
                    else
                    {
                        options.ResponseMode = OpenIdConnectResponseMode.FormPost;
                    }

                    options.Events = new OpenIdConnectEvents
                    {
                        OnAuthenticationFailed = async context =>
                        {
                            log.Error($"OIDC Authentication Failed. Error: {context.Exception.Message}",
                                context.Exception);

                            await LogOAuthFailureAsync(dynamicEnvironment, log, null, null,
                                context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                                context.HttpContext.Connection.LocalIpAddress?.ToString(),
                                context.HttpContext.Request.Headers["User-Agent"].ToString(),
                                9, context.Exception.Message);
                        },

                        OnRemoteFailure = async context =>
                        {
                            log.Warn($"OIDC Remote Failure encountered. Failure Message: {context.Failure?.Message}");

                            await LogOAuthFailureAsync(dynamicEnvironment, log, null, null,
                                context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                                context.HttpContext.Connection.LocalIpAddress?.ToString(),
                                context.HttpContext.Request.Headers["User-Agent"].ToString(),
                                8, context.Failure?.Message);

                            context.Response.Redirect("/Account/Login");
                            context.HandleResponse();
                        },

                        OnRedirectToIdentityProvider = context =>
                        {
                            context.ProtocolMessage.Prompt = OAuthPromptSelector.Resolve(
                                dynamicEnvironment.AppSettings("OAuthPrompt"), context.ProtocolMessage.Prompt);

                            log.Debug(
                                $"Redirecting to Entra ID. Redirect URI constructed by app: {context.ProtocolMessage.RedirectUri}");

                            return Task.CompletedTask;
                        },

                        OnTicketReceived = async context =>
                        {
                            log.Info("OAuth Ticket received from identity provider. Commencing claim parsing.");

                            var userName = context?.Principal?.Identity?.Name
                                           ?? context?.Principal?.FindFirstValue(ClaimTypes.Name)
                                           ?? context?.Principal?.FindFirstValue("preferred_username")
                                           ?? context?.Principal?.FindFirstValue(ClaimTypes.Email)
                                           ?? context?.Principal?.FindFirstValue("name")
                                           ?? context?.Principal?.FindFirstValue("email")
                                           ?? context?.Principal?.FindFirstValue(ClaimTypes.Upn)
                                           ?? context?.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                            var oauthRemoteIp = context?.HttpContext.Connection.RemoteIpAddress?.ToString();
                            var oauthLocalIp = context?.HttpContext.Connection.LocalIpAddress?.ToString();
                            var oauthUserAgent = context?.HttpContext.Request.Headers["User-Agent"].ToString();

                            if (string.IsNullOrEmpty(userName))
                            {
                                log.Error(
                                    "OAuth ticket parsing aborted: No usable identity claim found in Principal payload.");

                                await LogOAuthFailureAsync(dynamicEnvironment, log, null, null, oauthRemoteIp,
                                    oauthLocalIp, oauthUserAgent, 7,
                                    "No usable identity claim found in principal payload.");

                                context?.Fail("OAuth ticket contained no usable identity claim.");

                                return;
                            }

                            log.Info(
                                $"Parsed username '{userName}' from claims token. Validating user in database registry.");

                            DbContext dbContext;

                            try
                            {
                                dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                                    dynamicEnvironment.AppSettings("ConnectionString"), log);
                            }
                            catch (Exception ex)
                            {
                                log.Error(
                                    $"Critical exception establishing database context for '{userName}': {ex.Message}",
                                    ex);

                                await LogOAuthFailureAsync(dynamicEnvironment, log, null, userName, oauthRemoteIp,
                                    oauthLocalIp, oauthUserAgent, 10, ex.Message);

                                context.Fail("Internal server error validating identity.");

                                return;
                            }

                            try
                            {
                                var userRegistryRepository = new UserRegistryRepository(dbContext);
                                var userRegistry = await userRegistryRepository.GetByUserNameAsync(userName);

                                if (userRegistry == null)
                                {
                                    log.Warn(
                                        $"Authentication Denied: User '{userName}' does not exist in Jube database.");

                                    await LogOAuthFailureAsync(dynamicEnvironment, log, dbContext, userName,
                                        oauthRemoteIp, oauthLocalIp, oauthUserAgent, 1, null);

                                    context.Fail("User does not exist in Jube.");

                                    return;
                                }

                                if (userRegistry.Active != 1)
                                {
                                    log.Warn(
                                        $"Authentication Denied: User '{userName}' exists but account status is inactive (Active flag: {userRegistry.Active}).");

                                    await LogOAuthFailureAsync(dynamicEnvironment, log, dbContext, userName,
                                        oauthRemoteIp, oauthLocalIp, oauthUserAgent, 2, null);

                                    context.Fail("User does not exist in Jube.");

                                    return;
                                }

                                var userLoginRepository = new UserLoginRepository(dbContext, userName);

                                var userLogin = new UserLogin
                                {
                                    RemoteIp = oauthRemoteIp,
                                    LocalIp = oauthLocalIp,
                                    UserAgent = oauthUserAgent,
                                    Failed = 0,
                                    AuthenticationTypeId = 3
                                };

                                await userLoginRepository.InsertAsync(userLogin);
                                log.Info($"Login audit trail inserted for user '{userName}' from IP: {oauthRemoteIp}.");

                                var forcedRedirect = dynamicEnvironment.AppSettings("OAuthForceRedirect");
                                string targetRedirectUri;

                                if (!string.IsNullOrWhiteSpace(forcedRedirect))
                                {
                                    if (Uri.TryCreate(forcedRedirect, UriKind.Absolute, out var forcedUri))
                                    {
                                        targetRedirectUri = forcedUri.ToString();

                                        log.Info(
                                            $"OAuthForceRedirect override active. Redirect target forced to: {targetRedirectUri}");
                                    }
                                    else
                                    {
                                        log.Warn(
                                            $"OAuthForceRedirect value '{forcedRedirect}' is not a valid absolute URI. Ignoring override.");

                                        targetRedirectUri = context.Properties?.RedirectUri ?? "/";
                                    }
                                }
                                else
                                {
                                    targetRedirectUri = context.Properties?.RedirectUri ?? "/";
                                }

                                log.Info(
                                    $"User '{userName}' verified. Target UI path destination: {targetRedirectUri}");

                                AuthenticationCookieIssuer.IssueAuthenticationCookies(context.Response,
                                    dynamicEnvironment, userName);

                                var useInterstitial = dynamicEnvironment.AppSettings("OAuthSignInInterstitial")
                                    .Equals("True", StringComparison.OrdinalIgnoreCase);

                                if (useInterstitial && OAuthSignInInterstitial.IsSafeTarget(targetRedirectUri))
                                {
                                    OAuthSignInInterstitial.Write(context.Response, targetRedirectUri);
                                }
                                else
                                {
                                    OAuthSignInInterstitial.ApplyNoCacheHeaders(context.Response);
                                    context.Response.Redirect(targetRedirectUri);
                                }

                                context.HandleResponse();
                            }
                            catch (Exception ex)
                            {
                                log.Error(
                                    $"Critical exception during User OIDC Post-Ticket Processing for '{userName}': {ex.Message}",
                                    ex);

                                await LogOAuthFailureAsync(dynamicEnvironment, log, dbContext, userName,
                                    oauthRemoteIp, oauthLocalIp, oauthUserAgent, 10, ex.Message);

                                context.Fail("Internal server error validating identity.");
                            }
                            finally
                            {
                                try
                                {
                                    await dbContext.CloseAsync();
                                }
                                finally
                                {
                                    await dbContext.DisposeAsync();
                                }
                            }
                        }
                    };
                });
            }
        }

        private static async Task LogOAuthFailureAsync(DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            ILog log, DbContext existingDbContext, string userName, string remoteIp, string localIp,
            string userAgent, int failureTypeId, string failureMessage)
        {
            var dbContext = existingDbContext;
            var ownsDbContext = dbContext == null;

            try
            {
                dbContext ??= DataConnectionDbContext.GetResilientDbContextDataConnection(
                    dynamicEnvironment.AppSettings("ConnectionString"), log);

                var userLoginRepository = new UserLoginRepository(dbContext, userName);

                await userLoginRepository.InsertAsync(new UserLogin
                {
                    RemoteIp = remoteIp,
                    LocalIp = localIp,
                    UserAgent = userAgent,
                    Failed = 1,
                    AuthenticationTypeId = 3,
                    FailureTypeId = failureTypeId,
                    FailureMessage = failureMessage
                });
            }
            catch (Exception ex)
            {
                log.Warn($"LogOAuthFailureAsync: unable to record OAuth login failure audit row: {ex.Message}");
            }
            finally
            {
                if (ownsDbContext && dbContext != null)
                {
                    try
                    {
                        await dbContext.CloseAsync();
                    }
                    finally
                    {
                        await dbContext.DisposeAsync();
                    }
                }
            }
        }

        private static void AddSingletonForEngine(IServiceCollection services
            , DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log, IConnection rabbitMqConnection,
            CacheService cacheService, JsonSerializationHelper jsonSerializationHelper,
            ITaskCoordinator taskCoordinator, IImplicitAsyncInvocationTracker implicitAsyncInvocationTracker,
            OpenTelemetryExcludeCache openTelemetryExcludeCache, LogCounterRuleCache logCounterRuleCache)
        {
            if (!dynamicEnvironment.AppSettings("EnableEngine")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var engine = new Engine.Engine(dynamicEnvironment, log, rabbitMqConnection, cacheService,
                jsonSerializationHelper, taskCoordinator, implicitAsyncInvocationTracker,
                dynamicEnvironment.AppSettings("ReportConnectionString"), openTelemetryExcludeCache,
                logCounterRuleCache);

            services.AddSingleton(engine);
        }

        private static OpenTelemetryExcludeCache AddSingletonForOpenTelemetryExcludeCache(IServiceCollection services,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ITaskCoordinator taskCoordinator)
        {
            var openTelemetryExcludeCache =
                new OpenTelemetryExcludeCache(dynamicEnvironment.AppSettings("ConnectionString"), log);

            _ = taskCoordinator.RunAsync("OpenTelemetryExcludeCacheTask",
                _ => openTelemetryExcludeCache.StartAsync(taskCoordinator.CancellationToken));

            services.AddSingleton(openTelemetryExcludeCache);

            return openTelemetryExcludeCache;
        }

        private static LogCounterRuleCache AddSingletonForLogCounterRuleCache(IServiceCollection services, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ITaskCoordinator taskCoordinator,
            OpenTelemetryExcludeCache openTelemetryExcludeCache)
        {
            var logCounterRuleCache = new LogCounterRuleCache(dynamicEnvironment.AppSettings("ConnectionString"), log)
            {
                ExcludeCache = openTelemetryExcludeCache
            };

            _ = taskCoordinator.RunAsync("LogCounterRuleCacheTask",
                _ => logCounterRuleCache.StartAsync(taskCoordinator.CancellationToken));

            services.AddSingleton(logCounterRuleCache);

            return logCounterRuleCache;
        }

        private static IImplicitAsyncInvocationTracker AddSingletonForImplicitAsyncInvocationTracker(
            IServiceCollection services, ILog log)
        {
            var implicitAsyncInvocationTracker = new ImplicitAsyncInvocationTracker(log);
            services.AddSingleton(implicitAsyncInvocationTracker);

            return implicitAsyncInvocationTracker;
        }

        private static IConnection AddSingletonForRabbitMqConnection(IServiceCollection services,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log)
        {
            IConnection rabbitMqConnection = null;

            if (dynamicEnvironment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rabbitMqConnection = ConnectToRabbitMqChannel(services, log, dynamicEnvironment.AppSettings("AMQPUri"),
                    ComputeWindow(dynamicEnvironment.AppSettings("AMQPHeartbeatInterval"),
                        dynamicEnvironment.AppSettings("AMQPHeartbeatIntervalValue")));
            }
            else
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        "Start: No connection to AMQP is being made.  AMQP will be bypassed throughout the application.");
                }
            }

            return rabbitMqConnection;
        }

        private static ConcurrentDictionary<Guid, TaskCompletionSource<Callback>> AddSingletonForCallbacks(
            IServiceCollection services)
        {
            var callbacks = new ConcurrentDictionary<Guid, TaskCompletionSource<Callback>>();
            services.AddSingleton(callbacks);

            return callbacks;
        }

        private static ServiceChangeStreamRegistry AddSingletonForServiceChangeStreamRegistry(
            IServiceCollection services, ILog log)
        {
            var serviceChangeStreamRegistry = new ServiceChangeStreamRegistry(log);
            services.AddSingleton(serviceChangeStreamRegistry);

            return serviceChangeStreamRegistry;
        }

        private static void AddSingletonForAskJoobyAgentGuidanceExchange(IServiceCollection services)
        {
            services.AddSingleton<IAgentGuidanceExchange>(new AgentGuidanceExchange());
        }

        private static void AddSingletonForAskJoobyTurnTracker(IServiceCollection services)
        {
            services.AddSingleton<IAskJoobyTurnTracker>(new AskJoobyTurnTracker());
        }

        private static WatcherStreamRegistry AddSingletonForWatcherStreamRegistry(IServiceCollection services,
            ILog log)
        {
            var watcherStreamRegistry = new WatcherStreamRegistry(log);
            services.AddSingleton(watcherStreamRegistry);

            return watcherStreamRegistry;
        }

        private static CacheService AddSingletonForCacheService(IServiceCollection services,
            ConcurrentDictionary<Guid, TaskCompletionSource<Callback>> callbacks, int callbackTimeout,
            TaskCoordinator taskCoordinator, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log,
            WatcherStreamRegistry watcherStreamRegistry)
        {
            var lruJournalMaxAgeInterval = dynamicEnvironment.AppSettings("LruJournalMaxAgeInterval");
            var lruJournalMaxAgeValue = dynamicEnvironment.AppSettings("LruJournalMaxAgeValue");

            if (!double.TryParse(lruJournalMaxAgeValue, out var value))
            {
                value = 1;
            }

            var lruJournalMaxAgeTimeSpan = lruJournalMaxAgeInterval switch
            {
                "n" =>
                    TimeSpan.FromMinutes(value),
                "h" =>
                    TimeSpan.FromHours(value),
                _ => TimeSpan.FromDays(value)
            };

            var activationRuleIdempotency = dynamicEnvironment.AppSettings("ActivationRuleIdempotency")
                .Equals("True", StringComparison.OrdinalIgnoreCase);

            var cacheService =
                ConnectToRedis(dynamicEnvironment.AppSettings("RedisConnectionString"),
                    dynamicEnvironment.AppSettings("ConnectionString"),
                    callbacks,
                    callbackTimeout,
                    dynamicEnvironment.AppSettings("LocalCache").Equals("True", StringComparison.OrdinalIgnoreCase),
                    dynamicEnvironment.AppSettings("LocalCacheFill").Equals("True", StringComparison.OrdinalIgnoreCase),
                    long.Parse(dynamicEnvironment.AppSettings("LocalCacheBytes")),
                    dynamicEnvironment.AppSettings("RedisMessagePackCompression")
                        .Equals("True", StringComparison.OrdinalIgnoreCase),
                    dynamicEnvironment.AppSettings("RedisStorePayloadCountsAndBytes")
                        .Equals("True", StringComparison.OrdinalIgnoreCase),
                    dynamicEnvironment.AppSettings("RedisPublishSubscribeEvents")
                        .Equals("True", StringComparison.OrdinalIgnoreCase),
                    dynamicEnvironment.AppSettings("RedisHsetOffloadToPostgres")
                        .Equals("True", StringComparison.OrdinalIgnoreCase),
                    lruJournalMaxAgeTimeSpan,
                    activationRuleIdempotency,
                    log,
                    int.Parse(dynamicEnvironment.AppSettings("RedisSentinelConnectTimeout")),
                    int.Parse(dynamicEnvironment.AppSettings("RedisSentinelSyncTimeout")),
                    int.Parse(dynamicEnvironment.AppSettings("RedisSentinelConnectRetry")),
                    int.Parse(dynamicEnvironment.AppSettings("RedisReconnectRetryBaseDelay")),
                    int.Parse(dynamicEnvironment.AppSettings("RedisReconnectRetryMaxDelay")),
                    dynamicEnvironment.AppSettings("RedisBacklogFailFast")
                        .Equals("True", StringComparison.OrdinalIgnoreCase));

            if (dynamicEnvironment.AppSettings("EnableMigration").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                RunFluentMigrator(dynamicEnvironment, cacheService, log);
            }

            cacheService.InstantiateRepositoriesTask = taskCoordinator.RunAsync("InstantiateRepositoriesAsync",
                _ => cacheService.StartAsync(taskCoordinator, watcherStreamRegistry.Dispatch));

            services.AddSingleton(cacheService);

            return cacheService;
        }

        private static DynamicEnvironment.DynamicEnvironment AddSingletonForDynamicEnvironmentAndLogging(
            IServiceCollection services, out ILog log)
        {
            var dynamicEnvironment = new DynamicEnvironment.DynamicEnvironment();
            log = dynamicEnvironment.Log;

            PoolClearGate.ConfigureShared(TimeSpan.FromMilliseconds(
                int.Parse(dynamicEnvironment.AppSettings("PgPoolClearDebounceMilliseconds"))));

            PgRetryBudget.ConfigureShared(int.Parse(dynamicEnvironment.AppSettings("PgMaxConnectionRetries")));

            services.AddSingleton(log);
            services.AddSingleton(dynamicEnvironment);

            return dynamicEnvironment;
        }

        private static JsonSerializationHelper AddSingletonForJsonSerializationHelper(IServiceCollection services)
        {
            var jsonSerializationHelper = new JsonSerializationHelper();
            services.AddSingleton(jsonSerializationHelper);

            return jsonSerializationHelper;
        }

        private static void ValidateConnectionToPostgres(string connectionString, ILog log)
        {
            const int retryConnectionToPostgres = 10;

            for (var i = 0; i < retryConnectionToPostgres; i++)
            {
                try
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info("Is attempting a connection validation for Postgres.");
                    }

                    var connection = new NpgsqlConnection(connectionString);
                    var command = new NpgsqlCommand("select true");
                    connection.Open();
                    command.Connection = connection;
                    command.ExecuteNonQuery();
                    connection.Close();

                    if (log.IsInfoEnabled)
                    {
                        log.Info("Postgres connection validated.");
                    }

                    return;
                }
                catch (Exception ex)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Could not connect to Postgres after {i} attempts for {ex.Message}.");
                    }

                    Thread.Sleep(6000);
                }
            }

            throw new Exception($"Could not connect to Postgres after {retryConnectionToPostgres}.");
        }

        private static IConnection ConnectToRabbitMqChannel(IServiceCollection services, ILog log,
            string amqpUrl, TimeSpan heartbeat)
        {
            const int retryRabbitMqConnection = 10;

            for (var i = 0; i < retryRabbitMqConnection; i++)
            {
                try
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info("Start: Is going to make a connection to AMQP Uri " +
                                 amqpUrl + "");
                    }

                    var uri = new Uri(amqpUrl);

                    var rabbitMqConnectionFactory = new ConnectionFactory
                    {
                        Uri = uri,
                        RequestedHeartbeat = heartbeat
                    };

                    var rabbitMqConnection = rabbitMqConnectionFactory.CreateConnection();
                    services.AddSingleton(rabbitMqConnection);

                    if (log.IsInfoEnabled)
                    {
                        log.Info("Start: Has made a connection to AMQP Uri " + amqpUrl + "");
                    }

                    services.AddSingleton(rabbitMqConnection);

                    return rabbitMqConnection;
                }
                catch (Exception ex)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Start: Error making a connection to AMQP Uri after {i} attempts " +
                                 amqpUrl + " with error " + ex);
                    }

                    Thread.Sleep(3000);
                }
            }

            throw new Exception($"Could not connect to RabbitMQ after {retryRabbitMqConnection} attempts.");
        }

        private static CacheService ConnectToRedis(string redisConnectionString,
            string postgresConnectionString,
            ConcurrentDictionary<Guid, TaskCompletionSource<Callback>> callbacks,
            int callbackTimeout,
            bool localCache, bool localCacheFill,
            long localCacheBytes,
            bool messagePackCompression,
            bool storePayloadCountsAndBytes,
            bool publishSubscribe, bool hsetOffload,
            TimeSpan maxLruAge,
            bool activationRuleIdempotency,
            ILog log,
            int sentinelConnectTimeoutMilliseconds,
            int sentinelSyncTimeoutMilliseconds,
            int sentinelConnectRetry,
            int reconnectRetryBaseDelayMilliseconds,
            int reconnectRetryMaxDelayMilliseconds,
            bool backlogFailFast)
        {
            const int retryRedisConnectionRetry = 10;

            for (var i = 0; i < retryRedisConnectionRetry; i++)
            {
                try
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info("Start: Is going to make a connection to Redis Endpoints string showing " +
                                 "endpoints and port separated by :,  then combined separated by comma " +
                                 "for example localhost:1234,localhost4321.  Value for parsing is " +
                                 redisConnectionString + "");
                    }

                    var cacheService = new CacheService(redisConnectionString,
                        postgresConnectionString, callbacks,
                        callbackTimeout,
                        localCache, localCacheFill,
                        localCacheBytes, messagePackCompression, storePayloadCountsAndBytes,
                        publishSubscribe, hsetOffload, maxLruAge, activationRuleIdempotency, log,
                        sentinelConnectTimeoutMilliseconds, sentinelSyncTimeoutMilliseconds, sentinelConnectRetry,
                        reconnectRetryBaseDelayMilliseconds, reconnectRetryMaxDelayMilliseconds, backlogFailFast);

                    if (log.IsInfoEnabled)
                    {
                        log.Info("Connected to Redis.  Returning connection for startup.");
                    }

                    return cacheService;
                }
                catch (Exception ex)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Can't make a connection to Redis after {i} attempt(s) for {ex.Message}.");
                    }

                    Thread.Sleep(1500);
                }
            }

            throw new Exception($"Could not connect to Redis after {retryRedisConnectionRetry} attempts.");
        }

        private static async Task StartEngineInBackgroundAsync(IApplicationBuilder app, ILog log)
        {
            try
            {
                await app.StartEngineAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Error in App Configure as {ex}");
            }
        }

        private static void RunFluentMigrator(DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            CacheService cacheService, ILog log)
        {
            string connectionString;

            if (dynamicEnvironment.AppSettings("MigrationConnectionString") != null)
            {
                connectionString = dynamicEnvironment.AppSettings("MigrationConnectionString");
            }
            else
            {
                connectionString = dynamicEnvironment.AppSettings("ConnectionString");

                if (log.IsWarnEnabled)
                {
                    log.Warn("No MigrationConnectionString Environment Variable available.");
                }
            }

            var serviceCollection = new ServiceCollection().AddFluentMigratorCore()
                .AddSingleton(dynamicEnvironment)
                .AddSingleton(cacheService)
                .AddSingleton(log)
                .ConfigureRunner(rb => rb
                    .AddPostgres11_0()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(AddActivationWatcherTableIndex).Assembly).For.Migrations())
                .BuildServiceProvider(false);

            using var scope = serviceCollection.CreateScope();
            var runner = serviceCollection.GetRequiredService<IMigrationRunner>();
            runner.MigrateUp();
        }

        private void ConfigureThreadPool(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log)
        {
            if (dynamicEnvironment.AppSettings("ThreadPoolManualControl")
                .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                ThreadPool.SetMinThreads(int.Parse(dynamicEnvironment.AppSettings("MinThreadPoolThreads")),
                    int.Parse(dynamicEnvironment.AppSettings("MinThreadPoolThreads")));

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Start: Set the min threads to {dynamicEnvironment.AppSettings("MinThreadPoolThreads")} from the configuration file.");
                }

                ThreadPool.SetMaxThreads(int.Parse(dynamicEnvironment.AppSettings("MaxThreadPoolThreads")),
                    int.Parse(dynamicEnvironment.AppSettings("MaxThreadPoolThreads")));

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Start: Set the max threads to {int.Parse(dynamicEnvironment.AppSettings("MaxThreadPoolThreads"))} from the configuration file.");
                }
            }
            else
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        "Start: No manual thread pool parameters have been set will configure based on CPU count and certain other estimates.");
                }

                var logicalCores = Environment.ProcessorCount;
                var workerThreads = logicalCores * 2;
                var ioThreads = logicalCores * 4;
                ThreadPool.SetMinThreads(workerThreads, ioThreads);
            }
        }
    }
}
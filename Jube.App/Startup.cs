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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentMigrator.Runner;
using Jube.App.Code;
using Jube.App.Code.ServiceChange;
using Jube.App.Code.signalr;
using Jube.App.Code.WatcherDispatch;
using Jube.App.Endpoints;
using Jube.App.Endpoints.Mocks;
using Jube.App.Endpoints.Query;
using Jube.App.Endpoints.Repository;
using Jube.App.Middlewares;
using Jube.App.Middlewares.Extensions;
using Jube.App.Middlewares.Models;
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
using Jube.Service.Observability;
using Jube.Service.Observability.OtlpDispatchCounters;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.TaskCancellation;
using Jube.TaskCancellation.Interfaces;
using log4net;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using StackExchange.Redis;

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

            var cacheService = AddSingletonForCacheService(services, callbacks,
                int.Parse(dynamicEnvironment.AppSettings("CallbackTimeout") ?? "10000"),
                taskCoordinator, dynamicEnvironment, log);

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
            ConfigureAuthentication(services, dynamicEnvironment, log);
            AddGenericServicesRequired(services, dynamicEnvironment);
            AddDataProtection(services, dynamicEnvironment);
            AddSwagger(services);
            AddSingletonRelayToBeInstantiatedInConfigureServices(services, dynamicEnvironment);
            AddSingletonForServiceChangeBus(services, dynamicEnvironment, cacheService);

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

        private static void AddSingletonForServiceChangeBus(IServiceCollection services,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CacheService cacheService)
        {
            if (!dynamicEnvironment.AppSettings("EnableServiceChangeStream")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton<IServiceChangeBus, NullServiceChangeBus>();
                return;
            }

            if (dynamicEnvironment.AppSettings("RedisBackplane").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton<IServiceChangeBus>(new RedisServiceChangeBus(cacheService.ConnectionMultiplexer));
            }
            else
            {
                services.AddSingleton<IServiceChangeBus, InProcessServiceChangeBus>();
            }

            services.AddSingleton<ServiceChangeRelay>();
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

        private static void AddSingletonRelayToBeInstantiatedInConfigureServices(IServiceCollection services,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            if (!dynamicEnvironment.AppSettings("StreamingActivationWatcher")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            services.AddSingleton<Relay>();
        }

        private static void AddGenericServicesRequired(IServiceCollection services,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            services.AddAuthorization();
            services.AddLocalization();
            services.AddRazorPages();
            services.AddSingleton(WatcherConnectionRegistry.Instance);
            services.AddHostedService<WatcherConnectionSweeper>();
            services.AddHttpContextAccessor();
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                };
            });
            services.AddMvc();

            if (dynamicEnvironment.AppSettings("RedisBackplane").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                services.AddSignalR().AddStackExchangeRedis(dynamicEnvironment.AppSettings("RedisConnectionString"));
            }
            else
            {
                services.AddSignalR();
            }

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

                                context.Response.Redirect(targetRedirectUri);
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
                                await dbContext.CloseAsync();
                                await dbContext.DisposeAsync();
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
                    await dbContext.CloseAsync();
                    await dbContext.DisposeAsync();
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
                    int.Parse(dynamicEnvironment.AppSettings("AMQPHeartbeatSeconds") ?? "30"));
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

        private static CacheService AddSingletonForCacheService(IServiceCollection services,
            ConcurrentDictionary<Guid, TaskCompletionSource<Callback>> callbacks, int callbackTimeout,
            TaskCoordinator taskCoordinator, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log)
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
                _ => cacheService.StartAsync(taskCoordinator));

            services.AddSingleton(cacheService);

            return cacheService;
        }

        private static DynamicEnvironment.DynamicEnvironment AddSingletonForDynamicEnvironmentAndLogging(
            IServiceCollection services, out ILog log)
        {
            var dynamicEnvironment = new DynamicEnvironment.DynamicEnvironment();
            log = dynamicEnvironment.Log;

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

#pragma warning disable VSTHRD002
                    Task.Delay(6000).Wait();
#pragma warning restore VSTHRD002
                }
            }

            throw new Exception($"Could not connect to Postgres after {retryConnectionToPostgres}.");
        }

        private static IConnection ConnectToRabbitMqChannel(IServiceCollection services, ILog log,
            string amqpUrl, int heartbeat)
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
                        RequestedHeartbeat = TimeSpan.FromSeconds(heartbeat)
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

#pragma warning disable VSTHRD002
                    Task.Delay(3000).Wait();
#pragma warning restore VSTHRD002
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

#pragma warning disable VSTHRD002
                    Task.Delay(1500).Wait();
#pragma warning restore VSTHRD002
                }
            }

            throw new Exception($"Could not connect to Redis after {retryRedisConnectionRetry} attempts.");
        }

#pragma warning disable AsyncFixer03
#pragma warning disable VSTHRD100
        // ReSharper disable once AsyncVoidMethod
        public async void Configure(IApplicationBuilder app, IWebHostEnvironment env,
#pragma warning restore VSTHRD100
#pragma warning restore AsyncFixer03
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

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseMiddleware<TokenRefreshMiddleware>()
                );

                var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
                app.UseWhen(context => context.Request.Path.StartsWithSegments("/api") &&
                                       lifetime.ApplicationStopping.IsCancellationRequested,
                    branch => branch.UseConditionalConnectionCloseWhenStopping());

                app.UseWhen(context => context.Request.Path.StartsWithSegments("/Account/Login"), appBuilder =>
                {
                    appBuilder.Use(async (context, next) =>
                    {
                        context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors 'none'");
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
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseSwagger()
                );

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseSwaggerUI()
                );

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapRazorPages();
                    endpoints.MapControllers();
                    endpoints.MapHub<WatcherHub>("/watcherHub").RequireAuthorization();
                    endpoints.MapHub<ServiceChangeHub>("/serviceChangeHub").RequireAuthorization();
                    endpoints.MapEntityAnalysisModelEndpoints();
                    endpoints.MapEntityAnalysisModelRequestXPathEndpoints();
                    endpoints.MapEntityAnalysisModelInlineFunctionEndpoints();
                    endpoints.MapEntityAnalysisModelInlineScriptEndpoints();
                    endpoints.MapEntityAnalysisModelGatewayRuleEndpoints();
                    endpoints.MapEntityAnalysisModelSanctionEndpoints();
                    endpoints.MapEntityAnalysisModelTagEndpoints();
                    endpoints.MapEntityAnalysisModelStagePerformanceCounterEndpoints();
                    endpoints.MapEntityAnalysisModelResponseTimePipelineCounterEndpoints();
                    endpoints.MapEntityAnalysisModelTaskPerformanceCounterEndpoints();
                    endpoints.MapApplicationLogEntryEndpoints();
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
                    endpoints.MapPostgresActivityEndpoints();
                    endpoints.MapPostgresStatementStatisticsEndpoints();
                    endpoints.MapEntityAnalysisModelAbstractionRuleEndpoints();
                    endpoints.MapEntityAnalysisModelAbstractionCalculationEndpoints();
                    endpoints.MapEntityAnalysisModelHttpAdaptationEndpoints();
                    endpoints.MapExhaustiveSearchInstanceEndpoints();
                    endpoints.MapEntityAnalysisModelActivationRuleEndpoints();
                    endpoints.MapEntityAnalysisInlineScriptEndpoints();
                    endpoints.MapEntityAnalysisModelTtlCounterEndpoints();
                    endpoints.MapEntityAnalysisModelListEndpoints();
                    endpoints.MapEntityAnalysisModelListValueEndpoints();
                    endpoints.MapEntityAnalysisModelDictionaryEndpoints();
                    endpoints.MapEntityAnalysisModelDictionaryKvpEndpoints();
                    endpoints.MapEntityAnalysisModelSuppressionEndpoints();
                    endpoints.MapEntityAnalysisModelActivationRuleSuppressionEndpoints();
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
                    endpoints.MapEntityAnalysisModelActivationRuleSuppressionQueryEndpoints();
                    endpoints.MapEntityAnalysisModelSampleEndpoints();
                    endpoints.MapEntityAnalysisModelSuppressionQueryEndpoints();
                    endpoints.MapEntityAnalysisModelSynchronisationNodeStatusEntriesEndpoints();
                    endpoints.MapEntityAnalysisPotentialMultiPartStringNamesEndpoints();
                    endpoints.MapEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeEndpoints();
                    endpoints.MapExhaustiveSearchInstancePromotedTrialInstanceConfusionEndpoints();
                    endpoints.MapExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramEndpoints();
                    endpoints.MapExhaustiveSearchInstancePromotedTrialInstanceLearningCurveEndpoints();
                    endpoints.MapExhaustiveSearchInstancePromotedTrialInstancePredictedActualEndpoints();
                    endpoints.MapExhaustiveSearchInstancePromotedTrialInstanceQueryEndpoints();
                    endpoints.MapExhaustiveSearchInstancePromotedTrialInstanceRocEndpoints();
                    endpoints.MapExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionEndpoints();
                    endpoints.MapExhaustiveSearchInstanceTrialInstanceVariableEndpoints();
                    endpoints.MapExhaustiveSearchInstanceTrialInstanceVariableVarianceEndpoints();
                    endpoints.MapExhaustiveSearchInstanceVariableEndpoints();
                    endpoints.MapVisualisationRegistryDatasourceCommandExecutionEndpoints();
                    endpoints.MapTreeChildrenEndpoints();
                    endpoints.MapSessionCaseJournalEndpoints();
                    endpoints.MapSessionCaseSearchCompiledSqlEndpoints();
                    endpoints.MapPreservationEndpoints();
                    endpoints.MapReadyEndpoints();
                    endpoints.MapAuthenticationEndpoints();
                    endpoints.MapMockHttpAdaptationEndpoints();
                    endpoints.MapMockRsaMfaEndpoints();
                    endpoints.MapInvokeEndpoints();
                    endpoints.MapCompletionsEndpoints();
                    endpoints.MapIconsEndpoints();
                    endpoints.MapParserEndpoints();
                    endpoints.MapCaseWorkflowDisplayExecutionEndpoints();
                    endpoints.MapCaseWorkflowMacroExecutionEndpoints();
                    endpoints.MapRegisterSignalrConnectionEndpoints();
                });

                await app.StartRelayAsync().ConfigureAwait(false);
                await app.StartServiceChangeRelayAsync().ConfigureAwait(false);
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
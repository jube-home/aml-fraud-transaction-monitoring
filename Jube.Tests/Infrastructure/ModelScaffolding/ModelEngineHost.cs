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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jube.Cache;
using Jube.Engine.EntityAnalysisModelInvoke.ImplicitAsync;
using Jube.Engine.Helpers;
using Jube.TaskCancellation;

namespace Jube.Test.Infrastructure.ModelScaffolding
{
    public sealed class ModelEngineHost : IAsyncDisposable
    {
        private readonly CancellationTokenProvider cancellation;
        private readonly Task startTask;

        private ModelEngineHost(Jube.DynamicEnvironment.DynamicEnvironment environment, TestLog log,
            global::Jube.Engine.Engine engine, CancellationTokenProvider cancellation, Task startTask)
        {
            Environment = environment;
            Log = log;
            Engine = engine;
            this.cancellation = cancellation;
            this.startTask = startTask;
        }

        public Jube.DynamicEnvironment.DynamicEnvironment Environment { get; }

        public TestLog Log { get; }

        public global::Jube.Engine.Engine Engine { get; }

        public static string RedisConnectionString { get; } =
            System.Environment.GetEnvironmentVariable("JubeTestRedisConnectionString") ?? "localhost";

        private static Dictionary<string, string> DefaultSettings() => new()
        {
            ["ConnectionString"] = ModelScaffold.ConnectionString,
            ["ReportConnectionString"] = ModelScaffold.ConnectionString,
            ["JWTKey"] = "an-adequately-long-unit-test-signing-key-0123456789",
            ["RedisConnectionString"] = RedisConnectionString,
            ["AMQP"] = "False",
            ["EnableEngine"] = "True",
            ["EnablePublicInvokeController"] = "True",
            ["EnableMigration"] = "False",
            ["EnableExhaustiveTraining"] = "False",
            ["EnableReprocessing"] = "False",
            ["EnableSanctionLoader"] = "False",
            ["EnableCasesAutomation"] = "False",
            ["EnableTtlCounter"] = "False",
            ["EnableSearchKeyCache"] = "False",
            ["CachePruneServer"] = "False",
            ["EnableOpenTelemetry"] = "False",
            ["StreamingActivationWatcher"] = "False",
            ["ModelSynchronisationWait"] = "500",
            ["CaseCreationThreads"] = "1",
            ["LocalCache"] = "True",
            ["LocalCacheFill"] = "False",
            ["LocalCacheBytes"] = "50000000",
            ["MaxInvokeControllerRequestBytes"] = "20000",
            ["CallbackTimeout"] = "10000"
        };

        public static Jube.DynamicEnvironment.DynamicEnvironment CreateEnvironment(
            IReadOnlyDictionary<string, string>? overrides = null)
        {
            var settings = DefaultSettings();
            if (overrides == null)
            {
                return TestDynamicEnvironment.Create(settings);
            }

            foreach (var (key, value) in overrides)
            {
                settings[key] = value;
            }

            return TestDynamicEnvironment.Create(settings);
        }

        public static async Task<ModelEngineHost> StartAsync(IReadOnlyDictionary<string, string>? overrides = null,
            TimeSpan? readyTimeout = null)
        {
            var environment = CreateEnvironment(overrides);
            var log = new TestLog(false);
            var callbacks =
                new ConcurrentDictionary<Guid, TaskCompletionSource<global::Jube.Cache.Redis.Callback.Callback>>();
            var cancellation = new CancellationTokenProvider();
            var taskCoordinator = new TaskCoordinator(cancellation, log);

            var cacheService = new CacheService(RedisConnectionString, ModelScaffold.ConnectionString, callbacks,
                int.Parse(environment.AppSettings("CallbackTimeout")), true, false, 50_000_000, false, false, false,
                false, TimeSpan.FromDays(1), false, log);
            cacheService.InstantiateRepositoriesTask = taskCoordinator.RunAsync("InstantiateRepositoriesAsync",
                _ => cacheService.StartAsync(taskCoordinator));

            var engine = new global::Jube.Engine.Engine(environment, log, null, cacheService,
                new JsonSerializationHelper(), taskCoordinator, new ImplicitAsyncInvocationTracker(log),
                ModelScaffold.ConnectionString);

            var startTask = engine.StartAsync();
            var host = new ModelEngineHost(environment, log, engine, cancellation, startTask);

            var deadline = DateTime.UtcNow + (readyTimeout ?? TimeSpan.FromSeconds(120));
            while (!engine.Context.Ready)
            {
                if (DateTime.UtcNow > deadline)
                {
                    await host.DisposeAsync().ConfigureAwait(false);
                    throw new TimeoutException(
                        "The engine did not report ready in time. Errors: " + host.RecentErrors());
                }

                await Task.Delay(100).ConfigureAwait(false);
            }

            return host;
        }

        public global::Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel? FindActiveModel(
            Guid modelGuid)
        {
            return EngineModels().FirstOrDefault(m => m.Instance.Guid == modelGuid);

            IEnumerable<global::Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel>
                EngineModels()
            {
                return Engine.Context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels
                    .ActiveEntityAnalysisModels.Values;
            }
        }

        public string RecentErrors(int take = 10)
        {
            return string.Join(System.Environment.NewLine,
                Log.Entries.Where(e => e.Level == "ERROR").TakeLast(take).Select(e => e.Message));
        }

        public async ValueTask DisposeAsync()
        {
            cancellation.Cancel();
            try
            {
                await startTask.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
            }
        }
    }
}
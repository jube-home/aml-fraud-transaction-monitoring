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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Test.AdvancedAnalytics.Service;
using Jube.Test.AdvancedAnalytics.Slice;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;
using Jube.Engine.AdvancedAnalytics.Regression.Models;
using Jube.Analytics;

namespace Jube.Test.AdvancedAnalytics.Worker
{
    public abstract class AdvancedAnalyticsWorkerTestBase(DatabaseFixture fx) : AdvancedAnalyticsServiceTestBase(fx)
    {
        protected static readonly AdvancedAnalyticsTrainingSettings FastSettings = new()
        {
            PollInterval = TimeSpan.FromMilliseconds(20),
            HeartbeatInterval = TimeSpan.FromMilliseconds(50),
            StaleRecoveryInterval = TimeSpan.FromMilliseconds(1),
            MaxBackoff = TimeSpan.FromMilliseconds(200),
            ShutdownGrace = TimeSpan.FromSeconds(10),
            MaxConcurrentRuns = 4,
            MaxConcurrentRunsPerTenant = 4
        };

        protected async Task ResetQueueAsync()
        {
            await Db.AdvancedAnalyticsTrainingRun
                .Where(w => w.JobMode == AdvancedAnalyticsJobMode.Batch
                            && (w.StatusId == AdvancedAnalyticsStatus.Pending
                                || w.StatusId == AdvancedAnalyticsStatus.Running
                                || w.StatusId == AdvancedAnalyticsStatus.Cancelling))
                .Set(s => s.StatusId, AdvancedAnalyticsStatus.Cancelled)
                .UpdateAsync();

            await Db.AdvancedAnalyticsTrainingRun
                .Set(s => s.StartedDate, (DateTime?)null)
                .UpdateAsync();

            await Db.AdvancedAnalyticsCache.Where(w => w.CreatedUser == Permitted).DeleteAsync();
        }

        protected AdvancedAnalyticsTrainingWorker NewWorker(AdvancedAnalyticsTrainingSettings? settings = null,
            Func<DateTime>? clock = null, Func<DbContext>? factory = null, TestLog? log = null,
            AdvancedAnalyticsTrainingInputResolver? resolver = null)
        {
            return new AdvancedAnalyticsTrainingWorker(log ?? TestLog.NoOp, settings ?? FastSettings,
                factory ?? (() => Fixture.GetDbContext()), clock, "test-engine", resolver);
        }

        protected async Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitAsync(int instanceId,
            string? modelName = null,
            string? user = null, SliceDataset? data = null, int seed = 1)
        {
            var service = await TrainingServiceAsync(user ?? Permitted);

            return await service.SubmitTrainingAsync(
                TrainingRequest(instanceId, modelName ?? UniqueName("Model"), data, seed));
        }

        protected Task<AdvancedAnalyticsTrainingRun> RunAsync(int runId)
        {
            return Db.AdvancedAnalyticsTrainingRun.FirstAsync(w => w.Id == runId);
        }

        protected async Task<AdvancedAnalyticsTrainingRun> WaitForAsync(int runId,
            Func<AdvancedAnalyticsTrainingRun, bool> condition, int timeoutMilliseconds = 15000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
            AdvancedAnalyticsTrainingRun run;

            do
            {
                run = await RunAsync(runId);

                if (condition(run))
                {
                    return run;
                }

                await Task.Delay(15);
            } while (DateTime.UtcNow < deadline);

            throw new TimeoutException($"Run {runId} stayed {run.StatusId} ({run.Error}).");
        }

        protected Task<int> VersionCountAsync(int runId)
        {
            return Db.AskJoobyAnalyticsTrainedModelVersion.CountAsync(w => w.AdvancedAnalyticsTrainingRunId == runId);
        }

        protected async Task<AdvancedAnalyticsTrainingRun> InsertRunAsync(int tenant, int instanceId,
            AdvancedAnalyticsStatus status, DateTime createdDate, AdvancedAnalyticsJobMode mode =
                AdvancedAnalyticsJobMode.Batch, DateTime? heartbeat = null, DateTime? started = null,
            string? arguments = "{}")
        {
            var run = new AdvancedAnalyticsTrainingRun
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenant,
                AskJoobyAnalyticsInstanceId = instanceId,
                UserName = tenant == TenantA ? Permitted : OtherTenant,
                KindId = AdvancedAnalyticsKind.Classifier,
                JobMode = mode,
                StatusId = status,
                Arguments = arguments,
                Progress = 0,
                CreatedDate = createdDate,
                StartedDate = started,
                HeartbeatDate = heartbeat,
                CreatedUser = "ZzTest"
            };

            run.Id = await Db.InsertWithInt32IdentityAsync(run);

            return run;
        }

        protected sealed class BlockingTrainer
        {
            private readonly ManualResetEventSlim started = new(false);
            public int Calls;
            public bool Started => started.IsSet;

            public Func<double[][], int[], string[], LogisticRegressionOptions, CancellationToken,
                LogisticRegressionResult> Trainer => (_, _, _, _, token) =>
            {
                Interlocked.Increment(ref Calls);
                started.Set();
                token.WaitHandle.WaitOne();
                token.ThrowIfCancellationRequested();
                throw new InvalidOperationException("Unreachable");
            };

            public async Task WaitStartedAsync(int timeoutMilliseconds = 15000)
            {
                var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);

                while (!started.IsSet)
                {
                    if (DateTime.UtcNow > deadline)
                    {
                        throw new TimeoutException("The trainer never started.");
                    }

                    await Task.Delay(10);
                }
            }
        }
    }
}
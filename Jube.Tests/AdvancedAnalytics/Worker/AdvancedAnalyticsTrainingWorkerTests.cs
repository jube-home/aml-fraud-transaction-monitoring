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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Test.AdvancedAnalytics.Slice;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;
using Jube.Engine.AdvancedAnalytics.Regression.Models;
using Jube.Data.Query.GetNextAdvancedAnalyticsTrainingRunQuery;
using Jube.Analytics.Datasets;
using Jube.Analytics;

namespace Jube.Test.AdvancedAnalytics.Worker
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsTrainingWorkerTests(DatabaseFixture fx) : AdvancedAnalyticsWorkerTestBase(fx)
    {
        private async Task<int> InstanceAsync(int? tenant = null)
        {
            return (await NewInstanceAsync(tenant ?? TenantA)).Id;
        }

        private async Task<AdvancedAnalyticsTrainingRun> RunWorkerToCompletionAsync(int runId,
            AdvancedAnalyticsTrainingWorker? worker = null)
        {
            worker ??= NewWorker();
            await worker.RunPassAsync();
            await worker.WaitForIdleAsync();

            return await RunAsync(runId);
        }

        [Fact]
        public async Task TheWorkerTrainsAnInlineRunEndToEndAndRecordsWhatTheSynchronousPathRecordsAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var data = Dataset();
            var submission = await SubmitAsync(instanceId, UniqueName("Model"), data: data);

            var run = await RunWorkerToCompletionAsync(submission.RunId);

            run.StatusId.Should().Be(AdvancedAnalyticsStatus.Succeeded);
            run.Error.Should().BeNull();
            run.Progress.Should().Be(1);
            run.JobMode.Should().Be(AdvancedAnalyticsJobMode.Batch);
            run.StartedDate.Should().NotBeNull();
            run.CompletedDate.Should().NotBeNull();
            run.HeartbeatDate.Should().NotBeNull();
            run.ClaimedBy.Should().Be("test-engine");
            run.ClaimedDate.Should().NotBeNull();

            var metrics = (await new AdvancedAnalyticsTrainingRunMetricRepository(Db, TenantA, Permitted)
                .ListByRunIdAsync(run.Id)).ToList();

            metrics.Should().Contain(m => m.Name == "RocAuc" && m.Split == AdvancedAnalyticsSplit.Test);

            var variables = (await new AdvancedAnalyticsTrainingRunVariableRepository(Db, TenantA, Permitted)
                .ListByRunIdAsync(run.Id)).ToList();

            variables.Select(v => v.Name).Should().BeEquivalentTo(data.Names);

            var trials = (await new AdvancedAnalyticsTrainingRunTrialRepository(Db, TenantA, Permitted)
                .ListByRunIdAsync(run.Id)).ToList();
            trials.Should().ContainSingle();
            trials[0].StatusId.Should().Be(AdvancedAnalyticsStatus.Succeeded);
            trials[0].Selected.Should().Be(1);
            trials[0].Score.Should().BeGreaterThan(0.5);
            JObject.Parse(trials[0].Hyperparameters!)["seed"]!.Value<int>().Should().Be(1);
            JObject.Parse(trials[0].Hyperparameters!)["input"]!["inputs"]!.Type.Should().Be(JTokenType.Null);

            (await VersionCountAsync(run.Id)).Should().Be(1);
        }

        [Fact]
        public async Task TheWorkerAndTheSynchronousPathProduceIdenticalModelsForTheSameSeedAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var data = Dataset(5);
            data.Inputs[3][0] = double.NaN;
            data.Inputs[40][1] = double.NaN;

            var synchronous = await (await TrainingServiceAsync(Permitted))
                .TrainLogisticRegressionAsync(TrainingRequest(instanceId, UniqueName("Sync"), data, 7));

            var submission = await SubmitAsync(instanceId, UniqueName("Worker"), data: data, seed: 7);

            var run = await RunWorkerToCompletionAsync(submission.RunId);
            run.StatusId.Should().Be(AdvancedAnalyticsStatus.Succeeded);

            var versionRepository = new AdvancedAnalyticsTrainedModelVersionRepository(Db, TenantA, Permitted);
            var fromWorker = await versionRepository.GetByRunIdAsync(run.Id);
            var fromSync = await versionRepository.GetByRunIdAsync(synchronous.RunId);

            fromWorker!.ModelJson.Should().Be(fromSync!.ModelJson);
            fromWorker.VariablesJson.Should().Be(fromSync.VariablesJson);
            fromWorker.MetricsJson.Should().Be(fromSync.MetricsJson);
            fromWorker.CalibratorJson.Should().Be(fromSync.CalibratorJson);
        }

        [Fact]
        public async Task ACachePointerRunTrainsIdenticallyToTheInlineRunAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var data = Dataset(9);
            var pointer = await PutTableAsync(data);
            var service = await TrainingServiceAsync(Permitted);

            var request = TrainingRequest(instanceId, UniqueName("Cache"), data, 4);
            request.Inputs = null;
            request.Labels = null;
            request.Input = AdvancedAnalyticsTrainingInput.CachePointer(pointer);
            var cached = await service.SubmitTrainingAsync(request);
            var inline = await SubmitAsync(instanceId, UniqueName("Inline"), data: data, seed: 4);

            var cachedRun = await RunAsync(cached.RunId);
            cachedRun.InputPointer.Should().Be(pointer);
            JObject.Parse(cachedRun.Arguments!)["input"]!["inputs"]!.Type.Should().Be(JTokenType.Null);

            var worker = NewWorker();
            await worker.RunPassAsync();
            await worker.WaitForIdleAsync();

            (await RunAsync(cached.RunId)).StatusId.Should().Be(AdvancedAnalyticsStatus.Succeeded);
            (await RunAsync(inline.RunId)).StatusId.Should().Be(AdvancedAnalyticsStatus.Succeeded);

            var versions = new AdvancedAnalyticsTrainedModelVersionRepository(Db, TenantA, Permitted);

            (await versions.GetByRunIdAsync(cached.RunId))!.ModelJson.Should()
                .Be((await versions.GetByRunIdAsync(inline.RunId))!.ModelJson);
        }

        [Fact]
        public async Task AnExpiredCachePointerFailsTheRunClearlyAndStoresNoModelAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var data = Dataset(9);
            var pointer = await PutTableAsync(data);
            var request = TrainingRequest(instanceId, UniqueName("Expired"), data);
            request.Inputs = null;
            request.Labels = null;
            request.Input = AdvancedAnalyticsTrainingInput.CachePointer(pointer);
            var submission = await (await TrainingServiceAsync(Permitted)).SubmitTrainingAsync(request);

            await Db.AdvancedAnalyticsCache.Where(w => w.Guid == pointer)
                .Set(s => s.ExpiryDate, DateTime.UtcNow.AddMinutes(-1)).UpdateAsync();

            var run = await RunWorkerToCompletionAsync(submission.RunId);

            run.StatusId.Should().Be(AdvancedAnalyticsStatus.Failed);
            run.Error.Should().Contain("expired");
            (await VersionCountAsync(run.Id)).Should().Be(0);
        }

        [Fact]
        public async Task ADatasetInputRunReportsNotSupportedAndStoresNoModelAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();

            var arguments = new AdvancedAnalyticsTrainingArguments
            {
                ModelName = UniqueName("Dataset"),
                VariableNames = ["a"],
                ProcessingTypeIds = [1],
                Input = AdvancedAnalyticsTrainingInput.Dataset("anything")
            };

            var run = await InsertRunAsync(TenantA, instanceId, AdvancedAnalyticsStatus.Pending, DateTime.UtcNow,
                arguments: arguments.ToJson());

            var finished = await RunWorkerToCompletionAsync(run.Id);

            finished.StatusId.Should().Be(AdvancedAnalyticsStatus.Failed);
            finished.Error.Should().Contain("not supported");
            (await VersionCountAsync(run.Id)).Should().Be(0);
        }

        [Fact]
        public async Task ARunWithNoUsableArgumentsFailsAndTheWorkerCarriesOnAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();

            var broken = await InsertRunAsync(TenantA, instanceId, AdvancedAnalyticsStatus.Pending,
                DateTime.UtcNow.AddMinutes(-2), arguments: "[1]");

            var good = await SubmitAsync(instanceId);

            var worker = NewWorker();
            await worker.RunPassAsync();
            await worker.WaitForIdleAsync();

            var brokenRun = await RunAsync(broken.Id);
            brokenRun.StatusId.Should().Be(AdvancedAnalyticsStatus.Failed);
            brokenRun.Error.Should().NotBeNullOrWhiteSpace();
            (await RunAsync(good.RunId)).StatusId.Should().Be(AdvancedAnalyticsStatus.Succeeded);
        }

        [Fact]
        public async Task AFailedRunRecordsTheErrorAndStoresNoModelAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var submission = await SubmitAsync(instanceId);
            var worker = NewWorker();
            worker.Trainer = (_, _, _, _, _) => throw new InvalidOperationException("The classes separate perfectly.");

            var run = await RunWorkerToCompletionAsync(submission.RunId, worker);

            run.StatusId.Should().Be(AdvancedAnalyticsStatus.Failed);
            run.Error.Should().Be("The classes separate perfectly.");
            run.CompletedDate.Should().NotBeNull();
            (await VersionCountAsync(run.Id)).Should().Be(0);

            (await Db.AdvancedAnalyticsTrainedModel.CountAsync(w => w.AdvancedAnalyticsInstanceId == instanceId))
                .Should().Be(0);

            var trial = (await new AdvancedAnalyticsTrainingRunTrialRepository(Db, TenantA, Permitted)
                .ListByRunIdAsync(run.Id)).Single();

            trial.StatusId.Should().Be(AdvancedAnalyticsStatus.Failed);
            trial.Selected.Should().Be(0);
        }

        [Fact]
        public async Task AnUnexpectedTrainerExceptionFailsTheRunGenericallyAndTheNextRunStillTrainsAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var first = await SubmitAsync(instanceId);
            var second = await SubmitAsync(instanceId);
            var calls = 0;
            var worker = NewWorker();

            worker.Trainer = (inputs, labels, names, options, token) =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    throw new NullReferenceException("internal detail");
                }

                return LogisticRegressionTrainer.Train(inputs, labels, names, options, token);
            };

            await worker.RunPassAsync();
            await worker.WaitForIdleAsync();

            var failed = await RunAsync(first.RunId);
            var succeeded = await RunAsync(second.RunId);

            new[] { failed.StatusId, succeeded.StatusId }.Should().BeEquivalentTo(
                [AdvancedAnalyticsStatus.Failed, AdvancedAnalyticsStatus.Succeeded]);

            var failedRun = failed.StatusId == AdvancedAnalyticsStatus.Failed ? failed : succeeded;
            failedRun.Error.Should().NotContain("internal detail").And.NotBeNullOrWhiteSpace();
            (await VersionCountAsync(failedRun.Id)).Should().Be(0);
        }

        [Fact]
        public async Task CancellingAPendingRunCancelsItAtOnceAndTheWorkerNeverPicksItUpAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var submission = await SubmitAsync(instanceId);
            var service = await TrainingServiceAsync(Permitted);

            var status = await service.CancelRunAsync(submission.RunId);

            status.Status.Should().Be(AdvancedAnalyticsStatus.Cancelled);
            status.Finished.Should().BeTrue();
            var worker = NewWorker();
            (await worker.RunPassAsync()).Should().Be(0);
            var run = await RunAsync(submission.RunId);
            run.StatusId.Should().Be(AdvancedAnalyticsStatus.Cancelled);
            run.StartedDate.Should().BeNull();
            (await VersionCountAsync(run.Id)).Should().Be(0);

            var again = await service.CancelRunAsync(submission.RunId);
            again.Status.Should().Be(AdvancedAnalyticsStatus.Cancelled);
        }

        [Fact]
        public async Task StoppingByGuidBehavesLikeCancellingByIdAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var submission = await SubmitAsync(instanceId);

            var status = await (await TrainingServiceAsync(Permitted)).StopRunAsync(submission.RunGuid);

            status.Status.Should().Be(AdvancedAnalyticsStatus.Cancelled);
        }

        [Fact]
        public async Task CancellingARunningRunStopsTheTrainerAndStoresNoModelAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var submission = await SubmitAsync(instanceId);
            var blocking = new BlockingTrainer();
            var worker = NewWorker();
            worker.Trainer = blocking.Trainer;

            await worker.RunPassAsync();
            await blocking.WaitStartedAsync();

            var service = await TrainingServiceAsync(Permitted);
            var running = await service.GetRunAsync(submission.RunId);
            running.Status.Should().Be(AdvancedAnalyticsStatus.Running);
            running.Finished.Should().BeFalse();
            running.ElapsedSeconds.Should().BeGreaterThanOrEqualTo(0);

            var requested = await service.CancelRunAsync(submission.RunId);
            requested.Status.Should().Be(AdvancedAnalyticsStatus.Cancelling);

            await worker.WaitForIdleAsync();

            var run = await RunAsync(submission.RunId);
            run.StatusId.Should().Be(AdvancedAnalyticsStatus.Cancelled);
            run.Error.Should().NotBeNullOrWhiteSpace();
            (await VersionCountAsync(run.Id)).Should().Be(0);

            (await Db.AdvancedAnalyticsTrainedModel.CountAsync(w => w.AdvancedAnalyticsInstanceId == instanceId))
                .Should().Be(0);
        }

        [Fact]
        public async Task ARunOverTheMaximumDurationIsCancelledAndFailedWithAClearReasonAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var submission = await SubmitAsync(instanceId);
            var blocking = new BlockingTrainer();

            var worker = NewWorker(new AdvancedAnalyticsTrainingSettings
            {
                PollInterval = FastSettings.PollInterval,
                HeartbeatInterval = FastSettings.HeartbeatInterval,
                MaxRunDuration = TimeSpan.FromMilliseconds(300),
                MaxConcurrentRunsPerTenant = 4
            });

            worker.Trainer = blocking.Trainer;

            await worker.RunPassAsync();
            await worker.WaitForIdleAsync();

            var run = await RunAsync(submission.RunId);
            run.StatusId.Should().Be(AdvancedAnalyticsStatus.Failed);
            run.Error.Should().Be(AdvancedAnalyticsTrainingWorker.TimeoutError);
            (await VersionCountAsync(run.Id)).Should().Be(0);
        }

        [Fact]
        public async Task ARunningRunKeepsItsHeartbeatFreshAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var submission = await SubmitAsync(instanceId);
            var blocking = new BlockingTrainer();
            var worker = NewWorker();
            worker.Trainer = blocking.Trainer;

            await worker.RunPassAsync();
            await blocking.WaitStartedAsync();
            var claimed = await RunAsync(submission.RunId);
            var first = claimed.HeartbeatDate!.Value;

            var advanced = await WaitForAsync(submission.RunId, r => r.HeartbeatDate > first.AddMilliseconds(100));
            advanced.StatusId.Should().Be(AdvancedAnalyticsStatus.Running);

            await (await TrainingServiceAsync(Permitted)).CancelRunAsync(submission.RunId);
            await worker.WaitForIdleAsync();
        }

        [Fact]
        public async Task ShuttingDownCancelsTheRunningRunAndStoresNoModelAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var submission = await SubmitAsync(instanceId);
            var blocking = new BlockingTrainer();
            var worker = NewWorker();
            worker.Trainer = blocking.Trainer;
            using var shutdown = new CancellationTokenSource();

            var loop = worker.StartAsync(shutdown.Token);

            try
            {
                await blocking.WaitStartedAsync();
            }
            finally
            {
                await shutdown.CancelAsync();
                await loop.WaitAsync(TimeSpan.FromSeconds(20));
            }

            var run = await RunAsync(submission.RunId);
            run.StatusId.Should().Be(AdvancedAnalyticsStatus.Cancelled);
            run.Error.Should().Be(AdvancedAnalyticsTrainingWorker.ShutdownError);
            (await VersionCountAsync(run.Id)).Should().Be(0);
            worker.RunningCount.Should().Be(0);
        }

        [Fact]
        public async Task ARunRecoveredAsStaleWhileItsWorkerIsAliveNeverStoresAModelAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var submission = await SubmitAsync(instanceId);
            var blocking = new BlockingTrainer();
            var worker = NewWorker();
            worker.Trainer = blocking.Trainer;

            await worker.RunPassAsync();
            await blocking.WaitStartedAsync();

            var recovered = await AdvancedAnalyticsTrainingRunRepository
                .RecoverStaleAsync(Db, DateTime.UtcNow.AddMinutes(1), DateTime.UtcNow, "recovered by test");

            recovered.Should().Contain(submission.RunId);

            await worker.WaitForIdleAsync();

            var run = await RunAsync(submission.RunId);
            run.StatusId.Should().Be(AdvancedAnalyticsStatus.Failed);
            run.Error.Should().Be("recovered by test");
            (await VersionCountAsync(run.Id)).Should().Be(0);
        }

        [Fact]
        public async Task StaleRunningRunsAreRecoveredAtStartAndOnlyBatchRunsWithAnOldHeartbeatAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var now = DateTime.UtcNow;

            var stale = await InsertRunAsync(TenantA, instanceId, AdvancedAnalyticsStatus.Running, now.AddHours(-2),
                heartbeat: now.AddMinutes(-30), started: now.AddHours(-1));
            var staleCancelling = await InsertRunAsync(TenantA, instanceId, AdvancedAnalyticsStatus.Cancelling,
                now.AddHours(-2), heartbeat: now.AddMinutes(-30), started: now.AddHours(-1));
            var fresh = await InsertRunAsync(TenantA, instanceId, AdvancedAnalyticsStatus.Running, now.AddHours(-2),
                heartbeat: now.AddSeconds(-5), started: now.AddHours(-1));

            var online = await InsertRunAsync(TenantA, instanceId, AdvancedAnalyticsStatus.Running, now.AddHours(-2),
                AdvancedAnalyticsJobMode.Online, started: now.AddHours(-1));

            var pending = await InsertRunAsync(TenantA, instanceId, AdvancedAnalyticsStatus.Pending,
                now.AddHours(-2));

            var worker = NewWorker(new AdvancedAnalyticsTrainingSettings
            {
                StaleThreshold = TimeSpan.FromMinutes(2),
                PollInterval = TimeSpan.FromMilliseconds(20),
                MaxConcurrentRuns = 1
            });

            await worker.RecoverStaleAsync(true);

            var recovered = await RunAsync(stale.Id);
            recovered.StatusId.Should().Be(AdvancedAnalyticsStatus.Failed);
            recovered.Error.Should().Be(AdvancedAnalyticsTrainingWorker.EngineStoppedError);
            recovered.CompletedDate.Should().NotBeNull();
            (await RunAsync(staleCancelling.Id)).StatusId.Should().Be(AdvancedAnalyticsStatus.Cancelled);
            (await RunAsync(fresh.Id)).StatusId.Should().Be(AdvancedAnalyticsStatus.Running);
            (await RunAsync(online.Id)).StatusId.Should().Be(AdvancedAnalyticsStatus.Running);
            (await RunAsync(pending.Id)).StatusId.Should().Be(AdvancedAnalyticsStatus.Pending);
        }

        [Fact]
        public async Task StaleRecoveryUsesTheInjectedClockAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var now = DateTime.UtcNow;

            var run = await InsertRunAsync(TenantA, instanceId, AdvancedAnalyticsStatus.Running, now,
                heartbeat: now, started: now);

            var worker = NewWorker(new AdvancedAnalyticsTrainingSettings
            {
                StaleThreshold = TimeSpan.FromMinutes(2),
                MaxConcurrentRuns = 1
            }, () => now.AddMinutes(1));

            await worker.RecoverStaleAsync(true);
            (await RunAsync(run.Id)).StatusId.Should().Be(AdvancedAnalyticsStatus.Running);

            var later = NewWorker(new AdvancedAnalyticsTrainingSettings
            {
                StaleThreshold = TimeSpan.FromMinutes(2),
                MaxConcurrentRuns = 1
            }, () => now.AddMinutes(5));

            await later.RecoverStaleAsync(true);
            (await RunAsync(run.Id)).StatusId.Should().Be(AdvancedAnalyticsStatus.Failed);
        }

        [Fact]
        public async Task TheLoopSurvivesDatabaseErrorsBacksOffAndThenTrainsTheRunAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var submission = await SubmitAsync(instanceId);
            var failures = 4;
            var log = new TestLog();

            var worker = NewWorker(factory: () =>
            {
                if (Interlocked.Decrement(ref failures) >= 0)
                {
                    throw new InvalidOperationException("The database is unavailable.");
                }

                return Fixture.GetDbContext();
            }, log: log);

            using var shutdown = new CancellationTokenSource();

            var loop = worker.StartAsync(shutdown.Token);
            AdvancedAnalyticsTrainingRun run;

            try
            {
                run = await WaitForAsync(submission.RunId, r => r.StatusId == AdvancedAnalyticsStatus.Succeeded);
            }
            finally
            {
                await shutdown.CancelAsync();
                await loop.WaitAsync(TimeSpan.FromSeconds(20));
            }

            run.StatusId.Should().Be(AdvancedAnalyticsStatus.Succeeded);
            log.Entries.Count(e => e.Level == "ERROR").Should().BeGreaterThanOrEqualTo(3);
        }

        [Fact]
        public void TheBackOffGrowsAfterErrorsAndIsCapped()
        {
            var worker = NewWorker(new AdvancedAnalyticsTrainingSettings
            {
                PollInterval = TimeSpan.FromSeconds(5),
                MaxBackoff = TimeSpan.FromSeconds(60)
            });

            worker.NextDelay(0).Should().Be(TimeSpan.FromSeconds(5));
            worker.NextDelay(1).Should().BeGreaterThan(TimeSpan.FromSeconds(5));
            worker.NextDelay(2).Should().BeGreaterThan(worker.NextDelay(1));
            worker.NextDelay(10).Should().Be(TimeSpan.FromSeconds(60));
            worker.NextDelay(1000).Should().Be(TimeSpan.FromSeconds(60));
        }

        [Fact]
        public async Task TheGlobalConcurrencyCapHoldsSecondRunsBackUntilASlotFreesAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var b = await InstanceAsync(TenantB);
            var first = await SubmitAsync(instanceId);
            var second = await SubmitAsync(b, user: OtherTenant);
            var blocking = new BlockingTrainer();

            var worker = NewWorker(new AdvancedAnalyticsTrainingSettings
            {
                PollInterval = FastSettings.PollInterval,
                HeartbeatInterval = FastSettings.HeartbeatInterval,
                MaxConcurrentRuns = 1,
                MaxConcurrentRunsPerTenant = 4
            });

            worker.Trainer = blocking.Trainer;

            (await worker.RunPassAsync()).Should().Be(1);
            await blocking.WaitStartedAsync();
            (await worker.RunPassAsync()).Should().Be(0);
            worker.RunningCount.Should().Be(1);
            (await RunAsync(second.RunId)).StatusId.Should().Be(AdvancedAnalyticsStatus.Pending);

            await (await TrainingServiceAsync(Permitted)).CancelRunAsync(first.RunId);
            await worker.WaitForIdleAsync();
            worker.Trainer = null;
            (await worker.RunPassAsync()).Should().Be(1);
            await worker.WaitForIdleAsync();
            (await RunAsync(second.RunId)).StatusId.Should().Be(AdvancedAnalyticsStatus.Succeeded);
        }

        [Fact]
        public async Task OneTenantCannotExceedItsConcurrencyCapWhileAnotherTenantStillGetsServedAsync()
        {
            await ResetQueueAsync();
            var a = await InstanceAsync();
            var b = await InstanceAsync(TenantB);
            var now = DateTime.UtcNow;
            var a1 = await InsertRunAsync(TenantA, a, AdvancedAnalyticsStatus.Pending, now.AddSeconds(-30));
            var a2 = await InsertRunAsync(TenantA, a, AdvancedAnalyticsStatus.Pending, now.AddSeconds(-20));
            var b1 = await InsertRunAsync(TenantB, b, AdvancedAnalyticsStatus.Pending, now.AddSeconds(-10));

            var first = await ClaimAsync(now, 1);
            first!.Id.Should().Be(a1.Id);
            var second = await ClaimAsync(now.AddSeconds(1), 1);
            second!.Id.Should().Be(b1.Id);
            (await ClaimAsync(now.AddSeconds(2), 1)).Should().BeNull();

            await Db.AdvancedAnalyticsTrainingRun.Where(w => w.Id == a1.Id)
                .Set(s => s.StatusId, AdvancedAnalyticsStatus.Succeeded).UpdateAsync();

            var third = await ClaimAsync(now.AddSeconds(3), 1);
            third!.Id.Should().Be(a2.Id);
        }

        [Fact]
        public async Task PickupIsOldestFirstWithinATenantAndRoundRobinAcrossTenantsAsync()
        {
            await ResetQueueAsync();
            var a = await InstanceAsync();
            var b = await InstanceAsync(TenantB);
            var now = DateTime.UtcNow;
            var a1 = await InsertRunAsync(TenantA, a, AdvancedAnalyticsStatus.Pending, now.AddSeconds(-50));
            var a2 = await InsertRunAsync(TenantA, a, AdvancedAnalyticsStatus.Pending, now.AddSeconds(-40));
            var a3 = await InsertRunAsync(TenantA, a, AdvancedAnalyticsStatus.Pending, now.AddSeconds(-30));
            var b1 = await InsertRunAsync(TenantB, b, AdvancedAnalyticsStatus.Pending, now.AddSeconds(-20));
            var b2 = await InsertRunAsync(TenantB, b, AdvancedAnalyticsStatus.Pending, now.AddSeconds(-10));

            var order = new List<int>();

            for (var i = 0; i < 6; i++)
            {
                var claimed = await ClaimAsync(now.AddSeconds(i), 100);

                if (claimed == null)
                {
                    break;
                }

                order.Add(claimed.Id);
            }

            order.Should().Equal(a1.Id, b1.Id, a2.Id, b2.Id, a3.Id);
        }

        [Fact]
        public async Task ATenantWithAHugeBacklogCannotStarveAnotherTenantAsync()
        {
            await ResetQueueAsync();
            var a = await InstanceAsync();
            var b = await InstanceAsync(TenantB);
            var now = DateTime.UtcNow;

            for (var i = 0; i < 20; i++)
            {
                await InsertRunAsync(TenantA, a, AdvancedAnalyticsStatus.Pending, now.AddMinutes(-60 + i));
            }

            var late = await InsertRunAsync(TenantB, b, AdvancedAnalyticsStatus.Pending, now);

            var first = await ClaimAsync(now, 100);
            var second = await ClaimAsync(now.AddSeconds(1), 100);

            first!.TenantRegistryId.Should().Be(TenantA);
            second!.Id.Should().Be(late.Id);
        }

        [Fact]
        public async Task ManyConcurrentClaimersClaimEveryRunExactlyOnceAsync()
        {
            await ResetQueueAsync();
            var a = await InstanceAsync();
            var b = await InstanceAsync(TenantB);
            var now = DateTime.UtcNow;
            var expected = new HashSet<int>();

            for (var i = 0; i < 40; i++)
            {
                var run = await InsertRunAsync(i % 2 == 0 ? TenantA : TenantB, i % 2 == 0 ? a : b,
                    AdvancedAnalyticsStatus.Pending, now.AddSeconds(-100 + i));
                expected.Add(run.Id);
            }

            var claimed = new List<int>();
            var gate = new object();

            var claimers = Enumerable.Range(0, 12).Select(n => Task.Run(async () =>
            {
                await using var context = Fixture.GetDbContext();

                while (true)
                {
                    var dto = await new GetNextAdvancedAnalyticsTrainingRunQuery(context, $"claimer-{n}", 1000, now)
                        .ExecuteAsync();

                    if (dto == null)
                    {
                        return;
                    }

                    lock (gate)
                    {
                        claimed.Add(dto.Id);
                    }
                }
            })).ToArray();

            await Task.WhenAll(claimers);

            claimed.Should().HaveCount(40);
            claimed.Should().OnlyHaveUniqueItems();
            claimed.Should().BeEquivalentTo(expected);

            (await Db.AdvancedAnalyticsTrainingRun.CountAsync(w => expected.Contains(w.Id)
                                                                   && w.StatusId == AdvancedAnalyticsStatus.Running))
                .Should().Be(40);
        }

        [Fact]
        public async Task ConcurrentClaimersNeverExceedTheTenantCapAsync()
        {
            await ResetQueueAsync();
            var a = await InstanceAsync();
            var now = DateTime.UtcNow;

            for (var i = 0; i < 10; i++)
            {
                await InsertRunAsync(TenantA, a, AdvancedAnalyticsStatus.Pending, now.AddSeconds(-100 + i));
            }

            var claimed = 0;

            await Task.WhenAll(Enumerable.Range(0, 8).Select(n => Task.Run(async () =>
            {
                await using var context = Fixture.GetDbContext();
                var dto = await new GetNextAdvancedAnalyticsTrainingRunQuery(context, $"c{n}", 2, now).ExecuteAsync();

                if (dto != null)
                {
                    Interlocked.Increment(ref claimed);
                }
            })));

            claimed.Should().Be(2);
        }

        [Fact]
        public async Task TheClaimStampsTheRunAsRunningWithAStartAHeartbeatAndTheClaimingNodeAsync()
        {
            await ResetQueueAsync();
            var a = await InstanceAsync();
            var now = DateTime.UtcNow;
            var pending = await InsertRunAsync(TenantA, a, AdvancedAnalyticsStatus.Pending, now.AddSeconds(-5));

            var dto = await ClaimAsync(now, 1);

            dto!.Id.Should().Be(pending.Id);
            dto.TenantRegistryId.Should().Be(TenantA);
            var run = await RunAsync(pending.Id);
            run.StatusId.Should().Be(AdvancedAnalyticsStatus.Running);
            run.StartedDate.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
            run.HeartbeatDate.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
            run.ClaimedDate.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
            run.ClaimedBy.Should().Be("claimer");
        }

        [Fact]
        public async Task OnlineRunsAndDeletedRunsAreNeverClaimedAsync()
        {
            await ResetQueueAsync();
            var a = await InstanceAsync();
            var now = DateTime.UtcNow;

            await InsertRunAsync(TenantA, a, AdvancedAnalyticsStatus.Pending, now.AddSeconds(-5),
                AdvancedAnalyticsJobMode.Online);

            var deleted = await InsertRunAsync(TenantA, a, AdvancedAnalyticsStatus.Pending, now.AddSeconds(-4));

            await Db.AdvancedAnalyticsTrainingRun.Where(w => w.Id == deleted.Id)
                .Set(s => s.Deleted, (byte)1).UpdateAsync();

            (await ClaimAsync(now, 5)).Should().BeNull();
        }

        [Fact]
        public async Task TwoWorkersOnTheSameQueueNeverRunTheSameRunTwiceAsync()
        {
            await ResetQueueAsync();
            var a = await InstanceAsync();
            var submissions = new List<AdvancedAnalyticsTrainingSubmissionDto>();

            for (var i = 0; i < 6; i++)
            {
                submissions.Add(await SubmitAsync(a));
            }

            var calls = 0;

            Func<double[][], int[], string[], LogisticRegressionOptions, CancellationToken, LogisticRegressionResult>
                counting = (inputs, labels, names, options, token) =>
                {
                    Interlocked.Increment(ref calls);

                    return LogisticRegressionTrainer.Train(inputs, labels, names, options, token);
                };

            var one = NewWorker();
            var two = NewWorker();
            one.Trainer = counting;
            two.Trainer = counting;

            await Task.WhenAll(one.RunPassAsync(), two.RunPassAsync());
            await Task.WhenAll(one.WaitForIdleAsync(), two.WaitForIdleAsync());
            await one.RunPassAsync();
            await two.RunPassAsync();
            await Task.WhenAll(one.WaitForIdleAsync(), two.WaitForIdleAsync());
            await one.RunPassAsync();
            await two.RunPassAsync();
            await Task.WhenAll(one.WaitForIdleAsync(), two.WaitForIdleAsync());

            foreach (var submission in submissions)
            {
                (await RunAsync(submission.RunId)).StatusId.Should().Be(AdvancedAnalyticsStatus.Succeeded);
            }

            calls.Should().Be(6);
        }

        [Fact]
        public async Task TwoIndependentNodesRunningTheLoopBothMakeProgressAndNeverShareARunAsync()
        {
            await ResetQueueAsync();
            var a = await InstanceAsync();
            var submissions = new List<AdvancedAnalyticsTrainingSubmissionDto>();

            for (var i = 0; i < 8; i++)
            {
                submissions.Add(await SubmitAsync(a, seed: i + 1));
            }

            var running = 0;
            var peak = 0;

            Func<double[][], int[], string[], LogisticRegressionOptions, CancellationToken, LogisticRegressionResult>
                slow = (inputs, labels, names, options, token) =>
                {
                    var now = Interlocked.Increment(ref running);
                    int seen;

                    while ((seen = Volatile.Read(ref peak)) < now
                           && Interlocked.CompareExchange(ref peak, now, seen) != seen)
                    {
                    }

#pragma warning disable VSTHRD103, AsyncFixer02
                    Thread.Sleep(150);
#pragma warning restore VSTHRD103, AsyncFixer02
                    var result = LogisticRegressionTrainer.Train(inputs, labels, names, options, token);
                    Interlocked.Decrement(ref running);

                    return result;
                };

            var settings = new AdvancedAnalyticsTrainingSettings
            {
                PollInterval = TimeSpan.FromMilliseconds(20),
                HeartbeatInterval = TimeSpan.FromMilliseconds(50),
                MaxConcurrentRuns = 1,
                MaxConcurrentRunsPerTenant = 4
            };

            var one = new AdvancedAnalyticsTrainingWorker(TestLog.NoOp, settings, () => Fixture.GetDbContext(),
                null, "node-one") { Trainer = slow };
            var two = new AdvancedAnalyticsTrainingWorker(TestLog.NoOp, settings, () => Fixture.GetDbContext(),
                null, "node-two") { Trainer = slow };

            using var shutdown = new CancellationTokenSource();

            var loops = new[] { one.StartAsync(shutdown.Token), two.StartAsync(shutdown.Token) };

            try
            {
                foreach (var submission in submissions)
                {
                    await WaitForAsync(submission.RunId, r => r.StatusId == AdvancedAnalyticsStatus.Succeeded, 60000);
                }
            }
            finally
            {
                await shutdown.CancelAsync();
                await Task.WhenAll(loops).WaitAsync(TimeSpan.FromSeconds(30));
            }

            var claimedBy = new List<string?>();

            foreach (var submission in submissions)
            {
                claimedBy.Add((await RunAsync(submission.RunId)).ClaimedBy);
            }

            claimedBy.Should().Contain("node-one");
            claimedBy.Should().Contain("node-two");
            claimedBy.Should().OnlyContain(c => c == "node-one" || c == "node-two");
            peak.Should().BeGreaterThanOrEqualTo(2);

            foreach (var submission in submissions)
            {
                (await VersionCountAsync(submission.RunId)).Should().Be(1);
            }
        }

        private async Task<GetNextAdvancedAnalyticsTrainingRunQueryDto?> ClaimAsync(DateTime now, int cap)
        {
            await using var context = Fixture.GetDbContext();

            return await new GetNextAdvancedAnalyticsTrainingRunQuery(context, "claimer", cap, now).ExecuteAsync();
        }

        private async Task<Guid> PutTableAsync(SliceDataset data)
        {
            var columns = data.Names.Length + 1;
            var flat = new double[data.Inputs.Length * columns];

            for (var r = 0; r < data.Inputs.Length; r++)
            {
                Array.Copy(data.Inputs[r], 0, flat, r * columns, columns - 1);
                flat[r * columns + columns - 1] = data.Labels[r];
            }

            var entry = await new AdvancedAnalyticsCacheRepository(Db, TenantA, Permitted,
                new AdvancedAnalyticsCacheLimits()).PutAsync(AdvancedAnalyticsPacking.Pack(flat),
                data.Inputs.Length, columns);

            return entry.Guid;
        }
    }
}
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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Repository;
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Analytics.Datasets;
using Jube.Analytics;

namespace Jube.Test.AdvancedAnalytics.Worker
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsTrainingSubmitTests(DatabaseFixture fx) : AdvancedAnalyticsWorkerTestBase(fx)
    {
        private async Task<int> InstanceAsync(int? tenant = null)
        {
            return (await NewInstanceAsync(tenant ?? TenantA)).Id;
        }

        [Fact]
        public async Task SubmitReturnsAtOnceWithAPendingBatchRunAndTrainsNothingAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var service = await TrainingServiceAsync(Permitted);
            var trained = 0;

            service.Trainer = (_, _, _, _, _) =>
            {
                trained++;
                throw new InvalidOperationException("The trainer must not run during submit.");
            };

            var data = Dataset();

            var submission =
                await service.SubmitTrainingAsync(TrainingRequest(instanceId, UniqueName("Model"), data, 3));

            submission.Status.Should().Be(AdvancedAnalyticsStatus.Pending);
            submission.HowToPoll.Should().NotBeNullOrWhiteSpace();
            trained.Should().Be(0);
            var run = await RunAsync(submission.RunId);
            run.StatusId.Should().Be(AdvancedAnalyticsStatus.Pending);
            run.JobMode.Should().Be(AdvancedAnalyticsJobMode.Batch);
            run.KindId.Should().Be(AdvancedAnalyticsKind.Classifier);
            run.Seed.Should().Be(3);
            run.StartedDate.Should().BeNull();
            run.UserName.Should().Be(Permitted);
            run.TenantRegistryId.Should().Be(TenantA);
            var arguments = JObject.Parse(run.Arguments!);
            arguments["variableNames"]!.Select(v => v.Value<string>()).Should().Equal(data.Names);
            arguments["processingTypeIds"]!.Count().Should().Be(data.Names.Length);
            arguments["input"]!["kind"]!.Value<string>().Should().Be("Inline");
            arguments["input"]!["inputs"]!.Count().Should().Be(data.Inputs.Length);
            (await VersionCountAsync(run.Id)).Should().Be(0);

            (await Db.AdvancedAnalyticsTrainingRunStep.CountAsync(w => w.AdvancedAnalyticsTrainingRunId == run.Id))
                .Should().Be(0);
        }

        [Fact]
        public async Task SubmitAppliesTheExistingValidationAndCreatesNothingWhenItFailsAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var service = await TrainingServiceAsync(Permitted);
            var request = TrainingRequest(instanceId, UniqueName("Model"));
            request.VariableNames = [];

            var act = () => service.SubmitTrainingAsync(request);

            await act.Should().ThrowAsync<DtoValidationException>();

            (await Db.AdvancedAnalyticsTrainingRun.CountAsync(w => w.AdvancedAnalyticsInstanceId == instanceId))
                .Should().Be(0);
        }

        [Fact]
        public async Task SubmitRefusesInlineRowsOverTheConfiguredCapAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var service = await TrainingServiceAsync(Permitted);
            service.Settings = new AdvancedAnalyticsTrainingSettings { InlineMaxRows = 100 };

            var act = () => service.SubmitTrainingAsync(TrainingRequest(instanceId, UniqueName("Model")));

            var thrown = await act.Should().ThrowAsync<DtoValidationException>();
            thrown.Which.Result.Errors.Should().Contain(e => e.ErrorCode == "InlineTooManyRows");

            (await Db.AdvancedAnalyticsTrainingRun.CountAsync(w => w.AdvancedAnalyticsInstanceId == instanceId))
                .Should().Be(0);
        }

        [Fact]
        public async Task SubmitRefusesInlineDataOverTheConfiguredByteCapAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var service = await TrainingServiceAsync(Permitted);
            service.Settings = new AdvancedAnalyticsTrainingSettings { InlineMaxBytes = 1000 };

            var act = () => service.SubmitTrainingAsync(TrainingRequest(instanceId, UniqueName("Model")));

            var thrown = await act.Should().ThrowAsync<DtoValidationException>();
            thrown.Which.Result.Errors.Should().Contain(e => e.ErrorCode == "InlineTooLarge");
        }

        [Fact]
        public async Task SubmitRefusesACachePointerThatDoesNotExistOrHasTheWrongShapeAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var service = await TrainingServiceAsync(Permitted);
            var request = TrainingRequest(instanceId, UniqueName("Model"));
            request.Inputs = null;
            request.Labels = null;
            request.Input = AdvancedAnalyticsTrainingInput.CachePointer(Guid.NewGuid());

            var missing = await FluentActions.Awaiting(() => service.SubmitTrainingAsync(request))
                .Should().ThrowAsync<DtoValidationException>();

            missing.Which.Result.Errors.Should().Contain(e => e.ErrorCode == "CachePointerNotFound");

            var entry = await new AdvancedAnalyticsCacheRepository(Db, TenantA, Permitted,
                new AdvancedAnalyticsCacheLimits()).PutAsync(AdvancedAnalyticsPacking.Pack([1, 2, 3, 4]), 2, 2);

            request.Input = AdvancedAnalyticsTrainingInput.CachePointer(entry.Guid);

            var wrong = await FluentActions.Awaiting(() => service.SubmitTrainingAsync(request))
                .Should().ThrowAsync<DtoValidationException>();

            wrong.Which.Result.Errors.Should().Contain(e => e.ErrorCode == "CacheShapeMismatch");
        }

        [Fact]
        public async Task SubmitRefusesACachePointerOfAnotherTenantAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var data = Dataset();

            var entry = await new AdvancedAnalyticsCacheRepository(Db, TenantB, OtherTenant,
                new AdvancedAnalyticsCacheLimits()).PutAsync(AdvancedAnalyticsPacking.Pack([1, 2, 3]), 1, 3);

            var request = TrainingRequest(instanceId, UniqueName("Model"), data);
            request.Inputs = null;
            request.Labels = null;
            request.VariableNames = ["a", "b"];
            request.ProcessingTypeIds = [1, 1];
            request.Input = AdvancedAnalyticsTrainingInput.CachePointer(entry.Guid);
            var service = await TrainingServiceAsync(Permitted);

            var thrown = await FluentActions.Awaiting(() => service.SubmitTrainingAsync(request))
                .Should().ThrowAsync<DtoValidationException>();

            thrown.Which.Result.Errors.Should().Contain(e => e.ErrorCode == "CachePointerNotFound");
            await Db.AdvancedAnalyticsCache.Where(w => w.Guid == entry.Guid).DeleteAsync();
        }

        [Fact]
        public async Task SubmitReportsNotSupportedForDatasetInputAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var service = await TrainingServiceAsync(Permitted);
            var request = TrainingRequest(instanceId, UniqueName("Model"));
            request.Inputs = null;
            request.Labels = null;
            request.Input = AdvancedAnalyticsTrainingInput.Dataset("d1");

            var act = () => service.SubmitTrainingAsync(request);

            await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*not supported*");

            (await Db.AdvancedAnalyticsTrainingRun.CountAsync(w => w.AdvancedAnalyticsInstanceId == instanceId))
                .Should().Be(0);
        }

        [Fact]
        public async Task EveryTrainingOperationRefusesACallerWithoutThePermissionAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var submission = await SubmitAsync(instanceId);
            var service = await TrainingServiceAsync(Unpermitted);

            await FluentActions.Awaiting(() => service.SubmitTrainingAsync(TrainingRequest(instanceId, "x")))
                .Should().ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => service.CancelRunAsync(submission.RunId))
                .Should().ThrowAsync<ForbiddenException>();
            await FluentActions.Awaiting(() => service.StopRunAsync(submission.RunGuid))
                .Should().ThrowAsync<ForbiddenException>();
            await FluentActions.Awaiting(() => service.GetRunAsync(submission.RunId))
                .Should().ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => service.ListRunsAsync())
                .Should().ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => service.GetRunResultAsync(submission.RunId))
                .Should().ThrowAsync<ForbiddenException>();

            (await RunAsync(submission.RunId)).StatusId.Should().Be(AdvancedAnalyticsStatus.Pending);
        }

        [Fact]
        public async Task SubmittingAndCancellingNeedTrainingAndReadingNeedsTheAnalyticsPermissionAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();

            var readOnly = await TrainingServiceAsync(
                await UserWithSpecsAsync(AdvancedAnalyticsPermissionSpecifications.Analytics));
            var trainOnly = await TrainingServiceAsync(
                await UserWithSpecsAsync(AdvancedAnalyticsPermissionSpecifications.Training));

            await FluentActions.Awaiting(() => readOnly.SubmitTrainingAsync(TrainingRequest(instanceId, "x")))
                .Should().ThrowAsync<ForbiddenException>();

            var submission = await trainOnly.SubmitTrainingAsync(TrainingRequest(instanceId, UniqueName("Model")));

            (await readOnly.GetRunAsync(submission.RunId)).Status.Should().Be(AdvancedAnalyticsStatus.Pending);
            (await readOnly.ListRunsAsync(instanceId)).Should().ContainSingle();

            await FluentActions.Awaiting(() => readOnly.CancelRunAsync(submission.RunId))
                .Should().ThrowAsync<ForbiddenException>();
            await FluentActions.Awaiting(() => trainOnly.GetRunAsync(submission.RunId))
                .Should().ThrowAsync<ForbiddenException>();

            (await trainOnly.CancelRunAsync(submission.RunId)).Status.Should().Be(AdvancedAnalyticsStatus.Cancelled);
        }

        [Fact]
        public async Task ARunOfAnotherTenantCannotBeSeenCancelledOrListedAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var submission = await SubmitAsync(instanceId);
            var other = await TrainingServiceAsync(OtherTenant);

            await FluentActions.Awaiting(() => other.GetRunAsync(submission.RunId))
                .Should().ThrowAsync<NotFoundException>();
            await FluentActions.Awaiting(() => other.CancelRunAsync(submission.RunId))
                .Should().ThrowAsync<NotFoundException>();
            await FluentActions.Awaiting(() => other.StopRunAsync(submission.RunGuid))
                .Should().ThrowAsync<NotFoundException>();
            await FluentActions.Awaiting(() => other.GetRunResultAsync(submission.RunId))
                .Should().ThrowAsync<NotFoundException>();

            (await other.ListRunsAsync(instanceId)).Should().BeEmpty();
            (await RunAsync(submission.RunId)).StatusId.Should().Be(AdvancedAnalyticsStatus.Pending);
        }

        [Fact]
        public async Task SubmittingAgainstAnotherTenantsInstanceIsRefusedAsync()
        {
            await ResetQueueAsync();
            var foreign = await InstanceAsync(TenantB);
            var service = await TrainingServiceAsync(Permitted);

            await FluentActions.Awaiting(() => service.SubmitTrainingAsync(TrainingRequest(foreign, "x")))
                .Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task TheSynchronousPathRefusesMoreRowsThanTheOnlineCapAndCreatesNoRunAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var service = await TrainingServiceAsync(Permitted);
            service.Settings = new AdvancedAnalyticsTrainingSettings { OnlineMaxRows = 100 };

            var thrown = await FluentActions.Awaiting(() =>
                    service.TrainLogisticRegressionAsync(TrainingRequest(instanceId, UniqueName("Model"))))
                .Should().ThrowAsync<DtoValidationException>();

            thrown.Which.Result.Errors.Should().Contain(e => e.ErrorCode == "OnlineTooManyRows");

            (await Db.AdvancedAnalyticsTrainingRun.CountAsync(w => w.AdvancedAnalyticsInstanceId == instanceId))
                .Should().Be(0);
        }

        [Fact]
        public async Task TheSynchronousPathStillTrainsUnderTheCapAndRefusesCacheInputAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var service = await TrainingServiceAsync(Permitted);

            var outcome = await service.TrainLogisticRegressionAsync(TrainingRequest(instanceId, UniqueName("Model")));
            outcome.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded);
            (await RunAsync(outcome.RunId)).JobMode.Should().Be(AdvancedAnalyticsJobMode.Online);

            var request = TrainingRequest(instanceId, UniqueName("Model"));
            request.Input = AdvancedAnalyticsTrainingInput.CachePointer(Guid.NewGuid());

            await FluentActions.Awaiting(() => service.TrainLogisticRegressionAsync(request))
                .Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task TheResultCarriesTheFullEvidenceOnlyOnceTheRunSucceededAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var submission = await SubmitAsync(instanceId);
            var service = await TrainingServiceAsync(Permitted);

            var before = await service.GetRunResultAsync(submission.RunId);
            before.Run.Status.Should().Be(AdvancedAnalyticsStatus.Pending);
            before.Evidence.Should().BeNull();
            before.TrainedModelVersionId.Should().BeNull();

            var worker = NewWorker();
            await worker.RunPassAsync();
            await worker.WaitForIdleAsync();

            var after = await service.GetRunResultAsync(submission.RunId);
            after.Run.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded);
            after.Run.Finished.Should().BeTrue();
            after.Run.TrainedModelVersionId.Should().Be(after.TrainedModelVersionId);
            after.VersionNumber.Should().Be(1);
            after.Activated.Should().BeFalse();
            after.Evidence.Should().NotBeNull();
            after.Evidence!.TestMetrics!.RocAuc.Should().BeGreaterThan(0.5);
            after.Evidence.Coefficients.Should().NotBeEmpty();
            JObject.Parse(after.Run.Parameters!)["input"]!["inputs"]!.Type.Should().Be(JTokenType.Null);
            after.Run.Parameters.Should().Contain("modelName");
        }

        [Fact]
        public async Task TheStatusReadShowsFailureAndCancellationReasonsAndListsFilterByStatusAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var failing = await SubmitAsync(instanceId);
            var cancelled = await SubmitAsync(instanceId);
            var service = await TrainingServiceAsync(Permitted);
            await service.CancelRunAsync(cancelled.RunId);
            var worker = NewWorker();
            worker.Trainer = (_, _, _, _, _) => throw new InvalidOperationException("Only one class present.");
            await worker.RunPassAsync();
            await worker.WaitForIdleAsync();

            var status = await service.GetRunAsync(failing.RunId);
            status.Status.Should().Be(AdvancedAnalyticsStatus.Failed);
            status.Error.Should().Be("Only one class present.");
            status.Finished.Should().BeTrue();
            status.ElapsedSeconds.Should().BeGreaterThanOrEqualTo(0);
            status.Parameters.Should().NotBeNull();

            var failed = await service.ListRunsAsync(instanceId, AdvancedAnalyticsStatus.Failed);
            failed.Should().ContainSingle(r => r.RunId == failing.RunId);
            failed.Should().OnlyContain(r => r.Parameters == null);
            (await service.ListRunsAsync(instanceId)).Should().HaveCount(2);
            (await service.GetRunAsync(cancelled.RunId)).Status.Should().Be(AdvancedAnalyticsStatus.Cancelled);
        }

        [Fact]
        public async Task ACancelledRunIsFinalAndCannotBeRevivedByAnotherStopAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var submission = await SubmitAsync(instanceId);
            var service = await TrainingServiceAsync(Permitted);
            await service.CancelRunAsync(submission.RunId);
            var first = await RunAsync(submission.RunId);

            await service.StopRunAsync(submission.RunGuid);
            await service.CancelRunAsync(submission.RunId);

            var after = await RunAsync(submission.RunId);
            after.StatusId.Should().Be(AdvancedAnalyticsStatus.Cancelled);
            after.CompletedDate.Should().Be(first.CompletedDate);
        }

        [Fact]
        public async Task ASucceededRunStaysSucceededWhenCancelledAfterwardsAsync()
        {
            await ResetQueueAsync();
            var instanceId = await InstanceAsync();
            var submission = await SubmitAsync(instanceId);
            var worker = NewWorker();
            await worker.RunPassAsync();
            await worker.WaitForIdleAsync();
            var service = await TrainingServiceAsync(Permitted);

            var status = await service.CancelRunAsync(submission.RunId);

            status.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded);
            (await VersionCountAsync(submission.RunId)).Should().Be(1);
        }
    }
}
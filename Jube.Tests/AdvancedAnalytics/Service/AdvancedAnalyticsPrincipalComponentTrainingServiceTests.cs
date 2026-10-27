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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Repository;
using Jube.Engine.AdvancedAnalytics.DimensionReduction;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Reactivity;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Test.AdvancedAnalytics.Slice;
using Jube.Test.AdvancedAnalytics.Worker;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsPrincipalComponentTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsWorkerTestBase(fx)
    {
        private static double[][] Correlated(int n, int seed)
        {
            var random = new Random(seed);
            var rows = new double[n][];

            for (var i = 0; i < n; i++)
            {
                var x0 = SliceData.Gaussian(random);
                rows[i] = [x0, 0.9 * x0 + 0.3 * SliceData.Gaussian(random)];
            }

            return rows;
        }

        private Task<AdvancedAnalyticsPrincipalComponentTrainingService> PrincipalComponentServiceAsync(string user)
        {
            return AdvancedAnalyticsPrincipalComponentTrainingService.CreateAsync(Db, user, TestLog.NoOp, Localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

        [Fact]
        public async Task SubmitTrainQueuedPromoteSyncAndRecallRoundTripThroughTheDatabaseAsync()
        {
            await ResetQueueAsync();

            var instance = await NewInstanceAsync(TenantA);
            var data = Correlated(600, 41);
            var service = await PrincipalComponentServiceAsync(Permitted);
            var modelName = UniqueName("Pca");

            var submission = await service.SubmitTrainingAsync(new PrincipalComponentTrainingDto
            {
                AskJoobyAnalyticsInstanceId = instance.Id,
                ModelName = modelName,
                VariableNames = ["x0", "x1"],
                ProcessingTypeIds = [1, 1],
                Inputs = data,
                Seed = 3
            });

            submission.Status.Should().Be(AdvancedAnalyticsStatus.Pending);

            var pendingRun = await Db.AdvancedAnalyticsTrainingRun.FirstAsync(w => w.Id == submission.RunId);
            pendingRun.StatusId.Should().Be(AdvancedAnalyticsStatus.Pending);
            pendingRun.KindId.Should().Be(AdvancedAnalyticsKind.DimensionReducer);
            pendingRun.JobMode.Should().Be(AdvancedAnalyticsJobMode.Batch);

            var worker = NewWorker();
            await worker.RunPassAsync();
            await worker.WaitForIdleAsync();

            var status = await service.GetRunAsync(submission.RunId);
            status.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, status.Error);
            status.Finished.Should().BeTrue();

            var result = await service.GetRunResultAsync(submission.RunId);
            result.Evidence.Should().NotBeNull();
            result.Evidence!.Details.RetainedComponents.Should().BeGreaterThan(0);
            result.TrainedModelId.Should().NotBeNull();
            result.TrainedModelVersionId.Should().NotBeNull();
            result.Activated.Should().BeFalse();

            var versionRow =
                await Db.AskJoobyAnalyticsTrainedModelVersion.FirstAsync(w => w.Id == result.TrainedModelVersionId);
            versionRow.ModelJson.Should().Contain("PrincipalComponentPipeline");
            versionRow.MetricsJson.Should().NotBeNullOrWhiteSpace();

            var trainedModelRepository = new AskJoobyAnalyticsTrainedModelRepository(Db, TenantA, Permitted);

            await trainedModelRepository.SetActiveVersionAsync(result.TrainedModelId!.Value,
                result.TrainedModelVersionId!.Value);

            await trainedModelRepository.SetActiveAsync(result.TrainedModelId.Value, true);

            var trainedModelRow = await Db.AskJoobyAnalyticsTrainedModel.FirstAsync(w => w.Id == result.TrainedModelId);
            trainedModelRow.Active.Should().Be(1);
            trainedModelRow.ActiveVersionId.Should().Be(result.TrainedModelVersionId);
            trainedModelRow.KindId.Should().Be(AdvancedAnalyticsKind.DimensionReducer);

            var instanceRow = await Db.AskJoobyAnalyticsInstance.FirstAsync(w => w.Id == instance.Id);

            var syncedModel = SyncAskJoobyAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(instanceRow,
                trainedModelRow, versionRow);

            syncedModel.Should().NotBeNull();
            syncedModel!.Kind.Should().Be("PrincipalComponentPipeline");
            syncedModel.Name.Should().Be(modelName);

            var probe = data[0];
            var recalled = syncedModel.Recall(probe);
            recalled.Should().HaveCount(result.Evidence.Details.RetainedComponents);

            var direct = PrincipalComponentRecall.FromSnapshot(versionRow.ModelJson).Predict(probe);
            recalled.Should().Equal(direct);
        }

        [Fact]
        [Trait("Covers", "PrincipalComponentTrainingRunTools.SubmitPrincipalComponentTrainingAsync")]
        [Trait("Covers", "PrincipalComponentTrainingRunTools.GetPrincipalComponentTrainingRunStatusAsync")]
        [Trait("Covers", "PrincipalComponentTrainingRunTools.CancelPrincipalComponentTrainingRunAsync")]
        [Trait("Covers", "PrincipalComponentTrainingRunTools.GetPrincipalComponentTrainingRunResultAsync")]
        public async Task ToolsSubmitPollAndReadAPrincipalComponentTrainingRunAsync()
        {
            await ResetQueueAsync();

            var instance = await NewInstanceAsync(TenantA);
            var data = Correlated(600, 47);
            var service = await PrincipalComponentServiceAsync(Permitted);

            var tools = new PrincipalComponentTrainingRunTools(
                new AdvancedAnalyticsPrincipalComponentTrainingRunsGateway(service));

            var submission = await tools.SubmitPrincipalComponentTrainingAsync(instance.Id, UniqueName("Pca"),
                ["x0", "x1"], [1, 1], data, seed: 5);

            var worker = NewWorker();
            await worker.RunPassAsync();
            await worker.WaitForIdleAsync();

            var status = await tools.GetPrincipalComponentTrainingRunStatusAsync(submission.RunId);
            status.Finished.Should().BeTrue();
            status.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded.ToString());

            var result = await tools.GetPrincipalComponentTrainingRunResultAsync(submission.RunId);
            result.TrainedModelVersionId.Should().NotBeNull();

            var cancelled = await tools.CancelPrincipalComponentTrainingRunAsync(submission.RunId);
            cancelled.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded.ToString());
        }
    }
}
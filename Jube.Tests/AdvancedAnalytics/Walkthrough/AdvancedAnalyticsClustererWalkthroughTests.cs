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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Repository;
using Jube.Engine.AdvancedAnalytics.Clustering;
using Jube.Engine.AdvancedAnalytics.Recall;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Reactivity;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Test.AdvancedAnalytics.Slice;
using Jube.Test.AdvancedAnalytics.Worker;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Recall.Models;

namespace Jube.Test.AdvancedAnalytics.Walkthrough
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsClustererWalkthroughTests(DatabaseFixture fx)
        : AdvancedAnalyticsWorkerTestBase(fx)
    {
        private static readonly string[] variableNames = ["x0", "x1"];
        private static readonly int[] processingTypeIds = [1, 1];

        private static double[][] ThreeWellSeparatedBlobs(int perBlob, int seed)
        {
            var random = new Random(seed);
            var centres = new[] { new[] { -10d, -10d }, new[] { 10d, -10d }, new[] { 0d, 10d } };
            var rows = new double[perBlob * centres.Length][];
            var index = 0;

            foreach (var centre in centres)
            {
                for (var i = 0; i < perBlob; i++)
                {
                    rows[index++] =
                    [
                        centre[0] + 0.2 * SliceData.Gaussian(random),
                        centre[1] + 0.2 * SliceData.Gaussian(random)
                    ];
                }
            }

            return rows;
        }

        private Task<AdvancedAnalyticsKMeansClustererTrainingService> ClustererTrainingServiceAsync(string user)
        {
            return AdvancedAnalyticsKMeansClustererTrainingService.CreateAsync(Db, user, TestLog.NoOp, Localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

        [Fact]
        [Trait("Covers", "SubmitKMeansClustererTrainingAsync")]
        [Trait("Covers", "GetClustererTrainingRunStatusAsync")]
        [Trait("Covers", "GetClustererTrainingRunResultAsync")]
        public async Task SubmitTrainsPromotesSyncsAndRecallsAClustererModelThroughTheServiceLayerAsync()
        {
            await ResetQueueAsync();
            var instance = await NewInstanceAsync(TenantA);
            var data = ThreeWellSeparatedBlobs(200, 31);

            var service = await ClustererTrainingServiceAsync(Permitted);

            var tools = new KMeansClustererTrainingRunTools(
                new AdvancedAnalyticsKMeansClustererTrainingRunsGateway(service));

            var submission = await tools.SubmitKMeansClustererTrainingAsync(instance.Id, UniqueName("Cluster"),
                variableNames, processingTypeIds, data, seed: 7);

            submission.Status.Should().Be("Pending");

            var worker = NewWorker();
            await worker.RunPassAsync();
            await worker.WaitForIdleAsync();

            var status = await tools.GetClustererTrainingRunStatusAsync(submission.RunId);
            status.Status.Should().Be("Succeeded", status.Error);
            status.Finished.Should().BeTrue();

            var result = await tools.GetClustererTrainingRunResultAsync(submission.RunId);
            result.Activated.Should().BeFalse("training never makes a version live");
            result.TrainedModelVersionId.Should().NotBeNull();
            result.Evidence.Should().NotBeNull();
            result.Evidence!.Details.Clusters.Should().Be(3);
            result.Evidence.Variables.Should().Equal(variableNames);

            var trainedModelId = result.TrainedModelId!.Value;
            var versionId = result.TrainedModelVersionId!.Value;

            var modelsRepository = new AskJoobyAnalyticsTrainedModelRepository(Db, TenantA, Permitted);
            var versionsRepository = new AskJoobyAnalyticsTrainedModelVersionRepository(Db, TenantA, Permitted);

            await modelsRepository.SetActiveVersionAsync(trainedModelId, versionId);
            await modelsRepository.SetActiveAsync(trainedModelId, true);

            var model = await modelsRepository.GetByIdAsync(trainedModelId);
            var version = await versionsRepository.GetByIdAsync(versionId);

            model!.ActiveVersionId.Should().Be(versionId);
            model.Active.Should().Be(1);

            AdvancedAnalyticsModelRegistry.Register(KMeansClustererRecall.ModelKind, snapshotJson =>
            {
                var recall = KMeansClustererRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(KMeansClustererRecall.ModelKind, recall.Variables,
                    recall.AsFunction());
            });

            var variableRows = SyncAskJoobyAnalyticsInstancesExtensions.ParseVariableRows(version!.VariablesJson);

            var advancedModel = SyncAskJoobyAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(instance,
                model, version, variableRows);

            advancedModel.Should().NotBeNull("the same sync path the engine uses must build the model cleanly");
            advancedModel!.Kind.Should().Be(KMeansClustererRecall.ModelKind);

            foreach (var probe in new[] { new[] { -10d, -10d }, new[] { 10d, -10d }, new[] { 0d, 10d } })
            {
                var distances = advancedModel.Recall(probe);

                distances.Should().HaveCount(3);
                distances[0].Should().Be(distances.Min());
                distances[0].Should().BeLessThan(1d);
            }

            var ambiguous = new[] { 0d, -10d };
            var ambiguousDistances = advancedModel.Recall(ambiguous);
            ambiguousDistances[0].Should().Be(ambiguousDistances.Min());
        }

        [Fact]
        [Trait("Covers", "CancelClustererTrainingRunAsync")]
        public async Task CancelStopsAQueuedClustererRunBeforeAWorkerClaimsItAsync()
        {
            await ResetQueueAsync();
            var instance = await NewInstanceAsync(TenantA);
            var data = ThreeWellSeparatedBlobs(20, 3);

            var service = await ClustererTrainingServiceAsync(Permitted);

            var tools = new KMeansClustererTrainingRunTools(
                new AdvancedAnalyticsKMeansClustererTrainingRunsGateway(service));

            var submission = await tools.SubmitKMeansClustererTrainingAsync(instance.Id, UniqueName("Cluster"),
                variableNames, processingTypeIds, data, seed: 2);

            (await tools.GetClustererTrainingRunStatusAsync(submission.RunId)).Status.Should().Be("Pending");

            var cancelled = await tools.CancelClustererTrainingRunAsync(submission.RunId);

            cancelled.Status.Should().Be("Cancelled");
            cancelled.Finished.Should().BeTrue();

            (await tools.GetClustererTrainingRunResultAsync(submission.RunId)).TrainedModelVersionId.Should()
                .BeNull();
        }
    }
}
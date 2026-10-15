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
using System.Globalization;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Repository;
using Jube.Engine.AdvancedAnalytics.Density;
using Jube.Engine.AdvancedAnalytics.Recall;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Reactivity;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Test.AdvancedAnalytics.Service;
using Jube.Test.AdvancedAnalytics.Slice;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Infrastructure.ModelScaffolding;
using LinqToDB;
using log4net;
using Xunit;
using Jube.Service.Repository.AdvancedAnalyticsInstance.Models;
using Jube.Engine.AdvancedAnalytics.Recall.Models;

namespace Jube.Test.AdvancedAnalytics.Walkthrough
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsGaussianMixtureDensityWalkthroughTests(
        DatabaseFixture fx,
        ModelScaffoldFixture engineFixture) : AdvancedAnalyticsServiceTestBase(fx), IClassFixture<ModelScaffoldFixture>
    {
        private const string ModelName = "DensityGaussianMixture";
        private static readonly string[] variableNames = ["Payload.CurrencyAmount", "Payload.SettlementAmount"];
        private static readonly int[] processingTypeIds = [1, 1];

        static AdvancedAnalyticsGaussianMixtureDensityWalkthroughTests()
        {
            if (!AdvancedAnalyticsModelRegistry.IsRegistered(GaussianMixtureDensityRecall.ModelKind))
            {
                AdvancedAnalyticsModelRegistry.Register(GaussianMixtureDensityRecall.ModelKind, snapshotJson =>
                {
                    var recall = GaussianMixtureDensityRecall.FromSnapshot(snapshotJson);

                    return new AdvancedAnalyticsRecallModel(GaussianMixtureDensityRecall.ModelKind, recall.Variables,
                        recall.AsFunction(), recall.Explain);
                });
            }
        }

        [Fact]
        [Trait("Covers", "GaussianMixtureDensityTrainingRunTools.SubmitGaussianMixtureDensityTrainingAsync")]
        [Trait("Covers", "GaussianMixtureDensityTrainingRunTools.GetTrainingRunStatusAsync")]
        [Trait("Covers", "GaussianMixtureDensityTrainingRunTools.GetTrainingRunResultAsync")]
        public async Task SubmitRunsPromotesSyncsAndRecallsAGaussianMixtureDensityModelThroughTheServiceLayerAsync()
        {
            await using var scaffold = await engineFixture.CreateIsolatedAsync(
                new ModelScaffoldOptions { TenantRegistryId = TenantA, ModelGuid = null });

            var instances = await InstanceServiceAsync(Permitted);
            var models = await ModelServiceAsync(Permitted);

            var service = await AdvancedAnalyticsGaussianMixtureDensityTrainingService.CreateAsync(Db, Permitted,
                TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var tools = new GaussianMixtureDensityTrainingRunTools(service);

            var instance = await instances.InsertAsync(new AdvancedAnalyticsInstanceDto
                { Name = UniqueName("DensityWalk") });
            TrackInstance(instance.Id);

            var rows = ClusteredRows(1500, 2024);

            var submission = await tools.SubmitGaussianMixtureDensityTrainingAsync(instance.Id, ModelName,
                variableNames, processingTypeIds, rows, 2, seed: 7);

            submission.Status.Should().Be(AdvancedAnalyticsStatus.Pending);

            var pendingRun = await Db.AdvancedAnalyticsTrainingRun.FirstAsync(w => w.Id == submission.RunId);
            pendingRun.StatusId.Should().Be(AdvancedAnalyticsStatus.Pending);
            pendingRun.KindId.Should().Be(AdvancedAnalyticsKind.DensityEstimator);
            pendingRun.JobMode.Should().Be(AdvancedAnalyticsJobMode.Batch);

            var arguments = GaussianMixtureDensityTrainingArguments.FromJson(pendingRun.Arguments);
            arguments.Inputs.Should().NotBeNull();
            arguments.Inputs!.Length.Should().Be(rows.Length);

            var executor = new AdvancedAnalyticsGaussianMixtureDensityTrainingExecutor(Db, TenantA, Permitted,
                LogManager.GetLogger("Test"));

            var executed = await executor.ExecuteAsync(pendingRun, arguments,
                _ => Task.FromResult(arguments.Inputs!), default);

            executed.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, executed.Error);
            executed.Version.Should().NotBeNull();
            executed.Version!.ModelJson.Should().Contain(GaussianMixtureDensityRecall.ModelKind);
            executed.Version.MetricsJson.Should().NotBeNullOrWhiteSpace();

            var status = await tools.GetTrainingRunStatusAsync(submission.RunId);
            status.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded);
            status.Finished.Should().BeTrue();

            var result = await tools.GetTrainingRunResultAsync(submission.RunId);
            result.Activated.Should().BeFalse();
            result.TrainedModelVersionId.Should().Be(executed.Version.Id);
            result.Evidence.Should().NotBeNull();
            result.Evidence!.Variables.Should().Equal(variableNames);
            result.Evidence.Kind.Should().Be(GaussianMixtureDensityRecall.ModelKind);
            result.Evidence.Details.Components.Should().Be(2);

            var trainedModelId = executed.Model!.Id;
            var versionId = executed.Version.Id;

            var model = await models.GetModelAsync(trainedModelId);
            model!.ActiveVersionId.Should().BeNull();
            model.Kind.Should().Be(AdvancedAnalyticsKind.DensityEstimator);

            var allocated = await instances.AllocateToModelAsync(instance.Id, scaffold.ModelId);
            allocated.EntityAnalysisModelId.Should().Be(scaffold.ModelId);

            var trainedModelRepository = new AdvancedAnalyticsTrainedModelRepository(Db, TenantA, Permitted);
            await trainedModelRepository.SetActiveVersionAsync(trainedModelId, versionId);
            await trainedModelRepository.SetActiveAsync(trainedModelId, true);

            var promoted = await models.GetModelAsync(trainedModelId);
            promoted!.ActiveVersionId.Should().Be(versionId);
            promoted.Active.Should().BeTrue();

            var live = (await instances.GetByIdAsync(instance.Id))!;
            live.Active = true;
            live.ResponsePayload = true;
            var activated = await instances.UpdateAsync(live);

            await EntityApprovalSeed.ApproveAsync(Db, EntityApprovalKind.AdvancedAnalyticsInstance, instance.Id,
                activated.Version, TenantA);

            await ModelSync.SyncAsync(engineFixture.Engine, scaffold);

            engineFixture.Engine.FindActiveModel(scaffold.ModelGuid)!.Snapshot.AdvancedAnalyticsModels
                .Should().ContainSingle(m => m.Name == ModelName);

            var typicalScore = await ScoreAProbeAsync(scaffold, 100, 900);
            var outlierScore = await ScoreAProbeAsync(scaffold, 100000, -900000);

            double.IsFinite(typicalScore).Should().BeTrue();
            double.IsFinite(outlierScore).Should().BeTrue();
            typicalScore.Should().BeGreaterThan(outlierScore);
        }

        [Fact]
        [Trait("Covers", "GaussianMixtureDensityTrainingRunTools.CancelTrainingRunAsync")]
        public async Task CancelToolCancelsAPendingGaussianMixtureDensityRunAsync()
        {
            var service = await AdvancedAnalyticsGaussianMixtureDensityTrainingService.CreateAsync(Db, Permitted,
                TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var tools = new GaussianMixtureDensityTrainingRunTools(service);
            var instances = await InstanceServiceAsync(Permitted);

            var instance = await instances.InsertAsync(new AdvancedAnalyticsInstanceDto
                { Name = UniqueName("DensityCancel") });
            TrackInstance(instance.Id);

            var rows = ClusteredRows(200, 999);

            var submission = await tools.SubmitGaussianMixtureDensityTrainingAsync(instance.Id, UniqueName("Model"),
                variableNames, processingTypeIds, rows, 1);

            var cancelled = await tools.CancelTrainingRunAsync(submission.RunId);

            cancelled.Status.Should().Be(AdvancedAnalyticsStatus.Cancelled);
            cancelled.Finished.Should().BeTrue();
        }

        private static double[][] ClusteredRows(int n, int seed)
        {
            var random = new Random(seed);
            var rows = new double[n][];

            for (var i = 0; i < n; i++)
            {
                var amount = 100 + 20 * SliceData.Gaussian(random);
                var settlement = 900 + 0.9 * (amount - 100) + 5 * SliceData.Gaussian(random);
                rows[i] = [amount, settlement];
            }

            return rows;
        }

        private async Task<double> ScoreAProbeAsync(ModelScaffold scaffold, double currencyAmount,
            double settlementAmount)
        {
            var payload = Payloads.Example()
                .With("TxnId", $"density-{Guid.NewGuid():N}")
                .With("AccountId", $"density-{Guid.NewGuid():N}")
                .With("CurrencyAmount", currencyAmount.ToString("F2", CultureInfo.InvariantCulture))
                .With("SettlementAmount", settlementAmount.ToString("F2", CultureInfo.InvariantCulture))
                .With("AmountGBP", "90")
                .With("AmountEURRate", "1.0");

            var response = await engineFixture.Api.InvokeAsync(scaffold, payload);

            response.Status.Should().Be(200);
            var score = response.Find("AdvancedAnalyticsAdaptation", ModelName);
            score.Should().NotBeNull("the live model must score the probe row: " + response.Body);

            return score!.Value.GetDouble();
        }
    }
}
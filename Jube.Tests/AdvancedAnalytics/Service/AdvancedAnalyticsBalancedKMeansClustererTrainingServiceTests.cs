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
using Jube.Engine.AdvancedAnalytics.Clustering;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Recall;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Service.Reactivity;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Test.AdvancedAnalytics.Slice;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;
using Jube.Engine.AdvancedAnalytics.Recall.Models;
using Jube.Service.Repository.AdvancedAnalyticsModel.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsBalancedKMeansClustererTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        static AdvancedAnalyticsBalancedKMeansClustererTrainingServiceTests()
        {
            if (!AdvancedAnalyticsModelRegistry.IsRegistered(BalancedKMeansClustererRecall.ModelKind))
            {
                AdvancedAnalyticsModelRegistry.Register(BalancedKMeansClustererRecall.ModelKind, snapshotJson =>
                {
                    var recall = BalancedKMeansClustererRecall.FromSnapshot(snapshotJson);

                    return new AdvancedAnalyticsRecallModel(BalancedKMeansClustererRecall.ModelKind, recall.Variables,
                        recall.AsFunction());
                });
            }
        }

        private Task<AdvancedAnalyticsBalancedKMeansClustererTrainingService> ClustererServiceAsync(string user)
        {
            return AdvancedAnalyticsBalancedKMeansClustererTrainingService.CreateAsync(Db, user, TestLog.NoOp,
                Localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

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

        [Fact]
        [Trait("Covers", "BalancedKMeansClustererTrainingRunTools.SubmitBalancedKMeansClustererTrainingAsync")]
        public async Task SubmitTrainsExecutesAndRecallsBitForBitAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var inputs = ThreeWellSeparatedBlobs(80, 61);
            var names = new[] { "Amount", "RiskScore" };
            var processingTypeIds = new[] { 1, 5 };
            const string modelName = "WalkthroughBalancedKMeansClusterer";

            var service = await ClustererServiceAsync(Permitted);

            var submission = await service.SubmitTrainingAsync(new AdvancedAnalyticsBalancedKMeansClustererTrainingDto
            {
                AdvancedAnalyticsInstanceId = instance.Id,
                ModelName = modelName,
                VariableNames = names,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Clusters = 3,
                Seed = 61
            });

            submission.Status.Should().Be(AdvancedAnalyticsStatus.Pending);
            var runId = submission.RunId;

            var runs = new AdvancedAnalyticsTrainingRunRepository(Db, TenantA, Permitted);
            var run = await runs.GetByIdAsync(runId);
            run.Should().NotBeNull();
            run!.KindId.Should().Be(AdvancedAnalyticsKind.Clusterer);
            run.ModelKindId.Should().Be(AdvancedAnalyticsModelKind.BalancedKMeansClustererPipeline);

            await runs.UpdateStatusAsync(runId, AdvancedAnalyticsStatus.Running);
            var arguments = BalancedKMeansClustererTrainingArguments.FromJson(run.Arguments);

            var executor = new BalancedKMeansClustererTrainingExecutor(Db,
                TenantA, Permitted, TestLog.NoOp);

            var executed = await executor.ExecuteAsync(run, arguments,
                _ => Task.FromResult(arguments.Inputs!), default);

            executed.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, executed.Error);
            executed.Result.Should().NotBeNull();
            executed.Result!.Details.ClusterSummaries.Should().HaveCount(3);
            executed.Version.Should().NotBeNull();

            var evidence = AdvancedAnalyticsEvidenceJson
                .Deserialise<AdvancedAnalyticsEvidence<BalancedKMeansEvidenceDetails>>(
                    executed.Version!.MetricsJson);

            evidence.Should().NotBeNull();
            evidence!.Kind.Should().Be(BalancedKMeansClustererRecall.ModelKind);
            evidence.Preprocessing.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Name));

            var trainedModelId = executed.Model!.Id;
            var versionId = executed.Version.Id;

            var trainedModelRepository = new AdvancedAnalyticsTrainedModelRepository(Db, TenantA, Permitted);
            await trainedModelRepository.SetActiveVersionAsync(trainedModelId, versionId);
            await trainedModelRepository.SetActiveAsync(trainedModelId, true);

            var trainedModel = await trainedModelRepository.GetByIdAsync(trainedModelId);
            trainedModel!.ActiveVersionId.Should().Be(versionId);
            trainedModel.Active.Should().Be(1);

            var directResult = BalancedKMeansClustererTrainer.Train(inputs, names,
                new BalancedKMeansClustererOptions { Clusters = 3, Seed = 61 });

            var directRecall = BalancedKMeansClustererRecall.FromSnapshot(directResult.SnapshotJson);

            var recalledFromDb = BalancedKMeansClustererRecall.FromSnapshot(executed.Version.ModelJson);

            foreach (var row in inputs.Take(25))
            {
                recalledFromDb.Predict(row)[0].Should().Be(directRecall.Predict(row)[0]);
            }
        }
    }
}
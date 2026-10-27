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
using Jube.Service.Repository.AskJoobyAnalyticsModel.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsKMedoidsClustererTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        static AdvancedAnalyticsKMedoidsClustererTrainingServiceTests()
        {
            if (!AdvancedAnalyticsModelRegistry.IsRegistered(KMedoidsClustererRecall.ModelKind))
            {
                AdvancedAnalyticsModelRegistry.Register(KMedoidsClustererRecall.ModelKind, snapshotJson =>
                {
                    var recall = KMedoidsClustererRecall.FromSnapshot(snapshotJson);

                    return new AdvancedAnalyticsRecallModel(KMedoidsClustererRecall.ModelKind, recall.Variables,
                        recall.AsFunction());
                });
            }
        }

        private Task<AdvancedAnalyticsKMedoidsClustererTrainingService> ClustererServiceAsync(string user)
        {
            return AdvancedAnalyticsKMedoidsClustererTrainingService.CreateAsync(Db, user, TestLog.NoOp, Localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

        private static (double[][] Inputs, string[] Names) ThreeBlobs(int perBlob, int seed)
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

            return (rows, ["Amount", "RiskScore"]);
        }

        [Fact]
        [Trait("Covers", "KMedoidsClustererTrainingRunTools.SubmitKMedoidsClustererTrainingAsync")]
        public async Task SubmitTrainsExecutesAndRecallsBitForBitAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var (inputs, names) = ThreeBlobs(120, 71);
            var processingTypeIds = names.Select((_, i) => i % 2 == 0 ? 1 : 5).ToArray();
            const string modelName = "WalkthroughKMedoidsClusterer";

            var service = await ClustererServiceAsync(Permitted);

            var submission = await service.SubmitTrainingAsync(new AdvancedAnalyticsKMedoidsClustererTrainingDto
            {
                AskJoobyAnalyticsInstanceId = instance.Id,
                ModelName = modelName,
                VariableNames = names,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Seed = 71,
                Clusters = 3
            });

            submission.Status.Should().Be(AdvancedAnalyticsStatus.Pending);
            var runId = submission.RunId;

            var runs = new AdvancedAnalyticsTrainingRunRepository(Db, TenantA, Permitted);
            var run = await runs.GetByIdAsync(runId);
            run.Should().NotBeNull();
            run!.KindId.Should().Be(AdvancedAnalyticsKind.Clusterer);
            run.ModelKindId.Should().Be(AdvancedAnalyticsModelKind.KMedoidsClustererPipeline);

            await runs.UpdateStatusAsync(runId, AdvancedAnalyticsStatus.Running);
            var arguments = KMedoidsClustererTrainingArguments.FromJson(run.Arguments);
            var executor = new KMedoidsClustererTrainingExecutor(Db, TenantA, Permitted, TestLog.NoOp);

            var executed = await executor.ExecuteAsync(run, arguments,
                _ => Task.FromResult(arguments.Inputs!), default);

            executed.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, executed.Error);
            executed.Result.Should().NotBeNull();
            executed.Result!.Details.ClusterSummaries.Should().HaveCount(3);
            executed.Version.Should().NotBeNull();

            var evidence = AdvancedAnalyticsEvidenceJson
                .Deserialise<AdvancedAnalyticsEvidence<KMedoidsEvidenceDetails>>(executed.Version!.MetricsJson);

            evidence.Should().NotBeNull();
            evidence!.Kind.Should().Be(KMedoidsClustererRecall.ModelKind);
            evidence.Preprocessing.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Name));

            var trainedModelId = executed.Model!.Id;
            var versionId = executed.Version.Id;

            var trainedModelRepository = new AskJoobyAnalyticsTrainedModelRepository(Db, TenantA, Permitted);
            await trainedModelRepository.SetActiveVersionAsync(trainedModelId, versionId);
            await trainedModelRepository.SetActiveAsync(trainedModelId, true);

            var trainedModel = await trainedModelRepository.GetByIdAsync(trainedModelId);
            trainedModel!.ActiveVersionId.Should().Be(versionId);
            trainedModel.Active.Should().Be(1);

            var directResult = KMedoidsClustererTrainer.Train(inputs, names,
                new KMedoidsClustererOptions { Seed = 71, Clusters = 3 });

            var directRecall = KMedoidsClustererRecall.FromSnapshot(directResult.SnapshotJson);

            var recalledFromDb = KMedoidsClustererRecall.FromSnapshot(executed.Version.ModelJson);

            foreach (var row in inputs.Take(25))
            {
                recalledFromDb.Predict(row)[0].Should().Be(directRecall.Predict(row)[0]);
            }
        }
    }
}
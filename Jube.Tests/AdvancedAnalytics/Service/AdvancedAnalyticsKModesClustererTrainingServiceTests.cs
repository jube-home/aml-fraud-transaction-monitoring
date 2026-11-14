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
    public sealed class AdvancedAnalyticsKModesClustererTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        static AdvancedAnalyticsKModesClustererTrainingServiceTests()
        {
            if (!AdvancedAnalyticsModelRegistry.IsRegistered(KModesClustererRecall.ModelKind))
            {
                AdvancedAnalyticsModelRegistry.Register(KModesClustererRecall.ModelKind, snapshotJson =>
                {
                    var recall = KModesClustererRecall.FromSnapshot(snapshotJson);

                    return new AdvancedAnalyticsRecallModel(KModesClustererRecall.ModelKind, recall.Variables,
                        recall.AsFunction());
                });
            }
        }

        private Task<AdvancedAnalyticsKModesClustererTrainingService> KModesServiceAsync(string user)
        {
            return AdvancedAnalyticsKModesClustererTrainingService.CreateAsync(Db, user, TestLog.NoOp, Localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

        private static (double[][] Inputs, string[] Names) CategoricalData(int perGroup, int seed)
        {
            var random = new Random(seed);
            var groups = new[] { new[] { 1d, 1d }, new[] { 2d, 2d }, new[] { 3d, 3d } };
            var rows = new double[perGroup * groups.Length][];
            var index = 0;

            foreach (var group in groups)
            {
                for (var i = 0; i < perGroup; i++)
                {
                    var row = (double[])group.Clone();

                    if (random.NextDouble() < 0.05)
                    {
                        row[random.Next(row.Length)] = group[0] == 1 ? 2 : 1;
                    }

                    rows[index++] = row;
                }
            }

            return (rows, ["x0", "x1"]);
        }

        [Fact]
        [Trait("Covers", "KModesClustererTrainingRunTools.SubmitKModesClustererTrainingAsync")]
        public async Task SubmitTrainsExecutesAndRecallsBitForBitAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var (inputs, names) = CategoricalData(300, 71);
            var processingTypeIds = names.Select((_, i) => i % 2 == 0 ? 1 : 5).ToArray();
            const string modelName = "WalkthroughKModesClusterer";

            var service = await KModesServiceAsync(Permitted);

            var submission = await service.SubmitTrainingAsync(new AdvancedAnalyticsKModesClustererTrainingDto
            {
                AskJoobyAnalyticsInstanceId = instance.Id,
                ModelName = modelName,
                VariableNames = names,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Clusters = 3,
                Seed = 71
            });

            submission.Status.Should().Be(AdvancedAnalyticsStatus.Pending);
            var runId = submission.RunId;

            var runs = new AdvancedAnalyticsTrainingRunRepository(Db, TenantA, Permitted);
            var run = await runs.GetByIdAsync(runId);
            run.Should().NotBeNull();
            run!.KindId.Should().Be(AdvancedAnalyticsKind.Clusterer);
            run.ModelKindId.Should().Be(AdvancedAnalyticsModelKind.KModesClustererPipeline);

            await runs.UpdateStatusAsync(runId, AdvancedAnalyticsStatus.Running);
            var arguments = KModesClustererTrainingArguments.FromJson(run.Arguments);
            var executor = new KModesClustererTrainingExecutor(Db, TenantA, Permitted, TestLog.NoOp);

            var executed = await executor.ExecuteAsync(run, arguments,
                _ => Task.FromResult(arguments.Inputs!), default);

            executed.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, executed.Error);
            executed.Result.Should().NotBeNull();
            executed.Result!.Details.ClusterSummaries.Should().HaveCount(3);
            executed.Version.Should().NotBeNull();

            var evidence = AdvancedAnalyticsEvidenceJson
                .Deserialise<AdvancedAnalyticsEvidence<KModesEvidenceDetails>>(executed.Version!.MetricsJson);

            evidence.Should().NotBeNull();
            evidence!.Kind.Should().Be(KModesClustererRecall.ModelKind);
            evidence.Preprocessing.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Name));

            var trainedModelId = executed.Model!.Id;
            var versionId = executed.Version.Id;

            var trainedModelRepository = new AskJoobyAnalyticsTrainedModelRepository(Db, TenantA, Permitted);
            await trainedModelRepository.SetActiveVersionAsync(trainedModelId, versionId);
            await trainedModelRepository.SetActiveAsync(trainedModelId, true);

            var trainedModel = await trainedModelRepository.GetByIdAsync(trainedModelId);
            trainedModel!.ActiveVersionId.Should().Be(versionId);
            trainedModel.Active.Should().Be(1);

            var directResult = KModesClustererTrainer.Train(inputs, names,
                new KModesClustererOptions { Clusters = 3, Seed = 71 });

            var directRecall = KModesClustererRecall.FromSnapshot(directResult.SnapshotJson);

            var recalledFromDb = KModesClustererRecall.FromSnapshot(executed.Version.ModelJson);

            foreach (var row in inputs.Take(25))
            {
                recalledFromDb.Predict(row).Should().Equal(directRecall.Predict(row));
            }
        }
    }
}
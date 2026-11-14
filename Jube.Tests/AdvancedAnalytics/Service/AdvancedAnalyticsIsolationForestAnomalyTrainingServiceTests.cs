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
using Jube.Engine.AdvancedAnalytics.Anomaly;
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
using Jube.Engine.AdvancedAnalytics.Anomaly.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsIsolationForestAnomalyTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        static AdvancedAnalyticsIsolationForestAnomalyTrainingServiceTests()
        {
            if (!AdvancedAnalyticsModelRegistry.IsRegistered(IsolationForestAnomalyRecall.ModelKind))
            {
                AdvancedAnalyticsModelRegistry.Register(IsolationForestAnomalyRecall.ModelKind, snapshotJson =>
                {
                    var recall = IsolationForestAnomalyRecall.FromSnapshot(snapshotJson);

                    return new AdvancedAnalyticsRecallModel(IsolationForestAnomalyRecall.ModelKind, recall.Variables,
                        recall.AsFunction(), recall.Explain);
                });
            }
        }

        private Task<AdvancedAnalyticsIsolationForestAnomalyTrainingService> ForestServiceAsync(string user)
        {
            return AdvancedAnalyticsIsolationForestAnomalyTrainingService.CreateAsync(Db, user, TestLog.NoOp,
                Localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

        private static (double[][] Inputs, string[] Names) OutlierData(int normalCount, int outlierCount, int seed)
        {
            var random = new Random(seed);
            var rows = new double[normalCount + outlierCount][];

            for (var i = 0; i < normalCount; i++)
            {
                rows[i] = [SliceData.Gaussian(random) * 0.5, SliceData.Gaussian(random) * 0.5];
            }

            for (var i = 0; i < outlierCount; i++)
            {
                rows[normalCount + i] = [10d + SliceData.Gaussian(random), -10d + SliceData.Gaussian(random)];
            }

            return (rows, ["x0", "x1"]);
        }

        [Fact]
        [Trait("Covers", "IsolationForestAnomalyTrainingRunTools.SubmitIsolationForestAnomalyTrainingAsync")]
        public async Task SubmitTrainsExecutesAndRecallsBitForBitAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var (inputs, names) = OutlierData(900, 20, 71);
            var processingTypeIds = names.Select((_, i) => i % 2 == 0 ? 1 : 5).ToArray();
            const string modelName = "WalkthroughIsolationForestAnomaly";

            var service = await ForestServiceAsync(Permitted);

            var submission = await service.SubmitTrainingAsync(new AdvancedAnalyticsIsolationForestAnomalyTrainingDto
            {
                AskJoobyAnalyticsInstanceId = instance.Id,
                ModelName = modelName,
                VariableNames = names,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Seed = 71,
                TreeCount = 50,
                SubsampleSize = 128
            });

            submission.Status.Should().Be(AdvancedAnalyticsStatus.Pending);
            var runId = submission.RunId;

            var runs = new AdvancedAnalyticsTrainingRunRepository(Db, TenantA, Permitted);
            var run = await runs.GetByIdAsync(runId);
            run.Should().NotBeNull();
            run!.KindId.Should().Be(AdvancedAnalyticsKind.AnomalyDetector);
            run.ModelKindId.Should().Be(AdvancedAnalyticsModelKind.IsolationForestAnomalyPipeline);

            await runs.UpdateStatusAsync(runId, AdvancedAnalyticsStatus.Running);
            var arguments = IsolationForestAnomalyTrainingArguments.FromJson(run.Arguments);

            var executor = new AdvancedAnalyticsIsolationForestAnomalyTrainingExecutor(Db, TenantA, Permitted,
                TestLog.NoOp);

            var executed = await executor.ExecuteAsync(run, arguments, arguments.Inputs!, arguments.Labels, default);

            executed.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, executed.Error);
            executed.Result.Should().NotBeNull();
            executed.Result!.Details.TestScores.Should().NotBeNull();
            executed.Version.Should().NotBeNull();

            var evidence = AdvancedAnalyticsEvidenceJson
                .Deserialise<AdvancedAnalyticsEvidence<IsolationForestEvidenceDetails>>(
                    executed.Version!.MetricsJson);

            evidence.Should().NotBeNull();
            evidence!.Kind.Should().Be(IsolationForestAnomalyRecall.ModelKind);
            evidence.Preprocessing.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Name));

            var trainedModelId = executed.Model!.Id;
            var versionId = executed.Version.Id;

            var trainedModelRepository = new AskJoobyAnalyticsTrainedModelRepository(Db, TenantA, Permitted);
            await trainedModelRepository.SetActiveVersionAsync(trainedModelId, versionId);
            await trainedModelRepository.SetActiveAsync(trainedModelId, true);

            var trainedModel = await trainedModelRepository.GetByIdAsync(trainedModelId);
            trainedModel!.ActiveVersionId.Should().Be(versionId);
            trainedModel.Active.Should().Be(1);

            var directResult = IsolationForestAnomalyTrainer.Train(inputs, null, names,
                new IsolationForestAnomalyOptions { Seed = 71, TreeCount = 50, SubsampleSize = 128 });

            var directRecall = IsolationForestAnomalyRecall.FromSnapshot(directResult.SnapshotJson);

            var recalledFromDb = IsolationForestAnomalyRecall.FromSnapshot(executed.Version.ModelJson);

            foreach (var row in inputs.Take(25))
            {
                recalledFromDb.Predict(row)[0].Should().Be(directRecall.Predict(row)[0]);
            }
        }
    }
}
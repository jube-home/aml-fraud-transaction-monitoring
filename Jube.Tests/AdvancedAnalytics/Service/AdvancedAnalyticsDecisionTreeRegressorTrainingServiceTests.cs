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
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Recall;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.Service.Reactivity;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Test.AdvancedAnalytics.Slice;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;
using Jube.Service.Repository.AskJoobyAnalyticsModel.Models;
using Jube.Engine.AdvancedAnalytics.Recall.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsDecisionTreeRegressorTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        static AdvancedAnalyticsDecisionTreeRegressorTrainingServiceTests()
        {
            AdvancedAnalyticsModelRegistry.Register(DecisionTreeRegressorRecall.ModelKind, snapshotJson =>
            {
                var recall = DecisionTreeRegressorRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(DecisionTreeRegressorRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });
        }

        private Task<AdvancedAnalyticsDecisionTreeRegressorTrainingService> TreeServiceAsync(string user)
        {
            return AdvancedAnalyticsDecisionTreeRegressorTrainingService.CreateAsync(Db, user, TestLog.NoOp, Localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

        private static (double[][] Inputs, double[] Outputs, string[] Names) StepData(int n, int seed,
            double noiseStandardDeviation = 0.3)
        {
            var random = new Random(seed);
            var inputs = new double[n][];
            var outputs = new double[n];

            for (var i = 0; i < n; i++)
            {
                var x0 = SliceData.Gaussian(random);
                var x1 = SliceData.Gaussian(random);
                inputs[i] = [x0, x1];
                outputs[i] = (x0 > 0d ? 5d : -5d) + noiseStandardDeviation * SliceData.Gaussian(random);
            }

            return (inputs, outputs, ["x0", "x1"]);
        }

        [Fact]
        [Trait("Covers", "DecisionTreeRegressorTrainingRunTools.SubmitDecisionTreeRegressorTrainingAsync")]
        public async Task SubmitTrainsExecutesPromotesSyncsAndRecallsBitForBitAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var data = StepData(900, 71);
            var processingTypeIds = data.Names.Select((_, i) => i % 2 == 0 ? 1 : 5).ToArray();
            const string modelName = "WalkthroughDecisionTreeRegressor";

            var service = await TreeServiceAsync(Permitted);

            var submission = await service.SubmitTrainingAsync(new AdvancedAnalyticsDecisionTreeRegressorTrainingDto
            {
                AskJoobyAnalyticsInstanceId = instance.Id,
                ModelName = modelName,
                VariableNames = data.Names,
                ProcessingTypeIds = processingTypeIds,
                Inputs = data.Inputs,
                Outputs = data.Outputs,
                Seed = 71
            });

            submission.Status.Should().Be(AdvancedAnalyticsStatus.Pending);
            var runId = submission.RunId;

            var runs = new AdvancedAnalyticsTrainingRunRepository(Db, TenantA, Permitted);
            var run = await runs.GetByIdAsync(runId);
            run.Should().NotBeNull();
            run!.KindId.Should().Be(AdvancedAnalyticsKind.Regressor);
            run.ModelKindId.Should().Be(AdvancedAnalyticsModelKind.DecisionTreeRegressorPipeline);

            await runs.UpdateStatusAsync(runId, AdvancedAnalyticsStatus.Running);
            var arguments = DecisionTreeRegressorTrainingArguments.FromJson(run.Arguments);

            var executor = new AdvancedAnalyticsDecisionTreeRegressorTrainingExecutor(Db, TenantA, Permitted,
                TestLog.NoOp);

            var executed = await executor.ExecuteAsync(run, arguments, default);

            executed.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, executed.Error);
            executed.Result.Should().NotBeNull();
            executed.Result!.Details.TestMetrics.Should().NotBeNull();
            executed.Version.Should().NotBeNull();

            var evidence = AdvancedAnalyticsEvidenceJson
                .Deserialise<AdvancedAnalyticsEvidence<DecisionTreeRegressorEvidenceDetails>>(
                    executed.Version!.MetricsJson);

            evidence.Should().NotBeNull();
            evidence!.Kind.Should().Be(DecisionTreeRegressorRecall.ModelKind);
            evidence.Preprocessing.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Name));

            var trainedModelId = executed.Model!.Id;
            var versionId = executed.Version.Id;

            var trainedModelRepository = new AskJoobyAnalyticsTrainedModelRepository(Db, TenantA, Permitted);
            await trainedModelRepository.SetActiveVersionAsync(trainedModelId, versionId);
            await trainedModelRepository.SetActiveAsync(trainedModelId, true);

            var trainedModel = await trainedModelRepository.GetByIdAsync(trainedModelId);
            trainedModel!.ActiveVersionId.Should().Be(versionId);
            trainedModel.Active.Should().Be(1);

            var version = await new AskJoobyAnalyticsTrainedModelVersionRepository(Db, TenantA, Permitted)
                .GetActiveByModelIdAsync(trainedModelId);
            version.Should().NotBeNull();

            var syncedModel = SyncAskJoobyAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(instance,
                trainedModel, version!);

            syncedModel.Should().NotBeNull();
            syncedModel!.Kind.Should().Be(DecisionTreeRegressorRecall.ModelKind);
            syncedModel.VariablesInOrder.Select(v => v.Name).Should().Equal(data.Names);

            var probe = data.Inputs[0];

            var directResult = DecisionTreeRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new DecisionTreeRegressorOptions { Seed = 71 });

            var directRecall = DecisionTreeRegressorRecall.FromSnapshot(directResult.SnapshotJson);
            var expectedScore = directRecall.Predict(probe)[0];

            var syncedScore = syncedModel.Recall(probe)[0];
            syncedScore.Should().Be(expectedScore);

            syncedModel.Explain.Should().NotBeNull();
            var explanation = syncedModel.Explain!(probe);

            var adaptation = explanation.ToAdaptation(syncedScore, syncedModel.Name,
                [.. syncedModel.VariablesInOrder.Select(v => v.ProcessingTypeId)]);

            adaptation.Value.Should().Be(syncedScore);
            adaptation.Calibration!.Calibrated.Should().BeFalse();
            adaptation.Contribution!.Exact.Should().BeFalse();

            var recalledFromDb = DecisionTreeRegressorRecall.FromSnapshot(executed.Version.ModelJson);

            foreach (var row in data.Inputs.Take(25))
            {
                recalledFromDb.Predict(row)[0].Should().Be(directRecall.Predict(row)[0]);
            }
        }
    }
}
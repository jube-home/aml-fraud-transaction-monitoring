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
using Jube.Engine.AdvancedAnalytics.Regression.Models;
using Jube.Engine.AdvancedAnalytics.Recall.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsLinearRegressionServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        static AdvancedAnalyticsLinearRegressionServiceTests()
        {
            AdvancedAnalyticsModelRegistry.Register(MultipleLinearRegressionRecall.ModelKind, snapshotJson =>
            {
                var recall = MultipleLinearRegressionRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(MultipleLinearRegressionRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });
        }

        private Task<AdvancedAnalyticsLinearRegressionTrainingService> LinearServiceAsync(string user)
        {
            return AdvancedAnalyticsLinearRegressionTrainingService.CreateAsync(Db, user, TestLog.NoOp, Localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

        [Fact]
        [Trait("Covers", "LinearRegressionTrainingRunTools.SubmitLinearRegressionTrainingAsync")]
        [Trait("Covers", "LinearRegressionTrainingRunTools.GetTrainingRunStatusAsync")]
        [Trait("Covers", "LinearRegressionTrainingRunTools.CancelTrainingRunAsync")]
        [Trait("Covers", "LinearRegressionTrainingRunTools.GetTrainingRunResultAsync")]
        public async Task SubmitTrainRunExecutesPromotesSyncsAndRecallsBitForBitAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var data = LinearSliceData.Continuous(1500, 31, -0.4, [1.1, -0.7, 0.5], 1, 0.05);
            var processingTypeIds = data.Names.Select((_, i) => i % 2 == 0 ? 1 : 5).ToArray();
            const string modelName = "WalkthroughLinear";

            var service = await LinearServiceAsync(Permitted);

            var submission = await service.SubmitTrainingAsync(new AdvancedAnalyticsLinearRegressionTrainingDto
            {
                AdvancedAnalyticsInstanceId = instance.Id,
                ModelName = modelName,
                VariableNames = data.Names,
                ProcessingTypeIds = processingTypeIds,
                Inputs = data.Inputs,
                Outputs = data.Outputs,
                Seed = 9
            });

            submission.Status.Should().Be(AdvancedAnalyticsStatus.Pending);
            var runId = submission.RunId;

            (await service.GetRunAsync(runId)).Status.Should().Be(AdvancedAnalyticsStatus.Pending);

            var runs = new AdvancedAnalyticsTrainingRunRepository(Db, TenantA, Permitted);
            var run = await runs.GetByIdAsync(runId);
            run.Should().NotBeNull();
            run!.KindId.Should().Be(AdvancedAnalyticsKind.Regressor);

            await runs.UpdateStatusAsync(runId, AdvancedAnalyticsStatus.Running);
            var arguments = AdvancedAnalyticsLinearRegressionTrainingArguments.FromJson(run.Arguments);

            var executor = new AdvancedAnalyticsLinearRegressionTrainingExecutor(Db, TenantA, Permitted,
                TestLog.NoOp);

            var executed = await executor.ExecuteAsync(run, arguments, default);

            executed.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded);
            executed.Result.Should().NotBeNull();
            executed.Result!.TestMetrics.Should().NotBeNull();

            var status = await service.GetRunAsync(runId);
            status.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded);
            status.Finished.Should().BeTrue();
            status.TrainedModelVersionId.Should().NotBeNull();

            var result = await service.GetRunResultAsync(runId);
            result.Activated.Should().BeFalse("training never makes a version live");
            result.Evidence.Should().NotBeNull();
            result.Evidence!.Variables.Should().Equal(data.Names);
            result.Evidence.Details.Coefficients.Should().NotBeEmpty();
            result.Evidence.Details.TestMetrics.Should().NotBeNull();
            var trainedModelId = result.TrainedModelId!.Value;
            var versionId = result.TrainedModelVersionId!.Value;

            var trainedModelRepository = new AdvancedAnalyticsTrainedModelRepository(Db, TenantA, Permitted);
            await trainedModelRepository.SetActiveVersionAsync(trainedModelId, versionId);
            await trainedModelRepository.SetActiveAsync(trainedModelId, true);

            var trainedModel = await trainedModelRepository.GetByIdAsync(trainedModelId);
            trainedModel!.ActiveVersionId.Should().Be(versionId);
            trainedModel.Active.Should().Be(1);

            var version = await new AdvancedAnalyticsTrainedModelVersionRepository(Db, TenantA, Permitted)
                .GetActiveByModelIdAsync(trainedModelId);
            version.Should().NotBeNull();

            var syncedModel = SyncAdvancedAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(instance,
                trainedModel, version!);

            syncedModel.Should().NotBeNull();
            syncedModel!.Kind.Should().Be(MultipleLinearRegressionRecall.ModelKind);
            syncedModel.VariablesInOrder.Select(v => v.Name).Should().Equal(data.Names);

            var probe = data.Inputs[0];
            var directRecall = MultipleLinearRegressionRecall.FromSnapshot(version!.ModelJson);
            var expectedScore = directRecall.Predict(probe)[0];

            var syncedScore = syncedModel.Recall(probe)[0];
            syncedScore.Should().Be(expectedScore);

            syncedModel.Explain.Should().NotBeNull();
            var explanation = syncedModel.Explain!(probe);

            var adaptation = explanation.ToAdaptation(syncedScore, syncedModel.Name,
                [.. syncedModel.VariablesInOrder.Select(v => v.ProcessingTypeId)]);

            adaptation.Value.Should().Be(syncedScore);
            adaptation.Calibration!.Calibrated.Should().BeFalse();
            adaptation.Contribution!.Exact.Should().BeTrue();

            var cancelData = LinearSliceData.Continuous(200, 5, 0, [1.0]);

            var cancelSubmission = await service.SubmitTrainingAsync(new AdvancedAnalyticsLinearRegressionTrainingDto
            {
                AdvancedAnalyticsInstanceId = instance.Id,
                ModelName = modelName,
                VariableNames = cancelData.Names,
                ProcessingTypeIds = [1],
                Inputs = cancelData.Inputs,
                Outputs = cancelData.Outputs
            });

            var cancelled = await service.CancelRunAsync(cancelSubmission.RunId);
            cancelled.Status.Should().Be(AdvancedAnalyticsStatus.Cancelled);
            cancelled.Finished.Should().BeTrue();
            (await service.GetRunResultAsync(cancelSubmission.RunId)).TrainedModelVersionId.Should().BeNull();
        }

        [Fact]
        public async Task GetRunResultReturnsTheEvidenceTypedToTheLinearRegressionDetailsAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var data = LinearSliceData.Continuous(600, 41, 0.2, [0.8, -0.3]);
            var service = await LinearServiceAsync(Permitted);

            var submission = await service.SubmitTrainingAsync(new AdvancedAnalyticsLinearRegressionTrainingDto
            {
                AdvancedAnalyticsInstanceId = instance.Id,
                ModelName = "TypedEvidence",
                VariableNames = data.Names,
                ProcessingTypeIds = [1, 1],
                Inputs = data.Inputs,
                Outputs = data.Outputs
            });

            var runs = new AdvancedAnalyticsTrainingRunRepository(Db, TenantA, Permitted);
            var run = await runs.GetByIdAsync(submission.RunId);
            var arguments = AdvancedAnalyticsLinearRegressionTrainingArguments.FromJson(run!.Arguments);

            var executor = new AdvancedAnalyticsLinearRegressionTrainingExecutor(Db, TenantA, Permitted,
                TestLog.NoOp);

            await executor.ExecuteAsync(run, arguments, default);

            var result = await service.GetRunResultAsync(submission.RunId);
            var evidence = result.Evidence;

            evidence.Should().NotBeNull();
            evidence.Should().BeOfType<AdvancedAnalyticsEvidence<LinearRegressionEvidenceDetails>>();
            evidence!.Kind.Should().Be(MultipleLinearRegressionRecall.ModelKind);
            evidence.Preprocessing.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Name));
        }
    }
}
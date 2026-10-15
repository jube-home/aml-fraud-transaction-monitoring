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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Repository.AdvancedAnalyticsInstance;
using Jube.Service.Repository.AdvancedAnalyticsModel;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Test.AdvancedAnalytics.Slice;
using Jube.Test.AdvancedAnalytics.Worker;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Infrastructure.ModelScaffolding;
using Xunit;
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;
using Jube.Service.Repository.AdvancedAnalyticsInstance.Models;
using Jube.Service.Repository.AdvancedAnalyticsModel.Models;

namespace Jube.Test.AdvancedAnalytics.Walkthrough
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsWalkthroughTests(DatabaseFixture fx, ModelScaffoldFixture engineFixture)
        : AdvancedAnalyticsWorkerTestBase(fx), IClassFixture<ModelScaffoldFixture>
    {
        private const string ModelName = "FraudLogistic";
        private const double MinimumTestAuc = 0.70;
        private const double SignificanceLevel = 0.05;
        private const double MaximumCalibrationError = 0.10;
        private const double MinimumCalibrationSlope = 0.6;
        private const double MaximumCalibrationSlope = 1.4;
        private const double AucToleranceForReplacement = 0.01;
        private const double LogLossToleranceForReplacement = 0.02;
        private static readonly TimeSpan pollEvery = TimeSpan.FromMilliseconds(25);
        private static readonly TimeSpan giveUpAfter = TimeSpan.FromSeconds(60);

        private static readonly string[] variableNames =
            ["Payload.CurrencyAmount", "Payload.SettlementAmount", "Payload.AmountGBP", "Payload.AmountEURRate"];

        private static readonly int[] processingTypeIds = [1, 1, 1, 1];

        private static readonly Dictionary<string, int> permissionNumbers = new()
        {
            [AdvancedAnalyticsPermissions.Analytics] = AdvancedAnalyticsPermissionSpecifications.Analytics,
            [AdvancedAnalyticsPermissions.Training] = AdvancedAnalyticsPermissionSpecifications.Training,
            [AdvancedAnalyticsPermissions.Recall] = AdvancedAnalyticsPermissionSpecifications.Recall,
            [AdvancedAnalyticsPermissions.PromoteModel] = AdvancedAnalyticsPermissionSpecifications.PromoteModel
        };

        private static readonly string[] walkthroughToolGroups =
            ["LogisticRegression", "LogisticRegressionTrainingRuns"];

        private int candidateVersionId;
        private int goodRunId;
        private int goodVersionId;
        private int instanceId;
        private int junkVersionId;
        private double scoreOfGoodVersion;
        private int trainedModelId;

        private async Task<LogisticRegressionTrainingRunTools> ToolsAsync(string user)
        {
            var service = await TrainingServiceAsync(user);

            return new LogisticRegressionTrainingRunTools(new AdvancedAnalyticsTrainingRunsGateway(service));
        }

        [Fact]
        public async Task TheLlmTrainsJudgesPromotesComparesAndRollsBackAModelThroughTheServiceLayerAsync()
        {
            await ResetQueueAsync();

            await using var scaffold = await engineFixture.CreateIsolatedAsync(
                new ModelScaffoldOptions { TenantRegistryId = TenantA, ModelGuid = null });

            var instances = await InstanceServiceAsync(Permitted);
            var models = await ModelServiceAsync(Permitted);
            var tools = await ToolsAsync(Permitted);
            using var shutdown = new CancellationTokenSource();
            var worker = NewWorker();
            var workerLoop = Task.CompletedTask;

            try
            {
                DiscoverTheToolsIAmGiven();
                await CreateAnInstanceAsync(instances);

                workerLoop = worker.StartAsync(shutdown.Token);

                await SubmitTheGoodTrialAsync(tools);
                await WaitUntilTheRunHasFinishedAsync(tools, goodRunId, "Succeeded");
                await JudgeTheGoodTrialAsync(tools, models, goodRunId);
                await RefuseAJunkTrialAsync(tools, models);
                await PromoteAllocateAndRecallAsync(instances, models, scaffold);
                await ComparePromoteAndRollBackAsync(tools, models, scaffold);
            }
            finally
            {
                await shutdown.CancelAsync();
                await workerLoop.WaitAsync(TimeSpan.FromSeconds(30));
            }

            await CancelAPendingRunAsync(tools);
            await CheckThePermissionsAsync(tools);
            await DeleteTheInstanceAsync(instances);
        }

        private void DiscoverTheToolsIAmGiven()
        {
            var listing = WhatTheLlmSees();

            listing.Select(s => (s.Name, s.Permission)).Should().BeEquivalentTo([
                ("TrainLogisticRegression", AdvancedAnalyticsPermissionSpecifications.Training),
                ("RecallLogisticRegression", 46),
                ("AssessPredictorSignificance", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("SubmitLogisticRegressionTraining", AdvancedAnalyticsPermissionSpecifications.Training),
                ("GetTrainingRunStatus", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("CancelTrainingRun", AdvancedAnalyticsPermissionSpecifications.Training),
                ("GetTrainingRunResult", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsInstanceGet", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsInstanceGetByGuid", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsInstanceList", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsInstanceCreate", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsInstanceUpdate", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsInstanceDelete", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsInstanceAllocateToModel", 47),
                ("AdvancedAnalyticsInstanceDeallocate", 47),
                ("AdvancedAnalyticsInstanceValidate", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsInstanceFilterFields", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsInstanceFilter", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsInstanceCount", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsModelList", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsModelGet", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsModelListVersions", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsModelGetVersion", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsModelCompareVersions", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsModelGetPromotionEvidence", AdvancedAnalyticsPermissionSpecifications.Analytics),
                ("AdvancedAnalyticsModelPromoteVersion", 47),
                ("AdvancedAnalyticsModelRollback", 47)
            ]);

            listing.Should().OnlyContain(s => s.Description.Length > 40);

            listing.Single(s => s.Name == "AdvancedAnalyticsModelPromoteVersion").Description
                .Should().Contain("no quality gate");
            listing.Single(s => s.Name == "SubmitLogisticRegressionTraining").Description
                .Should().Contain("never makes the resulting version live");

            listing.Select(s => s.Name).Should().NotContain(n => n.Contains("Upload") || n.Contains("Demo"));
        }

        private async Task CreateAnInstanceAsync(AdvancedAnalyticsInstanceService instances)
        {
            var created = await instances.InsertAsync(new AdvancedAnalyticsInstanceDto { Name = UniqueName("Walk") });

            created.Id.Should().BeGreaterThan(0);
            created.EntityAnalysisModelId.Should().BeNull();
            created.Active.Should().BeFalse();
            instanceId = created.Id;
            TrackInstance(instanceId);
        }

        private async Task SubmitTheGoodTrialAsync(LogisticRegressionTrainingRunTools tools)
        {
            var data = FraudLikeRows(1500, 2024, true);

            var submission = await tools.SubmitLogisticRegressionTrainingAsync(instanceId, ModelName,
                variableNames, processingTypeIds, data.Inputs, data.Labels, seed: 7);

            submission.Status.Should().Be("Pending");
            submission.HowToPoll.Should().Contain("run status").And.Contain("run result");
            goodRunId = submission.RunId;
        }

        private async Task WaitUntilTheRunHasFinishedAsync(LogisticRegressionTrainingRunTools tools, int runId,
            string expectedFinalStatus)
        {
            var deadline = DateTime.UtcNow + giveUpAfter;
            TrainingRunStatusView status;

            do
            {
                status = await tools.GetTrainingRunStatusAsync(runId);

                if (status.Finished)
                {
                    break;
                }

                await Task.Delay(pollEvery);
            } while (DateTime.UtcNow < deadline);

            status.Finished.Should().BeTrue("the run must finish inside the bounded wait");
            status.Status.Should().Be(expectedFinalStatus, status.Error);
        }

        private async Task JudgeTheGoodTrialAsync(LogisticRegressionTrainingRunTools tools,
            AdvancedAnalyticsModelService models, int runId)
        {
            var result = await tools.GetTrainingRunResultAsync(runId);

            result.Activated.Should().BeFalse("training never makes a version live");
            result.TrainedModelVersionId.Should().NotBeNull();
            result.Evidence!.Variables.Should().Equal(variableNames);
            trainedModelId = result.TrainedModelId!.Value;
            goodVersionId = result.TrainedModelVersionId!.Value;

            var model = await models.GetModelAsync(trainedModelId);
            model!.ActiveVersionId.Should().BeNull();

            var evidence = (AdvancedAnalyticsPromotionEvidenceDto)await models.GetPromotionEvidenceAsync(
                goodVersionId);

            var decision = Decide(evidence);

            decision.Reasons.Should().BeEmpty();
            decision.Promote.Should().BeTrue();
            evidence.TestMetrics!.RocAuc.Should().BeGreaterThan(MinimumTestAuc);
            evidence.InstanceAllocated.Should().BeFalse();
        }

        private async Task RefuseAJunkTrialAsync(LogisticRegressionTrainingRunTools tools,
            AdvancedAnalyticsModelService models)
        {
            var activeBefore = (await models.GetModelAsync(trainedModelId))!.ActiveVersionId;
            var data = FraudLikeRows(1500, 2024, false);

            var submission = await tools.SubmitLogisticRegressionTrainingAsync(instanceId, ModelName,
                variableNames, processingTypeIds, data.Inputs, data.Labels, seed: 7);

            await WaitUntilTheRunHasFinishedAsync(tools, submission.RunId, "Succeeded");

            var result = await tools.GetTrainingRunResultAsync(submission.RunId);
            junkVersionId = result.TrainedModelVersionId!.Value;

            var evidence = (AdvancedAnalyticsPromotionEvidenceDto)await models.GetPromotionEvidenceAsync(
                junkVersionId);

            var decision = Decide(evidence);

            decision.Promote.Should().BeFalse();
            decision.Reasons.Should().Contain(r => r.StartsWith("held-out AUC"));
            (await models.GetModelAsync(trainedModelId))!.ActiveVersionId.Should().Be(activeBefore);
        }

        private async Task PromoteAllocateAndRecallAsync(AdvancedAnalyticsInstanceService instances,
            AdvancedAnalyticsModelService models, ModelScaffold scaffold)
        {
            var allocated = await instances.AllocateToModelAsync(instanceId, scaffold.ModelId);
            allocated.EntityAnalysisModelId.Should().Be(scaffold.ModelId);

            var evidence = (AdvancedAnalyticsPromotionEvidenceDto)await models.GetPromotionEvidenceAsync(
                goodVersionId);
            evidence.InstanceAllocated.Should().BeTrue();
            Decide(evidence).Promote.Should().BeTrue();

            var promotion = await models.PromoteVersionAsync(trainedModelId, goodVersionId);
            promotion.Changed.Should().BeTrue();
            promotion.ActiveVersionId.Should().Be(goodVersionId);
            promotion.PreviousActiveVersionId.Should().BeNull();
            ((AdvancedAnalyticsPromotionEvidenceDto)promotion.Evidence).TestMetrics.Should().NotBeNull();

            await GoLiveAsync(instances);
            await ModelSync.SyncAsync(engineFixture.Engine, scaffold);

            engineFixture.Engine.FindActiveModel(scaffold.ModelGuid)!.Snapshot.AdvancedAnalyticsModels
                .Should().ContainSingle(m => m.Name == ModelName);

            var scores = new List<double>();

            foreach (var amount in new[] { 40.0, 80.0, 120.0, 160.0, 200.0 })
            {
                scores.Add(await ScoreAProbeAsync(scaffold, amount));
            }

            scores.Should().OnlyContain(s => s > 0 && s < 1);
            scores.Should().BeInAscendingOrder();
            scores.Distinct().Should().HaveCount(scores.Count);
            scores.Last().Should().BeGreaterThan(scores.First() + 0.2);
            scoreOfGoodVersion = await ScoreAProbeAsync(scaffold, 160);
        }

        private async Task ComparePromoteAndRollBackAsync(LogisticRegressionTrainingRunTools tools,
            AdvancedAnalyticsModelService models, ModelScaffold scaffold)
        {
            var data = FraudLikeRows(1500, 2024, true);

            var submission = await tools.SubmitLogisticRegressionTrainingAsync(instanceId, ModelName,
                variableNames, processingTypeIds, data.Inputs, data.Labels, seed: 7, ridgeLambda: 0.5);

            await WaitUntilTheRunHasFinishedAsync(tools, submission.RunId, "Succeeded");
            candidateVersionId = (await tools.GetTrainingRunResultAsync(submission.RunId)).TrainedModelVersionId!.Value;

            var comparison = await models.CompareVersionsAsync(goodVersionId, candidateVersionId);

            var candidateEvidence = (AdvancedAnalyticsPromotionEvidenceDto)await models.GetPromotionEvidenceAsync(
                candidateVersionId);

            comparison.SameSeed.Should().BeTrue();
            comparison.SameSplitSizes.Should().BeTrue();
            comparison.SameVariables.Should().BeTrue();
            ShouldReplaceTheActiveVersion(comparison, candidateEvidence).Should().BeTrue();

            var promotion = await models.PromoteVersionAsync(trainedModelId, candidateVersionId);
            promotion.Changed.Should().BeTrue();
            promotion.PreviousActiveVersionId.Should().Be(goodVersionId);
            (await models.GetModelAsync(trainedModelId))!.ActiveVersionId.Should().Be(candidateVersionId);

            var rolledBack = await models.RollbackAsync(trainedModelId, goodVersionId);
            rolledBack.ActiveVersionId.Should().Be(goodVersionId);
            rolledBack.PreviousActiveVersionId.Should().Be(candidateVersionId);
            (await models.GetModelAsync(trainedModelId))!.ActiveVersionId.Should().Be(goodVersionId);

            ((AdvancedAnalyticsPromotionEvidenceDto)await models.GetPromotionEvidenceAsync(goodVersionId))
                .IsActiveVersion.Should().BeTrue();
            ((AdvancedAnalyticsPromotionEvidenceDto)await models.GetPromotionEvidenceAsync(candidateVersionId))
                .IsActiveVersion.Should().BeFalse();

            await ModelSync.SyncAsync(engineFixture.Engine, scaffold);
            (await ScoreAProbeAsync(scaffold, 160)).Should().BeApproximately(scoreOfGoodVersion, 1e-9);
        }

        private async Task CancelAPendingRunAsync(LogisticRegressionTrainingRunTools tools)
        {
            var data = FraudLikeRows(200, 5, true);

            var submission = await tools.SubmitLogisticRegressionTrainingAsync(instanceId, ModelName,
                variableNames, processingTypeIds, data.Inputs, data.Labels);

            (await tools.GetTrainingRunStatusAsync(submission.RunId)).Status.Should().Be("Pending");

            var cancelled = await tools.CancelTrainingRunAsync(submission.RunId);

            cancelled.Status.Should().Be("Cancelled");
            cancelled.Finished.Should().BeTrue();
            (await tools.GetTrainingRunResultAsync(submission.RunId)).TrainedModelVersionId.Should().BeNull();
        }

        private async Task CheckThePermissionsAsync(LogisticRegressionTrainingRunTools tools)
        {
            var data = FraudLikeRows(200, 5, true);

            var canOnlyRead =
                await ToolsAsync(await UserWithSpecsAsync(AdvancedAnalyticsPermissionSpecifications.Analytics));

            var withoutTraining = await ToolsAsync(await UserWithSpecsAsync(
                AdvancedAnalyticsPermissionSpecifications.Analytics,
                AdvancedAnalyticsPermissionSpecifications.PromoteModel));

            var withoutAnalytics = await ToolsAsync(await UserWithSpecsAsync(45, 47));

            var withoutPromote = await ModelServiceAsync(await UserWithSpecsAsync(
                AdvancedAnalyticsPermissionSpecifications.Analytics,
                AdvancedAnalyticsPermissionSpecifications.Training));

            var stranger = await ToolsAsync(OtherTenant);
            var strangerModels = await ModelServiceAsync(OtherTenant);
            var strangerInstances = await InstanceServiceAsync(OtherTenant);

            await FluentActions.Awaiting(() => withoutTraining.SubmitLogisticRegressionTrainingAsync(instanceId,
                    ModelName, variableNames, processingTypeIds, data.Inputs, data.Labels))
                .Should().ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => canOnlyRead.CancelTrainingRunAsync(goodRunId))
                .Should().ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => withoutPromote.PromoteVersionAsync(trainedModelId, goodVersionId))
                .Should().ThrowAsync<ForbiddenException>();
            await FluentActions.Awaiting(() => withoutPromote.RollbackAsync(trainedModelId, goodVersionId))
                .Should().ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => withoutAnalytics.GetTrainingRunStatusAsync(goodRunId))
                .Should().ThrowAsync<ForbiddenException>();
            await FluentActions.Awaiting(() => withoutAnalytics.GetTrainingRunResultAsync(goodRunId))
                .Should().ThrowAsync<ForbiddenException>();

            (await canOnlyRead.GetTrainingRunStatusAsync(goodRunId)).Status.Should().Be("Succeeded");
            (await tools.GetTrainingRunStatusAsync(goodRunId)).Status.Should().Be("Succeeded");

            await FluentActions.Awaiting(() => stranger.GetTrainingRunStatusAsync(goodRunId))
                .Should().ThrowAsync<NotFoundException>();
            await FluentActions.Awaiting(() => stranger.GetTrainingRunResultAsync(goodRunId))
                .Should().ThrowAsync<NotFoundException>();
            await FluentActions.Awaiting(() => stranger.CancelTrainingRunAsync(goodRunId))
                .Should().ThrowAsync<NotFoundException>();
            await FluentActions.Awaiting(() => strangerModels.GetPromotionEvidenceAsync(goodVersionId))
                .Should().ThrowAsync<NotFoundException>();

            await FluentActions.Awaiting(() => strangerModels.PromoteVersionAsync(trainedModelId, goodVersionId))
                .Should().ThrowAsync<NotFoundException>();

            (await strangerModels.GetModelAsync(trainedModelId)).Should().BeNull();
            (await strangerModels.ListModelsAsync()).Items.Should().NotContain(m => m.Id == trainedModelId);
            (await strangerInstances.GetByIdAsync(instanceId)).Should().BeNull();
            (await strangerInstances.ListAsync()).Items.Should().NotContain(i => i.Id == instanceId);
        }

        private async Task DeleteTheInstanceAsync(AdvancedAnalyticsInstanceService instances)
        {
            (await instances.ListAsync()).Items.Should().Contain(i => i.Id == instanceId);

            await instances.DeleteAsync(instanceId);

            (await instances.ListAsync()).Items.Should().NotContain(i => i.Id == instanceId);
            (await instances.GetByIdAsync(instanceId)).Should().BeNull();
        }

        private async Task GoLiveAsync(AdvancedAnalyticsInstanceService instances)
        {
            var instance = (await instances.GetByIdAsync(instanceId))!;
            instance.Active = true;
            instance.ResponsePayload = true;
            var live = await instances.UpdateAsync(instance);
            live.Active.Should().BeTrue();
        }

        private async Task<double> ScoreAProbeAsync(ModelScaffold scaffold, double currencyAmount)
        {
            var payload = Payloads.Example()
                .With("TxnId", $"walk-{Guid.NewGuid():N}")
                .With("AccountId", $"walk-{Guid.NewGuid():N}")
                .With("CurrencyAmount",
                    currencyAmount.ToString("F2", CultureInfo.InvariantCulture))
                .With("SettlementAmount", "900")
                .With("AmountGBP", "90")
                .With("AmountEURRate", "1.0");

            var response = await engineFixture.Api.InvokeAsync(scaffold, payload);

            response.Status.Should().Be(200);
            var score = response.Find("AdvancedAnalyticsAdaptation", ModelName);
            score.Should().NotBeNull("the live model must score the probe row: " + response.Body);

            return score!.Value.GetDouble();
        }

        private static Decision Decide(AdvancedAnalyticsPromotionEvidenceDto evidence)
        {
            var reasons = new List<string>();

            var auc = evidence.TestMetrics?.RocAuc;

            if (auc is null || auc < MinimumTestAuc)
            {
                reasons.Add($"held-out AUC {auc?.ToString("F3") ?? "missing"} is below {MinimumTestAuc}");
            }

            var predictors = evidence.Coefficients.Where(c => c.Kind == "Predictor").ToArray();
            var significant = predictors.Count(c => c.PValue is < SignificanceLevel);

            if (significant * 2 < predictors.Length)
            {
                reasons.Add($"only {significant} of {predictors.Length} predictors are significant");
            }

            var error = evidence.TestMetrics?.ExpectedCalibrationError;
            var slope = evidence.TestMetrics?.CalibrationSlope;

            if (error is null || error > MaximumCalibrationError)
            {
                reasons.Add($"calibration error {error?.ToString("F3") ?? "missing"} is too high");
            }

            if (slope is null || slope < MinimumCalibrationSlope || slope > MaximumCalibrationSlope)
            {
                reasons.Add($"calibration slope {slope?.ToString("F3") ?? "missing"} is off");
            }

            if (!evidence.Fit.Converged)
            {
                reasons.Add("the fit did not converge");
            }

            if (evidence.Warnings.Any(w => w.Code is LogisticWarningCodes.PerfectSeparation
                    or LogisticWarningCodes.NonConvergence or LogisticWarningCodes.HugeCoefficients))
            {
                reasons.Add("the trainer warned about separation, convergence or huge coefficients");
            }

            return new Decision(reasons.Count == 0, [.. reasons]);
        }

        private static bool ShouldReplaceTheActiveVersion(AdvancedAnalyticsVersionComparisonDto comparison,
            AdvancedAnalyticsPromotionEvidenceDto candidate)
        {
            if (!comparison.SameSeed || !comparison.SameSplitSizes || !comparison.SameVariables)
            {
                return false;
            }

            var auc = comparison.Metrics.Single(m => m.Name == "TestRocAuc").Difference;
            var logLoss = comparison.Metrics.Single(m => m.Name == "TestLogLoss").Difference;

            return Decide(candidate).Promote
                   && auc >= -AucToleranceForReplacement
                   && logLoss <= LogLossToleranceForReplacement;
        }

        private static (double[][] Inputs, int[] Labels) FraudLikeRows(int rows, int seed, bool informative)
        {
            var random = new Random(seed);
            var inputs = new double[rows][];
            var labels = new int[rows];

            for (var i = 0; i < rows; i++)
            {
                var amount = SliceData.Gaussian(random);
                var settlement = SliceData.Gaussian(random);
                var refund = SliceData.Gaussian(random);
                var rate = SliceData.Gaussian(random);

                inputs[i] =
                [
                    120 + 40 * amount,
                    900 + 300 * settlement,
                    90 + 30 * refund,
                    1 + 0.05 * rate
                ];

                var logit = informative ? -1.0 + 1.2 * amount + 0.8 * settlement - 0.7 * refund : -0.85;
                labels[i] = random.NextDouble() < SliceData.Sigmoid(logit) ? 1 : 0;
            }

            return (inputs, labels);
        }

        private static List<ToolListing> WhatTheLlmSees()
        {
            var listing = AdvancedAnalyticsToolCatalogue
                .Discover(typeof(LogisticRegressionTrainingRunTools).Assembly)
                .Where(d => walkthroughToolGroups.Contains(d.GroupName))
                .Select(d => new ToolListing(d.ToolName!, d.Description!, permissionNumbers[d.Permissions.Min()!]))
                .ToList();

            var operations = new[] { typeof(AdvancedAnalyticsInstanceService), typeof(AdvancedAnalyticsModelService) }
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Where(m => m.GetCustomAttribute<ServiceOperationAttribute>() != null);

            foreach (var method in operations)
            {
                listing.Add(new ToolListing(
                    method.GetCustomAttribute<ServiceOperationAttribute>()!.Name,
                    method.GetCustomAttribute<DescriptionAttribute>()!.Description,
                    permissionNumbers[method.GetCustomAttribute<AdvancedAnalyticsRequiresPermissionAttribute>()!
                        .Permission]));
            }

            return listing;
        }

        private sealed record Decision(bool Promote, string[] Reasons);

        private sealed record ToolListing(string Name, string Description, int Permission);
    }
}
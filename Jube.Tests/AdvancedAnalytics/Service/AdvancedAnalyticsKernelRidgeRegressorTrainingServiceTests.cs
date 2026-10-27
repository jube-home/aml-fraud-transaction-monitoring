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
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Reactivity;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Test.AdvancedAnalytics.Slice;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using log4net;
using Xunit;
using Jube.Service.Repository.AskJoobyAnalyticsModel.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsKernelRidgeRegressorTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        private static readonly string[] variableNames = ["Payload.CurrencyAmount"];
        private static readonly int[] processingTypeIds = [1];

        private static (double[][] Inputs, double[] Outputs) Sinusoid(int n, int seed, double noise = 0.05)
        {
            var random = new Random(seed);
            var inputs = new double[n][];
            var outputs = new double[n];

            for (var i = 0; i < n; i++)
            {
                var x = -3d + 6d * random.NextDouble();
                inputs[i] = [x];
                outputs[i] = Math.Sin(x) + noise * SliceData.Gaussian(random);
            }

            return (inputs, outputs);
        }

        [Fact]
        [Trait("Covers", "KernelRidgeRegressorTrainingRunTools.SubmitKernelRidgeRegressorTrainingAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.GetAdvancedAnalyticsTrainingRunStatusAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.GetAdvancedAnalyticsTrainingRunResultAsync")]
        public async Task SubmitTrainsAndRecallsAKernelRidgeRegressorThroughTheSharedToolsAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var (inputs, outputs) = Sinusoid(300, 71);

            var service = await AdvancedAnalyticsKernelRidgeRegressorTrainingService.CreateAsync(Db,
                Permitted, TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var tools = new KernelRidgeRegressorTrainingRunTools(service);

            var submission = await tools.SubmitKernelRidgeRegressorTrainingAsync(instance.Id,
                UniqueName("KernelRidgeModel"), variableNames, processingTypeIds, inputs, outputs, 2.0,
                seed: 71);

            submission.Status.Should().Be(nameof(AdvancedAnalyticsStatus.Pending));

            var pendingRun = await Db.AdvancedAnalyticsTrainingRun.FirstAsync(w => w.Id == submission.RunId);
            pendingRun.StatusId.Should().Be(AdvancedAnalyticsStatus.Pending);
            pendingRun.KindId.Should().Be(AdvancedAnalyticsKind.Regressor);
            pendingRun.JobMode.Should().Be(AdvancedAnalyticsJobMode.Batch);
            pendingRun.ModelKindId.Should().Be(AdvancedAnalyticsModelKind.KernelRidgeRegressorPipeline);

            var arguments = KernelRidgeRegressorTrainingArguments.FromJson(pendingRun.Arguments);
            arguments.Inputs.Should().NotBeNull();
            arguments.Inputs!.Length.Should().Be(inputs.Length);
            arguments.Outputs.Should().NotBeNull();

            var executor = new AdvancedAnalyticsKernelRidgeRegressorTrainingExecutor(Db, TenantA, Permitted,
                LogManager.GetLogger("Test"));

            var executed = await executor.ExecuteAsync(pendingRun, arguments, default);

            executed.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, executed.Error);
            executed.Version.Should().NotBeNull();
            executed.Version!.ModelJson.Should().Contain(KernelRidgeRegressorRecall.ModelKind);
            executed.Version.MetricsJson.Should().NotBeNullOrWhiteSpace();

            var sharedService = await AdvancedAnalyticsSharedTrainingRunService.CreateAsync(Db, Permitted,
                TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var sharedTools = new AdvancedAnalyticsTrainingRunTools(sharedService);

            var status = await sharedTools.GetAdvancedAnalyticsTrainingRunStatusAsync(submission.RunId);
            status.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded);
            status.Finished.Should().BeTrue();
            status.TrainedModelVersionId.Should().Be(executed.Version.Id);

            var result = await sharedTools.GetAdvancedAnalyticsTrainingRunResultAsync(submission.RunId);
            result.Activated.Should().BeFalse();
            result.TrainedModelId.Should().Be(executed.Model!.Id);
            result.TrainedModelVersionId.Should().Be(executed.Version.Id);

            var evidence =
                AdvancedAnalyticsEvidenceJson
                    .Deserialise<AdvancedAnalyticsEvidence<KernelRidgeRegressorEvidenceDetails>>(
                        executed.Version.MetricsJson);
            evidence.Should().NotBeNull();
            evidence!.Kind.Should().Be(KernelRidgeRegressorRecall.ModelKind);
            evidence.Variables.Should().Equal(variableNames);
            evidence.Details.Kernel.Should().Be("Gaussian");
            evidence.Details.TrainingRowsInExpansion.Should().BeGreaterThan(0);
            evidence.Details.TestMetrics.Should().NotBeNull();
            evidence.Details.TestMetrics!.RSquared.Should().NotBeNull();
            evidence.Details.TestMetrics.RSquared!.Value.Should().BeGreaterThan(0.8);

            var directResult = KernelRidgeRegressorTrainer.Train(inputs, outputs, variableNames,
                arguments.ToOptions());

            var directRecall = KernelRidgeRegressorRecall.FromSnapshot(directResult.SnapshotJson);
            var persistedRecall = KernelRidgeRegressorRecall.FromSnapshot(executed.Version.ModelJson);

            var probe = new[] { 1.2d };
            persistedRecall.Predict(probe)[0].Should().BeApproximately(directRecall.Predict(probe)[0], 1e-9);
            persistedRecall.SupportRowCount.Should().Be(directRecall.SupportRowCount);
        }

        [Fact]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.CancelAdvancedAnalyticsTrainingRunAsync")]
        public async Task CancelViaTheSharedToolCancelsAPendingKernelRidgeRegressorRunAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var (inputs, outputs) = Sinusoid(30, 555);

            var service = await AdvancedAnalyticsKernelRidgeRegressorTrainingService.CreateAsync(Db,
                Permitted, TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var tools = new KernelRidgeRegressorTrainingRunTools(service);

            var submission = await tools.SubmitKernelRidgeRegressorTrainingAsync(instance.Id,
                UniqueName("KernelRidgeCancel"), variableNames, processingTypeIds, inputs, outputs);

            var sharedService = await AdvancedAnalyticsSharedTrainingRunService.CreateAsync(Db, Permitted,
                TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var sharedTools = new AdvancedAnalyticsTrainingRunTools(sharedService);

            var cancelled = await sharedTools.CancelAdvancedAnalyticsTrainingRunAsync(submission.RunId);

            cancelled.Status.Should().Be(AdvancedAnalyticsStatus.Cancelled);
            cancelled.Finished.Should().BeTrue();

            var cancelledResult = await sharedTools.GetAdvancedAnalyticsTrainingRunResultAsync(submission.RunId);
            cancelledResult.TrainedModelVersionId.Should().BeNull();
            cancelledResult.Evidence.Should().BeNull();
        }
    }
}
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
using Jube.Engine.AdvancedAnalytics.Classification;
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
using Jube.Service.Repository.AdvancedAnalyticsModel.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsKernelSupportVectorClassifierTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        private static readonly string[] variableNames = ["Payload.CurrencyAmount", "Payload.SettlementAmount"];
        private static readonly int[] processingTypeIds = [1, 1];

        private static (double[][] Inputs, int[] Labels) XorClusters(int perCluster, int seed, double centre = 6d,
            double noise = 0.6)
        {
            var random = new Random(seed);

            var centres = new (double X, double Y, int Label)[]
            {
                (centre, centre, 0),
                (-centre, -centre, 0),
                (centre, -centre, 1),
                (-centre, centre, 1)
            };

            var inputs = new double[perCluster * 4][];
            var labels = new int[perCluster * 4];
            var index = 0;

            foreach (var (x, y, label) in centres)
            {
                for (var i = 0; i < perCluster; i++)
                {
                    inputs[index] = [x + noise * SliceData.Gaussian(random), y + noise * SliceData.Gaussian(random)];
                    labels[index] = label;
                    index++;
                }
            }

            return (inputs, labels);
        }

        [Fact]
        [Trait("Covers",
            "KernelSupportVectorClassifierTrainingRunTools.SubmitKernelSupportVectorClassifierTrainingAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.GetAdvancedAnalyticsTrainingRunStatusAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.GetAdvancedAnalyticsTrainingRunResultAsync")]
        public async Task SubmitTrainsAndRecallsAKernelSupportVectorClassifierThroughTheSharedToolsAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var (inputs, labels) = XorClusters(150, 71);

            var service = await AdvancedAnalyticsKernelSupportVectorClassifierTrainingService.CreateAsync(Db,
                Permitted, TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var tools = new KernelSupportVectorClassifierTrainingRunTools(service);

            var submission = await tools.SubmitKernelSupportVectorClassifierTrainingAsync(instance.Id,
                UniqueName("KernelSvmModel"), variableNames, processingTypeIds, inputs, labels, 2.0,
                seed: 71);

            submission.Status.Should().Be(nameof(AdvancedAnalyticsStatus.Pending));

            var pendingRun = await Db.AdvancedAnalyticsTrainingRun.FirstAsync(w => w.Id == submission.RunId);
            pendingRun.StatusId.Should().Be(AdvancedAnalyticsStatus.Pending);
            pendingRun.KindId.Should().Be(AdvancedAnalyticsKind.Classifier);
            pendingRun.JobMode.Should().Be(AdvancedAnalyticsJobMode.Batch);
            pendingRun.ModelKindId.Should().Be(AdvancedAnalyticsModelKind.KernelSupportVectorClassifierPipeline);

            var arguments = KernelSupportVectorClassifierTrainingArguments.FromJson(pendingRun.Arguments);
            arguments.Inputs.Should().NotBeNull();
            arguments.Inputs!.Length.Should().Be(inputs.Length);
            arguments.Labels.Should().NotBeNull();

            var executor = new AdvancedAnalyticsKernelSupportVectorClassifierTrainingExecutor(Db, TenantA, Permitted,
                LogManager.GetLogger("Test"));

            var executed = await executor.ExecuteAsync(pendingRun, arguments, arguments.Inputs!, arguments.Labels!,
                default);

            executed.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, executed.Error);
            executed.Version.Should().NotBeNull();
            executed.Version!.ModelJson.Should().Contain(KernelSupportVectorClassifierRecall.ModelKind);
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
                    .Deserialise<AdvancedAnalyticsEvidence<KernelSupportVectorEvidenceDetails>>(
                        executed.Version.MetricsJson);
            evidence.Should().NotBeNull();
            evidence!.Kind.Should().Be(KernelSupportVectorClassifierRecall.ModelKind);
            evidence.Variables.Should().Equal(variableNames);
            evidence.Details.Kernel.Should().Be("Gaussian");
            evidence.Details.SupportVectorCount.Should().BeGreaterThan(0);
            evidence.Details.TestMetrics.Should().NotBeNull();
            evidence.Details.TestMetrics!.RocAuc.Should().NotBeNull();
            evidence.Details.TestMetrics.RocAuc!.Value.Should().BeGreaterThan(0.85);

            var directResult = KernelSupportVectorClassifierTrainer.Train(inputs, labels, variableNames,
                arguments.ToOptions());

            var directRecall = KernelSupportVectorClassifierRecall.FromSnapshot(directResult.SnapshotJson);
            var persistedRecall = KernelSupportVectorClassifierRecall.FromSnapshot(executed.Version.ModelJson);

            var probe = new[] { 6d, -6d };
            persistedRecall.Predict(probe)[0].Should().BeApproximately(directRecall.Predict(probe)[0], 1e-9);
            persistedRecall.SupportVectorCount.Should().Be(directRecall.SupportVectorCount);
        }

        [Fact]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.CancelAdvancedAnalyticsTrainingRunAsync")]
        public async Task CancelViaTheSharedToolCancelsAPendingKernelSupportVectorClassifierRunAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var (inputs, labels) = XorClusters(30, 555);

            var service = await AdvancedAnalyticsKernelSupportVectorClassifierTrainingService.CreateAsync(Db,
                Permitted, TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var tools = new KernelSupportVectorClassifierTrainingRunTools(service);

            var submission = await tools.SubmitKernelSupportVectorClassifierTrainingAsync(instance.Id,
                UniqueName("KernelSvmCancel"), variableNames, processingTypeIds, inputs, labels);

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
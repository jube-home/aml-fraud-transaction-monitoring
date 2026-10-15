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
using Jube.Engine.AdvancedAnalytics.Anomaly;
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
using Jube.Engine.AdvancedAnalytics.Anomaly.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        private static readonly string[] variableNames = ["Payload.CurrencyAmount", "Payload.SettlementAmount"];
        private static readonly int[] processingTypeIds = [1, 1];

        private static double[][] NormalCluster(int n, int seed)
        {
            var random = new Random(seed);
            var rows = new double[n][];

            for (var i = 0; i < n; i++)
            {
                rows[i] = [SliceData.Gaussian(random), SliceData.Gaussian(random)];
            }

            return rows;
        }

        [Fact]
        [Trait("Covers",
            "OneClassSupportVectorAnomalyTrainingRunTools.SubmitOneClassSupportVectorAnomalyTrainingAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.GetAdvancedAnalyticsTrainingRunStatusAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.GetAdvancedAnalyticsTrainingRunResultAsync")]
        public async Task SubmitTrainsAndRecallsAOneClassSupportVectorAnomalyModelThroughTheSharedToolsAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var inputs = NormalCluster(400, 21);

            var service = await AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingService.CreateAsync(Db,
                Permitted, TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var tools = new OneClassSupportVectorAnomalyTrainingRunTools(service);

            var submission = await tools.SubmitOneClassSupportVectorAnomalyTrainingAsync(instance.Id,
                UniqueName("OneClassSupportVectorAnomalyModel"), variableNames, processingTypeIds, inputs,
                nu: 0.1, seed: 21);

            submission.Status.Should().Be(nameof(AdvancedAnalyticsStatus.Pending));

            var pendingRun = await Db.AdvancedAnalyticsTrainingRun.FirstAsync(w => w.Id == submission.RunId);
            pendingRun.StatusId.Should().Be(AdvancedAnalyticsStatus.Pending);
            pendingRun.KindId.Should().Be(AdvancedAnalyticsKind.AnomalyDetector);
            pendingRun.JobMode.Should().Be(AdvancedAnalyticsJobMode.Batch);
            pendingRun.ModelKindId.Should().Be(AdvancedAnalyticsModelKind.OneClassSupportVectorAnomalyPipeline);

            var arguments = OneClassSupportVectorAnomalyTrainingArguments.FromJson(pendingRun.Arguments);
            arguments.Inputs.Should().NotBeNull();
            arguments.Inputs!.Length.Should().Be(inputs.Length);
            arguments.Labels.Should().BeNull();

            var executor = new AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingExecutor(Db, TenantA, Permitted,
                LogManager.GetLogger("Test"));

            var executed = await executor.ExecuteAsync(pendingRun, arguments, arguments.Inputs!, arguments.Labels,
                default);

            executed.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, executed.Error);
            executed.Version.Should().NotBeNull();
            executed.Version!.ModelJson.Should().Contain(OneClassSupportVectorAnomalyRecall.ModelKind);
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
                    .Deserialise<AdvancedAnalyticsEvidence<OneClassSupportVectorEvidenceDetails>>(
                        executed.Version.MetricsJson);
            evidence.Should().NotBeNull();
            evidence!.Kind.Should().Be(OneClassSupportVectorAnomalyRecall.ModelKind);
            evidence.Variables.Should().Equal(variableNames);
            evidence.Details.SupportVectorCount.Should().BeGreaterThan(0);
            evidence.Details.Calibration.Applied.Should().BeFalse();

            var directResult = OneClassSupportVectorAnomalyTrainer.Train(inputs, null, variableNames,
                arguments.ToOptions());

            var directRecall = OneClassSupportVectorAnomalyRecall.FromSnapshot(directResult.SnapshotJson);
            var persistedRecall = OneClassSupportVectorAnomalyRecall.FromSnapshot(executed.Version.ModelJson);

            var probe = new[] { 20d, 20d };
            persistedRecall.Predict(probe)[0].Should().BeApproximately(directRecall.Predict(probe)[0], 1e-9);
            persistedRecall.SupportVectorCount.Should().Be(directRecall.SupportVectorCount);
        }

        [Fact]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.CancelAdvancedAnalyticsTrainingRunAsync")]
        public async Task CancelViaTheSharedToolCancelsAPendingOneClassSupportVectorAnomalyRunAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var inputs = NormalCluster(100, 555);

            var service = await AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingService.CreateAsync(Db,
                Permitted, TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var tools = new OneClassSupportVectorAnomalyTrainingRunTools(service);

            var submission = await tools.SubmitOneClassSupportVectorAnomalyTrainingAsync(instance.Id,
                UniqueName("OneClassSupportVectorAnomalyCancel"), variableNames, processingTypeIds, inputs);

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
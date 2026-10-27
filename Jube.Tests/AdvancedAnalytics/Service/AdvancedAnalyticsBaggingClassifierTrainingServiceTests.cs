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
using Jube.Service.Repository.AskJoobyAnalyticsModel.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsBaggingClassifierTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        [Fact]
        [Trait("Covers", "BaggingClassifierTrainingRunTools.SubmitBaggingClassifierTrainingAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.GetAdvancedAnalyticsTrainingRunStatusAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.GetAdvancedAnalyticsTrainingRunResultAsync")]
        public async Task SubmitTrainsThroughTheRealExecutorAndTheRecalledVersionMatchesDirectRecallAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var data = SliceData.Logistic(900, 71, 0d, [4.0, 0.05], 1);

            var baggingService = await AdvancedAnalyticsBaggingClassifierTrainingService.CreateAsync(Db, Permitted,
                TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var baggingTools = new BaggingClassifierTrainingRunTools(baggingService);

            var sharedService = await AdvancedAnalyticsSharedTrainingRunService.CreateAsync(Db, Permitted,
                TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var sharedTools = new AdvancedAnalyticsTrainingRunTools(sharedService);

            var submission = await baggingTools.SubmitBaggingClassifierTrainingAsync(instance.Id,
                UniqueName("Bagging"), data.Names, [1, 1, 1], data.Inputs, data.Labels,
                25, 4, seed: 71);

            var pendingRun = await Db.AdvancedAnalyticsTrainingRun.SingleAsync(w => w.Id == submission.RunId);
            pendingRun.KindId.Should().Be(AdvancedAnalyticsKind.Classifier);
            pendingRun.ModelKindId.Should().Be(AdvancedAnalyticsModelKind.BaggingClassifierPipeline);
            pendingRun.JobMode.Should().Be(AdvancedAnalyticsJobMode.Batch);

            var arguments = BaggingClassifierTrainingArguments.FromJson(pendingRun.Arguments);
            arguments.Inputs.Should().NotBeNull();
            arguments.Labels.Should().NotBeNull();

            var executor = new AdvancedAnalyticsBaggingClassifierTrainingExecutor(Db, TenantA, Permitted,
                LogManager.GetLogger("Test"));

            var executed = await executor.ExecuteAsync(pendingRun, arguments,
                _ => Task.FromResult((arguments.Inputs!, arguments.Labels!)), default);

            executed.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, executed.Error);
            executed.Version.Should().NotBeNull();

            var status = await sharedTools.GetAdvancedAnalyticsTrainingRunStatusAsync(submission.RunId);
            status.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, status.Error);
            status.Finished.Should().BeTrue();
            status.TrainedModelVersionId.Should().Be(executed.Version!.Id);

            var result = await sharedTools.GetAdvancedAnalyticsTrainingRunResultAsync(submission.RunId);
            result.Activated.Should().BeFalse();
            result.TrainedModelId.Should().NotBeNull();
            result.TrainedModelVersionId.Should().Be(status.TrainedModelVersionId);

            var version = await Db.AskJoobyAnalyticsTrainedModelVersion
                .Where(w => w.Id == status.TrainedModelVersionId)
                .SingleAsync();

            var evidence =
                AdvancedAnalyticsEvidenceJson.Deserialise<AdvancedAnalyticsEvidence<BaggingEvidenceDetails>>(
                    version.MetricsJson);

            evidence!.Kind.Should().Be(BaggingClassifierRecall.ModelKind);
            evidence.Details.TestMetrics.Should().NotBeNull();

            var recalledFromDb = BaggingClassifierRecall.FromSnapshot(version.ModelJson);

            var directResult = BaggingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new BaggingClassifierOptions { TreeCount = 25, MaxDepth = 4, Seed = 71 });

            var directRecall = BaggingClassifierRecall.FromSnapshot(directResult.SnapshotJson);

            foreach (var row in data.Inputs.Take(25))
            {
                recalledFromDb.Predict(row)[0].Should().Be(directRecall.Predict(row)[0]);
            }
        }

        [Fact]
        [Trait("Covers", "BaggingClassifierTrainingRunTools.SubmitBaggingClassifierTrainingAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.CancelAdvancedAnalyticsTrainingRunAsync")]
        public async Task CancelToolCancelsAPendingBaggingRunAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var data = SliceData.Logistic(200, 5, 0d, [1.0]);

            var baggingService = await AdvancedAnalyticsBaggingClassifierTrainingService.CreateAsync(Db, Permitted,
                TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var baggingTools = new BaggingClassifierTrainingRunTools(baggingService);

            var sharedService = await AdvancedAnalyticsSharedTrainingRunService.CreateAsync(Db, Permitted,
                TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var sharedTools = new AdvancedAnalyticsTrainingRunTools(sharedService);

            var submission = await baggingTools.SubmitBaggingClassifierTrainingAsync(instance.Id,
                UniqueName("BaggingCancel"), data.Names, [1], data.Inputs, data.Labels);

            var cancelled = await sharedTools.CancelAdvancedAnalyticsTrainingRunAsync(submission.RunId);
            cancelled.Status.Should().Be(AdvancedAnalyticsStatus.Cancelled);
            cancelled.Finished.Should().BeTrue();

            var cancelledResult = await sharedTools.GetAdvancedAnalyticsTrainingRunResultAsync(submission.RunId);
            cancelledResult.TrainedModelVersionId.Should().BeNull();
            cancelledResult.Evidence.Should().BeNull();
        }
    }
}
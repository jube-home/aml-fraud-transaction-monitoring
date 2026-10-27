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
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Reactivity;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Test.AdvancedAnalytics.Slice;
using Jube.Test.AdvancedAnalytics.Worker;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;
using Jube.Service.Repository.AskJoobyAnalyticsModel.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsRandomForestClassifierSharedTrainingRunServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsWorkerTestBase(fx)
    {
        [Fact]
        [Trait("Covers", "RandomForestClassifierTrainingRunTools.SubmitRandomForestClassifierTrainingAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.GetAdvancedAnalyticsTrainingRunStatusAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.CancelAdvancedAnalyticsTrainingRunAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.GetAdvancedAnalyticsTrainingRunResultAsync")]
        public async Task TheSharedToolsReadAndCancelARunSubmittedThroughTheRandomForestSubmitToolAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var data = SliceData.Logistic(500, 71, 0d, [3.2, -3.2]);
            var processingTypeIds = data.Names.Select(_ => 1).ToArray();

            var forestService = await AdvancedAnalyticsRandomForestClassifierTrainingService.CreateAsync(Db,
                Permitted, TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var forestTools = new RandomForestClassifierTrainingRunTools(forestService);

            var sharedService = await AdvancedAnalyticsSharedTrainingRunService.CreateAsync(Db, Permitted,
                TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var sharedTools = new AdvancedAnalyticsTrainingRunTools(sharedService);

            var submission = await forestTools.SubmitRandomForestClassifierTrainingAsync(instance.Id,
                UniqueName("SharedToolsRandomForest"), data.Names, processingTypeIds, data.Inputs, data.Labels,
                20, 4, seed: 71);

            var worker = NewWorker();
            await worker.RunPassAsync();
            await worker.WaitForIdleAsync();

            var status = await sharedTools.GetAdvancedAnalyticsTrainingRunStatusAsync(submission.RunId);
            status.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, status.Error);
            status.Finished.Should().BeTrue();
            status.TrainedModelVersionId.Should().NotBeNull();

            var result = await sharedTools.GetAdvancedAnalyticsTrainingRunResultAsync(submission.RunId);
            result.Activated.Should().BeFalse();
            result.TrainedModelId.Should().NotBeNull();
            result.TrainedModelVersionId.Should().Be(status.TrainedModelVersionId);
            result.Evidence.Should().BeOfType<AdvancedAnalyticsEvidence<RandomForestEvidenceDetails>>();
            var evidence = (AdvancedAnalyticsEvidence<RandomForestEvidenceDetails>)result.Evidence!;
            evidence.Kind.Should().Be(RandomForestClassifierRecall.ModelKind);
            evidence.Details.TreeCount.Should().Be(20);

            var version =
                await Db.AskJoobyAnalyticsTrainedModelVersion.FirstAsync(w => w.Id == status.TrainedModelVersionId);

            var recall = RandomForestClassifierRecall.FromSnapshot(version.ModelJson);

            for (var i = 0; i < 10; i++)
            {
                var direct = recall.Predict(data.Inputs[i]);
                direct.Should().HaveCount(1);
                direct[0].Should().BeInRange(0d, 1d);
            }

            var cancelData = SliceData.Logistic(150, 5, 0d, [1.0]);
            var cancelProcessingTypeIds = cancelData.Names.Select(_ => 1).ToArray();

            var cancelSubmission = await forestTools.SubmitRandomForestClassifierTrainingAsync(instance.Id,
                UniqueName("SharedToolsRandomForestCancel"), cancelData.Names, cancelProcessingTypeIds,
                cancelData.Inputs, cancelData.Labels);

            var cancelled = await sharedTools.CancelAdvancedAnalyticsTrainingRunAsync(cancelSubmission.RunId);
            cancelled.Status.Should().Be(AdvancedAnalyticsStatus.Cancelled);
            cancelled.Finished.Should().BeTrue();

            var cancelledResult =
                await sharedTools.GetAdvancedAnalyticsTrainingRunResultAsync(cancelSubmission.RunId);
            cancelledResult.TrainedModelVersionId.Should().BeNull();
            cancelledResult.Evidence.Should().BeNull();
        }
    }
}
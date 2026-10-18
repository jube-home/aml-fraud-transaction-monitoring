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
using Jube.Service.Repository.AdvancedAnalyticsModel.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class DecisionTreeClassifierTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsWorkerTestBase(fx)
    {
        [Fact]
        [Trait("Covers", "DecisionTreeClassifierTrainingRunTools.SubmitDecisionTreeClassifierTrainingAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.GetAdvancedAnalyticsTrainingRunStatusAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.GetAdvancedAnalyticsTrainingRunResultAsync")]
        public async Task SubmitTrainsThroughTheRealWorkerAndTheRecalledVersionMatchesDirectRecallAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var data = SliceData.Logistic(900, 71, 0d, [4.0, 0.05], 1);

            var treeService = await AdvancedAnalyticsDecisionTreeClassifierTrainingService.CreateAsync(Db, Permitted,
                TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var treeTools = new DecisionTreeClassifierTrainingRunTools(treeService);

            var sharedService = await AdvancedAnalyticsSharedTrainingRunService.CreateAsync(Db, Permitted, TestLog.NoOp,
                Localizers,
                new NullServiceChangeBus(), TestLog.NoOp);

            var sharedTools = new AdvancedAnalyticsTrainingRunTools(sharedService);

            var submission = await treeTools.SubmitDecisionTreeClassifierTrainingAsync(instance.Id,
                UniqueName("DecisionTree"), data.Names, [1, 1, 1], data.Inputs, data.Labels, seed: 71);

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
            result.Evidence.Should().BeOfType<AdvancedAnalyticsEvidence<DecisionTreeEvidenceDetails>>();

            var evidence = (AdvancedAnalyticsEvidence<DecisionTreeEvidenceDetails>)result.Evidence!;
            evidence.Kind.Should().Be(DecisionTreeClassifierRecall.ModelKind);
            evidence.Details.TestMetrics.Should().NotBeNull();

            var version = await Db.AdvancedAnalyticsTrainedModelVersion
                .Where(w => w.Id == status.TrainedModelVersionId)
                .SingleAsync();

            var recalledFromDb = DecisionTreeClassifierRecall.FromSnapshot(version.ModelJson);

            var directResult = DecisionTreeClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new DecisionTreeClassifierOptions { Seed = 71 });

            var directRecall = DecisionTreeClassifierRecall.FromSnapshot(directResult.SnapshotJson);

            foreach (var row in data.Inputs.Take(25))
            {
                recalledFromDb.Predict(row)[0].Should().Be(directRecall.Predict(row)[0]);
            }

            var cancelData = SliceData.Logistic(200, 5, 0d, [1.0]);

            var cancelSubmission = await treeTools.SubmitDecisionTreeClassifierTrainingAsync(instance.Id,
                UniqueName("DecisionTreeCancel"), cancelData.Names, [1], cancelData.Inputs, cancelData.Labels);

            var cancelled = await sharedTools.CancelAdvancedAnalyticsTrainingRunAsync(cancelSubmission.RunId);
            cancelled.Status.Should().Be(AdvancedAnalyticsStatus.Cancelled);
            cancelled.Finished.Should().BeTrue();

            var cancelledResult = await sharedTools.GetAdvancedAnalyticsTrainingRunResultAsync(cancelSubmission.RunId);
            cancelledResult.TrainedModelVersionId.Should().BeNull();
            cancelledResult.Evidence.Should().BeNull();
        }
    }
}
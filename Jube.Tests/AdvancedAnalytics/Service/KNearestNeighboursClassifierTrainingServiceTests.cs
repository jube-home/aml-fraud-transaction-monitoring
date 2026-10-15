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
    public sealed class KNearestNeighboursClassifierTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsWorkerTestBase(fx)
    {
        private static (double[][] Inputs, int[] Labels, string[] Names) TwoWellSeparatedBlobs(int perBlob, int seed)
        {
            var random = new Random(seed);
            var negativeCentre = new[] { -10d, -10d };
            var positiveCentre = new[] { 10d, 10d };
            var inputs = new double[perBlob * 2][];
            var labels = new int[perBlob * 2];
            var index = 0;

            foreach (var (centre, label) in new[] { (negativeCentre, 0), (positiveCentre, 1) })
            {
                for (var i = 0; i < perBlob; i++)
                {
                    inputs[index] =
                    [
                        centre[0] + 0.2 * SliceData.Gaussian(random),
                        centre[1] + 0.2 * SliceData.Gaussian(random)
                    ];

                    labels[index] = label;
                    index++;
                }
            }

            return (inputs, labels, ["x0", "x1"]);
        }

        [Fact]
        [Trait("Covers",
            "KNearestNeighboursClassifierTrainingRunTools.SubmitKNearestNeighboursClassifierTrainingAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.GetAdvancedAnalyticsTrainingRunStatusAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.GetAdvancedAnalyticsTrainingRunResultAsync")]
        public async Task SubmitTrainThenRecallRoundTripsThroughTheSharedToolsAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var (inputs, labels, names) = TwoWellSeparatedBlobs(200, 71);

            var service = await AdvancedAnalyticsKNearestNeighboursClassifierTrainingService.CreateAsync(Db,
                Permitted, TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var tools = new KNearestNeighboursClassifierTrainingRunTools(service);

            var submission = await tools.SubmitKNearestNeighboursClassifierTrainingAsync(instance.Id,
                UniqueName("KnnModel"), names, [1, 1], inputs, labels, 9, seed: 71);

            var worker = NewWorker();
            await worker.RunPassAsync();
            await worker.WaitForIdleAsync();

            var sharedService = await AdvancedAnalyticsSharedTrainingRunService.CreateAsync(Db, Permitted,
                TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var sharedTools = new AdvancedAnalyticsTrainingRunTools(sharedService);

            var status = await sharedTools.GetAdvancedAnalyticsTrainingRunStatusAsync(submission.RunId);
            status.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, status.Error);
            status.Finished.Should().BeTrue();
            status.TrainedModelVersionId.Should().NotBeNull();

            var result = await sharedTools.GetAdvancedAnalyticsTrainingRunResultAsync(submission.RunId);
            result.Activated.Should().BeFalse();
            result.TrainedModelId.Should().NotBeNull();
            result.TrainedModelVersionId.Should().Be(status.TrainedModelVersionId);
            result.Evidence.Should().BeOfType<AdvancedAnalyticsEvidence<KNearestNeighboursEvidenceDetails>>();

            var evidence = (AdvancedAnalyticsEvidence<KNearestNeighboursEvidenceDetails>)result.Evidence!;
            evidence.Kind.Should().Be(KNearestNeighboursClassifierRecall.ModelKind);
            evidence.Details.NeighbourCount.Should().Be(9);
            evidence.Details.StoredTrainingRows.Should().Be(evidence.SplitSizes.Train);

            var version = await Db.AdvancedAnalyticsTrainedModelVersion
                .FirstAsync(v => v.Id == status.TrainedModelVersionId);

            var recall = KNearestNeighboursClassifierRecall.FromSnapshot(version.ModelJson);

            var nearNegative = recall.Predict([-10d, -10d])[0];
            var nearPositive = recall.Predict([10d, 10d])[0];
            nearNegative.Should().BeLessThan(0.3);
            nearPositive.Should().BeGreaterThan(0.7);
        }
    }
}
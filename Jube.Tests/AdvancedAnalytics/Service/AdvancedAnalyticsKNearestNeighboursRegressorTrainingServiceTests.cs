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
using Jube.Data.Repository;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Service.Reactivity;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Test.AdvancedAnalytics.Slice;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;
using Jube.Service.Repository.AdvancedAnalyticsModel.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsKNearestNeighboursRegressorTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        private static (double[][] Inputs, double[] Outputs) TwoWellSeparatedBlobs(int perBlob, int seed)
        {
            var random = new Random(seed);
            var negativeCentre = new[] { -10d, -10d };
            var positiveCentre = new[] { 10d, 10d };
            var inputs = new double[perBlob * 2][];
            var outputs = new double[perBlob * 2];
            var index = 0;

            foreach (var (centre, target) in new[] { (negativeCentre, -100d), (positiveCentre, 100d) })
            {
                for (var i = 0; i < perBlob; i++)
                {
                    inputs[index] =
                    [
                        centre[0] + 0.2 * SliceData.Gaussian(random),
                        centre[1] + 0.2 * SliceData.Gaussian(random)
                    ];

                    outputs[index] = target + 0.5 * SliceData.Gaussian(random);
                    index++;
                }
            }

            return (inputs, outputs);
        }

        private Task<AdvancedAnalyticsKNearestNeighboursRegressorTrainingService> RegressorServiceAsync(string user)
        {
            return AdvancedAnalyticsKNearestNeighboursRegressorTrainingService.CreateAsync(Db, user, TestLog.NoOp,
                Localizers, new NullServiceChangeBus(), TestLog.NoOp);
        }

        [Fact]
        [Trait("Covers", "KNearestNeighboursRegressorTrainingRunTools.SubmitKNearestNeighboursRegressorTrainingAsync")]
        public async Task SubmitTrainExecutesDirectlyAndRecallsBitForBitAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var (inputs, outputs) = TwoWellSeparatedBlobs(200, 71);
            var names = new[] { "x0", "x1" };
            const string modelName = "WalkthroughKnnRegressor";

            var service = await RegressorServiceAsync(Permitted);

            var submission = await service.SubmitTrainingAsync(
                new AdvancedAnalyticsKNearestNeighboursRegressorTrainingDto
                {
                    AdvancedAnalyticsInstanceId = instance.Id,
                    ModelName = modelName,
                    VariableNames = names,
                    ProcessingTypeIds = [1, 1],
                    Inputs = inputs,
                    Outputs = outputs,
                    NeighbourCount = 9,
                    Seed = 71
                });

            submission.Status.Should().Be(AdvancedAnalyticsStatus.Pending);

            var runs = new AdvancedAnalyticsTrainingRunRepository(Db, TenantA, Permitted);
            var run = await runs.GetByIdAsync(submission.RunId);
            run.Should().NotBeNull();
            run!.KindId.Should().Be(AdvancedAnalyticsKind.Regressor);
            run.ModelKindId.Should().Be(AdvancedAnalyticsModelKind.KNearestNeighboursRegressorPipeline);

            await runs.UpdateStatusAsync(submission.RunId, AdvancedAnalyticsStatus.Running);
            var arguments = KNearestNeighboursRegressorTrainingArguments.FromJson(run.Arguments);

            var executor = new AdvancedAnalyticsKNearestNeighboursRegressorTrainingExecutor(Db, TenantA, Permitted,
                TestLog.NoOp);

            var executed = await executor.ExecuteAsync(run, arguments, default);

            executed.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, executed.Error);
            executed.Result.Should().NotBeNull();
            executed.Result!.Details.TestMetrics.Should().NotBeNull();
            executed.Result.Details.NeighbourCount.Should().Be(9);
            executed.CreatedModel.Should().BeTrue();
            executed.Version.Should().NotBeNull();

            var versionRow = await new AdvancedAnalyticsTrainedModelVersionRepository(Db, TenantA, Permitted)
                .GetByRunIdAsync(submission.RunId);
            versionRow.Should().NotBeNull();

            var evidence = AdvancedAnalyticsEvidenceJson
                .Deserialise<AdvancedAnalyticsEvidence<KNearestNeighboursRegressorEvidenceDetails>>(
                    versionRow!.MetricsJson!);

            evidence.Should().NotBeNull();
            evidence!.Kind.Should().Be(KNearestNeighboursRegressorRecall.ModelKind);
            evidence.Details.NeighbourCount.Should().Be(9);
            evidence.Details.StoredTrainingRows.Should().Be(evidence.SplitSizes.Train);
            evidence.Details.TestMetrics.Should().NotBeNull();
            evidence.Details.TestMetrics!.RSquared.Should().NotBeNull();
            evidence.Details.TestMetrics.RSquared!.Value.Should().BeGreaterThan(0.9);

            var recall = KNearestNeighboursRegressorRecall.FromSnapshot(versionRow.ModelJson);

            var nearNegative = recall.Predict([-10d, -10d])[0];
            var nearPositive = recall.Predict([10d, 10d])[0];
            nearNegative.Should().BeLessThan(-90d);
            nearPositive.Should().BeGreaterThan(90d);

            var repeat = KNearestNeighboursRegressorRecall.FromSnapshot(versionRow.ModelJson);
            var probe = inputs[0];
            SliceData.Bits(recall.Predict(probe)[0]).Should().Be(SliceData.Bits(repeat.Predict(probe)[0]));
        }
    }
}
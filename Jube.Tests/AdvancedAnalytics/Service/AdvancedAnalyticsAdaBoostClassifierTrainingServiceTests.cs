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
using Jube.Service.Repository.AskJoobyAnalyticsModel.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsAdaBoostClassifierTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        private static readonly string[] variableNames = ["Payload.CurrencyAmount", "Payload.SettlementAmount"];
        private static readonly int[] processingTypeIds = [1, 1];

        private static (double[][] Inputs, int[] Labels) SeparatedGaussianBlobs(int perClass, int seed)
        {
            var random = new Random(seed);
            var inputs = new double[perClass * 2][];
            var labels = new int[perClass * 2];
            var index = 0;

            for (var c = 0; c < 2; c++)
            {
                var centre = c == 1 ? 8d : -8d;

                for (var i = 0; i < perClass; i++)
                {
                    inputs[index] =
                    [
                        centre + SliceData.Gaussian(random), centre * 0.5 + SliceData.Gaussian(random)
                    ];

                    labels[index] = c;
                    index++;
                }
            }

            return (inputs, labels);
        }

        [Fact]
        [Trait("Covers", "AdaBoostClassifierTrainingRunTools.SubmitAdaBoostClassifierTrainingAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.GetAdvancedAnalyticsTrainingRunStatusAsync")]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.GetAdvancedAnalyticsTrainingRunResultAsync")]
        public async Task SubmitTrainsAndRecallsAnAdaBoostClassifierThroughTheSharedToolsAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var (inputs, labels) = SeparatedGaussianBlobs(300, 2024);

            var service = await AdvancedAnalyticsAdaBoostClassifierTrainingService.CreateAsync(Db, Permitted,
                TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var tools = new AdaBoostClassifierTrainingRunTools(service);

            var submission = await tools.SubmitAdaBoostClassifierTrainingAsync(instance.Id,
                UniqueName("AdaBoostModel"), variableNames, processingTypeIds, inputs, labels,
                20, seed: 7);

            submission.Status.Should().Be(AdvancedAnalyticsStatus.Pending.ToString());

            var pendingRun = await Db.AdvancedAnalyticsTrainingRun.FirstAsync(w => w.Id == submission.RunId);
            pendingRun.StatusId.Should().Be(AdvancedAnalyticsStatus.Pending);
            pendingRun.KindId.Should().Be(AdvancedAnalyticsKind.Classifier);
            pendingRun.JobMode.Should().Be(AdvancedAnalyticsJobMode.Batch);
            pendingRun.ModelKindId.Should().Be(AdvancedAnalyticsModelKind.AdaBoostClassifierPipeline);

            var arguments = AdaBoostClassifierTrainingArguments.FromJson(pendingRun.Arguments);
            arguments.Inputs.Should().NotBeNull();
            arguments.Inputs!.Length.Should().Be(inputs.Length);
            arguments.Labels.Should().NotBeNull();
            arguments.Rounds.Should().Be(20);
            arguments.MaxDepth.Should().Be(2);

            var executor = new AdvancedAnalyticsAdaBoostClassifierTrainingExecutor(Db, TenantA, Permitted,
                LogManager.GetLogger("Test"));

            var executed = await executor.ExecuteAsync(pendingRun, arguments,
                _ => Task.FromResult((arguments.Inputs!, arguments.Labels!)), default);

            executed.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, executed.Error);
            executed.Version.Should().NotBeNull();
            executed.Version!.ModelJson.Should().Contain(AdaBoostClassifierRecall.ModelKind);
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
                AdvancedAnalyticsEvidenceJson.Deserialise<AdvancedAnalyticsEvidence<AdaBoostEvidenceDetails>>(
                    executed.Version.MetricsJson);
            evidence.Should().NotBeNull();
            evidence!.Kind.Should().Be(AdaBoostClassifierRecall.ModelKind);
            evidence.Variables.Should().Equal(variableNames);
            evidence.Details.RoundsFitted.Should().BeGreaterThan(0);
            evidence.Details.RoundAlphas.Should().OnlyContain(a => double.IsFinite(a) && a >= 0d);
            evidence.Details.TestMetrics.Should().NotBeNull();
            evidence.Details.TestMetrics!.RocAuc.Should().NotBeNull();
            evidence.Details.TestMetrics.RocAuc!.Value.Should().BeGreaterThan(0.9);

            var directResult = AdaBoostClassifierTrainer.Train(inputs, labels, variableNames,
                new AdaBoostClassifierOptions { Rounds = 20, MaxDepth = 2, MinimumLeafSize = 5, Seed = 7 });

            var directRecall = AdaBoostClassifierRecall.FromSnapshot(directResult.SnapshotJson);

            var persistedRecall = AdaBoostClassifierRecall.FromSnapshot(executed.Version.ModelJson);

            for (var i = 0; i < 10; i++)
            {
                var persisted = persistedRecall.Predict(inputs[i]);
                var direct = directRecall.Predict(inputs[i]);

                persisted.Should().HaveCount(1);
                persisted[0].Should().BeInRange(0d, 1d);
                persisted[0].Should().BeApproximately(direct[0], 1e-9);
            }

            var positiveProbability = persistedRecall.Predict([8d, 4d])[0];
            var negativeProbability = persistedRecall.Predict([-8d, -4d])[0];

            positiveProbability.Should().BeGreaterThan(0.5);
            negativeProbability.Should().BeLessThan(0.5);
        }

        [Fact]
        [Trait("Covers", "AdvancedAnalyticsTrainingRunTools.CancelAdvancedAnalyticsTrainingRunAsync")]
        public async Task CancelViaTheSharedToolCancelsAPendingAdaBoostClassifierRunAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var (inputs, labels) = SeparatedGaussianBlobs(50, 555);

            var service = await AdvancedAnalyticsAdaBoostClassifierTrainingService.CreateAsync(Db, Permitted,
                TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var tools = new AdaBoostClassifierTrainingRunTools(service);

            var submission = await tools.SubmitAdaBoostClassifierTrainingAsync(instance.Id,
                UniqueName("AdaBoostCancel"), variableNames, processingTypeIds, inputs, labels);

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
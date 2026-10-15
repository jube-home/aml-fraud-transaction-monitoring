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
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Reactivity;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Test.AdvancedAnalytics.Slice;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using log4net;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsGradientBoostingClassifierTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        [Fact]
        [Trait("Covers", "GradientBoostingClassifierTrainingRunTools.SubmitGradientBoostingClassifierTrainingAsync")]
        public async Task SubmitRunsExecutesDirectlyAndPersistsAVersionWhoseRecallMatchesADirectInProcessTrainAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var data = SliceData.Logistic(500, 707, 0d, [3.2, -3.2]);
            var processingTypeIds = data.Names.Select(_ => 1).ToArray();

            var service = await AdvancedAnalyticsGradientBoostingClassifierTrainingService.CreateAsync(Db,
                Permitted, TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var tools = new GradientBoostingClassifierTrainingRunTools(service);

            var submission = await tools.SubmitGradientBoostingClassifierTrainingAsync(instance.Id,
                UniqueName("GradientBoosting"), data.Names, processingTypeIds, data.Inputs, data.Labels,
                30, 3, 0.2, seed: 707);

            submission.Status.Should().Be(AdvancedAnalyticsStatus.Pending.ToString());

            var pendingRun = await Db.AdvancedAnalyticsTrainingRun.FirstAsync(w => w.Id == submission.RunId);
            pendingRun.StatusId.Should().Be(AdvancedAnalyticsStatus.Pending);
            pendingRun.KindId.Should().Be(AdvancedAnalyticsKind.Classifier);
            pendingRun.ModelKindId.Should().Be(AdvancedAnalyticsModelKind.GradientBoostingClassifierPipeline);
            pendingRun.JobMode.Should().Be(AdvancedAnalyticsJobMode.Batch);

            var arguments = GradientBoostingClassifierTrainingArguments.FromJson(pendingRun.Arguments);
            arguments.Inputs.Should().NotBeNull();
            arguments.Inputs!.Length.Should().Be(data.Inputs.Length);

            var executor = new AdvancedAnalyticsGradientBoostingClassifierTrainingExecutor(Db, TenantA, Permitted,
                LogManager.GetLogger("Test"));

            var executed = await executor.ExecuteAsync(pendingRun, arguments,
                _ => Task.FromResult((arguments.Inputs!, arguments.Labels!)), default);

            executed.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, executed.Error);
            executed.Version.Should().NotBeNull();
            executed.Version!.ModelJson.Should().Contain(GradientBoostingClassifierRecall.ModelKind);
            executed.Version.MetricsJson.Should().NotBeNullOrWhiteSpace();

            var persistedRun = await Db.AdvancedAnalyticsTrainingRun.FirstAsync(w => w.Id == submission.RunId);
            persistedRun.StatusId.Should().Be(AdvancedAnalyticsStatus.Succeeded);

            var version = await Db.AdvancedAnalyticsTrainedModelVersion.FirstAsync(w => w.Id == executed.Version.Id);
            var persistedRecall = GradientBoostingClassifierRecall.FromSnapshot(version.ModelJson);

            var direct = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new GradientBoostingClassifierOptions
                    { Rounds = 30, MaxDepth = 3, LearningRate = 0.2, MinimumLeafSize = 5, Seed = 707 });

            var directRecall = GradientBoostingClassifierRecall.FromSnapshot(direct.SnapshotJson);

            foreach (var row in data.Inputs.Take(15))
            {
                var fromWorkerPath = persistedRecall.Predict(row)[0];
                var fromDirectTrain = directRecall.Predict(row)[0];

                fromWorkerPath.Should().BeApproximately(fromDirectTrain, 1e-9);
            }

            var trainedModelId = executed.Model!.Id;
            var model = await Db.AdvancedAnalyticsTrainedModel.FirstAsync(w => w.Id == trainedModelId);
            model.KindId.Should().Be(AdvancedAnalyticsKind.Classifier);
            model.ActiveVersionId.Should().BeNull();
        }

        [Fact]
        [Trait("Covers", "GradientBoostingClassifierTrainingRunTools.SubmitGradientBoostingClassifierTrainingAsync")]
        public async Task SubmitRejectsAnInvalidLearningRateBeforeQueuingARunAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var data = SliceData.Logistic(100, 5, 0d, [1d]);
            var processingTypeIds = data.Names.Select(_ => 1).ToArray();

            var service = await AdvancedAnalyticsGradientBoostingClassifierTrainingService.CreateAsync(Db,
                Permitted, TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var tools = new GradientBoostingClassifierTrainingRunTools(service);

            var act = () => tools.SubmitGradientBoostingClassifierTrainingAsync(instance.Id,
                UniqueName("GradientBoostingInvalid"), data.Names, processingTypeIds, data.Inputs, data.Labels,
                learningRate: 0d);

            await act.Should().ThrowAsync<DtoValidationException>();
        }
    }
}
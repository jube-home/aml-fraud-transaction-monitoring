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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Repository;
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
using Xunit;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsRandomForestRegressorTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        [Fact]
        [Trait("Covers", "RandomForestRegressorTrainingRunTools.SubmitRandomForestRegressorTrainingAsync")]
        [Trait("Covers", "AdvancedAnalyticsRandomForestRegressorTrainingExecutor.ExecuteAsync")]
        public async Task SubmitThenExecutingDirectlyProducesAPromotableVersionWithAMatchingRecallAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var data = LinearSliceData.Continuous(400, 71, 0d, [4d, -4d], noiseStandardDeviation: 0.2);
            var processingTypeIds = data.Names.Select(_ => 1).ToArray();

            var service = await AdvancedAnalyticsRandomForestRegressorTrainingService.CreateAsync(Db,
                Permitted, TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var tools = new RandomForestRegressorTrainingRunTools(service);

            var submission = await tools.SubmitRandomForestRegressorTrainingAsync(instance.Id,
                UniqueName("RandomForestRegressor"), data.Names, processingTypeIds, data.Inputs, data.Outputs,
                20, 4, seed: 71);

            submission.Status.Should().Be(nameof(AdvancedAnalyticsStatus.Pending));

            var runs = new AdvancedAnalyticsTrainingRunRepository(Db, TenantA, Permitted);

            var run = await runs.GetByIdAsync(submission.RunId)
                      ?? throw new InvalidOperationException("The submitted run was not found.");

            run.ModelKindId.Should().Be(AdvancedAnalyticsModelKind.RandomForestRegressorPipeline);
            run.KindId.Should().Be(AdvancedAnalyticsKind.Regressor);

            var arguments = RandomForestRegressorTrainingArguments.FromJson(run.Arguments);

            var executor = new AdvancedAnalyticsRandomForestRegressorTrainingExecutor(Db, TenantA, Permitted,
                TestLog.NoOp);

            var outcome = await executor.ExecuteAsync(run, arguments, default);

            outcome.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, outcome.Error);
            outcome.Version.Should().NotBeNull();
            outcome.CreatedModel.Should().BeTrue();

            var storedRun = await runs.GetByIdAsync(submission.RunId);
            storedRun!.StatusId.Should().Be(AdvancedAnalyticsStatus.Succeeded);

            var version = await Db.AdvancedAnalyticsTrainedModelVersion.FirstAsync(w => w.Id == outcome.Version!.Id);

            var evidence = AdvancedAnalyticsEvidenceJson
                .Deserialise<AdvancedAnalyticsEvidence<RandomForestRegressorEvidenceDetails>>(version.MetricsJson);

            evidence.Should().NotBeNull();
            evidence!.Kind.Should().Be(RandomForestRegressorRecall.ModelKind);
            evidence.Details.TreeCount.Should().Be(20);
            evidence.Details.TestMetrics.Should().NotBeNull();
            evidence.Details.TestMetrics!.RSquared.Should().NotBeNull();
            evidence.Details.TestMetrics.RSquared!.Value.Should().BeGreaterThan(0.3);

            var recall = RandomForestRegressorRecall.FromSnapshot(version.ModelJson);

            for (var i = 0; i < 10; i++)
            {
                var direct = recall.Predict(data.Inputs[i]);
                direct.Should().HaveCount(1);
                double.IsFinite(direct[0]).Should().BeTrue();
            }
        }
    }
}
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Classification;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Regression;
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
using Jube.Data.Query.GetNextAdvancedAnalyticsTrainingRunQuery;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsNeuralNetworkClassifierTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsWorkerTestBase(fx)
    {
        [Fact]
        [Trait("Covers", "AdvancedAnalyticsNeuralNetworkClassifierTrainingService.SubmitTrainingAsync")]
        [Trait("Covers", "AdvancedAnalyticsNeuralNetworkClassifierTrainingExecutor.ExecuteAsync")]
        [Trait("Covers", "NeuralNetworkClassifierTrainingRunTools.SubmitNeuralNetworkClassifierTrainingAsync")]
        public async Task ASubmittedRunTrainsPersistsAndIsReadableThroughTheSharedToolsAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var data = SliceData.Logistic(500, 41, 0.1, [3.5, -2.5]);

            var neuralNetworkService = await AdvancedAnalyticsNeuralNetworkClassifierTrainingService.CreateAsync(
                Db, Permitted, TestLog.NoOp, Localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var neuralNetworkTools = new NeuralNetworkClassifierTrainingRunTools(neuralNetworkService);

            var submission = await neuralNetworkTools.SubmitNeuralNetworkClassifierTrainingAsync(instance.Id,
                UniqueName("NeuralNetworkModel"), data.Names,
                [.. data.Names.Select((_, i) => i % 2 == 0 ? 1 : 5)], data.Inputs, data.Labels,
                5, 150, seed: 41);

            submission.Status.Should().Be("Pending");

            var claimed = await new GetNextAdvancedAnalyticsTrainingRunQuery(Db, "test-engine", 4, DateTime.UtcNow)
                .ExecuteAsync(CancellationToken.None);

            claimed.Should().NotBeNull();
            claimed!.Id.Should().Be(submission.RunId);
            claimed.ModelKindId.Should().Be(AdvancedAnalyticsModelKind.NeuralNetworkClassifierPipeline);

            var run = await RunAsync(claimed.Id);
            var arguments = NeuralNetworkClassifierTrainingArguments.FromJson(claimed.Arguments);

            var executor = new AdvancedAnalyticsNeuralNetworkClassifierTrainingExecutor(Db, claimed.TenantRegistryId,
                claimed.UserName ?? string.Empty, TestLog.NoOp);

            var executed = await executor.ExecuteAsync(run, arguments, arguments.Inputs!, arguments.Labels!,
                CancellationToken.None);

            executed.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded, executed.Error);

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

            var version = await Db.AskJoobyAnalyticsTrainedModelVersion
                .Where(w => w.Id == result.TrainedModelVersionId)
                .FirstAsync();

            var evidence = AdvancedAnalyticsEvidenceJson
                .Deserialise<AdvancedAnalyticsEvidence<NeuralNetworkEvidenceDetails>>(version.MetricsJson);

            evidence.Should().NotBeNull();
            evidence!.Kind.Should().Be(NeuralNetworkClassifierRecall.ModelKind);
            evidence.Details.HiddenUnits.Should().Be(5);
            evidence.Details.Calibration.Should().NotBeNull();

            var recall = NeuralNetworkClassifierRecall.FromSnapshot(version.ModelJson);
            recall.Variables.Should().Equal(data.Names);
        }
    }
}
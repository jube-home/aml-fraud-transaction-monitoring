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

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Worker
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsTrainingToolEndToEndTests(DatabaseFixture fx)
        : AdvancedAnalyticsWorkerTestBase(fx)
    {
        [Fact]
        public async Task TheToolsDriveARunThroughTheGatewayFromSubmitToResultAsync()
        {
            await ResetQueueAsync();
            var instanceId = (await NewInstanceAsync(TenantA)).Id;
            var data = Dataset();

            var tools = new LogisticRegressionTrainingRunTools(
                new AdvancedAnalyticsTrainingRunsGateway(await TrainingServiceAsync(Permitted)));

            var submission = await tools.SubmitLogisticRegressionTrainingAsync(instanceId, UniqueName("Model"),
                data.Names, [.. data.Names.Select((_, i) => i % 2 == 0 ? 1 : 5)], data.Inputs, data.Labels);
            submission.Status.Should().Be("Pending");

            (await tools.GetTrainingRunStatusAsync(submission.RunId)).Finished.Should().BeFalse();

            var worker = NewWorker();
            await worker.RunPassAsync();
            await worker.WaitForIdleAsync();

            var status = await tools.GetTrainingRunStatusAsync(submission.RunId);
            status.Status.Should().Be("Succeeded");
            status.Finished.Should().BeTrue();
            var result = await tools.GetTrainingRunResultAsync(submission.RunId);
            result.Evidence.Should().NotBeNull();
            result.TrainedModelVersionId.Should().Be(status.TrainedModelVersionId);
            result.Activated.Should().BeFalse();
        }

        [Fact]
        public async Task TheCancelToolCancelsAQueuedRunThroughTheGatewayAsync()
        {
            await ResetQueueAsync();
            var instanceId = (await NewInstanceAsync(TenantA)).Id;
            var data = Dataset();

            var tools = new LogisticRegressionTrainingRunTools(
                new AdvancedAnalyticsTrainingRunsGateway(await TrainingServiceAsync(Permitted)));

            var submission = await tools.SubmitLogisticRegressionTrainingAsync(instanceId, UniqueName("Model"),
                data.Names, [.. data.Names.Select(_ => 1)], data.Inputs, data.Labels);

            var cancelled = await tools.CancelTrainingRunAsync(submission.RunId);

            cancelled.Status.Should().Be("Cancelled");
        }
    }
}
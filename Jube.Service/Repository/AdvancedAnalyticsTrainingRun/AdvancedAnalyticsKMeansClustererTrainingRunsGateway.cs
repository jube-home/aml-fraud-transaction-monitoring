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

using Jube.Service.Agent.AdvancedAnalyticsTools.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun
{
    public sealed class AdvancedAnalyticsKMeansClustererTrainingRunsGateway(
        AdvancedAnalyticsKMeansClustererTrainingService service) : IAdvancedAnalyticsClustererTrainingRuns
    {
        public async Task<ClustererTrainingRunSubmissionView> SubmitAsync(ClustererTrainingRunSubmitRequest request,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var submission = await service.SubmitTrainingAsync(new AdvancedAnalyticsKMeansClustererTrainingDto
            {
                AskJoobyAnalyticsInstanceId = request.InstanceId,
                ModelName = request.ModelName,
                CorrelationKey = request.CorrelationKey,
                VariableNames = request.VariableNames,
                ProcessingTypeIds = request.ProcessingTypeIds,
                Inputs = request.Inputs,
                Clusters = request.Clusters,
                Seeding = request.Seeding,
                MaximumIterations = request.MaximumIterations,
                Tolerance = request.Tolerance,
                Standardise = request.Standardise,
                ValidationFraction = request.ValidationFraction,
                TestFraction = request.TestFraction,
                Seed = request.Seed
            }, token).ConfigureAwait(false);

            return new ClustererTrainingRunSubmissionView
            {
                RunId = submission.RunId,
                Status = submission.Status.ToString(),
                HowToPoll = submission.HowToPoll
            };
        }

        public async Task<ClustererTrainingRunStatusView> GetStatusAsync(int runId, CancellationToken token = default)
        {
            return ToView(await service.GetRunAsync(runId, token).ConfigureAwait(false));
        }

        public async Task<ClustererTrainingRunStatusView> CancelAsync(int runId, CancellationToken token = default)
        {
            return ToView(await service.CancelRunAsync(runId, token).ConfigureAwait(false));
        }

        public async Task<ClustererTrainingRunResultView> GetResultAsync(int runId, CancellationToken token = default)
        {
            var result = await service.GetRunResultAsync(runId, token).ConfigureAwait(false);

            return new ClustererTrainingRunResultView
            {
                Run = ToView(result.Run),
                TrainedModelId = result.TrainedModelId,
                TrainedModelVersionId = result.TrainedModelVersionId,
                VersionNumber = result.VersionNumber,
                Activated = result.Activated,
                Evidence = result.Evidence
            };
        }

        private static ClustererTrainingRunStatusView ToView(AdvancedAnalyticsTrainingRunStatusDto dto)
        {
            return new ClustererTrainingRunStatusView
            {
                RunId = dto.RunId,
                Status = dto.Status.ToString(),
                ElapsedSeconds = dto.ElapsedSeconds,
                Error = dto.Error,
                Finished = dto.Finished,
                TrainedModelVersionId = dto.TrainedModelVersionId
            };
        }
    }
}
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

using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Resources;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.AskJoobyAnalyticsModel;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun
{
    public sealed class AdvancedAnalyticsSharedTrainingRunService
    {
        private readonly AdvancedAnalyticsOperationRunner runner;
        private readonly AdvancedAnalyticsTrainingRunRepository runs;
        private readonly AskJoobyAnalyticsTrainedModelVersionRepository versions;

        private AdvancedAnalyticsSharedTrainingRunService(AdvancedAnalyticsOperationRunner runner)
        {
            this.runner = runner;
            var tenant = runner.TenantRegistryId;
            var user = runner.UserName;
            versions = new AskJoobyAnalyticsTrainedModelVersionRepository(runner.DbContext, tenant, user);
            runs = new AdvancedAnalyticsTrainingRunRepository(runner.DbContext, tenant, user);
        }

        public static Task<AdvancedAnalyticsSharedTrainingRunService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AdvancedAnalyticsSharedTrainingRunService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var runner = await AdvancedAnalyticsOperationRunner.CreateAsync(
                AdvancedAnalyticsChangeAreas.TrainedModel, typeof(AdvancedAnalyticsTrainingResources),
                AdvancedAnalyticsTrainingResources.NotAuthenticated, dbContext, userName, log,
                stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false);

            return new AdvancedAnalyticsSharedTrainingRunService(runner);
        }

        public Task<AdvancedAnalyticsTrainingRunStatusDto> GetRunAsync(int runId, CancellationToken token = default)
        {
            return runner.RunAsync("GetTrainingRunStatus", async op =>
            {
                runner.EnsurePermitted(AdvancedAnalyticsPermissionSpecifications.analyticsOnly,
                    "GetTrainingRunStatus", AdvancedAnalyticsTrainingResources.PermissionDenied);

                var run = await runs.GetByIdAsync(runId, token).ConfigureAwait(false)
                          ?? throw new NotFoundException(
                              runner.Strings[AdvancedAnalyticsTrainingResources.RunNotFound]);

                op.Entity(run.Id);

                return await StatusOfAsync(run, token).ConfigureAwait(false);
            });
        }

        public Task<AdvancedAnalyticsTrainingRunStatusDto> CancelRunAsync(int runId,
            CancellationToken token = default)
        {
            return runner.RunAsync("CancelTrainingRun", async op =>
            {
                runner.EnsurePermitted(AdvancedAnalyticsPermissionSpecifications.trainingOnly,
                    "CancelTrainingRun", AdvancedAnalyticsTrainingResources.PermissionDenied);

                var outcome = await runs.StopAsync(runId,
                        runner.Strings[AdvancedAnalyticsTrainingResources.TrainingCancelled], token)
                    .ConfigureAwait(false);

                if (outcome == AdvancedAnalyticsCancelOutcome.NotFound)
                {
                    throw new NotFoundException(runner.Strings[AdvancedAnalyticsTrainingResources.RunNotFound]);
                }

                op.Entity(runId);

                var run = await runs.GetByIdAsync(runId, token).ConfigureAwait(false)
                          ?? throw new NotFoundException(
                              runner.Strings[AdvancedAnalyticsTrainingResources.RunNotFound]);

                return await StatusOfAsync(run, token).ConfigureAwait(false);
            });
        }

        public Task<AdvancedAnalyticsSharedTrainingRunResultDto> GetRunResultAsync(int runId,
            CancellationToken token = default)
        {
            return runner.RunAsync("GetTrainingRunResult", async op =>
            {
                runner.EnsurePermitted(AdvancedAnalyticsPermissionSpecifications.analyticsOnly,
                    "GetTrainingRunResult", AdvancedAnalyticsTrainingResources.PermissionDenied);

                var run = await runs.GetByIdAsync(runId, token).ConfigureAwait(false)
                          ?? throw new NotFoundException(
                              runner.Strings[AdvancedAnalyticsTrainingResources.RunNotFound]);

                op.Entity(run.Id);

                var dto = new AdvancedAnalyticsSharedTrainingRunResultDto
                {
                    Run = await StatusOfAsync(run, token).ConfigureAwait(false)
                };

                if (run.StatusId != AdvancedAnalyticsStatus.Succeeded)
                {
                    return dto;
                }

                var version = await versions.GetByRunIdAsync(run.Id, token).ConfigureAwait(false);

                if (version == null)
                {
                    return dto;
                }

                dto.TrainedModelId = version.AskJoobyAnalyticsTrainedModelId;
                dto.TrainedModelVersionId = version.Id;
                dto.VersionNumber = version.VersionNumber;
                dto.Run.TrainedModelVersionId = version.Id;
                dto.Evidence = AskJoobyAnalyticsModelMapper.ReadEvidence(version.MetricsJson);

                return dto;
            });
        }

        private async Task<AdvancedAnalyticsTrainingRunStatusDto> StatusOfAsync(
            Jube.Data.Poco.AdvancedAnalyticsTrainingRun run,
            CancellationToken token)
        {
            var finished = AdvancedAnalyticsTrainingRunRepository.IsTerminal(run.StatusId);
            var now = DateTime.UtcNow;
            var end = run.CompletedDate ?? now;
            var start = run.StartedDate ?? run.CreatedDate;
            int? versionId = null;

            if (run.StatusId == AdvancedAnalyticsStatus.Succeeded)
            {
                versionId = (await versions.GetByRunIdAsync(run.Id, token).ConfigureAwait(false))?.Id;
            }

            return new AdvancedAnalyticsTrainingRunStatusDto
            {
                RunId = run.Id,
                RunGuid = run.Guid,
                AskJoobyAnalyticsInstanceId = run.AskJoobyAnalyticsInstanceId,
                Status = run.StatusId,
                JobMode = run.JobMode,
                ElapsedSeconds = Math.Max(0, (end - start).TotalSeconds),
                Error = run.Error,
                Finished = finished,
                CreatedDate = run.CreatedDate,
                TrainedModelVersionId = versionId
            };
        }
    }
}
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

using System.ComponentModel;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Engine.AdvancedAnalytics.Density;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Training.Exceptions;
using Jube.Resources;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.AdvancedAnalyticsModel.Models;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun
{
    public sealed class AdvancedAnalyticsGaussianMixtureDensityTrainingService
    {
        private readonly AdvancedAnalyticsOperationRunner runner;
        private readonly AdvancedAnalyticsTrainingRunRepository runs;
        private readonly AdvancedAnalyticsGaussianMixtureDensityTrainingValidator validator;
        private readonly AdvancedAnalyticsTrainedModelVersionRepository versions;

        private AdvancedAnalyticsGaussianMixtureDensityTrainingService(AdvancedAnalyticsOperationRunner runner)
        {
            this.runner = runner;
            var tenant = runner.TenantRegistryId;
            var user = runner.UserName;
            var instances = new AdvancedAnalyticsInstanceRepository(runner.DbContext, tenant, user);
            versions = new AdvancedAnalyticsTrainedModelVersionRepository(runner.DbContext, tenant, user);
            runs = new AdvancedAnalyticsTrainingRunRepository(runner.DbContext, tenant, user);

            validator = new AdvancedAnalyticsGaussianMixtureDensityTrainingValidator(runner.Strings,
                async (id, token) => await instances.GetByIdAsync(id, token).ConfigureAwait(false) != null);
        }

        public static Task<AdvancedAnalyticsGaussianMixtureDensityTrainingService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AdvancedAnalyticsGaussianMixtureDensityTrainingService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var runner = await AdvancedAnalyticsOperationRunner.CreateAsync(
                AdvancedAnalyticsChangeAreas.TrainedModel, typeof(AdvancedAnalyticsTrainingResources),
                AdvancedAnalyticsTrainingResources.NotAuthenticated, dbContext, userName, log,
                stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false);

            return new AdvancedAnalyticsGaussianMixtureDensityTrainingService(runner);
        }

        [Description("Queues a Gaussian mixture density estimation training run and returns its run identifier " +
                     "at once, before anything is trained. A background worker in the engine picks the run up, " +
                     "fits the mixture, records the steps and evidence and stores the model as a new immutable " +
                     "version that is never made live by training. This kind is always unsupervised: there are " +
                     "no labels and the score is never calibrated into a probability, only the raw log-density. " +
                     "Needs the Advanced Analytics Training permission.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitTrainingAsync(
            [Description("The rows, variable names and options of the training run.")]
            AdvancedAnalyticsGaussianMixtureDensityTrainingDto? request,
            CancellationToken token = default)
        {
            return runner.RunAsync("SubmitTraining", async op =>
            {
                ArgumentNullException.ThrowIfNull(request);

                runner.EnsurePermitted(AdvancedAnalyticsPermissionSpecifications.trainingOnly,
                    "SubmitTraining", AdvancedAnalyticsTrainingResources.PermissionDenied);

                var validation = await validator.ValidateAsync(request, token).ConfigureAwait(false);

                if (!validation.IsValid)
                {
                    throw new DtoValidationException(validation);
                }

                var arguments = new GaussianMixtureDensityTrainingArguments
                {
                    Rows = request.Inputs!.Length,
                    ModelName = request.ModelName,
                    CorrelationKey = request.CorrelationKey,
                    VariableNames = request.VariableNames,
                    ProcessingTypeIds = request.ProcessingTypeIds,
                    Components = request.Components,
                    Standardise = request.Standardise,
                    MaxIterations = request.MaxIterations,
                    Tolerance = request.Tolerance,
                    RegularizationEpsilon = request.RegularizationEpsilon,
                    ValidationFraction = request.ValidationFraction,
                    TestFraction = request.TestFraction,
                    Seed = request.Seed,
                    Inputs = request.Inputs
                };

                var run = await runs.InsertAsync(new Jube.Data.Poco.AdvancedAnalyticsTrainingRun
                {
                    AdvancedAnalyticsInstanceId = request.AdvancedAnalyticsInstanceId,
                    KindId = AdvancedAnalyticsKind.DensityEstimator,
                    ModelKindId = AdvancedAnalyticsModelKind.GaussianMixtureDensityPipeline,
                    JobMode = AdvancedAnalyticsJobMode.Batch,
                    Seed = request.Seed,
                    Arguments = arguments.ToJson()
                }, token).ConfigureAwait(false);

                op.Entity(run.Id);
                op.Created();

                runner.Info($"AdvancedAnalyticsTrainedModel.SubmitGaussianMixtureDensityTraining: run {run.Id} " +
                            $"queued rows={arguments.Rows} components={arguments.Components} user={runner.UserName}");

                return new AdvancedAnalyticsTrainingSubmissionDto
                {
                    RunId = run.Id,
                    RunGuid = run.Guid,
                    Status = AdvancedAnalyticsStatus.Pending,
                    HowToPoll = "Read the run status with this run id until Status is Succeeded, Failed or " +
                                "Cancelled, then read the run result. Nothing has been trained yet."
                };
            });
        }

        [Description("Cancels a queued or running Gaussian mixture density training run by its numeric " +
                     "identifier. A queued run becomes Cancelled at once; a running run becomes Cancelling and " +
                     "the worker stops it at the next step boundary, storing no model. A finished run is final " +
                     "and unchanged. Needs the Advanced Analytics Training permission.")]
        public Task<AdvancedAnalyticsTrainingRunStatusDto> CancelRunAsync(
            [Description("Numeric identifier of the training run to cancel.")]
            int runId,
            CancellationToken token = default)
        {
            return runner.RunAsync("CancelRun", op =>
            {
                runner.EnsurePermitted(AdvancedAnalyticsPermissionSpecifications.trainingOnly,
                    "CancelRun", AdvancedAnalyticsTrainingResources.PermissionDenied);

                return StopCoreAsync(op, runId, token);
            });
        }

        [Description("Reads the status of one Gaussian mixture density training run: its status, elapsed time, " +
                     "error and the version it produced, for polling a queued run. Needs the Advanced Analytics " +
                     "permission.")]
        public Task<AdvancedAnalyticsTrainingRunStatusDto> GetRunAsync(
            [Description("Numeric identifier of the training run.")]
            int runId,
            CancellationToken token = default)
        {
            return runner.RunAsync("GetRun", async op =>
            {
                runner.EnsurePermitted(AdvancedAnalyticsPermissionSpecifications.analyticsOnly,
                    "GetRun", AdvancedAnalyticsTrainingResources.PermissionDenied);

                var run = await runs.GetByIdAsync(runId, token).ConfigureAwait(false)
                          ?? throw new NotFoundException(
                              runner.Strings[AdvancedAnalyticsTrainingResources.RunNotFound]);

                op.Entity(run.Id);

                return await StatusOfAsync(run, true, token).ConfigureAwait(false);
            });
        }

        [Description("Reads the result of a finished Gaussian mixture density training run: its status and, " +
                     "once it Succeeded, the version it produced and the complete evidence needed to decide on " +
                     "promotion. Needs the Advanced Analytics permission.")]
        public Task<AdvancedAnalyticsGaussianMixtureDensityTrainingRunResultDto> GetRunResultAsync(
            [Description("Numeric identifier of the training run.")]
            int runId,
            CancellationToken token = default)
        {
            return runner.RunAsync("GetRunResult", async op =>
            {
                runner.EnsurePermitted(AdvancedAnalyticsPermissionSpecifications.analyticsOnly,
                    "GetRunResult", AdvancedAnalyticsTrainingResources.PermissionDenied);

                var run = await runs.GetByIdAsync(runId, token).ConfigureAwait(false)
                          ?? throw new NotFoundException(
                              runner.Strings[AdvancedAnalyticsTrainingResources.RunNotFound]);

                op.Entity(run.Id);

                var dto = new AdvancedAnalyticsGaussianMixtureDensityTrainingRunResultDto
                {
                    Run = await StatusOfAsync(run, true, token).ConfigureAwait(false)
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

                dto.TrainedModelId = version.AdvancedAnalyticsTrainedModelId;
                dto.TrainedModelVersionId = version.Id;
                dto.VersionNumber = version.VersionNumber;
                dto.Run.TrainedModelVersionId = version.Id;

                dto.Evidence = string.IsNullOrWhiteSpace(version.MetricsJson)
                    ? null
                    : AdvancedAnalyticsEvidenceJson.Deserialise<
                        AdvancedAnalyticsEvidence<GaussianMixtureDensityEvidenceDetails>>(version.MetricsJson);

                return dto;
            });
        }

        private async Task<AdvancedAnalyticsTrainingRunStatusDto> StopCoreAsync(
            OperationScope op, int runId, CancellationToken token)
        {
            var outcome = await runs.StopAsync(runId,
                runner.Strings[AdvancedAnalyticsTrainingResources.TrainingCancelled], token).ConfigureAwait(false);

            if (outcome == AdvancedAnalyticsCancelOutcome.NotFound)
            {
                throw new NotFoundException(runner.Strings[AdvancedAnalyticsTrainingResources.RunNotFound]);
            }

            op.Entity(runId);

            if (outcome is AdvancedAnalyticsCancelOutcome.CancelledPending
                or AdvancedAnalyticsCancelOutcome.CancellationRequested)
            {
                op.Updated();

                runner.Info($"AdvancedAnalyticsTrainedModel.CancelGaussianMixtureDensityTrainingRun: run {runId} " +
                            $"{outcome} user={runner.UserName}");
            }

            var run = await runs.GetByIdAsync(runId, token).ConfigureAwait(false)
                      ?? throw new NotFoundException(runner.Strings[AdvancedAnalyticsTrainingResources.RunNotFound]);

            return await StatusOfAsync(run, true, token).ConfigureAwait(false);
        }

        private async Task<AdvancedAnalyticsTrainingRunStatusDto> StatusOfAsync(
            Jube.Data.Poco.AdvancedAnalyticsTrainingRun run,
            bool includeDetail, CancellationToken token)
        {
            var finished = AdvancedAnalyticsTrainingRunRepository.IsTerminal(run.StatusId);
            var now = DateTime.UtcNow;
            var end = run.CompletedDate ?? now;
            var start = run.StartedDate ?? run.CreatedDate;
            string? parameters = null;
            int? versionId = null;

            if (includeDetail)
            {
                parameters = ParametersOf(run.Arguments);

                if (run.StatusId == AdvancedAnalyticsStatus.Succeeded)
                {
                    versionId = (await versions.GetByRunIdAsync(run.Id, token).ConfigureAwait(false))?.Id;
                }
            }

            return new AdvancedAnalyticsTrainingRunStatusDto
            {
                RunId = run.Id,
                RunGuid = run.Guid,
                AdvancedAnalyticsInstanceId = run.AdvancedAnalyticsInstanceId,
                Status = run.StatusId,
                JobMode = run.JobMode,
                ElapsedSeconds = Math.Max(0, (end - start).TotalSeconds),
                Error = run.Error,
                Finished = finished,
                CreatedDate = run.CreatedDate,
                StartedDate = run.StartedDate,
                CompletedDate = run.CompletedDate,
                Parameters = parameters,
                TrainedModelVersionId = versionId
            };
        }

        private static string? ParametersOf(string? argumentsJson)
        {
            if (string.IsNullOrWhiteSpace(argumentsJson))
            {
                return null;
            }

            try
            {
                var arguments = GaussianMixtureDensityTrainingArguments.FromJson(argumentsJson);

                return arguments.WithoutData().ToJson();
            }
            catch (AdvancedAnalyticsTrainingInputException)
            {
                return null;
            }
        }
    }
}
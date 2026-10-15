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
using FluentValidation.Results;
using Jube.Analytics;
using Jube.Analytics.Datasets;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Regression.Models;
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Engine.AdvancedAnalytics.Training.Exceptions;
using Jube.Engine.AdvancedAnalytics.Training.Models;
using Jube.Resources;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun
{
    public sealed class AdvancedAnalyticsTrainingService
    {
        private readonly AdvancedAnalyticsCacheRepository cache;
        private readonly AdvancedAnalyticsOperationRunner runner;
        private readonly AdvancedAnalyticsTrainingRunRepository runs;
        private readonly AdvancedAnalyticsLogisticRegressionTrainingValidator validator;
        private readonly AdvancedAnalyticsTrainedModelVersionRepository versions;

        private AdvancedAnalyticsTrainingService(AdvancedAnalyticsOperationRunner runner)
        {
            this.runner = runner;
            var tenant = runner.TenantRegistryId;
            var user = runner.UserName;
            var instances = new AdvancedAnalyticsInstanceRepository(runner.DbContext, tenant, user);
            versions = new AdvancedAnalyticsTrainedModelVersionRepository(runner.DbContext, tenant, user);
            runs = new AdvancedAnalyticsTrainingRunRepository(runner.DbContext, tenant, user);

            cache = new AdvancedAnalyticsCacheRepository(runner.DbContext, tenant, user,
                new AdvancedAnalyticsCacheLimits());

            validator = new AdvancedAnalyticsLogisticRegressionTrainingValidator(runner.Strings,
                async (id, token) => await instances.GetByIdAsync(id, token).ConfigureAwait(false) != null);
        }

        internal Func<double[][], int[], string[], LogisticRegressionOptions, CancellationToken,
            LogisticRegressionResult> Trainer { get; set; } =
            (inputs, labels, names, options, token) =>
                LogisticRegressionTrainer.Train(inputs, labels, names, options, token);

        internal AdvancedAnalyticsTrainingSettings Settings { get; set; } =
            AdvancedAnalyticsTrainingSettings.Current;

        internal AdvancedAnalyticsTrainingInputResolver InputResolver { get; set; } = new();

        public static Task<AdvancedAnalyticsTrainingService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AdvancedAnalyticsTrainingService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var runner = await AdvancedAnalyticsOperationRunner.CreateAsync(
                AdvancedAnalyticsChangeAreas.TrainedModel, typeof(AdvancedAnalyticsTrainingResources),
                AdvancedAnalyticsTrainingResources.NotAuthenticated, dbContext, userName, log,
                stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false);

            return new AdvancedAnalyticsTrainingService(runner);
        }

        [Description("Queues a logistic regression training run and returns its run identifier at once, before " +
                     "anything is trained. A background worker in the engine picks the run up, trains it, records " +
                     "the steps, metrics and evidence and stores the model as a new immutable version that is " +
                     "never made live by training. The rows may be inline (up to a configured cap) or held in " +
                     "the Advanced Analytics cache and named by a cache pointer. Poll the run status, then read " +
                     "the result. Needs the Advanced Analytics Training permission.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitTrainingAsync(
            [Description("The input reference, variable names and options of the training run.")]
            AdvancedAnalyticsLogisticRegressionTrainingDto? request,
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

                var input = request.Input ?? AdvancedAnalyticsTrainingInput.Inline(request.Inputs!, request.Labels!);
                Guid? pointer = null;
                int rows;

                switch (input.Kind)
                {
                    case AdvancedAnalyticsTrainingInputKind.Inline:
                        input = AdvancedAnalyticsTrainingInput.Inline(request.Inputs!, request.Labels!);
                        rows = request.Inputs!.Length;
                        EnsureInlineWithinLimits(rows, input);
                        break;
                    case AdvancedAnalyticsTrainingInputKind.CachePointer:
                        pointer = input.Pointer;
                        rows = await EnsureCacheInputAsync(request, input, token).ConfigureAwait(false);
                        input = input.WithoutData();
                        break;
                    default:
                        throw new NotSupportedException(runner.Strings[
                            AdvancedAnalyticsTrainingResources.DatasetInputNotSupported]);
                }

                if (!InputResolver.Supports(input.Kind))
                {
                    throw new NotSupportedException(runner.Strings[
                        AdvancedAnalyticsTrainingResources.DatasetInputNotSupported]);
                }

                var run = await runs.InsertAsync(new Jube.Data.Poco.AdvancedAnalyticsTrainingRun
                {
                    AdvancedAnalyticsInstanceId = request.AdvancedAnalyticsInstanceId,
                    KindId = AdvancedAnalyticsKind.Classifier,
                    ModelKindId = AdvancedAnalyticsModelKind.LogisticRegressionPipeline,
                    JobMode = AdvancedAnalyticsJobMode.Batch,
                    Seed = request.Seed,
                    InputPointer = pointer,
                    Arguments = ArgumentsOf(request, rows, input).ToJson()
                }, token).ConfigureAwait(false);

                op.Entity(run.Id);
                op.Created();

                runner.Info($"AdvancedAnalyticsTrainedModel.SubmitTraining: run {run.Id} queued " +
                            $"input={input.Kind} rows={rows} user={runner.UserName}");

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

        [Description("Trains a logistic regression on the supplied rows immediately and records the whole run: the " +
                     "ordered steps, the metrics, the variable statistics and the calibration and ROC curves. " +
                     "This is the quick path for small data and is limited to a configured number of rows " +
                     "(2,000 by default); anything larger must use the submit operation, which queues the run " +
                     "for a background worker. On success the model is stored as a new immutable version under " +
                     "the named model of the instance, and training never makes that version live. The result " +
                     "carries the complete evidence (held-out metrics, coefficient significance, calibration, " +
                     "warnings) needed to decide whether the version deserves promotion. A run the trainer " +
                     "rejects is recorded as Failed with its reason and stores no model. Needs the AskJooby " +
                     "Analytics Training permission.")]
        public Task<AdvancedAnalyticsTrainingOutcomeDto> TrainLogisticRegressionAsync(
            [Description("The rows, labels, variable names and options of the training run.")]
            AdvancedAnalyticsLogisticRegressionTrainingDto? request,
            CancellationToken token = default)
        {
            return runner.RunAsync("TrainLogisticRegression", async op =>
            {
                ArgumentNullException.ThrowIfNull(request);

                runner.EnsurePermitted(AdvancedAnalyticsPermissionSpecifications.trainingOnly,
                    "TrainLogisticRegression", AdvancedAnalyticsTrainingResources.PermissionDenied);

                var validation = await validator.ValidateAsync(request, token).ConfigureAwait(false);

                if (!validation.IsValid)
                {
                    throw new DtoValidationException(validation);
                }

                if (request.Input is { Kind: not AdvancedAnalyticsTrainingInputKind.Inline })
                {
                    throw Invalid("Input", "InputKindNotOnline", string.Format(
                        runner.Strings[AdvancedAnalyticsTrainingResources.OnlineTooManyRows],
                        Settings.OnlineMaxRows));
                }

                if (request.Inputs!.Length > Settings.OnlineMaxRows)
                {
                    throw Invalid("Inputs", "OnlineTooManyRows", string.Format(
                        runner.Strings[AdvancedAnalyticsTrainingResources.OnlineTooManyRows],
                        Settings.OnlineMaxRows));
                }

                var inputs = request.Inputs!;
                var labels = request.Labels!;
                var input = AdvancedAnalyticsTrainingInput.Inline(inputs, labels);

                var run = await runs.InsertAsync(new Jube.Data.Poco.AdvancedAnalyticsTrainingRun
                {
                    AdvancedAnalyticsInstanceId = request.AdvancedAnalyticsInstanceId,
                    KindId = AdvancedAnalyticsKind.Classifier,
                    ModelKindId = AdvancedAnalyticsModelKind.LogisticRegressionPipeline,
                    JobMode = AdvancedAnalyticsJobMode.Online,
                    Seed = request.Seed,
                    Arguments = ArgumentsOf(request, inputs.Length, input.WithoutData()).ToJson()
                }, token).ConfigureAwait(false);

                await runs.UpdateStatusAsync(run.Id, AdvancedAnalyticsStatus.Running, null, token)
                    .ConfigureAwait(false);

                var executor = new AdvancedAnalyticsTrainingExecutor(runner.DbContext, runner.TenantRegistryId,
                    runner.UserName, runner.Log, new AdvancedAnalyticsExecutionMessages
                    {
                        Cancelled = runner.Strings[AdvancedAnalyticsTrainingResources.TrainingCancelled],
                        FailedUnexpectedly =
                            runner.Strings[AdvancedAnalyticsTrainingResources.TrainingFailedUnexpectedly]
                    })
                {
                    Trainer = Trainer
                };

                AdvancedAnalyticsExecutionResult result;

                try
                {
                    result = await executor.ExecuteAsync(run, ArgumentsOf(request, inputs.Length, input.WithoutData()),
                        _ => Task.FromResult(new AdvancedAnalyticsResolvedTrainingData
                        {
                            Inputs = inputs,
                            Labels = labels
                        }), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    await executor.RecordTerminalAsync(run.Id, AdvancedAnalyticsStatus.Cancelled,
                        runner.Strings[AdvancedAnalyticsTrainingResources.TrainingCancelled]).ConfigureAwait(false);

                    throw;
                }

                if (result.Status == AdvancedAnalyticsStatus.Failed)
                {
                    runner.Info($"AdvancedAnalyticsTrainedModel.TrainLogisticRegression: run {run.Id} failed: " +
                                $"{result.Error} user={runner.UserName}");

                    op.Outcome("failed");

                    return new AdvancedAnalyticsTrainingOutcomeDto
                    {
                        RunId = run.Id,
                        RunGuid = run.Guid,
                        Status = AdvancedAnalyticsStatus.Failed,
                        Error = result.Error,
                        Activated = false
                    };
                }

                op.Entity(result.Model!.Id);
                op.Version(result.Version!.VersionNumber);

                if (result.CreatedModel)
                {
                    op.Created();
                }
                else
                {
                    op.Updated();
                }

                runner.Info($"AdvancedAnalyticsTrainedModel.TrainLogisticRegression: run {run.Id} stored " +
                            $"version {result.Version.VersionNumber} of model {result.Model.Id} (not activated) " +
                            $"user={runner.UserName}");

                return new AdvancedAnalyticsTrainingOutcomeDto
                {
                    RunId = run.Id,
                    RunGuid = run.Guid,
                    Status = AdvancedAnalyticsStatus.Succeeded,
                    TrainedModelId = result.Model.Id,
                    TrainedModelGuid = result.Model.Guid,
                    TrainedModelVersionId = result.Version.Id,
                    VersionNumber = result.Version.VersionNumber,
                    Activated = false,
                    Result = result.Result
                };
            });
        }

        [Description("Requests that a queued or running training run stop, identified by its Guid, the way an " +
                     "Exhaustive Adaptation is stopped. A queued run becomes Cancelled at once; a running run " +
                     "becomes Cancelling and the worker stops it at the next step boundary or inside the fit. " +
                     "A finished run is left as it is. Needs the Advanced Analytics Training permission.")]
        public Task<AdvancedAnalyticsTrainingRunStatusDto> StopRunAsync(
            [Description("Globally unique identifier of the training run to stop.")]
            Guid runGuid,
            CancellationToken token = default)
        {
            return runner.RunAsync("StopRun", async op =>
            {
                runner.EnsurePermitted(AdvancedAnalyticsPermissionSpecifications.trainingOnly,
                    "StopRun", AdvancedAnalyticsTrainingResources.PermissionDenied);

                var run = await runs.GetByGuidAsync(runGuid, token).ConfigureAwait(false)
                          ?? throw new NotFoundException(
                              runner.Strings[AdvancedAnalyticsTrainingResources.RunNotFound]);

                return await StopCoreAsync(op, run.Id, token).ConfigureAwait(false);
            });
        }

        [Description("Cancels a queued or running training run by its numeric identifier. A queued run becomes " +
                     "Cancelled at once; a running run becomes Cancelling and the worker stops it at the next step " +
                     "boundary or inside the fit, storing no model. A finished run is final and unchanged. Needs " +
                     "the Advanced Analytics Training permission.")]
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

        [Description("Reads the status of one training run: its status, elapsed time, error and the version it " +
                     "produced, for polling a queued run. Needs the Advanced Analytics permission.")]
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

        [Description("Lists training runs of the caller's tenant, newest first, optionally for one instance or one " +
                     "status. Needs the Advanced Analytics permission.")]
        public Task<List<AdvancedAnalyticsTrainingRunStatusDto>> ListRunsAsync(
            [Description("Only runs of this Advanced Analytics instance, or null for all.")]
            int? advancedAnalyticsInstanceId = null,
            [Description("Only runs in this status, or null for all.")]
            AdvancedAnalyticsStatus? status = null,
            [Description("Most runs to return, from 1 to 200.")]
            int take = 50,
            CancellationToken token = default)
        {
            return runner.RunAsync("ListRuns", async _ =>
            {
                runner.EnsurePermitted(AdvancedAnalyticsPermissionSpecifications.analyticsOnly,
                    "ListRuns", AdvancedAnalyticsTrainingResources.PermissionDenied);

                var list = await runs.ListAsync(advancedAnalyticsInstanceId, status, Math.Clamp(take, 1, 200), token)
                    .ConfigureAwait(false);

                var result = new List<AdvancedAnalyticsTrainingRunStatusDto>();

                foreach (var run in list)
                {
                    result.Add(await StatusOfAsync(run, false, token).ConfigureAwait(false));
                }

                return result;
            });
        }

        [Description("Reads the result of a finished training run: its status and, once it Succeeded, the version " +
                     "it produced and the complete evidence needed to decide on promotion. Needs the AskJooby " +
                     "Analytics permission.")]
        public Task<AdvancedAnalyticsTrainingRunResultDto> GetRunResultAsync(
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

                var dto = new AdvancedAnalyticsTrainingRunResultDto
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
                        Jube.Service.Repository.AdvancedAnalyticsModel.Models.AdvancedAnalyticsLogisticEvidence>(
                        version.MetricsJson);

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

                runner.Info($"AdvancedAnalyticsTrainedModel.StopRun: run {runId} {outcome} " +
                            $"user={runner.UserName}");
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
                var arguments = AdvancedAnalyticsTrainingArguments.FromJson(argumentsJson);
                arguments.Input = arguments.Input?.WithoutData();

                return arguments.ToJson();
            }
            catch (AdvancedAnalyticsTrainingInputException)
            {
                return null;
            }
        }

        private void EnsureInlineWithinLimits(int rowCount,
            AdvancedAnalyticsTrainingInput input)
        {
            if (rowCount > Settings.InlineMaxRows)
            {
                throw Invalid("Inputs", "InlineTooManyRows", string.Format(
                    runner.Strings[AdvancedAnalyticsTrainingResources.InlineTooManyRows], Settings.InlineMaxRows));
            }

            if (AdvancedAnalyticsTrainingArguments.InlineSizeBytes(input) > Settings.InlineMaxBytes)
            {
                throw Invalid("Inputs", "InlineTooLarge", string.Format(
                    runner.Strings[AdvancedAnalyticsTrainingResources.InlineTooLarge], Settings.InlineMaxBytes));
            }
        }

        private async Task<int> EnsureCacheInputAsync(AdvancedAnalyticsLogisticRegressionTrainingDto request,
            AdvancedAnalyticsTrainingInput input, CancellationToken token)
        {
            var entry = await cache.GetMetadataAsync(input.Pointer!.Value, token).ConfigureAwait(false);

            if (entry == null)
            {
                throw Invalid("Input.Pointer", "CachePointerNotFound",
                    runner.Strings[AdvancedAnalyticsTrainingResources.CachePointerNotFound]);
            }

            if (entry.Columns != request.VariableNames!.Length + 1 || input.LabelColumn >= entry.Columns)
            {
                throw Invalid("Input", "CacheShapeMismatch",
                    runner.Strings[AdvancedAnalyticsTrainingResources.CacheShapeMismatch]);
            }

            return entry.Rows;
        }

        private static DtoValidationException Invalid(string property, string code, string message)
        {
            return new DtoValidationException(new ValidationResult(
            [
                new ValidationFailure(property, message) { ErrorCode = code }
            ]));
        }

        private static AdvancedAnalyticsTrainingArguments ArgumentsOf(
            AdvancedAnalyticsLogisticRegressionTrainingDto request, int rows, AdvancedAnalyticsTrainingInput input)
        {
            return new AdvancedAnalyticsTrainingArguments
            {
                Rows = rows,
                ModelName = request.ModelName,
                CorrelationKey = request.CorrelationKey,
                VariableNames = request.VariableNames,
                ProcessingTypeIds = request.ProcessingTypeIds,
                ValidationFraction = request.ValidationFraction,
                TestFraction = request.TestFraction,
                Seed = request.Seed,
                RidgeLambda = request.RidgeLambda,
                ClassWeighting = request.ClassWeighting,
                MissingValuePolicy = request.MissingValuePolicy,
                Standardise = request.Standardise,
                Calibrate = request.Calibrate,
                BaseRateOverride = request.BaseRateOverride,
                CostFalseNegative = request.CostFalseNegative,
                CostFalsePositive = request.CostFalsePositive,
                DecisionThreshold = request.DecisionThreshold,
                MaximumIterations = request.MaximumIterations,
                Tolerance = request.Tolerance,
                Input = input
            };
        }
    }
}
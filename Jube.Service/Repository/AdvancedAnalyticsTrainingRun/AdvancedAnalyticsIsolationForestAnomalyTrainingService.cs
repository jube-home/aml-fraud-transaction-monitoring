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
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Engine.AdvancedAnalytics.Anomaly;
using Jube.Resources;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun
{
    public sealed class AdvancedAnalyticsIsolationForestAnomalyTrainingService
    {
        private readonly AdvancedAnalyticsOperationRunner runner;
        private readonly AdvancedAnalyticsTrainingRunRepository runs;
        private readonly AdvancedAnalyticsIsolationForestAnomalyTrainingValidator validator;

        private AdvancedAnalyticsIsolationForestAnomalyTrainingService(AdvancedAnalyticsOperationRunner runner)
        {
            this.runner = runner;
            var tenant = runner.TenantRegistryId;
            var user = runner.UserName;
            var instances = new AdvancedAnalyticsInstanceRepository(runner.DbContext, tenant, user);
            runs = new AdvancedAnalyticsTrainingRunRepository(runner.DbContext, tenant, user);

            validator = new AdvancedAnalyticsIsolationForestAnomalyTrainingValidator(runner.Strings,
                async (id, token) => await instances.GetByIdAsync(id, token).ConfigureAwait(false) != null);
        }

        internal AdvancedAnalyticsTrainingSettings Settings { get; set; } = AdvancedAnalyticsTrainingSettings.Current;

        public static Task<AdvancedAnalyticsIsolationForestAnomalyTrainingService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AdvancedAnalyticsIsolationForestAnomalyTrainingService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var runner = await AdvancedAnalyticsOperationRunner.CreateAsync(
                AdvancedAnalyticsChangeAreas.TrainedModel, typeof(AdvancedAnalyticsTrainingResources),
                AdvancedAnalyticsTrainingResources.NotAuthenticated, dbContext, userName, log,
                stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false);

            return new AdvancedAnalyticsIsolationForestAnomalyTrainingService(runner);
        }

        [Description("Queues an isolation forest anomaly training run and returns its run identifier at once, " +
                     "before anything is trained. A background worker in the engine grows the forest, records " +
                     "the steps and evidence and stores the model as a new immutable version that is never made " +
                     "live by training. Only inline rows are supported for this kind. Labels are optional: give " +
                     "a known-anomalous or known-normal flag per row to calibrate the raw score into a " +
                     "probability, or leave them out to train unsupervised. Poll the run status with the shared " +
                     "training-run tools, then read the result. Needs the Advanced Analytics Training permission.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitTrainingAsync(
            [Description("The rows, optional labels, variable names and options of the isolation forest training run.")]
            AdvancedAnalyticsIsolationForestAnomalyTrainingDto? request,
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

                EnsureInlineWithinLimits(request);

                var arguments = new IsolationForestAnomalyTrainingArguments
                {
                    Rows = request.Inputs!.Length,
                    ModelName = request.ModelName,
                    CorrelationKey = request.CorrelationKey,
                    VariableNames = request.VariableNames,
                    ProcessingTypeIds = request.ProcessingTypeIds,
                    TreeCount = request.TreeCount,
                    SubsampleSize = request.SubsampleSize,
                    ValidationFraction = request.ValidationFraction,
                    TestFraction = request.TestFraction,
                    Seed = request.Seed,
                    Calibrate = request.Calibrate,
                    Inputs = request.Inputs,
                    Labels = request.Labels
                };

                var run = await runs.InsertAsync(new Jube.Data.Poco.AdvancedAnalyticsTrainingRun
                {
                    AdvancedAnalyticsInstanceId = request.AdvancedAnalyticsInstanceId,
                    KindId = AdvancedAnalyticsKind.AnomalyDetector,
                    ModelKindId = AdvancedAnalyticsModelKind.IsolationForestAnomalyPipeline,
                    JobMode = AdvancedAnalyticsJobMode.Batch,
                    Seed = request.Seed,
                    Arguments = arguments.ToJson()
                }, token).ConfigureAwait(false);

                op.Entity(run.Id);
                op.Created();

                runner.Info($"AdvancedAnalyticsTrainedModel.SubmitIsolationForestAnomalyTraining: run {run.Id} " +
                            $"queued rows={arguments.Rows} labelled={request.Labels != null} user={runner.UserName}");

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

        private void EnsureInlineWithinLimits(AdvancedAnalyticsIsolationForestAnomalyTrainingDto request)
        {
            if (request.Inputs!.Length > Settings.InlineMaxRows)
            {
                throw Invalid("Inputs", "InlineTooManyRows", string.Format(
                    runner.Strings[AdvancedAnalyticsTrainingResources.InlineTooManyRows], Settings.InlineMaxRows));
            }

            if (IsolationForestAnomalyTrainingArguments.InlineSizeBytes(request.Inputs, request.Labels)
                > Settings.InlineMaxBytes)
            {
                throw Invalid("Inputs", "InlineTooLarge", string.Format(
                    runner.Strings[AdvancedAnalyticsTrainingResources.InlineTooLarge], Settings.InlineMaxBytes));
            }
        }

        private static DtoValidationException Invalid(string property, string code, string message)
        {
            return new DtoValidationException(new ValidationResult(
            [
                new ValidationFailure(property, message) { ErrorCode = code }
            ]));
        }
    }
}
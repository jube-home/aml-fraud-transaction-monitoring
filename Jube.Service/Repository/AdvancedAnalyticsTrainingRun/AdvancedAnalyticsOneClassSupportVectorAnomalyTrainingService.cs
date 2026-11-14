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
using Jube.Engine.AdvancedAnalytics.Anomaly.Models;
using Jube.Resources;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun
{
    public sealed class AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingService
    {
        private readonly AdvancedAnalyticsOperationRunner runner;
        private readonly AdvancedAnalyticsTrainingRunRepository runs;
        private readonly AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingValidator validator;

        private AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingService(AdvancedAnalyticsOperationRunner runner)
        {
            this.runner = runner;
            var tenant = runner.TenantRegistryId;
            var user = runner.UserName;
            var instances = new AskJoobyAnalyticsInstanceRepository(runner.DbContext, tenant, user);
            runs = new AdvancedAnalyticsTrainingRunRepository(runner.DbContext, tenant, user);

            validator = new AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingValidator(runner.Strings,
                async (id, token) => await instances.GetByIdAsync(id, token).ConfigureAwait(false) != null);
        }

        internal Func<double[][], int[]?, string[], OneClassSupportVectorAnomalyOptions, CancellationToken,
            OneClassSupportVectorAnomalyResult> Trainer { get; set; } = (inputs, labels, names, options, token) =>
            OneClassSupportVectorAnomalyTrainer.Train(inputs, labels, names, options, token);

        internal AdvancedAnalyticsTrainingSettings Settings { get; set; } = AdvancedAnalyticsTrainingSettings.Current;

        public static Task<AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var runner = await AdvancedAnalyticsOperationRunner.CreateAsync(
                AdvancedAnalyticsChangeAreas.TrainedModel, typeof(AdvancedAnalyticsTrainingResources),
                AdvancedAnalyticsTrainingResources.NotAuthenticated, dbContext, userName, log,
                stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false);

            return new AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingService(runner);
        }

        [Description(
            "Queues a one-class support vector anomaly detection training run and returns its run identifier at " +
            "once, before anything is trained. A one-class support vector machine fits a boundary in a reshaped, " +
            "kernel-induced space that wraps as tightly as the Nu hyperparameter allows around the bulk of the " +
            "predictor rows, without ever being shown which rows, if any, are anomalous; the background worker " +
            "stores the rows that end up defining that boundary (the support vectors), their weights and the " +
            "kernel's own parameters inside the model version. Labels are optional and are never used to draw " +
            "the boundary itself: when given, and both a normal and an anomalous row fall in the validation " +
            "split, they are used only to Platt-calibrate the raw score into a probability and to report " +
            "held-out quality; left out, the boundary is still fitted and the raw score is reported as is. Only " +
            "inline rows are supported for this kind. Poll the run status, then read the result. Needs the " +
            "Advanced Analytics Training permission.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitTrainingAsync(
            [Description(
                "The rows, optional labels, variable names and options of the one-class support vector training run.")]
            AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingDto? request,
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

                if (request.Inputs!.Length > Settings.InlineMaxRows)
                {
                    throw Invalid("Inputs", "InlineTooManyRows", string.Format(
                        runner.Strings[AdvancedAnalyticsTrainingResources.InlineTooManyRows], Settings.InlineMaxRows));
                }

                var run = await runs.InsertAsync(new Jube.Data.Poco.AdvancedAnalyticsTrainingRun
                {
                    AskJoobyAnalyticsInstanceId = request.AskJoobyAnalyticsInstanceId,
                    KindId = AdvancedAnalyticsKind.AnomalyDetector,
                    ModelKindId = AdvancedAnalyticsModelKind.OneClassSupportVectorAnomalyPipeline,
                    JobMode = AdvancedAnalyticsJobMode.Batch,
                    Seed = request.Seed,
                    Arguments = ArgumentsOf(request).ToJson()
                }, token).ConfigureAwait(false);

                op.Entity(run.Id);
                op.Created();

                runner.Info($"AskJoobyAnalyticsTrainedModel.SubmitTraining: run {run.Id} queued " +
                            $"kind=AnomalyDetector modelKind={OneClassSupportVectorAnomalyRecall.ModelKind} " +
                            $"rows={request.Inputs!.Length} labelled={request.Labels != null} user={runner.UserName}");

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

        private static DtoValidationException Invalid(string property, string code, string message)
        {
            return new DtoValidationException(new ValidationResult(
            [
                new ValidationFailure(property, message) { ErrorCode = code }
            ]));
        }

        private static OneClassSupportVectorAnomalyTrainingArguments ArgumentsOf(
            AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingDto request)
        {
            return new OneClassSupportVectorAnomalyTrainingArguments
            {
                Rows = request.Inputs!.Length,
                ModelName = request.ModelName,
                CorrelationKey = request.CorrelationKey,
                VariableNames = request.VariableNames,
                ProcessingTypeIds = request.ProcessingTypeIds,
                Inputs = request.Inputs,
                Labels = request.Labels,
                Nu = request.Nu,
                Gamma = request.Gamma,
                ValidationFraction = request.ValidationFraction,
                TestFraction = request.TestFraction,
                Seed = request.Seed,
                Calibrate = request.Calibrate
            };
        }
    }
}
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
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Regression.Models;
using Jube.Resources;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun
{
    public sealed class AdvancedAnalyticsKNearestNeighboursRegressorTrainingService
    {
        private readonly AdvancedAnalyticsOperationRunner runner;
        private readonly AdvancedAnalyticsTrainingRunRepository runs;
        private readonly AdvancedAnalyticsKNearestNeighboursRegressorTrainingValidator validator;

        private AdvancedAnalyticsKNearestNeighboursRegressorTrainingService(AdvancedAnalyticsOperationRunner runner)
        {
            this.runner = runner;
            var tenant = runner.TenantRegistryId;
            var user = runner.UserName;
            var instances = new AskJoobyAnalyticsInstanceRepository(runner.DbContext, tenant, user);
            runs = new AdvancedAnalyticsTrainingRunRepository(runner.DbContext, tenant, user);

            validator = new AdvancedAnalyticsKNearestNeighboursRegressorTrainingValidator(runner.Strings,
                async (id, token) => await instances.GetByIdAsync(id, token).ConfigureAwait(false) != null);
        }

        internal Func<double[][], double[], string[], KNearestNeighboursRegressorOptions, CancellationToken,
            KNearestNeighboursRegressorResult> Trainer { get; set; } = (inputs, outputs, names, options, token) =>
            KNearestNeighboursRegressorTrainer.Train(inputs, outputs, names, options, token);

        internal AdvancedAnalyticsTrainingSettings Settings { get; set; } = AdvancedAnalyticsTrainingSettings.Current;

        public static Task<AdvancedAnalyticsKNearestNeighboursRegressorTrainingService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AdvancedAnalyticsKNearestNeighboursRegressorTrainingService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var runner = await AdvancedAnalyticsOperationRunner.CreateAsync(
                AdvancedAnalyticsChangeAreas.TrainedModel, typeof(AdvancedAnalyticsTrainingResources),
                AdvancedAnalyticsTrainingResources.NotAuthenticated, dbContext, userName, log,
                stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false);

            return new AdvancedAnalyticsKNearestNeighboursRegressorTrainingService(runner);
        }

        [Description(
            "Queues a k nearest neighbours regression training run and returns its run identifier at once, " +
            "before anything is trained. K nearest neighbours regression makes no assumption about the shape of " +
            "the relationship between the predictors and the target: to score a new row it finds the k training " +
            "rows nearest to it on standardised predictors and predicts the plain mean of those neighbours' " +
            "targets, with no calibration of any kind since that mean already is the prediction, on the scale " +
            "the target itself was recorded in. Training itself is really just remembering the rows, so the " +
            "background worker stores every standardised, imputed training row inside the model version; " +
            "recalling this kind of model means searching those stored rows, unlike a family that fits a compact " +
            "set of parameters. Only inline rows are supported for this kind. Poll the run status, then read the " +
            "result. Needs the Advanced Analytics Training permission.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitTrainingAsync(
            [Description(
                "The rows, targets, variable names and options of the k nearest neighbours regression training run.")]
            AdvancedAnalyticsKNearestNeighboursRegressorTrainingDto? request,
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
                    KindId = AdvancedAnalyticsKind.Regressor,
                    ModelKindId = AdvancedAnalyticsModelKind.KNearestNeighboursRegressorPipeline,
                    JobMode = AdvancedAnalyticsJobMode.Batch,
                    Seed = request.Seed,
                    Arguments = ArgumentsOf(request).ToJson()
                }, token).ConfigureAwait(false);

                op.Entity(run.Id);
                op.Created();

                runner.Info($"AskJoobyAnalyticsTrainedModel.SubmitTraining: run {run.Id} queued " +
                            $"kind=Regressor modelKind={KNearestNeighboursRegressorRecall.ModelKind} " +
                            $"rows={request.Inputs!.Length} user={runner.UserName}");

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

        private static KNearestNeighboursRegressorTrainingArguments ArgumentsOf(
            AdvancedAnalyticsKNearestNeighboursRegressorTrainingDto request)
        {
            return new KNearestNeighboursRegressorTrainingArguments
            {
                Rows = request.Inputs!.Length,
                ModelName = request.ModelName,
                CorrelationKey = request.CorrelationKey,
                VariableNames = request.VariableNames,
                ProcessingTypeIds = request.ProcessingTypeIds,
                Inputs = request.Inputs,
                Outputs = request.Outputs,
                NeighbourCount = request.NeighbourCount,
                Standardise = request.Standardise,
                ValidationFraction = request.ValidationFraction,
                TestFraction = request.TestFraction,
                Seed = request.Seed
            };
        }
    }
}
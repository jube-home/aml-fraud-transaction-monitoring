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
    public sealed class AdvancedAnalyticsKernelRidgeRegressorTrainingService
    {
        private readonly AdvancedAnalyticsOperationRunner runner;
        private readonly AdvancedAnalyticsTrainingRunRepository runs;
        private readonly AdvancedAnalyticsKernelRidgeRegressorTrainingValidator validator;

        private AdvancedAnalyticsKernelRidgeRegressorTrainingService(
            AdvancedAnalyticsOperationRunner runner)
        {
            this.runner = runner;
            var tenant = runner.TenantRegistryId;
            var user = runner.UserName;
            var instances = new AskJoobyAnalyticsInstanceRepository(runner.DbContext, tenant, user);
            runs = new AdvancedAnalyticsTrainingRunRepository(runner.DbContext, tenant, user);

            validator = new AdvancedAnalyticsKernelRidgeRegressorTrainingValidator(runner.Strings,
                async (id, token) => await instances.GetByIdAsync(id, token).ConfigureAwait(false) != null);
        }

        internal Func<double[][], double[], string[], KernelRidgeRegressorOptions, CancellationToken,
            KernelRidgeRegressorResult> Trainer { get; set; } = (inputs, outputs, names, options, token) =>
            KernelRidgeRegressorTrainer.Train(inputs, outputs, names, options, token);

        internal AdvancedAnalyticsTrainingSettings Settings { get; set; } = AdvancedAnalyticsTrainingSettings.Current;

        public static Task<AdvancedAnalyticsKernelRidgeRegressorTrainingService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AdvancedAnalyticsKernelRidgeRegressorTrainingService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var runner = await AdvancedAnalyticsOperationRunner.CreateAsync(
                AdvancedAnalyticsChangeAreas.TrainedModel, typeof(AdvancedAnalyticsTrainingResources),
                AdvancedAnalyticsTrainingResources.NotAuthenticated, dbContext, userName, log,
                stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false);

            return new AdvancedAnalyticsKernelRidgeRegressorTrainingService(runner);
        }

        [Description(
            "Queues a kernel ridge regression training run and returns its run identifier at once, before " +
            "anything is trained. Kernel ridge regression fits a continuous outcome by solving a single ridge " +
            "regularised linear system directly against the kernel similarity of every pair of training rows, " +
            "rather than fitting a compact set of coefficients by gradient descent or an iterative solver; the " +
            "kernel reshapes the notion of similarity between two standardised rows first, so a boundary that is " +
            "still a straight ridge fit in that reshaped space corresponds to a curved surface back in the space " +
            "of the original predictors. Because the closed-form solution keeps a weight for every training row, " +
            "the background worker stores every standardised training row together with its fitted weight and " +
            "the kernel's own parameters inside the model version, since recalling this kind of model means " +
            "weighting the kernel similarity of a new row against every one of those stored rows; unlike a " +
            "support vector machine there is no sparsity mechanism that discards rows during fitting, so the " +
            "stored model grows with the size of the training split rather than with a small fixed parameter " +
            "count. Because the notion of similarity between two rows depends directly on their raw numeric " +
            "scale, every predictor is z-scored to the training mean and standard deviation before the kernel " +
            "ever sees it. The regression target is never calibrated: what is recalled is the fitted value " +
            "itself, on the target's own scale. Only inline rows are supported for this kind. Poll the run " +
            "status, then read the result. Needs the Advanced Analytics Training permission.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitTrainingAsync(
            [Description("The rows, outcomes, variable names and options of the kernel ridge regression training run.")]
            AdvancedAnalyticsKernelRidgeRegressorTrainingDto? request,
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
                    ModelKindId = AdvancedAnalyticsModelKind.KernelRidgeRegressorPipeline,
                    JobMode = AdvancedAnalyticsJobMode.Batch,
                    Seed = request.Seed,
                    Arguments = ArgumentsOf(request).ToJson()
                }, token).ConfigureAwait(false);

                op.Entity(run.Id);
                op.Created();

                runner.Info($"AskJoobyAnalyticsTrainedModel.SubmitTraining: run {run.Id} queued " +
                            $"kind=Regressor modelKind={KernelRidgeRegressorRecall.ModelKind} " +
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

        private static KernelRidgeRegressorTrainingArguments ArgumentsOf(
            AdvancedAnalyticsKernelRidgeRegressorTrainingDto request)
        {
            return new KernelRidgeRegressorTrainingArguments
            {
                Rows = request.Inputs!.Length,
                ModelName = request.ModelName,
                CorrelationKey = request.CorrelationKey,
                VariableNames = request.VariableNames,
                ProcessingTypeIds = request.ProcessingTypeIds,
                Inputs = request.Inputs,
                Outputs = request.Outputs,
                Complexity = request.Complexity,
                Kernel = request.Kernel,
                Gamma = request.Gamma,
                Degree = request.Degree,
                ValidationFraction = request.ValidationFraction,
                TestFraction = request.TestFraction,
                Seed = request.Seed
            };
        }
    }
}
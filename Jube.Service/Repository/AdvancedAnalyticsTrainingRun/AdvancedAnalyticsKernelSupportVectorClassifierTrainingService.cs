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
using Jube.Engine.AdvancedAnalytics.Classification;
using Jube.Engine.AdvancedAnalytics.Classification.Models;
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Engine.AdvancedAnalytics.Training.Models;
using Jube.Resources;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun
{
    public sealed class AdvancedAnalyticsKernelSupportVectorClassifierTrainingService
    {
        private readonly AdvancedAnalyticsCacheRepository cache;
        private readonly AdvancedAnalyticsOperationRunner runner;
        private readonly AdvancedAnalyticsTrainingRunRepository runs;
        private readonly AdvancedAnalyticsKernelSupportVectorClassifierTrainingValidator validator;

        private AdvancedAnalyticsKernelSupportVectorClassifierTrainingService(
            AdvancedAnalyticsOperationRunner runner)
        {
            this.runner = runner;
            var tenant = runner.TenantRegistryId;
            var user = runner.UserName;
            var instances = new AskJoobyAnalyticsInstanceRepository(runner.DbContext, tenant, user);
            runs = new AdvancedAnalyticsTrainingRunRepository(runner.DbContext, tenant, user);

            cache = new AdvancedAnalyticsCacheRepository(runner.DbContext, tenant, user,
                new AdvancedAnalyticsCacheLimits());

            validator = new AdvancedAnalyticsKernelSupportVectorClassifierTrainingValidator(runner.Strings,
                async (id, token) => await instances.GetByIdAsync(id, token).ConfigureAwait(false) != null);
        }

        internal Func<double[][], int[], string[], KernelSupportVectorClassifierOptions, CancellationToken,
            KernelSupportVectorClassifierResult> Trainer { get; set; } = (inputs, labels, names, options, token) =>
            KernelSupportVectorClassifierTrainer.Train(inputs, labels, names, options, token);

        internal AdvancedAnalyticsTrainingSettings Settings { get; set; } = AdvancedAnalyticsTrainingSettings.Current;
        internal AdvancedAnalyticsTrainingInputResolver InputResolver { get; set; } = new();

        public static Task<AdvancedAnalyticsKernelSupportVectorClassifierTrainingService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AdvancedAnalyticsKernelSupportVectorClassifierTrainingService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var runner = await AdvancedAnalyticsOperationRunner.CreateAsync(
                AdvancedAnalyticsChangeAreas.TrainedModel, typeof(AdvancedAnalyticsTrainingResources),
                AdvancedAnalyticsTrainingResources.NotAuthenticated, dbContext, userName, log,
                stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false);

            return new AdvancedAnalyticsKernelSupportVectorClassifierTrainingService(runner);
        }

        [Description(
            "Queues a nonlinear-kernel support vector classification training run and returns its run identifier " +
            "at once, before anything is trained. A kernel support vector machine fits the maximum-margin " +
            "boundary that best separates the two classes, not in the space of the raw predictors but in a " +
            "reshaped space implied by the chosen kernel, which lets the boundary bend around clusters that a " +
            "straight line or plane could never separate. Fitting keeps only the training rows that end up " +
            "sitting on or inside the margin (the support vectors), together with a weight for each; the " +
            "background worker stores those support vectors, their weights and the kernel's own parameters " +
            "inside the model version, since recalling this kind of model means weighting the kernel similarity " +
            "of a new row against every stored support vector, unlike a family that fits a compact set of " +
            "parameters. The raw decision function is a signed distance to the boundary in the reshaped space, " +
            "not a probability, so it is Platt-calibrated into a probability on the validation rows. The rows may " +
            "be inline (up to a configured cap) or held in the Advanced Analytics cache and named by a cache " +
            "pointer. Poll the run status, then read the result. Needs the Advanced Analytics Training " +
            "permission.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitTrainingAsync(
            [Description("The rows, labels, variable names and options of the kernel support vector training run.")]
            AdvancedAnalyticsKernelSupportVectorClassifierTrainingDto? request,
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
                        EnsureInlineWithinLimits(request);
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
                    AskJoobyAnalyticsInstanceId = request.AskJoobyAnalyticsInstanceId,
                    KindId = AdvancedAnalyticsKind.Classifier,
                    ModelKindId = AdvancedAnalyticsModelKind.KernelSupportVectorClassifierPipeline,
                    JobMode = AdvancedAnalyticsJobMode.Batch,
                    Seed = request.Seed,
                    InputPointer = pointer,
                    Arguments = ArgumentsOf(request, rows, input).ToJson()
                }, token).ConfigureAwait(false);

                op.Entity(run.Id);
                op.Created();

                runner.Info($"AskJoobyAnalyticsTrainedModel.SubmitTraining: run {run.Id} queued " +
                            $"kind=Classifier modelKind={KernelSupportVectorClassifierRecall.ModelKind} " +
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

        private void EnsureInlineWithinLimits(AdvancedAnalyticsKernelSupportVectorClassifierTrainingDto request)
        {
            if (request.Inputs!.Length > Settings.InlineMaxRows)
            {
                throw Invalid("Inputs", "InlineTooManyRows", string.Format(
                    runner.Strings[AdvancedAnalyticsTrainingResources.InlineTooManyRows], Settings.InlineMaxRows));
            }
        }

        private async Task<int> EnsureCacheInputAsync(
            AdvancedAnalyticsKernelSupportVectorClassifierTrainingDto request,
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

        private static KernelSupportVectorClassifierTrainingArguments ArgumentsOf(
            AdvancedAnalyticsKernelSupportVectorClassifierTrainingDto request, int rows,
            AdvancedAnalyticsTrainingInput input)
        {
            return new KernelSupportVectorClassifierTrainingArguments
            {
                Rows = rows,
                ModelName = request.ModelName,
                CorrelationKey = request.CorrelationKey,
                VariableNames = request.VariableNames,
                ProcessingTypeIds = request.ProcessingTypeIds,
                Complexity = request.Complexity,
                Kernel = request.Kernel,
                Gamma = request.Gamma,
                Degree = request.Degree,
                ValidationFraction = request.ValidationFraction,
                TestFraction = request.TestFraction,
                Seed = request.Seed,
                Calibrate = request.Calibrate,
                DecisionThreshold = request.DecisionThreshold,
                Inputs = input.Inputs,
                Labels = input.Labels,
                Input = input
            };
        }
    }
}
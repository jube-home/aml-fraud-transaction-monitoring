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
using Jube.Engine.AdvancedAnalytics.Clustering;
using Jube.Resources;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun
{
    public sealed class AdvancedAnalyticsBalancedKMeansClustererTrainingService
    {
        private readonly AdvancedAnalyticsOperationRunner runner;
        private readonly AdvancedAnalyticsTrainingRunRepository runs;
        private readonly AdvancedAnalyticsBalancedKMeansClustererTrainingValidator validator;

        private AdvancedAnalyticsBalancedKMeansClustererTrainingService(AdvancedAnalyticsOperationRunner runner)
        {
            this.runner = runner;
            var tenant = runner.TenantRegistryId;
            var user = runner.UserName;
            var instances = new AskJoobyAnalyticsInstanceRepository(runner.DbContext, tenant, user);
            runs = new AdvancedAnalyticsTrainingRunRepository(runner.DbContext, tenant, user);

            validator = new AdvancedAnalyticsBalancedKMeansClustererTrainingValidator(runner.Strings,
                async (id, token) => await instances.GetByIdAsync(id, token).ConfigureAwait(false) != null);
        }

        internal AdvancedAnalyticsTrainingSettings Settings { get; set; } = AdvancedAnalyticsTrainingSettings.Current;

        public static Task<AdvancedAnalyticsBalancedKMeansClustererTrainingService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AdvancedAnalyticsBalancedKMeansClustererTrainingService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var runner = await AdvancedAnalyticsOperationRunner.CreateAsync(
                AdvancedAnalyticsChangeAreas.TrainedModel, typeof(AdvancedAnalyticsTrainingResources),
                AdvancedAnalyticsTrainingResources.NotAuthenticated, dbContext, userName, log,
                stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false);

            return new AdvancedAnalyticsBalancedKMeansClustererTrainingService(runner);
        }

        [Description("Queues a balanced k-means clustering training run and returns its run identifier at once, " +
                     "before anything is trained. A background worker in the engine picks the run up, fits " +
                     "clusters of close to equal size, records the steps and evidence and stores the model as a " +
                     "new immutable version that is never made live by training. Only inline rows are supported " +
                     "for this kind, and the row count is capped well below plain k-means because the balanced " +
                     "assignment step costs grow with the cube of the row count. Poll the run status with the " +
                     "shared training-run tools, then read the result. Needs the Advanced Analytics Training " +
                     "permission.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitTrainingAsync(
            [Description("The rows, variable names and options of the balanced clustering training run.")]
            AdvancedAnalyticsBalancedKMeansClustererTrainingDto? request,
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

                var arguments = new BalancedKMeansClustererTrainingArguments
                {
                    Rows = request.Inputs!.Length,
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
                };

                var run = await runs.InsertAsync(new Jube.Data.Poco.AdvancedAnalyticsTrainingRun
                {
                    AskJoobyAnalyticsInstanceId = request.AskJoobyAnalyticsInstanceId,
                    KindId = AdvancedAnalyticsKind.Clusterer,
                    ModelKindId = AdvancedAnalyticsModelKind.BalancedKMeansClustererPipeline,
                    JobMode = AdvancedAnalyticsJobMode.Batch,
                    Seed = request.Seed,
                    Arguments = arguments.ToJson()
                }, token).ConfigureAwait(false);

                op.Entity(run.Id);
                op.Created();

                runner.Info($"AskJoobyAnalyticsTrainedModel.SubmitBalancedKMeansClustererTraining: run {run.Id} " +
                            $"queued rows={arguments.Rows} user={runner.UserName}");

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
    }
}
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
using Jube.Data.Context;
using Jube.Data.Helpers;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Regression.Models;
using Jube.Resources;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Observability;
using Jube.Service.Query.AdvancedAnalyticsChart;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.AskJoobyAnalyticsModel.Models;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.AskJoobyAnalyticsModel
{
    public sealed class AskJoobyAnalyticsModelService
    {
        private const int MaxListTake = 200;
        private readonly AskJoobyAnalyticsInstanceRepository instances;
        private readonly AskJoobyAnalyticsTrainedModelRepository models;
        private readonly AdvancedAnalyticsTrainingRunCurvePointRepository runCurves;
        private readonly AdvancedAnalyticsOperationRunner runner;
        private readonly AskJoobyAnalyticsTrainedModelVersionRepository versions;

        private AskJoobyAnalyticsModelService(AdvancedAnalyticsOperationRunner runner)
        {
            this.runner = runner;

            instances = new AskJoobyAnalyticsInstanceRepository(runner.DbContext, runner.TenantRegistryId,
                runner.UserName);
            models = new AskJoobyAnalyticsTrainedModelRepository(runner.DbContext, runner.TenantRegistryId,
                runner.UserName);
            versions = new AskJoobyAnalyticsTrainedModelVersionRepository(runner.DbContext,
                runner.TenantRegistryId, runner.UserName);
            runCurves = new AdvancedAnalyticsTrainingRunCurvePointRepository(runner.DbContext,
                runner.TenantRegistryId, runner.UserName);
        }

        public static Task<AskJoobyAnalyticsModelService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AskJoobyAnalyticsModelService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var runner = await AdvancedAnalyticsOperationRunner.CreateAsync(
                AdvancedAnalyticsChangeAreas.TrainedModel, typeof(AskJoobyAnalyticsModelResources),
                AskJoobyAnalyticsModelResources.NotAuthenticated, dbContext, userName, log,
                stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false);

            return new AskJoobyAnalyticsModelService(runner);
        }

        [Description("Lists the caller's trained models ordered by identifier, at most 'take' at a time " +
                     "(maximum 200), optionally only those of one AskJooby Analytics instance. Each row says " +
                     "which version, if any, is live. When more remain, call again with 'afterId' set to the last " +
                     "identifier returned.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AskJoobyAnalyticsModelList", OperationKind.Read, Idempotent = true)]
        public Task<PagedResult<AskJoobyAnalyticsTrainedModelDto>> ListModelsAsync(
            [Description("Maximum number of rows to return; clamped to between 1 and 200.")]
            int take = 50,
            [Description("When set, only models with an identifier above this value are returned.")]
            int? afterId = null,
            [Description("When set, only models of this AskJooby Analytics instance are returned.")]
            int? askJoobyAnalyticsInstanceId = null,
            CancellationToken token = default)
        {
            return runner.RunAsync("ListModels", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "ListModels");

                var page = (await models.ListAsync(askJoobyAnalyticsInstanceId, token).ConfigureAwait(false))
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .Take(Math.Clamp(take, 1, MaxListTake))
                    .ToList();

                var result = new List<AskJoobyAnalyticsTrainedModelDto>();

                foreach (var model in page)
                {
                    var active = model.ActiveVersionId.HasValue
                        ? await versions.GetByIdAsync(model.ActiveVersionId.Value, token).ConfigureAwait(false)
                        : null;

                    result.Add(AskJoobyAnalyticsModelMapper.ToDto(model, active, null));
                }

                op.Rows(result.Count);

                return new PagedResult<AskJoobyAnalyticsTrainedModelDto>(result);
            });
        }

        [Description("Returns one trained model with all of its versions, newest first, each with its headline " +
                     "held-out metrics and warning codes. Returns null when the model does not exist or belongs " +
                     "to another tenant.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AskJoobyAnalyticsModelGet", OperationKind.Read, Idempotent = true)]
        public Task<AskJoobyAnalyticsTrainedModelDto?> GetModelAsync(
            [Description("Numeric identifier of the trained model.")]
            int trainedModelId,
            CancellationToken token = default)
        {
            return runner.RunAsync("GetModel", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "GetModel");
                var model = await models.GetByIdAsync(trainedModelId, token).ConfigureAwait(false);

                if (model == null)
                {
                    return null;
                }

                var list = (await versions.ListByModelIdAsync(model.Id, token).ConfigureAwait(false)).ToList();
                op.Entity(model.Id);
                op.Rows(list.Count);

                return AskJoobyAnalyticsModelMapper.ToDto(model,
                    list.FirstOrDefault(v => v.Id == model.ActiveVersionId), list);
            });
        }

        [Description("Lists the versions of a trained model, newest first, each with its headline held-out " +
                     "metrics and warning codes but without the full evidence.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AskJoobyAnalyticsModelListVersions", OperationKind.Read, Idempotent = true)]
        public Task<List<AskJoobyAnalyticsModelVersionDto>> ListVersionsAsync(
            [Description("Numeric identifier of the trained model.")]
            int trainedModelId,
            CancellationToken token = default)
        {
            return runner.RunAsync("ListVersions", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "ListVersions");

                var model = await models.GetByIdAsync(trainedModelId, token).ConfigureAwait(false)
                            ?? throw ModelNotFound();

                var list = (await versions.ListByModelIdAsync(model.Id, token).ConfigureAwait(false))
                    .Select(v => AskJoobyAnalyticsModelMapper.ToDto(v, model.ActiveVersionId, false))
                    .ToList();

                op.Entity(model.Id);
                op.Rows(list.Count);

                return list;
            });
        }

        [Description("Returns one model version with its complete stored evidence: held-out metrics, " +
                     "coefficients with significance, calibration, split sizes, seed and warnings. Returns null " +
                     "when the version does not exist or belongs to another tenant.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AskJoobyAnalyticsModelGetVersion", OperationKind.Read, Idempotent = true)]
        public Task<AskJoobyAnalyticsModelVersionDto?> GetVersionAsync(
            [Description("Numeric identifier of the version.")]
            int versionId,
            CancellationToken token = default)
        {
            return runner.RunAsync("GetVersion", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "GetVersion");
                var version = await versions.GetByIdAsync(versionId, token).ConfigureAwait(false);

                var model = version == null
                    ? null
                    : await models.GetByIdAsync(version.AskJoobyAnalyticsTrainedModelId, token)
                        .ConfigureAwait(false);

                if (version == null || model == null)
                {
                    return null;
                }

                op.Entity(version.Id);

                return AskJoobyAnalyticsModelMapper.ToDto(version, model.ActiveVersionId, true);
            });
        }

        [Description("Compares two model versions on their held-out test figures, side by side with the " +
                     "difference, and says whether the comparison is fair: same seed, same split sizes and same " +
                     "predictors. Also lists the warnings only one of them has.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AskJoobyAnalyticsModelCompareVersions", OperationKind.Read, Idempotent = true)]
        public Task<AdvancedAnalyticsVersionComparisonDto> CompareVersionsAsync(
            [Description("Identifier of the first version.")]
            int versionIdA,
            [Description("Identifier of the second version, which must differ from the first.")]
            int versionIdB,
            CancellationToken token = default)
        {
            return runner.RunAsync("CompareVersions", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "CompareVersions");

                if (versionIdA == versionIdB)
                {
                    throw new DtoValidationException(new ValidationResult(
                    [
                        new ValidationFailure("versionIdB",
                            runner.Strings[AskJoobyAnalyticsModelResources.VersionIdsInvalid])
                        {
                            ErrorCode = "VersionIdsInvalid"
                        }
                    ]));
                }

                var a = await LoadVersionAsync(versionIdA, token).ConfigureAwait(false);
                var b = await LoadVersionAsync(versionIdB, token).ConfigureAwait(false);
                var dtoA = AskJoobyAnalyticsModelMapper.ToDto(a.Version, a.Model.ActiveVersionId, true);
                var dtoB = AskJoobyAnalyticsModelMapper.ToDto(b.Version, b.Model.ActiveVersionId, true);
                var evidenceA = dtoA.Evidence;
                var evidenceB = dtoB.Evidence;
                dtoA.Evidence = null;
                dtoB.Evidence = null;

                var iEvidenceA = evidenceA as IAdvancedAnalyticsEvidence;
                var iEvidenceB = evidenceB as IAdvancedAnalyticsEvidence;

                var comparison = new AdvancedAnalyticsVersionComparisonDto
                {
                    VersionA = dtoA,
                    VersionB = dtoB,
                    SameSeed = iEvidenceA != null && iEvidenceB != null && iEvidenceA.Seed == iEvidenceB.Seed,
                    SameSplitSizes = iEvidenceA?.CommonSplitSizes != null && iEvidenceB?.CommonSplitSizes != null
                                                                          && iEvidenceA.CommonSplitSizes ==
                                                                          iEvidenceB.CommonSplitSizes,
                    SameVariables = iEvidenceA != null && iEvidenceB != null
                                                       && iEvidenceA.CommonVariables.SequenceEqual(
                                                           iEvidenceB.CommonVariables),
                    Metrics = MetricComparisons(evidenceA as Models.AdvancedAnalyticsLogisticEvidence,
                        evidenceB as Models.AdvancedAnalyticsLogisticEvidence),
                    WarningsOnlyInA = dtoA.WarningCodes.Except(dtoB.WarningCodes).ToArray(),
                    WarningsOnlyInB = dtoB.WarningCodes.Except(dtoA.WarningCodes).ToArray()
                };

                op.Rows(2);

                return comparison;
            });
        }

        [Description("Returns exactly the evidence needed to decide whether a model version deserves to go live: " +
                     "held-out test metrics, train metrics to expose overfitting, calibration diagnostics, " +
                     "coefficient p-values, every warning, split sizes, the seed, and whether the owning instance " +
                     "is allocated to a Model. Promotion is the caller's judgement; the service only enforces " +
                     "permission and validity.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AskJoobyAnalyticsModelGetPromotionEvidence", OperationKind.Read, Idempotent = true)]
        public Task<object> GetPromotionEvidenceAsync(
            [Description("Numeric identifier of the model version.")]
            int versionId,
            CancellationToken token = default)
        {
            return runner.RunAsync("GetPromotionEvidence", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "GetPromotionEvidence");
                var loaded = await LoadVersionAsync(versionId, token).ConfigureAwait(false);

                var (evidence, _, _) = await BuildEvidenceAsync(loaded.Model, loaded.Version, token)
                    .ConfigureAwait(false);

                op.Entity(loaded.Version.Id);

                return evidence;
            });
        }

        [Description("Makes a model version the live one, so the Model its instance is allocated to serves it. " +
                     "Requires the instance to be allocated to an existing Jube Model. The evidence stored on the " +
                     "version is recorded in the audit trail and returned. The service applies no quality gate: " +
                     "read the evidence first and refuse to promote a version with junk statistics. Needs the " +
                     "Promote Model permission.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.PromoteModel)]
        [ServiceOperation("AskJoobyAnalyticsModelPromoteVersion", OperationKind.Write, Idempotent = true)]
        public Task<AdvancedAnalyticsPromotionResultDto> PromoteVersionAsync(
            [Description("Numeric identifier of the trained model.")]
            int trainedModelId,
            [Description("Numeric identifier of the version of that model to make live.")]
            int versionId,
            CancellationToken token = default)
        {
            return runner.RunAsync("PromoteVersion", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.promoteModelOnly, "PromoteVersion");

                var model = await models.GetByIdAsync(trainedModelId, token).ConfigureAwait(false)
                            ?? throw ModelNotFound();

                var version = await versions.GetByIdAsync(versionId, token).ConfigureAwait(false);

                if (version == null || version.AskJoobyAnalyticsTrainedModelId != model.Id)
                {
                    throw VersionNotFound();
                }

                return await ActivateAsync(op, "PromoteVersion", model, version, token).ConfigureAwait(false);
            });
        }

        [Description("Puts a model back on an earlier version: the one named, or when none is named the newest " +
                     "version older than the live one. The same allocation requirement and audit of the target's " +
                     "evidence apply as for promotion. Needs the Promote Model permission.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.PromoteModel)]
        [ServiceOperation("AskJoobyAnalyticsModelRollback", OperationKind.Write, Idempotent = true)]
        public Task<AdvancedAnalyticsPromotionResultDto> RollbackAsync(
            [Description("Numeric identifier of the trained model.")]
            int trainedModelId,
            [Description("Version to return to. Leave empty to return to the next older version.")]
            int? targetVersionId = null,
            CancellationToken token = default)
        {
            return runner.RunAsync("Rollback", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.promoteModelOnly, "Rollback");

                var model = await models.GetByIdAsync(trainedModelId, token).ConfigureAwait(false)
                            ?? throw ModelNotFound();

                if (!model.ActiveVersionId.HasValue)
                {
                    throw new ConflictException(
                        runner.Strings[AskJoobyAnalyticsModelResources.ModelHasNoActiveVersion],
                        "ModelHasNoActiveVersion");
                }

                var all = (await versions.ListByModelIdAsync(model.Id, token).ConfigureAwait(false)).ToList();

                var active = all.FirstOrDefault(v => v.Id == model.ActiveVersionId.Value)
                             ?? throw VersionNotFound();

                var targetSummary = targetVersionId.HasValue
                    ? all.FirstOrDefault(v => v.Id == targetVersionId.Value) ?? throw VersionNotFound()
                    : all.Where(v => v.VersionNumber < active.VersionNumber)
                          .OrderByDescending(v => v.VersionNumber)
                          .FirstOrDefault()
                      ?? throw new ConflictException(
                          runner.Strings[AskJoobyAnalyticsModelResources.NoEarlierVersion], "NoEarlierVersion");

                var target = await versions.GetByIdAsync(targetSummary.Id, token).ConfigureAwait(false)
                             ?? throw VersionNotFound();

                return await ActivateAsync(op, "Rollback", model, target, token).ConfigureAwait(false);
            });
        }

        private async Task<AdvancedAnalyticsPromotionResultDto> ActivateAsync(OperationScope op, string operation,
            AskJoobyAnalyticsTrainedModel model, AskJoobyAnalyticsTrainedModelVersion version,
            CancellationToken token)
        {
            var (evidence, instanceAllocated, allocatedEntityAnalysisModelId) =
                await BuildEvidenceAsync(model, version, token).ConfigureAwait(false);

            if (!instanceAllocated)
            {
                throw new ConflictException(
                    runner.Strings[AskJoobyAnalyticsModelResources.InstanceNotAllocated], "InstanceNotAllocated");
            }

            var previous = model.ActiveVersionId;
            var allocated = allocatedEntityAnalysisModelId!.Value;

            if (model.Active == 1 && previous == version.Id)
            {
                op.Entity(model.Id);

                return ResultOf(model, version, previous, allocated, false, evidence);
            }

            try
            {
                await models.SetActiveVersionAsync(model.Id, version.Id, token).ConfigureAwait(false);
                await models.SetActiveAsync(model.Id, true, token).ConfigureAwait(false);
            }
            catch (KeyNotFoundException ex)
            {
                throw new NotFoundException(runner.Strings[AskJoobyAnalyticsModelResources.VersionNotFound], ex);
            }

            var updated = await models.GetByIdAsync(model.Id, token).ConfigureAwait(false) ?? model;

            if (runner.AuditLog.IsInfoEnabled)
            {
                runner.AuditLog.Info(
                    $"area={runner.Area} op={operation}Evidence actor={runner.UserName} " +
                    $"tenant={runner.TenantRegistryId} model={model.Id} version={version.Id} " +
                    $"previousVersion={previous?.ToString() ?? "-"} entityAnalysisModel={allocated} " +
                    $"evidence={AdvancedAnalyticsEvidenceJson.Serialise(evidence)}");
            }

            op.Entity(model.Id);
            op.Version(updated.Version);
            op.Updated();

            runner.Info($"AskJoobyAnalyticsTrainedModel.{operation}: model {model.Id} now serves version " +
                        $"{version.Id} (was {previous?.ToString() ?? "none"}) user={runner.UserName}");

            return ResultOf(updated, version, previous, allocated, true, evidence);
        }

        private static AdvancedAnalyticsPromotionResultDto ResultOf(AskJoobyAnalyticsTrainedModel model,
            AskJoobyAnalyticsTrainedModelVersion version, int? previous, int allocated, bool changed,
            object evidence)
        {
            return new AdvancedAnalyticsPromotionResultDto
            {
                TrainedModelId = model.Id,
                ActiveVersionId = version.Id,
                ActiveVersionNumber = version.VersionNumber,
                PreviousActiveVersionId = previous,
                AskJoobyAnalyticsInstanceId = model.AskJoobyAnalyticsInstanceId,
                EntityAnalysisModelId = allocated,
                Changed = changed,
                Evidence = evidence
            };
        }

        private async Task<(object Evidence, bool InstanceAllocated, int? AllocatedEntityAnalysisModelId)>
            BuildEvidenceAsync(AskJoobyAnalyticsTrainedModel model, AskJoobyAnalyticsTrainedModelVersion version,
                CancellationToken token)
        {
            var evidence = AskJoobyAnalyticsModelMapper.ReadEvidence(version.MetricsJson)
                           ?? throw new ConflictException(
                               runner.Strings[AskJoobyAnalyticsModelResources.EvidenceUnavailable],
                               "EvidenceUnavailable");

            var instance = await instances.GetByIdAsync(model.AskJoobyAnalyticsInstanceId, token)
                .ConfigureAwait(false);

            var allocated = instance?.EntityAnalysisModelId;

            if (allocated.HasValue && !await EntityAnalysisModelParentGuard
                    .IsVisibleAsync(runner.DbContext, runner.TenantRegistryId, allocated.Value, token)
                    .ConfigureAwait(false))
            {
                throw new ConflictException(
                    runner.Strings[AskJoobyAnalyticsModelResources.AllocatedModelUnavailable],
                    "AllocatedModelUnavailable");
            }

            var dto = AskJoobyAnalyticsModelMapper.ToPromotionEvidence(model, version, evidence, allocated);

            return (dto, allocated.HasValue, allocated);
        }

        private async Task<(AskJoobyAnalyticsTrainedModel Model, AskJoobyAnalyticsTrainedModelVersion Version)>
            LoadVersionAsync(int versionId, CancellationToken token)
        {
            var version = await versions.GetByIdAsync(versionId, token).ConfigureAwait(false)
                          ?? throw VersionNotFound();

            var model = await models.GetByIdAsync(version.AskJoobyAnalyticsTrainedModelId, token)
                            .ConfigureAwait(false)
                        ?? throw VersionNotFound();

            return (model, version);
        }

        private static List<AdvancedAnalyticsMetricComparisonDto> MetricComparisons(
            Models.AdvancedAnalyticsLogisticEvidence? a, Models.AdvancedAnalyticsLogisticEvidence? b)
        {
            var selectors = new (string Name, Func<LogisticSplitMetrics, double?> Select)[]
            {
                ("TestRocAuc", m => m.RocAuc),
                ("TestAveragePrecision", m => m.AveragePrecision),
                ("TestLogLoss", m => m.LogLoss),
                ("TestBrierScore", m => m.BrierScore),
                ("TestExpectedCalibrationError", m => m.ExpectedCalibrationError),
                ("TestCalibrationSlope", m => m.CalibrationSlope),
                ("TestMatthewsCorrelation", m => m.MatthewsCorrelation),
                ("TestPrecision", m => m.Precision),
                ("TestRecall", m => m.Recall)
            };

            return selectors.Select(s =>
            {
                var valueA = a?.TestMetrics == null ? null : s.Select(a.TestMetrics);
                var valueB = b?.TestMetrics == null ? null : s.Select(b.TestMetrics);

                return new AdvancedAnalyticsMetricComparisonDto
                {
                    Name = s.Name,
                    ValueA = valueA,
                    ValueB = valueB,
                    Difference = valueA.HasValue && valueB.HasValue ? valueB - valueA : null
                };
            }).ToList();
        }

        internal Task<AdvancedAnalyticsChartSource?> GetChartSourceAsync(int versionId, CancellationToken token)
        {
            return runner.RunAsync("GetChartSource", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "GetChartSource");
                var version = await versions.GetByIdAsync(versionId, token).ConfigureAwait(false);

                var model = version == null
                    ? null
                    : await models.GetByIdAsync(version.AskJoobyAnalyticsTrainedModelId, token)
                        .ConfigureAwait(false);

                if (version == null || model == null)
                {
                    return null;
                }

                op.Entity(version.Id);
                var evidence = AskJoobyAnalyticsModelMapper.ReadEvidence(version.MetricsJson);
                var roc = new List<AdvancedAnalyticsRocPoint>();

                if (version.AdvancedAnalyticsTrainingRunId.HasValue)
                {
                    roc = (await runCurves.ListByRunIdAsync(version.AdvancedAnalyticsTrainingRunId.Value,
                            AdvancedAnalyticsCurve.Roc, AdvancedAnalyticsSplit.Test, token).ConfigureAwait(false))
                        .Where(p => p.X.HasValue && p.Y.HasValue)
                        .Select(p => new AdvancedAnalyticsRocPoint(p.X!.Value, p.Y!.Value, p.Threshold))
                        .ToList();
                }

                return new AdvancedAnalyticsChartSource(model.Name, version.VersionNumber, version.ModelJson,
                    version.MetricsJson, evidence, roc, version.VariablesJson);
            });
        }

        private void Ensure(int[] specifications, string operation)
        {
            runner.EnsurePermitted(specifications, operation, AskJoobyAnalyticsModelResources.PermissionDenied);
        }

        private NotFoundException ModelNotFound()
        {
            return new NotFoundException(runner.Strings[AskJoobyAnalyticsModelResources.ModelNotFound]);
        }

        private NotFoundException VersionNotFound()
        {
            return new NotFoundException(runner.Strings[AskJoobyAnalyticsModelResources.VersionNotFound]);
        }
    }
}
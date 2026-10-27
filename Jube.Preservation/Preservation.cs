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

namespace Jube.Preservation
{
    using Cryptography;
    using Cryptography.Exceptions;
    using Data.Context;
    using Data.Poco;
    using Data.Query.ApproveEntireModelQuery;
    using Data.Query.RecoverOrphanedUsersQuery;
    using Data.Repository;
    using Data.Repository.Models;
    using Exceptions;
    using LinqToDB;
    using System.Text;
    using Newtonsoft.Json;
    using MessagePack;
    using Models;

    public class Preservation(DbContext dbContext, string userName) : IAsyncDisposable
    {
        private static readonly JsonSerializerSettings BodyJsonSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public const byte BeforeImportSource = 4;

        private static readonly ExportOptions BeforeImportOptions = new()
        {
            Exhaustive = true,
            Lists = true,
            Dictionaries = true,
            Visualisations = true,
            Roles = true,
            AdvancedAnalytics = true
        };

        public ValueTask DisposeAsync()
        {
            return dbContext.DisposeAsync();
        }

        public async Task<PreservationSnapshot> SnapshotAsync(ExportOptions options, byte snapshotSourceId,
            string? name = null, CancellationToken token = default)
        {
            var snapshotRepository = new PreservationSnapshotRepository(dbContext, userName);

            var snapshot = new PreservationSnapshot
            {
                Guid = Guid.NewGuid(),
                SnapshotSourceId = snapshotSourceId,
                Name = name,
                CreatedDate = DateTime.UtcNow
            };

            snapshot = await snapshotRepository.InsertAsync(snapshot, token).ConfigureAwait(false);

            try
            {
                var payload = await ExportPayloadAsync(options, snapshot.TenantRegistryId, token)
                    .ConfigureAwait(false);

                var wrapper = new Wrapper
                {
                    Guid = snapshot.Guid,
                    Version = 2,
                    Payload = payload
                };

                var json = JsonConvert.SerializeObject(wrapper, Formatting.Indented, BodyJsonSettings);

                snapshot.Json = json;
                snapshot.ExportVersion = wrapper.Version;
                snapshot.ExportGuid = wrapper.Guid;
                snapshot.EntityAnalysisModelCount = payload.EntityAnalysisModel?.Count() ?? 0;
                snapshot.Bytes = Encoding.UTF8.GetByteCount(json);
                snapshot.CompletedDate = DateTime.UtcNow;

                await snapshotRepository.UpdateAsync(snapshot, token).ConfigureAwait(false);

                return snapshot;
            }
            catch (Exception ex)
            {
                snapshot.InError = 1;
                snapshot.ErrorStack = ex.ToString();
                await snapshotRepository.UpdateAsync(snapshot, CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }

        public async Task<PreservationSnapshot> ImportSnapshotAsync(int id, CancellationToken token = default)
        {
            var snapshotRepository = new PreservationSnapshotRepository(dbContext, userName);
            var snapshot = await snapshotRepository.GetByIdAsync(id, token).ConfigureAwait(false);

            if (snapshot == null)
            {
                throw new KeyNotFoundException();
            }

            if (string.IsNullOrEmpty(snapshot.Json))
            {
                throw new InvalidOperationException("The snapshot holds no body and cannot be imported.");
            }

            await ApplyWrapperAsync(null,
                () => JsonConvert.DeserializeObject<Wrapper>(snapshot.Json, BodyJsonSettings)
                      ?? throw new InvalidOperationException("The snapshot body could not be read."),
                token).ConfigureAwait(false);

            return snapshot;
        }

        public Task ImportAsync(byte[] bytes, ImportOptions options, CancellationToken token = default)
        {
            return ApplyWrapperAsync(bytes, () => ReadPackage(bytes, options), token);
        }

        private async Task ApplyWrapperAsync(byte[]? bytes, Func<Wrapper> read,
            CancellationToken token = default)
        {
            var importRepository = new ImportRepository(dbContext, userName);

            var import = new Import
            {
                Bytes = bytes,
                CreatedDate = DateTime.UtcNow,
                Guid = Guid.NewGuid()
            };

            import = await importRepository.InsertAsync(import, token);

            try
            {
                var wrapper = read();

                await SnapshotAsync(BeforeImportOptions, BeforeImportSource, "Before import of " + wrapper.Guid,
                    token).ConfigureAwait(false);

                await using var transaction = await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);

                var roleRegistryRepository = new RoleRegistryRepository(dbContext, import.TenantRegistryId);
                var roleRegistryAcrossAllTenants = await roleRegistryRepository.GetAllTenantsAsync(token);

                await roleRegistryRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId,
                    import.Id, token);

                var roleRegistryPermissionRepository =
                    new RoleRegistryPermissionRepository(dbContext, import.TenantRegistryId);

                await roleRegistryPermissionRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelRepository =
                    new EntityAnalysisModelRepository(dbContext, import.TenantRegistryId);

                var entityAnalysisModelsAcrossAllTenants =
                    await entityAnalysisModelRepository.GetAllTenantsAsync(token);

                await entityAnalysisModelRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelRequestXPathRepository =
                    new EntityAnalysisModelRequestXpathRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelRequestXPathRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelInlineFunctionRepository =
                    new EntityAnalysisModelInlineFunctionRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelInlineFunctionRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelInlineScriptRepository =
                    new EntityAnalysisModelInlineScriptRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelInlineScriptRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelGatewayRuleRepository =
                    new EntityAnalysisModelGatewayRuleRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelGatewayRuleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelSanctionRepository =
                    new EntityAnalysisModelSanctionRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelSanctionRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelTagRepository =
                    new EntityAnalysisModelTagRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelTagRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelResponseElevationRangeRepository =
                    new EntityAnalysisModelResponseElevationRangeRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelResponseElevationRangeRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelTtlCounterRepository =
                    new EntityAnalysisModelTtlCounterRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelTtlCounterRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelAbstractionRuleRepository =
                    new EntityAnalysisModelAbstractionRuleRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelAbstractionRuleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelAbstractionCalculationRepository =
                    new EntityAnalysisModelAbstractionCalculationRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelAbstractionCalculationRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId,
                        import.Id, token);

                var entityAnalysisModelHttpAdaptationRepository =
                    new EntityAnalysisModelHttpAdaptationRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelHttpAdaptationRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelActivationRuleRepository =
                    new EntityAnalysisModelActivationRuleRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelActivationRuleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var caseWorkflowRepository = new CaseWorkflowRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId,
                    import.Id, token);

                var caseWorkflowXPathRepository = new CaseWorkflowXPathRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowXPathRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var caseWorkflowStatusRepository = new CaseWorkflowStatusRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowStatusRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var caseWorkflowFormRepository = new CaseWorkflowFormRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowFormRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId,
                    import.Id, token);

                var caseWorkflowActionRepository = new CaseWorkflowActionRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowActionRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var caseWorkflowDisplayRepository =
                    new CaseWorkflowDisplayRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowDisplayRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var caseWorkflowMacro = new CaseWorkflowMacroRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowMacro.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId,
                    import.Id, token);

                var caseWorkflowFilterRepository = new CaseWorkflowFilterRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowFilterRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var exhaustiveSearchInstanceRepository =
                    new ExhaustiveSearchInstanceRepository(dbContext, import.TenantRegistryId);

                await exhaustiveSearchInstanceRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token)
                    .ConfigureAwait(false);

                var exhaustiveSearchInstanceDataRepository =
                    new ExhaustiveSearchInstanceDataRepository(dbContext);

                await exhaustiveSearchInstanceDataRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceCategoricalFieldRepository =
                    new ExhaustiveSearchInstanceCategoricalFieldRepository(dbContext);

                await exhaustiveSearchInstanceCategoricalFieldRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId,
                        import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceTrialInstanceRepository =
                    new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext);

                await exhaustiveSearchInstanceTrialInstanceRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceVariableRepository =
                    new ExhaustiveSearchInstanceVariableRepository(dbContext);

                await exhaustiveSearchInstanceVariableRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceTrialInstanceVariableRepository =
                    new ExhaustiveSearchInstanceTrialInstanceVariableRepository(dbContext);

                await exhaustiveSearchInstanceTrialInstanceVariableRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId,
                        import.Id, token);

                var exhaustiveSearchInstancePromotedTrialInstanceRepository =
                    new ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext,
                        import.TenantRegistryId);

                await exhaustiveSearchInstancePromotedTrialInstanceRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId,
                        import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository =
                    new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository(dbContext);

                await exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId, import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValueRepository =
                    new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValueRepository(
                        dbContext);

                await exhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValueRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId, import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstancePromotedTrialInstanceRocRepository =
                    new ExhaustiveSearchInstancePromotedTrialInstanceRocRepository(dbContext);

                await exhaustiveSearchInstancePromotedTrialInstanceRocRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId,
                        import.Id, token);

                var exhaustiveSearchInstanceTrialInstanceTopologyTrialRepository =
                    new ExhaustiveSearchInstanceTrialInstanceTopologyTrialRepository(dbContext);

                await exhaustiveSearchInstanceTrialInstanceTopologyTrialRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId,
                        import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceTrialInstanceSensitivityRepository =
                    new ExhaustiveSearchInstanceTrialInstanceSensitivityRepository(dbContext);

                await exhaustiveSearchInstanceTrialInstanceSensitivityRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId,
                        import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository =
                    new ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository(dbContext);

                await exhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId, import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository =
                    new ExhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository(dbContext);

                await exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId, import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstancePromotedTrialInstanceVariableRepository =
                    new ExhaustiveSearchInstancePromotedTrialInstanceVariableRepository(dbContext);

                await exhaustiveSearchInstancePromotedTrialInstanceVariableRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId, import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstancePrescriptionHistogramRepository =
                    new ExhaustiveSearchInstancePrescriptionHistogramRepository(dbContext);

                await exhaustiveSearchInstancePrescriptionHistogramRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, token)
                    .ConfigureAwait(false);

                var exhaustiveSearchInstanceVariableAnomalyRepository =
                    new ExhaustiveSearchInstanceVariableAnomalyRepository(dbContext);

                await exhaustiveSearchInstanceVariableAnomalyRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceVariableClassificationRepository =
                    new ExhaustiveSearchInstanceVariableClassificationRepository(dbContext);

                await exhaustiveSearchInstanceVariableClassificationRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId,
                        import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceVariableHistogramRepository =
                    new ExhaustiveSearchInstanceVariableHistogramRepository(dbContext);

                await exhaustiveSearchInstanceVariableHistogramRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId,
                        import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceVariableHistogramClassificationRepository =
                    new ExhaustiveSearchInstanceVariableHistogramClassificationRepository(dbContext);

                await exhaustiveSearchInstanceVariableHistogramClassificationRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId, import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceVariableHistogramClassRepository =
                    new ExhaustiveSearchInstanceVariableHistogramClassRepository(dbContext);

                await exhaustiveSearchInstanceVariableHistogramClassRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId, import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceVariableHistogramAnomalyRepository =
                    new ExhaustiveSearchInstanceVariableHistogramAnomalyRepository(dbContext);

                await exhaustiveSearchInstanceVariableHistogramAnomalyRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId,
                        import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceVariableMulticollinearityRepository =
                    new ExhaustiveSearchInstanceVariableMultiCollinearityRepository(dbContext);

                await exhaustiveSearchInstanceVariableMulticollinearityRepository
                    .DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                        import.TenantRegistryId,
                        import.Id, token).ConfigureAwait(false);

                var entityAnalysisModelListRepository =
                    new EntityAnalysisModelListRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelListRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelListValueRepository =
                    new EntityAnalysisModelListValueRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelListValueRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token);

                var entityAnalysisModelDictionaryRepository =
                    new EntityAnalysisModelDictionaryRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelDictionaryRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token);

                var entityAnalysisModelDictionaryKvpRepository =
                    new EntityAnalysisModelDictionaryKvpRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelDictionaryKvpRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token);

                await new AdvancedAnalyticsPreservation(dbContext, import.TenantRegistryId, userName)
                    .DeleteExistingAsync(token).ConfigureAwait(false);

                await new EntityApprovalRepository(dbContext, import.TenantRegistryId)
                    .DeleteByTenantRegistryIdAsync(import.TenantRegistryId, token).ConfigureAwait(false);

                var exhaustiveInstanceDatasetRepository =
                    new ExhaustiveInstanceDatasetRepository(dbContext, import.TenantRegistryId);

                await exhaustiveInstanceDatasetRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token).ConfigureAwait(false);

                var exhaustiveInstanceCsvIdMap = new Dictionary<int, int>();

                foreach (var exhaustiveInstanceCsvExport in wrapper.Payload?.ExhaustiveInstanceCsv ?? [])
                {
                    var oldExhaustiveInstanceCsvId = exhaustiveInstanceCsvExport.Dataset!.Id;

                    exhaustiveInstanceCsvIdMap[oldExhaustiveInstanceCsvId] = await exhaustiveInstanceDatasetRepository
                        .InsertImportedAsync(exhaustiveInstanceCsvExport.Dataset,
                            exhaustiveInstanceCsvExport.Rows ?? [], import.Id, false, token).ConfigureAwait(false);
                }

                var exhaustiveInstanceJsonIdMap = new Dictionary<int, int>();

                foreach (var exhaustiveInstanceJsonExport in wrapper.Payload?.ExhaustiveInstanceJson ?? [])
                {
                    var oldExhaustiveInstanceJsonId = exhaustiveInstanceJsonExport.Dataset!.Id;

                    exhaustiveInstanceJsonIdMap[oldExhaustiveInstanceJsonId] = await exhaustiveInstanceDatasetRepository
                        .InsertImportedAsync(exhaustiveInstanceJsonExport.Dataset,
                            exhaustiveInstanceJsonExport.Rows ?? [], import.Id, false, token).ConfigureAwait(false);
                }

                var visualisationRegistryRepository =
                    new VisualisationRegistryRepository(dbContext, import.TenantRegistryId);

                var visualisationRegistryAllTenants = await visualisationRegistryRepository.GetAllTenantsAsync(token);

                await visualisationRegistryRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var visualisationRegistryDatasourceRepository =
                    new VisualisationRegistryDatasourceRepository(dbContext, import.TenantRegistryId);

                await visualisationRegistryDatasourceRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var visualisationRegistryParameterRepository =
                    new VisualisationRegistryParameterRepository(dbContext, import.TenantRegistryId);

                await visualisationRegistryParameterRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var caseWorkflowRoleRepository = new CaseWorkflowRoleRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId,
                    import.Id, token);

                var caseWorkflowActionRoleRepository =
                    new CaseWorkflowActionRoleRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowActionRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var caseWorkflowDisplayRoleRepository =
                    new CaseWorkflowDisplayRoleRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowDisplayRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var caseWorkflowFilterRoleRepository =
                    new CaseWorkflowFilterRoleRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowFilterRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var caseWorkflowFormRoleRepository =
                    new CaseWorkflowFormRoleRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowFormRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var caseWorkflowFormValidatorRepository =
                    new CaseWorkflowFormValidatorRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowFormValidatorRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var caseWorkflowReleaseHookRepository =
                    new CaseWorkflowReleaseHookRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowReleaseHookRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var caseWorkflowMacroRoleRepository =
                    new CaseWorkflowMacroRoleRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowMacroRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var caseWorkflowStatusRoleRepository =
                    new CaseWorkflowStatusRoleRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowStatusRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var caseWorkflowXPathRoleRepository =
                    new CaseWorkflowXPathRoleRepository(dbContext, import.TenantRegistryId);

                await caseWorkflowXPathRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var visualisationRegistryRoleRepository =
                    new VisualisationRegistryRoleRepository(dbContext, import.TenantRegistryId);

                await visualisationRegistryRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelRoleRepository =
                    new EntityAnalysisModelRoleRepository(dbContext, import.TenantRegistryId);

                await entityAnalysisModelRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var visualisationRegistryDatasourceRoleRepository =
                    new VisualisationRegistryDatasourceRoleRepository(dbContext, import.TenantRegistryId);

                await visualisationRegistryDatasourceRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var visualisationRegistryParameterRoleRepository =
                    new VisualisationRegistryParameterRoleRepository(dbContext, import.TenantRegistryId);

                await visualisationRegistryParameterRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token);

                var roleRegistryRekey = new Dictionary<Guid, Guid>();
                var roleRegistryPermissionRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelRequestXpathRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelInlineFunctionRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelInlineScriptRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelGatewayRuleRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelSanctionRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelTagRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelResponseElevationRangeRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelTtlCounterRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelAbstractionRuleRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelAbstractionCalculationsRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelHttpAdaptationRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelExhaustiveSearchInstanceRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelActivationRuleRekey = new Dictionary<Guid, Guid>();
                var caseWorkflowRekey = new Dictionary<Guid, Guid>();
                var caseWorkflowsXPathRekey = new Dictionary<Guid, Guid>();
                var caseWorkflowsActionRekey = new Dictionary<Guid, Guid>();
                var caseWorkflowsDisplayRekey = new Dictionary<Guid, Guid>();
                var caseWorkflowsFormRekey = new Dictionary<Guid, Guid>();
                var caseWorkflowsFilterRekey = new Dictionary<Guid, Guid>();
                var caseWorkflowsMacroRekey = new Dictionary<Guid, Guid>();
                var caseWorkflowsStatusRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelListRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelListValueRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelDictionaryRekey = new Dictionary<Guid, Guid>();
                var entityAnalysisModelDictionaryKvpRekey = new Dictionary<Guid, Guid>();
                var visualisationRegistryRekey = new Dictionary<Guid, Guid>();
                var visualisationRegistryDatasourceRekey = new Dictionary<Guid, Guid>();
                var visualisationRegistryParameterRekey = new Dictionary<Guid, Guid>();

                if (wrapper.Payload?.VisualisationRegistry != null)
                {
                    foreach (var visualisationRegistry in wrapper.Payload.VisualisationRegistry)
                    {
                        var rekey = false;

                        var existsAnywhere =
                            visualisationRegistryAllTenants.FirstOrDefault(f => f.Guid == visualisationRegistry.Guid) !=
                            null;

                        if (existsAnywhere)
                        {
                            var existsInTenant = roleRegistryAcrossAllTenants
                                .FirstOrDefault(f =>
                                    f.Guid == visualisationRegistry.Guid &&
                                    f.TenantRegistryId == import.TenantRegistryId) != null;

                            rekey = !existsInTenant;
                        }

                        if (rekey)
                        {
                            visualisationRegistryRekey.TryAdd(visualisationRegistry.Guid, Guid.NewGuid());
                            visualisationRegistry.Guid = visualisationRegistryRekey[visualisationRegistry.Guid];
                        }

                        visualisationRegistry.ImportId = import.Id;

                        var visualisationRegistryId =
                            (await visualisationRegistryRepository.InsertAsync(visualisationRegistry, token)).Id;

                        foreach (var visualisationRegistryDatasource in visualisationRegistry
                                     .VisualisationRegistryDatasource)
                        {
                            visualisationRegistryDatasource.VisualisationRegistryId = visualisationRegistryId;
                            visualisationRegistryDatasource.ImportId = import.Id;

                            if (rekey)
                            {
                                visualisationRegistryDatasourceRekey.TryAdd(visualisationRegistryDatasource.Guid,
                                    Guid.NewGuid());

                                visualisationRegistryDatasource.Guid =
                                    visualisationRegistryDatasourceRekey[visualisationRegistryDatasource.Guid];
                            }

                            await visualisationRegistryDatasourceRepository.InsertAsync(visualisationRegistryDatasource,
                                token);
                        }

                        foreach (var visualisationRegistryParameter in visualisationRegistry
                                     .VisualisationRegistryParameter)
                        {
                            visualisationRegistryParameter.VisualisationRegistryId = visualisationRegistryId;
                            visualisationRegistryParameter.ImportId = import.Id;

                            if (rekey)
                            {
                                visualisationRegistryParameterRekey.TryAdd(visualisationRegistryParameter.Guid,
                                    Guid.NewGuid());

                                visualisationRegistryParameter.Guid =
                                    visualisationRegistryParameterRekey[visualisationRegistryParameter.Guid];
                            }

                            await visualisationRegistryParameterRepository.InsertAsync(visualisationRegistryParameter,
                                token);
                        }
                    }
                }

                var importedEntityAnalysisModelIds = new List<int>();
                var entityAnalysisModelIdMap = new Dictionary<int, int>();
                var entityApprovalEntityIdMaps = new Dictionary<EntityApprovalKind, Dictionary<int, int>>();

                foreach (var entityApprovalKindForMap in Enum.GetValues<EntityApprovalKind>())
                {
                    entityApprovalEntityIdMaps[entityApprovalKindForMap] = new Dictionary<int, int>();
                }

                if (wrapper.Payload?.EntityAnalysisModel != null)
                {
                    foreach (var oldEntityAnalysisModel in wrapper.Payload.EntityAnalysisModel)
                    {
                        var oldEntityAnalysisModelId = oldEntityAnalysisModel.Id;
                        var oldEntityAnalysisModelVersion = oldEntityAnalysisModel.Version;
                        var rekey = false;

                        var existsAnywhere =
                            entityAnalysisModelsAcrossAllTenants.FirstOrDefault(f =>
                                f.Guid == oldEntityAnalysisModel.Guid) != null;

                        if (existsAnywhere)
                        {
                            var existsInTenant = entityAnalysisModelsAcrossAllTenants
                                .FirstOrDefault(f =>
                                    f.Guid == oldEntityAnalysisModel.Guid &&
                                    f.TenantRegistryId == import.TenantRegistryId) != null;

                            rekey = !existsInTenant;
                        }

                        if (rekey)
                        {
                            entityAnalysisModelRekey.TryAdd(oldEntityAnalysisModel.Guid, Guid.NewGuid());
                            oldEntityAnalysisModel.Guid = entityAnalysisModelRekey[oldEntityAnalysisModel.Guid];
                        }

                        oldEntityAnalysisModel.ImportId = import.Id;

                        var newEntityAnalysisModel =
                            await entityAnalysisModelRepository.InsertAsync(oldEntityAnalysisModel, token);

                        entityAnalysisModelIdMap[oldEntityAnalysisModelId] = newEntityAnalysisModel.Id;

                        await CarryVersionForApprovalReplayAsync(newEntityAnalysisModel,
                            oldEntityAnalysisModelVersion, oldEntityAnalysisModelId, newEntityAnalysisModel.Id,
                            EntityApprovalKind.EntityAnalysisModel, entityApprovalEntityIdMaps, token);

                        importedEntityAnalysisModelIds.Add(newEntityAnalysisModel.Id);

                        foreach (var entityAnalysisModelRequestXpath in oldEntityAnalysisModel
                                     .EntityAnalysisModelRequestXpath)
                        {
                            var oldRequestXpathId = entityAnalysisModelRequestXpath.Id;
                            var oldRequestXpathVersion = entityAnalysisModelRequestXpath.Version;

                            entityAnalysisModelRequestXpath.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelRequestXpath.ImportId = import.Id;

                            if (rekey)
                            {
                                entityAnalysisModelRequestXpathRekey.TryAdd(entityAnalysisModelRequestXpath.Guid,
                                    Guid.NewGuid());

                                entityAnalysisModelRequestXpath.Guid =
                                    entityAnalysisModelRequestXpathRekey[entityAnalysisModelRequestXpath.Guid];
                            }

                            await entityAnalysisModelRequestXPathRepository.InsertAsync(entityAnalysisModelRequestXpath,
                                token);

                            await CarryVersionForApprovalReplayAsync(entityAnalysisModelRequestXpath,
                                oldRequestXpathVersion, oldRequestXpathId, entityAnalysisModelRequestXpath.Id,
                                EntityApprovalKind.EntityAnalysisModelRequestXPath, entityApprovalEntityIdMaps, token);
                        }

                        foreach (var entityAnalysisModelInlineFunction in oldEntityAnalysisModel
                                     .EntityAnalysisModelInlineFunction)
                        {
                            var oldInlineFunctionId = entityAnalysisModelInlineFunction.Id;
                            var oldInlineFunctionVersion = entityAnalysisModelInlineFunction.Version;

                            entityAnalysisModelInlineFunction.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelInlineFunction.ImportId = import.Id;

                            if (rekey)
                            {
                                entityAnalysisModelInlineFunctionRekey.TryAdd(entityAnalysisModelInlineFunction.Guid,
                                    Guid.NewGuid());

                                entityAnalysisModelInlineFunction.Guid =
                                    entityAnalysisModelInlineFunctionRekey[entityAnalysisModelInlineFunction.Guid];
                            }

                            await entityAnalysisModelInlineFunctionRepository.InsertAsync(
                                entityAnalysisModelInlineFunction, token);

                            await CarryVersionForApprovalReplayAsync(entityAnalysisModelInlineFunction,
                                oldInlineFunctionVersion, oldInlineFunctionId, entityAnalysisModelInlineFunction.Id,
                                EntityApprovalKind.EntityAnalysisModelInlineFunction, entityApprovalEntityIdMaps,
                                token);
                        }

                        foreach (var entityAnalysisModelInlineScript in oldEntityAnalysisModel
                                     .EntityAnalysisModelInlineScript)
                        {
                            var oldInlineScriptId = entityAnalysisModelInlineScript.Id;
                            var oldInlineScriptVersion = entityAnalysisModelInlineScript.Version;

                            entityAnalysisModelInlineScript.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelInlineScript.ImportId = import.Id;

                            if (rekey)
                            {
                                entityAnalysisModelInlineScriptRekey.TryAdd(entityAnalysisModelInlineScript.Guid,
                                    Guid.NewGuid());

                                entityAnalysisModelInlineScript.Guid =
                                    entityAnalysisModelInlineScriptRekey[entityAnalysisModelInlineScript.Guid];
                            }

                            await entityAnalysisModelInlineScriptRepository.InsertAsync(entityAnalysisModelInlineScript,
                                token);

                            await CarryVersionForApprovalReplayAsync(entityAnalysisModelInlineScript,
                                oldInlineScriptVersion, oldInlineScriptId, entityAnalysisModelInlineScript.Id,
                                EntityApprovalKind.EntityAnalysisModelInlineScript, entityApprovalEntityIdMaps, token);
                        }

                        foreach (var entityAnalysisModelGatewayRule in oldEntityAnalysisModel
                                     .EntityAnalysisModelGatewayRule)
                        {
                            var oldGatewayRuleId = entityAnalysisModelGatewayRule.Id;
                            var oldGatewayRuleVersion = entityAnalysisModelGatewayRule.Version;

                            entityAnalysisModelGatewayRule.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelGatewayRule.ImportId = import.Id;

                            if (rekey)
                            {
                                entityAnalysisModelGatewayRuleRekey.TryAdd(entityAnalysisModelGatewayRule.Guid,
                                    Guid.NewGuid());

                                entityAnalysisModelGatewayRule.Guid =
                                    entityAnalysisModelGatewayRuleRekey[entityAnalysisModelGatewayRule.Guid];
                            }

                            await entityAnalysisModelGatewayRuleRepository.InsertAsync(entityAnalysisModelGatewayRule,
                                token);

                            await CarryVersionForApprovalReplayAsync(entityAnalysisModelGatewayRule,
                                oldGatewayRuleVersion, oldGatewayRuleId, entityAnalysisModelGatewayRule.Id,
                                EntityApprovalKind.EntityAnalysisModelGatewayRule, entityApprovalEntityIdMaps, token);
                        }

                        foreach (var entityAnalysisModelSanction in oldEntityAnalysisModel
                                     .EntityAnalysisModelSanction)
                        {
                            var oldSanctionId = entityAnalysisModelSanction.Id;
                            var oldSanctionVersion = entityAnalysisModelSanction.Version;

                            entityAnalysisModelSanction.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelSanction.ImportId = import.Id;

                            if (rekey)
                            {
                                entityAnalysisModelSanctionRekey.TryAdd(entityAnalysisModelSanction.Guid,
                                    Guid.NewGuid());

                                entityAnalysisModelSanction.Guid =
                                    entityAnalysisModelSanctionRekey[entityAnalysisModelSanction.Guid];
                            }

                            await entityAnalysisModelSanctionRepository.InsertAsync(entityAnalysisModelSanction, token);

                            await CarryVersionForApprovalReplayAsync(entityAnalysisModelSanction, oldSanctionVersion,
                                oldSanctionId, entityAnalysisModelSanction.Id,
                                EntityApprovalKind.EntityAnalysisModelSanction, entityApprovalEntityIdMaps, token);
                        }

                        foreach (var entityAnalysisModelTag in oldEntityAnalysisModel
                                     .EntityAnalysisModelTag)
                        {
                            var oldTagId = entityAnalysisModelTag.Id;
                            var oldTagVersion = entityAnalysisModelTag.Version;

                            entityAnalysisModelTag.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelTag.ImportId = import.Id;

                            if (rekey)
                            {
                                entityAnalysisModelTagRekey.TryAdd(entityAnalysisModelTag.Guid, Guid.NewGuid());
                                entityAnalysisModelTag.Guid = entityAnalysisModelTagRekey[entityAnalysisModelTag.Guid];
                            }

                            await entityAnalysisModelTagRepository.InsertAsync(entityAnalysisModelTag, token);

                            await CarryVersionForApprovalReplayAsync(entityAnalysisModelTag, oldTagVersion, oldTagId,
                                entityAnalysisModelTag.Id, EntityApprovalKind.EntityAnalysisModelTag,
                                entityApprovalEntityIdMaps, token);
                        }

                        foreach (var entityAnalysisModelResponseElevationRange in oldEntityAnalysisModel
                                     .EntityAnalysisModelResponseElevationRange ?? [])
                        {
                            var oldResponseElevationRangeId = entityAnalysisModelResponseElevationRange.Id;
                            var oldResponseElevationRangeVersion = entityAnalysisModelResponseElevationRange.Version;

                            entityAnalysisModelResponseElevationRange.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelResponseElevationRange.ImportId = import.Id;

                            if (rekey)
                            {
                                entityAnalysisModelResponseElevationRangeRekey.TryAdd(
                                    entityAnalysisModelResponseElevationRange.Guid, Guid.NewGuid());

                                entityAnalysisModelResponseElevationRange.Guid =
                                    entityAnalysisModelResponseElevationRangeRekey[
                                        entityAnalysisModelResponseElevationRange.Guid];
                            }

                            await entityAnalysisModelResponseElevationRangeRepository.InsertAsync(
                                entityAnalysisModelResponseElevationRange, token);

                            await CarryVersionForApprovalReplayAsync(entityAnalysisModelResponseElevationRange,
                                oldResponseElevationRangeVersion, oldResponseElevationRangeId,
                                entityAnalysisModelResponseElevationRange.Id,
                                EntityApprovalKind.EntityAnalysisModelResponseElevationRange,
                                entityApprovalEntityIdMaps, token);
                        }

                        foreach (var entityAnalysisModelTtlCounter in oldEntityAnalysisModel
                                     .EntityAnalysisModelTtlCounter)
                        {
                            var oldTtlCounterId = entityAnalysisModelTtlCounter.Id;
                            var oldTtlCounterVersion = entityAnalysisModelTtlCounter.Version;

                            entityAnalysisModelTtlCounter.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelTtlCounter.ImportId = import.Id;

                            if (rekey)
                            {
                                entityAnalysisModelTtlCounterRekey.TryAdd(entityAnalysisModelTtlCounter.Guid,
                                    Guid.NewGuid());

                                entityAnalysisModelTtlCounter.Guid =
                                    entityAnalysisModelTtlCounterRekey[entityAnalysisModelTtlCounter.Guid];
                            }

                            await entityAnalysisModelTtlCounterRepository.InsertAsync(entityAnalysisModelTtlCounter,
                                token);

                            await CarryVersionForApprovalReplayAsync(entityAnalysisModelTtlCounter,
                                oldTtlCounterVersion, oldTtlCounterId, entityAnalysisModelTtlCounter.Id,
                                EntityApprovalKind.EntityAnalysisModelTtlCounter, entityApprovalEntityIdMaps, token);
                        }

                        foreach (var entityAnalysisModelAbstractionRule in oldEntityAnalysisModel
                                     .EntityAnalysisModelAbstractionRule)
                        {
                            var oldAbstractionRuleId = entityAnalysisModelAbstractionRule.Id;
                            var oldAbstractionRuleVersion = entityAnalysisModelAbstractionRule.Version;

                            entityAnalysisModelAbstractionRule.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelAbstractionRule.ImportId = import.Id;

                            if (rekey)
                            {
                                entityAnalysisModelAbstractionRuleRekey.TryAdd(entityAnalysisModelAbstractionRule.Guid,
                                    Guid.NewGuid());

                                entityAnalysisModelAbstractionRule.Guid =
                                    entityAnalysisModelAbstractionRuleRekey[entityAnalysisModelAbstractionRule.Guid];
                            }

                            await entityAnalysisModelAbstractionRuleRepository.InsertAsync(
                                entityAnalysisModelAbstractionRule, token);

                            await CarryVersionForApprovalReplayAsync(entityAnalysisModelAbstractionRule,
                                oldAbstractionRuleVersion, oldAbstractionRuleId, entityAnalysisModelAbstractionRule.Id,
                                EntityApprovalKind.EntityAnalysisModelAbstractionRule, entityApprovalEntityIdMaps,
                                token);
                        }

                        foreach (var entityAnalysisModelAbstractionCalculations in oldEntityAnalysisModel
                                     .EntityAnalysisModelAbstractionCalculation)
                        {
                            var oldAbstractionCalculationId = entityAnalysisModelAbstractionCalculations.Id;
                            var oldAbstractionCalculationVersion = entityAnalysisModelAbstractionCalculations.Version;

                            entityAnalysisModelAbstractionCalculations.EntityAnalysisModelId =
                                newEntityAnalysisModel.Id;
                            entityAnalysisModelAbstractionCalculations.ImportId = import.Id;

                            if (rekey)
                            {
                                entityAnalysisModelAbstractionCalculationsRekey.TryAdd(
                                    entityAnalysisModelAbstractionCalculations.Guid, Guid.NewGuid());

                                entityAnalysisModelAbstractionCalculations.Guid =
                                    entityAnalysisModelAbstractionCalculationsRekey[
                                        entityAnalysisModelAbstractionCalculations.Guid];
                            }

                            await entityAnalysisModelAbstractionCalculationRepository.InsertAsync(
                                entityAnalysisModelAbstractionCalculations, token);

                            await CarryVersionForApprovalReplayAsync(entityAnalysisModelAbstractionCalculations,
                                oldAbstractionCalculationVersion, oldAbstractionCalculationId,
                                entityAnalysisModelAbstractionCalculations.Id,
                                EntityApprovalKind.EntityAnalysisModelAbstractionCalculation,
                                entityApprovalEntityIdMaps, token);
                        }

                        foreach (var entityAnalysisModelHttpAdaptation in oldEntityAnalysisModel
                                     .EntityAnalysisModelHttpAdaptation)
                        {
                            var oldHttpAdaptationId = entityAnalysisModelHttpAdaptation.Id;
                            var oldHttpAdaptationVersion = entityAnalysisModelHttpAdaptation.Version;

                            entityAnalysisModelHttpAdaptation.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelHttpAdaptation.ImportId = import.Id;

                            if (rekey)
                            {
                                entityAnalysisModelHttpAdaptationRekey.TryAdd(entityAnalysisModelHttpAdaptation.Guid,
                                    Guid.NewGuid());

                                entityAnalysisModelHttpAdaptation.Guid =
                                    entityAnalysisModelHttpAdaptationRekey[entityAnalysisModelHttpAdaptation.Guid];
                            }

                            await entityAnalysisModelHttpAdaptationRepository.InsertAsync(
                                entityAnalysisModelHttpAdaptation, token);

                            await CarryVersionForApprovalReplayAsync(entityAnalysisModelHttpAdaptation,
                                oldHttpAdaptationVersion, oldHttpAdaptationId, entityAnalysisModelHttpAdaptation.Id,
                                EntityApprovalKind.EntityAnalysisModelHttpAdaptation, entityApprovalEntityIdMaps,
                                token);
                        }

                        if (oldEntityAnalysisModel.ExhaustiveSearchInstance != null)
                        {
                            foreach (var entityAnalysisModelExhaustiveSearchInstance in oldEntityAnalysisModel
                                         .ExhaustiveSearchInstance)
                            {
                                var oldExhaustiveSearchInstanceId = entityAnalysisModelExhaustiveSearchInstance.Id;

                                var oldExhaustiveSearchInstanceVersion =
                                    entityAnalysisModelExhaustiveSearchInstance.Version;

                                entityAnalysisModelExhaustiveSearchInstance.EntityAnalysisModelId =
                                    newEntityAnalysisModel.Id;
                                entityAnalysisModelExhaustiveSearchInstance.ImportId = import.Id;

                                if (entityAnalysisModelExhaustiveSearchInstance.ExhaustiveInstanceCsvId.HasValue)
                                {
                                    entityAnalysisModelExhaustiveSearchInstance.ExhaustiveInstanceCsvId =
                                        exhaustiveInstanceCsvIdMap.TryGetValue(
                                            entityAnalysisModelExhaustiveSearchInstance.ExhaustiveInstanceCsvId.Value,
                                            out var remappedExhaustiveInstanceCsvId)
                                            ? remappedExhaustiveInstanceCsvId
                                            : null;

                                    if (entityAnalysisModelExhaustiveSearchInstance.ExhaustiveInstanceCsvId == null)
                                    {
                                        entityAnalysisModelExhaustiveSearchInstance.DataSourceId = 0;
                                    }
                                }

                                if (entityAnalysisModelExhaustiveSearchInstance.ExhaustiveInstanceJsonId.HasValue)
                                {
                                    entityAnalysisModelExhaustiveSearchInstance.ExhaustiveInstanceJsonId =
                                        exhaustiveInstanceJsonIdMap.TryGetValue(
                                            entityAnalysisModelExhaustiveSearchInstance.ExhaustiveInstanceJsonId.Value,
                                            out var remappedExhaustiveInstanceJsonId)
                                            ? remappedExhaustiveInstanceJsonId
                                            : null;

                                    if (entityAnalysisModelExhaustiveSearchInstance.ExhaustiveInstanceJsonId == null)
                                    {
                                        entityAnalysisModelExhaustiveSearchInstance.DataSourceId = 0;
                                    }
                                }

                                if (rekey)
                                {
                                    entityAnalysisModelExhaustiveSearchInstanceRekey.TryAdd(
                                        entityAnalysisModelExhaustiveSearchInstance.Guid, Guid.NewGuid());

                                    entityAnalysisModelExhaustiveSearchInstance.Guid =
                                        entityAnalysisModelExhaustiveSearchInstanceRekey[
                                            entityAnalysisModelExhaustiveSearchInstance.Guid];
                                }

                                var entityAnalysisModelExhaustiveSearchInstanceId =
                                    (await exhaustiveSearchInstanceRepository
                                        .InsertAsync(entityAnalysisModelExhaustiveSearchInstance, token)
                                        .ConfigureAwait(false)).Id;

                                await CarryVersionForApprovalReplayAsync(entityAnalysisModelExhaustiveSearchInstance,
                                    oldExhaustiveSearchInstanceVersion, oldExhaustiveSearchInstanceId,
                                    entityAnalysisModelExhaustiveSearchInstanceId,
                                    EntityApprovalKind.ExhaustiveSearchInstance, entityApprovalEntityIdMaps, token);

                                foreach (var exhaustiveSearchInstanceData in entityAnalysisModelExhaustiveSearchInstance
                                             .ExhaustiveSearchInstanceData)
                                {
                                    exhaustiveSearchInstanceData.ExhaustiveSearchInstanceId =
                                        entityAnalysisModelExhaustiveSearchInstanceId;

                                    exhaustiveSearchInstanceData.ImportId = import.Id;

                                    await exhaustiveSearchInstanceDataRepository
                                        .InsertAsync(exhaustiveSearchInstanceData, token).ConfigureAwait(false);
                                }

                                var exhaustiveSearchInstanceCategoricalFieldIdMap = new Dictionary<int, int>();

                                foreach (var exhaustiveSearchInstanceCategoricalField in
                                         entityAnalysisModelExhaustiveSearchInstance
                                             .ExhaustiveSearchInstanceCategoricalField)
                                {
                                    var oldExhaustiveSearchInstanceCategoricalFieldId =
                                        exhaustiveSearchInstanceCategoricalField.Id;

                                    exhaustiveSearchInstanceCategoricalField.ExhaustiveSearchInstanceId =
                                        entityAnalysisModelExhaustiveSearchInstanceId;

                                    exhaustiveSearchInstanceCategoricalField.ImportId = import.Id;

                                    var exhaustiveSearchInstanceCategoricalFieldId =
                                        await exhaustiveSearchInstanceCategoricalFieldRepository
                                            .InsertAsync(exhaustiveSearchInstanceCategoricalField, token)
                                            .ConfigureAwait(false);

                                    exhaustiveSearchInstanceCategoricalFieldIdMap[
                                            oldExhaustiveSearchInstanceCategoricalFieldId] =
                                        exhaustiveSearchInstanceCategoricalFieldId;
                                }

                                var exhaustiveSearchInstanceVariableIdMap = new Dictionary<int, int>();

                                foreach (var exhaustiveSearchInstanceVariable in
                                         entityAnalysisModelExhaustiveSearchInstance.ExhaustiveSearchInstanceVariable)
                                {
                                    var oldExhaustiveSearchInstanceVariableId = exhaustiveSearchInstanceVariable.Id;

                                    exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceId =
                                        entityAnalysisModelExhaustiveSearchInstanceId;

                                    exhaustiveSearchInstanceVariable.ImportId = import.Id;

                                    exhaustiveSearchInstanceVariable.CategoricalFieldId =
                                        exhaustiveSearchInstanceVariable.CategoricalFieldId.HasValue
                                        && exhaustiveSearchInstanceCategoricalFieldIdMap.TryGetValue(
                                            exhaustiveSearchInstanceVariable.CategoricalFieldId.Value,
                                            out var remappedCategoricalFieldIdForVariable)
                                            ? remappedCategoricalFieldIdForVariable
                                            : null;

                                    var exhaustiveSearchInstanceVariableId =
                                        await exhaustiveSearchInstanceVariableRepository.InsertAsync(
                                            exhaustiveSearchInstanceVariable, token).ConfigureAwait(false);

                                    exhaustiveSearchInstanceVariableIdMap[oldExhaustiveSearchInstanceVariableId] =
                                        exhaustiveSearchInstanceVariableId;
                                }

                                foreach (var exhaustiveSearchInstanceTrialInstance in
                                         entityAnalysisModelExhaustiveSearchInstance
                                             .ExhaustiveSearchInstanceTrialInstance)
                                {
                                    exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceId =
                                        entityAnalysisModelExhaustiveSearchInstanceId;

                                    exhaustiveSearchInstanceTrialInstance.ImportId = import.Id;

                                    var exhaustiveSearchInstanceTrialInstanceId =
                                        (await exhaustiveSearchInstanceTrialInstanceRepository.InsertAsync(
                                            exhaustiveSearchInstanceTrialInstance, token).ConfigureAwait(false)).Id;

                                    foreach (var exhaustiveSearchInstanceTrialInstanceVariable in
                                             exhaustiveSearchInstanceTrialInstance
                                                 .ExhaustiveSearchInstanceTrialInstanceVariable)
                                    {
                                        exhaustiveSearchInstanceTrialInstanceVariable
                                                .ExhaustiveSearchInstanceTrialInstanceId =
                                            exhaustiveSearchInstanceTrialInstanceId;

                                        exhaustiveSearchInstanceTrialInstanceVariable.ImportId = import.Id;

                                        exhaustiveSearchInstanceTrialInstanceVariable.ExhaustiveSearchInstanceVariableId
                                            = exhaustiveSearchInstanceTrialInstanceVariable
                                                  .ExhaustiveSearchInstanceVariableId.HasValue
                                              && exhaustiveSearchInstanceVariableIdMap.TryGetValue(
                                                  exhaustiveSearchInstanceTrialInstanceVariable
                                                      .ExhaustiveSearchInstanceVariableId.Value,
                                                  out var remappedVariableIdForTrialInstanceVariable)
                                                ? remappedVariableIdForTrialInstanceVariable
                                                : null;

                                        var exhaustiveSearchInstanceTrialInstanceVariableId =
                                            (await exhaustiveSearchInstanceTrialInstanceVariableRepository.InsertAsync(
                                                    exhaustiveSearchInstanceTrialInstanceVariable, token)
                                                .ConfigureAwait(false)).Id;

                                        foreach (var exhaustiveSearchInstancePromotedTrialInstanceSensitivity in
                                                 exhaustiveSearchInstanceTrialInstanceVariable
                                                     .ExhaustiveSearchInstancePromotedTrialInstanceSensitivity)
                                        {
                                            exhaustiveSearchInstancePromotedTrialInstanceSensitivity
                                                    .ExhaustiveSearchInstanceTrialInstanceVariableId =
                                                exhaustiveSearchInstanceTrialInstanceVariableId;

                                            exhaustiveSearchInstancePromotedTrialInstanceSensitivity.ImportId =
                                                import.Id;

                                            await exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository
                                                .InsertAsync(
                                                    exhaustiveSearchInstancePromotedTrialInstanceSensitivity, token)
                                                .ConfigureAwait(false);
                                        }

                                        foreach (var exhaustiveSearchInstancePromotedTrialInstanceVariable in
                                                 exhaustiveSearchInstanceTrialInstanceVariable
                                                     .ExhaustiveSearchInstancePromotedTrialInstanceVariable)
                                        {
                                            exhaustiveSearchInstancePromotedTrialInstanceVariable
                                                    .ExhaustiveSearchInstanceTrialInstanceVariableId =
                                                exhaustiveSearchInstanceTrialInstanceVariableId;

                                            exhaustiveSearchInstancePromotedTrialInstanceVariable.ImportId = import.Id;

                                            var exhaustiveSearchInstancePromotedTrialInstanceVariableId =
                                                (await exhaustiveSearchInstancePromotedTrialInstanceVariableRepository
                                                    .InsertAsync(
                                                        exhaustiveSearchInstancePromotedTrialInstanceVariable, token)
                                                    .ConfigureAwait(false)).Id;

                                            foreach (var exhaustiveSearchInstancePrescriptionHistogram in
                                                     exhaustiveSearchInstancePromotedTrialInstanceVariable
                                                         .ExhaustiveSearchInstancePrescriptionHistogram ?? [])
                                            {
                                                exhaustiveSearchInstancePrescriptionHistogram
                                                        .ExhaustiveSearchInstancePromotedTrialInstanceVariableId =
                                                    exhaustiveSearchInstancePromotedTrialInstanceVariableId;

                                                await exhaustiveSearchInstancePrescriptionHistogramRepository
                                                    .InsertAsync(exhaustiveSearchInstancePrescriptionHistogram, token)
                                                    .ConfigureAwait(false);
                                            }
                                        }
                                    }

                                    foreach (var exhaustiveSearchInstancePromotedTrialInstance in
                                             exhaustiveSearchInstanceTrialInstance
                                                 .ExhaustiveSearchInstancePromotedTrialInstance)
                                    {
                                        exhaustiveSearchInstancePromotedTrialInstance
                                                .ExhaustiveSearchInstanceTrialInstanceId =
                                            exhaustiveSearchInstanceTrialInstanceId;

                                        exhaustiveSearchInstancePromotedTrialInstance.ImportId = import.Id;

                                        await exhaustiveSearchInstancePromotedTrialInstanceRepository.InsertAsync(
                                            exhaustiveSearchInstancePromotedTrialInstance, token).ConfigureAwait(false);
                                    }

                                    foreach (var exhaustiveSearchInstancePromotedTrialInstancePredictedActual in
                                             exhaustiveSearchInstanceTrialInstance
                                                 .ExhaustiveSearchInstancePromotedTrialInstancePredictedActual)
                                    {
                                        exhaustiveSearchInstancePromotedTrialInstancePredictedActual
                                                .ExhaustiveSearchInstanceTrialInstanceId =
                                            exhaustiveSearchInstanceTrialInstanceId;

                                        exhaustiveSearchInstancePromotedTrialInstancePredictedActual.ImportId =
                                            import.Id;

                                        var exhaustiveSearchInstancePromotedTrialInstancePredictedActualId =
                                            (await
                                                exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository
                                                    .InsertAsync(
                                                        exhaustiveSearchInstancePromotedTrialInstancePredictedActual,
                                                        token)
                                                    .ConfigureAwait(false)).Id;

                                        foreach (var
                                                     exhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue
                                                 in exhaustiveSearchInstancePromotedTrialInstancePredictedActual
                                                     .ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue)
                                        {
                                            exhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue
                                                    .ExhaustiveSearchInstancePromotedTrialInstancePredictedActualId =
                                                exhaustiveSearchInstancePromotedTrialInstancePredictedActualId;

                                            exhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue
                                                .ImportId = import.Id;

                                            exhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue
                                                    .ExhaustiveSearchInstanceVariableId =
                                                exhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue
                                                    .ExhaustiveSearchInstanceVariableId.HasValue
                                                && exhaustiveSearchInstanceVariableIdMap.TryGetValue(
                                                    exhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue
                                                        .ExhaustiveSearchInstanceVariableId.Value,
                                                    out var remappedVariableIdForPredictedActualVariableValue)
                                                    ? remappedVariableIdForPredictedActualVariableValue
                                                    : null;

                                            await
                                                exhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValueRepository
                                                    .InsertAsync(
                                                        exhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue,
                                                        token).ConfigureAwait(false);
                                        }
                                    }

                                    foreach (var exhaustiveSearchInstancePromotedTrialInstanceRoc in
                                             exhaustiveSearchInstanceTrialInstance
                                                 .ExhaustiveSearchInstancePromotedTrialInstanceRoc)
                                    {
                                        exhaustiveSearchInstancePromotedTrialInstanceRoc
                                                .ExhaustiveSearchInstanceTrialInstanceId =
                                            exhaustiveSearchInstanceTrialInstanceId;

                                        exhaustiveSearchInstancePromotedTrialInstanceRoc.ImportId = import.Id;

                                        await exhaustiveSearchInstancePromotedTrialInstanceRocRepository.InsertAsync(
                                                exhaustiveSearchInstancePromotedTrialInstanceRoc, token)
                                            .ConfigureAwait(false);
                                    }

                                    foreach (var exhaustiveSearchInstanceTrialInstanceTopologyTrial in
                                             exhaustiveSearchInstanceTrialInstance
                                                 .ExhaustiveSearchInstanceTrialInstanceTopologyTrial)
                                    {
                                        exhaustiveSearchInstanceTrialInstanceTopologyTrial
                                                .ExhaustiveSearchInstanceTrialInstanceId =
                                            exhaustiveSearchInstanceTrialInstanceId;

                                        exhaustiveSearchInstanceTrialInstanceTopologyTrial.ImportId = import.Id;

                                        await exhaustiveSearchInstanceTrialInstanceTopologyTrialRepository.InsertAsync(
                                                exhaustiveSearchInstanceTrialInstanceTopologyTrial, token)
                                            .ConfigureAwait(false);
                                    }

                                    foreach (var exhaustiveSearchInstanceTrialInstanceSensitivity in
                                             exhaustiveSearchInstanceTrialInstance
                                                 .ExhaustiveSearchInstanceTrialInstanceSensitivity)
                                    {
                                        exhaustiveSearchInstanceTrialInstanceSensitivity
                                                .ExhaustiveSearchInstanceTrialInstanceId =
                                            exhaustiveSearchInstanceTrialInstanceId;

                                        exhaustiveSearchInstanceTrialInstanceSensitivity.ImportId = import.Id;

                                        exhaustiveSearchInstanceTrialInstanceSensitivity
                                                .ExhaustiveSearchInstanceVariableId
                                            = exhaustiveSearchInstanceTrialInstanceSensitivity
                                                  .ExhaustiveSearchInstanceVariableId.HasValue
                                              && exhaustiveSearchInstanceVariableIdMap.TryGetValue(
                                                  exhaustiveSearchInstanceTrialInstanceSensitivity
                                                      .ExhaustiveSearchInstanceVariableId.Value,
                                                  out var remappedVariableIdForTrialInstanceSensitivity)
                                                ? remappedVariableIdForTrialInstanceSensitivity
                                                : null;

                                        await exhaustiveSearchInstanceTrialInstanceSensitivityRepository.InsertAsync(
                                                exhaustiveSearchInstanceTrialInstanceSensitivity, token)
                                            .ConfigureAwait(false);
                                    }

                                    foreach (var exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial in
                                             exhaustiveSearchInstanceTrialInstance
                                                 .ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial)
                                    {
                                        exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial
                                                .ExhaustiveSearchInstanceTrialInstanceId =
                                            exhaustiveSearchInstanceTrialInstanceId;

                                        exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial.ImportId =
                                            import.Id;

                                        await exhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository
                                            .InsertAsync(
                                                exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial, token)
                                            .ConfigureAwait(false);
                                    }
                                }

                                foreach (var exhaustiveSearchInstanceVariable in
                                         entityAnalysisModelExhaustiveSearchInstance
                                             .ExhaustiveSearchInstanceVariable)
                                {
                                    var exhaustiveSearchInstanceVariableId =
                                        exhaustiveSearchInstanceVariableIdMap[exhaustiveSearchInstanceVariable.Id];

                                    foreach (var exhaustiveSearchInstanceVariableHistogram in
                                             exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableHistogram)
                                    {
                                        exhaustiveSearchInstanceVariableHistogram.ExhaustiveSearchInstanceVariableId =
                                            exhaustiveSearchInstanceVariableId;

                                        exhaustiveSearchInstanceVariableHistogram.ImportId = import.Id;

                                        await exhaustiveSearchInstanceVariableHistogramRepository.InsertAsync(
                                            exhaustiveSearchInstanceVariableHistogram, token).ConfigureAwait(false);
                                    }

                                    foreach (var exhaustiveSearchInstanceVariableHistogramClass in
                                             exhaustiveSearchInstanceVariable
                                                 .ExhaustiveSearchInstanceVariableHistogramClass)
                                    {
                                        exhaustiveSearchInstanceVariableHistogramClass
                                                .ExhaustiveSearchInstanceVariableId =
                                            exhaustiveSearchInstanceVariableId;

                                        exhaustiveSearchInstanceVariableHistogramClass.ImportId = import.Id;

                                        await exhaustiveSearchInstanceVariableHistogramClassRepository.InsertAsync(
                                                exhaustiveSearchInstanceVariableHistogramClass, token)
                                            .ConfigureAwait(false);
                                    }

                                    foreach (var exhaustiveSearchInstanceVariableAnomaly in
                                             exhaustiveSearchInstanceVariable
                                                 .ExhaustiveSearchInstanceVariableAnomaly)
                                    {
                                        var oldExhaustiveSearchInstanceVariableAnomalyId =
                                            exhaustiveSearchInstanceVariableAnomaly.Id;

                                        exhaustiveSearchInstanceVariableAnomaly.ExhaustiveSearchInstanceVariableId =
                                            exhaustiveSearchInstanceVariableId;

                                        exhaustiveSearchInstanceVariableAnomaly.ImportId = import.Id;

                                        var exhaustiveSearchInstanceVariableAnomalyId =
                                            await exhaustiveSearchInstanceVariableAnomalyRepository.InsertAsync(
                                                exhaustiveSearchInstanceVariableAnomaly, token).ConfigureAwait(false);

                                        foreach (var exhaustiveSearchInstanceVariableHistogramAnomaly in
                                                 exhaustiveSearchInstanceVariable
                                                     .ExhaustiveSearchInstanceVariableHistogramAnomaly.Where(w =>
                                                         w.ExhaustiveSearchInstanceVariableAnomalyId ==
                                                         oldExhaustiveSearchInstanceVariableAnomalyId))
                                        {
                                            exhaustiveSearchInstanceVariableHistogramAnomaly
                                                    .ExhaustiveSearchInstanceVariableAnomalyId =
                                                exhaustiveSearchInstanceVariableAnomalyId;

                                            exhaustiveSearchInstanceVariableHistogramAnomaly.ImportId = import.Id;

                                            await exhaustiveSearchInstanceVariableHistogramAnomalyRepository
                                                .InsertAsync(
                                                    exhaustiveSearchInstanceVariableHistogramAnomaly, token)
                                                .ConfigureAwait(false);
                                        }
                                    }

                                    foreach (var exhaustiveSearchInstanceVariableClassification in
                                             exhaustiveSearchInstanceVariable
                                                 .ExhaustiveSearchInstanceVariableClassification)
                                    {
                                        var oldExhaustiveSearchInstanceVariableClassificationId =
                                            exhaustiveSearchInstanceVariableClassification.Id;

                                        exhaustiveSearchInstanceVariableClassification
                                            .ExhaustiveSearchInstanceVariableId = exhaustiveSearchInstanceVariableId;

                                        exhaustiveSearchInstanceVariableClassification.ImportId = import.Id;

                                        var exhaustiveSearchInstanceVariableClassificationId =
                                            await exhaustiveSearchInstanceVariableClassificationRepository.InsertAsync(
                                                    exhaustiveSearchInstanceVariableClassification, token)
                                                .ConfigureAwait(false);

                                        foreach (var exhaustiveSearchInstanceVariableHistogramClassification in
                                                 exhaustiveSearchInstanceVariable
                                                     .ExhaustiveSearchInstanceVariableHistogramClassification.Where(w =>
                                                         w.ExhaustiveSearchInstanceVariableClassificationId ==
                                                         oldExhaustiveSearchInstanceVariableClassificationId))
                                        {
                                            exhaustiveSearchInstanceVariableHistogramClassification
                                                    .ExhaustiveSearchInstanceVariableClassificationId =
                                                exhaustiveSearchInstanceVariableClassificationId;

                                            exhaustiveSearchInstanceVariableHistogramClassification.ImportId =
                                                import.Id;

                                            await exhaustiveSearchInstanceVariableHistogramClassificationRepository
                                                .InsertAsync(
                                                    exhaustiveSearchInstanceVariableHistogramClassification, token)
                                                .ConfigureAwait(false);
                                        }
                                    }

                                    foreach (var exhaustiveSearchInstanceVariableMultiCollinearity in
                                             exhaustiveSearchInstanceVariable
                                                 .ExhaustiveSearchInstanceVariableMultiCollinearity)
                                    {
                                        exhaustiveSearchInstanceVariableMultiCollinearity
                                            .ExhaustiveSearchInstanceVariableId = exhaustiveSearchInstanceVariableId;

                                        exhaustiveSearchInstanceVariableMultiCollinearity.ImportId = import.Id;

                                        exhaustiveSearchInstanceVariableMultiCollinearity
                                                .TestExhaustiveSearchInstanceVariableId =
                                            exhaustiveSearchInstanceVariableMultiCollinearity
                                                .TestExhaustiveSearchInstanceVariableId.HasValue
                                            && exhaustiveSearchInstanceVariableIdMap.TryGetValue(
                                                exhaustiveSearchInstanceVariableMultiCollinearity
                                                    .TestExhaustiveSearchInstanceVariableId.Value,
                                                out var remappedTestVariableId)
                                                ? remappedTestVariableId
                                                : null;

                                        await exhaustiveSearchInstanceVariableMulticollinearityRepository.InsertAsync(
                                                exhaustiveSearchInstanceVariableMultiCollinearity, token)
                                            .ConfigureAwait(false);
                                    }
                                }
                            }
                        }

                        foreach (var oldEntityAnalysisModelCaseWorkflow in oldEntityAnalysisModel
                                     .CaseWorkflow)
                        {
                            oldEntityAnalysisModelCaseWorkflow.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            oldEntityAnalysisModelCaseWorkflow.ImportId = import.Id;

                            oldEntityAnalysisModelCaseWorkflow.VisualisationRegistryGuid
                                = visualisationRegistryRekey.TryGetValue(
                                    oldEntityAnalysisModelCaseWorkflow.VisualisationRegistryGuid,
                                    out var entityAnalysisModelGuidTtlCounter)
                                    ? entityAnalysisModelGuidTtlCounter
                                    : newEntityAnalysisModel.Guid;

                            if (rekey)
                            {
                                caseWorkflowRekey.TryAdd(oldEntityAnalysisModelCaseWorkflow.Guid, Guid.NewGuid());

                                oldEntityAnalysisModelCaseWorkflow.Guid =
                                    caseWorkflowRekey[oldEntityAnalysisModelCaseWorkflow.Guid];
                            }

                            var entityAnalysisModelCaseWorkflowId =
                                (await caseWorkflowRepository.InsertAsync(oldEntityAnalysisModelCaseWorkflow, token))
                                .Id;

                            foreach (var oldCaseWorkflowsXPath in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowXPath)
                            {
                                oldCaseWorkflowsXPath.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                oldCaseWorkflowsXPath.ImportId = import.Id;

                                if (rekey)
                                {
                                    caseWorkflowsXPathRekey.TryAdd(oldCaseWorkflowsXPath.Guid, Guid.NewGuid());
                                    oldCaseWorkflowsXPath.Guid = caseWorkflowsXPathRekey[oldCaseWorkflowsXPath.Guid];
                                }

                                await caseWorkflowXPathRepository.InsertAsync(oldCaseWorkflowsXPath, token);
                            }

                            foreach (var oldCaseWorkflowsAction in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowAction)
                            {
                                oldCaseWorkflowsAction.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                oldCaseWorkflowsAction.ImportId = import.Id;

                                if (rekey)
                                {
                                    caseWorkflowsActionRekey.TryAdd(oldCaseWorkflowsAction.Guid, Guid.NewGuid());
                                    oldCaseWorkflowsAction.Guid = caseWorkflowsActionRekey[oldCaseWorkflowsAction.Guid];
                                }

                                await caseWorkflowActionRepository.InsertAsync(oldCaseWorkflowsAction, token);
                            }

                            foreach (var oldCaseWorkflowsDisplay in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowDisplay)
                            {
                                oldCaseWorkflowsDisplay.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                oldCaseWorkflowsDisplay.ImportId = import.Id;

                                if (rekey)
                                {
                                    caseWorkflowsDisplayRekey.TryAdd(oldCaseWorkflowsDisplay.Guid, Guid.NewGuid());

                                    oldCaseWorkflowsDisplay.Guid =
                                        caseWorkflowsDisplayRekey[oldCaseWorkflowsDisplay.Guid];
                                }

                                await caseWorkflowDisplayRepository.InsertAsync(oldCaseWorkflowsDisplay, token)
                                    .ConfigureAwait(false);
                            }

                            foreach (var oldCaseWorkflowsForm in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowForm)
                            {
                                oldCaseWorkflowsForm.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                oldCaseWorkflowsForm.ImportId = import.Id;

                                if (rekey)
                                {
                                    caseWorkflowsFormRekey.TryAdd(oldCaseWorkflowsForm.Guid, Guid.NewGuid());
                                    oldCaseWorkflowsForm.Guid = caseWorkflowsFormRekey[oldCaseWorkflowsForm.Guid];
                                }

                                await caseWorkflowFormRepository.InsertAsync(oldCaseWorkflowsForm, token);
                            }

                            foreach (var oldCaseWorkflowsFilter in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowFilter)
                            {
                                oldCaseWorkflowsFilter.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                oldCaseWorkflowsFilter.ImportId = import.Id;

                                if (rekey)
                                {
                                    caseWorkflowsFilterRekey.TryAdd(oldCaseWorkflowsFilter.Guid, Guid.NewGuid());
                                    oldCaseWorkflowsFilter.Guid = caseWorkflowsFilterRekey[oldCaseWorkflowsFilter.Guid];
                                }

                                await caseWorkflowFilterRepository.InsertAsync(oldCaseWorkflowsFilter, token);
                            }

                            foreach (var oldCaseWorkflowsMacro in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowMacro)
                            {
                                oldCaseWorkflowsMacro.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                oldCaseWorkflowsMacro.ImportId = import.Id;

                                if (rekey)
                                {
                                    caseWorkflowsMacroRekey.TryAdd(oldCaseWorkflowsMacro.Guid, Guid.NewGuid());
                                    oldCaseWorkflowsMacro.Guid = caseWorkflowsMacroRekey[oldCaseWorkflowsMacro.Guid];
                                }

                                await caseWorkflowMacro.InsertAsync(oldCaseWorkflowsMacro, token);
                            }

                            foreach (var caseWorkflowsStatus in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowStatus)
                            {
                                caseWorkflowsStatus.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                caseWorkflowsStatus.ImportId = import.Id;

                                if (rekey)
                                {
                                    caseWorkflowsStatusRekey.TryAdd(caseWorkflowsStatus.Guid, Guid.NewGuid());
                                    caseWorkflowsStatus.Guid = caseWorkflowsStatusRekey[caseWorkflowsStatus.Guid];
                                }

                                await caseWorkflowStatusRepository.InsertAsync(caseWorkflowsStatus, token);
                            }
                        }

                        foreach (var entityAnalysisModelActivationRule in oldEntityAnalysisModel
                                     .EntityAnalysisModelActivationRule)
                        {
                            var oldActivationRuleId = entityAnalysisModelActivationRule.Id;
                            var oldActivationRuleVersion = entityAnalysisModelActivationRule.Version;

                            entityAnalysisModelActivationRule.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelActivationRule.ImportId = import.Id;

                            if (rekey)
                            {
                                entityAnalysisModelActivationRuleRekey.TryAdd(entityAnalysisModelActivationRule.Guid,
                                    Guid.NewGuid());

                                entityAnalysisModelActivationRule.Guid =
                                    entityAnalysisModelActivationRuleRekey[entityAnalysisModelActivationRule.Guid];
                            }

                            entityAnalysisModelActivationRule.EntityAnalysisModelGuidTtlCounter
                                = entityAnalysisModelRekey.TryGetValue(
                                    entityAnalysisModelActivationRule.EntityAnalysisModelGuidTtlCounter,
                                    out var entityAnalysisModelGuidTtlCounter)
                                    ? entityAnalysisModelGuidTtlCounter
                                    : entityAnalysisModelActivationRule.EntityAnalysisModelGuidTtlCounter;

                            entityAnalysisModelActivationRule.EntityAnalysisModelTtlCounterGuid
                                = entityAnalysisModelTtlCounterRekey.TryGetValue(
                                    entityAnalysisModelActivationRule.EntityAnalysisModelTtlCounterGuid,
                                    out var entityAnalysisModelTtlCounterGuid)
                                    ? entityAnalysisModelTtlCounterGuid
                                    : entityAnalysisModelActivationRule.EntityAnalysisModelTtlCounterGuid;

                            entityAnalysisModelActivationRule.CaseWorkflowStatusGuid
                                = caseWorkflowsStatusRekey.TryGetValue(
                                    entityAnalysisModelActivationRule.CaseWorkflowStatusGuid,
                                    out var caseWorkflowStatusGuid)
                                    ? caseWorkflowStatusGuid
                                    : entityAnalysisModelActivationRule.CaseWorkflowStatusGuid;

                            entityAnalysisModelActivationRule.CaseWorkflowGuid
                                = caseWorkflowRekey.TryGetValue(entityAnalysisModelActivationRule.CaseWorkflowGuid,
                                    out var caseWorkflowGuid)
                                    ? caseWorkflowGuid
                                    : entityAnalysisModelActivationRule.CaseWorkflowGuid;

                            await entityAnalysisModelActivationRuleRepository.InsertAsync(
                                entityAnalysisModelActivationRule, token);

                            await CarryVersionForApprovalReplayAsync(entityAnalysisModelActivationRule,
                                oldActivationRuleVersion, oldActivationRuleId, entityAnalysisModelActivationRule.Id,
                                EntityApprovalKind.EntityAnalysisModelActivationRule, entityApprovalEntityIdMaps,
                                token);
                        }

                        if (oldEntityAnalysisModel.EntityAnalysisModelList != null)
                        {
                            foreach (var oldEntityAnalysisModelList in oldEntityAnalysisModel.EntityAnalysisModelList)
                            {
                                var oldEntityAnalysisModelListId = oldEntityAnalysisModelList.Id;
                                var oldEntityAnalysisModelListVersion = oldEntityAnalysisModelList.Version;

                                if (rekey)
                                {
                                    entityAnalysisModelListRekey.TryAdd(oldEntityAnalysisModelList.Guid,
                                        Guid.NewGuid());

                                    oldEntityAnalysisModelList.Guid =
                                        entityAnalysisModelListRekey[oldEntityAnalysisModelList.Guid];
                                }

                                oldEntityAnalysisModelList.EntityAnalysisModelGuid
                                    = entityAnalysisModelRekey.TryGetValue(
                                        oldEntityAnalysisModelList.EntityAnalysisModelGuid, out var guid)
                                        ? guid
                                        : newEntityAnalysisModel.Guid;

                                oldEntityAnalysisModelList.ImportId = import.Id;

                                var newEntityAnalysisModelListId =
                                    (await entityAnalysisModelListRepository.InsertAsync(oldEntityAnalysisModelList,
                                        token)).Id;

                                await CarryVersionForApprovalReplayAsync(oldEntityAnalysisModelList,
                                    oldEntityAnalysisModelListVersion, oldEntityAnalysisModelListId,
                                    newEntityAnalysisModelListId, EntityApprovalKind.EntityAnalysisModelList,
                                    entityApprovalEntityIdMaps, token);

                                foreach (var entityAnalysisModelListValue in oldEntityAnalysisModelList
                                             .EntityAnalysisModelListValue)
                                {
                                    var oldEntityAnalysisModelListValueId = entityAnalysisModelListValue.Id;

                                    var oldEntityAnalysisModelListValueVersion =
                                        entityAnalysisModelListValue.Version;

                                    entityAnalysisModelListValue.EntityAnalysisModelListId =
                                        newEntityAnalysisModelListId;

                                    entityAnalysisModelListValue.ImportId = import.Id;

                                    if (rekey)
                                    {
                                        entityAnalysisModelListValueRekey.TryAdd(entityAnalysisModelListValue.Guid,
                                            Guid.NewGuid());

                                        entityAnalysisModelListValue.Guid =
                                            entityAnalysisModelListValueRekey[entityAnalysisModelListValue.Guid];
                                    }

                                    await entityAnalysisModelListValueRepository.InsertAsync(
                                        entityAnalysisModelListValue, token);

                                    await CarryVersionForApprovalReplayAsync(entityAnalysisModelListValue,
                                        oldEntityAnalysisModelListValueVersion, oldEntityAnalysisModelListValueId,
                                        entityAnalysisModelListValue.Id,
                                        EntityApprovalKind.EntityAnalysisModelListValue, entityApprovalEntityIdMaps,
                                        token);
                                }
                            }
                        }

                        if (oldEntityAnalysisModel.EntityAnalysisModelDictionary != null)
                        {
                            foreach (var oldEntityAnalysisModelDictionary in oldEntityAnalysisModel
                                         .EntityAnalysisModelDictionary)
                            {
                                var oldEntityAnalysisModelDictionaryId = oldEntityAnalysisModelDictionary.Id;

                                var oldEntityAnalysisModelDictionaryVersion =
                                    oldEntityAnalysisModelDictionary.Version;

                                oldEntityAnalysisModelDictionary.ImportId = import.Id;

                                oldEntityAnalysisModelDictionary.EntityAnalysisModelGuid
                                    = entityAnalysisModelRekey.TryGetValue(
                                        oldEntityAnalysisModelDictionary.EntityAnalysisModelGuid, out var guid)
                                        ? guid
                                        : newEntityAnalysisModel.Guid;

                                if (rekey)
                                {
                                    entityAnalysisModelDictionaryRekey.TryAdd(oldEntityAnalysisModelDictionary.Guid,
                                        Guid.NewGuid());

                                    oldEntityAnalysisModelDictionary.Guid =
                                        entityAnalysisModelDictionaryRekey[oldEntityAnalysisModelDictionary.Guid];
                                }

                                var newEntityAnalysisModelDictionaryId = (await entityAnalysisModelDictionaryRepository
                                    .InsertAsync(oldEntityAnalysisModelDictionary, token)).Id;

                                await CarryVersionForApprovalReplayAsync(oldEntityAnalysisModelDictionary,
                                    oldEntityAnalysisModelDictionaryVersion, oldEntityAnalysisModelDictionaryId,
                                    newEntityAnalysisModelDictionaryId,
                                    EntityApprovalKind.EntityAnalysisModelDictionary, entityApprovalEntityIdMaps,
                                    token);

                                foreach (var oldEntityAnalysisModelDictionaryKvp in oldEntityAnalysisModelDictionary
                                             .EntityAnalysisModelDictionaryKvp)
                                {
                                    var oldEntityAnalysisModelDictionaryKvpId = oldEntityAnalysisModelDictionaryKvp.Id;

                                    var oldEntityAnalysisModelDictionaryKvpVersion =
                                        oldEntityAnalysisModelDictionaryKvp.Version;

                                    oldEntityAnalysisModelDictionaryKvp.EntityAnalysisModelDictionaryId =
                                        newEntityAnalysisModelDictionaryId;

                                    oldEntityAnalysisModelDictionaryKvp.ImportId = import.Id;

                                    if (rekey)
                                    {
                                        entityAnalysisModelDictionaryKvpRekey.TryAdd(
                                            oldEntityAnalysisModelDictionaryKvp.Guid, Guid.NewGuid());

                                        oldEntityAnalysisModelDictionaryKvp.Guid =
                                            entityAnalysisModelDictionaryKvpRekey[
                                                oldEntityAnalysisModelDictionaryKvp.Guid];
                                    }

                                    await entityAnalysisModelDictionaryKvpRepository.InsertAsync(
                                        oldEntityAnalysisModelDictionaryKvp, token);

                                    await CarryVersionForApprovalReplayAsync(oldEntityAnalysisModelDictionaryKvp,
                                        oldEntityAnalysisModelDictionaryKvpVersion,
                                        oldEntityAnalysisModelDictionaryKvpId, oldEntityAnalysisModelDictionaryKvp.Id,
                                        EntityApprovalKind.EntityAnalysisModelDictionaryKvp,
                                        entityApprovalEntityIdMaps, token);
                                }
                            }
                        }
                    }
                }

                if (wrapper.Payload?.AdvancedAnalytics != null)
                {
                    var askJoobyAnalyticsInstanceIdMap =
                        await new AdvancedAnalyticsPreservation(dbContext, import.TenantRegistryId, userName)
                            .ImportAsync(wrapper.Payload.AdvancedAnalytics, entityAnalysisModelIdMap, token)
                            .ConfigureAwait(false);

                    foreach (var (oldAskJoobyAnalyticsInstanceId, newAskJoobyAnalyticsInstanceId) in
                             askJoobyAnalyticsInstanceIdMap)
                    {
                        entityApprovalEntityIdMaps[EntityApprovalKind.AskJoobyAnalyticsInstance][
                            oldAskJoobyAnalyticsInstanceId] = newAskJoobyAnalyticsInstanceId;
                    }
                }

                if (wrapper.Payload?.EntityApproval != null)
                {
                    foreach (var entityApproval in wrapper.Payload.EntityApproval)
                    {
                        if (entityApproval.EntityApprovalKindId == null || entityApproval.EntityId == null)
                        {
                            continue;
                        }

                        if (!Enum.IsDefined(typeof(EntityApprovalKind), entityApproval.EntityApprovalKindId.Value))
                        {
                            continue;
                        }

                        var entityApprovalKind = (EntityApprovalKind)entityApproval.EntityApprovalKindId.Value;

                        if (!entityApprovalEntityIdMaps[entityApprovalKind]
                                .TryGetValue(entityApproval.EntityId.Value, out var newEntityApprovalEntityId))
                        {
                            continue;
                        }

                        entityApproval.EntityId = newEntityApprovalEntityId;
                        entityApproval.TenantRegistryId = import.TenantRegistryId;
                        entityApproval.Guid = Guid.NewGuid();
                        entityApproval.Id = await dbContext.InsertWithInt32IdentityAsync(entityApproval, token: token);
                    }
                }
                else
                {
                    var approveEntireModelQuery = new ApproveEntireModelQuery(dbContext);

                    foreach (var importedEntityAnalysisModelId in importedEntityAnalysisModelIds)
                    {
                        await approveEntireModelQuery
                            .ExecuteAsync(importedEntityAnalysisModelId, userName, token);
                    }

                    await approveEntireModelQuery
                        .ExecuteForUnallocatedAdvancedAnalyticsAsync(import.TenantRegistryId, userName, token);
                }

                if (wrapper.Payload?.RoleRegistry != null)
                {
                    foreach (var roleRegistry in wrapper.Payload.RoleRegistry)
                    {
                        var rekey = false;

                        var existsAnywhere = roleRegistryAcrossAllTenants
                            .FirstOrDefault(f => f.Guid == roleRegistry.Guid) != null;

                        if (existsAnywhere)
                        {
                            var existsInTenant = roleRegistryAcrossAllTenants
                                                     .FirstOrDefault(f =>
                                                         f.Guid == roleRegistry.Guid &&
                                                         f.TenantRegistryId == import.TenantRegistryId) !=
                                                 null;

                            rekey = !existsInTenant;
                        }

                        if (rekey)
                        {
                            var userRegistryRepositoryForRekey =
                                new UserRegistryRepository(dbContext, import.TenantRegistryId);

                            if (await userRegistryRepositoryForRekey.AnyAsync(token))
                            {
                                throw new UserRegistryRecordsExistOnRoleRegistryRekeyImportException();
                            }

                            roleRegistryRekey.Add(roleRegistry.Guid, Guid.NewGuid());
                            roleRegistry.Guid = roleRegistryRekey[roleRegistry.Guid];
                        }

                        roleRegistry.ImportId = import.Id;

                        var newRoleRegistry = await roleRegistryRepository.InsertAsync(roleRegistry, token);

                        foreach (var roleRegistryPermission in roleRegistry.RoleRegistryPermission)
                        {
                            roleRegistryPermission.RoleRegistryId = newRoleRegistry.Id;
                            roleRegistryPermission.ImportId = import.Id;

                            if (rekey)
                            {
                                roleRegistryPermissionRekey.TryAdd(roleRegistryPermission.Guid, Guid.NewGuid());
                                roleRegistryPermission.Guid = roleRegistryPermissionRekey[roleRegistryPermission.Guid];
                            }

                            await roleRegistryPermissionRepository.InsertAsync(roleRegistryPermission, token);
                        }
                    }
                }

                if (wrapper.Payload is { EntityPermission: not null })
                {
                    if (wrapper.Payload.EntityPermission.VisualisationRegistryRole != null)
                    {
                        foreach (var visualisationRegistryRole in wrapper.Payload.EntityPermission
                                     .VisualisationRegistryRole)
                        {
                            visualisationRegistryRole.VisualisationRegistryGuid
                                = visualisationRegistryRekey.TryGetValue(
                                    visualisationRegistryRole.VisualisationRegistryGuid, out var entityGuid)
                                    ? entityGuid
                                    : visualisationRegistryRole.VisualisationRegistryGuid;

                            visualisationRegistryRole.RoleRegistryGuid
                                = roleRegistryRekey.TryGetValue(visualisationRegistryRole.RoleRegistryGuid,
                                    out var roleGuid)
                                    ? roleGuid
                                    : visualisationRegistryRole.RoleRegistryGuid;

                            visualisationRegistryRole.ImportId = import.Id;

                            await visualisationRegistryRoleRepository.InsertAsync(visualisationRegistryRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.VisualisationRegistryParameterRole != null)
                    {
                        foreach (var visualisationRegistryParameterRole in wrapper.Payload.EntityPermission
                                     .VisualisationRegistryParameterRole)
                        {
                            visualisationRegistryParameterRole.VisualisationRegistryParameterGuid
                                = visualisationRegistryParameterRekey.TryGetValue(
                                    visualisationRegistryParameterRole.VisualisationRegistryParameterGuid,
                                    out var entityGuid)
                                    ? entityGuid
                                    : visualisationRegistryParameterRole.VisualisationRegistryParameterGuid;

                            visualisationRegistryParameterRole.RoleRegistryGuid
                                = roleRegistryRekey.TryGetValue(visualisationRegistryParameterRole.RoleRegistryGuid,
                                    out var roleGuid)
                                    ? roleGuid
                                    : visualisationRegistryParameterRole.RoleRegistryGuid;

                            visualisationRegistryParameterRole.ImportId = import.Id;

                            await visualisationRegistryParameterRoleRepository.InsertAsync(
                                visualisationRegistryParameterRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.VisualisationRegistryDatasourceRole != null)
                    {
                        foreach (var visualisationRegistryDatasourceRole in wrapper.Payload.EntityPermission
                                     .VisualisationRegistryDatasourceRole)
                        {
                            visualisationRegistryDatasourceRole.VisualisationRegistryDatasourceGuid
                                = visualisationRegistryDatasourceRekey.TryGetValue(
                                    visualisationRegistryDatasourceRole.VisualisationRegistryDatasourceGuid,
                                    out var entityGuid)
                                    ? entityGuid
                                    : visualisationRegistryDatasourceRole.VisualisationRegistryDatasourceGuid;

                            visualisationRegistryDatasourceRole.RoleRegistryGuid
                                = roleRegistryRekey.TryGetValue(visualisationRegistryDatasourceRole.RoleRegistryGuid,
                                    out var roleGuid)
                                    ? roleGuid
                                    : visualisationRegistryDatasourceRole.RoleRegistryGuid;

                            visualisationRegistryDatasourceRole.ImportId = import.Id;

                            await visualisationRegistryDatasourceRoleRepository.InsertAsync(
                                visualisationRegistryDatasourceRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.EntityAnalysisModelRole != null)
                    {
                        foreach (var entityAnalysisModelRole in
                                 wrapper.Payload.EntityPermission.EntityAnalysisModelRole)
                        {
                            entityAnalysisModelRole.EntityAnalysisModelGuid
                                = entityAnalysisModelRekey.TryGetValue(entityAnalysisModelRole.EntityAnalysisModelGuid,
                                    out var entityGuid)
                                    ? entityGuid
                                    : entityAnalysisModelRole.EntityAnalysisModelGuid;

                            entityAnalysisModelRole.RoleRegistryGuid
                                = roleRegistryRekey.TryGetValue(entityAnalysisModelRole.RoleRegistryGuid,
                                    out var roleGuid)
                                    ? roleGuid
                                    : entityAnalysisModelRole.RoleRegistryGuid;

                            entityAnalysisModelRole.ImportId = import.Id;

                            await entityAnalysisModelRoleRepository.InsertAsync(entityAnalysisModelRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowRole != null)
                    {
                        foreach (var caseWorkflowRole in wrapper.Payload.EntityPermission.CaseWorkflowRole)
                        {
                            caseWorkflowRole.CaseWorkflowGuid
                                = caseWorkflowRekey.TryGetValue(caseWorkflowRole.CaseWorkflowGuid, out var entityGuid)
                                    ? entityGuid
                                    : caseWorkflowRole.CaseWorkflowGuid;

                            caseWorkflowRole.RoleRegistryGuid
                                = roleRegistryRekey.TryGetValue(caseWorkflowRole.RoleRegistryGuid, out var roleGuid)
                                    ? roleGuid
                                    : caseWorkflowRole.RoleRegistryGuid;

                            caseWorkflowRole.ImportId = import.Id;

                            await caseWorkflowRoleRepository.InsertAsync(caseWorkflowRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowActionRole != null)
                    {
                        foreach (var caseWorkflowActionRole in wrapper.Payload.EntityPermission.CaseWorkflowActionRole)
                        {
                            caseWorkflowActionRole.CaseWorkflowActionGuid
                                = caseWorkflowsActionRekey.TryGetValue(caseWorkflowActionRole.CaseWorkflowActionGuid,
                                    out var entityGuid)
                                    ? entityGuid
                                    : caseWorkflowActionRole.CaseWorkflowActionGuid;

                            caseWorkflowActionRole.RoleRegistryGuid
                                = roleRegistryRekey.TryGetValue(caseWorkflowActionRole.RoleRegistryGuid,
                                    out var roleGuid)
                                    ? roleGuid
                                    : caseWorkflowActionRole.RoleRegistryGuid;

                            caseWorkflowActionRole.ImportId = import.Id;

                            await caseWorkflowActionRoleRepository.InsertAsync(caseWorkflowActionRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowDisplayRole != null)
                    {
                        foreach (var caseWorkflowDisplayRole in
                                 wrapper.Payload.EntityPermission.CaseWorkflowDisplayRole)
                        {
                            caseWorkflowDisplayRole.CaseWorkflowDisplayGuid
                                = caseWorkflowsDisplayRekey.TryGetValue(caseWorkflowDisplayRole.CaseWorkflowDisplayGuid,
                                    out var entityGuid)
                                    ? entityGuid
                                    : caseWorkflowDisplayRole.CaseWorkflowDisplayGuid;

                            caseWorkflowDisplayRole.RoleRegistryGuid
                                = roleRegistryRekey.TryGetValue(caseWorkflowDisplayRole.RoleRegistryGuid,
                                    out var roleGuid)
                                    ? roleGuid
                                    : caseWorkflowDisplayRole.RoleRegistryGuid;

                            caseWorkflowDisplayRole.ImportId = import.Id;

                            await caseWorkflowDisplayRoleRepository.InsertAsync(caseWorkflowDisplayRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowFilterRole != null)
                    {
                        foreach (var caseWorkflowFilterRole in wrapper.Payload.EntityPermission.CaseWorkflowFilterRole)
                        {
                            caseWorkflowFilterRole.CaseWorkflowFilterGuid
                                = caseWorkflowsFilterRekey.TryGetValue(caseWorkflowFilterRole.CaseWorkflowFilterGuid,
                                    out var entityGuid)
                                    ? entityGuid
                                    : caseWorkflowFilterRole.CaseWorkflowFilterGuid;

                            caseWorkflowFilterRole.RoleRegistryGuid
                                = roleRegistryRekey.TryGetValue(caseWorkflowFilterRole.RoleRegistryGuid,
                                    out var roleGuid)
                                    ? roleGuid
                                    : caseWorkflowFilterRole.RoleRegistryGuid;

                            caseWorkflowFilterRole.ImportId = import.Id;

                            await caseWorkflowFilterRoleRepository.InsertAsync(caseWorkflowFilterRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowFormRole != null)
                    {
                        foreach (var caseWorkflowFormRole in wrapper.Payload.EntityPermission.CaseWorkflowFormRole)
                        {
                            caseWorkflowFormRole.CaseWorkflowFormGuid
                                = caseWorkflowsFormRekey.TryGetValue(caseWorkflowFormRole.CaseWorkflowFormGuid,
                                    out var entityGuid)
                                    ? entityGuid
                                    : caseWorkflowFormRole.CaseWorkflowFormGuid;

                            caseWorkflowFormRole.RoleRegistryGuid
                                = roleRegistryRekey.TryGetValue(caseWorkflowFormRole.RoleRegistryGuid, out var roleGuid)
                                    ? roleGuid
                                    : caseWorkflowFormRole.RoleRegistryGuid;

                            caseWorkflowFormRole.ImportId = import.Id;

                            await caseWorkflowFormRoleRepository.InsertAsync(caseWorkflowFormRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowFormValidator != null)
                    {
                        foreach (var caseWorkflowFormValidator in wrapper.Payload.EntityPermission
                                     .CaseWorkflowFormValidator)
                        {
                            caseWorkflowFormValidator.CaseWorkflowFormGuid
                                = caseWorkflowsFormRekey.TryGetValue(caseWorkflowFormValidator.CaseWorkflowFormGuid,
                                    out var entityGuid)
                                    ? entityGuid
                                    : caseWorkflowFormValidator.CaseWorkflowFormGuid;

                            caseWorkflowFormValidator.ImportId = import.Id;

                            await caseWorkflowFormValidatorRepository.InsertAsync(caseWorkflowFormValidator, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowReleaseHook != null)
                    {
                        foreach (var caseWorkflowReleaseHook in wrapper.Payload.EntityPermission
                                     .CaseWorkflowReleaseHook)
                        {
                            caseWorkflowReleaseHook.CaseWorkflowGuid
                                = caseWorkflowRekey.TryGetValue(caseWorkflowReleaseHook.CaseWorkflowGuid,
                                    out var entityGuid)
                                    ? entityGuid
                                    : caseWorkflowReleaseHook.CaseWorkflowGuid;

                            caseWorkflowReleaseHook.ImportId = import.Id;

                            await caseWorkflowReleaseHookRepository.InsertAsync(caseWorkflowReleaseHook, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowMacroRole != null)
                    {
                        foreach (var caseWorkflowMacroRole in wrapper.Payload.EntityPermission.CaseWorkflowMacroRole)
                        {
                            caseWorkflowMacroRole.CaseWorkflowMacroGuid
                                = caseWorkflowsMacroRekey.TryGetValue(caseWorkflowMacroRole.CaseWorkflowMacroGuid,
                                    out var entityGuid)
                                    ? entityGuid
                                    : caseWorkflowMacroRole.CaseWorkflowMacroGuid;

                            caseWorkflowMacroRole.RoleRegistryGuid
                                = roleRegistryRekey.TryGetValue(caseWorkflowMacroRole.RoleRegistryGuid,
                                    out var roleGuid)
                                    ? roleGuid
                                    : caseWorkflowMacroRole.RoleRegistryGuid;

                            caseWorkflowMacroRole.ImportId = import.Id;

                            await caseWorkflowMacroRoleRepository.InsertAsync(caseWorkflowMacroRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowStatusRole != null)
                    {
                        foreach (var caseWorkflowStatusRole in wrapper.Payload.EntityPermission.CaseWorkflowStatusRole)
                        {
                            caseWorkflowStatusRole.CaseWorkflowStatusGuid
                                = caseWorkflowsStatusRekey.TryGetValue(caseWorkflowStatusRole.CaseWorkflowStatusGuid,
                                    out var entityGuid)
                                    ? entityGuid
                                    : caseWorkflowStatusRole.CaseWorkflowStatusGuid;

                            caseWorkflowStatusRole.RoleRegistryGuid
                                = roleRegistryRekey.TryGetValue(caseWorkflowStatusRole.RoleRegistryGuid,
                                    out var roleGuid)
                                    ? roleGuid
                                    : caseWorkflowStatusRole.RoleRegistryGuid;

                            caseWorkflowStatusRole.ImportId = import.Id;

                            await caseWorkflowStatusRoleRepository.InsertAsync(caseWorkflowStatusRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowXPathRole != null)
                    {
                        foreach (var caseWorkflowXPathRole in wrapper.Payload.EntityPermission.CaseWorkflowXPathRole)
                        {
                            caseWorkflowXPathRole.CaseWorkflowXPathGuid
                                = caseWorkflowsXPathRekey.TryGetValue(caseWorkflowXPathRole.CaseWorkflowXPathGuid,
                                    out var entityGuid)
                                    ? entityGuid
                                    : caseWorkflowXPathRole.CaseWorkflowXPathGuid;

                            caseWorkflowXPathRole.RoleRegistryGuid
                                = roleRegistryRekey.TryGetValue(caseWorkflowXPathRole.RoleRegistryGuid,
                                    out var roleGuid)
                                    ? roleGuid
                                    : caseWorkflowXPathRole.RoleRegistryGuid;

                            caseWorkflowXPathRole.ImportId = import.Id;

                            await caseWorkflowXPathRoleRepository.InsertAsync(caseWorkflowXPathRole, token);
                        }
                    }
                }

                await new RecoverOrphanedUsersQuery(dbContext, import.TenantRegistryId, userName)
                    .ExecuteAsync(token);

                await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);
                import.CompletedDate = DateTime.UtcNow;
                import.ExportGuid = wrapper.Guid;
                import.ExportVersion = wrapper.Version;

                await importRepository.UpdateAsync(import, token);
            }
            catch (Exception ex)
            {
                await dbContext.RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false);

                import.InError = 1;
                import.ErrorStack = ex.ToString();

                await importRepository.UpdateAsync(import, CancellationToken.None);
                throw;
            }
        }

        public async Task<Export> ExportAsync(ExportOptions options, CancellationToken token = default)
        {
            if (!JempAesEncryption.IsAcceptablePassword(options.Password))
            {
                throw new InvalidPasswordException(JempAesEncryption.MinimumPasswordLength);
            }

            var exportRepository = new ExportRepository(dbContext, userName);

            var export = new Export
            {
                CreatedDate = DateTime.UtcNow,
                Guid = Guid.NewGuid()
            };

            export = await exportRepository.InsertAsync(export, token);

            try
            {
                var wrapper = new Wrapper
                {
                    Guid = export.Guid,
                    Version = 2,
                    Payload = await ExportPayloadAsync(options, export.TenantRegistryId, token).ConfigureAwait(false)
                };

                var lz4Options =
                    MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

                var bytes = MessagePackSerializer.Serialize(wrapper, lz4Options);

                var aesEncryption = new JempAesEncryption(options.Password ?? "");
                var encryptedBytes = aesEncryption.Encrypt(bytes);

                export.Bytes = bytes;
                export.EncryptedBytes = encryptedBytes;
                export.CompletedDate = DateTime.UtcNow;
                export.Version = wrapper.Version;

                await exportRepository.UpdateAsync(export, token);

                return export;
            }
            catch (Exception ex)
            {
                export.InError = 1;
                export.ErrorStack = ex.ToString();
                await exportRepository.UpdateAsync(export, token);
                throw;
            }
        }

        public async Task<Payload> LivePayloadAsync(ExportOptions options, CancellationToken token = default)
        {
            var tenantRegistryId = await dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefaultAsync(token).ConfigureAwait(false);

            return await ExportPayloadAsync(options, tenantRegistryId, token).ConfigureAwait(false);
        }

        public static string PayloadJson(Payload payload)
        {
            return JsonConvert.SerializeObject(payload, Formatting.None, BodyJsonSettings);
        }

        public Wrapper ReadPackage(byte[] bytes, ImportOptions options)
        {
            var aesEncryption = new JempAesEncryption(options.Password ?? "");
            var decryptedBytes = aesEncryption.Decrypt(bytes);

            var lz4Options =
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

            return MessagePackSerializer.Deserialize<Wrapper>(decryptedBytes, lz4Options);
        }

        private async Task CarryVersionForApprovalReplayAsync<T>(T imported, int? oldVersion, int oldId, int newId,
            EntityApprovalKind kind, Dictionary<EntityApprovalKind, Dictionary<int, int>> entityApprovalEntityIdMaps,
            CancellationToken token) where T : class
        {
            if (oldVersion is > 1)
            {
                typeof(T).GetProperty("Version")!.SetValue(imported, oldVersion);
                await dbContext.UpdateAsync(imported, token: token);
            }

            entityApprovalEntityIdMaps[kind][oldId] = newId;
        }

        private async Task<List<EntityApproval>> ExportEntityApprovalsAsync(Payload payload, int tenantRegistryId,
            CancellationToken token = default)
        {
            var entityApprovalRepository = new EntityApprovalRepository(dbContext, tenantRegistryId);
            var entityIdsByKind = new Dictionary<EntityApprovalKind, List<int>>();

            foreach (var entityApprovalKind in Enum.GetValues<EntityApprovalKind>())
            {
                entityIdsByKind[entityApprovalKind] = [];
            }

            if (payload.EntityAnalysisModel != null)
            {
                foreach (var entityAnalysisModel in payload.EntityAnalysisModel)
                {
                    entityIdsByKind[EntityApprovalKind.EntityAnalysisModel].Add(entityAnalysisModel.Id);

                    entityIdsByKind[EntityApprovalKind.EntityAnalysisModelRequestXPath].AddRange(
                        (entityAnalysisModel.EntityAnalysisModelRequestXpath ?? []).Select(s => s.Id));

                    entityIdsByKind[EntityApprovalKind.EntityAnalysisModelInlineFunction].AddRange(
                        (entityAnalysisModel.EntityAnalysisModelInlineFunction ?? []).Select(s => s.Id));

                    entityIdsByKind[EntityApprovalKind.EntityAnalysisModelInlineScript].AddRange(
                        (entityAnalysisModel.EntityAnalysisModelInlineScript ?? []).Select(s => s.Id));

                    entityIdsByKind[EntityApprovalKind.EntityAnalysisModelGatewayRule].AddRange(
                        (entityAnalysisModel.EntityAnalysisModelGatewayRule ?? []).Select(s => s.Id));

                    entityIdsByKind[EntityApprovalKind.EntityAnalysisModelSanction].AddRange(
                        (entityAnalysisModel.EntityAnalysisModelSanction ?? []).Select(s => s.Id));

                    entityIdsByKind[EntityApprovalKind.EntityAnalysisModelTag].AddRange(
                        (entityAnalysisModel.EntityAnalysisModelTag ?? []).Select(s => s.Id));

                    entityIdsByKind[EntityApprovalKind.EntityAnalysisModelResponseElevationRange].AddRange(
                        (entityAnalysisModel.EntityAnalysisModelResponseElevationRange ?? []).Select(s => s.Id));

                    entityIdsByKind[EntityApprovalKind.EntityAnalysisModelTtlCounter].AddRange(
                        (entityAnalysisModel.EntityAnalysisModelTtlCounter ?? []).Select(s => s.Id));

                    entityIdsByKind[EntityApprovalKind.EntityAnalysisModelAbstractionRule].AddRange(
                        (entityAnalysisModel.EntityAnalysisModelAbstractionRule ?? []).Select(s => s.Id));

                    entityIdsByKind[EntityApprovalKind.EntityAnalysisModelAbstractionCalculation].AddRange(
                        (entityAnalysisModel.EntityAnalysisModelAbstractionCalculation ?? []).Select(s => s.Id));

                    entityIdsByKind[EntityApprovalKind.EntityAnalysisModelHttpAdaptation].AddRange(
                        (entityAnalysisModel.EntityAnalysisModelHttpAdaptation ?? []).Select(s => s.Id));

                    entityIdsByKind[EntityApprovalKind.EntityAnalysisModelActivationRule].AddRange(
                        (entityAnalysisModel.EntityAnalysisModelActivationRule ?? []).Select(s => s.Id));

                    entityIdsByKind[EntityApprovalKind.ExhaustiveSearchInstance].AddRange(
                        (entityAnalysisModel.ExhaustiveSearchInstance ?? []).Select(s => s.Id));

                    foreach (var entityAnalysisModelList in entityAnalysisModel.EntityAnalysisModelList ?? [])
                    {
                        entityIdsByKind[EntityApprovalKind.EntityAnalysisModelList].Add(entityAnalysisModelList.Id);

                        entityIdsByKind[EntityApprovalKind.EntityAnalysisModelListValue].AddRange(
                            (entityAnalysisModelList.EntityAnalysisModelListValue ?? []).Select(s => s.Id));
                    }

                    foreach (var entityAnalysisModelDictionary in entityAnalysisModel.EntityAnalysisModelDictionary
                                                                  ?? [])
                    {
                        entityIdsByKind[EntityApprovalKind.EntityAnalysisModelDictionary].Add(
                            entityAnalysisModelDictionary.Id);

                        entityIdsByKind[EntityApprovalKind.EntityAnalysisModelDictionaryKvp].AddRange(
                            (entityAnalysisModelDictionary.EntityAnalysisModelDictionaryKvp ?? [])
                            .Select(s => s.Id));
                    }
                }
            }

            if (payload.AdvancedAnalytics?.Instances != null)
            {
                entityIdsByKind[EntityApprovalKind.AskJoobyAnalyticsInstance].AddRange(
                    payload.AdvancedAnalytics.Instances.Select(s => s.Id));
            }

            var entityApprovals = new List<EntityApproval>();

            foreach (var (entityApprovalKind, entityIds) in entityIdsByKind)
            {
                entityApprovals.AddRange(await entityApprovalRepository.GetByKindAndEntityIdsAsync(entityApprovalKind,
                    entityIds.Distinct().ToList(), token).ConfigureAwait(false));
            }

            foreach (var entityApproval in entityApprovals)
            {
                entityApproval.TenantRegistryId = null;
            }

            return entityApprovals;
        }

        private async Task<Payload> ExportPayloadAsync(ExportOptions options, int tenantRegistryId,
            CancellationToken token = default)
        {
            var payload = new Payload
            {
                EntityPermission = new EntityPermission()
            };

            if (options.Roles)
            {
                var roleRegistryRepository = new RoleRegistryRepository(dbContext, tenantRegistryId);
                payload.RoleRegistry = await roleRegistryRepository.GetAsync(token);

                foreach (var roleRegistry in payload.RoleRegistry)
                {
                    var roleRegistryPermissionRepository =
                        new RoleRegistryPermissionRepository(dbContext, tenantRegistryId);

                    roleRegistry.RoleRegistryPermission =
                        await roleRegistryPermissionRepository
                            .GetByRoleRegistryIdOrderByIdAsync(roleRegistry.Id, token);
                }
            }

            if (options.AdvancedAnalytics)
            {
                var advancedAnalyticsPreservation = new AdvancedAnalyticsPreservation(dbContext, tenantRegistryId,
                    userName);

                payload.AdvancedAnalytics = await advancedAnalyticsPreservation.ExportAsync(token)
                    .ConfigureAwait(false);
            }

            if (options.Exhaustive)
            {
                var exhaustiveInstanceDatasetRepository =
                    new ExhaustiveInstanceDatasetRepository(dbContext, tenantRegistryId);

                payload.ExhaustiveInstanceCsv = (await exhaustiveInstanceDatasetRepository
                        .GetForExportAsync(DatasetKind.Csv, token: token).ConfigureAwait(false))
                    .Select(s => new ExhaustiveInstanceCsvExport
                    {
                        Dataset = (ExhaustiveInstanceCsv)s.Dataset,
                        Rows = s.Rows.Cast<ExhaustiveInstanceCsvRow>().ToList()
                    }).ToList();

                payload.ExhaustiveInstanceJson = (await exhaustiveInstanceDatasetRepository
                        .GetForExportAsync(DatasetKind.Json, token: token).ConfigureAwait(false))
                    .Select(s => new ExhaustiveInstanceJsonExport
                    {
                        Dataset = (ExhaustiveInstanceJson)s.Dataset,
                        Rows = s.Rows.Cast<ExhaustiveInstanceJsonRow>().ToList()
                    }).ToList();
            }

            var entityAnalysisModelRepository = new EntityAnalysisModelRepository(dbContext, tenantRegistryId);
            payload.EntityAnalysisModel = await entityAnalysisModelRepository.GetAsync(token).ConfigureAwait(false);

            foreach (var entityAnalysisModel in payload.EntityAnalysisModel)
            {
                var entityAnalysisModelRequestXPathRepository =
                    new EntityAnalysisModelRequestXpathRepository(dbContext, tenantRegistryId);

                entityAnalysisModel.EntityAnalysisModelRequestXpath
                    = await entityAnalysisModelRequestXPathRepository
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                var entityAnalysisModelInlineFunctionRepository =
                    new EntityAnalysisModelInlineFunctionRepository(dbContext, tenantRegistryId);

                entityAnalysisModel.EntityAnalysisModelInlineFunction
                    = await entityAnalysisModelInlineFunctionRepository
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                var entityAnalysisModelInlineScriptRepository =
                    new EntityAnalysisModelInlineScriptRepository(dbContext, tenantRegistryId);

                entityAnalysisModel.EntityAnalysisModelInlineScript
                    = await entityAnalysisModelInlineScriptRepository
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                var entityAnalysisModelGatewayRuleRepository =
                    new EntityAnalysisModelGatewayRuleRepository(dbContext, tenantRegistryId);

                entityAnalysisModel.EntityAnalysisModelGatewayRule
                    = await entityAnalysisModelGatewayRuleRepository
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                var entityAnalysisModelSanctionRepository =
                    new EntityAnalysisModelSanctionRepository(dbContext, tenantRegistryId);

                entityAnalysisModel.EntityAnalysisModelSanction
                    = await entityAnalysisModelSanctionRepository
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                var entityAnalysisModelTagRepository =
                    new EntityAnalysisModelTagRepository(dbContext, tenantRegistryId);

                entityAnalysisModel.EntityAnalysisModelTag
                    = await entityAnalysisModelTagRepository
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                var entityAnalysisModelResponseElevationRangeRepository =
                    new EntityAnalysisModelResponseElevationRangeRepository(dbContext, tenantRegistryId);

                entityAnalysisModel.EntityAnalysisModelResponseElevationRange
                    = await entityAnalysisModelResponseElevationRangeRepository
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token)
                        .ConfigureAwait(false);

                var entityAnalysisModelTtlCounterRepository =
                    new EntityAnalysisModelTtlCounterRepository(dbContext, tenantRegistryId);

                entityAnalysisModel.EntityAnalysisModelTtlCounter
                    = await entityAnalysisModelTtlCounterRepository
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                var entityAnalysisModelAbstractionRuleRepository =
                    new EntityAnalysisModelAbstractionRuleRepository(dbContext, tenantRegistryId);

                entityAnalysisModel.EntityAnalysisModelAbstractionRule
                    = await entityAnalysisModelAbstractionRuleRepository
                        .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModel.Id, token)
                        .ConfigureAwait(false);

                var entityAnalysisModelAbstractionCalculationRepository =
                    new EntityAnalysisModelAbstractionCalculationRepository(dbContext, tenantRegistryId);

                entityAnalysisModel.EntityAnalysisModelAbstractionCalculation
                    = await entityAnalysisModelAbstractionCalculationRepository
                        .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModel.Id, token)
                        .ConfigureAwait(false);

                var entityAnalysisModelHttpAdaptationRepository =
                    new EntityAnalysisModelHttpAdaptationRepository(dbContext, tenantRegistryId);

                entityAnalysisModel.EntityAnalysisModelHttpAdaptation
                    = await entityAnalysisModelHttpAdaptationRepository
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                if (options.Exhaustive)
                {
                    var exhaustiveSearchInstanceRepository =
                        new ExhaustiveSearchInstanceRepository(dbContext, tenantRegistryId);

                    entityAnalysisModel.ExhaustiveSearchInstance
                        = await exhaustiveSearchInstanceRepository
                            .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token)
                            .ConfigureAwait(false);

                    foreach (var exhaustiveSearchInstance in entityAnalysisModel.ExhaustiveSearchInstance)
                    {
                        var exhaustiveSearchInstanceDataRepository =
                            new ExhaustiveSearchInstanceDataRepository(dbContext);

                        exhaustiveSearchInstance.ExhaustiveSearchInstanceData =
                            await exhaustiveSearchInstanceDataRepository.GetByExhaustiveSearchInstanceIdOrderByIdAsync(
                                exhaustiveSearchInstance.Id, token).ConfigureAwait(false);

                        var exhaustiveSearchInstanceCategoricalFieldRepository =
                            new ExhaustiveSearchInstanceCategoricalFieldRepository(dbContext);

                        exhaustiveSearchInstance.ExhaustiveSearchInstanceCategoricalField =
                            await exhaustiveSearchInstanceCategoricalFieldRepository
                                .GetByExhaustiveSearchInstanceIdOrderByIdAsync(
                                    exhaustiveSearchInstance.Id, token).ConfigureAwait(false);

                        var exhaustiveSearchInstanceTrialInstanceRepository =
                            new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext);

                        exhaustiveSearchInstance.ExhaustiveSearchInstanceTrialInstance =
                            await exhaustiveSearchInstanceTrialInstanceRepository
                                .GetByExhaustiveSearchInstanceIdOrderByIdAsync(
                                    exhaustiveSearchInstance.Id, token).ConfigureAwait(false);

                        var exhaustiveSearchInstanceVariableRepository =
                            new ExhaustiveSearchInstanceVariableRepository(dbContext);

                        exhaustiveSearchInstance.ExhaustiveSearchInstanceVariable =
                            await exhaustiveSearchInstanceVariableRepository
                                .GetByExhaustiveSearchInstanceIdOrderByIdAsync(
                                    exhaustiveSearchInstance.Id, token).ConfigureAwait(false);

                        foreach (var exhaustiveSearchInstanceTrialInstance in exhaustiveSearchInstance
                                     .ExhaustiveSearchInstanceTrialInstance)
                        {
                            var exhaustiveSearchInstanceTrialInstanceVariableRepository =
                                new ExhaustiveSearchInstanceTrialInstanceVariableRepository(dbContext);

                            exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceTrialInstanceVariable =
                                await exhaustiveSearchInstanceTrialInstanceVariableRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id, token);

                            var exhaustiveSearchInstancePromotedTrialInstanceRepository =
                                new ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext);

                            exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstancePromotedTrialInstance =
                                await exhaustiveSearchInstancePromotedTrialInstanceRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository =
                                new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository(dbContext);

                            exhaustiveSearchInstanceTrialInstance
                                    .ExhaustiveSearchInstancePromotedTrialInstancePredictedActual =
                                await exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValueRepository =
                                new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValueRepository(
                                    dbContext);

                            foreach (var exhaustiveSearchInstancePromotedTrialInstancePredictedActual in
                                     exhaustiveSearchInstanceTrialInstance
                                         .ExhaustiveSearchInstancePromotedTrialInstancePredictedActual)
                            {
                                exhaustiveSearchInstancePromotedTrialInstancePredictedActual
                                        .ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue =
                                    await
                                        exhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValueRepository
                                            .GetByExhaustiveSearchInstancePromotedTrialInstancePredictedActualIdOrderByIdAsync(
                                                exhaustiveSearchInstancePromotedTrialInstancePredictedActual.Id, token)
                                            .ConfigureAwait(false);
                            }

                            var exhaustiveSearchInstancePromotedTrialInstanceRocRepository =
                                new ExhaustiveSearchInstancePromotedTrialInstanceRocRepository(dbContext);

                            exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstancePromotedTrialInstanceRoc =
                                await exhaustiveSearchInstancePromotedTrialInstanceRocRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(
                                        exhaustiveSearchInstanceTrialInstance.Id, token);

                            var exhaustiveSearchInstanceTrialInstanceTopologyTrialRepository =
                                new ExhaustiveSearchInstanceTrialInstanceTopologyTrialRepository(dbContext);

                            exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceTrialInstanceTopologyTrial =
                                await exhaustiveSearchInstanceTrialInstanceTopologyTrialRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstanceTrialInstanceSensitivityRepository =
                                new ExhaustiveSearchInstanceTrialInstanceSensitivityRepository(dbContext);

                            exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceTrialInstanceSensitivity =
                                await exhaustiveSearchInstanceTrialInstanceSensitivityRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository =
                                new ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository(dbContext);

                            exhaustiveSearchInstanceTrialInstance
                                    .ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial =
                                await exhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id, token).ConfigureAwait(false);

                            foreach (var exhaustiveSearchInstanceTrialInstanceVariable in
                                     exhaustiveSearchInstanceTrialInstance
                                         .ExhaustiveSearchInstanceTrialInstanceVariable)
                            {
                                var exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository =
                                    new ExhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository(dbContext);

                                exhaustiveSearchInstanceTrialInstanceVariable
                                        .ExhaustiveSearchInstancePromotedTrialInstanceSensitivity =
                                    await exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository
                                        .GetByExhaustiveSearchInstanceTrialInstanceVariableIdOrderByIdAsync(
                                            exhaustiveSearchInstanceTrialInstanceVariable.Id, token)
                                        .ConfigureAwait(false);

                                var exhaustiveSearchInstancePromotedTrialInstanceVariableRepository =
                                    new ExhaustiveSearchInstancePromotedTrialInstanceVariableRepository(dbContext);

                                exhaustiveSearchInstanceTrialInstanceVariable
                                        .ExhaustiveSearchInstancePromotedTrialInstanceVariable =
                                    await exhaustiveSearchInstancePromotedTrialInstanceVariableRepository
                                        .GetByExhaustiveSearchInstanceTrialInstanceVariableIdOrderByIdAsync(
                                            exhaustiveSearchInstanceTrialInstanceVariable.Id, token)
                                        .ConfigureAwait(false);

                                var exhaustiveSearchInstancePrescriptionHistogramRepository =
                                    new ExhaustiveSearchInstancePrescriptionHistogramRepository(dbContext);

                                foreach (var exhaustiveSearchInstancePromotedTrialInstanceVariable in
                                         exhaustiveSearchInstanceTrialInstanceVariable
                                             .ExhaustiveSearchInstancePromotedTrialInstanceVariable)
                                {
                                    exhaustiveSearchInstancePromotedTrialInstanceVariable
                                            .ExhaustiveSearchInstancePrescriptionHistogram =
                                        await exhaustiveSearchInstancePrescriptionHistogramRepository
                                            .GetByPromotedTrialInstanceVariableIdOrderByIdAsync(
                                                exhaustiveSearchInstancePromotedTrialInstanceVariable.Id, token)
                                            .ConfigureAwait(false);
                                }
                            }
                        }

                        foreach (var exhaustiveSearchInstanceVariable in exhaustiveSearchInstance
                                     .ExhaustiveSearchInstanceVariable)
                        {
                            var exhaustiveSearchInstanceVariableAnomalyRepository =
                                new ExhaustiveSearchInstanceVariableAnomalyRepository(dbContext);

                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableAnomaly =
                                await exhaustiveSearchInstanceVariableAnomalyRepository
                                    .GetByExhaustiveSearchInstanceVariableIdOrderByIdAsync(
                                        exhaustiveSearchInstanceVariable
                                            .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstanceVariableClassificationRepository =
                                new ExhaustiveSearchInstanceVariableClassificationRepository(dbContext);

                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableClassification =
                                await exhaustiveSearchInstanceVariableClassificationRepository
                                    .GetByExhaustiveSearchInstanceVariableIdOrderByIdAsync(
                                        exhaustiveSearchInstanceVariable
                                            .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstanceVariableHistogramRepository =
                                new ExhaustiveSearchInstanceVariableHistogramRepository(dbContext);

                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableHistogram =
                                await exhaustiveSearchInstanceVariableHistogramRepository
                                    .GetByExhaustiveSearchInstanceVariableIdOrderByIdAsync(
                                        exhaustiveSearchInstanceVariable
                                            .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstanceVariableHistogramClassRepository =
                                new ExhaustiveSearchInstanceVariableHistogramClassRepository(dbContext);

                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableHistogramClass =
                                await exhaustiveSearchInstanceVariableHistogramClassRepository
                                    .GetByExhaustiveSearchInstanceVariableIdOrderByIdAsync(
                                        exhaustiveSearchInstanceVariable
                                            .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstanceVariableHistogramClassificationRepository =
                                new ExhaustiveSearchInstanceVariableHistogramClassificationRepository(dbContext);

                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableHistogramClassification =
                                await exhaustiveSearchInstanceVariableHistogramClassificationRepository
                                    .GetByExhaustiveSearchInstanceVariableIdAsync(exhaustiveSearchInstanceVariable
                                        .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstanceVariableHistogramAnomalyRepository =
                                new ExhaustiveSearchInstanceVariableHistogramAnomalyRepository(dbContext);

                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableHistogramAnomaly =
                                await exhaustiveSearchInstanceVariableHistogramAnomalyRepository
                                    .GetByExhaustiveSearchInstanceVariableIdOrderByIdAsync(
                                        exhaustiveSearchInstanceVariable
                                            .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstanceVariableMulticollinearityRepository =
                                new ExhaustiveSearchInstanceVariableMultiCollinearityRepository(dbContext);

                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableMultiCollinearity =
                                await exhaustiveSearchInstanceVariableMulticollinearityRepository
                                    .GetByExhaustiveSearchInstanceVariableIdOrderByIdAsync(
                                        exhaustiveSearchInstanceVariable
                                            .Id, token).ConfigureAwait(false);
                        }
                    }
                }

                var entityAnalysisModelActivationRuleRepository =
                    new EntityAnalysisModelActivationRuleRepository(dbContext, tenantRegistryId);

                entityAnalysisModel.EntityAnalysisModelActivationRule
                    = await entityAnalysisModelActivationRuleRepository
                        .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModel.Id, token);

                var caseWorkflowRepository = new CaseWorkflowRepository(dbContext, tenantRegistryId);

                entityAnalysisModel.CaseWorkflow
                    = await caseWorkflowRepository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id,
                        token);

                foreach (var entityAnalysisModelCaseWorkflowEntityAnalysisModel in entityAnalysisModel
                             .CaseWorkflow)
                {
                    var caseWorkflowXPathRepository =
                        new CaseWorkflowXPathRepository(dbContext, tenantRegistryId);

                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowXPath
                        = await caseWorkflowXPathRepository.GetByCasesWorkflowIdOrderByIdDescAsync(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id, token);

                    var caseWorkflowStatusRepository =
                        new CaseWorkflowStatusRepository(dbContext, tenantRegistryId);

                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowStatus
                        = await caseWorkflowStatusRepository.GetByCasesWorkflowIdOrderByIdAsync(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id, token);

                    var caseWorkflowFormRepository = new CaseWorkflowFormRepository(dbContext, tenantRegistryId);

                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowForm
                        = await caseWorkflowFormRepository.GetByCasesWorkflowIdOrderByIdAsync(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id, token);

                    var caseWorkflowActionRepository =
                        new CaseWorkflowActionRepository(dbContext, tenantRegistryId);

                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowAction
                        = await caseWorkflowActionRepository.GetByCasesWorkflowIdOrderByIdAsync(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id, token);

                    var caseWorkflowDisplayRepository =
                        new CaseWorkflowDisplayRepository(dbContext, tenantRegistryId);

                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowDisplay
                        = await caseWorkflowDisplayRepository.GetByCasesWorkflowIdOrderByIdAsync(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id, token);

                    var caseWorkflowMacro = new CaseWorkflowMacroRepository(dbContext, tenantRegistryId);

                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowMacro
                        = await caseWorkflowMacro.GetByCasesWorkflowIdOrderByIdAsync(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id, token);

                    var caseWorkflowFilterRepository =
                        new CaseWorkflowFilterRepository(dbContext, tenantRegistryId);

                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowFilter
                        = await caseWorkflowFilterRepository.GetByCasesWorkflowIdOrderByIdAsync(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id, token);
                }

                if (options.Lists)
                {
                    var entityAnalysisModelListRepository =
                        new EntityAnalysisModelListRepository(dbContext, tenantRegistryId);

                    entityAnalysisModel.EntityAnalysisModelList
                        = await entityAnalysisModelListRepository
                            .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token)
                            .ConfigureAwait(false);

                    foreach (var entityAnalysisModelListsEntityAnalysisModel in entityAnalysisModel
                                 .EntityAnalysisModelList)
                    {
                        var entityAnalysisModelListValueRepository =
                            new EntityAnalysisModelListValueRepository(dbContext, tenantRegistryId);

                        entityAnalysisModelListsEntityAnalysisModel.EntityAnalysisModelListValue
                            = await entityAnalysisModelListValueRepository
                                .GetByEntityAnalysisModelListIdOrderByIdAsync(
                                    entityAnalysisModelListsEntityAnalysisModel.Id, token);
                    }
                }

                if (options.Dictionaries)
                {
                    var entityAnalysisModelDictionaryRepository =
                        new EntityAnalysisModelDictionaryRepository(dbContext, tenantRegistryId);

                    entityAnalysisModel.EntityAnalysisModelDictionary
                        = await entityAnalysisModelDictionaryRepository
                            .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token)
                            .ConfigureAwait(false);

                    foreach (var entityAnalysisModelDictionaryEntityAnalysisModel in entityAnalysisModel
                                 .EntityAnalysisModelDictionary)
                    {
                        var entityAnalysisModelDictionaryKvpRepository =
                            new EntityAnalysisModelDictionaryKvpRepository(dbContext, tenantRegistryId);

                        entityAnalysisModelDictionaryEntityAnalysisModel
                                .EntityAnalysisModelDictionaryKvp
                            = await entityAnalysisModelDictionaryKvpRepository
                                .GetByEntityAnalysisModelDictionaryIdOrderByIdAsync(
                                    entityAnalysisModelDictionaryEntityAnalysisModel
                                        .Id, token).ConfigureAwait(false);
                    }
                }

                if (options.Visualisations)
                {
                    var visualisationRegistryRepository =
                        new VisualisationRegistryRepository(dbContext, tenantRegistryId);

                    payload.VisualisationRegistry = await visualisationRegistryRepository.GetOrderByIdAsync(token);

                    foreach (var visualisationRegistry in payload.VisualisationRegistry)
                    {
                        var visualisationRegistryDatasourceRepository =
                            new VisualisationRegistryDatasourceRepository(dbContext, tenantRegistryId);

                        visualisationRegistry.VisualisationRegistryDatasource
                            = await visualisationRegistryDatasourceRepository
                                .GetByVisualisationRegistryIdOrderByIdAsync(visualisationRegistry.Id, token);

                        var visualisationRegistryParameterRepository =
                            new VisualisationRegistryParameterRepository(dbContext, tenantRegistryId);

                        visualisationRegistry.VisualisationRegistryParameter
                            = await visualisationRegistryParameterRepository
                                .GetByVisualisationRegistryIdOrderByIdAsync(visualisationRegistry.Id, token);
                    }
                }
            }

            var entityAnalysisModelRole = new EntityAnalysisModelRoleRepository(dbContext, tenantRegistryId);
            payload.EntityPermission.EntityAnalysisModelRole = await entityAnalysisModelRole.GetAllDescAsync(token);

            var caseWorkflowRoleRepository = new CaseWorkflowRoleRepository(dbContext, tenantRegistryId);
            payload.EntityPermission.CaseWorkflowRole = await caseWorkflowRoleRepository.GetAllDescAsync(token);

            var caseWorkflowActionRoleRepository = new CaseWorkflowActionRoleRepository(dbContext, tenantRegistryId);

            payload.EntityPermission.CaseWorkflowActionRole =
                await caseWorkflowActionRoleRepository.GetAllDescAsync(token);

            var caseWorkflowDisplayRoleRepository = new CaseWorkflowDisplayRoleRepository(dbContext, tenantRegistryId);

            payload.EntityPermission.CaseWorkflowDisplayRole =
                await caseWorkflowDisplayRoleRepository.GetAllDescAsync(token);

            var caseWorkflowFilterRoleRepository = new CaseWorkflowFilterRoleRepository(dbContext, tenantRegistryId);

            payload.EntityPermission.CaseWorkflowFilterRole =
                await caseWorkflowFilterRoleRepository.GetAllDescAsync(token);

            var caseWorkflowFormRoleRepository = new CaseWorkflowFormRoleRepository(dbContext, tenantRegistryId);
            payload.EntityPermission.CaseWorkflowFormRole = await caseWorkflowFormRoleRepository.GetAllDescAsync(token);

            var caseWorkflowFormValidatorRepository =
                new CaseWorkflowFormValidatorRepository(dbContext, tenantRegistryId);

            payload.EntityPermission.CaseWorkflowFormValidator =
                await caseWorkflowFormValidatorRepository.GetAllDescAsync(token);

            var caseWorkflowReleaseHookRepository =
                new CaseWorkflowReleaseHookRepository(dbContext, tenantRegistryId);

            payload.EntityPermission.CaseWorkflowReleaseHook =
                await caseWorkflowReleaseHookRepository.GetAllDescAsync(token);

            var caseWorkflowMacroRoleRepository = new CaseWorkflowMacroRoleRepository(dbContext, tenantRegistryId);

            payload.EntityPermission.CaseWorkflowMacroRole =
                await caseWorkflowMacroRoleRepository.GetAllDescAsync(token);

            var caseWorkflowStatusRoleRepository = new CaseWorkflowStatusRoleRepository(dbContext, tenantRegistryId);

            payload.EntityPermission.CaseWorkflowStatusRole =
                await caseWorkflowStatusRoleRepository.GetAllDescAsync(token);

            var caseWorkflowXPathRoleRepository = new CaseWorkflowXPathRoleRepository(dbContext, tenantRegistryId);

            payload.EntityPermission.CaseWorkflowXPathRole =
                await caseWorkflowXPathRoleRepository.GetAllDescAsync(token);

            if (options.Visualisations)
            {
                var visualisationRegistryRole = new VisualisationRegistryRoleRepository(dbContext, tenantRegistryId);

                payload.EntityPermission.VisualisationRegistryRole =
                    await visualisationRegistryRole.GetAllDescAsync(token);

                var visualisationRegistryParameterRole =
                    new VisualisationRegistryParameterRoleRepository(dbContext, tenantRegistryId);

                payload.EntityPermission.VisualisationRegistryParameterRole =
                    await visualisationRegistryParameterRole.GetAllDescAsync(token);

                var visualisationRegistryDatasourceRole =
                    new VisualisationRegistryDatasourceRoleRepository(dbContext, tenantRegistryId);

                payload.EntityPermission.VisualisationRegistryDatasourceRole =
                    await visualisationRegistryDatasourceRole.GetAllDescAsync(token);
            }

            payload.EntityApproval = await ExportEntityApprovalsAsync(payload, tenantRegistryId, token);

            return payload;
        }
    }
}
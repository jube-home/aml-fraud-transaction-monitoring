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

using Jube.Data.Poco;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;

namespace Jube.Data.Context
{
    public class DbContext(LinqToDbConnectionOptions<DbContext> options) : DataConnection(options)
    {
        public ITable<ActivationWatcher> ActivationWatcher => GetTable<ActivationWatcher>();

        public ITable<EntityAnalysisModelTag> EntityAnalysisModelTag => GetTable<EntityAnalysisModelTag>();

        public ITable<EntityAnalysisModelStagePerformanceCounter> EntityAnalysisModelStagePerformanceCounter =>
            GetTable<EntityAnalysisModelStagePerformanceCounter>();

        public ITable<ArchiverStagePerformanceCounter> ArchiverStagePerformanceCounter =>
            GetTable<ArchiverStagePerformanceCounter>();

        public ITable<CaseCreationStagePerformanceCounter> CaseCreationStagePerformanceCounter =>
            GetTable<CaseCreationStagePerformanceCounter>();

        public ITable<CaseCreationWarning> CaseCreationWarning => GetTable<CaseCreationWarning>();

        public ITable<ArchiverWarning> ArchiverWarning => GetTable<ArchiverWarning>();

        public ITable<CaptureQueueHealth> CaptureQueueHealth => GetTable<CaptureQueueHealth>();

        public ITable<HAProxyServerStatus> HaProxyServerStatus => GetTable<HAProxyServerStatus>();

        public ITable<HAProxyReachabilityProbe> HaProxyReachabilityProbe => GetTable<HAProxyReachabilityProbe>();

        public ITable<OverlayNetworkTaskDrift> OverlayNetworkTaskDrift => GetTable<OverlayNetworkTaskDrift>();

        public ITable<EntityAnalysisModelTaskPerformanceCounter> EntityAnalysisModelTaskPerformanceCounter =>
            GetTable<EntityAnalysisModelTaskPerformanceCounter>();

        public ITable<ApplicationLogEntry> ApplicationLogEntry => GetTable<ApplicationLogEntry>();

        public ITable<DotNetRuntimeMetric> DotNetRuntimeMetric => GetTable<DotNetRuntimeMetric>();

        public ITable<PostgresMetric> PostgresMetric => GetTable<PostgresMetric>();

        public ITable<PostgresLogEntry> PostgresLogEntry => GetTable<PostgresLogEntry>();

        public ITable<ContainerLogEntry> ContainerLogEntry => GetTable<ContainerLogEntry>();

        public ITable<DockerEvent> DockerEvent => GetTable<DockerEvent>();

        public ITable<OpenTelemetryLogCounter> OpenTelemetryLogCounter => GetTable<OpenTelemetryLogCounter>();

        public ITable<OpenTelemetryExclude> OpenTelemetryExclude => GetTable<OpenTelemetryExclude>();

        public ITable<OtlpDispatchCounter> OtlpDispatchCounter => GetTable<OtlpDispatchCounter>();

        public ITable<RedisMetric> RedisMetric => GetTable<RedisMetric>();

        public ITable<RedisSlowOperation> RedisSlowOperation => GetTable<RedisSlowOperation>();

        public ITable<RedisConnectionMultiplexerMetric> RedisConnectionMultiplexerMetric =>
            GetTable<RedisConnectionMultiplexerMetric>();

        public ITable<PostgresReplicationStatus> PostgresReplicationStatus => GetTable<PostgresReplicationStatus>();

        public ITable<RedisSentinelStatus> RedisSentinelStatus => GetTable<RedisSentinelStatus>();

        public ITable<EtcdMemberStatus> EtcdMemberStatus => GetTable<EtcdMemberStatus>();

        public ITable<PatroniMemberStatus> PatroniMemberStatus => GetTable<PatroniMemberStatus>();

        public ITable<DockerContainerMetric> DockerContainerMetric => GetTable<DockerContainerMetric>();

        public ITable<DockerHostMetric> DockerHostMetric => GetTable<DockerHostMetric>();
        public ITable<OpenTelemetryMetric> OpenTelemetryMetric => GetTable<OpenTelemetryMetric>();

        public ITable<EtcdClusterEvent> EtcdClusterEvent => GetTable<EtcdClusterEvent>();

        public ITable<PatroniClusterEvent> PatroniClusterEvent => GetTable<PatroniClusterEvent>();

        public ITable<RedisSentinelEvent> RedisSentinelEvent => GetTable<RedisSentinelEvent>();

        public ITable<ModelInvokeWarning> ModelInvokeWarning => GetTable<ModelInvokeWarning>();

        public ITable<RedisConnectionEvent> RedisConnectionEvent => GetTable<RedisConnectionEvent>();

        public ITable<RedisCallCounter> RedisCallCounter => GetTable<RedisCallCounter>();

        public ITable<EntityAnalysisModelResponseTimePipelineCounter> EntityAnalysisModelResponseTimePipelineCounter =>
            GetTable<EntityAnalysisModelResponseTimePipelineCounter>();

        public ITable<CaseWorkflowFormEntryValue> CaseWorkflowFormEntryValue => GetTable<CaseWorkflowFormEntryValue>();

        public ITable<CaseWorkflowFormEntry> CaseWorkflowFormEntry => GetTable<CaseWorkflowFormEntry>();

        public ITable<CaseFile> CaseFile => GetTable<CaseFile>();

        public ITable<UserLogin> UserLogin => GetTable<UserLogin>();

        public ITable<CaseNote> CaseNote => GetTable<CaseNote>();

        public ITable<SessionCaseJournal> SessionCaseJournal => GetTable<SessionCaseJournal>();

        public ITable<SessionCaseSearchCompiledSql> SessionCaseSearchCompiledSql =>
            GetTable<SessionCaseSearchCompiledSql>();

        public ITable<ArchiveEntityAnalysisModelAbstractionEntry> ArchiveEntityAnalysisModelAbstractionEntry =>
            GetTable<ArchiveEntityAnalysisModelAbstractionEntry>();

        public ITable<EntityAnalysisModelSearchKeyDistinctValueCalculationInstance>
            EntityAnalysisModelSearchKeyDistinctValueCalculationInstance =>
            GetTable<EntityAnalysisModelSearchKeyDistinctValueCalculationInstance>();

        public ITable<EntityAnalysisModelSearchKeyCalculationInstance>
            EntityAnalysisModelSearchKeyCalculationInstance =>
            GetTable<EntityAnalysisModelSearchKeyCalculationInstance>();

        public ITable<EntityAnalysisInstance> EntityAnalysisInstance => GetTable<EntityAnalysisInstance>();

        public ITable<EntityAnalysisModelInstance> EntityAnalysisModelInstance =>
            GetTable<EntityAnalysisModelInstance>();

        public ITable<EntityAnalysisModelSynchronisationNodeStatusEntry>
            EntityAnalysisModelSynchronisationNodeStatusEntry =>
            GetTable<EntityAnalysisModelSynchronisationNodeStatusEntry>();

        public ITable<EntityAnalysisModelSynchronisationSchedule> EntityAnalysisModelSynchronisationSchedule =>
            GetTable<EntityAnalysisModelSynchronisationSchedule>();

        public ITable<ArchiveKey> ArchiveKey => GetTable<ArchiveKey>();

        public ITable<ArchiveKeyVersion> ArchiveKeyVersion => GetTable<ArchiveKeyVersion>();

        public ITable<ExhaustiveSearchInstance> ExhaustiveSearchInstance => GetTable<ExhaustiveSearchInstance>();

        public ITable<UserRegistry> UserRegistry => GetTable<UserRegistry>();

        public ITable<Currency> Currency => GetTable<Currency>();

        public ITable<ExhaustiveSearchInstanceVariable> ExhaustiveSearchInstanceVariable =>
            GetTable<ExhaustiveSearchInstanceVariable>();

        public ITable<ExhaustiveSearchInstanceVariableClassification> ExhaustiveSearchInstanceVariableClassification =>
            GetTable<ExhaustiveSearchInstanceVariableClassification>();

        public ITable<ExhaustiveSearchInstanceTrialInstanceVariable> ExhaustiveSearchInstanceTrialInstanceVariable =>
            GetTable<ExhaustiveSearchInstanceTrialInstanceVariable>();

        public ITable<ExhaustiveSearchInstanceTrialInstance> ExhaustiveSearchInstanceTrialInstance =>
            GetTable<ExhaustiveSearchInstanceTrialInstance>();

        public ITable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>
            ExhaustiveSearchInstanceTrialInstanceVariablePrescription =>
            GetTable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>();

        public ITable<ExhaustiveSearchInstanceTrialInstanceVariablePrescriptionHistogram>
            ExhaustiveSearchInstanceTrialInstanceVariablePrescriptionHistogram =>
            GetTable<ExhaustiveSearchInstanceTrialInstanceVariablePrescriptionHistogram>();

        public ITable<ExhaustiveSearchInstancePromotedTrialInstanceSensitivity>
            ExhaustiveSearchInstancePromotedTrialInstanceSensitivity =>
            GetTable<ExhaustiveSearchInstancePromotedTrialInstanceSensitivity>();

        public ITable<ExhaustiveSearchInstancePromotedTrialInstanceRoc>
            ExhaustiveSearchInstancePromotedTrialInstanceRoc =>
            GetTable<ExhaustiveSearchInstancePromotedTrialInstanceRoc>();

        public ITable<ExhaustiveSearchInstanceVariableHistogram> ExhaustiveSearchInstanceVariableHistogram =>
            GetTable<ExhaustiveSearchInstanceVariableHistogram>();

        public ITable<ExhaustiveSearchInstancePromotedTrialInstance> ExhaustiveSearchInstancePromotedTrialInstance =>
            GetTable<ExhaustiveSearchInstancePromotedTrialInstance>();

        public ITable<ExhaustiveSearchInstancePromotedTrialInstancePredictedActual>
            ExhaustiveSearchInstancePromotedTrialInstancePredictedActual =>
            GetTable<ExhaustiveSearchInstancePromotedTrialInstancePredictedActual>();

        public ITable<ExhaustiveSearchInstanceTrialInstanceTopologyTrial>
            ExhaustiveSearchInstanceTrialInstanceTopologyTrial =>
            GetTable<ExhaustiveSearchInstanceTrialInstanceTopologyTrial>();

        public ITable<ExhaustiveSearchInstanceTrialInstanceSensitivity>
            ExhaustiveSearchInstanceTrialInstanceSensitivity =>
            GetTable<ExhaustiveSearchInstanceTrialInstanceSensitivity>();

        public ITable<ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial>
            ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial =>
            GetTable<ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial>();

        public ITable<ExhaustiveSearchInstanceVariableMultiCollinearity>
            ExhaustiveSearchInstanceVariableMultiCollinearity =>
            GetTable<ExhaustiveSearchInstanceVariableMultiCollinearity>();

        public ITable<HttpProcessingCounter> HttpProcessingCounter => GetTable<HttpProcessingCounter>();

        public ITable<Archive> Archive => GetTable<Archive>();

        public ITable<ArchiveTag> ArchiveTag => GetTable<ArchiveTag>();

        public ITable<HttpResponseHeader> HttpHeader => GetTable<HttpResponseHeader>();

        public ITable<ArchiveTagVersion> ArchiveTagVersion => GetTable<ArchiveTagVersion>();

        public ITable<ArchiveVersion> ArchiveVersion => GetTable<ArchiveVersion>();

        public ITable<MockArchive> MockArchive => GetTable<MockArchive>();

        public ITable<EntityAnalysisModelProcessingCounter> EntityAnalysisModelProcessingCounter =>
            GetTable<EntityAnalysisModelProcessingCounter>();

        public ITable<SanctionEntry> SanctionEntry => GetTable<SanctionEntry>();

        public ITable<SanctionEntrySource> SanctionEntrySource => GetTable<SanctionEntrySource>();

        public ITable<SanctionEntryImport> SanctionEntryImport => GetTable<SanctionEntryImport>();

        public ITable<SanctionEntryRejection> SanctionEntryRejection => GetTable<SanctionEntryRejection>();

        public ITable<EntityAnalysisModelSynchronisationError> EntityAnalysisModelSynchronisationError =>
            GetTable<EntityAnalysisModelSynchronisationError>();

        public ITable<EntityAnalysisModelActivationRuleCounterHistory>
            EntityAnalysisModelActivationRuleCounterHistory =>
            GetTable<EntityAnalysisModelActivationRuleCounterHistory>();

        public ITable<EntityAnalysisModelGatewayRuleCounterHistory> EntityAnalysisModelGatewayRuleCounterHistory =>
            GetTable<EntityAnalysisModelGatewayRuleCounterHistory>();

        public ITable<CaseWorkflow> CaseWorkflow => GetTable<CaseWorkflow>();

        public ITable<CaseEvent> CaseEvent => GetTable<CaseEvent>();

        public ITable<Case> Case => GetTable<Case>();

        public ITable<CaseWorkflowStatus> CaseWorkflowStatus => GetTable<CaseWorkflowStatus>();

        public ITable<CaseWorkflowXPath> CaseWorkflowXPath => GetTable<CaseWorkflowXPath>();

        public ITable<CaseWorkflowForm> CaseWorkflowForm => GetTable<CaseWorkflowForm>();

        public ITable<CaseWorkflowAction> CaseWorkflowAction => GetTable<CaseWorkflowAction>();

        public ITable<CaseWorkflowDisplay> CaseWorkflowDisplay => GetTable<CaseWorkflowDisplay>();

        public ITable<CaseWorkflowFilter> CaseWorkflowFilter => GetTable<CaseWorkflowFilter>();

        public ITable<CaseWorkflowMacro> CaseWorkflowMacro => GetTable<CaseWorkflowMacro>();

        public ITable<PermissionSpecification> PermissionSpecification => GetTable<PermissionSpecification>();

        public ITable<RoleRegistry> RoleRegistry => GetTable<RoleRegistry>();

        public ITable<RoleRegistryPermission> RoleRegistryPermission => GetTable<RoleRegistryPermission>();

        public ITable<UserInTenant> UserInTenant => GetTable<UserInTenant>();

        public ITable<UserInTenantSwitchLog> UserInTenantSwitchLog => GetTable<UserInTenantSwitchLog>();

        public ITable<EntityAnalysisModel> EntityAnalysisModel => GetTable<EntityAnalysisModel>();

        public ITable<EntityAnalysisModelGatewayRule> EntityAnalysisModelGatewayRule =>
            GetTable<EntityAnalysisModelGatewayRule>();

        public ITable<EntityAnalysisModelActivationRule> EntityAnalysisModelActivationRule =>
            GetTable<EntityAnalysisModelActivationRule>();

        public ITable<EntityAnalysisModelSanction> EntityAnalysisModelSanction =>
            GetTable<EntityAnalysisModelSanction>();

        public ITable<EntityAnalysisInlineScript> EntityAnalysisInlineScript => GetTable<EntityAnalysisInlineScript>();

        public ITable<EntityAnalysisModelListCsvFileUpload> EntityAnalysisModelListCsvFileUpload =>
            GetTable<EntityAnalysisModelListCsvFileUpload>();

        public ITable<EntityAnalysisModelDictionaryCsvFileUpload> EntityAnalysisModelDictionaryCsvFileUpload =>
            GetTable<EntityAnalysisModelDictionaryCsvFileUpload>();

        public ITable<EntityAnalysisModelInlineFunction> EntityAnalysisModelInlineFunction =>
            GetTable<EntityAnalysisModelInlineFunction>();

        public ITable<EntityAnalysisModelRequestXpath> EntityAnalysisModelRequestXpath =>
            GetTable<EntityAnalysisModelRequestXpath>();

        public ITable<EntityAnalysisModelTtlCounter> EntityAnalysisModelTtlCounter =>
            GetTable<EntityAnalysisModelTtlCounter>();

        public ITable<EntityAnalysisModelAbstractionCalculation> EntityAnalysisModelAbstractionCalculation =>
            GetTable<EntityAnalysisModelAbstractionCalculation>();

        public ITable<EntityAnalysisModelHttpAdaptation> EntityAnalysisModelHttpAdaptation =>
            GetTable<EntityAnalysisModelHttpAdaptation>();

        public ITable<TenantRegistry> TenantRegistry => GetTable<TenantRegistry>();

        public ITable<VisualisationRegistry> VisualisationRegistry => GetTable<VisualisationRegistry>();

        public ITable<VisualisationRegistryParameter> VisualisationRegistryParameter =>
            GetTable<VisualisationRegistryParameter>();

        public ITable<VisualisationRegistryDatasource> VisualisationRegistryDatasource =>
            GetTable<VisualisationRegistryDatasource>();

        public ITable<EntityAnalysisModelDictionary> EntityAnalysisModelDictionary =>
            GetTable<EntityAnalysisModelDictionary>();

        public ITable<EntityAnalysisModelReprocessingRule> EntityAnalysisModelReprocessingRule =>
            GetTable<EntityAnalysisModelReprocessingRule>();

        public ITable<EntityAnalysisModelReprocessingRuleInstance> EntityAnalysisModelReprocessingRuleInstance =>
            GetTable<EntityAnalysisModelReprocessingRuleInstance>();

        public ITable<EntityAnalysisModelList> EntityAnalysisModelList => GetTable<EntityAnalysisModelList>();

        public ITable<EntityAnalysisAsynchronousQueueBalance> EntityAnalysisAsynchronousQueueBalance =>
            GetTable<EntityAnalysisAsynchronousQueueBalance>();

        public ITable<EntityAnalysisModelListValue> EntityAnalysisModelListValue =>
            GetTable<EntityAnalysisModelListValue>();

        public ITable<EntityAnalysisModelSuppression> EntityAnalysisModelSuppression =>
            GetTable<EntityAnalysisModelSuppression>();

        public ITable<EntityAnalysisModelSuppressionVersion> EntityAnalysisModelSuppressionVersion =>
            GetTable<EntityAnalysisModelSuppressionVersion>();

        public ITable<EntityAnalysisModelActivationRuleSuppression> EntityAnalysisModelActivationRuleSuppression =>
            GetTable<EntityAnalysisModelActivationRuleSuppression>();

        public ITable<EntityAnalysisModelActivationRuleSuppressionVersion>
            EntityAnalysisModelActivationRuleSuppressionVersion =>
            GetTable<EntityAnalysisModelActivationRuleSuppressionVersion>();

        public ITable<EntityAnalysisModelDictionaryKvp> EntityAnalysisModelDictionaryKvp =>
            GetTable<EntityAnalysisModelDictionaryKvp>();

        public ITable<RuleScriptToken> RuleScriptToken => GetTable<RuleScriptToken>();

        public ITable<DictionaryEvalExpression> DictionaryEvalExpression => GetTable<DictionaryEvalExpression>();

        public ITable<EntityAnalysisModelAbstractionRule> EntityAnalysisModelAbstractionRule =>
            GetTable<EntityAnalysisModelAbstractionRule>();

        public ITable<EntityAnalysisModelInlineScript> EntityAnalysisModelInlineScript =>
            GetTable<EntityAnalysisModelInlineScript>();

        public ITable<VisualisationRegistryDatasourceSeries> VisualisationRegistryDatasourceSeries =>
            GetTable<VisualisationRegistryDatasourceSeries>();

        public ITable<EntityAnalysisModelAsynchronousQueueBalance> EntityAnalysisModelAsynchronousQueueBalance =>
            GetTable<EntityAnalysisModelAsynchronousQueueBalance>();

        public ITable<EntityAnalysisModelRequestXpathVersion> EntityAnalysisModelRequestXpathVersion =>
            GetTable<EntityAnalysisModelRequestXpathVersion>();

        public ITable<EntityAnalysisModelInlineFunctionVersion> EntityAnalysisModelInlineFunctionVersion =>
            GetTable<EntityAnalysisModelInlineFunctionVersion>();

        public ITable<EntityAnalysisModelInlineScriptVersion> EntityAnalysisModelInlineScriptVersion =>
            GetTable<EntityAnalysisModelInlineScriptVersion>();

        public ITable<EntityAnalysisModelGatewayRuleVersion> EntityAnalysisModelGatewayRuleVersion =>
            GetTable<EntityAnalysisModelGatewayRuleVersion>();

        public ITable<EntityAnalysisModelTagVersion> EntityAnalysisModelTagVersion =>
            GetTable<EntityAnalysisModelTagVersion>();

        public ITable<EntityAnalysisModelSanctionVersion> EntityAnalysisModelSanctionVersion =>
            GetTable<EntityAnalysisModelSanctionVersion>();

        public ITable<EntityAnalysisModelAbstractionRuleVersion> EntityAnalysisModelAbstractionRuleVersion =>
            GetTable<EntityAnalysisModelAbstractionRuleVersion>();

        public ITable<EntityAnalysisModelAbstractionCalculationVersion>
            EntityAnalysisModelAbstractionCalculationVersion =>
            GetTable<EntityAnalysisModelAbstractionCalculationVersion>();

        public ITable<EntityAnalysisModelHttpAdaptationVersion> EntityAnalysisModelHttpAdaptationVersion =>
            GetTable<EntityAnalysisModelHttpAdaptationVersion>();

        public ITable<EntityAnalysisModelActivationRuleVersion> EntityAnalysisModelActivationRuleVersion =>
            GetTable<EntityAnalysisModelActivationRuleVersion>();

        public ITable<EntityAnalysisModelListVersion> EntityAnalysisModelListVersion =>
            GetTable<EntityAnalysisModelListVersion>();

        public ITable<EntityAnalysisModelDictionaryVersion> EntityAnalysisModelDictionaryVersion =>
            GetTable<EntityAnalysisModelDictionaryVersion>();

        public ITable<CaseWorkflowXPathVersion> CaseWorkflowXPathVersion => GetTable<CaseWorkflowXPathVersion>();

        public ITable<CaseWorkflowFormVersion> CaseWorkflowFormVersion => GetTable<CaseWorkflowFormVersion>();

        public ITable<CaseWorkflowActionVersion> CaseWorkflowActionVersion => GetTable<CaseWorkflowActionVersion>();

        public ITable<CaseWorkflowDisplayVersion> CaseWorkflowDisplayVersion => GetTable<CaseWorkflowDisplayVersion>();

        public ITable<CaseWorkflowMacroVersion> CaseWorkflowMacroVersion => GetTable<CaseWorkflowMacroVersion>();

        public ITable<CaseWorkflowFilterVersion> CaseWorkflowFilterVersion => GetTable<CaseWorkflowFilterVersion>();

        public ITable<EntityAnalysisModelListValueVersion> EntityAnalysisModelListValueVersion =>
            GetTable<EntityAnalysisModelListValueVersion>();

        public ITable<EntityAnalysisModelDictionaryKvpVersion> EntityAnalysisModelDictionaryKvpVersion =>
            GetTable<EntityAnalysisModelDictionaryKvpVersion>();

        public ITable<VisualisationRegistryDatasourceVersion> VisualisationRegistryDatasourceVersion =>
            GetTable<VisualisationRegistryDatasourceVersion>();

        public ITable<VisualisationRegistryParameterVersion> VisualisationRegistryParameterVersion =>
            GetTable<VisualisationRegistryParameterVersion>();

        public ITable<ExhaustiveSearchInstanceVariableAnomaly> ExhaustiveSearchInstanceVariableAnomaly =>
            GetTable<ExhaustiveSearchInstanceVariableAnomaly>();

        public ITable<ExhaustiveSearchInstanceVariableHistogramClassification>
            ExhaustiveSearchInstanceVariableHistogramClassification =>
            GetTable<ExhaustiveSearchInstanceVariableHistogramClassification>();

        public ITable<ExhaustiveSearchInstanceVariableHistogramAnomaly>
            ExhaustiveSearchInstanceVariableHistogramAnomaly =>
            GetTable<ExhaustiveSearchInstanceVariableHistogramAnomaly>();

        public ITable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>
            ExhaustiveSearchInstancePromotedTrialInstanceVariable =>
            GetTable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>();

        public ITable<ExhaustiveSearchInstanceData> ExhaustiveSearchInstanceData =>
            GetTable<ExhaustiveSearchInstanceData>();

        public ITable<Import> Import => GetTable<Import>();

        public ITable<Export> Export => GetTable<Export>();

        public ITable<ExportPeek> ExportPeek => GetTable<ExportPeek>();

        public ITable<LocalCacheInstance> LocalCacheInstance => GetTable<LocalCacheInstance>();

        public ITable<LocalCacheInstanceKey> LocalCacheInstanceKey => GetTable<LocalCacheInstanceKey>();

        public ITable<LocalCacheInstanceLru> LocalCacheInstanceLru => GetTable<LocalCacheInstanceLru>();

        public ITable<HashCacheAssemblyInstance> HashCacheAssemblyInstance => GetTable<HashCacheAssemblyInstance>();

        public ITable<HashCacheAssemblyInstanceEntry> HashCacheAssemblyInstanceEntry =>
            GetTable<HashCacheAssemblyInstanceEntry>();

        public ITable<HashCacheAssemblyInstanceJournal> HashCacheAssemblyInstanceJournal =>
            GetTable<HashCacheAssemblyInstanceJournal>();

        public ITable<CachePayloadRemovalBatch> CachePayloadRemovalBatch => GetTable<CachePayloadRemovalBatch>();

        public ITable<CachePayloadRemovalBatchEntry> CachePayloadRemovalBatchEntry =>
            GetTable<CachePayloadRemovalBatchEntry>();

        public ITable<CachePayloadLatestRemovalBatchEntry> CachePayloadLatestRemovalBatchEntry =>
            GetTable<CachePayloadLatestRemovalBatchEntry>();

        public ITable<CachePayloadRemovalBatchResponseTime> CachePayloadRemovalBatchResponseTime =>
            GetTable<CachePayloadRemovalBatchResponseTime>();

        public ITable<CachePayloadLatestRemovalBatch> CachePayloadLatestRemovalBatch =>
            GetTable<CachePayloadLatestRemovalBatch>();

        public ITable<CachePayloadLatestRemovalBatchResponseTime> CachePayloadLatestRemovalBatchResponseTime =>
            GetTable<CachePayloadLatestRemovalBatchResponseTime>();

        public ITable<CacheTtlCounterEntryRemovalBatch> CacheTtlCounterEntryRemovalBatch =>
            GetTable<CacheTtlCounterEntryRemovalBatch>();

        public ITable<CacheTtlCounterEntryRemovalBatchEntry> CacheTtlCounterEntryRemovalBatchEntry =>
            GetTable<CacheTtlCounterEntryRemovalBatchEntry>();

        public ITable<CaseWorkflowStatusRole> CaseWorkflowStatusRole => GetTable<CaseWorkflowStatusRole>();

        public ITable<CaseWorkflowRole> CaseWorkflowRole => GetTable<CaseWorkflowRole>();

        public ITable<CaseWorkflowMacroRole> CaseWorkflowMacroRole => GetTable<CaseWorkflowMacroRole>();

        public ITable<CaseWorkflowFormRole> CaseWorkflowFormRole => GetTable<CaseWorkflowFormRole>();

        public ITable<CaseWorkflowActionRole> CaseWorkflowActionRole => GetTable<CaseWorkflowActionRole>();

        public ITable<CaseWorkflowDisplayRole> CaseWorkflowDisplayRole => GetTable<CaseWorkflowDisplayRole>();

        public ITable<CaseWorkflowXPathRole> CaseWorkflowXPathRole => GetTable<CaseWorkflowXPathRole>();

        public ITable<EntityAnalysisModelRole> EntityAnalysisModelRole => GetTable<EntityAnalysisModelRole>();

        public ITable<VisualisationRegistryRole> VisualisationRegistryRole => GetTable<VisualisationRegistryRole>();

        public ITable<VisualisationRegistryDatasourceRole> VisualisationRegistryDatasourceRole =>
            GetTable<VisualisationRegistryDatasourceRole>();

        public ITable<VisualisationRegistryParameterRole> VisualisationRegistryParameterRole =>
            GetTable<VisualisationRegistryParameterRole>();

        public ITable<CaseWorkflowFilterRole> CaseWorkflowFilterRole => GetTable<CaseWorkflowFilterRole>();

        public ITable<UserRegistryApiKey> UserRegistryApiKey => GetTable<UserRegistryApiKey>();

        public ITable<SanctionStopToken> SanctionStopToken => GetTable<SanctionStopToken>();
    }
}
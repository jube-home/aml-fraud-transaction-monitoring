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
using LinqToDB.Data;

namespace Jube.Data.Context
{
    public class DbContext(DataOptions<DbContext> options) : DataConnection(options.Options)
    {
        public ITable<ActivationWatcher> ActivationWatcher => this.GetTable<ActivationWatcher>();
        public ITable<EntityAnalysisModelTag> EntityAnalysisModelTag => this.GetTable<EntityAnalysisModelTag>();

        public ITable<EntityAnalysisModelResponseElevationRange> EntityAnalysisModelResponseElevationRange =>
            this.GetTable<EntityAnalysisModelResponseElevationRange>();

        public ITable<EntityAnalysisModelStagePerformanceCounter> EntityAnalysisModelStagePerformanceCounter =>
            this.GetTable<EntityAnalysisModelStagePerformanceCounter>();

        public ITable<ArchiverStagePerformanceCounter> ArchiverStagePerformanceCounter =>
            this.GetTable<ArchiverStagePerformanceCounter>();

        public ITable<CaseCreationStagePerformanceCounter> CaseCreationStagePerformanceCounter =>
            this.GetTable<CaseCreationStagePerformanceCounter>();

        public ITable<CaseCreationWarning> CaseCreationWarning => this.GetTable<CaseCreationWarning>();
        public ITable<ArchiverWarning> ArchiverWarning => this.GetTable<ArchiverWarning>();
        public ITable<CaptureQueueHealth> CaptureQueueHealth => this.GetTable<CaptureQueueHealth>();
        public ITable<HaProxyServerStatus> HaProxyServerStatus => this.GetTable<HaProxyServerStatus>();
        public ITable<HaProxyReachabilityProbe> HaProxyReachabilityProbe => this.GetTable<HaProxyReachabilityProbe>();
        public ITable<OverlayNetworkTaskDrift> OverlayNetworkTaskDrift => this.GetTable<OverlayNetworkTaskDrift>();

        public ITable<EntityAnalysisModelTaskPerformanceCounter> EntityAnalysisModelTaskPerformanceCounter =>
            this.GetTable<EntityAnalysisModelTaskPerformanceCounter>();

        public ITable<ApplicationLogEntry> ApplicationLogEntry => this.GetTable<ApplicationLogEntry>();
        public ITable<WafSignature> WafSignature => this.GetTable<WafSignature>();
        public ITable<WafException> WafException => this.GetTable<WafException>();
        public ITable<WafAttack> WafAttack => this.GetTable<WafAttack>();
        public ITable<DotNetRuntimeMetric> DotNetRuntimeMetric => this.GetTable<DotNetRuntimeMetric>();
        public ITable<PostgresMetric> PostgresMetric => this.GetTable<PostgresMetric>();
        public ITable<PostgresLogEntry> PostgresLogEntry => this.GetTable<PostgresLogEntry>();
        public ITable<ContainerLogEntry> ContainerLogEntry => this.GetTable<ContainerLogEntry>();
        public ITable<DockerEvent> DockerEvent => this.GetTable<DockerEvent>();
        public ITable<OpenTelemetryLogCounter> OpenTelemetryLogCounter => this.GetTable<OpenTelemetryLogCounter>();
        public ITable<AskJoobyConversation> AskJoobyConversation => this.GetTable<AskJoobyConversation>();
        public ITable<AskJoobyMessage> AskJoobyMessage => this.GetTable<AskJoobyMessage>();
        public ITable<AskJoobyRun> AskJoobyRun => this.GetTable<AskJoobyRun>();
        public ITable<AskJoobyRouting> AskJoobyRouting => this.GetTable<AskJoobyRouting>();

        public ITable<AskJoobyAgentGuardRejection> AskJoobyAgentGuardRejection =>
            this.GetTable<AskJoobyAgentGuardRejection>();

        public ITable<AskJoobyAgentGuardRule> AskJoobyAgentGuardRule => this.GetTable<AskJoobyAgentGuardRule>();

        public ITable<AskJoobyToolCall> AskJoobyToolCall => this.GetTable<AskJoobyToolCall>();
        public ITable<AskJoobyWorkflowStep> AskJoobyWorkflowStep => this.GetTable<AskJoobyWorkflowStep>();
        public ITable<AskJoobyErrata> AskJoobyErrata => this.GetTable<AskJoobyErrata>();
        public ITable<AskJoobyCompaction> AskJoobyCompaction => this.GetTable<AskJoobyCompaction>();
        public ITable<AskJoobyModelCall> AskJoobyModelCall => this.GetTable<AskJoobyModelCall>();
        public ITable<AskJoobyEmbeddingCall> AskJoobyEmbeddingCall => this.GetTable<AskJoobyEmbeddingCall>();
        public ITable<AskJoobyRerankCall> AskJoobyRerankCall => this.GetTable<AskJoobyRerankCall>();
        public ITable<AskJoobyWebSearchCall> AskJoobyWebSearchCall => this.GetTable<AskJoobyWebSearchCall>();

        public ITable<AskJoobyOpenCorporatesCall> AskJoobyOpenCorporatesCall =>
            this.GetTable<AskJoobyOpenCorporatesCall>();

        public ITable<AskJoobyEndpointFee> AskJoobyEndpointFee => this.GetTable<AskJoobyEndpointFee>();
        public ITable<AskJoobyRerankFee> AskJoobyRerankFee => this.GetTable<AskJoobyRerankFee>();
        public ITable<AskJoobyTurnCost> AskJoobyTurnCost => this.GetTable<AskJoobyTurnCost>();
        public ITable<AskJoobyRagChunk> AskJoobyRagChunk => this.GetTable<AskJoobyRagChunk>();
        public ITable<AskJoobyFile> AskJoobyFile => this.GetTable<AskJoobyFile>();
        public ITable<AskJoobyMemory> AskJoobyMemory => this.GetTable<AskJoobyMemory>();
        public ITable<AskJoobyDataCache> AskJoobyDataCache => this.GetTable<AskJoobyDataCache>();
        public ITable<DynamicSqlQueryLog> DynamicSqlQueryLog => this.GetTable<DynamicSqlQueryLog>();
        public ITable<OpenTelemetryExclude> OpenTelemetryExclude => this.GetTable<OpenTelemetryExclude>();
        public ITable<OtlpDispatchCounter> OtlpDispatchCounter => this.GetTable<OtlpDispatchCounter>();
        public ITable<RespMetric> RespMetric => this.GetTable<RespMetric>();
        public ITable<RespSlowOperation> RespSlowOperation => this.GetTable<RespSlowOperation>();

        public ITable<RespConnectionMultiplexerMetric> RespConnectionMultiplexerMetric =>
            this.GetTable<RespConnectionMultiplexerMetric>();

        public ITable<PostgresReplicationStatus> PostgresReplicationStatus =>
            this.GetTable<PostgresReplicationStatus>();

        public ITable<RespSentinelStatus> RespSentinelStatus => this.GetTable<RespSentinelStatus>();
        public ITable<EtcdMemberStatus> EtcdMemberStatus => this.GetTable<EtcdMemberStatus>();
        public ITable<PatroniMemberStatus> PatroniMemberStatus => this.GetTable<PatroniMemberStatus>();
        public ITable<DockerContainerMetric> DockerContainerMetric => this.GetTable<DockerContainerMetric>();
        public ITable<DockerHostMetric> DockerHostMetric => this.GetTable<DockerHostMetric>();
        public ITable<OpenTelemetryMetric> OpenTelemetryMetric => this.GetTable<OpenTelemetryMetric>();
        public ITable<EtcdClusterEvent> EtcdClusterEvent => this.GetTable<EtcdClusterEvent>();
        public ITable<PatroniClusterEvent> PatroniClusterEvent => this.GetTable<PatroniClusterEvent>();
        public ITable<RespSentinelEvent> RespSentinelEvent => this.GetTable<RespSentinelEvent>();
        public ITable<ModelInvokeWarning> ModelInvokeWarning => this.GetTable<ModelInvokeWarning>();
        public ITable<RespConnectionEvent> RespConnectionEvent => this.GetTable<RespConnectionEvent>();
        public ITable<RespCallCounter> RespCallCounter => this.GetTable<RespCallCounter>();

        public ITable<EntityAnalysisModelResponseTimePipelineCounter> EntityAnalysisModelResponseTimePipelineCounter =>
            this.GetTable<EntityAnalysisModelResponseTimePipelineCounter>();

        public ITable<CaseWorkflowFormEntryValue> CaseWorkflowFormEntryValue =>
            this.GetTable<CaseWorkflowFormEntryValue>();

        public ITable<CaseWorkflowFormEntry> CaseWorkflowFormEntry => this.GetTable<CaseWorkflowFormEntry>();
        public ITable<CaseFile> CaseFile => this.GetTable<CaseFile>();
        public ITable<UserLogin> UserLogin => this.GetTable<UserLogin>();
        public ITable<UserLogout> UserLogout => this.GetTable<UserLogout>();
        public ITable<CaseNote> CaseNote => this.GetTable<CaseNote>();
        public ITable<CaseResearch> CaseResearch => this.GetTable<CaseResearch>();
        public ITable<CaseResearchCitation> CaseResearchCitation => this.GetTable<CaseResearchCitation>();
        public ITable<CaseSuppression> CaseSuppression => this.GetTable<CaseSuppression>();
        public ITable<CaseSuppressionVersion> CaseSuppressionVersion => this.GetTable<CaseSuppressionVersion>();
        public ITable<CaseSuppressionTarget> CaseSuppressionTarget => this.GetTable<CaseSuppressionTarget>();

        public ITable<CaseSuppressionTransaction> CaseSuppressionTransaction =>
            this.GetTable<CaseSuppressionTransaction>();

        public ITable<CaseSuppressionApproval> CaseSuppressionApproval => this.GetTable<CaseSuppressionApproval>();
        public ITable<SessionCaseJournal> SessionCaseJournal => this.GetTable<SessionCaseJournal>();

        public ITable<SessionCaseSearchCompiledSql> SessionCaseSearchCompiledSql =>
            this.GetTable<SessionCaseSearchCompiledSql>();

        public ITable<CaseSearchFilter> CaseSearchFilter => this.GetTable<CaseSearchFilter>();

        public ITable<CaseRiskAssessment> CaseRiskAssessment => this.GetTable<CaseRiskAssessment>();

        public ITable<CaseView> CaseView => this.GetTable<CaseView>();

        public ITable<CaseKeyJournalPin> CaseKeyJournalPin => this.GetTable<CaseKeyJournalPin>();

        public ITable<CaseKeyJournalReplay> CaseKeyJournalReplay => this.GetTable<CaseKeyJournalReplay>();

        public ITable<CaseKeyJournalPeer> CaseKeyJournalPeer => this.GetTable<CaseKeyJournalPeer>();

        public ITable<GraphRegistry> GraphRegistry => this.GetTable<GraphRegistry>();

        public ITable<GraphRegistryVersion> GraphRegistryVersion => this.GetTable<GraphRegistryVersion>();

        public ITable<GraphRegistryRole> GraphRegistryRole => this.GetTable<GraphRegistryRole>();

        public ITable<GraphRegistryEdge> GraphRegistryEdge => this.GetTable<GraphRegistryEdge>();

        public ITable<GraphRegistryEdgeVersion> GraphRegistryEdgeVersion => this.GetTable<GraphRegistryEdgeVersion>();

        public ITable<GraphNode> GraphNode => this.GetTable<GraphNode>();

        public ITable<GraphEdge> GraphEdge => this.GetTable<GraphEdge>();

        public ITable<GraphEdgeEvent> GraphEdgeEvent => this.GetTable<GraphEdgeEvent>();

        public ITable<GraphBuildState> GraphBuildState => this.GetTable<GraphBuildState>();

        public ITable<GraphExploration> GraphExploration => this.GetTable<GraphExploration>();

        public ITable<GraphCommunity> GraphCommunity => this.GetTable<GraphCommunity>();

        public ITable<GraphPattern> GraphPattern => this.GetTable<GraphPattern>();

        public ITable<GraphPatternNode> GraphPatternNode => this.GetTable<GraphPatternNode>();

        public ITable<GraphNodeTag> GraphNodeTag => this.GetTable<GraphNodeTag>();

        public ITable<EntityAnalysisModelGraphRecall> EntityAnalysisModelGraphRecall =>
            this.GetTable<EntityAnalysisModelGraphRecall>();

        public ITable<EntityAnalysisModelGraphRecallVersion> EntityAnalysisModelGraphRecallVersion =>
            this.GetTable<EntityAnalysisModelGraphRecallVersion>();

        public ITable<EntityAnalysisModelSvirfd> EntityAnalysisModelSvirfd =>
            this.GetTable<EntityAnalysisModelSvirfd>();

        public ITable<EntityAnalysisModelSvirfdVersion> EntityAnalysisModelSvirfdVersion =>
            this.GetTable<EntityAnalysisModelSvirfdVersion>();

        public ITable<CaseTag> CaseTag => this.GetTable<CaseTag>();

        public ITable<CaseKeyJournalKeyIndex> CaseKeyJournalKeyIndex => this.GetTable<CaseKeyJournalKeyIndex>();

        public ITable<CaseKeyJournalTagJob> CaseKeyJournalTagJob => this.GetTable<CaseKeyJournalTagJob>();

        public ITable<CaseKeyJournalTagJobRow> CaseKeyJournalTagJobRow => this.GetTable<CaseKeyJournalTagJobRow>();

        public ITable<CaseStatusTransition> CaseStatusTransition => this.GetTable<CaseStatusTransition>();

        public ITable<UserNotification> UserNotification => this.GetTable<UserNotification>();

        public ITable<SanctionExemption> SanctionExemption => this.GetTable<SanctionExemption>();

        public ITable<CaseLink> CaseLink => this.GetTable<CaseLink>();

        public ITable<CaseSimilarityComponent> CaseSimilarityComponent => this.GetTable<CaseSimilarityComponent>();

        public ITable<CaseSimilarityComponentFrequency> CaseSimilarityComponentFrequency =>
            this.GetTable<CaseSimilarityComponentFrequency>();

        public ITable<CaseAgentRun> CaseAgentRun => this.GetTable<CaseAgentRun>();

        public ITable<CaseQaReview> CaseQaReview => this.GetTable<CaseQaReview>();
        public ITable<CaseAnalystAssessment> CaseAnalystAssessment => this.GetTable<CaseAnalystAssessment>();

        public ITable<GraphEntityMatch> GraphEntityMatch => this.GetTable<GraphEntityMatch>();

        public ITable<TransactionInboundQueue> TransactionInboundQueue => this.GetTable<TransactionInboundQueue>();

        public ITable<CaseReportProfile> CaseReportProfile => this.GetTable<CaseReportProfile>();

        public ITable<CaseReport> CaseReport => this.GetTable<CaseReport>();

        public ITable<CaseReportVersion> CaseReportVersion => this.GetTable<CaseReportVersion>();

        public ITable<CaseReportApproval> CaseReportApproval => this.GetTable<CaseReportApproval>();

        public ITable<CaseReportFeedback> CaseReportFeedback => this.GetTable<CaseReportFeedback>();

        public ITable<ErasureRequest> ErasureRequest => this.GetTable<ErasureRequest>();

        public ITable<EntityRiskRatingRequest> EntityRiskRatingRequest => this.GetTable<EntityRiskRatingRequest>();

        public ITable<ModelGovernanceRecord> ModelGovernanceRecord => this.GetTable<ModelGovernanceRecord>();

        public ITable<CaseAgentReplay> CaseAgentReplay => this.GetTable<CaseAgentReplay>();

        public ITable<CaseAgentReplayResult> CaseAgentReplayResult => this.GetTable<CaseAgentReplayResult>();

        public ITable<EntityRiskRating> EntityRiskRating => this.GetTable<EntityRiskRating>();

        public ITable<ModelDriftObservation> ModelDriftObservation => this.GetTable<ModelDriftObservation>();

        public ITable<SanctionRescreenRun> SanctionRescreenRun => this.GetTable<SanctionRescreenRun>();

        public ITable<SanctionRescreenHit> SanctionRescreenHit => this.GetTable<SanctionRescreenHit>();

        public ITable<CaseServiceLevelBreach> CaseServiceLevelBreach => this.GetTable<CaseServiceLevelBreach>();

        public ITable<ArchiveEntityAnalysisModelAbstractionEntry> ArchiveEntityAnalysisModelAbstractionEntry =>
            this.GetTable<ArchiveEntityAnalysisModelAbstractionEntry>();

        public ITable<EntityAnalysisModelSearchKeyDistinctValueCalculationInstance>
            EntityAnalysisModelSearchKeyDistinctValueCalculationInstance =>
            this.GetTable<EntityAnalysisModelSearchKeyDistinctValueCalculationInstance>();

        public ITable<EntityAnalysisModelSearchKeyCalculationInstance>
            EntityAnalysisModelSearchKeyCalculationInstance =>
            this.GetTable<EntityAnalysisModelSearchKeyCalculationInstance>();

        public ITable<EntityAnalysisInstance> EntityAnalysisInstance => this.GetTable<EntityAnalysisInstance>();

        public ITable<EntityAnalysisModelInstance> EntityAnalysisModelInstance =>
            this.GetTable<EntityAnalysisModelInstance>();

        public ITable<EntityAnalysisModelSynchronisationNodeStatusEntry>
            EntityAnalysisModelSynchronisationNodeStatusEntry =>
            this.GetTable<EntityAnalysisModelSynchronisationNodeStatusEntry>();

        public ITable<EntityAnalysisModelEngineSnapshot> EntityAnalysisModelEngineSnapshot =>
            this.GetTable<EntityAnalysisModelEngineSnapshot>();

        public ITable<EntityAnalysisModelBacktestInstance> EntityAnalysisModelBacktestInstance =>
            this.GetTable<EntityAnalysisModelBacktestInstance>();

        public ITable<EntityAnalysisModelSynchronisationSchedule> EntityAnalysisModelSynchronisationSchedule =>
            this.GetTable<EntityAnalysisModelSynchronisationSchedule>();

        public ITable<ArchiveKey> ArchiveKey => this.GetTable<ArchiveKey>();
        public ITable<ArchiveKeyVersion> ArchiveKeyVersion => this.GetTable<ArchiveKeyVersion>();
        public ITable<ExhaustiveSearchInstance> ExhaustiveSearchInstance => this.GetTable<ExhaustiveSearchInstance>();
        public ITable<ExhaustiveInstanceCsv> ExhaustiveInstanceCsv => this.GetTable<ExhaustiveInstanceCsv>();
        public ITable<ExhaustiveInstanceCsvRow> ExhaustiveInstanceCsvRow => this.GetTable<ExhaustiveInstanceCsvRow>();
        public ITable<ExhaustiveInstanceJson> ExhaustiveInstanceJson => this.GetTable<ExhaustiveInstanceJson>();

        public ITable<ExhaustiveInstanceJsonRow> ExhaustiveInstanceJsonRow =>
            this.GetTable<ExhaustiveInstanceJsonRow>();

        public ITable<UserRegistry> UserRegistry => this.GetTable<UserRegistry>();
        public ITable<Currency> Currency => this.GetTable<Currency>();

        public ITable<ExhaustiveSearchInstanceVariable> ExhaustiveSearchInstanceVariable =>
            this.GetTable<ExhaustiveSearchInstanceVariable>();

        public ITable<ExhaustiveSearchInstanceCategoricalField> ExhaustiveSearchInstanceCategoricalField =>
            this.GetTable<ExhaustiveSearchInstanceCategoricalField>();

        public ITable<ExhaustiveSearchInstanceVariableClassification> ExhaustiveSearchInstanceVariableClassification =>
            this.GetTable<ExhaustiveSearchInstanceVariableClassification>();

        public ITable<ExhaustiveSearchInstanceTrialInstanceVariable> ExhaustiveSearchInstanceTrialInstanceVariable =>
            this.GetTable<ExhaustiveSearchInstanceTrialInstanceVariable>();

        public ITable<ExhaustiveSearchInstanceTrialInstance> ExhaustiveSearchInstanceTrialInstance =>
            this.GetTable<ExhaustiveSearchInstanceTrialInstance>();

        public ITable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>
            ExhaustiveSearchInstanceTrialInstanceVariablePrescription =>
            this.GetTable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>();

        public ITable<ExhaustiveSearchInstancePrescriptionHistogram>
            ExhaustiveSearchInstancePrescriptionHistogram =>
            this.GetTable<ExhaustiveSearchInstancePrescriptionHistogram>();

        public ITable<ExhaustiveSearchInstancePromotedTrialInstanceSensitivity>
            ExhaustiveSearchInstancePromotedTrialInstanceSensitivity =>
            this.GetTable<ExhaustiveSearchInstancePromotedTrialInstanceSensitivity>();

        public ITable<ExhaustiveSearchInstancePromotedTrialInstanceRoc>
            ExhaustiveSearchInstancePromotedTrialInstanceRoc =>
            this.GetTable<ExhaustiveSearchInstancePromotedTrialInstanceRoc>();

        public ITable<ExhaustiveSearchInstanceVariableHistogram> ExhaustiveSearchInstanceVariableHistogram =>
            this.GetTable<ExhaustiveSearchInstanceVariableHistogram>();

        public ITable<ExhaustiveSearchInstancePromotedTrialInstance> ExhaustiveSearchInstancePromotedTrialInstance =>
            this.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>();

        public ITable<ExhaustiveSearchInstancePromotedTrialInstancePredictedActual>
            ExhaustiveSearchInstancePromotedTrialInstancePredictedActual =>
            this.GetTable<ExhaustiveSearchInstancePromotedTrialInstancePredictedActual>();

        public ITable<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue>
            ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue =>
            this.GetTable<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue>();

        public ITable<ExhaustiveSearchInstanceTrialInstanceTopologyTrial>
            ExhaustiveSearchInstanceTrialInstanceTopologyTrial =>
            this.GetTable<ExhaustiveSearchInstanceTrialInstanceTopologyTrial>();

        public ITable<ExhaustiveSearchInstanceTrialInstanceSensitivity>
            ExhaustiveSearchInstanceTrialInstanceSensitivity =>
            this.GetTable<ExhaustiveSearchInstanceTrialInstanceSensitivity>();

        public ITable<ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial>
            ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial =>
            this.GetTable<ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial>();

        public ITable<ExhaustiveSearchInstanceVariableMultiCollinearity>
            ExhaustiveSearchInstanceVariableMultiCollinearity =>
            this.GetTable<ExhaustiveSearchInstanceVariableMultiCollinearity>();

        public ITable<HttpProcessingCounter> HttpProcessingCounter => this.GetTable<HttpProcessingCounter>();
        public ITable<Archive> Archive => this.GetTable<Archive>();
        public ITable<ArchiveTag> ArchiveTag => this.GetTable<ArchiveTag>();
        public ITable<HttpResponseHeader> HttpHeader => this.GetTable<HttpResponseHeader>();
        public ITable<ArchiveTagVersion> ArchiveTagVersion => this.GetTable<ArchiveTagVersion>();
        public ITable<ArchiveVersion> ArchiveVersion => this.GetTable<ArchiveVersion>();
        public ITable<MockArchive> MockArchive => this.GetTable<MockArchive>();

        public ITable<EntityAnalysisModelProcessingCounter> EntityAnalysisModelProcessingCounter =>
            this.GetTable<EntityAnalysisModelProcessingCounter>();

        public ITable<SanctionEntry> SanctionEntry => this.GetTable<SanctionEntry>();
        public ITable<SanctionEntrySource> SanctionEntrySource => this.GetTable<SanctionEntrySource>();
        public ITable<SanctionEntryImport> SanctionEntryImport => this.GetTable<SanctionEntryImport>();
        public ITable<SanctionEntryRejection> SanctionEntryRejection => this.GetTable<SanctionEntryRejection>();

        public ITable<EntityAnalysisModelSynchronisationError> EntityAnalysisModelSynchronisationError =>
            this.GetTable<EntityAnalysisModelSynchronisationError>();

        public ITable<EntityAnalysisModelActivationRuleCounterHistory>
            EntityAnalysisModelActivationRuleCounterHistory =>
            this.GetTable<EntityAnalysisModelActivationRuleCounterHistory>();

        public ITable<EntityAnalysisModelGatewayRuleCounterHistory> EntityAnalysisModelGatewayRuleCounterHistory =>
            this.GetTable<EntityAnalysisModelGatewayRuleCounterHistory>();

        public ITable<CaseWorkflow> CaseWorkflow => this.GetTable<CaseWorkflow>();
        public ITable<CaseEvent> CaseEvent => this.GetTable<CaseEvent>();
        public ITable<Case> Case => this.GetTable<Case>();
        public ITable<CaseWorkflowStatus> CaseWorkflowStatus => this.GetTable<CaseWorkflowStatus>();
        public ITable<CaseWorkflowXPath> CaseWorkflowXPath => this.GetTable<CaseWorkflowXPath>();
        public ITable<CaseWorkflowForm> CaseWorkflowForm => this.GetTable<CaseWorkflowForm>();
        public ITable<CaseWorkflowAction> CaseWorkflowAction => this.GetTable<CaseWorkflowAction>();
        public ITable<CaseWorkflowDisplay> CaseWorkflowDisplay => this.GetTable<CaseWorkflowDisplay>();
        public ITable<CaseWorkflowFilter> CaseWorkflowFilter => this.GetTable<CaseWorkflowFilter>();
        public ITable<CaseWorkflowMacro> CaseWorkflowMacro => this.GetTable<CaseWorkflowMacro>();
        public ITable<PermissionSpecification> PermissionSpecification => this.GetTable<PermissionSpecification>();
        public ITable<RoleRegistry> RoleRegistry => this.GetTable<RoleRegistry>();
        public ITable<RoleRegistryPermission> RoleRegistryPermission => this.GetTable<RoleRegistryPermission>();
        public ITable<UserInTenant> UserInTenant => this.GetTable<UserInTenant>();
        public ITable<UserInTenantSwitchLog> UserInTenantSwitchLog => this.GetTable<UserInTenantSwitchLog>();
        public ITable<EntityAnalysisModel> EntityAnalysisModel => this.GetTable<EntityAnalysisModel>();

        public ITable<EntityAnalysisModelGatewayRule> EntityAnalysisModelGatewayRule =>
            this.GetTable<EntityAnalysisModelGatewayRule>();

        public ITable<EntityAnalysisModelActivationRule> EntityAnalysisModelActivationRule =>
            this.GetTable<EntityAnalysisModelActivationRule>();

        public ITable<EntityAnalysisModelSanction> EntityAnalysisModelSanction =>
            this.GetTable<EntityAnalysisModelSanction>();

        public ITable<EntityAnalysisInlineScript> EntityAnalysisInlineScript =>
            this.GetTable<EntityAnalysisInlineScript>();

        public ITable<EntityAnalysisModelListCsvFileUpload> EntityAnalysisModelListCsvFileUpload =>
            this.GetTable<EntityAnalysisModelListCsvFileUpload>();

        public ITable<EntityAnalysisModelDictionaryCsvFileUpload> EntityAnalysisModelDictionaryCsvFileUpload =>
            this.GetTable<EntityAnalysisModelDictionaryCsvFileUpload>();

        public ITable<EntityAnalysisModelInlineFunction> EntityAnalysisModelInlineFunction =>
            this.GetTable<EntityAnalysisModelInlineFunction>();

        public ITable<EntityAnalysisModelRequestXpath> EntityAnalysisModelRequestXpath =>
            this.GetTable<EntityAnalysisModelRequestXpath>();

        public ITable<EntityAnalysisModelTtlCounter> EntityAnalysisModelTtlCounter =>
            this.GetTable<EntityAnalysisModelTtlCounter>();

        public ITable<EntityAnalysisModelAbstractionCalculation> EntityAnalysisModelAbstractionCalculation =>
            this.GetTable<EntityAnalysisModelAbstractionCalculation>();

        public ITable<EntityAnalysisModelHttpAdaptation> EntityAnalysisModelHttpAdaptation =>
            this.GetTable<EntityAnalysisModelHttpAdaptation>();

        public ITable<TenantRegistry> TenantRegistry => this.GetTable<TenantRegistry>();
        public ITable<VisualisationRegistry> VisualisationRegistry => this.GetTable<VisualisationRegistry>();

        public ITable<VisualisationRegistryParameter> VisualisationRegistryParameter =>
            this.GetTable<VisualisationRegistryParameter>();

        public ITable<VisualisationRegistryDatasource> VisualisationRegistryDatasource =>
            this.GetTable<VisualisationRegistryDatasource>();

        public ITable<EntityAnalysisModelDictionary> EntityAnalysisModelDictionary =>
            this.GetTable<EntityAnalysisModelDictionary>();

        public ITable<EntityAnalysisModelReprocessingRule> EntityAnalysisModelReprocessingRule =>
            this.GetTable<EntityAnalysisModelReprocessingRule>();

        public ITable<EntityAnalysisModelReprocessingRuleInstance> EntityAnalysisModelReprocessingRuleInstance =>
            this.GetTable<EntityAnalysisModelReprocessingRuleInstance>();

        public ITable<EntityAnalysisModelList> EntityAnalysisModelList => this.GetTable<EntityAnalysisModelList>();

        public ITable<EntityAnalysisAsynchronousQueueBalance> EntityAnalysisAsynchronousQueueBalance =>
            this.GetTable<EntityAnalysisAsynchronousQueueBalance>();

        public ITable<EntityAnalysisModelListValue> EntityAnalysisModelListValue =>
            this.GetTable<EntityAnalysisModelListValue>();

        public ITable<EntityAnalysisModelOverride> EntityAnalysisModelOverride =>
            this.GetTable<EntityAnalysisModelOverride>();

        public ITable<EntityAnalysisModelOverrideVersion> EntityAnalysisModelOverrideVersion =>
            this.GetTable<EntityAnalysisModelOverrideVersion>();

        public ITable<EntityAnalysisModelActivationRuleOverride> EntityAnalysisModelActivationRuleOverride =>
            this.GetTable<EntityAnalysisModelActivationRuleOverride>();

        public ITable<EntityAnalysisModelActivationRuleOverrideVersion>
            EntityAnalysisModelActivationRuleOverrideVersion =>
            this.GetTable<EntityAnalysisModelActivationRuleOverrideVersion>();

        public ITable<EntityAnalysisModelDictionaryKvp> EntityAnalysisModelDictionaryKvp =>
            this.GetTable<EntityAnalysisModelDictionaryKvp>();

        public ITable<RuleScriptToken> RuleScriptToken => this.GetTable<RuleScriptToken>();
        public ITable<DictionaryEvalExpression> DictionaryEvalExpression => this.GetTable<DictionaryEvalExpression>();

        public ITable<EntityAnalysisModelAbstractionRule> EntityAnalysisModelAbstractionRule =>
            this.GetTable<EntityAnalysisModelAbstractionRule>();

        public ITable<EntityAnalysisModelInlineScript> EntityAnalysisModelInlineScript =>
            this.GetTable<EntityAnalysisModelInlineScript>();

        public ITable<VisualisationRegistryDatasourceSeries> VisualisationRegistryDatasourceSeries =>
            this.GetTable<VisualisationRegistryDatasourceSeries>();

        public ITable<EntityAnalysisModelAsynchronousQueueBalance> EntityAnalysisModelAsynchronousQueueBalance =>
            this.GetTable<EntityAnalysisModelAsynchronousQueueBalance>();

        public ITable<EntityAnalysisModelRequestXpathVersion> EntityAnalysisModelRequestXpathVersion =>
            this.GetTable<EntityAnalysisModelRequestXpathVersion>();

        public ITable<EntityAnalysisModelInlineFunctionVersion> EntityAnalysisModelInlineFunctionVersion =>
            this.GetTable<EntityAnalysisModelInlineFunctionVersion>();

        public ITable<EntityAnalysisModelInlineScriptVersion> EntityAnalysisModelInlineScriptVersion =>
            this.GetTable<EntityAnalysisModelInlineScriptVersion>();

        public ITable<EntityAnalysisModelGatewayRuleVersion> EntityAnalysisModelGatewayRuleVersion =>
            this.GetTable<EntityAnalysisModelGatewayRuleVersion>();

        public ITable<EntityAnalysisModelTagVersion> EntityAnalysisModelTagVersion =>
            this.GetTable<EntityAnalysisModelTagVersion>();

        public ITable<EntityAnalysisModelResponseElevationRangeVersion>
            EntityAnalysisModelResponseElevationRangeVersion =>
            this.GetTable<EntityAnalysisModelResponseElevationRangeVersion>();

        public ITable<EntityAnalysisModelSanctionVersion> EntityAnalysisModelSanctionVersion =>
            this.GetTable<EntityAnalysisModelSanctionVersion>();

        public ITable<EntityAnalysisModelAbstractionRuleVersion> EntityAnalysisModelAbstractionRuleVersion =>
            this.GetTable<EntityAnalysisModelAbstractionRuleVersion>();

        public ITable<EntityAnalysisModelAbstractionCalculationVersion>
            EntityAnalysisModelAbstractionCalculationVersion =>
            this.GetTable<EntityAnalysisModelAbstractionCalculationVersion>();

        public ITable<EntityAnalysisModelHttpAdaptationVersion> EntityAnalysisModelHttpAdaptationVersion =>
            this.GetTable<EntityAnalysisModelHttpAdaptationVersion>();

        public ITable<EntityAnalysisModelActivationRuleVersion> EntityAnalysisModelActivationRuleVersion =>
            this.GetTable<EntityAnalysisModelActivationRuleVersion>();

        public ITable<EntityAnalysisModelListVersion> EntityAnalysisModelListVersion =>
            this.GetTable<EntityAnalysisModelListVersion>();

        public ITable<EntityAnalysisModelDictionaryVersion> EntityAnalysisModelDictionaryVersion =>
            this.GetTable<EntityAnalysisModelDictionaryVersion>();

        public ITable<CaseWorkflowXPathVersion> CaseWorkflowXPathVersion => this.GetTable<CaseWorkflowXPathVersion>();
        public ITable<CaseWorkflowFormVersion> CaseWorkflowFormVersion => this.GetTable<CaseWorkflowFormVersion>();
        public ITable<CaseWorkflowDispatch> CaseWorkflowDispatch => this.GetTable<CaseWorkflowDispatch>();

        public ITable<CaseWorkflowFormValidator> CaseWorkflowFormValidator =>
            this.GetTable<CaseWorkflowFormValidator>();

        public ITable<CaseWorkflowFormValidatorVersion> CaseWorkflowFormValidatorVersion =>
            this.GetTable<CaseWorkflowFormValidatorVersion>();

        public ITable<CaseWorkflowChecklist> CaseWorkflowChecklist => this.GetTable<CaseWorkflowChecklist>();

        public ITable<CaseWorkflowChecklistVersion> CaseWorkflowChecklistVersion =>
            this.GetTable<CaseWorkflowChecklistVersion>();

        public ITable<CaseWorkflowChecklistItem> CaseWorkflowChecklistItem =>
            this.GetTable<CaseWorkflowChecklistItem>();

        public ITable<CaseWorkflowChecklistItemVersion> CaseWorkflowChecklistItemVersion =>
            this.GetTable<CaseWorkflowChecklistItemVersion>();

        public ITable<CaseChecklistItem> CaseChecklistItem => this.GetTable<CaseChecklistItem>();
        public ITable<CaseChecklistEvent> CaseChecklistEvent => this.GetTable<CaseChecklistEvent>();
        public ITable<CaseChecklistOverride> CaseChecklistOverride => this.GetTable<CaseChecklistOverride>();
        public ITable<CaseWorkflowReleaseHook> CaseWorkflowReleaseHook => this.GetTable<CaseWorkflowReleaseHook>();

        public ITable<CaseWorkflowReleaseHookVersion> CaseWorkflowReleaseHookVersion =>
            this.GetTable<CaseWorkflowReleaseHookVersion>();

        public ITable<CaseWorkflowActionVersion> CaseWorkflowActionVersion =>
            this.GetTable<CaseWorkflowActionVersion>();

        public ITable<CaseWorkflowDisplayVersion> CaseWorkflowDisplayVersion =>
            this.GetTable<CaseWorkflowDisplayVersion>();

        public ITable<CaseWorkflowMacroVersion> CaseWorkflowMacroVersion => this.GetTable<CaseWorkflowMacroVersion>();

        public ITable<CaseWorkflowFilterVersion> CaseWorkflowFilterVersion =>
            this.GetTable<CaseWorkflowFilterVersion>();

        public ITable<EntityAnalysisModelListValueVersion> EntityAnalysisModelListValueVersion =>
            this.GetTable<EntityAnalysisModelListValueVersion>();

        public ITable<EntityAnalysisModelDictionaryKvpVersion> EntityAnalysisModelDictionaryKvpVersion =>
            this.GetTable<EntityAnalysisModelDictionaryKvpVersion>();

        public ITable<VisualisationRegistryDatasourceVersion> VisualisationRegistryDatasourceVersion =>
            this.GetTable<VisualisationRegistryDatasourceVersion>();

        public ITable<VisualisationRegistryParameterVersion> VisualisationRegistryParameterVersion =>
            this.GetTable<VisualisationRegistryParameterVersion>();

        public ITable<ExhaustiveSearchInstanceVariableAnomaly> ExhaustiveSearchInstanceVariableAnomaly =>
            this.GetTable<ExhaustiveSearchInstanceVariableAnomaly>();

        public ITable<ExhaustiveSearchInstanceVariableHistogramClassification>
            ExhaustiveSearchInstanceVariableHistogramClassification =>
            this.GetTable<ExhaustiveSearchInstanceVariableHistogramClassification>();

        public ITable<ExhaustiveSearchInstanceVariableHistogramAnomaly>
            ExhaustiveSearchInstanceVariableHistogramAnomaly =>
            this.GetTable<ExhaustiveSearchInstanceVariableHistogramAnomaly>();

        public ITable<ExhaustiveSearchInstanceVariableHistogramClass>
            ExhaustiveSearchInstanceVariableHistogramClass =>
            this.GetTable<ExhaustiveSearchInstanceVariableHistogramClass>();

        public ITable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>
            ExhaustiveSearchInstancePromotedTrialInstanceVariable =>
            this.GetTable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>();

        public ITable<ExhaustiveSearchInstanceData> ExhaustiveSearchInstanceData =>
            this.GetTable<ExhaustiveSearchInstanceData>();

        public ITable<Import> Import => this.GetTable<Import>();
        public ITable<PreservationSnapshot> PreservationSnapshot => this.GetTable<PreservationSnapshot>();
        public ITable<Export> Export => this.GetTable<Export>();
        public ITable<PreservationDiff> PreservationDiff => this.GetTable<PreservationDiff>();
        public ITable<LocalCacheInstance> LocalCacheInstance => this.GetTable<LocalCacheInstance>();
        public ITable<LocalCacheInstanceKey> LocalCacheInstanceKey => this.GetTable<LocalCacheInstanceKey>();
        public ITable<LocalCacheInstanceLru> LocalCacheInstanceLru => this.GetTable<LocalCacheInstanceLru>();

        public ITable<HashCacheAssemblyInstance> HashCacheAssemblyInstance =>
            this.GetTable<HashCacheAssemblyInstance>();

        public ITable<HashCacheAssemblyInstanceEntry> HashCacheAssemblyInstanceEntry =>
            this.GetTable<HashCacheAssemblyInstanceEntry>();

        public ITable<HashCacheAssemblyInstanceJournal> HashCacheAssemblyInstanceJournal =>
            this.GetTable<HashCacheAssemblyInstanceJournal>();

        public ITable<CachePayloadRemovalBatch> CachePayloadRemovalBatch => this.GetTable<CachePayloadRemovalBatch>();

        public ITable<CachePayloadRemovalBatchEntry> CachePayloadRemovalBatchEntry =>
            this.GetTable<CachePayloadRemovalBatchEntry>();

        public ITable<CachePayloadLatestRemovalBatchEntry> CachePayloadLatestRemovalBatchEntry =>
            this.GetTable<CachePayloadLatestRemovalBatchEntry>();

        public ITable<CachePayloadRemovalBatchResponseTime> CachePayloadRemovalBatchResponseTime =>
            this.GetTable<CachePayloadRemovalBatchResponseTime>();

        public ITable<CachePayloadLatestRemovalBatch> CachePayloadLatestRemovalBatch =>
            this.GetTable<CachePayloadLatestRemovalBatch>();

        public ITable<CachePayloadLatestRemovalBatchResponseTime> CachePayloadLatestRemovalBatchResponseTime =>
            this.GetTable<CachePayloadLatestRemovalBatchResponseTime>();

        public ITable<CacheTtlCounterEntryRemovalBatch> CacheTtlCounterEntryRemovalBatch =>
            this.GetTable<CacheTtlCounterEntryRemovalBatch>();

        public ITable<CacheTtlCounterEntryRemovalBatchEntry> CacheTtlCounterEntryRemovalBatchEntry =>
            this.GetTable<CacheTtlCounterEntryRemovalBatchEntry>();

        public ITable<CaseWorkflowStatusRole> CaseWorkflowStatusRole => this.GetTable<CaseWorkflowStatusRole>();
        public ITable<CaseWorkflowRole> CaseWorkflowRole => this.GetTable<CaseWorkflowRole>();
        public ITable<CaseWorkflowMacroRole> CaseWorkflowMacroRole => this.GetTable<CaseWorkflowMacroRole>();
        public ITable<CaseWorkflowFormRole> CaseWorkflowFormRole => this.GetTable<CaseWorkflowFormRole>();
        public ITable<CaseWorkflowActionRole> CaseWorkflowActionRole => this.GetTable<CaseWorkflowActionRole>();
        public ITable<CaseWorkflowDisplayRole> CaseWorkflowDisplayRole => this.GetTable<CaseWorkflowDisplayRole>();
        public ITable<CaseWorkflowXPathRole> CaseWorkflowXPathRole => this.GetTable<CaseWorkflowXPathRole>();
        public ITable<EntityAnalysisModelRole> EntityAnalysisModelRole => this.GetTable<EntityAnalysisModelRole>();

        public ITable<VisualisationRegistryRole> VisualisationRegistryRole =>
            this.GetTable<VisualisationRegistryRole>();

        public ITable<VisualisationRegistryDatasourceRole> VisualisationRegistryDatasourceRole =>
            this.GetTable<VisualisationRegistryDatasourceRole>();

        public ITable<VisualisationRegistryParameterRole> VisualisationRegistryParameterRole =>
            this.GetTable<VisualisationRegistryParameterRole>();

        public ITable<CaseWorkflowFilterRole> CaseWorkflowFilterRole => this.GetTable<CaseWorkflowFilterRole>();
        public ITable<UserRegistryApiKey> UserRegistryApiKey => this.GetTable<UserRegistryApiKey>();
        public ITable<SanctionStopToken> SanctionStopToken => this.GetTable<SanctionStopToken>();
        public ITable<EntityApproval> EntityApproval => this.GetTable<EntityApproval>();

        public ITable<AskJoobyAnalyticsInstance> AskJoobyAnalyticsInstance =>
            this.GetTable<AskJoobyAnalyticsInstance>();

        public ITable<AdvancedAnalyticsCache> AdvancedAnalyticsCache =>
            this.GetTable<AdvancedAnalyticsCache>();

        public ITable<AskJoobyAnalyticsTrainedModel> AskJoobyAnalyticsTrainedModel =>
            this.GetTable<AskJoobyAnalyticsTrainedModel>();

        public ITable<AskJoobyAnalyticsTrainedModelVersion> AskJoobyAnalyticsTrainedModelVersion =>
            this.GetTable<AskJoobyAnalyticsTrainedModelVersion>();

        public ITable<AdvancedAnalyticsTrainingRun> AdvancedAnalyticsTrainingRun =>
            this.GetTable<AdvancedAnalyticsTrainingRun>();

        public ITable<AdvancedAnalyticsTrainingRunStep> AdvancedAnalyticsTrainingRunStep =>
            this.GetTable<AdvancedAnalyticsTrainingRunStep>();

        public ITable<AdvancedAnalyticsTrainingRunTrial> AdvancedAnalyticsTrainingRunTrial =>
            this.GetTable<AdvancedAnalyticsTrainingRunTrial>();

        public ITable<AdvancedAnalyticsTrainingRunIteration> AdvancedAnalyticsTrainingRunIteration =>
            this.GetTable<AdvancedAnalyticsTrainingRunIteration>();

        public ITable<AdvancedAnalyticsTrainingRunVariable> AdvancedAnalyticsTrainingRunVariable =>
            this.GetTable<AdvancedAnalyticsTrainingRunVariable>();

        public ITable<AdvancedAnalyticsTrainingRunMetric> AdvancedAnalyticsTrainingRunMetric =>
            this.GetTable<AdvancedAnalyticsTrainingRunMetric>();

        public ITable<AdvancedAnalyticsTrainingRunCurvePoint> AdvancedAnalyticsTrainingRunCurvePoint =>
            this.GetTable<AdvancedAnalyticsTrainingRunCurvePoint>();
    }
}
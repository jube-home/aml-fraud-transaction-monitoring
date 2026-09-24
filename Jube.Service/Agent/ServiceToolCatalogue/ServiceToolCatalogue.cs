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

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        private static List<ServiceToolDescriptor>? all;

        public static IReadOnlyList<ServiceToolDescriptor> All
        {
            get
            {
                if (all != null)
                {
                    return all;
                }

                var tools = new List<ServiceToolDescriptor>();

                AddEntityAnalysisModel(tools);
                AddEntityAnalysisModelRequestXPath(tools);
                AddEntityAnalysisModelInlineFunction(tools);
                AddEntityAnalysisModelInlineScript(tools);
                AddEntityAnalysisModelGatewayRule(tools);
                AddEntityAnalysisModelSanction(tools);
                AddEntityAnalysisModelTag(tools);
                AddEntityAnalysisModelAbstractionRule(tools);
                AddEntityAnalysisModelAbstractionCalculation(tools);
                AddEntityAnalysisModelHttpAdaptation(tools);
                AddExhaustiveSearchInstance(tools);
                AddEntityAnalysisModelActivationRule(tools);
                AddEntityAnalysisInlineScript(tools);
                AddEntityAnalysisModelTtlCounter(tools);
                AddEntityAnalysisModelList(tools);
                AddEntityAnalysisModelListValue(tools);
                AddEntityAnalysisModelDictionary(tools);
                AddEntityAnalysisModelDictionaryKvp(tools);
                AddEntityAnalysisModelSuppression(tools);
                AddEntityAnalysisModelActivationRuleSuppression(tools);
                AddEntityAnalysisModelReprocessingRule(tools);
                AddEntityAnalysisModelReprocessingRuleInstance(tools);
                AddEntityAnalysisModelStagePerformanceCounter(tools);
                AddEntityAnalysisModelResponseTimePipelineCounter(tools);
                AddEntityAnalysisModelTaskPerformanceCounter(tools);
                AddApplicationLogEntry(tools);
                AddDotNetRuntimeMetric(tools);
                AddPostgresMetric(tools);
                AddRedisMetric(tools);
                AddRedisSlowOperation(tools);
                AddRedisConnectionMultiplexerMetric(tools);
                AddPostgresTableStatistics(tools);
                AddPostgresIndexStatistics(tools);
                AddPostgresReplicationStatus(tools);
                AddPostgresLogEntry(tools);
                AddContainerLogEntry(tools);
                AddDockerEvent(tools);
                AddRedisSentinelStatus(tools);
                AddRedisSentinelEvent(tools);
                AddModelInvokeWarning(tools);
                AddArchiverStagePerformanceCounter(tools);
                AddCaseCreationStagePerformanceCounter(tools);
                AddCaseCreationWarning(tools);
                AddArchiverWarning(tools);
                AddCaptureQueueHealth(tools);
                AddRedisConnectionEvent(tools);
                AddRedisCallCounter(tools);
                AddEtcdMemberStatus(tools);
                AddPatroniMemberStatus(tools);
                AddEtcdClusterEvent(tools);
                AddPatroniClusterEvent(tools);
                AddDockerContainerMetric(tools);
                AddDockerHostMetric(tools);
                AddOpenTelemetryMetric(tools);
                AddOpenTelemetryLogCounter(tools);
                AddOpenTelemetryExclude(tools);
                AddOtlpDispatchCounter(tools);
                AddUserLogin(tools);
                AddUserLogout(tools);
                AddPostgresActivity(tools);
                AddPostgresStatementStatistics(tools);
                AddHttpProcessingCounter(tools);
                AddEntityAnalysisAsynchronousQueueBalance(tools);
                AddEntityAnalysisModelAsynchronousQueueBalance(tools);
                AddEntityAnalysisModelProcessingCounter(tools);
                AddArchive(tools);
                AddActivationWatcher(tools);
                AddCase(tools);
                AddCaseFile(tools);
                AddCaseNote(tools);
                AddCaseWorkflow(tools);
                AddCaseWorkflowAction(tools);
                AddCaseWorkflowActionRole(tools);
                AddCaseWorkflowDisplay(tools);
                AddCaseWorkflowDisplayRole(tools);
                AddCaseWorkflowFilter(tools);
                AddCaseWorkflowFilterRole(tools);
                AddCaseWorkflowForm(tools);
                AddCaseWorkflowFormEntry(tools);
                AddCaseWorkflowFormEntryValue(tools);
                AddCaseWorkflowFormRole(tools);
                AddCaseWorkflowMacro(tools);
                AddCaseWorkflowMacroRole(tools);
                AddCaseWorkflowPriority(tools);
                AddCaseWorkflowRole(tools);
                AddCaseWorkflowStatus(tools);
                AddCaseWorkflowStatusRole(tools);
                AddCaseWorkflowXPath(tools);
                AddCaseWorkflowXPathRole(tools);
                AddEntityAnalysisModelRole(tools);
                AddEntityAnalysisModelSynchronisationSchedule(tools);
                AddExhaustiveSearchInstancePromotedTrialInstance(tools);
                AddPermissionSpecification(tools);
                AddRoleRegistry(tools);
                AddRoleRegistryPermission(tools);
                AddSanctionEntrySource(tools);
                AddTenantRegistry(tools);
                AddUserInTenant(tools);
                AddUserRegistry(tools);
                AddUserRegistryApiKey(tools);
                AddVisualisationRegistry(tools);
                AddVisualisationRegistryDatasource(tools);
                AddVisualisationRegistryDatasourceRole(tools);
                AddVisualisationRegistryDatasourceSeries(tools);
                AddVisualisationRegistryParameter(tools);
                AddVisualisationRegistryParameterRole(tools);
                AddVisualisationRegistryRole(tools);
                AddCaseByCaseKeyValue(tools);
                AddCaseById(tools);
                AddCaseBySessionCaseSearchCompile(tools);
                AddCaseEventByCaseKeyValue(tools);
                AddCaseJournal(tools);
                AddCaseNoteByCaseKeyValue(tools);
                AddCaseWorkflowFormEntryByCaseKeyValue(tools);
                AddEntityAnalysisModelActivationRuleSuppressionQuery(tools);
                AddEntityAnalysisModelSample(tools);
                AddEntityAnalysisModelSuppressionQuery(tools);
                AddEntityAnalysisModelSynchronisationNodeStatusEntries(tools);
                AddEntityAnalysisPotentialMultiPartStringNames(tools);
                AddEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType(tools);
                AddExhaustiveSearchInstancePromotedTrialInstanceConfusion(tools);
                AddExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram(tools);
                AddExhaustiveSearchInstancePromotedTrialInstanceLearningCurve(tools);
                AddExhaustiveSearchInstancePromotedTrialInstancePredictedActual(tools);
                AddExhaustiveSearchInstancePromotedTrialInstanceQuery(tools);
                AddExhaustiveSearchInstancePromotedTrialInstanceRoc(tools);
                AddExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription(tools);
                AddExhaustiveSearchInstanceTrialInstanceVariable(tools);
                AddExhaustiveSearchInstanceTrialInstanceVariableVariance(tools);
                AddExhaustiveSearchInstanceVariable(tools);
                AddVisualisationRegistryDatasourceCommandExecution(tools);
                AddTreeChildren(tools);
                AddSessionCaseJournal(tools);
                AddSessionCaseSearchCompiledSql(tools);
                AddCompletions(tools);
                AddIcons(tools);
                AddParser(tools);
                AddEntityAnalysisModelInvocationContext(tools);
                AddEntityAnalysisModelHistorySimulation(tools);
                AddEntityAnalysisModelIntegrity(tools);
                AddRuleVocabulary(tools);
                AddEntityAnalysisModelTimeWindow(tools);
                AddEntityAnalysisModelDependency(tools);
                AddEntityAnalysisModelBacktest(tools);
                AddCaseWorkflowDisplayExecution(tools);
                AddCaseWorkflowMacroExecution(tools);
                AddRegisterSignalrConnection(tools);
                all = tools;

                return all;
            }
        }

        static partial void AddEntityAnalysisModelInvocationContext(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelHistorySimulation(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelIntegrity(List<ServiceToolDescriptor> tools);
        static partial void AddRuleVocabulary(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelTimeWindow(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelDependency(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelBacktest(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModel(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelRequestXPath(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelInlineFunction(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelInlineScript(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelGatewayRule(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelSanction(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelTag(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelAbstractionRule(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelAbstractionCalculation(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelHttpAdaptation(List<ServiceToolDescriptor> tools);
        static partial void AddExhaustiveSearchInstance(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelActivationRule(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisInlineScript(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelTtlCounter(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelList(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelListValue(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelDictionary(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelDictionaryKvp(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelSuppression(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelActivationRuleSuppression(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelReprocessingRule(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelReprocessingRuleInstance(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelStagePerformanceCounter(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelResponseTimePipelineCounter(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelTaskPerformanceCounter(List<ServiceToolDescriptor> tools);
        static partial void AddApplicationLogEntry(List<ServiceToolDescriptor> tools);
        static partial void AddDotNetRuntimeMetric(List<ServiceToolDescriptor> tools);
        static partial void AddPostgresMetric(List<ServiceToolDescriptor> tools);
        static partial void AddRedisMetric(List<ServiceToolDescriptor> tools);
        static partial void AddRedisSlowOperation(List<ServiceToolDescriptor> tools);
        static partial void AddRedisConnectionMultiplexerMetric(List<ServiceToolDescriptor> tools);
        static partial void AddPostgresTableStatistics(List<ServiceToolDescriptor> tools);
        static partial void AddPostgresIndexStatistics(List<ServiceToolDescriptor> tools);
        static partial void AddPostgresReplicationStatus(List<ServiceToolDescriptor> tools);
        static partial void AddPostgresLogEntry(List<ServiceToolDescriptor> tools);
        static partial void AddContainerLogEntry(List<ServiceToolDescriptor> tools);
        static partial void AddDockerEvent(List<ServiceToolDescriptor> tools);
        static partial void AddRedisSentinelStatus(List<ServiceToolDescriptor> tools);
        static partial void AddRedisSentinelEvent(List<ServiceToolDescriptor> tools);
        static partial void AddModelInvokeWarning(List<ServiceToolDescriptor> tools);
        static partial void AddArchiverStagePerformanceCounter(List<ServiceToolDescriptor> tools);
        static partial void AddCaseCreationStagePerformanceCounter(List<ServiceToolDescriptor> tools);
        static partial void AddCaseCreationWarning(List<ServiceToolDescriptor> tools);
        static partial void AddArchiverWarning(List<ServiceToolDescriptor> tools);
        static partial void AddCaptureQueueHealth(List<ServiceToolDescriptor> tools);
        static partial void AddRedisConnectionEvent(List<ServiceToolDescriptor> tools);
        static partial void AddRedisCallCounter(List<ServiceToolDescriptor> tools);
        static partial void AddEtcdMemberStatus(List<ServiceToolDescriptor> tools);
        static partial void AddPatroniMemberStatus(List<ServiceToolDescriptor> tools);
        static partial void AddEtcdClusterEvent(List<ServiceToolDescriptor> tools);
        static partial void AddPatroniClusterEvent(List<ServiceToolDescriptor> tools);
        static partial void AddDockerContainerMetric(List<ServiceToolDescriptor> tools);
        static partial void AddDockerHostMetric(List<ServiceToolDescriptor> tools);
        static partial void AddOpenTelemetryMetric(List<ServiceToolDescriptor> tools);
        static partial void AddOpenTelemetryLogCounter(List<ServiceToolDescriptor> tools);
        static partial void AddOpenTelemetryExclude(List<ServiceToolDescriptor> tools);
        static partial void AddOtlpDispatchCounter(List<ServiceToolDescriptor> tools);
        static partial void AddUserLogin(List<ServiceToolDescriptor> tools);
        static partial void AddUserLogout(List<ServiceToolDescriptor> tools);
        static partial void AddPostgresActivity(List<ServiceToolDescriptor> tools);
        static partial void AddPostgresStatementStatistics(List<ServiceToolDescriptor> tools);
        static partial void AddHttpProcessingCounter(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisAsynchronousQueueBalance(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelAsynchronousQueueBalance(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelProcessingCounter(List<ServiceToolDescriptor> tools);
        static partial void AddArchive(List<ServiceToolDescriptor> tools);
        static partial void AddActivationWatcher(List<ServiceToolDescriptor> tools);
        static partial void AddCase(List<ServiceToolDescriptor> tools);
        static partial void AddCaseFile(List<ServiceToolDescriptor> tools);
        static partial void AddCaseNote(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflow(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowAction(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowActionRole(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowDisplay(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowDisplayRole(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowFilter(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowFilterRole(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowForm(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowFormEntry(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowFormEntryValue(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowFormRole(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowMacro(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowMacroRole(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowPriority(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowRole(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowStatus(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowStatusRole(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowXPath(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowXPathRole(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelRole(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelSynchronisationSchedule(List<ServiceToolDescriptor> tools);
        static partial void AddExhaustiveSearchInstancePromotedTrialInstance(List<ServiceToolDescriptor> tools);
        static partial void AddPermissionSpecification(List<ServiceToolDescriptor> tools);
        static partial void AddRoleRegistry(List<ServiceToolDescriptor> tools);
        static partial void AddRoleRegistryPermission(List<ServiceToolDescriptor> tools);
        static partial void AddSanctionEntrySource(List<ServiceToolDescriptor> tools);
        static partial void AddTenantRegistry(List<ServiceToolDescriptor> tools);
        static partial void AddUserInTenant(List<ServiceToolDescriptor> tools);
        static partial void AddUserRegistry(List<ServiceToolDescriptor> tools);
        static partial void AddUserRegistryApiKey(List<ServiceToolDescriptor> tools);
        static partial void AddVisualisationRegistry(List<ServiceToolDescriptor> tools);
        static partial void AddVisualisationRegistryDatasource(List<ServiceToolDescriptor> tools);
        static partial void AddVisualisationRegistryDatasourceRole(List<ServiceToolDescriptor> tools);
        static partial void AddVisualisationRegistryDatasourceSeries(List<ServiceToolDescriptor> tools);
        static partial void AddVisualisationRegistryParameter(List<ServiceToolDescriptor> tools);
        static partial void AddVisualisationRegistryParameterRole(List<ServiceToolDescriptor> tools);
        static partial void AddVisualisationRegistryRole(List<ServiceToolDescriptor> tools);
        static partial void AddCaseByCaseKeyValue(List<ServiceToolDescriptor> tools);
        static partial void AddCaseById(List<ServiceToolDescriptor> tools);
        static partial void AddCaseBySessionCaseSearchCompile(List<ServiceToolDescriptor> tools);
        static partial void AddCaseEventByCaseKeyValue(List<ServiceToolDescriptor> tools);
        static partial void AddCaseJournal(List<ServiceToolDescriptor> tools);
        static partial void AddCaseNoteByCaseKeyValue(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowFormEntryByCaseKeyValue(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelActivationRuleSuppressionQuery(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelSample(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelSuppressionQuery(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelSynchronisationNodeStatusEntries(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisPotentialMultiPartStringNames(List<ServiceToolDescriptor> tools);

        static partial void AddEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType(
            List<ServiceToolDescriptor> tools);

        static partial void
            AddExhaustiveSearchInstancePromotedTrialInstanceConfusion(List<ServiceToolDescriptor> tools);

        static partial void AddExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram(
            List<ServiceToolDescriptor> tools);

        static partial void AddExhaustiveSearchInstancePromotedTrialInstanceLearningCurve(
            List<ServiceToolDescriptor> tools);

        static partial void AddExhaustiveSearchInstancePromotedTrialInstancePredictedActual(
            List<ServiceToolDescriptor> tools);

        static partial void AddExhaustiveSearchInstancePromotedTrialInstanceQuery(List<ServiceToolDescriptor> tools);
        static partial void AddExhaustiveSearchInstancePromotedTrialInstanceRoc(List<ServiceToolDescriptor> tools);

        static partial void AddExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription(
            List<ServiceToolDescriptor> tools);

        static partial void AddExhaustiveSearchInstanceTrialInstanceVariable(List<ServiceToolDescriptor> tools);
        static partial void AddExhaustiveSearchInstanceTrialInstanceVariableVariance(List<ServiceToolDescriptor> tools);
        static partial void AddExhaustiveSearchInstanceVariable(List<ServiceToolDescriptor> tools);
        static partial void AddVisualisationRegistryDatasourceCommandExecution(List<ServiceToolDescriptor> tools);
        static partial void AddTreeChildren(List<ServiceToolDescriptor> tools);
        static partial void AddSessionCaseJournal(List<ServiceToolDescriptor> tools);
        static partial void AddSessionCaseSearchCompiledSql(List<ServiceToolDescriptor> tools);
        static partial void AddCompletions(List<ServiceToolDescriptor> tools);
        static partial void AddIcons(List<ServiceToolDescriptor> tools);
        static partial void AddParser(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowDisplayExecution(List<ServiceToolDescriptor> tools);
        static partial void AddCaseWorkflowMacroExecution(List<ServiceToolDescriptor> tools);
        static partial void AddRegisterSignalrConnection(List<ServiceToolDescriptor> tools);
    }
}
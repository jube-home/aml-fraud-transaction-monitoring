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
                AddPostgresActivity(tools);
                AddPostgresStatementStatistics(tools);
                AddHttpProcessingCounter(tools);
                AddEntityAnalysisAsynchronousQueueBalance(tools);
                AddEntityAnalysisModelAsynchronousQueueBalance(tools);
                AddEntityAnalysisModelProcessingCounter(tools);
                all = tools;

                return all;
            }
        }

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
        static partial void AddPostgresActivity(List<ServiceToolDescriptor> tools);
        static partial void AddPostgresStatementStatistics(List<ServiceToolDescriptor> tools);
        static partial void AddHttpProcessingCounter(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisAsynchronousQueueBalance(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelAsynchronousQueueBalance(List<ServiceToolDescriptor> tools);
        static partial void AddEntityAnalysisModelProcessingCounter(List<ServiceToolDescriptor> tools);
    }
}
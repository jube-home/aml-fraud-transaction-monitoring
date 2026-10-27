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

import type { Component } from 'vue'
import { Cpu, Database, MemoryStick, Radar, ScrollText, Server } from 'lucide-vue-next'

export type FamilyId = 'Engine' | 'Logs' | 'Postgres' | 'Redis' | 'Telemetry' | 'Infrastructure'

export interface PerformanceArea {
  area: string
  titleKey: string
}

export interface PerformanceFamily {
  id: FamilyId
  titleKey: string
  hintKey: string
  icon: Component
  areas: PerformanceArea[]
}

export const performanceFamilies: PerformanceFamily[] = [
  {
    id: 'Engine',
    titleKey: 'perfHub.family.engine.title',
    hintKey: 'perfHub.family.engine.hint',
    icon: Cpu,
    areas: [
      { area: 'HttpProcessingCounter', titleKey: 'perfHub.area.HttpProcessingCounter' },
      {
        area: 'EntityAnalysisAsynchronousQueueBalance',
        titleKey: 'perfHub.area.EntityAnalysisAsynchronousQueueBalance',
      },
      { area: 'CaptureQueueHealth', titleKey: 'perfHub.area.CaptureQueueHealth' },
      {
        area: 'EntityAnalysisModelAsynchronousQueueBalance',
        titleKey: 'perfHub.area.EntityAnalysisModelAsynchronousQueueBalance',
      },
      {
        area: 'EntityAnalysisModelProcessingCounter',
        titleKey: 'perfHub.area.EntityAnalysisModelProcessingCounter',
      },
      {
        area: 'EntityAnalysisModelStagePerformanceCounter',
        titleKey: 'perfHub.area.EntityAnalysisModelStagePerformanceCounter',
      },
      {
        area: 'EntityAnalysisModelResponseTimePipelineCounter',
        titleKey: 'perfHub.area.EntityAnalysisModelResponseTimePipelineCounter',
      },
      {
        area: 'EntityAnalysisModelTaskPerformanceCounter',
        titleKey: 'perfHub.area.EntityAnalysisModelTaskPerformanceCounter',
      },
      {
        area: 'ArchiverStagePerformanceCounter',
        titleKey: 'perfHub.area.ArchiverStagePerformanceCounter',
      },
      {
        area: 'CaseCreationStagePerformanceCounter',
        titleKey: 'perfHub.area.CaseCreationStagePerformanceCounter',
      },
      { area: 'ArchiverWarning', titleKey: 'perfHub.area.ArchiverWarning' },
      { area: 'CaseCreationWarning', titleKey: 'perfHub.area.CaseCreationWarning' },
    ],
  },
  {
    id: 'Logs',
    titleKey: 'perfHub.family.logs.title',
    hintKey: 'perfHub.family.logs.hint',
    icon: ScrollText,
    areas: [
      { area: 'ApplicationLogEntry', titleKey: 'perfHub.area.ApplicationLogEntry' },
      { area: 'WafAttack', titleKey: 'perfHub.area.WafAttack' },
      { area: 'PostgresLogEntry', titleKey: 'perfHub.area.PostgresLogEntry' },
      { area: 'ContainerLogEntry', titleKey: 'perfHub.area.ContainerLogEntry' },
      { area: 'ModelInvokeWarning', titleKey: 'perfHub.area.ModelInvokeWarning' },
    ],
  },
  {
    id: 'Postgres',
    titleKey: 'perfHub.family.postgres.title',
    hintKey: 'perfHub.family.postgres.hint',
    icon: Database,
    areas: [
      { area: 'PostgresMetric', titleKey: 'perfHub.area.PostgresMetric' },
      { area: 'PostgresActivity', titleKey: 'perfHub.area.PostgresActivity' },
      { area: 'PostgresStatementStatistics', titleKey: 'perfHub.area.PostgresStatementStatistics' },
      { area: 'PostgresTableStatistics', titleKey: 'perfHub.area.PostgresTableStatistics' },
      { area: 'PostgresIndexStatistics', titleKey: 'perfHub.area.PostgresIndexStatistics' },
      { area: 'PostgresReplicationStatus', titleKey: 'perfHub.area.PostgresReplicationStatus' },
    ],
  },
  {
    id: 'Redis',
    titleKey: 'perfHub.family.redis.title',
    hintKey: 'perfHub.family.redis.hint',
    icon: MemoryStick,
    areas: [
      { area: 'RedisMetric', titleKey: 'perfHub.area.RedisMetric' },
      { area: 'RedisSlowOperation', titleKey: 'perfHub.area.RedisSlowOperation' },
      {
        area: 'RedisConnectionMultiplexerMetric',
        titleKey: 'perfHub.area.RedisConnectionMultiplexerMetric',
      },
      { area: 'RedisSentinelStatus', titleKey: 'perfHub.area.RedisSentinelStatus' },
      { area: 'RedisSentinelEvent', titleKey: 'perfHub.area.RedisSentinelEvent' },
      { area: 'RedisConnectionEvent', titleKey: 'perfHub.area.RedisConnectionEvent' },
      { area: 'RedisCallCounter', titleKey: 'perfHub.area.RedisCallCounter' },
    ],
  },
  {
    id: 'Telemetry',
    titleKey: 'perfHub.family.telemetry.title',
    hintKey: 'perfHub.family.telemetry.hint',
    icon: Radar,
    areas: [
      { area: 'OpenTelemetryMetric', titleKey: 'perfHub.area.OpenTelemetryMetric' },
      { area: 'OtlpDispatchCounter', titleKey: 'perfHub.area.OtlpDispatchCounter' },
      { area: 'DotNetRuntimeMetric', titleKey: 'perfHub.area.DotNetRuntimeMetric' },
    ],
  },
  {
    id: 'Infrastructure',
    titleKey: 'perfHub.family.infrastructure.title',
    hintKey: 'perfHub.family.infrastructure.hint',
    icon: Server,
    areas: [
      { area: 'EtcdMemberStatus', titleKey: 'perfHub.area.EtcdMemberStatus' },
      { area: 'PatroniMemberStatus', titleKey: 'perfHub.area.PatroniMemberStatus' },
      { area: 'EtcdClusterEvent', titleKey: 'perfHub.area.EtcdClusterEvent' },
      { area: 'PatroniClusterEvent', titleKey: 'perfHub.area.PatroniClusterEvent' },
      { area: 'DockerContainerMetric', titleKey: 'perfHub.area.DockerContainerMetric' },
      { area: 'DockerEvent', titleKey: 'perfHub.area.DockerEvent' },
      { area: 'DockerHostMetric', titleKey: 'perfHub.area.DockerHostMetric' },
      { area: 'HaProxyServerStatus', titleKey: 'perfHub.area.HaProxyServerStatus' },
      { area: 'HaProxyReachabilityProbe', titleKey: 'perfHub.area.HaProxyReachabilityProbe' },
      { area: 'OverlayNetworkTaskDrift', titleKey: 'perfHub.area.OverlayNetworkTaskDrift' },
    ],
  },
]

export const securityAreas = ['UserLogin', 'UserLogout']

export function familyById(id: string): PerformanceFamily | undefined {
  return performanceFamilies.find((family) => family.id === id)
}

export function familyOfArea(area: string): PerformanceFamily | undefined {
  return performanceFamilies.find((family) => family.areas.some((item) => item.area === area))
}

export function areaById(area: string): PerformanceArea | undefined {
  return performanceFamilies.flatMap((family) => family.areas).find((item) => item.area === area)
}

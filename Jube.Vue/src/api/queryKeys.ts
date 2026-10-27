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
import { apiLanguage } from './language'

import type { MappingEntry } from '../datasource/types'
import type { OverrideScope } from '../overrides/types'
import type { SanctionParams } from '../sanction/types'

export const queryKeys = {
  datasourceChartKinds: () => ['datasource', 'chartKinds', apiLanguage.value] as const,
  datasourceIntrospect: (
    registryId: number,
    command: string,
    kindId: number,
    mapping: MappingEntry[],
    templateText: string,
  ) =>
    [
      'datasource',
      'introspect',
      registryId,
      command,
      kindId,
      mapping,
      templateText,
      apiLanguage.value,
    ] as const,
  datasourcePreview: (
    registryId: number,
    command: string,
    kindId: number,
    mapping: MappingEntry[],
    templateText: string,
    includeGrid: boolean,
    parameters: unknown,
  ) =>
    [
      'datasource',
      'preview',
      registryId,
      command,
      kindId,
      mapping,
      templateText,
      includeGrid,
      parameters,
      apiLanguage.value,
    ] as const,
  datasourceRegistries: () => ['datasource', 'registries'] as const,
  datasourceList: (registryId: number) => ['datasource', 'list', registryId] as const,
  datasourceRecord: (id: number) => ['datasource', 'record', id] as const,
  visualisationDirectory: () => ['visualisation', 'directory'] as const,
  visualisationSummary: () => ['visualisation', 'summary'] as const,
  visualisationRegistry: (guid: string) => ['visualisation', 'registry', guid] as const,
  visualisationParameters: (registryId: number) =>
    ['visualisation', 'parameters', registryId] as const,
  visualisationTiles: (registryId: number) => ['visualisation', 'tiles', registryId] as const,
  visualisationCard: (id: number, parameters: unknown, generation: number, contextId = 0) =>
    ['visualisation', 'card', id, parameters, generation, contextId, apiLanguage.value] as const,
  preservationSummary: () => ['preservation', 'summary'] as const,
  preservationSnapshots: () => ['preservation', 'snapshots'] as const,
  antiforgery: () => ['antiforgery'] as const,
  me: () => ['me'] as const,
  service: (area: string) => ['service', area] as const,
  tenants: () => ['tenants'] as const,
  caseFilterCompletions: (workflow: string) =>
    ['filterBuilder', 'caseCompletions', workflow] as const,
  caseFilterStatuses: (workflow: string) => ['filterBuilder', 'caseStatuses', workflow] as const,
  exhaustiveFilterCompletions: (source: string) =>
    ['filterBuilder', 'exhaustiveCompletions', source] as const,
  permissionRoles: () => ['permissions', 'roles'] as const,
  permissionSpecifications: () => ['permissions', 'specifications'] as const,
  permissionGrants: (roleRegistryId: number) => ['permissions', 'grants', roleRegistryId] as const,
  roleAllocation: (baseEndpoint: string, guid: string) =>
    ['roleAllocation', baseEndpoint, guid] as const,
  approval: (endpoint: string, entityId: number) => ['approval', endpoint, entityId] as const,
  approvalStatus: (kind: number, entityId: number) =>
    ['approval', 'status', kind, entityId] as const,
  approvalCompare: (endpoint: string, fromId: number, toId: number) =>
    ['approval', endpoint, 'compare', fromId, toId] as const,
  valueGrid: (endpoint: string, parentId: number) => ['valueGrid', endpoint, parentId] as const,
  formModels: () => ['formModel', 'all', apiLanguage.value] as const,
  formModelAreas: () => ['formModel', 'areas'] as const,
  gridAreas: () => ['gridModel', 'areas'] as const,
  gridModel: (area: string) => ['gridModel', area, apiLanguage.value] as const,
  gridRows: (area: string, params: Record<string, string | number>) =>
    ['gridData', area, params] as const,
  formModel: (area: string) => ['formModel', area, 'model', apiLanguage.value] as const,
  apiKeys: (userRegistryId: number) => ['service', 'UserRegistryApiKey', userRegistryId] as const,
  formRecords: (area: string, parentId: number | string | undefined) =>
    ['service', area, 'records', parentId ?? 0] as const,
  formLookup: (source: string) => ['formModel', 'lookup', source] as const,
  overrideKeyNames: () => ['override', 'keyNames'] as const,
  overrideKeys: () => ['override', 'keys'] as const,
  overrideValues: (overrideKey: string) => ['override', 'values', overrideKey] as const,
  overrideModels: (scope: OverrideScope) => ['override', 'models', scope] as const,
  overrideRules: (scope: OverrideScope, entityAnalysisModelGuid: string) =>
    ['override', 'rules', scope, entityAnalysisModelGuid] as const,
  exhaustive: (id: number, part: string, ...rest: (string | number)[]) =>
    ['exhaustive', id, part, ...rest] as const,
  analyticsInstances: () => ['analytics', 'instances'] as const,
  analyticsModelNames: () => ['analytics', 'modelNames'] as const,
  analyticsStatuses: (modelIds: number[]) => ['analytics', 'statuses', modelIds] as const,
  analyticsModels: (instanceId: number) => ['analytics', 'models', instanceId] as const,
  analyticsModel: (modelId: number) => ['analytics', 'model', modelId] as const,
  analyticsVersions: (modelId: number) => ['analytics', 'versions', modelId] as const,
  analyticsVersion: (versionId: number) => ['analytics', 'version', versionId] as const,
  analyticsPromotion: (versionId: number) => ['analytics', 'promotion', versionId] as const,
  analyticsNarrative: (versionId: number) => ['analytics', 'narrative', versionId] as const,
  analyticsCompare: (a: number, b: number) => ['analytics', 'compare', a, b] as const,
  datasets: () => ['datasets', 'list'] as const,
  datasetDetail: (kind: string, id: number) => ['datasets', 'detail', kind, id] as const,
  sanctionSources: () => ['sanctions', 'sources'] as const,
  sampleModels: () => ['sample', 'models'] as const,
  integrityModels: () => ['integrity', 'models'] as const,
  integrityReport: (modelId: number) => ['integrity', 'report', modelId] as const,
  integrityEngine: (modelId: number) => ['integrity', 'engine', modelId] as const,
  integrityGraph: (modelId: number) => ['integrity', 'graph', modelId] as const,
  integrityPending: (modelId: number) => ['integrity', 'pending', modelId] as const,
  integritySchedule: (modelId?: number) => ['integrity', 'schedule', modelId ?? 'tenant'] as const,
  integritySyncNodes: () => ['integrity', 'syncNodes'] as const,
  backtestFields: (modelId: number) => ['ruleTools', 'fields', modelId] as const,
  backtestInstances: (modelId: number, ruleType: string, ruleId: number | null) =>
    ['ruleTools', 'instances', modelId, ruleType, ruleId] as const,
  backtestResult: (id: number) => ['ruleTools', 'result', id] as const,
  ruleDependencies: (modelId: number, ruleType: string, ruleId: number) =>
    ['ruleTools', 'dependencies', modelId, ruleType, ruleId] as const,
  ruleDependents: (modelId: number, ruleType: string, ruleId: number) =>
    ['ruleTools', 'dependents', modelId, ruleType, ruleId] as const,
  sanctionSearch: (params: SanctionParams | null) => ['sanction', 'search', params] as const,
  ruleVocabulary: () => ['ruleBuilder', 'vocabulary'] as const,
  ruleOperators: () => ['ruleBuilder', 'operators'] as const,
  ruleCompletions: (entityAnalysisModelId: number, parseType: number) =>
    ['ruleBuilder', 'completions', entityAnalysisModelId, parseType] as const,
}

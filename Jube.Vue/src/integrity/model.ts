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

import type {
  EngineInstance,
  EngineLoaded,
  EngineState,
  GraphEdge,
  IntegrityCheck,
  IntegrityReport,
  ModelGraph,
  PendingRow,
  Severity,
} from './types'

export const integrityPermission = { view: 60, synchronise: 5 } as const

export type Verdict = 'unsound' | 'warnings' | 'sound'

export function verdictOf(report: { errors: number; warnings: number }): Verdict {
  if (report.errors > 0) return 'unsound'

  return report.warnings > 0 ? 'warnings' : 'sound'
}

export const severityRank: Record<Severity, number> = { Error: 0, Warning: 1, Info: 2 }

export interface FindingRow {
  id: string
  kind: string
  name: string
  entityId: number | null
  checks: IntegrityCheck[]
  errors: number
  warnings: number
  infos: number
  worst: Severity
  worstRank: number
  categories: string
}

export function sortedChecks(checks: IntegrityCheck[]): IntegrityCheck[] {
  return [...checks].sort((a, b) => severityRank[a.severity] - severityRank[b.severity])
}

export function findingRows(report: IntegrityReport): FindingRow[] {
  const groups = new Map<string, FindingRow>()

  for (const check of report.checks) {
    const kind =
      check.entityKind ||
      (check.category === 'Engine' && check.entityName ? 'Engine instance' : 'Model')
    const name = check.entityName || report.modelName || ''
    const key = `${kind}|${check.entityId === null ? name : check.entityId}`
    const group = groups.get(key) ?? {
      id: key,
      kind,
      name,
      entityId: check.entityId,
      checks: [],
      errors: 0,
      warnings: 0,
      infos: 0,
      worst: 'Info' as Severity,
      worstRank: 2,
      categories: '',
    }

    group.checks.push(check)

    if (check.severity === 'Error') group.errors++
    else if (check.severity === 'Warning') group.warnings++
    else group.infos++

    groups.set(key, group)
  }

  return [...groups.values()]
    .map((group) => {
      const worst: Severity = group.errors > 0 ? 'Error' : group.warnings > 0 ? 'Warning' : 'Info'

      return {
        ...group,
        worst,
        worstRank: severityRank[worst],
        categories: [...new Set(group.checks.map((check) => check.category))].join(', '),
      }
    })
    .sort(
      (a, b) =>
        a.worstRank - b.worstRank || a.kind.localeCompare(b.kind) || a.name.localeCompare(b.name),
    )
}

export interface EngineRow {
  id: string
  instance: string
  heartbeatDate: string | null
  synchronisedDate: string | null
  started: boolean | null
  loadedTotal: number
  loaded: EngineLoaded[]
}

export function engineRows(state: EngineState): EngineRow[] {
  const rows = new Map<string, EngineRow>()

  for (const node of state.nodes) {
    const instance = node.instance ?? ''

    rows.set(instance, {
      id: instance,
      instance,
      heartbeatDate: node.heartbeatDate,
      synchronisedDate: node.synchronisedDate,
      started: null,
      loadedTotal: 0,
      loaded: [],
    })
  }

  for (const instance of state.instances) {
    const row: EngineRow = rows.get(instance.instance) ?? {
      id: instance.instance,
      instance: instance.instance,
      heartbeatDate: null,
      synchronisedDate: null,
      started: null,
      loadedTotal: 0,
      loaded: [],
    }

    row.started = instance.started
    row.loaded = instance.loaded
    row.loadedTotal = instance.loaded.reduce((total, loaded) => total + loaded.count, 0)
    rows.set(row.instance, row)
  }

  return [...rows.values()]
}

export type EngineModelState = 'started' | 'loaded' | 'notLoaded'

export function engineModelState(row: Pick<EngineRow, 'started'>): EngineModelState {
  return row.started === null ? 'notLoaded' : row.started ? 'started' : 'loaded'
}

export interface DependencyLink {
  entity: string
  through: string
  how: 'setting' | 'ruleText'
  missing: boolean
}

export interface DependencyRow {
  id: string
  kind: string
  name: string
  active: boolean
  uses: DependencyLink[]
  usedBy: DependencyLink[]
  usesCount: number
  usedByCount: number
  missingCount: number
}

export function dependencyRows(graph: ModelGraph): DependencyRow[] {
  const labels = new Map<string, string>()

  for (const node of graph.nodes) {
    labels.set(
      node.id,
      node.kind === 'Missing' ? `${node.label} (does not exist)` : `${node.kind} ${node.label}`,
    )
  }

  const how = (edge: GraphEdge): 'setting' | 'ruleText' => (edge.dashed ? 'setting' : 'ruleText')

  return graph.nodes
    .filter((node) => node.kind !== 'Missing')
    .map((node) => {
      const uses = graph.edges
        .filter((edge) => edge.from === node.id)
        .map((edge) => ({
          entity: labels.get(edge.to) ?? edge.to,
          through: edge.label,
          how: how(edge),
          missing: edge.to.startsWith('Missing:'),
        }))
      const usedBy = graph.edges
        .filter((edge) => edge.to === node.id)
        .map((edge) => ({
          entity: labels.get(edge.from) ?? edge.from,
          through: edge.label,
          how: how(edge),
          missing: false,
        }))

      return {
        id: node.id,
        kind: node.kind,
        name: node.label,
        active: node.active,
        uses,
        usedBy,
        usesCount: uses.length,
        usedByCount: usedBy.length,
        missingCount: uses.filter((link) => link.missing).length,
      }
    })
    .sort(
      (a, b) =>
        b.missingCount - a.missingCount ||
        a.kind.localeCompare(b.kind) ||
        a.name.localeCompare(b.name),
    )
}

export interface ApprovalKindInfo {
  kind: number
  name: string
  endpoint: string | null
  historyRoute: string | null
  valueKind?: number
}

export const approvalKinds: ApprovalKindInfo[] = [
  {
    kind: 1,
    name: 'Model',
    endpoint: '/api/EntityAnalysisModel',
    historyRoute: 'EntityAnalysisModelVersionHistory',
  },
  {
    kind: 2,
    name: 'List',
    endpoint: '/api/EntityAnalysisModelList',
    historyRoute: 'EntityAnalysisModelListVersionHistory',
    valueKind: 3,
  },
  { kind: 3, name: 'List value', endpoint: null, historyRoute: null },
  {
    kind: 4,
    name: 'Dictionary',
    endpoint: '/api/EntityAnalysisModelDictionary',
    historyRoute: 'EntityAnalysisModelDictionaryVersionHistory',
    valueKind: 5,
  },
  { kind: 5, name: 'Dictionary pair', endpoint: null, historyRoute: null },
  {
    kind: 6,
    name: 'Request XPath',
    endpoint: '/api/EntityAnalysisModelRequestXPath',
    historyRoute: 'EntityAnalysisModelRequestXpathVersionHistory',
  },
  {
    kind: 7,
    name: 'Inline script',
    endpoint: '/api/EntityAnalysisModelInlineScript',
    historyRoute: 'EntityAnalysisModelInlineScriptVersionHistory',
  },
  {
    kind: 8,
    name: 'Inline function',
    endpoint: '/api/EntityAnalysisModelInlineFunction',
    historyRoute: 'EntityAnalysisModelInlineFunctionVersionHistory',
  },
  {
    kind: 9,
    name: 'Gateway rule',
    endpoint: '/api/EntityAnalysisModelGatewayRule',
    historyRoute: 'EntityAnalysisModelGatewayRuleVersionHistory',
  },
  {
    kind: 10,
    name: 'Sanction',
    endpoint: '/api/EntityAnalysisModelSanction',
    historyRoute: 'EntityAnalysisModelSanctionVersionHistory',
  },
  {
    kind: 11,
    name: 'Abstraction rule',
    endpoint: '/api/EntityAnalysisModelAbstractionRule',
    historyRoute: 'EntityAnalysisModelAbstractionRuleVersionHistory',
  },
  {
    kind: 12,
    name: 'Abstraction calculation',
    endpoint: '/api/EntityAnalysisModelAbstractionCalculation',
    historyRoute: 'EntityAnalysisModelAbstractionCalculationVersionHistory',
  },
  {
    kind: 13,
    name: 'TTL counter',
    endpoint: '/api/EntityAnalysisModelTtlCounter',
    historyRoute: 'EntityAnalysisModelTtlCounterVersionHistory',
  },
  {
    kind: 14,
    name: 'HTTP adaptation',
    endpoint: '/api/EntityAnalysisModelAdaptation',
    historyRoute: 'EntityAnalysisModelHttpAdaptationVersionHistory',
  },
  {
    kind: 15,
    name: 'Exhaustive instance',
    endpoint: '/api/ExhaustiveSearchInstance',
    historyRoute: 'ExhaustiveSearchInstanceVersionHistory',
  },
  {
    kind: 16,
    name: 'Activation rule',
    endpoint: '/api/EntityAnalysisModelActivationRule',
    historyRoute: 'EntityAnalysisModelActivationRuleVersionHistory',
  },
  {
    kind: 17,
    name: 'Tag',
    endpoint: '/api/EntityAnalysisModelTag',
    historyRoute: 'EntityAnalysisModelTagVersionHistory',
  },
]

export function approvalKindOf(kind: number): ApprovalKindInfo | undefined {
  return approvalKinds.find((entry) => entry.kind === kind)
}

const valueParentKind: Record<number, number> = { 3: 2, 5: 4 }

export interface PendingEntity {
  kind: number
  entityId: number
  name: string
  version: number
  deleted: boolean
  makerUser: string | null
  parentId: number | null
  values: PendingEntity[]
  valueCount: number
  change: 'deletion' | 'edit'
}

export interface PendingGroup {
  kind: number
  kindName: string
  entities: PendingEntity[]
  count: number
  deletions: number
}

export function pendingGroups(rows: PendingRow[]): PendingGroup[] {
  const entities: PendingEntity[] = rows.map((row) => ({
    ...row,
    values: [],
    valueCount: 0,
    change: row.deleted ? 'deletion' : 'edit',
  }))

  const parents = new Map<string, PendingEntity>()

  for (const entity of entities) {
    if (approvalKindOf(entity.kind)?.valueKind !== undefined) {
      parents.set(`${entity.kind}|${entity.entityId}`, entity)
    }
  }

  const groups: PendingGroup[] = []
  const byKind = new Map<number, PendingGroup>()

  for (const entity of entities) {
    const parentKind = valueParentKind[entity.kind]
    const parent =
      parentKind === undefined ? undefined : parents.get(`${parentKind}|${entity.parentId}`)

    if (parent) {
      parent.values.push(entity)
      parent.valueCount = parent.values.length

      continue
    }

    let group = byKind.get(entity.kind)

    if (!group) {
      group = {
        kind: entity.kind,
        kindName: approvalKindOf(entity.kind)?.name ?? String(entity.kind),
        entities: [],
        count: 0,
        deletions: 0,
      }
      byKind.set(entity.kind, group)
      groups.push(group)
    }

    group.entities.push(entity)
  }

  for (const group of groups) {
    group.count = group.entities.reduce((total, entity) => total + 1 + entity.values.length, 0)
    group.deletions = group.entities.filter((entity) => entity.deleted).length
  }

  return groups
}

export function instanceSummary(instance: EngineInstance): number {
  return instance.loaded.reduce((total, loaded) => total + loaded.count, 0)
}

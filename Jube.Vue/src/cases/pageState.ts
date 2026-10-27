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

export const defaultView = 6
export const defaultSort = 0
export const pageSize = 100

export const tabs = ['queue', 'health', 'quality', 'supervision'] as const

export type CasesTab = (typeof tabs)[number]

export const defaultTab: CasesTab = 'queue'

export type CaseSource =
  | { kind: 'view'; view: number }
  | { kind: 'workflow'; guid: string }
  | { kind: 'own'; guid: string }

export interface CasesPageState {
  workflow: string
  source: CaseSource
  sort: number
  caseId: number | null
  tab: CasesTab
}

export const defaultState: CasesPageState = {
  workflow: '',
  source: { kind: 'view', view: defaultView },
  sort: defaultSort,
  caseId: null,
  tab: defaultTab,
}

type Query = Record<string, unknown>

function first(value: unknown): string | undefined {
  const single = Array.isArray(value) ? value[0] : value

  return typeof single === 'string' ? single : undefined
}

function integer(value: string | undefined, min: number, max: number): number | null {
  if (value === undefined || !/^\d+$/.test(value)) return null

  const parsed = Number(value)

  return parsed >= min && parsed <= max ? parsed : null
}

const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

function guid(value: string | undefined): string | null {
  return value !== undefined && guidPattern.test(value) ? value.toLowerCase() : null
}

export function parseState(query: Query): CasesPageState {
  const workflow = guid(first(query.wf)) ?? ''
  const workflowFilter = guid(first(query.filter))
  const own = guid(first(query.mine))
  const view = integer(first(query.view), 1, 7)

  const source: CaseSource = workflowFilter
    ? { kind: 'workflow', guid: workflowFilter }
    : own
      ? { kind: 'own', guid: own }
      : { kind: 'view', view: view ?? defaultView }

  const tab = first(query.tab)

  return {
    workflow,
    source,
    sort: integer(first(query.sort), 0, 6) ?? defaultSort,
    caseId: integer(first(query.case), 1, 2_147_483_647),
    tab: (tabs as readonly string[]).includes(tab ?? '') ? (tab as CasesTab) : defaultTab,
  }
}

export function serialiseState(state: CasesPageState): Record<string, string> {
  const query: Record<string, string> = {}

  if (state.workflow !== '') query.wf = state.workflow

  if (state.source.kind === 'workflow') query.filter = state.source.guid
  else if (state.source.kind === 'own') query.mine = state.source.guid
  else if (state.source.view !== defaultView) query.view = String(state.source.view)

  if (state.sort !== defaultSort) query.sort = String(state.sort)
  if (state.caseId !== null) query.case = String(state.caseId)
  if (state.tab !== defaultTab) query.tab = state.tab

  return query
}

export function sameSource(a: CaseSource, b: CaseSource): boolean {
  if (a.kind !== b.kind) return false

  return a.kind === 'view' ? a.view === (b as typeof a).view : a.guid === (b as typeof a).guid
}

export function sameState(a: CasesPageState, b: CasesPageState): boolean {
  return (
    a.workflow === b.workflow &&
    a.sort === b.sort &&
    a.caseId === b.caseId &&
    a.tab === b.tab &&
    sameSource(a.source, b.source)
  )
}

export function sourceKey(source: CaseSource): string {
  return source.kind === 'view' ? `view:${source.view}` : `${source.kind}:${source.guid}`
}

export function requestFor(state: CasesPageState, skip = 0): import('./types').CaseSearchRequest {
  const request: import('./types').CaseSearchRequest = {
    caseWorkflowGuid: state.workflow,
    sort: state.sort,
    take: pageSize,
    skip,
  }

  if (state.source.kind === 'view') request.view = state.source.view
  else if (state.source.kind === 'workflow') request.caseWorkflowFilterGuid = state.source.guid
  else request.caseSearchFilterGuid = state.source.guid

  return request
}

const stateKeys = ['wf', 'view', 'filter', 'mine', 'sort', 'case', 'tab']

export function mergeState(query: Query, state: CasesPageState): Record<string, string> {
  const kept: Record<string, string> = {}

  for (const [key, value] of Object.entries(query)) {
    const single = first(value)

    if (!stateKeys.includes(key) && single !== undefined) kept[key] = single
  }

  return { ...kept, ...serialiseState(state) }
}

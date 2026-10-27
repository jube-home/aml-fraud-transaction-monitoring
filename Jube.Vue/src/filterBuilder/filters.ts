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
import type { Completion } from '../ruleBuilder/types'

export type SqlFilterType = 'string' | 'integer' | 'double' | 'datetime'

export interface SqlFilter {
  optgroup: string
  id: string
  field: string
  label: string
  type: SqlFilterType
  input?: 'radio' | 'select'
  default_value?: string | number
  values?: Record<string, string>
  operators?: string[]
}

export interface SqlOperator {
  type: string
  optgroup: string
  nb_inputs?: number
  multiple?: boolean
  apply_to?: string[]
}

export interface CaseStatus {
  guid: string
  name: string
}

export interface FilterLabels {
  yes: string
  no: string
  ascending: string
  descending: string
  closedStatus: [string, string, string, string, string]
  priority: [string, string, string, string, string]
  diaryType: [string, string, string, string]
}

export const exhaustiveOperators: SqlOperator[] = [
  { type: 'equal', optgroup: 'basic' },
  { type: 'less', optgroup: 'basic' },
  { type: 'less_or_equal', optgroup: 'basic' },
  { type: 'greater', optgroup: 'basic' },
  { type: 'begins_with', optgroup: 'basic' },
  { type: 'contains', optgroup: 'basic' },
  { type: 'ends_with', optgroup: 'basic' },
]

export const caseWhereOperators: SqlOperator[] = [
  { type: 'equal', optgroup: 'basic' },
  { type: 'not_equal', optgroup: 'basic' },
  { type: 'less', optgroup: 'basic' },
  { type: 'less_or_equal', optgroup: 'basic' },
  { type: 'greater', optgroup: 'basic' },
  { type: 'greater_or_equal', optgroup: 'basic' },
  { type: 'like', optgroup: 'basic', nb_inputs: 1, apply_to: ['string'] },
  { type: 'not_like', optgroup: 'basic', nb_inputs: 1, apply_to: ['string'] },
]

export const caseSelectOperators: SqlOperator[] = [
  { type: 'order', optgroup: 'basic', nb_inputs: 1, multiple: false, apply_to: ['string'] },
]

function yesNo(labels: FilterLabels): Record<string, string> {
  return { 1: labels.yes, 0: labels.no }
}

function scale(labels: string[], first: number): Record<string, string> {
  return Object.fromEntries(labels.map((label, index) => [first + index, label]))
}

function completionFilter(completion: Completion): SqlFilter | null {
  const base = {
    optgroup: completion.group,
    id: completion.name,
    field: completion.field || completion.value,
    label: completion.name,
  }

  switch (completion.dataType) {
    case 'string':
      return { ...base, type: 'string' }
    case 'integer':
      return { ...base, type: 'integer' }
    case 'double':
      return { ...base, type: 'double' }
    case 'boolean':
      return {
        ...base,
        type: 'string',
        input: 'radio',
        default_value: 'True',
        values: { True: 'Yes', False: 'No' },
        operators: ['equal'],
      }
    default:
      return null
  }
}

export function exhaustiveFilters(completions: Completion[]): SqlFilter[] {
  const filters: SqlFilter[] = []

  for (const completion of completions) {
    if (filters.some((filter) => filter.id === completion.name)) continue

    const filter = completionFilter(completion)

    if (filter) filters.push(filter)
  }

  return filters
}

interface CaseColumn {
  id: string
  label: string
  field: string
  type: SqlFilterType
  selectId?: string
  selectField?: string
  selectLabel?: string
  filterId?: string
  extra?: (labels: FilterLabels, statuses: CaseStatus[]) => Partial<SqlFilter>
}

const caseColumns: CaseColumn[] = [
  {
    id: 'CaseKey',
    label: 'CaseKey',
    field: '"Case"."CaseKey"',
    type: 'string',
    selectField: '"CaseKey"',
  },
  { id: 'CaseKeyValue', label: 'CaseKeyValue', field: '"CaseKeyValue"', type: 'string' },
  { id: 'Id', label: 'Id', field: '"Case"."Id"', type: 'integer' },
  {
    id: 'Locked',
    label: 'Locked',
    field: '"Case"."Locked"',
    type: 'integer',
    extra: (labels) => ({
      input: 'radio',
      default_value: 0,
      values: yesNo(labels),
      operators: ['equal'],
    }),
  },
  { id: 'LockedUser', label: 'LockedUser', field: '"Case"."LockedUser"', type: 'string' },
  { id: 'LockedDate', label: 'LockedDate', field: '"Case"."LockedDate"', type: 'datetime' },
  {
    id: 'ClosedStatusId',
    label: 'ClosedStatus',
    field: '"Case"."ClosedStatusId"',
    type: 'integer',
    extra: (labels) => ({ input: 'select', values: scale(labels.closedStatus, 0) }),
  },
  { id: 'ClosedDate', label: 'ClosedDate', field: '"Case"."ClosedDate"', type: 'datetime' },
  { id: 'ClosedUser', label: 'ClosedUser', field: '"Case"."ClosedUser"', type: 'string' },
  {
    id: 'CreatedDate',
    label: 'Created Date',
    selectLabel: 'CreatedDate',
    field: '"Case"."CreatedDate"',
    type: 'datetime',
  },
  {
    id: 'Diary',
    label: 'Diary',
    field: '"Case"."Diary"',
    type: 'integer',
    extra: (labels) => ({
      input: 'radio',
      default_value: 0,
      values: yesNo(labels),
      operators: ['equal'],
    }),
  },
  { id: 'DiaryDate', label: 'DiaryDate', field: '"Case"."DiaryDate"', type: 'datetime' },
  {
    id: 'DiaryTypeId',
    label: 'DiaryType',
    field: '"Case"."DiaryTypeId"',
    type: 'integer',
    extra: (labels) => ({ input: 'select', values: scale(labels.diaryType, 1) }),
  },
  {
    id: 'CaseWorkflowStatus',
    filterId: 'CaseWorkflowStatusGuid',
    label: 'CaseWorkflowStatus',
    field: '"CaseWorkflowStatus"."Guid"',
    selectField: '"CaseWorkflowStatus"."Name"',
    type: 'string',
    extra: (_labels, statuses) => ({
      input: 'select',
      values: Object.fromEntries(statuses.map((status) => [status.guid, status.name])),
    }),
  },
  {
    id: 'Priority',
    label: 'Priority',
    field: '"CaseWorkflowStatus"."Priority"',
    type: 'integer',
    extra: (labels) => ({ input: 'radio', values: scale(labels.priority, 1) }),
  },
  { id: 'Rating', label: 'Rating', field: '"Case"."Rating"', type: 'integer' },
]

function selectFor(
  id: string,
  field: string,
  label: string,
  group: string,
  labels: FilterLabels,
): SqlFilter {
  return {
    optgroup: group,
    id,
    field,
    label,
    type: 'string',
    input: 'radio',
    default_value: 'ASC',
    values: { ASC: labels.ascending, DESC: labels.descending },
    operators: ['order'],
  }
}

export function caseFilterSets(
  completions: Completion[],
  statuses: CaseStatus[],
  labels: FilterLabels,
): { filters: SqlFilter[]; selects: SqlFilter[] } {
  const filters: SqlFilter[] = []
  const selects: SqlFilter[] = []

  for (const column of caseColumns) {
    filters.push({
      optgroup: 'Case',
      id: column.filterId ?? column.id,
      field: column.field,
      label: column.label,
      type: column.type,
      ...column.extra?.(labels, statuses),
    })

    selects.push(
      selectFor(
        column.id,
        column.selectField ?? column.field,
        column.selectLabel ?? column.label,
        'Case',
        labels,
      ),
    )
  }

  for (const completion of completions) {
    if (filters.some((filter) => filter.id === completion.name)) continue

    if (!selects.some((select) => select.id === completion.name)) {
      selects.push(
        selectFor(
          completion.name,
          completion.field || completion.value,
          completion.name,
          completion.group,
          labels,
        ),
      )
    }

    const filter = completionFilter(completion)

    if (filter) filters.push(filter)
  }

  return { filters, selects }
}

export interface FilterIslandApi {
  getRules: () => import('../ruleBuilder/types').BuilderGroup | null
  getSql: () => { sql: string; params: unknown[] } | null
  hasRules: () => boolean
  validate: () => boolean
  validationMessages: () => string[]
  focusFirstError: () => void
}

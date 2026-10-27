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
import type { FormValues } from '../formModel/types'
import { fromLocalInput, toLocalInput } from '../composables/localDateTime'

export interface ValueField {
  field: string
  title: string
  type: 'string' | 'number'
}

export interface ValueConfig {
  endpoint: string
  readParameter: string
  parentField: string
  parentKind: number
  valueFields: ValueField[]
}

export interface ValueRecord {
  id: number
  deleteExpiryDate: string | null
  deletedUser: string | null
  [field: string]: unknown
}

export interface ValueStatus {
  kind: number
  entityId: number
  currentVersion: number
  effectiveVersion: number | null
  pending: boolean
  rejected: boolean
  canApprove: boolean
  makerUser: string | null
  approvalsRecorded: number
  approvalsRequired: number
  note: string | null
  returnedNote: string | null
}

export type ValueState =
  'approved' | 'awaiting' | 'deletionAwaiting' | 'rejected' | 'deletionRejected' | 'unavailable'

export interface ValueRow {
  [field: string]: unknown
  id: number
  isNew: boolean
  editing: boolean
  deleted: boolean
  state: ValueState
  progress: string
  maker: string
  note: string
  returnNote: string
  pending: boolean
  rejected: boolean
  canApprove: boolean
  hasBaseline: boolean
  deleteExpiryDate: string | null
}

export type Editing = { id: number } | { id: 'new' } | null

export function stateOf(
  record: Pick<ValueRecord, 'deletedUser'>,
  status: ValueStatus | undefined,
  unavailable: boolean,
): ValueState {
  if (unavailable) return 'unavailable'

  const deleted = Boolean(record.deletedUser)

  if (status?.pending) return deleted ? 'deletionAwaiting' : 'awaiting'
  if (status?.rejected) return deleted ? 'deletionRejected' : 'rejected'

  return 'approved'
}

export function isVisible(
  record: Pick<ValueRecord, 'deletedUser'>,
  status: ValueStatus | undefined,
): boolean {
  if (!record.deletedUser) return true

  return Boolean(status && (status.pending || status.rejected))
}

export function buildRows(
  records: ValueRecord[],
  statuses: ValueStatus[],
  unavailable: boolean,
  editing: Editing,
  fields: ValueField[],
): ValueRow[] {
  const byId = new Map(statuses.map((status) => [status.entityId, status]))
  const rows: ValueRow[] = []

  if (editing && editing.id === 'new') {
    rows.push(blankRow(fields))
  }

  for (const record of records) {
    const status = byId.get(record.id)

    if (!unavailable && !isVisible(record, status)) continue

    const row: ValueRow = {
      id: record.id,
      isNew: false,
      editing: editing !== null && editing.id === record.id,
      deleted: Boolean(record.deletedUser),
      state: stateOf(record, status, unavailable),
      progress: status?.pending ? `${status.approvalsRecorded} of ${status.approvalsRequired}` : '',
      maker: status?.makerUser ?? '',
      note: status?.note ?? '',
      returnNote: status?.returnedNote ?? '',
      pending: Boolean(status?.pending),
      rejected: Boolean(status?.rejected),
      canApprove: Boolean(status?.canApprove),
      hasBaseline: status?.effectiveVersion !== null && status?.effectiveVersion !== undefined,
      deleteExpiryDate: record.deleteExpiryDate,
    }

    for (const field of fields) row[field.field] = record[field.field] ?? ''

    rows.push(row)
  }

  return rows
}

function blankRow(fields: ValueField[]): ValueRow {
  const row: ValueRow = {
    id: 0,
    isNew: true,
    editing: true,
    deleted: false,
    state: 'approved',
    progress: '',
    maker: '',
    note: '',
    returnNote: '',
    pending: false,
    rejected: false,
    canApprove: false,
    hasBaseline: false,
    deleteExpiryDate: null,
  }

  for (const field of fields) row[field.field] = ''

  return row
}

export type Draft = Record<string, string>

export function draftOf(row: ValueRow, fields: ValueField[]): Draft {
  const draft: Draft = {
    deleteExpiryDate: toLocalInput(row.deleteExpiryDate),
  }

  for (const field of fields) draft[field.field] = String(row[field.field] ?? '')

  return draft
}

export type DraftProblem = { field: string; code: 'number' }

export function draftProblems(draft: Draft, fields: ValueField[]): DraftProblem[] {
  return fields
    .filter(
      (field) =>
        field.type === 'number' &&
        draft[field.field]!.trim() !== '' &&
        !Number.isFinite(Number(draft[field.field])),
    )
    .map((field) => ({ field: field.field, code: 'number' as const }))
}

export function payloadOf(
  config: ValueConfig,
  parentId: number,
  draft: Draft,
  id?: number,
): Record<string, unknown> {
  const body: Record<string, unknown> = {}

  if (id !== undefined) body.id = id

  body[config.parentField] = parentId

  for (const field of config.valueFields) {
    const text = draft[field.field] ?? ''

    body[field.field] = field.type === 'number' ? (text.trim() === '' ? null : Number(text)) : text
  }

  const expiry = draft.deleteExpiryDate ?? ''

  body.deleteExpiryDate = fromLocalInput(expiry)?.toISOString() ?? null

  return body
}

export function matches(row: ValueRow, fields: ValueField[], needle: string): boolean {
  const text = needle.trim().toLowerCase()

  if (text === '') return true

  return fields.some((field) =>
    String(row[field.field] ?? '')
      .toLowerCase()
      .includes(text),
  )
}

export function readPath(config: ValueConfig): string {
  return `${config.endpoint}/By${config.readParameter.charAt(0).toUpperCase()}${config.readParameter.slice(1)}`
}

export function valueConfigOf(values: FormValues): ValueConfig {
  return {
    endpoint: values.endpoint,
    readParameter: values.parentField,
    parentField: values.parentField,
    parentKind: values.parentKind,
    valueFields: values.fields.map((field) => ({
      field: field.field,
      title: field.title,
      type: field.type,
    })),
  }
}

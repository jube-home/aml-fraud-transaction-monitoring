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
import { bodyReasons } from '../api/problems'

export interface ApprovalKind {
  kind: number
  historyRoute: string
}

export const returnKind = { retracted: 1, resubmitted: 2 } as const

export interface ApprovalStatus {
  kind: number
  entityId: number
  name: string
  currentVersion: number
  effectiveVersion: number | null
  deleted: boolean
  pending: boolean
  rejected: boolean
  makerUser: string | null
  approvalsRecorded: number
  approvalsRequired: number
  canApprove: boolean
}

export interface ApprovalRecord {
  entityVersion: number
  state: string
  note: string | null
  createdUser: string | null
  createdDate: string | null
  returnedNote: string | null
  returnKindId: number | null
}

export type VersionRow = Record<string, unknown>

export type ChangeKind = 'created' | 'update' | 'deletion' | 'restore' | 'retracted' | 'resubmitted'

export interface HistoryRow {
  version: number
  change: ChangeKind
  decision: string
  by: string
  when: string
  note: string
  returnNote: string
  pending: boolean
  currentRejected: boolean
}

export function field(row: VersionRow, name: string): unknown {
  const key = Object.keys(row).find((candidate) => candidate.toLowerCase() === name.toLowerCase())

  return key === undefined ? undefined : row[key]
}

export type ApprovalState =
  'approved' | 'awaiting' | 'rejected' | 'deleted' | 'deletionAwaiting' | 'deletionRejected'

export function stateOf(status: ApprovalStatus): ApprovalState {
  if (status.rejected) return status.deleted ? 'deletionRejected' : 'rejected'
  if (status.pending) return status.deleted ? 'deletionAwaiting' : 'awaiting'

  return status.deleted ? 'deleted' : 'approved'
}

export function buildHistoryRows(
  status: ApprovalStatus,
  records: ApprovalRecord[],
  versions: VersionRow[],
): HistoryRow[] {
  const deletedAt = new Set<number>()
  const retractedAt = new Set<number>()
  const resubmittedAt = new Set<number>()

  for (const row of versions) {
    if (field(row, 'deletedUser') || field(row, 'deleted'))
      deletedAt.add(Number(field(row, 'version')))
  }

  for (const record of records) {
    if (record.returnKindId === returnKind.retracted) retractedAt.add(record.entityVersion)
    if (record.returnKindId === returnKind.resubmitted) resubmittedAt.add(record.entityVersion)
  }

  const changeOf = (version: number, isCurrent: boolean): ChangeKind => {
    if (resubmittedAt.has(version - 1)) return 'resubmitted'
    if (retractedAt.has(version - 1)) return 'retracted'
    if (isCurrent && status.deleted) return 'deletion'
    if (version === 1) return 'created'
    if (deletedAt.has(version)) return 'deletion'
    if (deletedAt.has(version - 1)) return 'restore'

    return 'update'
  }

  const pending: HistoryRow[] = status.canApprove
    ? [
        {
          version: status.currentVersion,
          change: changeOf(status.currentVersion, true),
          decision: '',
          by: status.makerUser ?? '',
          when: '',
          note: '',
          returnNote: '',
          pending: true,
          currentRejected: false,
        },
      ]
    : []

  return [
    ...pending,
    ...records.map((record) => ({
      version: record.entityVersion,
      change: changeOf(record.entityVersion, false),
      decision: record.state,
      by: record.createdUser ?? '',
      when: record.createdDate ?? '',
      note: record.note ?? '',
      returnNote: record.returnedNote ?? '',
      pending: false,
      currentRejected: status.rejected && record.entityVersion === status.currentVersion,
    })),
  ]
}

export interface VersionSummary {
  version: number
  by: string
  when: string
  id: number
  previousId: number | null
}

export function summariseVersions(rows: VersionRow[]): VersionSummary[] {
  const ordered = [...rows].sort(
    (a, b) => Number(field(a, 'version')) - Number(field(b, 'version')),
  )

  return ordered.map((row, index) => ({
    version: Number(field(row, 'version')),
    by: String(field(row, 'createdUser') ?? ''),
    when: String(field(row, 'createdDate') ?? ''),
    id: Number(field(row, 'id')),
    previousId: index > 0 ? Number(field(ordered[index - 1]!, 'id')) : null,
  }))
}

export interface FieldChange {
  field: string
  earlier: string
  version: string
}

export function summariseChanges(rows: VersionRow[]): FieldChange[] {
  return rows.map((row) => ({
    field: String(field(row, 'propertyName') ?? ''),
    earlier: String(field(row, 'fromValue') ?? ''),
    version: String(field(row, 'toValue') ?? ''),
  }))
}

export function refusalReasons(body: unknown, fallback: string): string[] {
  return bodyReasons(body, fallback)
}

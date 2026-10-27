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

import type { GridBadge, GridColumn } from '../components/dataGridTypes'
import {
  diffEntryKind,
  snapshotSource,
  type Diff,
  type DiffChange,
  type DiffEntry,
  type ImportPreview,
  type MoveOptions,
  type OptionKey,
  type Snapshot,
  type Summary,
  type SummarySection,
} from './types'

export const optionKeys: OptionKey[] = [
  'visualisations',
  'roles',
  'lists',
  'dictionaries',
  'exhaustive',
  'askJoobyAnalytics',
]

export const defaultMinimumPasswordLength = 12

export function passwordAcceptable(password: string, summary: Summary | undefined): boolean {
  return password.length >= (summary?.minimumPasswordLength ?? defaultMinimumPasswordLength)
}

export const alwaysIncluded = ['models', 'approvals']

export function defaultOptions(): MoveOptions {
  return {
    password: '',
    exhaustive: false,
    lists: true,
    dictionaries: true,
    visualisations: true,
    roles: true,
    askJoobyAnalytics: true,
  }
}

export function sectionOf(summary: Summary | undefined, key: string): SummarySection | undefined {
  return summary?.sections.find((section) => section.key === key)
}

export function selectedBytes(summary: Summary | undefined, options: MoveOptions): number {
  if (!summary) return 0

  const keys = [...alwaysIncluded, ...optionKeys.filter((key) => options[key])]

  return keys.reduce((total, key) => total + (sectionOf(summary, key)?.bytes ?? 0), 0)
}

export function formatBytes(bytes: number, tag: string): string {
  const units = ['byte', 'kilobyte', 'megabyte', 'gigabyte'] as const
  let value = bytes
  let index = 0

  while (value >= 1000 && index < units.length - 1) {
    value /= 1000
    index += 1
  }

  return new Intl.NumberFormat(tag, {
    style: 'unit',
    unit: units[index]!,
    unitDisplay: 'short',
    maximumFractionDigits: index === 0 ? 0 : 1,
  }).format(value)
}

export function changeSummary(changes: DiffChange[], limit = 3): string {
  const shown = changes
    .slice(0, limit)
    .map((change) => `${change.property}: ${change.before ?? '∅'} → ${change.after ?? '∅'}`)

  return changes.length > limit
    ? `${shown.join('; ')}; +${changes.length - limit}`
    : shown.join('; ')
}

export interface EntryRow {
  section: string
  path: string
  kindId: number
  detail: string
}

export function entryRows(diff: Diff): EntryRow[] {
  return diff.entries.map((entry: DiffEntry) => ({
    section: entry.section,
    path: entry.path.join(' › '),
    kindId: entry.kindId,
    detail:
      entry.kindId === diffEntryKind.changed
        ? changeSummary(entry.changes)
        : entry.nestedCount > 0
          ? `+${entry.nestedCount}`
          : '',
  }))
}

export function kindBadge(
  kindId: number,
  labels: { added: string; removed: string; changed: string },
): GridBadge {
  switch (kindId) {
    case diffEntryKind.added:
      return { label: labels.added, tone: 'success' }
    case diffEntryKind.removed:
      return { label: labels.removed, tone: 'error' }
    default:
      return { label: labels.changed, tone: 'info' }
  }
}

export function entryColumns(labels: {
  section: string
  path: string
  kind: string
  detail: string
  added: string
  removed: string
  changed: string
}): GridColumn[] {
  return [
    { field: 'section', title: labels.section, mono: true, width: 190 },
    { field: 'path', title: labels.path, minWidth: 260 },
    {
      field: 'kindId',
      title: labels.kind,
      width: 120,
      badge: (value) => kindBadge(Number(value), labels),
    },
    { field: 'detail', title: labels.detail, minWidth: 220, mono: true },
  ]
}

export function sectionColumns(labels: {
  section: string
  added: string
  removed: string
  changed: string
  unchanged: string
}): GridColumn[] {
  return [
    { field: 'name', title: labels.section, mono: true, minWidth: 200 },
    { field: 'added', title: labels.added, align: 'end', mono: true, width: 110 },
    { field: 'removed', title: labels.removed, align: 'end', mono: true, width: 110 },
    { field: 'changed', title: labels.changed, align: 'end', mono: true, width: 110 },
    { field: 'unchanged', title: labels.unchanged, align: 'end', mono: true, width: 120 },
  ]
}

export function canImport(preview: ImportPreview | undefined, confirmed: boolean): boolean {
  return preview !== undefined && preview.blockers.length === 0 && confirmed
}

export interface SnapshotLabels {
  preservationPage: string
  modelSync: string
  askJooby: string
  beforeImport: string
  ready: string
  failed: string
  unnamed: string
}

export interface SnapshotRow {
  id: number
  name: string
  source: string
  taken: string
  by: string
  models: string
  size: string
  state: string
  inError: boolean
}

export function sourceLabel(id: number, labels: SnapshotLabels): string {
  switch (id) {
    case snapshotSource.modelSync:
      return labels.modelSync
    case snapshotSource.askJooby:
      return labels.askJooby
    case snapshotSource.beforeImport:
      return labels.beforeImport
    default:
      return labels.preservationPage
  }
}

export function snapshotRows(
  snapshots: Snapshot[],
  labels: SnapshotLabels,
  format: {
    date: (value: string) => string
    number: (value: number) => string
    bytes: (value: number) => string
  },
): SnapshotRow[] {
  return snapshots.map((snapshot) => ({
    id: snapshot.id,
    name: snapshot.name?.trim() ? snapshot.name : `${labels.unnamed} ${snapshot.id}`,
    source: sourceLabel(snapshot.snapshotSourceId, labels),
    taken: snapshot.createdDate ? format.date(snapshot.createdDate) : '',
    by: snapshot.createdUser ?? '',
    models:
      snapshot.entityAnalysisModelCount === null
        ? ''
        : format.number(snapshot.entityAnalysisModelCount),
    size: snapshot.bytes === null ? '' : format.bytes(snapshot.bytes),
    state: snapshot.inError ? labels.failed : labels.ready,
    inError: snapshot.inError,
  }))
}

export function snapshotColumns(labels: {
  name: string
  source: string
  taken: string
  by: string
  models: string
  size: string
  state: string
  actions: string
  compare: string
  restore: string
  download: string
  remove: string
  failed: string
  ready: string
}): GridColumn[] {
  return [
    { field: 'name', title: labels.name, minWidth: 220 },
    { field: 'source', title: labels.source, width: 170 },
    { field: 'taken', title: labels.taken, mono: true, width: 170 },
    { field: 'by', title: labels.by, width: 140 },
    { field: 'models', title: labels.models, align: 'end', mono: true, width: 100 },
    { field: 'size', title: labels.size, align: 'end', mono: true, width: 110 },
    {
      field: 'state',
      title: labels.state,
      width: 110,
      badge: (value) =>
        value === labels.failed
          ? { label: labels.failed, tone: 'error' }
          : { label: String(value), tone: 'neutral' },
    },
    {
      field: 'actions',
      title: labels.actions,
      width: 330,
      actions: (row) => {
        const failed = (row as SnapshotRow).inError

        return [
          { id: 'compare', label: labels.compare, disabled: failed },
          { id: 'restore', label: labels.restore, disabled: failed },
          { id: 'download', label: labels.download, disabled: failed },
          { id: 'remove', label: labels.remove, tone: 'error' },
        ]
      },
    },
  ]
}

export const liveValue = 'live'

export function sideValue(id: number | null): string {
  return id === null ? liveValue : String(id)
}

export function sideId(value: string): number | null {
  return value === liveValue ? null : Number(value)
}

export function compareDefaults(snapshots: Snapshot[]): { left: string; right: string } {
  const newest = snapshots.find((snapshot) => !snapshot.inError)

  return { left: newest ? String(newest.id) : liveValue, right: liveValue }
}

export function compareReady(left: string, right: string): boolean {
  return left !== right
}

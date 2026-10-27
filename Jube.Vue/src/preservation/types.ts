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

export const diffEntryKind = { added: 1, removed: 2, changed: 3 } as const

export const diffKind = {
  twoSnapshots: 1,
  snapshotAgainstLive: 2,
  packageImportPreview: 3,
  snapshotRestorePreview: 4,
} as const

export type OptionKey =
  'exhaustive' | 'lists' | 'dictionaries' | 'visualisations' | 'roles' | 'askJoobyAnalytics'

export interface MoveOptions {
  password: string
  exhaustive: boolean
  lists: boolean
  dictionaries: boolean
  visualisations: boolean
  roles: boolean
  askJoobyAnalytics: boolean
}

export interface SummarySection {
  key: string
  count: number
  rows: number
  bytes: number
}

export interface Summary {
  sections: SummarySection[]
  totalBytes: number
  minimumPasswordLength: number
}

export interface DiffChange {
  property: string
  before: string | null
  after: string | null
}

export interface DiffEntry {
  section: string
  path: string[]
  kindId: number
  changes: DiffChange[]
  nestedCount: number
}

export interface DiffSection {
  name: string
  added: number
  removed: number
  changed: number
  unchanged: number
}

export interface DiffSide {
  snapshotId: number | null
  label: string
  createdDate: string | null
}

export interface Diff {
  id: number
  diffKindId: number
  left: DiffSide
  right: DiffSide
  identical: boolean
  added: number
  removed: number
  changed: number
  unchanged: number
  referencesIgnored: number
  truncated: boolean
  sections: DiffSection[]
  entries: DiffEntry[]
}

export interface Advice {
  code: string
  message: string
}

export interface OrphanedUsers {
  count: number
  names: string[]
  importerStaysActive: boolean
}

export interface ImportPreview {
  diff: Diff
  packageVersion: number
  packageGuid: string
  warnings: Advice[]
  blockers: Advice[]
  orphanedUsers: OrphanedUsers
}

export const snapshotSource = {
  preservationPage: 1,
  modelSync: 2,
  askJooby: 3,
  beforeImport: 4,
} as const

export interface Snapshot {
  id: number
  guid: string
  snapshotSourceId: number
  name: string | null
  exportVersion: number | null
  exportGuid: string | null
  entityAnalysisModelCount: number | null
  bytes: number | null
  inError: boolean
  createdUser: string | null
  createdDate: string | null
  completedDate: string | null
}

export interface SnapshotRequest {
  name: string | null
}

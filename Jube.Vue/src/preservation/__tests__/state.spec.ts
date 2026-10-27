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

import { describe, expect, it } from 'vitest'
import {
  canImport,
  compareDefaults,
  compareReady,
  liveValue,
  sideId,
  sideValue,
  snapshotColumns,
  snapshotRows,
  sourceLabel,
  changeSummary,
  defaultOptions,
  entryRows,
  formatBytes,
  kindBadge,
  passwordAcceptable,
  sectionOf,
  selectedBytes,
} from '../state'
import type { Diff, ImportPreview, Snapshot, Summary } from '../types'

const summary: Summary = {
  totalBytes: 1000,
  minimumPasswordLength: 12,
  sections: [
    { key: 'models', count: 2, rows: 5, bytes: 100 },
    { key: 'approvals', count: 1, rows: 0, bytes: 20 },
    { key: 'lists', count: 1, rows: 3, bytes: 300 },
    { key: 'exhaustive', count: 1, rows: 9, bytes: 5000 },
    { key: 'roles', count: 1, rows: 4, bytes: 40 },
  ],
}

const emptyDiff = (): Diff => ({
  id: 1,
  diffKindId: 3,
  left: { snapshotId: null, label: 'Live', createdDate: null },
  right: { snapshotId: null, label: 'x', createdDate: null },
  identical: false,
  added: 0,
  removed: 0,
  changed: 0,
  unchanged: 0,
  referencesIgnored: 0,
  truncated: false,
  sections: [],
  entries: [],
})

const preview = (blockers: number): ImportPreview => ({
  diff: emptyDiff(),
  packageVersion: 2,
  packageGuid: 'g',
  warnings: [],
  blockers: Array.from({ length: blockers }, (_, i) => ({ code: `b${i}`, message: 'm' })),
  orphanedUsers: { count: 0, names: [], importerStaysActive: false },
})

describe('defaultOptions', () => {
  it('carries everything but the largest, as a snapshot does', () => {
    const options = defaultOptions()

    expect(options.exhaustive).toBe(false)
    expect(options.lists && options.dictionaries && options.visualisations && options.roles).toBe(
      true,
    )
    expect(options.askJoobyAnalytics).toBe(true)
    expect(options.password).toBe('')
  })
})

describe('selectedBytes', () => {
  it('always counts models and approvals and only the switched-on sections', () => {
    expect(selectedBytes(summary, { ...defaultOptions(), lists: false, roles: false })).toBe(120)
    expect(selectedBytes(summary, defaultOptions())).toBe(460)
    expect(selectedBytes(summary, { ...defaultOptions(), exhaustive: true })).toBe(5460)
  })

  it('is zero before the summary arrives', () => {
    expect(selectedBytes(undefined, defaultOptions())).toBe(0)
  })

  it('finds a section by key', () => {
    expect(sectionOf(summary, 'lists')?.rows).toBe(3)
    expect(sectionOf(summary, 'nothing')).toBeUndefined()
  })
})

describe('formatBytes', () => {
  it('scales to the largest sensible unit', () => {
    expect(formatBytes(512, 'en-GB')).toBe('512 byte')
    expect(formatBytes(1500, 'en-GB')).toBe('1.5 kB')
    expect(formatBytes(2_500_000, 'en-GB')).toBe('2.5 MB')
  })
})

describe('changeSummary', () => {
  it('shows a few changes and counts the rest', () => {
    const changes = Array.from({ length: 5 }, (_, i) => ({
      property: `p${i}`,
      before: String(i),
      after: null,
    }))

    expect(changeSummary(changes)).toBe('p0: 0 → ∅; p1: 1 → ∅; p2: 2 → ∅; +2')
  })
})

describe('entryRows', () => {
  it('joins the path and describes changes and nested additions', () => {
    const diff = emptyDiff()

    diff.entries = [
      {
        section: 'EntityAnalysisModel',
        path: ['EntityAnalysisModel[A]', 'EntityAnalysisModelTag[t]'],
        kindId: 3,
        changes: [{ property: 'Colour', before: 'red', after: 'blue' }],
        nestedCount: 0,
      },
      {
        section: 'RoleRegistry',
        path: ['RoleRegistry[R]'],
        kindId: 1,
        changes: [],
        nestedCount: 2,
      },
    ]

    expect(entryRows(diff)).toEqual([
      {
        section: 'EntityAnalysisModel',
        path: 'EntityAnalysisModel[A] › EntityAnalysisModelTag[t]',
        kindId: 3,
        detail: 'Colour: red → blue',
      },
      { section: 'RoleRegistry', path: 'RoleRegistry[R]', kindId: 1, detail: '+2' },
    ])
  })
})

describe('kindBadge', () => {
  const labels = { added: 'Added', removed: 'Removed', changed: 'Changed' }

  it('says the change in words with a tone', () => {
    expect(kindBadge(1, labels)).toEqual({ label: 'Added', tone: 'success' })
    expect(kindBadge(2, labels)).toEqual({ label: 'Removed', tone: 'error' })
    expect(kindBadge(3, labels)).toEqual({ label: 'Changed', tone: 'info' })
  })
})

describe('canImport', () => {
  it('needs a preview with no blockers and the confirmation', () => {
    expect(canImport(undefined, true)).toBe(false)
    expect(canImport(preview(0), false)).toBe(false)
    expect(canImport(preview(1), true)).toBe(false)
    expect(canImport(preview(0), true)).toBe(true)
  })
})

const snapshot = (overrides: Partial<Snapshot> = {}): Snapshot => ({
  id: 3,
  guid: 'g',
  snapshotSourceId: 1,
  name: 'Nightly',
  exportVersion: 2,
  exportGuid: null,
  entityAnalysisModelCount: 4,
  bytes: 2048,
  inError: false,
  createdUser: 'alex',
  createdDate: '2026-10-22T10:00:00Z',
  completedDate: null,
  ...overrides,
})

const sourceLabels = {
  preservationPage: 'Page',
  modelSync: 'Sync',
  askJooby: 'Jooby',
  beforeImport: 'Before import',
  ready: 'Ready',
  failed: 'Failed',
  unnamed: 'Snapshot',
}

const formats = {
  date: (value: string) => `d:${value}`,
  number: (value: number) => `n:${value}`,
  bytes: (value: number) => `b:${value}`,
}

describe('sourceLabel', () => {
  it('names every source and falls back to the page', () => {
    expect(sourceLabel(1, sourceLabels)).toBe('Page')
    expect(sourceLabel(2, sourceLabels)).toBe('Sync')
    expect(sourceLabel(3, sourceLabels)).toBe('Jooby')
    expect(sourceLabel(4, sourceLabels)).toBe('Before import')
    expect(sourceLabel(99, sourceLabels)).toBe('Page')
  })
})

describe('snapshotRows', () => {
  it('formats a row and names an unnamed snapshot by its id', () => {
    const rows = snapshotRows([snapshot(), snapshot({ id: 9, name: ' ' })], sourceLabels, formats)

    expect(rows[0]).toMatchObject({
      id: 3,
      name: 'Nightly',
      models: 'n:4',
      size: 'b:2048',
      state: 'Ready',
    })
    expect(rows[1]?.name).toBe('Snapshot 9')
  })

  it('leaves unknown figures blank and flags a failed snapshot', () => {
    const [row] = snapshotRows(
      [snapshot({ entityAnalysisModelCount: null, bytes: null, createdDate: null, inError: true })],
      sourceLabels,
      formats,
    )

    expect(row).toMatchObject({ models: '', size: '', taken: '', state: 'Failed', inError: true })
  })
})

describe('snapshotColumns', () => {
  const labels = {
    name: 'N',
    source: 'S',
    taken: 'T',
    by: 'B',
    models: 'M',
    size: 'Z',
    state: 'St',
    actions: 'A',
    compare: 'Compare',
    restore: 'Restore',
    download: 'Download',
    remove: 'Delete',
    failed: 'Failed',
    ready: 'Ready',
  }

  it('disables every action but delete for a failed snapshot', () => {
    const actions = snapshotColumns(labels).find((column) => column.field === 'actions')?.actions

    expect(actions?.({ inError: true }).map((a) => [a.id, a.disabled ?? false])).toEqual([
      ['compare', true],
      ['restore', true],
      ['download', true],
      ['remove', false],
    ])
    expect(actions?.({ inError: false }).every((a) => !a.disabled)).toBe(true)
  })

  it('badges a failed state as an error in words', () => {
    const badge = snapshotColumns(labels).find((column) => column.field === 'state')?.badge

    expect(badge?.('Failed')).toEqual({ label: 'Failed', tone: 'error' })
    expect(badge?.('Ready')).toEqual({ label: 'Ready', tone: 'neutral' })
  })
})

describe('compare sides', () => {
  it('round trips live and ids', () => {
    expect(sideValue(null)).toBe(liveValue)
    expect(sideValue(5)).toBe('5')
    expect(sideId(liveValue)).toBeNull()
    expect(sideId('5')).toBe(5)
  })

  it('defaults to the newest usable snapshot against live', () => {
    expect(compareDefaults([snapshot({ id: 8, inError: true }), snapshot({ id: 7 })])).toEqual({
      left: '7',
      right: 'live',
    })
    expect(compareDefaults([])).toEqual({ left: 'live', right: 'live' })
  })

  it('needs two different sides', () => {
    expect(compareReady('live', 'live')).toBe(false)
    expect(compareReady('4', '4')).toBe(false)
    expect(compareReady('4', 'live')).toBe(true)
  })
})

describe('passwordAcceptable', () => {
  it('follows the length the server reports', () => {
    expect(passwordAcceptable('elevenchars', summary)).toBe(false)
    expect(passwordAcceptable('twelve-chars', summary)).toBe(true)
    expect(passwordAcceptable('0123456789', { ...summary, minimumPasswordLength: 10 })).toBe(true)
  })

  it('assumes twelve until the server has answered', () => {
    expect(passwordAcceptable('elevenchars', undefined)).toBe(false)
    expect(passwordAcceptable('twelve-chars', undefined)).toBe(true)
  })
})

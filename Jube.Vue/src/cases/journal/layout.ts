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

import type { GridColumn } from '../../components/dataGridTypes'

export interface ColumnEntry {
  field: string
  width?: number
  hidden?: boolean
}

export interface JournalLayout {
  version: 1
  lenses: Record<string, ColumnEntry[]>
}

export const minimumWidth = 40

export function emptyLayout(): JournalLayout {
  return { version: 1, lenses: {} }
}

function entryOf(value: unknown): ColumnEntry | null {
  if (typeof value !== 'object' || value === null) return null

  const record = value as Record<string, unknown>

  if (typeof record.field !== 'string' || record.field === '') return null

  const entry: ColumnEntry = { field: record.field }

  if (typeof record.width === 'number' && Number.isFinite(record.width)) {
    entry.width = Math.max(minimumWidth, Math.round(record.width))
  }

  if (record.hidden === true) entry.hidden = true

  return entry
}

export function parseLayout(json: string | null | undefined): JournalLayout {
  if (!json) return emptyLayout()

  let parsed: unknown

  try {
    parsed = JSON.parse(json)
  } catch {
    return emptyLayout()
  }

  if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) return emptyLayout()

  const lenses = (parsed as Record<string, unknown>).lenses

  if (typeof lenses !== 'object' || lenses === null) return emptyLayout()

  const result = emptyLayout()

  for (const [lens, list] of Object.entries(lenses)) {
    if (!Array.isArray(list)) continue

    const seen = new Set<string>()
    const entries: ColumnEntry[] = []

    for (const item of list) {
      const entry = entryOf(item)

      if (entry && !seen.has(entry.field)) {
        seen.add(entry.field)
        entries.push(entry)
      }
    }

    result.lenses[lens] = entries
  }

  return result
}

export function serialiseLayout(layout: JournalLayout): string {
  return JSON.stringify(layout)
}

export function movable(columns: readonly GridColumn[]): GridColumn[] {
  return columns.filter((column) => !column.pinned)
}

export function complete(
  defaults: readonly GridColumn[],
  entries: readonly ColumnEntry[] | undefined,
): ColumnEntry[] {
  const known = new Map(movable(defaults).map((column) => [column.field, column]))
  const saved = entries ?? []
  const savedFields = new Set(saved.map((entry) => entry.field))
  const result = saved.map((entry) => ({ ...entry }))

  for (const field of known.keys()) {
    if (!savedFields.has(field)) result.push({ field })
  }

  return result
}

export function apply(
  defaults: readonly GridColumn[],
  entries: readonly ColumnEntry[] | undefined,
): GridColumn[] {
  const pinned = defaults.filter((column) => column.pinned)
  const byField = new Map(movable(defaults).map((column) => [column.field, column]))
  const shown: GridColumn[] = []

  for (const entry of complete(defaults, entries)) {
    const column = byField.get(entry.field)

    if (!column || entry.hidden) continue

    shown.push(entry.width ? { ...column, width: entry.width, minWidth: undefined } : column)
  }

  return [...pinned, ...shown]
}

export function visibleFields(
  defaults: readonly GridColumn[],
  entries: readonly ColumnEntry[] | undefined,
): string[] {
  const known = new Set(movable(defaults).map((column) => column.field))

  return complete(defaults, entries)
    .filter((entry) => known.has(entry.field) && !entry.hidden)
    .map((entry) => entry.field)
}

export function moved(
  defaults: readonly GridColumn[],
  entries: readonly ColumnEntry[] | undefined,
  order: readonly string[],
): ColumnEntry[] {
  const all = complete(defaults, entries)
  const visible = new Set(visibleFields(defaults, entries))
  const incoming = order.filter((field) => visible.has(field))

  if (incoming.length !== visible.size) return all

  const queue = [...incoming]

  return all.map((entry) => {
    if (!visible.has(entry.field)) return entry

    const field = queue.shift() as string

    return { ...(all.find((item) => item.field === field) as ColumnEntry) }
  })
}

export function resized(
  defaults: readonly GridColumn[],
  entries: readonly ColumnEntry[] | undefined,
  field: string,
  width: number,
): ColumnEntry[] {
  return complete(defaults, entries).map((entry) =>
    entry.field === field ? { ...entry, width: Math.max(minimumWidth, Math.round(width)) } : entry,
  )
}

export function shown(
  defaults: readonly GridColumn[],
  entries: readonly ColumnEntry[] | undefined,
  field: string,
  on: boolean,
): ColumnEntry[] {
  return complete(defaults, entries).map((entry) => {
    if (entry.field !== field) return entry

    const { hidden: _hidden, ...rest } = entry

    return on ? rest : { ...rest, hidden: true }
  })
}

export function withLens(
  layout: JournalLayout,
  lens: string,
  entries: ColumnEntry[] | undefined,
): JournalLayout {
  const lenses = { ...layout.lenses }

  if (entries === undefined) delete lenses[lens]
  else lenses[lens] = entries

  return { version: 1, lenses }
}

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

import type { JournalRow } from './types'

export const transactionKey = 'EntityAnalysisModelInstanceEntryGuid'

export interface PresentTag {
  name: string
  rows: number
}

export function guidOf(row: JournalRow): string {
  return String(row[transactionKey] ?? '')
}

export function tagsOf(row: JournalRow): string[] {
  const value = row.Tag

  return Array.isArray(value) ? value.map((item) => String(item)) : []
}

export function presentTags(
  rows: readonly JournalRow[],
  selected: ReadonlySet<string>,
): PresentTag[] {
  const counts = new Map<string, number>()

  for (const row of rows) {
    if (!selected.has(guidOf(row))) continue

    for (const name of new Set(tagsOf(row))) counts.set(name, (counts.get(name) ?? 0) + 1)
  }

  return [...counts.entries()]
    .map(([name, count]) => ({ name, rows: count }))
    .sort((left, right) => left.name.localeCompare(right.name))
}

export function keepSelected(
  selected: ReadonlySet<string>,
  rows: readonly JournalRow[],
): Set<string> {
  const present = new Set(rows.map(guidOf))

  return new Set([...selected].filter((guid) => present.has(guid)))
}

export function toggled(selected: ReadonlySet<string>, guid: string, on: boolean): Set<string> {
  const next = new Set(selected)

  if (on) next.add(guid)
  else next.delete(guid)

  return next
}

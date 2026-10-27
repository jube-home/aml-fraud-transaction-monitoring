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
export interface LeafText {
  yes: string
  no: string
  none: string
  empty: string
  number: (value: number) => string
}

export type EvidenceKind = 'branch' | 'leaf' | 'null' | 'empty'

export interface EvidenceRow {
  key: string
  value: string
  kind: EvidenceKind
  _children?: EvidenceRow[]
}

export function formatLeaf(value: unknown, text: LeafText): { value: string; kind: EvidenceKind } {
  if (value === null || value === undefined) return { value: text.none, kind: 'null' }
  if (typeof value === 'boolean') return { value: value ? text.yes : text.no, kind: 'leaf' }
  if (typeof value === 'number') return { value: text.number(value), kind: 'leaf' }
  if (value === '') return { value: text.empty, kind: 'empty' }

  return { value: String(value), kind: 'leaf' }
}

export function evidenceRow(key: string, value: unknown, text: LeafText): EvidenceRow {
  if (value !== null && typeof value === 'object') {
    const entries: [string, unknown][] = Array.isArray(value)
      ? value.map((item, index) => [`[${index}]`, item])
      : Object.entries(value)

    if (entries.length === 0) {
      return { key, value: Array.isArray(value) ? '[]' : '{}', kind: 'empty' }
    }

    return {
      key,
      value: '',
      kind: 'branch',
      _children: entries.map(([childKey, child]) => evidenceRow(childKey, child, text)),
    }
  }

  return { key, ...formatLeaf(value, text) }
}

export function evidenceRows(evidence: unknown, text: LeafText): EvidenceRow[] {
  if (evidence === null || typeof evidence !== 'object') return []

  const entries: [string, unknown][] = Array.isArray(evidence)
    ? evidence.map((item, index) => [`[${index}]`, item])
    : Object.entries(evidence)

  return entries.map(([key, value]) => evidenceRow(key, value, text))
}

export function countLeaves(rows: EvidenceRow[]): number {
  return rows.reduce((total, row) => total + (row._children ? countLeaves(row._children) : 1), 0)
}

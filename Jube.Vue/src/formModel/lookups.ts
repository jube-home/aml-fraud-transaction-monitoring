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

import type { FormLookup, FormModel } from './types'

const emptyGuid = '00000000-0000-0000-0000-000000000000'

export interface LookupChoice {
  value: unknown
  text: string
  upper?: number
}

export function isBlank(value: unknown): boolean {
  return value === null || value === undefined || value === '' || value === emptyGuid
}

export function lookupUrl(lookup: FormLookup, parentValue: unknown): string | undefined {
  if (!lookup.parentField) return lookup.source

  return isBlank(parentValue)
    ? undefined
    : `${lookup.source}/${encodeURIComponent(String(parentValue))}`
}

export function lookupChoices(rows: unknown, lookup: FormLookup): LookupChoice[] {
  if (!Array.isArray(rows)) return []

  return rows.flatMap((row): LookupChoice[] => {
    if (typeof row === 'string' || typeof row === 'number') {
      return [{ value: row, text: String(row) }]
    }

    if (typeof row !== 'object' || row === null) return []

    const record = row as Record<string, unknown>
    const value = record[lookup.valueField]

    if (value === undefined) return []

    const upper = lookup.upperField ? record[lookup.upperField] : undefined

    return [
      {
        value,
        text: String(record[lookup.textField] ?? value),
        ...(typeof upper === 'number' ? { upper } : {}),
      },
    ]
  })
}

export function lookupSelected(choices: readonly LookupChoice[], value: unknown): string {
  if (value === null || value === undefined) return ''

  const text = String(value)
  const exact = choices.find((choice) => String(choice.value) === text)

  if (exact) return String(exact.value)

  const number = typeof value === 'number' ? value : Number(value)

  if (!Number.isFinite(number)) return text

  const inside = choices.find(
    (choice) =>
      typeof choice.value === 'number' &&
      choice.upper !== undefined &&
      number >= choice.value &&
      number <= choice.upper,
  )

  return inside ? String(inside.value) : text
}

export function dependentsOf(model: FormModel, name: string): string[] {
  const found: string[] = []
  const queue = [name]

  while (queue.length > 0) {
    const parent = queue.shift()!

    for (const field of model.fields) {
      if (field.lookup?.parentField === parent && !found.includes(field.name)) {
        found.push(field.name)
        queue.push(field.name)
      }
    }
  }

  return found
}

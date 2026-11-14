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

export type Convert = ((value: unknown) => unknown) | null

export function setOwn(target: Record<string, unknown>, key: string, value: unknown): void {
  if (key === '__proto__') {
    Object.defineProperty(target, key, {
      value,
      enumerable: true,
      writable: true,
      configurable: true,
    })
  } else {
    target[key] = value
  }
}

export function buildRows(
  rowCount: number,
  keys: string[],
  columns: unknown[][],
  sparse: [number, number[]][] | null | undefined,
  nullRows: number[] | null | undefined,
  inner: Convert,
): (Record<string, unknown> | null)[] {
  const absent = new Map<number, Set<number>>()
  const nulls = new Set<number>(nullRows ?? [])
  const rows: (Record<string, unknown> | null)[] = new Array<Record<string, unknown> | null>(
    rowCount,
  )

  for (const [column, missing] of sparse ?? []) {
    absent.set(column, new Set(missing))
  }

  for (let row = 0; row < rowCount; row += 1) {
    if (nulls.has(row)) {
      rows[row] = null
      continue
    }

    const item: Record<string, unknown> = {}

    for (let column = 0; column < keys.length; column += 1) {
      if (absent.get(column)?.has(row)) continue

      const cell = columns[column]![row] ?? null

      setOwn(item, keys[column]!, inner === null ? cell : inner(cell))
    }

    rows[row] = item
  }

  return rows
}

export function many(value: unknown, convert: (item: unknown) => unknown): unknown {
  if (value == null) return value

  return (value as unknown[]).map((item) => convert(item))
}

export function dict(value: unknown, convert: (item: unknown) => unknown): unknown {
  if (value == null) return value

  const result: Record<string, unknown> = {}

  for (const [key, item] of Object.entries(value as Record<string, unknown>)) {
    setOwn(result, key, convert(item))
  }

  return result
}

export function rows(value: unknown, inner: Convert): unknown {
  if (value == null) return value

  const [rowCount, keys, columns, sparse, nullRows] = value as [
    number,
    string[],
    unknown[][],
      [number, number[]][] | null,
      number[] | null,
  ]

  return buildRows(rowCount, keys, columns, sparse, nullRows, inner)
}

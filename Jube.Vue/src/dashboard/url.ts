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

import type { DirectoryEntry, ParameterDefinition, ParameterValues } from './types'

export function parametersFromQuery(query: Record<string, unknown>): Record<string, string> {
  const found: Record<string, string> = {}

  for (const [name, value] of Object.entries(query)) {
    const first = Array.isArray(value) ? value[0] : value

    if (typeof first === 'string') found[name] = first
  }

  return found
}

export function namedValues(
  definitions: ParameterDefinition[],
  values: ParameterValues,
): Record<string, string> {
  const named: Record<string, string> = {}

  for (const definition of definitions) {
    if (definition.name) named[definition.name] = values[definition.id] ?? ''
  }

  return named
}

export function sameValues(a: Record<string, string>, b: Record<string, string>): boolean {
  const keys = Object.keys(a)

  return keys.length === Object.keys(b).length && keys.every((key) => a[key] === b[key])
}

export function listedEntries(entries: DirectoryEntry[]): DirectoryEntry[] {
  return entries
    .filter((entry) => entry.contextKindId === 1)
    .sort((a, b) => (a.name ?? '').localeCompare(b.name ?? ''))
}

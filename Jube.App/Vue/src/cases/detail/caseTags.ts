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

import type {CaseTag} from './types'

export function tagNames(tags: readonly CaseTag[]): string[] {
  const names: string[] = []

  for (const tag of tags) {
    const name = tag.name ?? ''

    if (name !== '' && !names.includes(name)) names.push(name)
  }

  return names
}

export function remainingChoices(choices: readonly string[], names: readonly string[]): string[] {
  return choices.filter((choice) => choice !== '' && !names.includes(choice))
}

export function withTag(names: readonly string[], name: string): string[] {
  return name === '' || names.includes(name) ? [...names] : [...names, name]
}

export function withoutTag(names: readonly string[], name: string): string[] {
  return names.filter((item) => item !== name)
}

export function sameTags(left: readonly string[], right: readonly string[]): boolean {
  if (left.length !== right.length) return false

  const sorted = [...right].sort()

  return [...left].sort().every((name, index) => name === sorted[index])
}

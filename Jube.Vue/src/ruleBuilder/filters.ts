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

import { operatorsFor } from './ruleText'
import type { BuilderFilter, BuilderOperator, Completion, RuleContext } from './types'

export function showsBuilder(parseType: number): boolean {
  return parseType === 2 || parseType === 3 || parseType === 5
}

export function filtersFromCompletions(
  completions: Completion[],
  operators: BuilderOperator[],
): { filters: BuilderFilter[]; context: RuleContext } {
  const filters: BuilderFilter[] = []
  const fieldTypes: Record<string, string> = {}
  const listChoices: Record<string, string> = {}

  for (const completion of completions) {
    if (completion.dataType === 'string') listChoices[completion.name] = completion.name
  }

  for (const completion of completions) {
    if (filters.some((filter) => filter.id === completion.name)) continue

    const available = operatorsFor(operators, completion.dataType)

    if (available.length === 0) continue

    const filter: BuilderFilter = {
      optgroup: completion.group,
      id: completion.name,
      name: completion.name,
      operators: available,
      value_separator: ',',
      type: 'string',
    }

    switch (completion.dataType) {
      case 'string':
      case 'integer':
      case 'double':
      case 'datetime':
        filter.type = completion.dataType
        break
      case 'boolean':
        filter.input = 'radio'
        filter.default_value = 'True'
        filter.values = { True: 'Yes', False: 'No' }
        break
      case 'list':
        filter.input = 'select'
        filter.values = listChoices
        break
      default:
        continue
    }

    fieldTypes[completion.name] = completion.dataType
    filters.push(filter)
  }

  return { filters, context: { operators, fieldTypes } }
}

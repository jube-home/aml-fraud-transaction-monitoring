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
import type { FormCondition, FormField, RecordValues } from './types'

function same(left: unknown, right: unknown): boolean {
  return String(left).toLowerCase() === String(right).toLowerCase()
}

function empty(value: unknown): boolean {
  return value === null || value === undefined || value === ''
}

export function holds(condition: FormCondition, values: RecordValues): boolean {
  const actual = values[condition.property]

  switch (condition.op) {
    case 'equals':
    case 'in':
      return condition.values.some((candidate) => same(actual, candidate))
    case 'notEquals':
    case 'notIn':
      return !condition.values.some((candidate) => same(actual, candidate))
    case 'greaterThan':
      return Number(actual) > Number(condition.values[0])
    case 'isEmpty':
      return empty(actual)
    case 'isNotEmpty':
      return !empty(actual)
    default:
      return false
  }
}

export function allow(conditions: FormCondition[], values: RecordValues): boolean {
  if (conditions.length === 0) return true

  const any = conditions[0]!.matchMode === 'any'

  return any
    ? conditions.some((condition) => holds(condition, values))
    : conditions.every((condition) => holds(condition, values))
}

export interface FieldState {
  visible: boolean
  required: boolean
  enabled: boolean
}

export function fieldState(field: FormField, values: RecordValues): FieldState {
  return {
    visible: allow(field.visibleWhen, values),
    required: field.requiredWhen.length > 0 && allow(field.requiredWhen, values),
    enabled: allow(field.enabledWhen, values),
  }
}

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
import type { FormField, FormModel, RecordValues } from './types'
import { fromLocalInput, toLocalInput } from '../composables/localDateTime'

export function defaultFor(field: FormField): unknown {
  if (field.newDefault !== null && field.newDefault !== undefined) return field.newDefault

  switch (field.type) {
    case 'boolean':
      return false
    case 'string':
    case 'guid':
      return ''
    case 'json':
      return []
    default:
      return null
  }
}

export function newRecord(model: FormModel): RecordValues {
  const values: RecordValues = {}

  for (const field of model.fields) values[field.name] = defaultFor(field)

  return values
}

export function isLockField(model: FormModel, field: FormField): boolean {
  return model.lockFields.includes(field.name)
}

export function editableFields(model: FormModel): FormField[] {
  return model.fields.filter((field) => !field.readOnly && !isLockField(model, field))
}

export function isLocked(model: FormModel, values: RecordValues): boolean {
  return model.lockFields.some((name) => values[name] === true)
}

function forInput(field: FormField, value: unknown): string {
  if (value === null || value === undefined) return ''
  if (field.type === 'datetime') return toLocalInput(String(value))

  return String(value)
}

export function toInput(field: FormField, value: unknown): string {
  return forInput(field, value)
}

export function fromInput(field: FormField, text: string): unknown {
  switch (field.type) {
    case 'integer':
    case 'number': {
      if (text.trim() === '') return null

      const number = Number(text)

      return Number.isFinite(number) ? number : text
    }
    case 'datetime':
      return fromLocalInput(text)?.toISOString() ?? null
    default:
      return text
  }
}

export function problemsOf(
  model: FormModel,
  values: RecordValues,
): { field: string; code: 'number' }[] {
  return editableFields(model)
    .filter((field) => field.type === 'integer' || field.type === 'number')
    .filter((field) => {
      const value = values[field.name]

      return typeof value === 'string' && value.trim() !== '' && !Number.isFinite(Number(value))
    })
    .map((field) => ({ field: field.name, code: 'number' as const }))
}

const emptyGuid = '00000000-0000-0000-0000-000000000000'

export function wireValue(field: FormField, value: unknown): unknown {
  if (field.nullable) return value === '' && field.type !== 'string' ? null : value

  const blank = value === null || value === undefined || value === ''

  if (!blank) return value

  switch (field.type) {
    case 'guid':
      return emptyGuid
    case 'integer':
    case 'number':
      return 0
    case 'string':
      return ''
    default:
      return value
  }
}

export function payloadOf(
  model: FormModel,
  values: RecordValues,
  stored: RecordValues | undefined,
  parentId: number | string | undefined,
): RecordValues {
  const body: RecordValues = { ...stored }

  for (const field of editableFields(model)) body[field.name] = wireValue(field, values[field.name])

  for (const name of model.lockFields) body[name] = values[name] ?? false

  if (model.keys.parent && parentId !== undefined) body[model.keys.parent] = parentId

  return body
}

export function comparable(model: FormModel, values: RecordValues): string {
  return JSON.stringify(editableFields(model).map((field) => values[field.name] ?? null))
}

export function changedFields(
  model: FormModel,
  before: RecordValues,
  after: RecordValues,
): string[] {
  const same = (left: unknown, right: unknown): boolean =>
    JSON.stringify(left ?? null) === JSON.stringify(right ?? null)

  return model.fields
    .filter((field) => !same(before[field.name], after[field.name]))
    .map((field) => field.name)
}

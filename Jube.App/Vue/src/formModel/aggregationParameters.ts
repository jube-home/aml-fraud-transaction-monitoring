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
import type {FormEditor, FormParameter, FormParameterSet, RecordValues} from './types'

export type ParameterMap = Record<string, number>

export interface ParameterLimit {
  bound: 'atLeast' | 'moreThan' | 'atMost' | 'lessThan'
  value: number
}

export function parameterSetOf(
  editor: FormEditor,
  values: RecordValues,
): FormParameterSet | undefined {
  const selected = Number(values[editor.reads.function ?? ''])

  return (editor.parameterSets ?? []).find((set) => set.value === selected)
}

export function parameterMapOf(editor: FormEditor, values: RecordValues): ParameterMap {
  const stored = values[editor.roles.parameters ?? '']
  const map: ParameterMap = {}

  if (stored === null || typeof stored !== 'object' || Array.isArray(stored)) return map

  for (const [name, value] of Object.entries(stored)) {
    if (typeof value === 'number' && Number.isFinite(value)) map[name] = value
  }

  return map
}

function keyOf(map: ParameterMap, parameter: FormParameter): string | undefined {
  const wanted = parameter.name.toLowerCase()

  return Object.keys(map).find((name) => name.toLowerCase() === wanted)
}

export function parameterValue(map: ParameterMap, parameter: FormParameter): number | null {
  const key = keyOf(map, parameter)

  return key === undefined ? null : map[key]!
}

export function effectiveValue(map: ParameterMap, parameter: FormParameter): number {
  return parameterValue(map, parameter) ?? parameter.default
}

export function withParameter(
  map: ParameterMap,
  parameter: FormParameter,
  value: number | null,
): ParameterMap | null {
  const next: ParameterMap = {...map}
  const key = keyOf(next, parameter)

  if (key !== undefined) delete next[key]
  if (value !== null && value !== parameter.default) next[parameter.name] = value

  return Object.keys(next).length === 0 ? null : next
}

export function prunedFor(
  map: ParameterMap,
  set: FormParameterSet | undefined,
): ParameterMap | null {
  const next: ParameterMap = {}

  for (const parameter of set?.parameters ?? []) {
    const value = parameterValue(map, parameter)

    if (value !== null) next[parameter.name] = value
  }

  return Object.keys(next).length === 0 ? null : next
}

export function sameMap(left: ParameterMap | null, right: ParameterMap | null): boolean {
  const a = Object.entries(left ?? {}).sort(([x], [y]) => x.localeCompare(y))
  const b = Object.entries(right ?? {}).sort(([x], [y]) => x.localeCompare(y))

  return (
    a.length === b.length &&
    a.every(([name, value], at) => b[at]![0] === name && b[at]![1] === value)
  )
}

export function parameterLabel(name: string): string {
  const words = name.replace(/([a-z0-9])([A-Z])/g, '$1 $2').toLowerCase()

  return words.charAt(0).toUpperCase() + words.slice(1)
}

export function limitsOf(parameter: FormParameter): ParameterLimit[] {
  const limits: ParameterLimit[] = []

  if (parameter.kind === 'boolean') return limits

  if (parameter.minimum !== null) {
    limits.push({
      bound: parameter.minimumIsExclusive ? 'moreThan' : 'atLeast',
      value: parameter.minimum,
    })
  }

  if (parameter.maximum !== null && parameter.maximum < 2147483647) {
    limits.push({
      bound: parameter.maximumIsExclusive ? 'lessThan' : 'atMost',
      value: parameter.maximum,
    })
  }

  return limits
}

export function parseTyped(text: string, parameter: FormParameter): number | null | undefined {
  const trimmed = text.trim()

  if (trimmed === '') return null

  const value = Number(trimmed)

  if (!Number.isFinite(value)) return undefined

  return parameter.kind === 'integer' && !Number.isInteger(value) ? undefined : value
}

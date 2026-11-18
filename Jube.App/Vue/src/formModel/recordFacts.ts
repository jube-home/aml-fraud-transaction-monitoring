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

import type {GridSummaryFact} from '../components/gridRowSummaryTypes'
import {featureLayout} from './features'
import type {FormField, FormModel, RecordValues} from './types'

export interface RecordFactFormat {
  yes: string
  no: string
  dateTime: (value: string) => string
}

const longest = 240

export function optionLabel(field: FormField | undefined, value: unknown): string | undefined {
  if (typeof value !== 'string' && typeof value !== 'number') return undefined

  return field?.options.find((candidate) => candidate.value === value)?.label.trim()
}

function factValue(field: FormField, value: unknown, format: RecordFactFormat): string {
  if (value === null || value === undefined || value === '') return ''

  if (typeof value === 'boolean') return value ? format.yes : format.no

  if (typeof value !== 'string' && typeof value !== 'number') return ''

  const option = field.options.find((candidate) => candidate.value === value)

  if (option) return option.label.trim()

  if (field.type === 'datetime') return format.dateTime(String(value))

  const text = String(value)
    .replace(/\p{Cc}/gu, '')
    .trim()

  return text.length > longest ? `${text.slice(0, longest)}…` : text
}

export interface RecordFact extends GridSummaryFact {
  state?: 'on' | 'off'
}

export interface RecordFactSection {
  path: string
  label: string
  facts: RecordFact[]
}

export interface RecordFactGroup {
  path: string
  label: string
  feature?: 'on' | 'off'
  sections: RecordFactSection[]
}

function switchOf(model: FormModel, path: string): FormField | undefined {
  if (path === '') return undefined

  if (model.fields.some((field) => field.group.startsWith(`${path}/`))) return undefined

  const {plain, features} = featureLayout(model.fields.filter((field) => field.group === path))

  return plain.length === 0 && features.length === 1 ? features[0]!.switchField : undefined
}

export function recordFactGroups(
  model: FormModel,
  row: RecordValues,
  format: RecordFactFormat,
  shown?: string[],
): RecordFactGroup[] {
  const listed = new Set(
    shown ?? model.listColumns.filter((column) => !column.hidden).map((column) => column.field),
  )
  const declared = new Map(model.groups.map((group) => [group.path, group]))
  const orderOf = (path: string): number =>
    path === '' ? Number.MIN_SAFE_INTEGER : (declared.get(path)?.order ?? Number.MAX_SAFE_INTEGER)
  const labelOf = (path: string): string =>
    declared.get(path)?.label ?? path.slice(path.lastIndexOf('/') + 1)
  const groups = new Map<string, RecordFactGroup>()
  const switches = new Map<string, string | undefined>()

  const groupFor = (root: string): RecordFactGroup => {
    const existing = groups.get(root)

    if (existing) return existing

    const gate = switchOf(model, root)
    const group: RecordFactGroup = {path: root, label: labelOf(root), sections: []}

    if (gate) group.feature = row[gate.name] === true ? 'on' : 'off'

    switches.set(root, gate?.name)
    groups.set(root, group)

    return group
  }

  for (const field of [...model.fields].sort((a, b) => a.order - b.order)) {
    const parts = field.group === '' ? [] : field.group.split('/')
    const root = parts[0] ?? ''
    const group = groupFor(root)

    if (group.feature === 'off' || switches.get(root) === field.name) continue

    if (listed.has(field.name) || field.type === 'json') continue

    const raw = row[field.name]
    const value = factValue(field, raw, format)

    if (value === '') continue

    let section = group.sections.find((candidate) => candidate.path === field.group)

    if (!section) {
      section = {
        path: field.group,
        label: parts
          .slice(1)
          .map((_, index) => labelOf(parts.slice(0, index + 2).join('/')))
          .join(' / '),
        facts: [],
      }
      group.sections.push(section)
    }

    section.facts.push({
      label: field.label,
      value,
      mono: field.type === 'integer' || field.type === 'number' || field.type === 'guid',
      ...(typeof raw === 'boolean' ? {state: raw ? ('on' as const) : ('off' as const)} : {}),
    })
  }

  const ordered = [...groups.values()]
    .filter((group) => group.feature !== undefined || group.sections.length > 0)
    .sort((a, b) => orderOf(a.path) - orderOf(b.path))

  for (const group of ordered)
    group.sections.sort(
      (a, b) =>
        Number(a.path !== group.path) - Number(b.path !== group.path) ||
        orderOf(a.path) - orderOf(b.path),
    )

  return ordered
}

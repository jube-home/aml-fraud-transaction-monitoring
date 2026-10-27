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

import type { FormChild, FormModel, RecordValues } from './types'

export interface OpenedRecord {
  id: number
  guid: string | undefined
  name: string
  values: RecordValues
}

export interface Level {
  key: string
  area: string
  label: string
  parentValue: number | string | undefined
  listRoute: string | undefined
  initialId: number | undefined
  startNew: boolean
  record: OpenedRecord | undefined
}

export interface PathItem {
  area: string
  id: number
}

export function humanise(name: string): string {
  const words = name.replace(/([a-z0-9])([A-Z])/g, '$1 $2').toLowerCase()

  return `${words.charAt(0).toUpperCase()}${words.slice(1)}`
}

export function rootAreas(models: FormModel[]): string[] {
  const children = new Set(
    models.flatMap((model) => (model.children ?? []).map((child) => child.area)),
  )

  return models
    .filter((model) => !children.has(model.area) && model.operations.includes('list'))
    .sort((a, b) => b.children.length - a.children.length || a.area.localeCompare(b.area))
    .map((model) => model.area)
}

export function hubsOf(children: FormChild[]): { hub: string; children: FormChild[] }[] {
  const groups: { hub: string; children: FormChild[] }[] = []

  for (const child of [...children].sort((a, b) => a.order - b.order)) {
    const found = child.hub === '' ? undefined : groups.find((group) => group.hub === child.hub)

    if (found) found.children.push(child)
    else groups.push({ hub: child.hub, children: [child] })
  }

  return groups
}

export function recordName(model: FormModel, values: RecordValues, id: number): string {
  const natural = model.keys.naturalKey
  const name = natural ? values[natural] : undefined

  return typeof name === 'string' && name !== '' ? name : `#${id}`
}

export function parentValueOf(
  child: FormChild,
  model: FormModel,
  record: OpenedRecord,
): number | string | undefined {
  if (child.parentField === 'guid') return record.guid

  return record.values[model.keys.id] as number | undefined
}

export function newLevel(
  area: string,
  label: string,
  options: Partial<Pick<Level, 'parentValue' | 'listRoute' | 'initialId' | 'startNew'>> = {},
): Level {
  return {
    key: `${area}:${options.parentValue ?? 'root'}`,
    area,
    label,
    parentValue: options.parentValue,
    listRoute: options.listRoute,
    initialId: options.initialId,
    startNew: options.startNew ?? false,
    record: undefined,
  }
}

export function encodePath(levels: Level[]): string {
  return levels
    .filter((level) => level.record !== undefined)
    .map((level) => `${level.area}:${level.record!.id}`)
    .join(',')
}

export function decodePath(text: string | undefined): PathItem[] {
  if (!text) return []

  return text.split(',').flatMap((part): PathItem[] => {
    const [area, id] = part.split(':')
    const number = Number(id)

    return area && Number.isInteger(number) && number > 0 ? [{ area, id: number }] : []
  })
}

export interface ChildSummary {
  total: number
  active: number | null
  locked: number | null
}

export function summariseChildren(
  model: FormModel | undefined,
  rows: RecordValues[],
): ChildSummary {
  const hasActive = model?.fields.some(
    (field) => field.name === 'active' && field.type === 'boolean',
  )
  const lockName = model?.lockFields[0]

  return {
    total: rows.length,
    active: hasActive ? rows.filter((row) => row.active === true).length : null,
    locked: lockName ? rows.filter((row) => row[lockName] === true).length : null,
  }
}

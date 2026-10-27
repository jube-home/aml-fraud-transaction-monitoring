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

import type { GridBadge, GridColumn } from '../components/dataGridTypes'
import {
  cardState,
  dataType,
  roleAccepts,
  templateKindId,
  type CardGrid,
  type ChartKind,
  type ChartRole,
  type DatasourceCard,
  type Introspection,
  type IntrospectionColumn,
  type MappingEntry,
  type ResolvedRole,
  type SqlState,
} from './types'

export interface GridLabels {
  yes: string
  no: string
  number: (value: number) => string
  dateTime: (value: string) => string
}

export function acceptsColumn(acceptsId: number, dataTypeId: number): boolean {
  const numeric =
    dataTypeId === dataType.integer ||
    dataTypeId === dataType.decimal ||
    dataTypeId === dataType.flag
  const time = dataTypeId === dataType.dateTime

  if (acceptsId === roleAccepts.numeric) return numeric
  if (acceptsId === roleAccepts.time) return time
  if (acceptsId === roleAccepts.numericOrTime) return numeric || time

  return true
}

export function normaliseMapping(mapping: MappingEntry[]): MappingEntry[] {
  return mapping
    .filter((entry) => entry.columns.length > 0)
    .map((entry) => ({ roleId: entry.roleId, columns: [...entry.columns] }))
    .sort((a, b) => a.roleId - b.roleId)
}

export function roleColumns(mapping: MappingEntry[], roleId: number): string[] {
  return mapping.find((entry) => entry.roleId === roleId)?.columns ?? []
}

export function assignRole(
  mapping: MappingEntry[],
  roleId: number,
  columns: string[],
): MappingEntry[] {
  const taken = new Set(columns)
  const others = mapping
    .filter((entry) => entry.roleId !== roleId)
    .map((entry) => ({
      roleId: entry.roleId,
      columns: entry.columns.filter((column) => !taken.has(column)),
    }))

  return normaliseMapping([...others, { roleId, columns }])
}

export function dropMissingColumns(mapping: MappingEntry[], names: string[]): MappingEntry[] {
  const known = new Set(names)

  return normaliseMapping(
    mapping.map((entry) => ({
      roleId: entry.roleId,
      columns: entry.columns.filter((column) => known.has(column)),
    })),
  )
}

export function sameMapping(a: MappingEntry[], b: MappingEntry[]): boolean {
  return JSON.stringify(normaliseMapping(a)) === JSON.stringify(normaliseMapping(b))
}

export function resolvedColumns(resolved: ResolvedRole[], roleId: number): string[] {
  return resolved.find((role) => role.roleId === roleId)?.columns ?? []
}

export function choices(role: ChartRole, columns: IntrospectionColumn[]): IntrospectionColumn[] {
  return columns.filter((column) => acceptsColumn(role.acceptsId, column.dataTypeId))
}

export interface ColumnRole {
  name: string
  explicit: boolean
}

export function columnRoles(
  kind: ChartKind | undefined,
  resolved: ResolvedRole[],
): Map<string, ColumnRole> {
  const byColumn = new Map<string, ColumnRole>()

  if (!kind) return byColumn

  for (const role of resolved) {
    const name = kind.roles.find((candidate) => candidate.roleId === role.roleId)?.name

    if (!name) continue

    for (const column of role.columns) {
      if (!byColumn.has(column)) byColumn.set(column, { name, explicit: role.explicit })
    }
  }

  return byColumn
}

export function fitOf(introspection: Introspection | undefined, kindId: number) {
  return introspection?.kinds.find((fit) => fit.kindId === kindId)
}

export function sqlState(input: {
  command: string
  settled: string
  fetching: boolean
  failed: boolean
  introspection: Introspection | undefined
}): SqlState {
  if (!input.command.trim()) return 'incomplete'
  if (input.command !== input.settled) return 'checking'
  if (input.failed) return 'disconnected'
  if (input.fetching && !input.introspection) return 'checking'
  if (!input.introspection) return 'checking'

  return input.introspection.valid ? 'valid' : 'errors'
}

export function isValid(input: {
  state: SqlState
  introspection: Introspection | undefined
  displayKindId: number
  includeGrid: boolean
  templateText?: string
}): boolean {
  const template = input.displayKindId === templateKindId

  return (
    input.state === 'valid' &&
    !input.introspection?.mappingError &&
    !(template && (!input.templateText?.trim() || input.introspection?.templateError)) &&
    (input.displayKindId > 0 || input.includeGrid)
  )
}

function flagBadge(value: unknown, labels: GridLabels): GridBadge {
  const on = value === true || value === 1

  return { label: on ? labels.yes : labels.no, tone: on ? 'success' : 'neutral' }
}

export function gridColumns(grid: CardGrid, labels: GridLabels): GridColumn[] {
  return grid.columns.map((column): GridColumn => {
    const base: GridColumn = { field: column.name, title: column.name }

    switch (column.dataTypeId) {
      case dataType.integer:
      case dataType.decimal:
        return {
          ...base,
          align: 'end',
          mono: true,
          type: 'number',
          format: (value) => (typeof value === 'number' ? labels.number(value) : ''),
        }
      case dataType.dateTime:
        return {
          ...base,
          mono: true,
          type: 'date',
          format: (value) => (typeof value === 'string' ? labels.dateTime(value) : ''),
        }
      case dataType.flag:
        return { ...base, align: 'center', badge: (value) => flagBadge(value, labels) }
      case dataType.decimalArray:
      case dataType.array:
        return {
          ...base,
          mono: true,
          format: (value) => (Array.isArray(value) ? value.join(', ') : ''),
        }
      default:
        return base
    }
  })
}

export function gridRows(grid: CardGrid): Record<string, unknown>[] {
  return grid.rows.map((row) =>
    Object.fromEntries(grid.columns.map((column, index) => [column.name, row[index] ?? null])),
  )
}

export function cardReason(card: DatasourceCard | undefined): string | null {
  return card && card.stateId !== cardState.ready ? card.message : null
}

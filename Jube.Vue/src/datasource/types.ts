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

export const cardState = { ready: 1, empty: 2, error: 3 } as const

export const styleMode = { external: 1, inline: 2 } as const

export const themeMode = { auto: 1, light: 2, dark: 3 } as const

export const roleAccepts = { any: 1, numeric: 2, time: 3, numericOrTime: 4 } as const

export const markupKind = { svg: 1, html: 2 } as const

export const templateKindId = 29

export const dataType = {
  text: 1,
  integer: 2,
  decimal: 3,
  dateTime: 4,
  flag: 5,
  decimalArray: 6,
  array: 7,
} as const

export interface MappingEntry {
  roleId: number
  columns: string[]
}

export interface IntrospectionColumn {
  name: string
  ordinal: number
  dataTypeName: string
  dataTypeId: number
}

export interface KindFit {
  kindId: number
  fitsByPosition: boolean
  reason: string | null
}

export interface ResolvedRole {
  roleId: number
  columns: string[]
  explicit: boolean
}

export interface Introspection {
  valid: boolean
  columns: IntrospectionColumn[]
  error: string | null
  errorLine: number | null
  errorColumn: number | null
  kinds: KindFit[]
  resolvedMapping: ResolvedRole[]
  mappingError: string | null
  templateError: string | null
}

export interface ChartRole {
  roleId: number
  name: string
  description: string
  many: boolean
  optional: boolean
  acceptsId: number
}

export interface ChartKind {
  kindId: number
  name: string
  description: string
  roles: ChartRole[]
}

export interface CardParameter {
  id: number
  value: string | null
}

export interface CardGridColumn {
  name: string
  ordinal: number
  dataTypeId: number
}

export interface CardGrid {
  columns: CardGridColumn[]
  rows: unknown[][]
}

export interface CardDisplay {
  kindId: number
  markupKindId: number
  markup: string
}

export interface CardStyle {
  modeId: number
  version: string
  css: string | null
}

export interface DatasourceCard {
  id: number
  guid: string
  name: string
  columnSpan: number
  rowSpan: number
  priority: number
  stateId: number
  message: string | null
  rowCount: number
  truncated: boolean
  executionMilliseconds: number
  display: CardDisplay | null
  grid: CardGrid | null
  style: CardStyle
}

export interface CardRequest {
  parameters: CardParameter[]
  themeId: number
  styleModeId: number
  knownStylesheetVersion: string | null
  width: number
  height: number
  contextId?: number
}

export interface DatasourceValue {
  command: string
  displayKindId: number
  includeGrid: boolean
  mapping: MappingEntry[]
  templateText: string
  valid: boolean
}

export type SqlState = 'incomplete' | 'checking' | 'valid' | 'errors' | 'disconnected'

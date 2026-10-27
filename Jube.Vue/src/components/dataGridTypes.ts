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

export type GridTone = 'neutral' | 'info' | 'success' | 'warning' | 'error'

export type GridDirection = 'ltr' | 'rtl'

export interface GridBadge {
  label: string
  tone: GridTone
}

export type GridActionTone = 'neutral' | 'primary' | 'success' | 'error'

export interface GridAction {
  id: string
  label: string
  tone?: GridActionTone
  disabled?: boolean
}

export interface GridInput {
  placeholder: string
  label: string
  value?: string
  prefix?: string
  type?: 'text' | 'number' | 'datetime-local'
}

export interface GridActionEvent {
  id: string
  row: object
}

export interface GridInputEvent {
  field: string
  value: string
  row: object
}

export interface GridPaint {
  fore?: string
  back?: string
}

export interface GridRowPaint extends GridPaint {
  bold?: boolean
  title?: string
}

export interface GridColumn {
  field: string
  title: string
  hiddenTitle?: string
  align?: 'start' | 'center' | 'end'
  mono?: boolean
  width?: number
  minWidth?: number
  pinned?: boolean
  type?: 'text' | 'number' | 'date'
  format?: (value: unknown) => string
  badge?: (value: unknown) => GridBadge
  actions?: (row: object) => GridAction[]
  draw?: (row: object) => Element
  input?: (row: object) => GridInput | null
  paint?: (row: object) => GridPaint | null
}

export interface GridSort {
  field: string
  dir: 'asc' | 'desc'
}

export interface GridPagerLabels {
  navigation: string
  first: string
  previous: string
  next: string
  last: string
  pageSize: string
  summary: (from: number, to: number, total: number) => string
  page: (page: number, pages: number) => string
}

export interface GridExpandLabels {
  expand: string
  collapse: string
}

export interface GridExpandEvent {
  id: unknown
  row: object
  host: HTMLElement | undefined
}

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

import type { FormOption } from '../formModel/types'
import type { PayloadStatistics } from '../statistics/model'

export type GridFormat =
  'default' | 'integer' | 'decimal' | 'percent' | 'dateTime' | 'date' | 'code'

export interface GridModelColumn {
  field: string
  title: string
  description: string
  type: 'string' | 'integer' | 'number' | 'boolean' | 'datetime' | 'guid'
  format: GridFormat
  unit: string | null
  order: number
  sortable: boolean
  hidden: boolean
  numerator: string | null
  denominator: string | null
}

export interface GridModelFilter {
  param: string
  label: string
  kind: 'select' | 'lookup' | 'text'
  options: FormOption[]
  lookup: string | null
}

export interface GridModel {
  area: string
  endpoint: string
  title: string
  snapshot: boolean
  sortField: string
  sortDirection: 'asc' | 'desc'
  take: number
  timeField: string | null
  windowMinutes: number
  search: GridModelFilter | null
  filters: GridModelFilter[]
  columns: GridModelColumn[]
}

export interface GridState {
  from: string
  to: string
  filters: Record<string, string>
  search: string
  sort: { field: string; dir: 'asc' | 'desc' } | undefined
}

export interface PayloadResult {
  rows: Record<string, unknown>[]
  total: number
  statistics: PayloadStatistics | null
}

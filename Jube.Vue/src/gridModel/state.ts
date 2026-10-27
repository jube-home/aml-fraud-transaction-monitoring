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

import { toLocalInput } from '../composables/localDateTime'
import type { GridModel, GridModelColumn, GridState } from './types'

export function defaultState(model: GridModel, now: Date = new Date()): GridState {
  return {
    from: toLocalInput(new Date(now.getTime() - model.windowMinutes * 60_000)),
    to: toLocalInput(now),
    filters: {},
    search: '',
    sort: model.sortField ? { field: model.sortField, dir: model.sortDirection } : undefined,
  }
}

export function requestParams(model: GridModel, state: GridState): Record<string, string | number> {
  const params: Record<string, string | number> = {}

  if (state.sort) {
    params.sortField = state.sort.field
    params.sortDirection = state.sort.dir
  }

  if (!model.snapshot) {
    params.take = model.take

    if (model.timeField) {
      const from = new Date(state.from)
      const to = new Date(state.to)

      if (!Number.isNaN(from.getTime())) params.from = from.toISOString()
      if (!Number.isNaN(to.getTime())) params.to = to.toISOString()
    }
  }

  for (const filter of model.filters) {
    const value = state.filters[filter.param]

    if (value) params[filter.param] = value
  }

  if (model.search && state.search.trim() !== '') params[model.search.param] = state.search.trim()

  return params
}

export function ratioOf(row: Record<string, unknown>, column: GridModelColumn): number {
  const top = Number(row[column.numerator!])
  const bottom = Number(row[column.denominator!])

  return Number.isFinite(top) && Number.isFinite(bottom) && bottom !== 0 ? top / bottom : 0
}

export function withRatios(
  rows: Record<string, unknown>[],
  columns: GridModelColumn[],
): Record<string, unknown>[] {
  const ratios = columns.filter((column) => column.numerator !== null)

  if (ratios.length === 0) return rows

  return rows.map((row) => {
    const next = { ...row }

    for (const column of ratios) next[column.field] = ratioOf(row, column)

    return next
  })
}

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
import { describe, expect, it } from 'vitest'
import { toLocalInput } from '../../composables/localDateTime'
import { defaultState, ratioOf, requestParams, withRatios } from '../state'
import type { GridModel, GridModelColumn } from '../types'

const column = (overrides: Partial<GridModelColumn>): GridModelColumn => ({
  field: 'x',
  title: 'X',
  description: '',
  type: 'integer',
  format: 'integer',
  unit: null,
  order: 10,
  sortable: true,
  hidden: false,
  numerator: null,
  denominator: null,
  ...overrides,
})

const model = (overrides: Partial<GridModel> = {}): GridModel => ({
  area: 'Thing',
  endpoint: '/api/Thing',
  title: 'Thing',
  snapshot: false,
  sortField: 'createdDate',
  sortDirection: 'desc',
  take: 2000,
  timeField: 'createdDate',
  windowMinutes: 60,
  search: { param: 'search', label: 'Search', kind: 'text', options: [], lookup: null },
  filters: [{ param: 'stageId', label: 'Stage', kind: 'select', options: [], lookup: null }],
  columns: [],
  ...overrides,
})

describe('grid state', () => {
  it('starts the window an hour back from now, in the form the date inputs take', () => {
    const now = new Date(2026, 9, 21, 14, 30)
    const state = defaultState(model(), now)

    expect(state.to).toBe('2026-10-21T14:30')
    expect(state.from).toBe('2026-10-21T13:30')
    expect(state.sort).toEqual({ field: 'createdDate', dir: 'desc' })
    expect(toLocalInput(new Date(2026, 0, 5, 3, 7))).toBe('2026-01-05T03:07')
  })

  it('sends the window as UTC, the filters that are set, the search and the sort', () => {
    const state = {
      ...defaultState(model(), new Date(2026, 9, 21, 14, 30)),
      filters: { stageId: '3' },
      search: '  deadlock ',
    }

    expect(requestParams(model(), state)).toEqual({
      sortField: 'createdDate',
      sortDirection: 'desc',
      take: 2000,
      from: new Date(2026, 9, 21, 13, 30).toISOString(),
      to: new Date(2026, 9, 21, 14, 30).toISOString(),
      stageId: '3',
      search: 'deadlock',
    })
  })

  it('leaves out empty filters, an empty search, and the window and limit of a snapshot', () => {
    const snapshot = model({ snapshot: true, timeField: null })

    expect(
      requestParams(snapshot, { ...defaultState(snapshot), filters: { stageId: '' } }),
    ).toEqual({
      sortField: 'createdDate',
      sortDirection: 'desc',
    })
  })

  it('works a ratio per row and treats a missing or zero divisor as zero', () => {
    const average = column({ field: 'average', numerator: 'total', denominator: 'count' })

    expect(ratioOf({ total: 600, count: 3 }, average)).toBe(200)
    expect(ratioOf({ total: 600, count: 0 }, average)).toBe(0)
    expect(ratioOf({ total: 'x', count: 3 }, average)).toBe(0)
    expect(withRatios([{ total: 10, count: 4 }], [average])[0]).toEqual({
      total: 10,
      count: 4,
      average: 2.5,
    })
    expect(withRatios([{ total: 10 }], [column({})])).toEqual([{ total: 10 }])
  })
})

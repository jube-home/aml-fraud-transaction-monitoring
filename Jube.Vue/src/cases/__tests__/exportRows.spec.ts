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
import { exportColumns, exportName, flattenRow, toCsv } from '../exportRows'
import { row } from './fixtures'

const t = (key: string) => key

describe('exportRows', () => {
  it('has a cell for every column and no cell without a column', () => {
    const flat = flattenRow(row({ caseId: 9041 }), t)
    const fields = exportColumns(t).map((column) => column.field)

    expect(Object.keys(flat).sort()).toEqual([...fields].sort())
  })

  it('flattens the nested risk, attention and service level into plain values', () => {
    const flat = flattenRow(row({ caseId: 9041 }), t)

    expect(flat.case).toBe('C-9041')
    expect(typeof flat.priority).toBe('number')
    expect(typeof flat.why).toBe('string')
    expect(Object.values(flat).some((value) => typeof value === 'object' && value !== null)).toBe(
      false,
    )
  })

  it('writes a byte order mark, a header and quotes anything that needs it', () => {
    const csv = toCsv(
      [
        { title: 'Name', field: 'name', type: 'text' },
        { title: 'Count', field: 'count', type: 'number' },
        { title: 'When', field: 'when', type: 'date' },
      ],
      [{ name: 'He said "hi", twice', count: 3, when: '2026-10-24T09:30:00Z' }, { name: null }],
    )

    expect(csv.startsWith('﻿Name,Count,When\r\n')).toBe(true)
    expect(csv).toContain('"He said ""hi"", twice",3,2026-10-24T09:30:00.000Z\r\n')
    expect(csv.endsWith(',,\r\n')).toBe(true)
  })

  it('names the file after the day it was taken', () => {
    expect(exportName('csv', new Date('2026-10-24T23:59:00Z'))).toBe('cases-2026-10-24.csv')
  })
})

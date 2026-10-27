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
import {
  cardParameters,
  gridStyle,
  initialValue,
  initialValues,
  missingRequired,
  orderedTiles,
  tileSize,
  tileStyle,
  wireValue,
} from '../state'
import { parameterType, type ParameterDefinition, type VisualisationRegistry } from '../types'

const definition = (
  id: number,
  dataTypeId: number,
  defaultValue: string | null,
  required = false,
): ParameterDefinition => ({ id, name: `P${id}`, dataTypeId, defaultValue, required })

const registry: VisualisationRegistry = {
  id: 1,
  guid: 'g',
  name: 'Sales',
  columns: 3,
  columnWidth: 300,
  rowHeight: 200,
}

describe('initialValue', () => {
  const now = new Date(2026, 9, 22, 10, 30)

  it('uses the default text for text and numbers', () => {
    expect(initialValue(definition(1, parameterType.text, 'abc'), now)).toBe('abc')
    expect(initialValue(definition(2, parameterType.integer, '7'), now)).toBe('7')
    expect(initialValue(definition(3, parameterType.decimal, null), now)).toBe('')
  })

  it('counts a date default back in days, as the server does when nothing is supplied', () => {
    expect(initialValue(definition(4, parameterType.dateTime, '2'), now)).toBe('2026-10-20T10:30')
    expect(initialValue(definition(4, parameterType.dateTime, null), now)).toBe('2026-10-22T10:30')
  })

  it('reads a flag default of 1 or true as on', () => {
    expect(initialValue(definition(5, parameterType.flag, '1'), now)).toBe('1')
    expect(initialValue(definition(5, parameterType.flag, 'True'), now)).toBe('1')
    expect(initialValue(definition(5, parameterType.flag, '0'), now)).toBe('0')
  })
})

describe('initialValues', () => {
  it('lets a host override a value by parameter name', () => {
    const values = initialValues(
      [definition(1, parameterType.text, 'a'), definition(2, parameterType.text, 'b')],
      new Date(),
      { P2: 'host' },
    )

    expect(values).toEqual({ 1: 'a', 2: 'host' })
  })
})

describe('missingRequired', () => {
  it('lists required parameters left blank but never a flag', () => {
    const definitions = [
      definition(1, parameterType.text, '', true),
      definition(2, parameterType.text, '', false),
      definition(3, parameterType.flag, '0', true),
      definition(4, parameterType.integer, '5', true),
    ]

    expect(missingRequired(definitions, { 1: ' ', 2: '', 3: '0', 4: '5' })).toEqual([1])
  })
})

describe('wireValue and cardParameters', () => {
  it('sends a date as an ISO instant and everything else as typed', () => {
    const when = definition(1, parameterType.dateTime, '0')

    expect(wireValue(when, '2026-10-22T10:30')).toBe(new Date('2026-10-22T10:30').toISOString())
    expect(wireValue(when, '')).toBe('')
    expect(wireValue(definition(2, parameterType.text, ''), 'x')).toBe('x')
  })

  it('pairs each definition with its value', () => {
    expect(
      cardParameters(
        [definition(1, parameterType.text, ''), definition(2, parameterType.flag, '')],
        {
          1: 'a',
          2: '1',
        },
      ),
    ).toEqual([
      { id: 1, value: 'a' },
      { id: 2, value: '1' },
    ])
  })
})

describe('layout', () => {
  it('orders tiles by priority then id', () => {
    const tile = (id: number, priority: number) => ({
      id,
      name: null,
      columnSpan: 1,
      rowSpan: 1,
      priority,
    })

    expect(orderedTiles([tile(3, 2), tile(2, 1), tile(1, 2)]).map((t) => t.id)).toEqual([2, 1, 3])
  })

  it('turns the registry into grid variables with sane minimums', () => {
    expect(gridStyle(registry)).toEqual({
      '--dash-columns': 'repeat(3, minmax(0, 300px))',
      '--dash-rows': '200px',
    })
    expect(gridStyle({ ...registry, columns: 0, columnWidth: 0, rowHeight: 0 })).toEqual({
      '--dash-columns': 'repeat(1, minmax(0, 120px))',
      '--dash-rows': '80px',
    })
  })

  it('spans a tile but never wider than the grid, and sizes it for the server', () => {
    const tile = { id: 1, name: 'x', columnSpan: 5, rowSpan: 2, priority: 0 }

    expect(tileStyle(registry, tile)).toEqual({
      '--tile-columns': '3',
      '--tile-rows': '2',
      '--tile-height': '400px',
    })
    expect(tileSize(registry, tile)).toEqual({ width: 1500, height: 400 })
    expect(tileSize(registry, { ...tile, columnSpan: 0, rowSpan: 0 })).toEqual({
      width: 300,
      height: 200,
    })
  })
})

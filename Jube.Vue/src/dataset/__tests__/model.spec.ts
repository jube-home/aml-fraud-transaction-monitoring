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
  canInclude,
  confirmBody,
  initialMapping,
  kindOfFile,
  mappingProblems,
  parseReference,
  pickable,
  reference,
  type ColumnMapping,
  type DatasetColumn,
} from '../model'

const column = (overrides: Partial<DatasetColumn> = {}): DatasetColumn => ({
  index: 0,
  header: 'Amount',
  kind: 'Numeric',
  missingCount: 0,
  min: 0,
  max: 1,
  isCategorical: false,
  include: true,
  name: 'Amount',
  processingTypeId: 1,
  distinctValues: [],
  ...overrides,
})

const mapped = (overrides: Partial<ColumnMapping> = {}): ColumnMapping => ({
  index: 0,
  include: true,
  name: 'Amount',
  processingTypeId: 1,
  ...overrides,
})

describe('reference', () => {
  it('round trips a dataset through its picker value', () => {
    expect(reference({ kind: 'Json', id: 7 })).toBe('Json:7')
    expect(parseReference('Json:7')).toEqual({ kind: 'Json', id: 7 })
  })

  it.each(['', 'Csv', 'Xml:1', 'Csv:0', 'Csv:x', 'Csv:1.5'])('rejects %j', (value) => {
    expect(parseReference(value)).toBeNull()
  })
})

describe('kindOfFile', () => {
  it('sends JSON files to the JSON store and everything else to the CSV store', () => {
    expect(kindOfFile('rows.json')).toBe('Json')
    expect(kindOfFile('ROWS.JSON')).toBe('Json')
    expect(kindOfFile('rows.csv')).toBe('Csv')
    expect(kindOfFile('rows.txt')).toBe('Csv')
  })
})

describe('canInclude', () => {
  it.each([
    ['Numeric', false, true],
    ['Text', true, true],
    ['Mixed', true, true],
    ['Text', false, false],
    ['Mixed', false, false],
    ['Empty', true, false],
    [null, false, false],
  ] as const)('%s categorical=%s gives %s', (kind, isCategorical, expected) => {
    expect(canInclude({ kind, isCategorical })).toBe(expected)
  })
})

describe('initialMapping', () => {
  it('starts from what the server inferred, defaulting a missing type to Payload', () => {
    expect(initialMapping([column({ processingTypeId: 0, name: null, include: false })])).toEqual([
      { index: 0, include: false, name: '', processingTypeId: 1 },
    ])
  })
})

describe('mappingProblems', () => {
  it('accepts a sound mapping', () => {
    expect(mappingProblems([mapped(), mapped({ index: 1, name: 'Risk_2' })])).toEqual([])
  })

  it('asks for at least one included column', () => {
    expect(mappingProblems([mapped({ include: false })])).toEqual([{ code: 'none' }])
    expect(mappingProblems([])).toEqual([{ code: 'none' }])
  })

  it.each(['', '1abc', 'has space', 'dash-ed', 'ünï'])('rejects the name %j', (name) => {
    expect(mappingProblems([mapped({ name })])).toEqual([{ code: 'name', index: 0 }])
  })

  it('ignores the name of a column that is left out', () => {
    expect(mappingProblems([mapped(), mapped({ index: 1, include: false, name: '!' })])).toEqual([])
  })

  it('reports a repeated name whatever its case, on the later column', () => {
    expect(mappingProblems([mapped(), mapped({ index: 1, name: 'AMOUNT' })])).toEqual([
      { code: 'duplicate', index: 1 },
    ])
  })
})

describe('confirmBody', () => {
  it('sends the mapping with numeric processing types', () => {
    expect(
      confirmBody({ id: 4 }, [mapped({ processingTypeId: '3' as unknown as number })]),
    ).toEqual({
      id: 4,
      columns: [{ index: 0, include: true, name: 'Amount', processingTypeId: 3 }],
    })
  })
})

describe('pickable', () => {
  it('offers only confirmed datasets', () => {
    expect(
      pickable([
        { id: 1, name: 'a', kind: 'Csv', statusId: 0 },
        { id: 2, name: 'b', kind: 'Csv', statusId: 1 },
      ]).map((dataset) => dataset.id),
    ).toEqual([2])
  })
})

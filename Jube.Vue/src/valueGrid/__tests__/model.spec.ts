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
  buildRows,
  draftOf,
  draftProblems,
  isVisible,
  matches,
  payloadOf,
  readPath,
  stateOf,
  valueConfigOf,
  type ValueRecord,
  type ValueStatus,
} from '../model'

const dictionary = valueConfigOf({
  area: 'EntityAnalysisModelDictionaryKvp',
  endpoint: '/api/EntityAnalysisModelDictionaryKvp',
  parentField: 'entityAnalysisModelDictionaryId',
  parentKind: 4,
  fields: [
    { field: 'kvpKey', title: 'Key', type: 'string' },
    { field: 'kvpValue', title: 'Value', type: 'number' },
  ],
})
const list = valueConfigOf({
  area: 'EntityAnalysisModelListValue',
  endpoint: '/api/EntityAnalysisModelListValue',
  parentField: 'entityAnalysisModelListId',
  parentKind: 2,
  fields: [{ field: 'listValue', title: 'Value', type: 'string' }],
})
const fields = dictionary.valueFields

const record = (overrides: Partial<ValueRecord> = {}): ValueRecord => ({
  id: 1,
  kvpKey: 'a',
  kvpValue: 2,
  deleteExpiryDate: null,
  deletedUser: null,
  ...overrides,
})

const status = (overrides: Partial<ValueStatus> = {}): ValueStatus => ({
  kind: 5,
  entityId: 1,
  currentVersion: 3,
  effectiveVersion: 2,
  pending: false,
  rejected: false,
  canApprove: false,
  makerUser: 'maker',
  approvalsRecorded: 0,
  approvalsRequired: 1,
  note: null,
  returnedNote: null,
  ...overrides,
})

describe('stateOf', () => {
  it.each([
    [{}, false, false, 'approved'],
    [{ pending: true }, false, false, 'awaiting'],
    [{ pending: true }, true, false, 'deletionAwaiting'],
    [{ rejected: true }, false, false, 'rejected'],
    [{ rejected: true }, true, false, 'deletionRejected'],
    [{}, false, true, 'unavailable'],
  ] as const)('%j deleted=%s unavailable=%s is %s', (overrides, deleted, unavailable, expected) => {
    expect(stateOf({ deletedUser: deleted ? 'x' : null }, status(overrides), unavailable)).toBe(
      expected,
    )
  })
})

describe('isVisible', () => {
  it('shows live rows and hides a deleted row once it is resolved', () => {
    expect(isVisible({ deletedUser: null }, undefined)).toBe(true)
    expect(isVisible({ deletedUser: 'x' }, undefined)).toBe(false)
    expect(isVisible({ deletedUser: 'x' }, status())).toBe(false)
  })

  it('keeps a deleted row listed while its deletion is pending or rejected', () => {
    expect(isVisible({ deletedUser: 'x' }, status({ pending: true }))).toBe(true)
    expect(isVisible({ deletedUser: 'x' }, status({ rejected: true }))).toBe(true)
  })
})

describe('buildRows', () => {
  it('drops resolved deleted rows and carries the status onto the row', () => {
    const rows = buildRows(
      [record(), record({ id: 2, deletedUser: 'x' })],
      [status({ pending: true, canApprove: true, approvalsRecorded: 1 })],
      false,
      null,
      fields,
    )

    expect(rows).toHaveLength(1)
    expect(rows[0]).toMatchObject({
      state: 'awaiting',
      progress: '1 of 1',
      maker: 'maker',
      canApprove: true,
      hasBaseline: true,
    })
  })

  it('keeps every row when the status is unavailable', () => {
    const rows = buildRows([record({ id: 2, deletedUser: 'x' })], [], true, null, fields)

    expect(rows[0]!.state).toBe('unavailable')
  })

  it('prepends a blank editing row for a new value and flags the row being edited', () => {
    const adding = buildRows([record()], [], false, { id: 'new' }, fields)

    expect(adding[0]).toMatchObject({ isNew: true, editing: true, kvpKey: '' })
    expect(adding).toHaveLength(2)

    const editing = buildRows([record()], [], false, { id: 1 }, fields)

    expect(editing[0]!.editing).toBe(true)
  })
})

describe('draft and payload', () => {
  it('builds a draft from a row, showing the expiry in local time to the minute', () => {
    const [row] = buildRows(
      [record({ deleteExpiryDate: new Date('2026-10-20T10:30:45').toISOString() })],
      [],
      false,
      null,
      fields,
    )

    expect(draftOf(row!, fields)).toEqual({
      deleteExpiryDate: '2026-10-20T10:30',
      kvpKey: 'a',
      kvpValue: '2',
    })
  })

  it('sends numbers as numbers, blanks as null and the expiry as UTC', () => {
    expect(
      payloadOf(
        dictionary,
        9,
        { kvpKey: 'k', kvpValue: '1.5', deleteExpiryDate: '2026-10-20T10:30' },
        4,
      ),
    ).toEqual({
      id: 4,
      entityAnalysisModelDictionaryId: 9,
      kvpKey: 'k',
      kvpValue: 1.5,
      deleteExpiryDate: new Date('2026-10-20T10:30').toISOString(),
    })
    expect(
      payloadOf(dictionary, 9, { kvpKey: 'k', kvpValue: ' ', deleteExpiryDate: '' }),
    ).toMatchObject({ kvpValue: null, deleteExpiryDate: null })
  })

  it('reports a number field that is not a number', () => {
    expect(draftProblems({ kvpKey: 'a', kvpValue: 'x', deleteExpiryDate: '' }, fields)).toEqual([
      { field: 'kvpValue', code: 'number' },
    ])
    expect(draftProblems({ kvpKey: 'a', kvpValue: '', deleteExpiryDate: '' }, fields)).toEqual([])
  })
})

describe('valueConfigOf', () => {
  it('reads the model relation into a grid config, the parent field also naming the read parameter', () => {
    expect(dictionary).toEqual({
      endpoint: '/api/EntityAnalysisModelDictionaryKvp',
      readParameter: 'entityAnalysisModelDictionaryId',
      parentField: 'entityAnalysisModelDictionaryId',
      parentKind: 4,
      valueFields: [
        { field: 'kvpKey', title: 'Key', type: 'string' },
        { field: 'kvpValue', title: 'Value', type: 'number' },
      ],
    })
  })
})

describe('matches and readPath', () => {
  it('searches every value field, case-insensitively', () => {
    const [row] = buildRows([record({ kvpKey: 'Alpha' })], [], false, null, fields)

    expect(matches(row!, fields, 'alp')).toBe(true)
    expect(matches(row!, fields, 'zzz')).toBe(false)
    expect(matches(row!, fields, '  ')).toBe(true)
  })

  it('names the read endpoint from the parent parameter', () => {
    expect(readPath(list)).toBe('/api/EntityAnalysisModelListValue/ByEntityAnalysisModelListId')
  })
})

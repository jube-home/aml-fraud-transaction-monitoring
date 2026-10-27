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
  buildHistoryRows,
  refusalReasons,
  stateOf,
  summariseChanges,
  summariseVersions,
  type ApprovalRecord,
  type ApprovalStatus,
} from '../model'

const status = (overrides: Partial<ApprovalStatus> = {}): ApprovalStatus => ({
  kind: 1,
  entityId: 5,
  name: 'Model',
  currentVersion: 2,
  effectiveVersion: 1,
  deleted: false,
  pending: true,
  rejected: false,
  makerUser: 'maker',
  approvalsRecorded: 0,
  approvalsRequired: 1,
  canApprove: true,
  ...overrides,
})

const record = (overrides: Partial<ApprovalRecord> = {}): ApprovalRecord => ({
  entityVersion: 1,
  state: 'Approved',
  note: null,
  createdUser: 'checker',
  createdDate: '2026-10-20T10:00:00Z',
  returnedNote: null,
  returnKindId: null,
  ...overrides,
})

describe('stateOf', () => {
  it.each([
    [{}, 'awaiting'],
    [{ pending: false }, 'approved'],
    [{ rejected: true }, 'rejected'],
    [{ deleted: true }, 'deletionAwaiting'],
    [{ deleted: true, pending: false }, 'deleted'],
    [{ deleted: true, rejected: true }, 'deletionRejected'],
  ] as const)('%j is %s', (overrides, expected) => {
    expect(stateOf(status(overrides))).toBe(expected)
  })
})

describe('buildHistoryRows', () => {
  it('leads with an awaiting-decision row only when the user can approve', () => {
    expect(buildHistoryRows(status(), [], [])[0]).toMatchObject({ pending: true, version: 2 })
    expect(buildHistoryRows(status({ canApprove: false }), [], [])).toEqual([])
  })

  it('names the change from the version history', () => {
    const rows = buildHistoryRows(
      status({ canApprove: false }),
      [record({ entityVersion: 1 }), record({ entityVersion: 2 }), record({ entityVersion: 3 })],
      [{ version: 3, deletedUser: 'x' }],
    )

    expect(rows.map((row) => row.change)).toEqual(['created', 'update', 'deletion'])
  })

  it('labels the version after a retract or resubmit', () => {
    const rows = buildHistoryRows(
      status({ canApprove: false }),
      [
        record({ entityVersion: 1, returnKindId: 1 }),
        record({ entityVersion: 2 }),
        record({ entityVersion: 3, returnKindId: 2 }),
        record({ entityVersion: 4 }),
      ],
      [],
    )

    expect(rows.map((row) => row.change)).toEqual(['created', 'retracted', 'update', 'resubmitted'])
  })

  it('marks only the current rejected version as returnable', () => {
    const rows = buildHistoryRows(
      status({ canApprove: false, rejected: true, pending: false, currentVersion: 2 }),
      [record({ entityVersion: 1 }), record({ entityVersion: 2, state: 'Rejected' })],
      [],
    )

    expect(rows.map((row) => row.currentRejected)).toEqual([false, true])
  })

  it('finds fields whatever the casing of the version history', () => {
    const rows = buildHistoryRows(
      status({ canApprove: false }),
      [record({ entityVersion: 3 })],
      [{ Version: 2, DeletedUser: 'x' }],
    )

    expect(rows[0]!.change).toBe('restore')
  })
})

describe('summariseVersions', () => {
  it('orders by version and links each to the one before', () => {
    const rows = summariseVersions([
      { Version: 2, Id: 20, CreatedUser: 'b' },
      { Version: 1, Id: 10, CreatedUser: 'a' },
    ])

    expect(rows.map((row) => [row.version, row.id, row.previousId])).toEqual([
      [1, 10, null],
      [2, 20, 10],
    ])
  })
})

describe('summariseChanges', () => {
  it('reads the property and both values', () => {
    expect(summariseChanges([{ propertyName: 'Name', fromValue: 'a', toValue: 'b' }])).toEqual([
      { field: 'Name', earlier: 'a', version: 'b' },
    ])
  })
})

describe('refusalReasons', () => {
  it('prefers the reasons, then the message, then the fallback', () => {
    expect(refusalReasons({ reasons: ['one', 'two'] }, 'x')).toEqual(['one', 'two'])
    expect(refusalReasons({ message: 'm' }, 'x')).toEqual(['m'])
    expect(refusalReasons(undefined, 'x')).toEqual(['x'])
  })
})

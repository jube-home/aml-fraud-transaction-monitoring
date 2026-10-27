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
  allocatedModelIds,
  bestVersionIds,
  comparable,
  defaultViewedVersion,
  differences,
  instanceRows,
  liveStanding,
  movementOf,
} from '../model'
import type { AnalyticsInstance, ModelVersion } from '../types'
import type { ValueStatus } from '../../valueGrid/model'

const version = (id: number, overrides: Partial<ModelVersion> = {}): ModelVersion => ({
  id,
  trainedModelId: 1,
  versionNumber: id,
  isActive: false,
  createdDate: null,
  testRows: 100,
  testRocAuc: 0.9,
  testAveragePrecision: 0.5,
  testLogLoss: 0.3,
  testBrierScore: 0.1,
  testExpectedCalibrationError: 0.05,
  warningCodes: [],
  transformedVariableCount: null,
  ...overrides,
})

describe('bestVersionIds', () => {
  it('marks the highest AUC and the lowest loss, and every version that ties', () => {
    const best = bestVersionIds([
      version(1, { testRocAuc: 0.9, testLogLoss: 0.4 }),
      version(2, { testRocAuc: 0.95, testLogLoss: 0.4 }),
      version(3, { testRocAuc: 0.95, testLogLoss: 0.5 }),
    ])

    expect(best.testRocAuc).toEqual([2, 3])
    expect(best.testLogLoss).toEqual([1, 2])
  })

  it('marks nothing for a metric with fewer than two scores', () => {
    const best = bestVersionIds([version(1), version(2, { testBrierScore: null })])

    expect(best.testBrierScore).toEqual([])
    expect(best.testRocAuc.length).toBeGreaterThan(0)
  })
})

describe('liveStanding', () => {
  it('counts the metrics on which the live version is best, a tie counting as best', () => {
    const standing = liveStanding([
      version(1, { isActive: true, testRocAuc: 0.96, testAveragePrecision: 0.4 }),
      version(2, { testRocAuc: 0.9, testAveragePrecision: 0.6 }),
    ])

    expect(standing.liveId).toBe(1)
    expect(standing.of).toBe(5)
    expect(standing.best).toBe(4)
    expect(standing.allBest).toBe(false)
  })

  it('is all best when the live version wins every compared metric', () => {
    const standing = liveStanding([
      version(1, {
        isActive: true,
        testRocAuc: 0.99,
        testAveragePrecision: 0.9,
        testLogLoss: 0.1,
        testBrierScore: 0.01,
        testExpectedCalibrationError: 0.01,
      }),
      version(2),
    ])

    expect(standing.allBest).toBe(true)
  })

  it('has no standing without a live version', () => {
    expect(liveStanding([version(1)])).toEqual({ liveId: null, best: 0, of: 0, allBest: false })
  })
})

describe('defaultViewedVersion', () => {
  it('starts on the live version, else the newest', () => {
    expect(defaultViewedVersion([version(1), version(2, { isActive: true })])).toBe(2)
    expect(defaultViewedVersion([version(1), version(3)])).toBe(3)
    expect(defaultViewedVersion([])).toBeNull()
  })
})

describe('movementOf', () => {
  const metric = (name: string, valueA: number | null, valueB: number | null) => ({
    name,
    valueA,
    valueB,
    difference: valueA !== null && valueB !== null ? valueB - valueA : null,
  })

  it.each([
    ['TestRocAuc', 0.9, 0.95, 'better'],
    ['TestRocAuc', 0.95, 0.9, 'worse'],
    ['TestLogLoss', 0.4, 0.3, 'better'],
    ['TestBrierScore', 0.1, 0.2, 'worse'],
    ['TestRecall', 0.5, 0.5, 'same'],
    ['TestRocAuc', null, 0.5, 'unknown'],
    ['Surprising', 1, 2, 'unknown'],
  ] as const)('%s %s to %s is %s', (name, a, b, expected) => {
    expect(movementOf(metric(name, a, b))).toBe(expected)
  })

  it('judges a calibration slope by its distance from one', () => {
    expect(movementOf(metric('TestCalibrationSlope', 0.7, 0.95))).toBe('better')
    expect(movementOf(metric('TestCalibrationSlope', 1.05, 1.4))).toBe('worse')
    expect(movementOf(metric('TestCalibrationSlope', 0.9, 1.1))).toBe('same')
  })
})

describe('comparability', () => {
  it('names what differs and is comparable only when nothing does', () => {
    const all = { sameSeed: true, sameSplitSizes: true, sameVariables: true }
    const some = { sameSeed: false, sameSplitSizes: true, sameVariables: false }

    expect(comparable(all)).toBe(true)
    expect(differences(all)).toEqual([])
    expect(comparable(some)).toBe(false)
    expect(differences(some)).toEqual(['seed', 'variables'])
  })
})

describe('instanceRows', () => {
  const instance = (id: number, overrides: Partial<AnalyticsInstance> = {}): AnalyticsInstance => ({
    id,
    guid: `g${id}`,
    name: `Instance ${id}`,
    entityAnalysisModelId: 7,
    responsePayload: true,
    reportTable: false,
    active: true,
    createdUser: 'maker',
    createdDate: null,
    deletedUser: null,
    version: 1,
    ...overrides,
  })

  const status = (entityId: number, overrides: Partial<ValueStatus> = {}): ValueStatus => ({
    kind: 1,
    entityId,
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

  it('names the allocated model, or says it is not allocated', () => {
    const rows = instanceRows(
      [instance(1), instance(2, { entityAnalysisModelId: null })],
      [],
      [{ id: 7, name: 'Payments' }],
      false,
      'Not allocated',
    )

    expect(rows.map((row) => row.allocated)).toEqual(['Payments', 'Not allocated'])
  })

  it('carries the approval state and progress of an instance awaiting a checker', () => {
    const [row] = instanceRows(
      [instance(1)],
      [status(1, { pending: true, canApprove: true, approvalsRecorded: 1, approvalsRequired: 2 })],
      [],
      false,
      '-',
    )

    expect(row).toMatchObject({
      state: 'awaiting',
      progress: '1 of 2',
      maker: 'maker',
      canApprove: true,
      hasBaseline: true,
    })
  })

  it('hides a deleted instance once its deletion is settled, but not while it is pending or rejected', () => {
    const rows = instanceRows(
      [
        instance(1, { deletedUser: 'x' }),
        instance(2, { deletedUser: 'x' }),
        instance(3, { deletedUser: 'x' }),
      ],
      [status(2, { pending: true }), status(3, { rejected: true })],
      [],
      false,
      '-',
    )

    expect(rows.map((row) => row.id)).toEqual([2, 3])
    expect(rows.map((row) => row.state)).toEqual(['deletionAwaiting', 'deletionRejected'])
  })

  it('reports every state unavailable when the status could not be read', () => {
    const [row] = instanceRows([instance(1)], [], [], true, '-')

    expect(row!.state).toBe('unavailable')
  })
})

describe('allocatedModelIds', () => {
  it('lists each allocated model once', () => {
    expect(
      allocatedModelIds([
        { entityAnalysisModelId: 7 },
        { entityAnalysisModelId: null },
        { entityAnalysisModelId: 7 },
        { entityAnalysisModelId: 9 },
      ]),
    ).toEqual([7, 9])
  })
})

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

import {describe, expect, it} from 'vitest'
import {
  analystLikelihoodKeys,
  canRecord,
  earlierOf,
  maximumReason,
  type AnalystAssessment,
} from '../assessmentApi'

function assessment(guid: string, withdrawnDate: string | null = null): AnalystAssessment {
  return {
    guid,
    caseId: 1,
    likelihoodId: 2,
    reason: 'r',
    computedLikelihoodProbability: null,
    createdUser: 'sam',
    createdDate: '2026-11-14T10:00:00Z',
    withdrawnDate,
    mine: true,
  }
}

describe('assessmentApi', () => {
  it('names each of the three likelihood choices', () => {
    expect(Object.keys(analystLikelihoodKeys)).toEqual(['1', '2', '3'])
  })

  it('needs a choice from one to three and a reason within the limit', () => {
    expect(canRecord(3, 'Known pattern')).toBe(true)
    expect(canRecord(0, 'Known pattern')).toBe(false)
    expect(canRecord(4, 'Known pattern')).toBe(false)
    expect(canRecord(2, '   ')).toBe(false)
    expect(canRecord(2, 'x'.repeat(maximumReason))).toBe(true)
    expect(canRecord(2, 'x'.repeat(maximumReason + 1))).toBe(false)
  })

  it('lists every view but the current one as earlier, withdrawn ones included', () => {
    const current = assessment('b')
    const state = {current, history: [assessment('c', '2026-11-14T11:00:00Z'), current, assessment('a')]}

    expect(earlierOf(state).map((item) => item.guid)).toEqual(['c', 'a'])
    expect(earlierOf({current: null, history: [assessment('a')]}).map((item) => item.guid)).toEqual(['a'])
    expect(earlierOf(undefined)).toEqual([])
  })
})

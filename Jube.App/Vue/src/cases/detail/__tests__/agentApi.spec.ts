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
import {currentRun, type CaseAgentRun} from '../agentApi'

function run(guid: string, stateId: number): CaseAgentRun {
  return {
    guid,
    recommendedOutcomeId: 1,
    rationale: 'r',
    evidence: [],
    stateId,
    overrideReason: null,
    decidedUser: null,
    decidedDate: null,
    createdDate: '2026-11-11T00:00:00Z',
    modelName: null,
  }
}

describe('currentRun', () => {
  it('is null without runs', () => {
    expect(currentRun(undefined)).toBeNull()
    expect(currentRun([])).toBeNull()
  })

  it('skips superseded runs and takes the newest remaining', () => {
    expect(currentRun([run('a', 4), run('b', 1), run('c', 2)])?.guid).toBe('b')
  })

  it('is null when every run is superseded', () => {
    expect(currentRun([run('a', 4)])).toBeNull()
  })
})

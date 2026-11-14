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

import {beforeEach, describe, expect, it} from 'vitest'
import {loadLastUsed, saveLastUsed, type LastUsedScope} from '../lastUsed'
import {parseState} from '../pageState'

const wf = '11111111-1111-1111-1111-111111111111'
const scope: LastUsedScope = {userName: 'alice', tenantRegistryId: 1}
const mine = '22222222-2222-2222-2222-222222222222'

describe('last used filter', () => {
  beforeEach(() => window.localStorage.clear())

  it('restores the workflow, filter and sort but never the open case or tab', () => {
    saveLastUsed({...parseState({wf, mine, sort: '3', case: '9', tab: 'quality'})}, scope)

    const last = loadLastUsed(scope)

    expect(last?.workflow).toBe(wf)
    expect(last?.source).toEqual({kind: 'own', guid: mine})
    expect(last?.sort).toBe(3)
    expect(last?.caseId).toBeNull()
    expect(last?.tab).toBe('queue')
  })

  it('has nothing to restore before anything is saved or when storage is corrupt', () => {
    expect(loadLastUsed(scope)).toBeNull()

    window.localStorage.setItem('jube.cases.last:1:alice', '{nope')

    expect(loadLastUsed(scope)).toBeNull()
  })

  it('does not save a state with no workflow', () => {
    saveLastUsed(parseState({}), scope)

    expect(loadLastUsed(scope)).toBeNull()
  })

  it('keeps each user and tenant apart and does nothing without a signed-in user', () => {
    saveLastUsed(parseState({wf, sort: '2'}), scope)

    expect(loadLastUsed({userName: 'bob', tenantRegistryId: 1})).toBeNull()
    expect(loadLastUsed({userName: 'alice', tenantRegistryId: 2})).toBeNull()
    expect(loadLastUsed(undefined)).toBeNull()

    saveLastUsed(parseState({wf}), undefined)

    expect(window.localStorage.length).toBe(1)
  })

  it('never throws when storage is unavailable', () => {
    const original = window.localStorage.getItem
    window.localStorage.getItem = () => {
      throw new Error('blocked')
    }

    expect(loadLastUsed(scope)).toBeNull()

    window.localStorage.getItem = original
  })
})

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
  defaultState,
  mergeState,
  parseState,
  requestFor,
  sameSource,
  sameState,
  serialiseState,
  sourceKey,
} from '../pageState'

const guid = '0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d'
const other = 'ffffffff-4e5f-4a6b-8c7d-9e0f1a2b3c4d'

describe('cases page state', () => {
  it('reads nothing as the defaults', () => {
    expect(parseState({})).toEqual(defaultState)
  })

  it('round trips every part of the state', () => {
    const state = {
      workflow: guid,
      source: { kind: 'own' as const, guid: other },
      sort: 3,
      caseId: 9041,
      tab: 'supervision' as const,
    }

    expect(parseState(serialiseState(state))).toEqual(state)
  })

  it('leaves the defaults out of the address', () => {
    expect(serialiseState(defaultState)).toEqual({})
    expect(serialiseState({ ...defaultState, workflow: guid })).toEqual({ wf: guid })
    expect(serialiseState({ ...defaultState, source: { kind: 'view', view: 6 } })).toEqual({})
    expect(serialiseState({ ...defaultState, source: { kind: 'view', view: 2 } })).toEqual({
      view: '2',
    })
  })

  it.each([
    [{ wf: 'not-a-guid' }, ''],
    [{ wf: guid.toUpperCase() }, guid],
  ])('accepts a workflow only when it is a guid', (query, expected) => {
    expect(parseState(query).workflow).toBe(expected)
  })

  it.each(['0', '8', '-1', '1.5', 'x', '', '99999999999'])(
    'ignores an out of range view of %s',
    (view) => {
      expect(parseState({ view }).source).toEqual({ kind: 'view', view: 6 })
    },
  )

  it('ignores a bad sort or case and takes the first of repeated values', () => {
    expect(parseState({ sort: '9', case: '0' })).toMatchObject({ sort: 0, caseId: null })
    expect(parseState({ sort: ['2', '5'], case: ['12', '13'] })).toMatchObject({
      sort: 2,
      caseId: 12,
    })
  })

  it('prefers a workflow filter over a saved search over a view', () => {
    expect(parseState({ filter: guid, mine: other, view: '2' }).source).toEqual({
      kind: 'workflow',
      guid,
    })
    expect(parseState({ mine: other, view: '2' }).source).toEqual({ kind: 'own', guid: other })
  })

  it('keeps the queue tab out of the address and reads only the tabs that exist', () => {
    expect(serialiseState({ ...defaultState, tab: 'health' })).toEqual({ tab: 'health' })
    expect(serialiseState({ ...defaultState, tab: 'queue' })).toEqual({})
    expect(parseState({ tab: 'quality' }).tab).toBe('quality')
    expect(parseState({ tab: 'nonsense' }).tab).toBe('queue')
    expect(parseState({ tab: ['health', 'quality'] }).tab).toBe('health')
  })

  it('merges a new state over the address and leaves every other tab its own settings', () => {
    const merged = mergeState(
      { wf: guid, view: '2', case: '5', tab: 'health', days: '90', idle: '240' },
      { ...defaultState, workflow: guid, tab: 'supervision' },
    )

    expect(merged).toEqual({ days: '90', idle: '240', wf: guid, tab: 'supervision' })
  })

  it('compares states and sources', () => {
    const a = { ...defaultState, workflow: guid }

    expect(sameState(a, { ...a })).toBe(true)
    expect(sameState(a, { ...a, sort: 1 })).toBe(false)
    expect(sameState(a, { ...a, caseId: 5 })).toBe(false)
    expect(sameState(a, { ...a, tab: 'health' })).toBe(false)
    expect(sameSource({ kind: 'own', guid }, { kind: 'workflow', guid })).toBe(false)
    expect(sameSource({ kind: 'own', guid }, { kind: 'own', guid: other })).toBe(false)
    expect(sourceKey({ kind: 'view', view: 3 })).toBe('view:3')
    expect(sourceKey({ kind: 'own', guid })).toBe(`own:${guid}`)
  })

  it('builds the request for each kind of source', () => {
    const base = { ...defaultState, workflow: guid, sort: 1 }

    expect(requestFor({ ...base, source: { kind: 'view', view: 2 } })).toEqual({
      caseWorkflowGuid: guid,
      view: 2,
      sort: 1,
      take: 100,
      skip: 0,
    })
    expect(requestFor({ ...base, source: { kind: 'workflow', guid: other } }, 100)).toEqual({
      caseWorkflowGuid: guid,
      caseWorkflowFilterGuid: other,
      sort: 1,
      take: 100,
      skip: 100,
    })
    expect(requestFor({ ...base, source: { kind: 'own', guid: other } }).caseSearchFilterGuid).toBe(
      other,
    )
  })
})

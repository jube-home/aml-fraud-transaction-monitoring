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
import {deriveGate, openItemNames, progressOf} from '../gate'
import {itemState, type ChecklistOverride} from '../types'
import {item, view} from './support'

const override: ChecklistOverride = {
  id: 5,
  caseId: 9041,
  reason: 'Verified by phone.',
  grantedUser: 'sam',
  grantedDate: '2026-11-04T10:00:00Z',
  openItems: ['Contact the customer'],
}

describe('openItemNames', () => {
  it('lists the shown items in order and leaves out done ones', () => {
    const current = view([
      item({id: 1, name: 'First', stateId: itemState.done}),
      item({id: 2, name: 'Second'}),
      item({id: 3, name: 'Third'}),
    ])

    expect(openItemNames(current)).toEqual(['Second', 'Third'])
  })

  it('is empty with no checklist', () => {
    expect(openItemNames(undefined)).toEqual([])
  })
})

describe('deriveGate', () => {
  const context = {open: true, mine: true, permitted: false}

  it('blocks while shown items are open', () => {
    const gate = deriveGate(view([item()]), context)

    expect(gate.blocked).toBe(true)
    expect(gate.names).toEqual(['Record an investigation note'])
  })

  it('does not block when everything required is done', () => {
    const gate = deriveGate(view([item({stateId: itemState.done})]), context)

    expect(gate.blocked).toBe(false)
    expect(gate.names).toEqual([])
  })

  it('does not block a closed case', () => {
    expect(deriveGate(view([item()]), {...context, open: false}).blocked).toBe(false)
  })

  it('does not block when an override is in place and reports it', () => {
    const gate = deriveGate(view([item()], {override}), context)

    expect(gate.blocked).toBe(false)
    expect(gate.override?.grantedUser).toBe('sam')
    expect(gate.canOverride).toBe(false)
  })

  it('offers the override to a permitted user who is not the holder', () => {
    const gate = deriveGate(view([item()]), {open: true, mine: false, permitted: true})

    expect(gate.canOverride).toBe(true)
  })

  it('never offers the override to the holder, even with the permission', () => {
    const gate = deriveGate(view([item()]), {open: true, mine: true, permitted: true})

    expect(gate.canOverride).toBe(false)
  })

  it('never offers the override without the permission', () => {
    const gate = deriveGate(view([item()]), {open: true, mine: false, permitted: false})

    expect(gate.canOverride).toBe(false)
  })

  it('offers nothing when there is nothing open', () => {
    const gate = deriveGate(view([item({stateId: itemState.done})]), {
      open: true,
      mine: false,
      permitted: true,
    })

    expect(gate.canOverride).toBe(false)
  })
})

describe('progressOf', () => {
  it('reports done over required, zero without a checklist', () => {
    expect(progressOf(view([item({stateId: itemState.done}), item({id: 2})]))).toEqual({
      done: 1,
      required: 2,
    })
    expect(progressOf(undefined)).toEqual({done: 0, required: 0})
  })
})

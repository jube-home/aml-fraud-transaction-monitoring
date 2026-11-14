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
import {createOverrideSelection} from '../selection'

describe('createOverrideSelection', () => {
  it('toggles a model and a rule independently and starts both as suppress', async () => {
    const selection = createOverrideSelection()

    await selection.actions.setModel('m1', true)
    await selection.actions.setRule('m1', 'Rule', true)

    expect(selection.hasModel('m1')).toBe(true)
    expect(selection.hasRule('m1', 'Rule')).toBe(true)
    expect(selection.hasRule('m1', 'Other')).toBe(false)
    expect(selection.kindOfRule('m1', 'Rule')).toBe(0)
    expect(selection.targets.value).toHaveLength(2)

    await selection.actions.setModel('m1', false)

    expect(selection.hasModel('m1')).toBe(false)
    expect(selection.hasRule('m1', 'Rule')).toBe(true)
  })

  it('changes only the kind of the rule asked for', async () => {
    const selection = createOverrideSelection()

    await selection.actions.setRule('m1', 'A', true)
    await selection.actions.setRule('m1', 'B', true)
    await selection.actions.setRuleKind('m1', 'A', 1)

    expect(selection.kindOfRule('m1', 'A')).toBe(1)
    expect(selection.kindOfRule('m1', 'B')).toBe(0)
  })

  it('never writes to the server and ignores expiry changes', async () => {
    const selection = createOverrideSelection()

    await expect(selection.actions.setModelExpiry('m1', new Date())).resolves.toBe(true)
    await expect(selection.actions.setRuleExpiry('m1', 'A', null)).resolves.toBe(true)
    expect(selection.targets.value).toEqual([])
  })
})

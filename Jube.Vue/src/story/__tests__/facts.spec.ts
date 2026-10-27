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
import { recordFacts, type FactLabels } from '../facts'
import type { FormModel } from '../../formModel/types'

const labels: FactLabels = {
  active: 'Active',
  inactive: 'Inactive',
  locked: 'Locked',
  version: (version) => `v${version}`,
  awaiting: (count) => `${count} awaiting`,
}

const model = {
  fields: [{ name: 'active', type: 'boolean' }],
  lockFields: ['locked'],
} as unknown as FormModel

describe('recordFacts', () => {
  it('lists version, state and lock', () => {
    expect(recordFacts(model, { version: 14, active: true, locked: true }, labels)).toEqual([
      { text: 'v14' },
      { text: 'Active', tone: 'success' },
      { text: 'Locked', tone: 'warning' },
    ])
  })

  it('says inactive and omits a lock that is off', () => {
    expect(recordFacts(model, { version: 2, active: false, locked: false }, labels)).toEqual([
      { text: 'v2' },
      { text: 'Inactive', tone: 'neutral' },
    ])
  })

  it('adds awaiting approvals only when there are some', () => {
    expect(recordFacts(undefined, {}, labels, 3)).toEqual([{ text: '3 awaiting', tone: 'warning' }])
    expect(recordFacts(undefined, {}, labels, 0)).toEqual([])
    expect(recordFacts(undefined, {}, labels, undefined)).toEqual([])
  })

  it('states nothing it cannot know without a model', () => {
    expect(recordFacts(undefined, { version: 1 }, labels)).toEqual([{ text: 'v1' }])
  })
})

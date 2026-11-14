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
import {directionOf, scoreKind, shownInputs, trainedAgeDays, unlabelled} from '../facts'
import type {Adaptation, ContributionItem} from '../types'

function adaptation(overrides: Partial<Adaptation>): Adaptation {
  return {
    source: 'exhaustive',
    name: 'StructNet',
    value: 0.9,
    error: null,
    humanLabel: null,
    narrative: null,
    model: null,
    result: null,
    calibration: null,
    contribution: null,
    journey: [],
    ...overrides,
  }
}

function item(overrides: Partial<ContributionItem>): ContributionItem {
  return {
    name: 'Amount',
    weight: 1,
    direction: 1,
    significance: null,
    source: 'Payload',
    humanLabel: 'Amount',
    signed: 1,
    hidden: false,
    ...overrides,
  }
}

describe('model facts', () => {
  it('counts whole days since training and says nothing when it is not declared or unreadable', () => {
    const now = new Date('2026-10-31T12:00:00Z')

    expect(trainedAgeDays('2026-10-01T00:00:00Z', now)).toBe(30)
    expect(trainedAgeDays('2027-01-01T00:00:00Z', now)).toBe(0)
    expect(trainedAgeDays(null, now)).toBeNull()
    expect(trainedAgeDays('not a date', now)).toBeNull()
  })

  it('calls a score a probability only when it is calibrated in probability space', () => {
    const calibrated = {
      space: 'Probability',
      calibrated: true,
      method: null,
      sample: null,
      brier: null,
      bands: [],
    }

    expect(scoreKind(adaptation({calibration: calibrated}))).toBe('probability')
    expect(scoreKind(adaptation({calibration: {...calibrated, calibrated: false}}))).toBe(
      'score',
    )
    expect(scoreKind(adaptation({calibration: {...calibrated, space: 'LogOdds'}}))).toBe(
      'score',
    )
    expect(scoreKind(adaptation({}))).toBe('score')
  })

  it('reads the direction of an input and flags one with no description', () => {
    expect(directionOf(item({direction: 1}))).toBe('higher')
    expect(directionOf(item({direction: -1}))).toBe('lower')
    expect(directionOf(item({direction: null}))).toBe('undeclared')
    expect(directionOf(item({direction: 0}))).toBe('undeclared')
    expect(unlabelled(item({humanLabel: null}))).toBe(true)
    expect(unlabelled(item({}))).toBe(false)
  })

  it('lists only the inputs the caller can see', () => {
    const shown = shownInputs(
      adaptation({
        contribution: {
          space: null,
          method: null,
          exact: null,
          baseValue: null,
          hiddenItems: 1,
          items: [item({}), item({hidden: true, name: null}), item({name: null})],
        },
      }),
    )

    expect(shown).toHaveLength(1)
  })
})

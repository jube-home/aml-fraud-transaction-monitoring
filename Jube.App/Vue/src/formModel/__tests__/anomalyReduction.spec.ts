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
import {reductionOf, withReduction} from '../anomalyReduction'

describe('anomaly reduction', () => {
  it('reads the choice back from the stored parameters', () => {
    expect(reductionOf(undefined)).toBe('default')
    expect(reductionOf('')).toBe('default')
    expect(reductionOf('not json')).toBe('default')
    expect(reductionOf('{"reduction":{"mode":"None","scaling":"None"}}')).toBe('none')
    expect(reductionOf('{"reduction":{"mode":"Standard","scaling":"Robust"}}')).toBe('standard')
    expect(reductionOf('{"reduction":{"mode":"Standard","projection":"ParallelAnalysis"}}')).toBe(
      'components',
    )
  })

  it('writes each preset into the parameters', () => {
    expect(JSON.parse(withReduction('', 'none'))).toEqual({
      reduction: {mode: 'None', scaling: 'None'},
    })
    expect(JSON.parse(withReduction('', 'components'))).toEqual({
      reduction: {mode: 'Standard', scaling: 'Robust', projection: 'ParallelAnalysis'},
    })
  })

  it('keeps the other parameters when the reduction changes', () => {
    const next = JSON.parse(withReduction('{"contamination":0.1}', 'standard')) as object

    expect(next).toEqual({
      contamination: 0.1,
      reduction: {mode: 'Standard', scaling: 'Robust'},
    })
  })

  it('removes the reduction for the default and empties the parameters when nothing remains', () => {
    expect(withReduction('{"reduction":{"mode":"None"}}', 'default')).toBe('')
    expect(JSON.parse(withReduction('{"contamination":0.1,"reduction":{}}', 'default'))).toEqual({
      contamination: 0.1,
    })
  })

  it('round-trips every preset', () => {
    for (const choice of ['none', 'standard', 'components'] as const) {
      expect(reductionOf(withReduction('', choice))).toBe(choice)
    }
  })
})

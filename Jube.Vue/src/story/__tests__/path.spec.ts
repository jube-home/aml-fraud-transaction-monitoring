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
import { decodeAddress, encodeAddress } from '../path'

describe('story address', () => {
  it('is empty before a model is chosen', () => {
    expect(encodeAddress({ modelId: undefined, operate: undefined, steps: [] })).toBe('')
    expect(decodeAddress(undefined).modelId).toBeUndefined()
    expect(decodeAddress('').steps).toEqual([])
  })

  it('round trips a model with a drill down', () => {
    const address = {
      modelId: 12,
      operate: undefined,
      steps: [
        { area: 'CaseWorkflow', id: 5 },
        { area: 'CaseWorkflowXPath', id: 9 },
      ],
    }

    expect(encodeAddress(address)).toBe('EntityAnalysisModel:12,CaseWorkflow:5,CaseWorkflowXPath:9')
    expect(decodeAddress(encodeAddress(address))).toEqual(address)
  })

  it('round trips an operate step', () => {
    const address = { modelId: 3, operate: 'integrity' as const, steps: [] }

    expect(encodeAddress(address)).toBe('EntityAnalysisModel:3,~integrity')
    expect(decodeAddress('EntityAnalysisModel:3,~integrity')).toEqual(address)
  })

  it('refuses an address that does not start with a model', () => {
    expect(decodeAddress('CaseWorkflow:5').modelId).toBeUndefined()
    expect(decodeAddress('EntityAnalysisModel:x').modelId).toBeUndefined()
    expect(decodeAddress('EntityAnalysisModel:-2').modelId).toBeUndefined()
  })

  it('drops steps it cannot read', () => {
    expect(decodeAddress('EntityAnalysisModel:1,Tag:x,Tag:4,:5').steps).toEqual([
      { area: 'Tag', id: 4 },
    ])
  })
})

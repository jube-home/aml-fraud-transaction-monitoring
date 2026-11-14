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
import {bulkLimit, skipSummary} from '../bulkApi'
import {useCaseSelection} from '../caseSelection'

describe('the case selection', () => {
  it('adds, removes and counts', () => {
    const selection = useCaseSelection()

    selection.toggle(1, true)
    selection.toggle(2, true)
    selection.toggle(1, false)

    expect(selection.ids()).toEqual([2])
    expect(selection.count.value).toBe(1)
    expect(selection.has(2)).toBe(true)
    expect(selection.has(1)).toBe(false)
  })

  it('refuses a case beyond the bulk limit but accepts one already chosen', () => {
    const selection = useCaseSelection()

    for (let id = 1; id <= bulkLimit; id++) expect(selection.toggle(id, true)).toBe(true)

    expect(selection.toggle(bulkLimit + 1, true)).toBe(false)
    expect(selection.toggle(1, true)).toBe(true)
    expect(selection.count.value).toBe(bulkLimit)
  })

  it('drops cases that are no longer on screen', () => {
    const selection = useCaseSelection()

    selection.toggle(1, true)
    selection.toggle(2, true)
    selection.toggle(3, true)
    selection.keepOnly([2, 3, 4])

    expect(selection.ids().sort()).toEqual([2, 3])
    expect(selection.count.value).toBe(2)
  })

  it('clears everything', () => {
    const selection = useCaseSelection()

    selection.toggle(1, true)
    selection.clear()

    expect(selection.ids()).toEqual([])
    expect(selection.count.value).toBe(0)
  })
})

describe('skipSummary', () => {
  it('counts only the skipped cases by reason', () => {
    const counts = skipSummary({
      done: 1,
      skipped: 3,
      items: [
        {caseId: 1, done: true, skipReasonId: 0},
        {caseId: 2, done: false, skipReasonId: 3},
        {caseId: 3, done: false, skipReasonId: 3},
        {caseId: 4, done: false, skipReasonId: 2},
      ],
    })

    expect(counts.get(3)).toBe(2)
    expect(counts.get(2)).toBe(1)
    expect(counts.has(0)).toBe(false)
  })
})

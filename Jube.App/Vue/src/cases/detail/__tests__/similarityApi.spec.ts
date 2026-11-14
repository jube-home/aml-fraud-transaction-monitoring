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
import {headlineOf, outcomeOf, reasonKey, type CaseSimilarity} from '../similarityApi'

const base: CaseSimilarity = {
  built: true,
  checkedDate: null,
  similar: 7,
  closed: 6,
  confirmed: 5,
  open: 1,
  enoughToJudge: true,
  neighbours: [],
}

describe('similar cases', () => {
  it('reads the outcome from the closed status first and the status outcome second', () => {
    expect(outcomeOf({closedStatusId: 0, outcomeId: 1})).toBe('open')
    expect(outcomeOf({closedStatusId: 3, outcomeId: 1})).toBe('confirmed')
    expect(outcomeOf({closedStatusId: 3, outcomeId: 2})).toBe('notConfirmed')
    expect(outcomeOf({closedStatusId: 3, outcomeId: 0})).toBe('closed')
  })

  it('names rules, models and tags and calls every other shared fact the same field', () => {
    expect(reasonKey(1)).toBe('caseDetail.similar.reason.rule')
    expect(reasonKey(2)).toBe('caseDetail.similar.reason.model')
    expect(reasonKey(3)).toBe('caseDetail.similar.reason.tag')
    expect([4, 5, 6, 7, 8].map(reasonKey)).toEqual(Array(5).fill('caseDetail.similar.reason.same'))
  })

  it('chooses the headline from the state of the comparison', () => {
    expect(headlineOf(undefined, true)).toBe('failed')
    expect(headlineOf(undefined, false)).toBe('loading')
    expect(headlineOf({...base, built: false}, false)).toBe('pending')
    expect(headlineOf({...base, similar: 0}, false)).toBe('none')
    expect(headlineOf({...base, enoughToJudge: false}, false)).toBe('tooFew')
    expect(headlineOf(base, false)).toBe('headline')
  })
})

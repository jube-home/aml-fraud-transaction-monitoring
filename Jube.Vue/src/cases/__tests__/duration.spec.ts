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
import { durationText, relativeText, type Translate } from '../duration'

const t: Translate = (key, params) => `${key}:${JSON.stringify(params ?? {})}`
const number = (value: number) => String(value)

describe('durations', () => {
  it.each([
    [30, 'cases.time.minutes:{"n":"30"}'],
    [59.4, 'cases.time.minutes:{"n":"59"}'],
    [90, 'cases.time.hours:{"n":"1.5"}'],
    [1439, 'cases.time.hours:{"n":"24"}'],
    [2160, 'cases.time.days:{"n":"1.5"}'],
    [-90, 'cases.time.hours:{"n":"1.5"}'],
  ])('puts %s minutes in the unit that reads best', (minutes, expected) => {
    expect(durationText(t, minutes, number)).toBe(expected)
  })

  it('says whether a deadline is ahead or behind', () => {
    const now = Date.parse('2026-10-23T12:00:00Z')

    const ahead = relativeText(t, '2026-10-23T14:00:00Z', now, number)

    expect(ahead).toContain('cases.time.in')
    expect(ahead).toContain('cases.time.hours')
    expect(relativeText(t, '2026-10-23T11:00:00Z', now, number)).toContain('cases.time.overdue')
  })
})

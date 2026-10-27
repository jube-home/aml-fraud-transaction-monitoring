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
import { joobyWelcome, welcomeFor } from '../welcome'

describe('joobyWelcome', () => {
  it('says the same welcome as the chat, with thousands separators', () => {
    expect(
      joobyWelcome({ bites: 117683, trails: 3, saltlicks: 0, joobriks: 223, yokes: 824 }),
    ).toBe(
      "I'm Jooby -- this platform's fraud and AML assistant, not anyone standing in for me. " +
        '117,683 Bites, 3 Trails, 0 Saltlicks, 223 Joobriks on call, 824 Yokes ready, and counting. ' +
        'What brings you in?',
    )
  })
})

describe('welcomeFor', () => {
  const stats = { bites: 1, trails: 2, saltlicks: 3, joobriks: 4, yokes: 5 }

  it('is the online welcome when Jooby is switched on', () => {
    expect(welcomeFor(stats, false)).toBe(joobyWelcome(stats))
  })

  it('is the offline welcome, with the setup call to action, when it is not', () => {
    const text = welcomeFor(stats, true)

    expect(text).toContain('DigitalOcean')
    expect(text).not.toBe(joobyWelcome(stats))
  })

  it('is empty online until the statistics arrive', () => {
    expect(welcomeFor(undefined, false)).toBe('')
  })
})

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
import { escapeHtml, formatStats, joinWithAnd, relativeTime } from '../format'
import { pickOfflineWelcome } from '../offlineWelcome'

describe('escapeHtml', () => {
  it('escapes every character that can open markup or an attribute', () => {
    expect(escapeHtml(`<img src="x" onerror='a&b'>`)).toBe(
      '&lt;img src=&quot;x&quot; onerror=&#39;a&amp;b&#39;&gt;',
    )
  })
})

describe('relativeTime', () => {
  const now = Date.parse('2026-10-20T12:00:00Z')

  it.each([
    ['2026-10-20T11:59:30Z', 'just now'],
    ['2026-10-20T11:15:00Z', '45m ago'],
    ['2026-10-20T07:00:00Z', '5h ago'],
    ['2026-10-17T12:00:00Z', '3d ago'],
  ])('renders %s as %s', (value, expected) => {
    expect(relativeTime(value, now)).toBe(expected)
  })

  it('renders nothing for a missing date', () => {
    expect(relativeTime(null)).toBe('')
  })
})

describe('formatStats', () => {
  it('shows tokens with the cached share, duration and cost', () => {
    expect(
      formatStats({
        inputTokens: 200,
        cachedInputTokens: 100,
        outputTokens: 50,
        durationMs: 1500,
        joobrikCount: 3,
        distinctJoobrikCount: 2,
        totalCost: 0.0123,
      }),
    ).toBe('↑200 / 100 (50%) · ↓50 · 1.5s · 3 Joobriks (2 distinct) · $0.0123')
  })

  it('uses milliseconds under a second and says when the cost is unknown', () => {
    expect(formatStats({ durationMs: 240, joobrikCount: 1 })).toBe(
      '240ms · 1 Joobrik · cost unknown',
    )
  })

  it('is empty when nothing is known', () => {
    expect(formatStats({})).toBe('')
  })
})

describe('joinWithAnd', () => {
  it.each([
    [[], ''],
    [['A'], 'A'],
    [['A', 'B'], 'A and B'],
    [['A', 'B', 'C'], 'A, B and C'],
  ])('joins %j', (items, expected) => {
    expect(joinWithAnd(items)).toBe(expected)
  })
})

describe('pickOfflineWelcome', () => {
  it('weaves the startup counts into a chosen opener and ends with the call to action', () => {
    const text = pickOfflineWelcome(
      { bites: 1234, trails: 2, saltlicks: 3, joobriks: 4, yokes: 5 },
      () => 0,
    )

    expect(text).toContain('1,234')
    expect(text).toContain('DigitalOcean')
  })

  it('falls back to a generic opener without statistics', () => {
    expect(pickOfflineWelcome(null)).toContain("I'm Jooby")
  })
})

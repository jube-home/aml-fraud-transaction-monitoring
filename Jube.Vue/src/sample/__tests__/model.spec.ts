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
import { defaultDraft, percent, sampleBody, sampleProblems, type SampleDraft } from '../model'

const draft = (overrides: Partial<SampleDraft> = {}): SampleDraft => ({
  modelGuid: 'g',
  from: '2026-10-20T09:00',
  to: '2026-10-21T09:00',
  sample: 0.02,
  ...overrides,
})

describe('defaultDraft', () => {
  it('opens on the last day at two percent with no model chosen', () => {
    const value = defaultDraft(new Date('2026-10-21T10:30:00'))

    expect(value).toEqual({
      modelGuid: '',
      from: '2026-10-20T10:30',
      to: '2026-10-21T10:30',
      sample: 0.02,
    })
  })
})

describe('sampleProblems', () => {
  it('accepts a complete draft', () => {
    expect(sampleProblems(draft())).toEqual([])
  })

  it.each([
    [{ modelGuid: '' }, ['model']],
    [{ from: '' }, ['from']],
    [{ to: '' }, ['to']],
    [{ to: '2026-10-20T09:00' }, ['order']],
    [{ to: '2026-10-19T09:00' }, ['order']],
    [{ sample: 1.5 }, ['sample']],
    [{ sample: -0.1 }, ['sample']],
    [{ sample: Number.NaN }, ['sample']],
  ] as const)('%j reports %j', (overrides, expected) => {
    expect(sampleProblems(draft(overrides))).toEqual(expected)
  })

  it('allows the extremes of the rate', () => {
    expect(sampleProblems(draft({ sample: 0 }))).toEqual([])
    expect(sampleProblems(draft({ sample: 1 }))).toEqual([])
  })
})

describe('sampleBody', () => {
  it('sends the window as UTC instants and the rate as a number', () => {
    expect(sampleBody(draft())).toEqual({
      entityAnalysisModelGuid: 'g',
      sample: 0.02,
      dateFrom: new Date('2026-10-20T09:00').toISOString(),
      dateTo: new Date('2026-10-21T09:00').toISOString(),
    })
  })
})

describe('percent', () => {
  it('shows a decimal below ten percent and whole numbers above', () => {
    expect(percent(0.02)).toBe('2.0%')
    expect(percent(0.055)).toBe('5.5%')
    expect(percent(0.25)).toBe('25%')
    expect(percent(1)).toBe('100%')
  })
})

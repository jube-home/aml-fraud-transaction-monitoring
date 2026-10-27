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
import { canvasLayouts, chartPath, defaultPeriod, periodFrom, periodOf } from '../analyticsLayout'

const guid = '11111111-1111-1111-1111-111111111111'

describe('periodFrom', () => {
  it('starts at midnight UTC the given number of days back so the address is stable all day', () => {
    const morning = periodFrom(30, new Date('2026-10-23T01:15:00Z'))
    const evening = periodFrom(30, new Date('2026-10-23T22:50:00Z'))

    expect(morning).toBe('2026-09-23T00:00:00.000Z')
    expect(evening).toBe(morning)
  })
})

describe('periodOf', () => {
  it('accepts only the offered periods and otherwise uses the default', () => {
    expect(periodOf('7')).toBe(7)
    expect(periodOf(['90', '7'])).toBe(90)
    expect(periodOf('45')).toBe(defaultPeriod)
    expect(periodOf(undefined)).toBe(defaultPeriod)
    expect(periodOf('abc')).toBe(defaultPeriod)
  })
})

describe('chartPath', () => {
  it('names the chart kind, the workflow and the start of the period', () => {
    const path = chartPath('Aging', guid, 7, new Date('2026-10-23T10:00:00Z'))

    expect(path).toBe(
      `/api/CaseAnalytics/Chart/Aging?caseWorkflowGuid=${guid}&from=2026-10-16T00%3A00%3A00.000Z`,
    )
  })
})

describe('canvasLayouts', () => {
  it('draws every chart kind the server offers exactly once across both canvases', () => {
    const kinds = Object.values(canvasLayouts)
      .flatMap((layout) => layout.sections)
      .flatMap((section) => section.charts.map((chart) => chart.kind))
      .sort()

    expect(kinds).toEqual(
      [
        'Aging',
        'Arrivals',
        'Calibration',
        'FirstView',
        'FirstViewByHour',
        'Flow',
        'Gains',
        'Handoffs',
        'Resolution',
        'RuleEffort',
        'RulePrecision',
        'ServiceLevelWeekly',
        'StatusDwell',
      ].sort(),
    )
  })

  it('keeps the quality review sample as a card with no chart of its own', () => {
    const sample = canvasLayouts.quality.sections.find((section) => section.id === 'sample')

    expect(sample?.card).toBe('qaSample')
    expect(sample?.charts).toEqual([])
  })

  it('gives every section a unique id', () => {
    const ids = Object.values(canvasLayouts).flatMap((layout) =>
      layout.sections.map((section) => section.id),
    )

    expect(new Set(ids).size).toBe(ids.length)
  })
})

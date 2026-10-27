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
import {
  batchErrorText,
  canStop,
  chartUrl,
  controlsOf,
  correlationTone,
  generationOf,
  groupPrescriptions,
  hasResults,
  initialParams,
  isTerminal,
  modelTypeLabel,
  narrativeParagraphs,
  performanceCharts,
  phaseOf,
  refreshInterval,
  sliderStep,
  sortedVariance,
  variableLabel,
} from '../model'
import type { PrescriptionRow, SliderVariable } from '../types'

describe('training status', () => {
  it.each([
    [0, false],
    [15, false],
    [16, true],
    [17, true],
    [18, false],
    [19, true],
    [20, true],
    [21, true],
  ])('status %i terminal=%s', (status, terminal) => {
    expect(isTerminal(status)).toBe(terminal)
  })

  it('treats a missing status as terminal so polling stops', () => {
    expect(isTerminal(undefined)).toBe(true)
    expect(isTerminal(null)).toBe(true)
    expect(refreshInterval(undefined)).toBe(false)
    expect(refreshInterval(9)).toBe(5000)
    expect(refreshInterval(16)).toBe(false)
  })

  it.each([
    [4, 'running'],
    [16, 'finished'],
    [17, 'finished'],
    [18, 'stopping'],
    [19, 'stopped'],
    [20, 'empty'],
    [99, 'unknown'],
  ] as const)('status %i is %s', (status, phase) => {
    expect(phaseOf(status)).toBe(phase)
  })

  it('lets only a running training be stopped', () => {
    expect(canStop(15)).toBe(true)
    expect(canStop(18)).toBe(false)
    expect(canStop(19)).toBe(false)
    expect(canStop(16)).toBe(false)
  })

  it('shows results from correlation analysis onwards', () => {
    expect(hasResults(13)).toBe(false)
    expect(hasResults(14)).toBe(true)
    expect(hasResults(19)).toBe(true)
  })

  it('names a generation after the score and status so a better model refreshes the results', () => {
    expect(generationOf({ score: 0.9, statusId: 15 })).toBe('0.9:15')
    expect(generationOf(undefined)).toBe('')
  })
})

describe('labels', () => {
  it('names the evolved network and splits other kinds into words', () => {
    expect(modelTypeLabel(null)).toBe('Neural Network (evolved topology)')
    expect(modelTypeLabel('ExhaustiveTopologyNetwork')).toBe('Neural Network (evolved topology)')
    expect(modelTypeLabel('GradientBoostingPipeline')).toBe('Gradient Boosting')
  })

  it('labels a categorical level with its source field', () => {
    expect(
      variableLabel({ name: 'x', categoricalSourceName: 'Country', categoricalLevel: 'GB' }),
    ).toBe('Country = GB')
    expect(
      variableLabel({ name: 'Amount', categoricalSourceName: null, categoricalLevel: null }),
    ).toBe('Amount')
  })

  it.each([
    [0.95, 'error'],
    [-0.95, 'error'],
    [0.8, 'warning'],
    [0.7, 'success'],
    [0.1, 'success'],
  ] as const)('correlation %f is %s', (value, tone) => {
    expect(correlationTone(value)).toBe(tone)
  })
})

describe('groupPrescriptions', () => {
  const row = (overrides: Partial<PrescriptionRow>): PrescriptionRow => ({
    id: 1,
    name: 'Amount',
    variableMean: 1,
    variableStandardDeviation: 2,
    variableMaximum: 3,
    variableMinimum: 0,
    prescriptionMean: 10,
    prescriptionStandardDeviation: 1,
    prescriptionMaximum: 20,
    prescriptionMinimum: 0,
    sensitivity: 0.5,
    positive: true,
    promotedTrialInstanceVariableId: 100,
    ...overrides,
  })

  it('folds the positive and negative rows of a variable into one line pointing at the positive chart', () => {
    const grouped = groupPrescriptions([
      row({ positive: false, promotedTrialInstanceVariableId: 99, prescriptionMean: 3 }),
      row({ positive: true, promotedTrialInstanceVariableId: 100, prescriptionMean: 10 }),
      row({ id: 2, name: 'Count', positive: null, promotedTrialInstanceVariableId: 7 }),
    ])

    expect(grouped).toHaveLength(2)
    expect(grouped[0]).toMatchObject({
      id: 1,
      chartId: 100,
      positivePrescriptionMean: 10,
      negativePrescriptionMean: 3,
    })
    expect(grouped[1]).toMatchObject({ id: 2, chartId: 7, positivePrescriptionMean: 10 })
  })
})

describe('sortedVariance', () => {
  it('orders by strength of correlation without disturbing the input', () => {
    const rows = [
      { name: 'b', correlation: 0.1, correlationAbsRank: 2 },
      { name: 'a', correlation: 0.9, correlationAbsRank: 1 },
    ]

    expect(sortedVariance(rows).map((r) => r.name)).toEqual(['a', 'b'])
    expect(rows[0]!.name).toBe('b')
  })
})

describe('sliders', () => {
  it.each([
    [0, 1, 0.005],
    [0, 100, 0.5],
    [0, 1000, 5],
    [5, 5, 1],
    [10, 0, 1],
  ])('range %f to %f steps by %f', (min, max, step) => {
    expect(sliderStep(min, max)).toBeCloseTo(step)
  })

  const variable = (overrides: Partial<SliderVariable>): SliderVariable => ({
    id: 1,
    name: 'Amount',
    mean: 5,
    maximum: 10,
    minimum: 0,
    emptyRange: false,
    categoricalSourceName: null,
    categoricalKind: null,
    categoricalLevel: null,
    categoricalReferenceLabel: null,
    ...overrides,
  })

  it('builds a slider per numeric variable and one choice per categorical source', () => {
    const controls = controlsOf([
      variable({}),
      variable({ id: 2, name: 'Flat', emptyRange: true }),
      variable({
        id: 3,
        name: 'c1',
        categoricalSourceName: 'Country',
        categoricalLevel: 'GB',
        categoricalKind: 1,
        categoricalReferenceLabel: 'Other',
      }),
      variable({
        id: 4,
        name: 'c2',
        categoricalSourceName: 'Country',
        categoricalLevel: 'Missing',
        categoricalKind: 3,
        categoricalReferenceLabel: 'Other',
      }),
    ])

    expect(controls).toHaveLength(2)
    expect(controls[0]).toMatchObject({ kind: 'number', name: 'Amount', value: 5, min: 0, max: 10 })
    expect(controls[1]).toEqual({
      kind: 'category',
      name: 'Country',
      reference: 'Other',
      options: [
        { text: 'Other', value: 'Other' },
        { text: 'GB', value: 'GB' },
        { text: 'Missing', value: '' },
      ],
    })
  })

  it('starts every control at its mean or reference level', () => {
    expect(
      initialParams([
        { kind: 'number', name: 'Amount', min: 0, max: 10, step: 1, value: 5 },
        { kind: 'category', name: 'Country', reference: 'Other', options: [] },
      ]),
    ).toEqual({ Amount: 5, Country: 'Other' })
  })
})

describe('performance charts', () => {
  it('leaves out value captured until a value variable is chosen', () => {
    expect(performanceCharts(null).map((c) => c.key)).not.toContain('valueCaptured')
    expect(performanceCharts(7).map((c) => c.key)).toContain('valueCaptured')
    expect(performanceCharts(null)).toHaveLength(10)
  })

  it('adds the value basis only to the charts that take one', () => {
    expect(chartUrl('Ks', 3, true, { id: 7, name: 'Amount GBP' })).toBe(
      '/api/Chart/Exhaustive/Ks/3?variableId=7&variableName=Amount%20GBP',
    )
    expect(chartUrl('Measures', 3, false, { id: 7, name: 'x' })).toBe(
      '/api/Chart/Exhaustive/Measures/3',
    )
    expect(chartUrl('Ks', 3, true, null)).toBe('/api/Chart/Exhaustive/Ks/3')
  })
})

describe('narrative and batch recall text', () => {
  const narrative = { holistic: ['One.'], groups: { Gains: ['Two.'] } }

  it('reads the holistic paragraphs or a named group, empty when absent', () => {
    expect(narrativeParagraphs(narrative, null)).toEqual(['One.'])
    expect(narrativeParagraphs(narrative, 'Gains')).toEqual(['Two.'])
    expect(narrativeParagraphs(narrative, 'Nope')).toEqual([])
    expect(narrativeParagraphs(undefined, null)).toEqual([])
  })

  it('prefers the listed errors, then the message, then the fallback', () => {
    expect(batchErrorText({ errors: ['a', 'b'] }, 'x')).toBe('a b')
    expect(batchErrorText({ message: 'm' }, 'x')).toBe('m')
    expect(batchErrorText({}, 'x')).toBe('x')
    expect(batchErrorText(null, 'x')).toBe('x')
  })
})

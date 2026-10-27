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
  anyActive,
  backtestWindows,
  builderJson,
  chartBody,
  dependentRows,
  figures,
  instanceRows,
  isStoppable,
  ratio,
  submitBody,
  useRows,
  validationText,
} from '../model'
import type { BacktestInstance, BacktestResult, ModelDependency } from '../types'

const group = {
  condition: 'AND' as const,
  rules: [{ id: 'Amount', operator: 'greater', value: '5' }],
}

const instance = (overrides: Partial<BacktestInstance> = {}): BacktestInstance => ({
  id: 1,
  ruleType: 'ActivationRule',
  ruleId: 5,
  status: 'Succeeded',
  progress: 1,
  scanned: 100,
  evaluated: 80,
  createdDate: '2026-10-21T10:00:00Z',
  startedDate: null,
  error: null,
  request: {
    entityAnalysisModelId: 1,
    ruleType: 'ActivationRule',
    ruleId: 5,
    ruleText: 'A',
    filterJson: null,
    classJson: null,
    from: null,
    to: null,
    limit: 1000,
    sampleSize: 10,
  },
  fired: 20,
  positives: 25,
  truePositives: 15,
  falsePositives: 5,
  falseNegatives: 10,
  trueNegatives: 50,
  ...overrides,
})

const result = (overrides: Partial<BacktestResult> = {}): BacktestResult => ({
  valid: true,
  scanned: 1000,
  filteredOut: 200,
  filterErrors: 0,
  evaluated: 800,
  limitReached: false,
  earliestReferenceDate: null,
  latestReferenceDate: null,
  fired: 120,
  notFired: 680,
  runtimeErrors: 0,
  aborted: false,
  abortReason: null,
  classDefined: true,
  positives: 100,
  falsePositives: 40,
  falseNegatives: 20,
  trueNegatives: 660,
  truePositives: 80,
  durationMicroseconds: 1000,
  tagsInSample: [{ name: 'Fraud', count: 7 }],
  truePositiveSamples: [],
  falsePositiveSamples: [],
  falseNegativeSamples: [],
  errorSamples: [],
  errors: [],
  ...overrides,
})

describe('ratio and builderJson', () => {
  it('has no ratio over nothing, and no json for an empty builder', () => {
    expect(ratio(1, 4)).toBe(0.25)
    expect(ratio(1, 0)).toBeNull()
    expect(builderJson(null)).toBeNull()
    expect(builderJson({ condition: 'AND', rules: [] })).toBeNull()
    expect(JSON.parse(builderJson(group)!)).toEqual(group)
  })
})

describe('submitBody', () => {
  const base = {
    modelId: 7,
    ruleType: 'ActivationRule',
    ruleId: 5,
    ruleText: 'Amount > 5',
    filter: group,
    positive: null,
    window: backtestWindows[1]!,
    limit: 5000,
    sampleSize: 10,
  }

  it('sends the rule as it is in the editor, the two builders as json and the window as interval and amount', () => {
    const body = submitBody(base)

    expect(body.request).toMatchObject({
      entityAnalysisModelId: 7,
      ruleType: 'ActivationRule',
      ruleId: 5,
      ruleText: 'Amount > 5',
      classJson: null,
      limit: 5000,
      sampleSize: 10,
    })
    expect(JSON.parse(body.request.filterJson!)).toEqual(group)
    expect(body).toMatchObject({ windowInterval: 'd', windowValue: 30 })
  })

  it('falls back to the default limit, asks for no examples when blank, and allows the whole archive', () => {
    const body = submitBody({
      ...base,
      limit: null,
      sampleSize: null,
      window: backtestWindows[4]!,
      ruleId: null,
    })

    expect(body.request).toMatchObject({ limit: 100000, sampleSize: 0, ruleId: null })
    expect(body).toMatchObject({ windowInterval: null, windowValue: null })
  })
})

describe('validationText', () => {
  it('names the property, the message and the line when there is one', () => {
    expect(
      validationText([
        { propertyName: 'RuleText', errorCode: 'x', message: 'Bad.', line: 3 },
        { propertyName: 'Limit', errorCode: 'y', message: 'Too big.', line: null },
      ]),
    ).toBe('RuleText: Bad. (line 3) Limit: Too big.')
  })
})

describe('instances', () => {
  it('knows which statuses are active and which can be stopped', () => {
    expect(isStoppable('Pending')).toBe(true)
    expect(isStoppable('Cancelling')).toBe(false)
    expect(anyActive([instance({ status: 'Succeeded' }), instance({ status: 'Cancelling' })])).toBe(
      true,
    )
    expect(anyActive([instance()])).toBe(false)
  })

  it('works out precision and recall, and flags a backtest of a rule that has since been edited', () => {
    const [row] = instanceRows([instance()], 'B')

    expect(row).toMatchObject({ classed: true, edited: true, stoppable: false })
    expect(row!.precision).toBeCloseTo(0.75, 6)
    expect(row!.recall).toBeCloseTo(0.6, 6)
    expect(instanceRows([instance()], 'A')[0]!.edited).toBe(false)
  })

  it('leaves precision and recall blank when nothing was classed', () => {
    const [row] = instanceRows([instance({ truePositives: null })], 'A')

    expect(row).toMatchObject({ classed: false, precision: null, recall: null })
  })
})

describe('figures and chartBody', () => {
  it('computes the six rates from the confusion counts', () => {
    const rates = figures(result())

    expect(rates.precision).toBeCloseTo(80 / 120, 6)
    expect(rates.recall).toBeCloseTo(0.8, 6)
    expect(rates.f1).toBeCloseTo((2 * (80 / 120) * 0.8) / (80 / 120 + 0.8), 6)
    expect(rates.specificity).toBeCloseTo(660 / 700, 6)
    expect(rates.firedRate).toBeCloseTo(0.15, 6)
    expect(rates.positiveRate).toBeCloseTo(0.125, 6)
  })

  it('has no rate over an empty denominator', () => {
    const rates = figures(
      result({
        evaluated: 0,
        fired: 0,
        positives: 0,
        truePositives: 0,
        falsePositives: 0,
        falseNegatives: 0,
        trueNegatives: 0,
      }),
    )

    expect(Object.values(rates).every((value) => value === null)).toBe(true)
  })

  it('hands the server the counts, the theme and the tags to chart', () => {
    expect(chartBody(result(), true)).toMatchObject({
      dark: true,
      scanned: 1000,
      truePositives: 80,
      classDefined: true,
      tags: [{ name: 'Fraud', count: 7 }],
    })
  })
})

describe('dependency rows', () => {
  const use = (overrides: Partial<ModelDependency> = {}): ModelDependency => ({
    dependent: { kind: 'ActivationRule', name: 'High value', active: true },
    target: { kind: 'TtlCounter', name: 'Spend', active: false },
    dependencyKind: 'Abstraction',
    name: 'Spend',
    line: 0,
    depth: 1,
    via: null,
    ...overrides,
  })

  it('says what a rule uses, flagging an inactive target, a missing one and showing lines from one', () => {
    const rows = useRows([use(), use({ target: null, name: 'Gone', line: null })])

    expect(rows[0]).toMatchObject({
      entity: 'TtlCounter Spend (inactive)',
      missing: false,
      line: 1,
    })
    expect(rows[1]).toMatchObject({ entity: '', missing: true, line: null })
  })

  it('says what uses a rule, through what, and how far removed', () => {
    expect(
      dependentRows([use({ via: 'Counter chain', depth: 2 }), use({ via: null })]),
    ).toMatchObject([
      { entity: 'ActivationRule High value', through: 'Counter chain', depth: 2 },
      { through: 'Spend', depth: 1 },
    ])
  })
})

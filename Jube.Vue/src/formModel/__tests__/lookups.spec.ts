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
import { dependentsOf, isBlank, lookupChoices, lookupSelected, lookupUrl } from '../lookups'
import type { FormLookup, FormModel } from '../types'

const lookup = (overrides: Partial<FormLookup> = {}): FormLookup => ({
  source: '/api/CaseWorkflowStatus/ByCaseWorkflowGuid',
  textField: 'name',
  valueField: 'guid',
  parentField: 'caseWorkflowGuid',
  allowEmpty: false,
  ...overrides,
})

const guid = '11111111-1111-1111-1111-111111111111'

describe('lookup url', () => {
  it('is the source itself when the lookup has no parent', () => {
    expect(
      lookupUrl(lookup({ parentField: null, source: '/api/EntityAnalysisModel' }), undefined),
    ).toBe('/api/EntityAnalysisModel')
  })

  it('adds the parent value as the last path segment', () => {
    expect(lookupUrl(lookup(), guid)).toBe(`/api/CaseWorkflowStatus/ByCaseWorkflowGuid/${guid}`)
    expect(
      lookupUrl(lookup({ source: '/api/Names', parentField: 'entityAnalysisModelId' }), 7),
    ).toBe('/api/Names/7')
  })

  it('waits while the parent is empty, including the empty guid', () => {
    for (const blank of [undefined, null, '', '00000000-0000-0000-0000-000000000000']) {
      expect(lookupUrl(lookup(), blank)).toBeUndefined()
      expect(isBlank(blank)).toBe(true)
    }

    expect(isBlank(0)).toBe(false)
  })
})

describe('lookup choices', () => {
  it('reads value and text from the named fields', () => {
    expect(lookupChoices([{ guid, name: 'Fraud' }], lookup())).toEqual([
      { value: guid, text: 'Fraud' },
    ])
  })

  it('accepts a plain list of strings as value and text alike', () => {
    expect(
      lookupChoices(['AccountId', 'CardId'], lookup({ textField: 'value', valueField: 'value' })),
    ).toEqual([
      { value: 'AccountId', text: 'AccountId' },
      { value: 'CardId', text: 'CardId' },
    ])
  })

  it('skips rows with no value and survives a body that is not a list', () => {
    expect(lookupChoices([{ name: 'No guid' }, null], lookup())).toEqual([])
    expect(lookupChoices(undefined, lookup())).toEqual([])
    expect(lookupChoices({ error: true }, lookup())).toEqual([])
  })

  it('falls back to the value when the text is missing', () => {
    expect(lookupChoices([{ guid }], lookup())).toEqual([{ value: guid, text: guid }])
  })
})

describe('dependent lookups', () => {
  const model = {
    fields: [
      { name: 'workflow', lookup: lookup({ parentField: 'entityAnalysisModelId' }) },
      { name: 'status', lookup: lookup({ parentField: 'workflow' }) },
      { name: 'step', lookup: lookup({ parentField: 'status' }) },
      { name: 'other', lookup: lookup({ parentField: null }) },
      { name: 'plain', lookup: null },
    ],
  } as unknown as FormModel

  it('lists every field that hangs from a field, directly or through another', () => {
    expect(dependentsOf(model, 'workflow')).toEqual(['status', 'step'])
    expect(dependentsOf(model, 'status')).toEqual(['step'])
    expect(dependentsOf(model, 'other')).toEqual([])
    expect(dependentsOf(model, 'entityAnalysisModelId')).toEqual(['workflow', 'status', 'step'])
  })
})

describe('lookup of ranges', () => {
  const ranges = lookup({
    source: '/api/EntityAnalysisModelResponseElevationRange/ByEntityAnalysisModelId',
    textField: 'label',
    valueField: 'minimum',
    upperField: 'maximum',
  })

  const choices = lookupChoices(
    [
      { label: 'Review (100-199)', minimum: 100, maximum: 199 },
      { label: 'Decline (500)', minimum: 500, maximum: 500 },
    ],
    ranges,
  )

  it('carries the highest value of each range', () => {
    expect(choices).toEqual([
      { value: 100, text: 'Review (100-199)', upper: 199 },
      { value: 500, text: 'Decline (500)', upper: 500 },
    ])
  })

  it('selects the choice whose own value is stored', () => {
    expect(lookupSelected(choices, 100)).toBe('100')
    expect(lookupSelected(choices, 500)).toBe('500')
  })

  it('selects the range a stored value in the middle of it falls inside', () => {
    expect(lookupSelected(choices, 150)).toBe('100')
    expect(lookupSelected(choices, 199)).toBe('100')
  })

  it('selects nothing for a value inside no range and for an empty one', () => {
    expect(lookupSelected(choices, 300)).toBe('300')
    expect(lookupSelected(choices, null)).toBe('')
    expect(lookupSelected(choices, undefined)).toBe('')
  })
})

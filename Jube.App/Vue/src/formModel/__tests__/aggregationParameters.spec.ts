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
import {
  effectiveValue,
  limitsOf,
  parameterLabel,
  parameterMapOf,
  parameterSetOf,
  parameterValue,
  parseTyped,
  prunedFor,
  sameMap,
  withParameter,
} from '../aggregationParameters'
import type {FormEditor, FormParameter, FormParameterSet} from '../types'

const trimFraction: FormParameter = {
  name: 'trimFraction',
  kind: 'number',
  default: 0.1,
  minimum: 0,
  maximum: 0.5,
  minimumIsExclusive: false,
  maximumIsExclusive: true,
  description: 'Share discarded from each tail.',
}

const unbiased: FormParameter = {
  name: 'unbiased',
  kind: 'boolean',
  default: 1,
  minimum: null,
  maximum: null,
  minimumIsExclusive: false,
  maximumIsExclusive: false,
  description: 'Sample or population estimator.',
}

const position: FormParameter = {
  name: 'position',
  kind: 'integer',
  default: 1,
  minimum: 1,
  maximum: 2147483647,
  minimumIsExclusive: false,
  maximumIsExclusive: false,
  description: 'One-based position.',
}

const trimmedMean: FormParameterSet = {
  value: 19,
  name: 'TrimmedMean',
  description: 'Mean after discarding the tails.',
  orderSensitive: false,
  parameters: [trimFraction],
}

const range: FormParameterSet = {
  value: 24,
  name: 'Range',
  description: 'Largest minus smallest.',
  orderSensitive: false,
  parameters: [],
}

const editor: FormEditor = {
  kind: 'aggregationParameters',
  parseType: null,
  roles: {parameters: 'searchFunctionParameters'},
  reads: {function: 'searchFunctionTypeId'},
  parameterSets: [trimmedMean, range],
}

describe('aggregation parameters', () => {
  it('finds the parameter set of the selected function and none for a function outside the catalogue', () => {
    expect(parameterSetOf(editor, {searchFunctionTypeId: 19})).toBe(trimmedMean)
    expect(parameterSetOf(editor, {searchFunctionTypeId: '24'})).toBe(range)
    expect(parameterSetOf(editor, {searchFunctionTypeId: 3})).toBeUndefined()
    expect(
      parameterSetOf({...editor, parameterSets: undefined}, {searchFunctionTypeId: 19}),
    ).toBeUndefined()
  })

  it('reads the stored map and ignores anything that is not a finite number', () => {
    expect(parameterMapOf(editor, {searchFunctionParameters: {trimFraction: 0.2}})).toEqual({
      trimFraction: 0.2,
    })
    expect(parameterMapOf(editor, {searchFunctionParameters: null})).toEqual({})
    expect(parameterMapOf(editor, {searchFunctionParameters: 'text'})).toEqual({})
    expect(parameterMapOf(editor, {searchFunctionParameters: [1]})).toEqual({})
    expect(
      parameterMapOf(editor, {searchFunctionParameters: {a: Number.NaN, b: 'x', c: 2}}),
    ).toEqual({c: 2})
  })

  it('looks a parameter up without regard to case and falls back to its default', () => {
    expect(parameterValue({TRIMFRACTION: 0.3}, trimFraction)).toBe(0.3)
    expect(parameterValue({}, trimFraction)).toBeNull()
    expect(effectiveValue({}, trimFraction)).toBe(0.1)
    expect(effectiveValue({unbiased: 0}, unbiased)).toBe(0)
  })

  it('keeps the map minimal: a cleared or default value removes the entry and an empty map is null', () => {
    expect(withParameter({}, trimFraction, 0.2)).toEqual({trimFraction: 0.2})
    expect(withParameter({TrimFraction: 0.2}, trimFraction, 0.3)).toEqual({trimFraction: 0.3})
    expect(withParameter({trimFraction: 0.2}, trimFraction, null)).toBeNull()
    expect(withParameter({trimFraction: 0.2}, trimFraction, 0.1)).toBeNull()
    expect(withParameter({trimFraction: 0.2}, unbiased, 0)).toEqual({
      trimFraction: 0.2,
      unbiased: 0,
    })
  })

  it('drops the parameters the newly selected function does not take', () => {
    expect(prunedFor({trimFraction: 0.2, lag: 3}, trimmedMean)).toEqual({trimFraction: 0.2})
    expect(prunedFor({lag: 3}, trimmedMean)).toBeNull()
    expect(prunedFor({trimFraction: 0.2}, range)).toBeNull()
    expect(prunedFor({trimFraction: 0.2}, undefined)).toBeNull()
  })

  it('compares maps by content', () => {
    expect(sameMap({a: 1, b: 2}, {b: 2, a: 1})).toBe(true)
    expect(sameMap(null, {})).toBe(true)
    expect(sameMap({a: 1}, {a: 2})).toBe(false)
    expect(sameMap({a: 1}, null)).toBe(false)
  })

  it('turns a parameter name into a readable label', () => {
    expect(parameterLabel('trimFraction')).toBe('Trim fraction')
    expect(parameterLabel('scaleToNormal')).toBe('Scale to normal')
    expect(parameterLabel('lag')).toBe('Lag')
  })

  it('states the limits a value must respect and none for a flag or an open-ended whole number', () => {
    expect(limitsOf(trimFraction)).toEqual([
      {bound: 'atLeast', value: 0},
      {bound: 'lessThan', value: 0.5},
    ])
    expect(limitsOf(position)).toEqual([{bound: 'atLeast', value: 1}])
    expect(limitsOf(unbiased)).toEqual([])
    expect(
      limitsOf({...trimFraction, minimumIsExclusive: true, maximumIsExclusive: false}),
    ).toEqual([
      {bound: 'moreThan', value: 0},
      {bound: 'atMost', value: 0.5},
    ])
  })

  it('reads what was typed: blank clears, text is ignored, and a whole number must be whole', () => {
    expect(parseTyped('0.25', trimFraction)).toBe(0.25)
    expect(parseTyped('  ', trimFraction)).toBeNull()
    expect(parseTyped('abc', trimFraction)).toBeUndefined()
    expect(parseTyped('3', position)).toBe(3)
    expect(parseTyped('1.5', position)).toBeUndefined()
  })
})

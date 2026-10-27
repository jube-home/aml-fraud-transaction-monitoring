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
  levelOf,
  levelsOf,
  normalise,
  readAddress,
  reopen,
  sameAddress,
  writeAddress,
} from '../address'

const full = { key: 'AccountId', value: 'ACC-1', model: 'm1' }

describe('readAddress', () => {
  it('reads key, value and model from the query', () => {
    expect(readAddress({ key: 'AccountId', value: 'ACC-1', model: 'm1' })).toEqual(full)
  })

  it('takes the first of a repeated parameter and trims it', () => {
    expect(readAddress({ key: [' AccountId ', 'x'] })).toEqual({
      key: 'AccountId',
      value: '',
      model: '',
    })
  })

  it('drops a value without a key and a model without a value', () => {
    expect(readAddress({ value: 'ACC-1', model: 'm1' })).toEqual({ key: '', value: '', model: '' })
    expect(readAddress({ key: 'K', model: 'm1' })).toEqual({ key: 'K', value: '', model: '' })
  })

  it('ignores non-string parameters', () => {
    expect(readAddress({ key: 5, value: null })).toEqual({ key: '', value: '', model: '' })
  })
})

describe('writeAddress', () => {
  it('omits empty parts so the address stays short', () => {
    expect(writeAddress({ key: 'K', value: '', model: '' })).toEqual({
      key: 'K',
      value: undefined,
      model: undefined,
    })
  })

  it('round trips with readAddress', () => {
    const written = writeAddress(full)

    expect(readAddress(written)).toEqual(full)
  })
})

describe('normalise and sameAddress', () => {
  it('clears what the level above does not support', () => {
    expect(normalise({ key: '', value: 'v', model: 'm' })).toEqual({
      key: '',
      value: '',
      model: '',
    })
  })

  it('compares all three parts', () => {
    expect(sameAddress(full, { ...full })).toBe(true)
    expect(sameAddress(full, { ...full, model: '' })).toBe(false)
  })
})

describe('levels', () => {
  it('names the level from how much is chosen', () => {
    expect(levelOf({ key: '', value: '', model: '' })).toBe('keys')
    expect(levelOf({ key: 'K', value: '', model: '' })).toBe('values')
    expect(levelOf(full)).toBe('models')
  })

  it('lists every level reached', () => {
    expect(levelsOf({ key: '', value: '', model: '' })).toEqual(['keys'])
    expect(levelsOf({ key: 'K', value: '', model: '' })).toEqual(['keys', 'values'])
    expect(levelsOf(full)).toEqual(['keys', 'values', 'models'])
  })
})

describe('reopen', () => {
  it('drops everything below the chosen beat', () => {
    expect(reopen(full, 0)).toEqual({ key: '', value: '', model: '' })
    expect(reopen(full, 1)).toEqual({ key: 'AccountId', value: '', model: '' })
    expect(reopen(full, 2)).toEqual(full)
  })
})

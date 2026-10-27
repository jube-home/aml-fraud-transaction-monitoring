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
import { decodeStory, encodeStory, whereOf, type Beat } from '../model'

const beat = (id: string, name?: string): Beat => ({ id, title: id.toUpperCase(), name, facts: [] })

describe('story model', () => {
  const story = [beat('models', 'Fraud model'), beat('area', 'Rules'), beat('record', 'Velocity')]

  it('names a beat with its choice when it has one', () => {
    expect(whereOf(story[0]!)).toBe('MODELS: Fraud model')
    expect(whereOf(beat('models'))).toBe('MODELS')
  })

  it('round trips the story through the address', () => {
    expect(decodeStory(encodeStory(['m1', 'rules', 'r2']))).toEqual(['m1', 'rules', 'r2'])
    expect(decodeStory(undefined)).toEqual([])
    expect(decodeStory('a,,b')).toEqual(['a', 'b'])
  })
})

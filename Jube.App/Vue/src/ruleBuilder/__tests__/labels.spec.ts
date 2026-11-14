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
import {argumentLabel, groupKeys, groupLabel, operatorLabel, type Translate} from '../labels'

const messages: Record<string, string> = {
  'ruleBuilder.operators.equal': 'jest równe',
  'ruleBuilder.groups.textComparison': 'Tekst: porównanie',
  'ruleBuilder.arguments.value': 'wartość',
}

const translate: Translate = {
  t: (key) => messages[key] ?? key,
  te: (key) => key in messages,
}

describe('labels', () => {
  it('uses the translation when there is one and the server wording when there is not', () => {
    expect(operatorLabel(translate, 'equal', 'equal')).toBe('jest równe')
    expect(operatorLabel(translate, 'is_just_below_threshold', 'Is just below threshold')).toBe(
      'Is just below threshold',
    )
    expect(groupLabel(translate, 'Text: comparison')).toBe('Tekst: porównanie')
    expect(groupLabel(translate, 'Text: validation')).toBe('Text: validation')
    expect(groupLabel(translate, 'A group nobody knows')).toBe('A group nobody knows')
    expect(argumentLabel(translate, 'value')).toBe('wartość')
    expect(argumentLabel(translate, 'Threshold')).toBe('Threshold')
  })

  it('gives every group a distinct key', () => {
    const keys = Object.values(groupKeys)

    expect(new Set(keys).size).toBe(keys.length)
    expect(keys).toHaveLength(23)
  })
})

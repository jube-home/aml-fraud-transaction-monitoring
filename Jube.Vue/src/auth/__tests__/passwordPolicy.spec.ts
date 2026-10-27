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
import { failedPasswordRules } from '../passwordPolicy'

describe('password policy', () => {
  it('accepts a password that meets every rule', () => {
    expect(failedPasswordRules('Correct-Horse-Battery-9')).toEqual([])
  })

  it('names every rule that fails', () => {
    expect(failedPasswordRules('abc')).toEqual(['minLength', 'uppercase', 'number', 'special'])
  })

  it('rejects a character repeated three times and a common prefix', () => {
    expect(failedPasswordRules('Aaaa-Bbbb-Cccc-1')).toContain('repeat')
    expect(failedPasswordRules('Password-Is-Long-1!')).toContain('common')
  })

  it('rejects a password longer than the maximum', () => {
    expect(failedPasswordRules(`Aa1-${'xYz-'.repeat(40)}`)).toContain('maxLength')
  })
})

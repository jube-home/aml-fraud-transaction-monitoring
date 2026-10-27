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
import { matchesFormat, plainText } from '../text'
import type { CaseFieldEntry } from '../types'

describe('plainText', () => {
  it('keeps the words and drops the markup', () => {
    expect(plainText('<p>Customer <b>confirmed</b>   the transfer</p>')).toBe(
      'Customer confirmed the transfer',
    )
  })

  it('drops script and style content rather than showing it as text', () => {
    expect(plainText('ok<script>alert(1)</script><style>p{color:red}</style>')).toBe('ok')
  })

  it('is empty for nothing', () => {
    expect(plainText(null)).toBe('')
    expect(plainText('')).toBe('')
  })
})

describe('matchesFormat', () => {
  const entry = (conditional: boolean, exists: boolean) =>
    ({ conditionalRegularExpressionFormatting: conditional, existsMatch: exists }) as CaseFieldEntry

  it('applies a plain format always and a conditional one only on a match', () => {
    expect(matchesFormat(entry(false, false))).toBe(true)
    expect(matchesFormat(entry(true, false))).toBe(false)
    expect(matchesFormat(entry(true, true))).toBe(true)
  })
})

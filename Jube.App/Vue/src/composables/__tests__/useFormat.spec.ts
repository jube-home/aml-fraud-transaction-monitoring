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
import {intlTag} from '../useFormat'

const nonLatinDigits = /[٠-٩۰-۹०-९]/

describe('intlTag', () => {
  it('maps English to en-GB and pins the Latin numbering system', () => {
    expect(intlTag('en')).toBe('en-GB-u-nu-latn')
    expect(intlTag('ka')).toBe('ka-u-nu-latn')
  })

  it.each(['ar', 'ur', 'hi', 'ka', 'id', 'sv', 'pl'])(
    'formats numbers with Western digits for %s',
    (locale) => {
      const text = new Intl.NumberFormat(intlTag(locale)).format(1234567.89)

      expect(text).not.toMatch(nonLatinDigits)
      expect(text).toContain('1')
    },
  )

  it('keeps Western digits in Arabic-script dates', () => {
    const text = new Intl.DateTimeFormat(intlTag('ar'), {dateStyle: 'medium'}).format(
      new Date(Date.UTC(2026, 10, 8)),
    )

    expect(text).not.toMatch(nonLatinDigits)
  })
})

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
import {hubMatches} from '../hubDeck'

describe('hubMatches', () => {
  it('matches everything for a blank query', () => {
    expect(hubMatches('', 'Users')).toBe(true)
    expect(hubMatches('   ', 'Users')).toBe(true)
  })

  it('is case-insensitive and needs every word', () => {
    expect(hubMatches('API keys', 'Users', 'api KEYS held')).toBe(true)
    expect(hubMatches('api roles', 'Users', 'api keys')).toBe(false)
  })

  it('tolerates a missing meta', () => {
    expect(hubMatches('use', 'Users', undefined)).toBe(true)
    expect(hubMatches('zzz', 'Users', undefined)).toBe(false)
  })
})

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
import {governanceApi, maximumText, statementReady} from '../governanceApi'

describe('governance statement', () => {
  it('needs an owner, a purpose, an oversight statement and a tier', () => {
    expect(statementReady('Head of Fraud', 'Scores payments', 'Analysts review', 2)).toBe(true)
    expect(statementReady('', 'p', 'o', 2)).toBe(false)
    expect(statementReady('o', ' ', 'o', 2)).toBe(false)
    expect(statementReady('o', 'p', '', 2)).toBe(false)
    expect(statementReady('o', 'p', 'o', 0)).toBe(false)
    expect(statementReady('o', 'p', 'o', 4)).toBe(false)
  })

  it('refuses text over the limit', () => {
    expect(statementReady('o', 'x'.repeat(maximumText + 1), 'o', 1)).toBe(false)
  })

  it('builds the card link with an encoded guid', () => {
    expect(governanceApi.cardUrl(1, 'a b')).toBe(
      '/api/ModelGovernance/Card?kindId=1&modelGuid=a%20b',
    )
  })
})

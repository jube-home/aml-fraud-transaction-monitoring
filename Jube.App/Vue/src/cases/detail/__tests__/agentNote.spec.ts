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
import {acceptedNote, maximumNote} from '../agentNote'

describe('acceptedNote', () => {
  it('names the outcome and carries the rationale', () => {
    expect(acceptedNote(1, ' Moved funds fast. ')).toBe(
      'Triage recommendation accepted (confirmed): Moved funds fast.',
    )
    expect(acceptedNote(2, 'Salary.')).toContain('(not confirmed)')
  })

  it('is clipped to what the close note allows', () => {
    expect(acceptedNote(1, 'x'.repeat(5000))).toHaveLength(maximumNote)
  })
})

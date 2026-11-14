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
import {blockCloser, leadingSpace} from '../blocks'

function closer(text: string, line: number) {
  return blockCloser(text.split('\n'), line)
}

describe('block closing', () => {
  it('closes an If block that has no End If', () => {
    expect(closer('If (Payload.Amount > 1) Then', 0)).toBe('End If')
    expect(closer('  if x then  ', 0)).toBe('End If')
  })

  it('closes a Select Case block that has no End Select', () => {
    expect(closer('Select Case Payload.Country', 0)).toBe('End Select')
  })

  it('leaves a block that is already closed', () => {
    expect(closer('If x Then\n  Return True\nEnd If', 0)).toBeNull()
    expect(closer('Select Case x\n  Case 1\nEnd Select', 0)).toBeNull()
  })

  it('closes only the block that is still open when blocks nest', () => {
    expect(closer('If x Then\n  If y Then\nEnd If', 1)).toBe('End If')
    expect(closer('If x Then\n  If y Then\n  End If\nEnd If', 1)).toBeNull()
  })

  it('leaves a single-line If, an ElseIf and other lines', () => {
    expect(closer('If x Then Return True', 0)).toBeNull()
    expect(closer('If x Then Exit For', 0)).toBeNull()
    expect(closer('If x Then\nElseIf y Then', 1)).toBeNull()
    expect(closer('Return True', 0)).toBeNull()
    expect(closer('', 0)).toBeNull()
  })

  it('ignores a comment after Then and an If inside text or a comment', () => {
    expect(closer("If x Then ' explain", 0)).toBe('End If')
    expect(closer("' If x Then", 0)).toBeNull()
  })

  it('reads the indentation of a line', () => {
    expect(leadingSpace('    If x Then')).toBe('    ')
    expect(leadingSpace('\tIf x Then')).toBe('\t')
    expect(leadingSpace('If x Then')).toBe('')
  })
})

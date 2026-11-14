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
import {declaredVariables, findVariable} from '../variables'

describe('declared variables', () => {
  it('reads the name, type and lifetime of each declaration', () => {
    expect(
      declaredVariables(
        [
          'Static declines As Integer',
          '  dim Doubled as double = Payload.Amount * 2',
          'Static previous As String = ""',
          'Matched = declines = 3',
        ].join('\n'),
      ),
    ).toEqual([
      {name: 'declines', type: 'Integer', kind: 'scan', line: 0},
      {name: 'Doubled', type: 'Double', kind: 'record', line: 1},
      {name: 'previous', type: 'String', kind: 'scan', line: 2},
    ])
  })

  it('ignores what the server would not read as a declaration', () => {
    expect(
      declaredVariables(
        [
          'Dim x',
          'Dim y As Object',
          'Dim a_b As Integer',
          'Dim 1x As Integer',
          'If x Then Dim z As Integer',
          'Dimension As Integer',
          'Dim w As Integers',
        ].join('\n'),
      ),
    ).toEqual([])
  })

  it('keeps the first declaration of a name', () => {
    expect(declaredVariables('Dim total As Integer\nStatic TOTAL As Double')).toEqual([
      {name: 'total', type: 'Integer', kind: 'record', line: 0},
    ])
  })

  it('finds a variable without regard to case', () => {
    const variables = declaredVariables('Static declines As Integer')

    expect(findVariable(variables, 'DECLINES')?.type).toBe('Integer')
    expect(findVariable(variables, 'other')).toBeUndefined()
  })
})

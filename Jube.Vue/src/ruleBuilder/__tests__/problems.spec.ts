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
import { builderProblem, compilerProblem, toProblems } from '../problems'

describe('compilerProblem', () => {
  it('moves the line prefix into the line and capitalises the message', () => {
    expect(
      compilerProblem({ start: 0, length: 1, line: 2, message: 'Line 3: expression expected.' }),
    ).toEqual({
      source: 'compiler',
      line: 3,
      column: null,
      endColumn: null,
      text: 'Expression expected.',
    })
  })

  it('keeps the columns when the backend located the span', () => {
    expect(
      compilerProblem({
        start: 0,
        length: 1,
        line: 0,
        column: 4,
        endColumn: 9,
        message: "'Bogus' is not a member of 'String'.",
      }),
    ).toMatchObject({
      line: 1,
      column: 4,
      endColumn: 9,
      text: "'Bogus' is not a member of 'String'.",
    })
  })

  it('leaves a message with no prefix alone', () => {
    expect(
      compilerProblem({ start: 0, length: 0, line: 0, message: 'The rule never sets Matched.' })
        .text,
    ).toBe('The rule never sets Matched.')
  })
})

describe('builderProblem', () => {
  it('labels the argument and drops the full stop', () => {
    expect(builderProblem('Value: a value is needed.')).toMatchObject({
      source: 'builder',
      line: null,
      text: 'Value: a value is needed',
    })
  })

  it('handles a message with no argument', () => {
    expect(builderProblem('choose a field.').text).toBe('Choose a field.')
  })
})

describe('toProblems', () => {
  it('lists compiler problems first and de-duplicates builder ones', () => {
    const problems = toProblems(
      [{ start: 0, length: 1, line: 0, message: 'Line 1: Expression expected.' }],
      ['Value: a value is needed.', 'Value: a value is needed.'],
    )

    expect(problems.map((p) => p.source)).toEqual(['compiler', 'builder'])
  })
})

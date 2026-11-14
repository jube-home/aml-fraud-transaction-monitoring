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
import {distance, localProblems, nearest} from '../diagnostics'
import {buildVocabulary} from '../typeResolver'
import {baseWords, cursorWords, fields} from './vocabularyFixture'

const general = buildVocabulary(baseWords)
const abstraction = buildVocabulary([...baseWords, ...cursorWords])

function problems(text: string, vocabulary = abstraction) {
  return localProblems(text, fields, vocabulary)
}

describe('edit distance', () => {
  it('counts single character edits', () => {
    expect(distance('amount', 'amount')).toBe(0)
    expect(distance('amont', 'amount')).toBe(1)
    expect(distance('amuont', 'amount')).toBe(2)
    expect(distance('', 'abc')).toBe(3)
  })

  it('offers the nearest name only when it is close', () => {
    expect(nearest('Payload.Amont', ['Payload.Amount', 'Payload.Country'])).toBe('Payload.Amount')
    expect(nearest('Payload.Zzzzzzzzz', ['Payload.Amount', 'Payload.Country'])).toBeUndefined()
  })
})

describe('local rule problems', () => {
  it('finds nothing in rules written before the cursor existed', () => {
    const rules = [
      'Return Payload.Amount > 1000',
      'If (Payload.Amount > 1000 AndAlso Payload.Country = "GB") Then\n  Return True\nEnd If',
      'Matched = Payload.AccountId.Contains("x") Or Not Payload.Country = "FR"',
      'Select Case Payload.Country\n  Case "GB"\n    Return True\nEnd Select',
      'Return Abstraction.Total.Round(2) >= 1e5',
      'Return Graph.Customer.Degree > 3',
    ]

    for (const rule of rules) expect(problems(rule, general)).toEqual([])
  })

  it('finds nothing in a cursor rule for an abstraction', () => {
    const rule = [
      'If Cursor.IsCurrent Then Continue For',
      'Matched = Cursor.Position = 3',
      'Static total As Double',
      'total += Payload.Amount',
      'If total.Round(0) >= 10000 Then Exit For',
      'Cursor.Result = Cursor.Result + Payload.Amount',
    ].join('\n')

    expect(problems(rule)).toEqual([])
  })

  it('marks an unknown field and suggests the nearest one of the same object', () => {
    expect(problems('Return Payload.Amont > 1')).toEqual([
      {
        kind: 'unknownField',
        line: 0,
        column: 7,
        endColumn: 20,
        name: 'Payload.Amont',
        suggestion: 'Payload.Amount',
      },
    ])
  })

  it('marks an unknown field without a suggestion when nothing is close', () => {
    const [problem] = problems('Return True\nReturn Payload.Qqqqqqqq > 1')

    expect(problem).toMatchObject({kind: 'unknownField', line: 1, name: 'Payload.Qqqqqqqq'})
    expect(problem!.suggestion).toBeUndefined()
  })

  it('accepts the entity of a three-part name', () => {
    expect(problems('Return Graph.Customer.Degree > 1')).toEqual([])
    expect(problems('Return Graph.Custmer.Degree > 1')[0]).toMatchObject({
      kind: 'unknownField',
      name: 'Graph.Custmer',
      suggestion: 'Graph.Customer',
    })
  })

  it('leaves an object alone when no field of it is known', () => {
    expect(problems('Return List.Anything.Contains(Payload.Country)')).toEqual([])
  })

  it('marks a word the rule may not use', () => {
    expect(problems('Return Payload.Amount > Foo')).toEqual([
      {kind: 'notAllowed', line: 0, column: 24, endColumn: 27, name: 'Foo'},
    ])
    expect(problems('Return Payload.Country.Explode()')[0]).toMatchObject({
      kind: 'notAllowed',
      name: 'Explode',
    })
  })

  it('marks the scan words outside an abstraction rule and accepts them inside one', () => {
    expect(problems('If Payload.Amount > 1 Then Exit  For', general)).toEqual([
      {kind: 'notAllowed', line: 0, column: 27, endColumn: 36, name: 'Exit For'},
    ])
    expect(problems('If Payload.Amount > 1 Then Exit  For')).toEqual([])
  })

  it('marks a member the cursor does not have', () => {
    expect(problems('Return Cursor.Positon = 1')).toEqual([
      {kind: 'noMember', line: 0, column: 14, endColumn: 21, name: 'Positon', owner: 'Cursor'},
    ])
    expect(problems('Matched = Cursor.Explode(1) > 1')).toEqual([
      {kind: 'noMember', line: 0, column: 17, endColumn: 24, name: 'Explode', owner: 'Cursor'},
    ])
  })

  it('ignores words inside text, dates and comments', () => {
    expect(problems('Return Payload.Country = "Foo Payload.Nope" \' Bar Payload.Nope')).toEqual([])
  })

  it('accepts a word joined by underscores when every part is allowed', () => {
    expect(problems('Return True_False')).toEqual([])
    expect(problems('Return True_Foo')[0]).toMatchObject({kind: 'notAllowed', name: 'True_Foo'})
  })

  it('accepts the line continuation mark and every object the server translates', () => {
    const rule = [
      'Return Abstraction.Total / _',
      '  (Abstraction.Total _',
      '  + AbstractionCalculation.Ratio)',
      'If (Sanction.FuzzyMatchDistance2JoinedName < 1) Then',
      '  Return True',
      'End If',
    ].join('\n')

    expect(problems(rule, general)).toEqual([])
  })

  it('accepts the variables an abstraction rule declares and refuses them in other rules', () => {
    const rule = [
      'Static declines As Integer = 0',
      '  Dim doubled As Double = Payload.Amount * 2',
      'declines += 1',
      'Matched = declines = 3 Or doubled > 100',
    ].join('\n')

    expect(problems(rule)).toEqual([])
    expect(problems(rule, general).map((problem) => problem.name)).toEqual([
      'Static',
      'declines',
      'As',
      'Dim',
      'doubled',
      'As',
      'declines',
      'declines',
      'doubled',
    ])
  })

  it('still marks a name that was never declared', () => {
    expect(problems('Static declines As Integer\nMatched = decline = 3')).toEqual([
      {kind: 'notAllowed', line: 1, column: 10, endColumn: 17, name: 'decline'},
    ])
  })

  it('does not judge words before the vocabulary has arrived', () => {
    expect(problems('Return Foo', buildVocabulary([]))).toEqual([])
  })
})

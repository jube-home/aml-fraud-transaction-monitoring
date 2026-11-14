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
import {tokenise} from '../tokens'

function kinds(line: string): string[] {
  return tokenise(line).map((token) => `${token.type}:${token.text}`)
}

describe('rule dialect tokens', () => {
  it('tells language words, the answer and literals apart', () => {
    expect(kinds('If x > 1 Then Return True')).toEqual([
      'language:If',
      'word:x',
      'literal:1',
      'language:Then',
      'answer:Return',
      'literal:True',
    ])
    expect(kinds('Matched = False')).toEqual(['answer:Matched', 'literal:False'])
  })

  it('reads an object, its field and the methods called on it', () => {
    expect(kinds('Payload.Amount.Start().MatchGreater(1000)')).toEqual([
      'root:Payload',
      'field:Amount',
      'method:Start',
      'method:MatchGreater',
      'literal:1000',
    ])
    expect(kinds('List.HighRisk.Contains(Payload.Country)')).toEqual([
      'root:List',
      'field:HighRisk',
      'method:Contains',
      'root:Payload',
      'field:Country',
    ])
  })

  it('treats cursor properties as data and cursor calls as methods', () => {
    expect(kinds('Cursor.Position >= 5')).toEqual(['root:Cursor', 'field:Position', 'literal:5'])
    expect(kinds('Cursor.Result.Round(2)')).toEqual([
      'root:Cursor',
      'field:Result',
      'method:Round',
      'literal:2',
    ])
    expect(kinds('Cursor.Result.Round (2)')[2]).toBe('method:Round')
  })

  it('marks the two words that steer the scan as one token each, in any case', () => {
    expect(kinds('If x Then Exit For')).toEqual([
      'language:If',
      'word:x',
      'language:Then',
      'scan:Exit For',
    ])
    expect(kinds('continue   for')).toEqual(['scan:continue   for'])
    expect(kinds('Exit Function')).toEqual(['language:Exit', 'word:Function'])
  })

  it('keeps strings, doubled quotes, dates and comments whole', () => {
    expect(kinds('x = "a ""quoted"" If"')).toEqual(['word:x', 'literal:"a ""quoted"" If"'])
    expect(kinds('d = #2026-11-09#')).toEqual(['word:d', 'literal:#2026-11-09#'])
    expect(kinds("Matched = True ' because If")).toEqual([
      'answer:Matched',
      'literal:True',
      "comment:' because If",
    ])
    expect(kinds('x = "unterminated If')).toEqual(['word:x', 'literal:"unterminated If'])
  })

  it('only calls a name an object when a member follows it', () => {
    expect(kinds('Payload')).toEqual(['word:Payload'])
    expect(kinds('payload.amount')).toEqual(['root:payload', 'field:amount'])
    expect(kinds('Other.Thing')).toEqual(['word:Other', 'field:Thing'])
  })

  it('does not mistake a member called like a keyword for the keyword', () => {
    expect(kinds('Payload.Case.In("a")')).toEqual([
      'root:Payload',
      'field:Case',
      'method:In',
      'literal:"a"',
    ])
    expect(kinds('Payload.Return')).toEqual(['root:Payload', 'field:Return'])
  })

  it('reads the singular and long object names the server translates', () => {
    expect(kinds('Sanction.Name < 1')).toEqual(['root:Sanction', 'field:Name', 'literal:1'])
    expect(kinds('AbstractionCalculation.Ratio')).toEqual([
      'root:AbstractionCalculation',
      'field:Ratio',
    ])
  })

  it('reads a declaration as language words around the name the author chose', () => {
    expect(kinds('Static declines As Integer = 0')).toEqual([
      'language:Static',
      'word:declines',
      'language:As',
      'language:Integer',
      'literal:0',
    ])
    expect(kinds('declines += 1')).toEqual(['word:declines', 'literal:1'])
  })

  it('reads a number with an exponent as one literal', () => {
    expect(kinds('x >= 1e5')).toEqual(['word:x', 'literal:1e5'])
    expect(kinds('x >= 2.5E-3')).toEqual(['word:x', 'literal:2.5E-3'])
  })
})

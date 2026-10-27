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
import { blockDepthAfter, formatRuleText, indentLevelFor } from '../format'

const longCondition =
  'If (Payload.Amount > 100 AND ( Payload.Country = "GB" OR Payload.Country = "FR" OR Payload.Country = "DE" ) AND NOT ( Payload.Flag = True AND Payload.Count.In(1, 2, 3) )  AND Payload.Name.StartsWith("Smith, John")) Then\n  Return True\nEnd If'

describe('formatRuleText', () => {
  it('removes the redundant spaces the builder leaves around parentheses', () => {
    expect(
      formatRuleText(
        'If (NOT ( Payload.Amount > 1 AND ( Payload.Country = "GB" OR Payload.Country = "FR" )  )) Then\n  Return True\nEnd If',
      ),
    ).toBe(
      'If (NOT (Payload.Amount > 1 AND (Payload.Country = "GB" OR Payload.Country = "FR"))) Then\n  Return True\nEnd If',
    )
  })

  it('leaves spaces inside string literals alone', () => {
    expect(formatRuleText('If (Payload.Name = "a  (  b  )  c") Then\n Return True\nEnd If')).toBe(
      'If (Payload.Name = "a  (  b  )  c") Then\n  Return True\nEnd If',
    )
  })

  it('keeps AND, OR and parentheses inside literals from being treated as logic', () => {
    const text = `If (${'Payload.Name = "x AND ( y OR z"'.repeat(1)} AND Payload.Other = "${'q '.repeat(40)}") Then\n  Return True\nEnd If`
    const formatted = formatRuleText(text)

    expect(formatted).toContain('"x AND ( y OR z"')
    expect(formatRuleText(formatted)).toBe(formatted)
  })

  it('splits a long condition after each logical operator and breaks only grouping parentheses', () => {
    expect(formatRuleText(longCondition)).toBe(
      [
        'If (',
        '  Payload.Amount > 100 AND',
        '  (Payload.Country = "GB" OR Payload.Country = "FR" OR Payload.Country = "DE") AND',
        '  NOT (Payload.Flag = True AND Payload.Count.In(1, 2, 3)) AND',
        '  Payload.Name.StartsWith("Smith, John")',
        ') Then',
        '  Return True',
        'End If',
      ].join('\n'),
    )
  })

  it('expands a nested group that is itself too long', () => {
    const inner = Array.from(
      { length: 6 },
      (_, i) => `Payload.Country = "COUNTRY${i}XXXXXXXX"`,
    ).join(' OR ')
    const formatted = formatRuleText(
      `If (Payload.Amount > 1 AND (${inner})) Then\n  Return True\nEnd If`,
    )

    expect(formatted.split('\n')).toContain('  (')
    expect(formatted.split('\n')).toContain('    Payload.Country = "COUNTRY0XXXXXXXX" OR')
    expect(formatted.split('\n')).toContain('  )')
  })

  it('is idempotent', () => {
    const once = formatRuleText(longCondition)

    expect(formatRuleText(once)).toBe(once)
    expect(formatRuleText(formatRuleText('If (a=1) Then\nReturn True\nEnd If'))).toBe(
      formatRuleText('If (a=1) Then\nReturn True\nEnd If'),
    )
  })

  it('indents blocks, dedents Else and keeps single-line If as a plain statement', () => {
    expect(
      formatRuleText(
        'If (a = 1) Then\nIf (b = 2) Then\nReturn True\nElse\nReturn False\nEnd If\nEnd If\nIf (c) Then Return True',
      ),
    ).toBe(
      [
        'If (a = 1) Then',
        '  If (b = 2) Then',
        '    Return True',
        '  Else',
        '    Return False',
        '  End If',
        'End If',
        'If (c) Then Return True',
      ].join('\n'),
    )
  })

  it('collapses blank lines and trims the ends', () => {
    expect(formatRuleText('\n\nReturn True\n\n\n\nReturn False\n\n')).toBe(
      'Return True\n\nReturn False',
    )
  })

  it('does not join a conditional that carries a comment or a line continuation', () => {
    const withComment = "If (a = 1 AND ' why\nb = 2) Then\n  Return True\nEnd If"

    expect(formatRuleText(withComment)).toBe(
      "If (a = 1 AND ' why\n  b = 2) Then\n  Return True\nEnd If",
    )
  })

  it('writes nothing for empty text', () => {
    expect(formatRuleText('   \n  ')).toBe('')
  })
})

describe('indentation while typing', () => {
  it('indents after Then and Else and dedents End If and Else', () => {
    expect(blockDepthAfter('If (a) Then')).toBe(1)
    expect(blockDepthAfter('If (a) Then\n  If (b) Then')).toBe(2)
    expect(blockDepthAfter('If (a) Then\n  Return True\nEnd If')).toBe(0)
    expect(indentLevelFor('If (a) Then\n  x = 1', 'Else')).toBe(0)
    expect(indentLevelFor('If (a) Then\n  x = 1', 'End If')).toBe(0)
    expect(indentLevelFor('If (a) Then\n  x = 1', 'y = 2')).toBe(1)
  })

  it('does not indent a single-line If', () => {
    expect(blockDepthAfter('If (a) Then Return True')).toBe(0)
  })

  it('waits for a conditional split over lines to finish before indenting', () => {
    expect(blockDepthAfter('If (\n  a AND\n  b\n) Then')).toBe(1)
  })
})

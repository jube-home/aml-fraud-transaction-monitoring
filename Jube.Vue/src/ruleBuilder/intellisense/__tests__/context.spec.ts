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
import { argumentContext, cursorContext, insideLiteralOrComment, receiverBefore } from '../context'

describe('insideLiteralOrComment', () => {
  it.each([
    ['Return "abc', true],
    ['Return "abc"', false],
    ['Return "a""b', true],
    ["Return True ' note", true],
    ['Return "it\'s', true],
    ['If (a', false],
  ])('%j gives %s', (text, expected) => {
    expect(insideLiteralOrComment(text)).toBe(expected)
  })
})

describe('receiverBefore', () => {
  it.each([
    ['Payload.Name.', 'Payload.Name'],
    ['If (Payload.Name.', 'Payload.Name'],
    ['Payload.Name.Trim().', 'Payload.Name.Trim()'],
    ['x = Payload.Name.Replace("a", "b").', 'Payload.Name.Replace("a", "b")'],
    ['Payload.Name.Replace(")", "b").', 'Payload.Name.Replace(")", "b")'],
    ['Payload.At.AddDays(Payload.Count.Abs()).', 'Payload.At.AddDays(Payload.Count.Abs())'],
  ])('%j has receiver %j', (text, expected) => {
    expect(receiverBefore(text, text.length - 1)).toBe(expected)
  })
})

describe('cursorContext', () => {
  it('recognises a member access with and without a typed prefix', () => {
    expect(cursorContext('If (Payload.Name.')).toEqual({
      kind: 'member',
      receiver: 'Payload.Name',
      prefix: '',
      from: 17,
    })
    expect(cursorContext('If (Payload.Name.Sta')).toEqual({
      kind: 'member',
      receiver: 'Payload.Name',
      prefix: 'Sta',
      from: 17,
    })
  })

  it('reads a dotted field prefix as a member access of its group, left to the field source', () => {
    expect(cursorContext('If (Payload.Am')).toMatchObject({
      kind: 'member',
      receiver: 'Payload',
      prefix: 'Am',
    })
  })

  it('offers a plain word when there is no dot', () => {
    expect(cursorContext('   Ret')).toMatchObject({ kind: 'word', prefix: 'Ret', from: 3 })
    expect(cursorContext('')).toMatchObject({ kind: 'word', prefix: '' })
  })

  it('offers nothing inside a string literal or a comment', () => {
    expect(cursorContext('Return "Payload.Na')).toBeNull()
    expect(cursorContext("Return True ' Payload.")).toBeNull()
  })
})

describe('argumentContext', () => {
  it('finds the call, its receiver and the active argument', () => {
    expect(argumentContext('If (Payload.Name.Replace("a", ')).toMatchObject({
      name: 'Replace',
      receiver: 'Payload.Name',
      index: 1,
    })
    expect(argumentContext('If (Payload.Amount.Between(')).toMatchObject({
      name: 'Between',
      receiver: 'Payload.Amount',
      index: 0,
    })
  })

  it('ignores commas inside nested calls and literals', () => {
    expect(argumentContext('Payload.Name.Replace(Foo(1, 2), "a, b", ')).toMatchObject({ index: 2 })
  })

  it('is null outside a call, after it closes and inside literals', () => {
    expect(argumentContext('If (Payload.Amount > 1')).toBeNull()
    expect(argumentContext('Payload.Name.Trim() = ')).toBeNull()
    expect(argumentContext('Payload.Name.Replace("a(')).toBeNull()
  })
})

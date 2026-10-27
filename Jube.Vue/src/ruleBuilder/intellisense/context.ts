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
export interface WordContext {
  kind: 'word'
  prefix: string
  from: number
}

export interface MemberContext {
  kind: 'member'
  receiver: string
  prefix: string
  from: number
}

export interface ArgumentContext {
  kind: 'argument'
  name: string
  receiver: string | null
  index: number
  openAt: number
}

export type CursorContext = WordContext | MemberContext | null

function isIdentifier(character: string | undefined): boolean {
  return character !== undefined && /[A-Za-z0-9_]/.test(character)
}

export function insideLiteralOrComment(text: string): boolean {
  let inLiteral = false

  for (let index = 0; index < text.length; index++) {
    const character = text[index]

    if (inLiteral) {
      if (character === '"' && text[index + 1] === '"') index++
      else if (character === '"') inLiteral = false
    } else if (character === '"') {
      inLiteral = true
    } else if (character === "'") {
      return true
    }
  }

  return inLiteral
}

function skipGroup(text: string, closeAt: number): number {
  let depth = 0
  let index = closeAt

  while (index >= 0) {
    const character = text[index]

    if (character === '"') {
      index--

      while (index >= 0 && !(text[index] === '"' && text[index - 1] !== '"')) index--
    } else if (character === ')') {
      depth++
    } else if (character === '(') {
      depth--

      if (depth === 0) return index - 1
    }

    index--
  }

  return -1
}

export function receiverBefore(text: string, dotAt: number): string {
  let index = dotAt - 1

  while (index >= 0) {
    if (text[index] === ')') index = skipGroup(text, index)

    if (!isIdentifier(text[index])) break

    while (index >= 0 && isIdentifier(text[index])) index--

    if (text[index] === '.') index--
    else break
  }

  return text.slice(index + 1, dotAt).trim()
}

export function cursorContext(textBefore: string): CursorContext {
  if (insideLiteralOrComment(textBefore)) return null

  const word = /[A-Za-z_][\w.]*$|\.$/.exec(textBefore)
  const prefixMatch = /[A-Za-z_]\w*$/.exec(textBefore)
  const prefix = prefixMatch ? prefixMatch[0] : ''
  const dotAt = textBefore.length - prefix.length - 1

  if (dotAt >= 0 && textBefore[dotAt] === '.') {
    const receiver = receiverBefore(textBefore, dotAt)

    if (receiver.length > 0) {
      return { kind: 'member', receiver, prefix, from: textBefore.length - prefix.length }
    }
  }

  if (!word) return { kind: 'word', prefix: '', from: textBefore.length }

  return { kind: 'word', prefix: word[0], from: textBefore.length - word[0].length }
}

export function argumentContext(textBefore: string): ArgumentContext | null {
  if (insideLiteralOrComment(textBefore)) return null

  const stack: { at: number; commas: number }[] = []
  let literal = false

  for (let index = 0; index < textBefore.length; index++) {
    const character = textBefore[index]

    if (literal) {
      if (character === '"' && textBefore[index + 1] === '"') index++
      else if (character === '"') literal = false
    } else if (character === '"') {
      literal = true
    } else if (character === '(') {
      stack.push({ at: index, commas: 0 })
    } else if (character === ')') {
      stack.pop()
    } else if (character === ',' && stack.length > 0) {
      stack[stack.length - 1]!.commas++
    }
  }

  const open = stack[stack.length - 1]

  if (!open) return null

  const head = textBefore.slice(0, open.at)
  const name = /[A-Za-z_]\w*$/.exec(head)

  if (!name) return null

  const nameStart = head.length - name[0].length
  const dotAt = nameStart - 1
  const receiver = dotAt >= 0 && head[dotAt] === '.' ? receiverBefore(head, dotAt) : null

  return { kind: 'argument', name: name[0], receiver, index: open.commas, openAt: open.at }
}

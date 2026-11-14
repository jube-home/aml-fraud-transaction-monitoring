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
export type RuleTokenType =
  'comment' | 'literal' | 'language' | 'answer' | 'scan' | 'root' | 'field' | 'method' | 'word'

export interface RuleLexState {
  afterDot: boolean
}

export interface RuleToken {
  from: number
  to: number
  type: RuleTokenType | null
  text: string
}

export const ruleRoots = [
  'Payload',
  'Abstraction',
  'TTLCounter',
  'List',
  'KVP',
  'Sanctions',
  'Sanction',
  'Calculation',
  'AbstractionCalculation',
  'Tag',
  'Activation',
  'Graph',
  'Svirfd',
  'Cursor',
  'HttpAdaptation',
  'ExhaustiveAdaptation',
  'AdvancedAnalyticsAdaptation',
  'Dictionary',
  'Data',
]

const roots = new Set(ruleRoots.map((root) => root.toLowerCase()))

const language = new Set([
  'if',
  'then',
  'else',
  'elseif',
  'end',
  'select',
  'case',
  'and',
  'or',
  'not',
  'andalso',
  'orelse',
  'xor',
  'mod',
  'is',
  'isnot',
  'like',
  'in',
  'to',
  'for',
  'next',
  'dim',
  'as',
  'static',
  'exit',
  'continue',
  'double',
  'integer',
  'string',
  'boolean',
  'datetime',
])

const answers = new Set(['matched', 'return'])
const literals = new Set(['true', 'false', 'nothing'])

const scanControl = /(?:exit|continue)[ \t]+for\b/iy
const identifier = /[A-Za-z_]\w*/y
const number = /\d+(?:\.\d+)?(?:[eE][+-]?\d+)?/y
const dateLiteral = /#[^#]*#/y
const space = /\s+/y

function match(pattern: RegExp, text: string, pos: number): string | null {
  pattern.lastIndex = pos

  const found = pattern.exec(text)

  return found ? found[0] : null
}

function stringEnd(text: string, pos: number): number {
  let index = pos + 1

  while (index < text.length) {
    if (text[index] === '"') {
      if (text[index + 1] === '"') index += 2
      else return index + 1
    } else {
      index++
    }
  }

  return text.length
}

function nextSignificant(text: string, pos: number): string | undefined {
  let index = pos

  while (index < text.length && (text[index] === ' ' || text[index] === '\t')) index++

  return text[index]
}

export function nextToken(text: string, pos: number, state: RuleLexState): RuleToken {
  const token = (to: number, type: RuleTokenType | null): RuleToken => ({
    from: pos,
    to,
    type,
    text: text.slice(pos, to),
  })

  const blank = match(space, text, pos)

  if (blank) return token(pos + blank.length, null)

  const character = text[pos]!

  if (character === "'") {
    state.afterDot = false

    return token(text.length, 'comment')
  }

  if (character === '"') {
    state.afterDot = false

    return token(stringEnd(text, pos), 'literal')
  }

  if (character === '#') {
    const date = match(dateLiteral, text, pos)

    state.afterDot = false

    return date ? token(pos + date.length, 'literal') : token(pos + 1, null)
  }

  const digits = match(number, text, pos)

  if (digits) {
    state.afterDot = false

    return token(pos + digits.length, 'literal')
  }

  if (!state.afterDot) {
    const control = match(scanControl, text, pos)

    if (control) return token(pos + control.length, 'scan')
  }

  const word = match(identifier, text, pos)

  if (word) {
    const end = pos + word.length
    const lower = word.toLowerCase()

    if (state.afterDot) {
      state.afterDot = false

      return token(end, nextSignificant(text, end) === '(' ? 'method' : 'field')
    }

    if (answers.has(lower)) return token(end, 'answer')
    if (literals.has(lower)) return token(end, 'literal')
    if (roots.has(lower) && text[end] === '.') return token(end, 'root')
    if (language.has(lower)) return token(end, 'language')

    return token(end, 'word')
  }

  state.afterDot = character === '.'

  return token(pos + 1, null)
}

export function tokenise(line: string, state: RuleLexState = {afterDot: false}): RuleToken[] {
  const tokens: RuleToken[] = []
  let pos = 0

  while (pos < line.length) {
    const token = nextToken(line, pos, state)

    if (token.type !== null) tokens.push(token)

    pos = token.to
  }

  return tokens
}

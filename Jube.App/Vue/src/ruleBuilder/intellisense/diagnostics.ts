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
import type {Completion} from '../types'
import {tokenise, type RuleToken} from '../language/tokens'
import {overloadsFor, type Vocabulary} from './typeResolver'
import {declaredVariables} from './variables'

export type LocalProblemKind = 'unknownField' | 'notAllowed' | 'noMember'

export interface LocalProblem {
  kind: LocalProblemKind
  line: number
  column: number
  endColumn: number
  name: string
  owner?: string
  suggestion?: string
}

export function distance(left: string, right: string): number {
  const previous = Array.from({length: right.length + 1}, (_, index) => index)

  for (let i = 1; i <= left.length; i++) {
    let diagonal = previous[0]!

    previous[0] = i

    for (let j = 1; j <= right.length; j++) {
      const above = previous[j]!
      const cost = left[i - 1] === right[j - 1] ? 0 : 1

      previous[j] = Math.min(above + 1, previous[j - 1]! + 1, diagonal + cost)
      diagonal = above
    }
  }

  return previous[right.length]!
}

export function nearest(name: string, candidates: string[]): string | undefined {
  const wanted = name.toLowerCase()
  const limit = Math.max(1, Math.floor(name.length / 3))
  let best: string | undefined
  let bestDistance = limit + 1

  for (const candidate of candidates) {
    const found = distance(wanted, candidate.toLowerCase())

    if (found < bestDistance) {
      best = candidate
      bestDistance = found
    }
  }

  return best
}

function allowedWords(vocabulary: Vocabulary): Set<string> {
  const allowed = new Set<string>()

  for (const word of vocabulary.words) {
    for (const part of word.name.toLowerCase().split(/\s+/)) allowed.add(part)
  }

  return allowed
}

function phrases(vocabulary: Vocabulary): Set<string> {
  return new Set(vocabulary.words.map((word) => word.name.toLowerCase().replace(/\s+/g, ' ')))
}

function isAllowed(text: string, allowed: Set<string>): boolean {
  if (allowed.has(text)) return true

  if (!text.includes('_')) return false

  return text
    .split('_')
    .filter((part) => part !== '')
    .every((part) => /^\d+$/.test(part) || allowed.has(part))
}

function fieldPaths(fields: Completion[]): Map<string, string> {
  const paths = new Map<string, string>()

  for (const field of fields) {
    const parts = field.name.split('.')

    if (parts.length < 2) continue

    const path = `${parts[0]}.${parts[1]}`

    if (!paths.has(path.toLowerCase())) paths.set(path.toLowerCase(), path)
  }

  return paths
}

function memberOf(tokens: RuleToken[], at: number): RuleToken | undefined {
  const owner = tokens[at]!
  const member = tokens[at + 1]

  return member &&
  (member.type === 'field' || member.type === 'method') &&
  member.from === owner.to + 1
    ? member
    : undefined
}

export function localProblems(
  text: string,
  fields: Completion[],
  vocabulary: Vocabulary,
): LocalProblem[] {
  const problems: LocalProblem[] = []
  const fieldNames = fieldPaths(fields)
  const fieldRoots = new Set(fields.map((field) => field.name.split('.')[0]!.toLowerCase()))
  const allowed = allowedWords(vocabulary)

  if (allowed.has('dim') || allowed.has('static')) {
    for (const variable of declaredVariables(text)) allowed.add(variable.name.toLowerCase())
  }
  const known = phrases(vocabulary)
  const checkWords = vocabulary.words.length > 0
  const lines = text.split('\n')

  lines.forEach((lineText, line) => {
    const tokens = tokenise(lineText)

    tokens.forEach((token, at) => {
      const lower = token.text.toLowerCase()

      if (token.type === 'root') {
        const member = memberOf(tokens, at)

        if (!member) return

        if (fieldRoots.has(lower)) {
          const name = `${token.text}.${member.text}`

          if (member.type === 'field' && !fieldNames.has(name.toLowerCase())) {
            problems.push({
              kind: 'unknownField',
              line,
              column: token.from,
              endColumn: member.to,
              name,
              suggestion: nearest(
                name,
                [...fieldNames.values()].filter((field) =>
                  field.toLowerCase().startsWith(`${lower}.`),
                ),
              ),
            })
          }

          return
        }

        const type = vocabulary.roots.get(lower)

        if (type !== undefined && overloadsFor(vocabulary, member.text, type).length === 0) {
          problems.push({
            kind: 'noMember',
            line,
            column: member.from,
            endColumn: member.to,
            name: member.text,
            owner: token.text,
          })
        }

        return
      }

      if (!checkWords) return

      if (token.type === 'scan') {
        if (!known.has(lower.replace(/\s+/g, ' '))) {
          problems.push({
            kind: 'notAllowed',
            line,
            column: token.from,
            endColumn: token.to,
            name: token.text.replace(/\s+/g, ' '),
          })
        }

        return
      }

      const checked =
        token.type === 'word' ||
        token.type === 'method' ||
        token.type === 'language' ||
        token.type === 'answer'

      if (!checked || isAllowed(lower, allowed) || fieldRoots.has(lower)) return

      const owner = tokens[at - 1]

      if (
        token.type === 'method' &&
        owner?.type === 'root' &&
        vocabulary.roots.has(owner.text.toLowerCase())
      ) {
        return
      }

      problems.push({
        kind: 'notAllowed',
        line,
        column: token.from,
        endColumn: token.to,
        name: token.text,
      })
    })
  })

  return problems
}

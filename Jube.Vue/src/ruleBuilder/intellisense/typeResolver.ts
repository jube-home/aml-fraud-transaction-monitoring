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
import type { Completion, RuleWord, RuleWordOverload } from '../types'

export interface Vocabulary {
  words: RuleWord[]
  functions: Map<string, RuleWord>
  keywords: RuleWord[]
}

const widening: Record<string, string[]> = {
  Integer: ['Integer', 'Long', 'Double', 'Single', 'Decimal', 'Object'],
  Long: ['Long', 'Double', 'Single', 'Decimal', 'Object'],
  Short: ['Short', 'Integer', 'Long', 'Double', 'Single', 'Decimal', 'Object'],
  Single: ['Single', 'Double', 'Object'],
  Double: ['Double', 'Object'],
  Decimal: ['Decimal', 'Object'],
  String: ['String', 'Object'],
  Boolean: ['Boolean', 'Object'],
  DateTime: ['DateTime', 'Object'],
  Char: ['Char', 'String', 'Object'],
}

const vbTypes: Record<string, string> = {
  string: 'String',
  integer: 'Integer',
  double: 'Double',
  datetime: 'DateTime',
  boolean: 'Boolean',
}

export function buildVocabulary(words: RuleWord[]): Vocabulary {
  const functions = new Map<string, RuleWord>()

  for (const word of words) {
    if (word.kind === 'Function' || word.kind === 'CuratedExpression') {
      functions.set(word.name.toLowerCase(), word)
    }
  }

  return { words, functions, keywords: words.filter((word) => word.kind === 'Keyword') }
}

export function vbTypeOf(dataType: string | undefined): string | null {
  if (!dataType) return null

  return vbTypes[dataType.toLowerCase()] ?? null
}

export function isCompatible(actual: string, declared: string): boolean {
  if (actual === declared || declared === 'Object') return true

  return (widening[actual] ?? [actual]).includes(declared)
}

const genericName = /^T(?:[A-Z][a-z]+)?$/
const genericType = /^(\w+)\(Of (.+)\)$/

export function bindReceiver(actual: string, declared: string): Record<string, string> | null {
  if (isCompatible(actual, declared)) return {}

  const wanted = genericType.exec(declared)
  const given = genericType.exec(actual)

  if (!wanted || !given || wanted[1] !== given[1] || !genericName.test(wanted[2]!)) return null

  return { [wanted[2]!]: given[2]! }
}

export function substituteReturn(returnType: string, bindings: Record<string, string>): string {
  const wrapped = genericType.exec(returnType)
  const inner = wrapped ? wrapped[2]! : returnType

  if (!genericName.test(inner)) return returnType

  const bound = bindings[inner] ?? 'Object'

  return wrapped ? `${wrapped[1]}(Of ${bound})` : bound
}

export function overloadsFor(
  vocabulary: Vocabulary,
  name: string,
  receiverType: string,
): RuleWordOverload[] {
  const word = vocabulary.functions.get(name.toLowerCase())

  if (!word) return []

  return word.overloads.filter(
    (overload) => bindReceiver(receiverType, overload.receiverType) !== null,
  )
}

export function fieldTypes(fields: Completion[]): Map<string, string> {
  const map = new Map<string, string>()

  for (const field of fields) map.set(field.name, field.dataType)

  return map
}

function matchingOpen(text: string): number {
  let depth = 0

  for (let index = text.length - 1; index >= 0; index--) {
    const character = text[index]

    if (character === '"') {
      index--

      while (index >= 0 && !(text[index] === '"' && text[index - 1] !== '"')) index--
    } else if (character === ')') {
      depth++
    } else if (character === '(') {
      depth--

      if (depth === 0) return index
    }
  }

  return -1
}

export function argumentCount(inner: string): number {
  if (inner.trim() === '') return 0

  let depth = 0
  let literal = false
  let commas = 0

  for (let index = 0; index < inner.length; index++) {
    const character = inner[index]

    if (literal) {
      if (character === '"' && inner[index + 1] === '"') index++
      else if (character === '"') literal = false
    } else if (character === '"') {
      literal = true
    } else if (character === '(') {
      depth++
    } else if (character === ')') {
      depth--
    } else if (character === ',' && depth === 0) {
      commas++
    }
  }

  return commas + 1
}

function fits(overload: RuleWordOverload, count: number | null): boolean {
  if (count === null) return true

  const required = overload.parameters.filter((parameter) => !parameter.optional).length

  return count >= required && count <= overload.parameters.length
}

export function resolveType(
  expression: string,
  fields: Map<string, string>,
  vocabulary: Vocabulary,
): string | null {
  const trimmed = expression.trim()

  if (trimmed === '') return null

  const direct = fields.get(trimmed)

  if (direct !== undefined) return vbTypeOf(direct)

  let head = trimmed
  let count: number | null = null

  if (trimmed.endsWith(')')) {
    const open = matchingOpen(trimmed)

    if (open < 0) return null

    count = argumentCount(trimmed.slice(open + 1, -1))
    head = trimmed.slice(0, open)
  }

  const dotAt = head.lastIndexOf('.')

  if (dotAt < 0) return null

  const receiverType = resolveType(head.slice(0, dotAt), fields, vocabulary)

  if (!receiverType) return null

  const candidates = overloadsFor(vocabulary, head.slice(dotAt + 1), receiverType)
  const chosen = candidates.find((overload) => fits(overload, count)) ?? candidates[0]

  if (!chosen) return null

  return substituteReturn(chosen.returnType, bindReceiver(receiverType, chosen.receiverType) ?? {})
}

export function matchingOverloads(
  vocabulary: Vocabulary,
  name: string,
  receiverType: string,
  count: number | null,
): RuleWordOverload[] {
  return overloadsFor(vocabulary, name, receiverType).filter((overload) => fits(overload, count))
}

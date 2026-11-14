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
import {EditorState} from '@codemirror/state'
import {CompletionContext, type CompletionResult} from '@codemirror/autocomplete'
import {fieldSource, keywordSource, memberSource, type IntellisenseOptions} from '../extension'
import {buildVocabulary} from '../typeResolver'
import type {Completion, RuleWord} from '../../types'
import {baseWords, cursorWords, fields as modelFields} from './vocabularyFixture'

const words: RuleWord[] = [
  {name: 'Then', kind: 'Keyword', overloads: []},
  {name: 'Exit For', kind: 'Keyword', overloads: []},
  {
    name: 'StartsWith',
    kind: 'Function',
    overloads: [
      {
        signature: '<String>.StartsWith(prefix As String) As Boolean',
        receiverType: 'String',
        returnType: 'Boolean',
        description: 'Checks if the string starts with the prefix.',
        parameters: [
          {
            name: 'prefix',
            type: 'String',
            optional: false,
            defaultValue: null,
            description: 'The text it must start with.',
          },
        ],
      },
    ],
  },
]

const fields = [
  {
    name: 'Payload.Name',
    value: 'Payload.Name',
    meta: 'Name:string',
    score: 1000,
    dataType: 'string',
    group: 'Payload',
  },
] as unknown as Completion[]

const options: IntellisenseOptions = {
  getFields: () => fields,
  getVocabulary: () => buildVocabulary(words),
  labels: {
    fields: 'Fields',
    methods: 'Methods',
    keywords: 'Keywords',
    snippets: 'Snippets',
    field: 'Field',
    members: 'Members',
    patterns: 'Patterns',
    pattern: {
      nth: 'The Nth record',
      firstMatching: 'The first N matching records',
      runningTotal: 'Running total until a limit',
      skipCurrent: 'Skip the transaction being processed',
      ownResult: 'Return a value the rule computes',
    },
    variables: 'Variables',
    variable: {record: 'Kept for the record', scan: 'Kept for the scan'},
  },
}

const patternLabels = Object.values(options.labels.pattern)

function cursorOptions(parseType: number): IntellisenseOptions {
  return {
    ...options,
    getFields: () => modelFields,
    getVocabulary: () => buildVocabulary([...baseWords, ...cursorWords]),
    getParseType: () => parseType,
  }
}

function complete(
  source: ReturnType<typeof keywordSource>,
  doc: string,
  explicit = false,
): { result: CompletionResult | null; typed: string } {
  const state = EditorState.create({doc})
  const result = source(
    new CompletionContext(state, doc.length, explicit),
  ) as CompletionResult | null

  return {result, typed: result ? doc.slice(result.from) : ''}
}

describe('rule intellisense sources', () => {
  it('offers keywords from the word being typed on the first line', () => {
    const {result, typed} = complete(keywordSource(options), 'Th')

    expect(typed).toBe('Th')
    expect(result?.options.map((option) => option.label)).toContain('Then')
  })

  it('offers keywords from the word being typed on a later line, not from the top of the document', () => {
    const doc = 'If x Then\n  Return True\nEx'
    const {result, typed} = complete(keywordSource(options), doc)

    expect(result?.from).toBe(doc.length - 2)
    expect(typed).toBe('Ex')
    expect(result?.options.map((option) => option.label)).toContain('Exit For')
  })

  it('keeps a keyword of two words open while its second word is typed', () => {
    const {result} = complete(keywordSource(options), 'Ex')

    expect(result?.validFor).toBeInstanceOf(RegExp)
    expect((result?.validFor as RegExp).test('Exit F')).toBe(true)
  })

  it('offers block snippets only at the start of a line', () => {
    const atStart = complete(keywordSource(options), 'x\n  I').result
    const midLine = complete(keywordSource(options), 'x\n  y = I').result

    expect(atStart?.options.some((option) => option.detail === 'Return True')).toBe(true)
    expect(midLine?.options.some((option) => option.detail === 'Return True')).toBe(false)
  })

  it('offers the methods of a field on a later line from the text after the dot', () => {
    const doc = 'If x Then\n  Matched = Payload.Name.Sta'
    const {result, typed} = complete(memberSource(options), doc)

    expect(typed).toBe('Sta')
    expect(result?.options.map((option) => option.label)).toEqual(['StartsWith'])
    expect(result?.options[0]?.info).toBe('Checks if the string starts with the prefix.')
  })

  it('offers fields from the dotted word being typed on a later line', () => {
    const doc = 'x\nPayload.Na'
    const {result, typed} = complete(fieldSource(options), doc)

    expect(typed).toBe('Payload.Na')
    expect(result?.options.map((option) => option.label)).toEqual(['Payload.Name'])
  })

  it('offers the cursor properties and methods after Cursor and a dot', () => {
    const doc = 'Matched = True\nIf Cursor.'
    const {result, typed} = complete(memberSource(cursorOptions(3)), doc, true)
    const byLabel = new Map(result?.options.map((option) => [option.label, option]))

    expect(typed).toBe('')
    expect([...byLabel.keys()].sort()).toEqual(['IsCurrent', 'Matches', 'Position', 'Result'])
    expect(byLabel.get('Position')).toMatchObject({
      type: 'property',
      detail: ' As Integer',
      info: 'One-based position.',
      apply: 'Position',
    })
  })

  it('offers the declared variables of an abstraction rule with their type and lifetime', () => {
    const doc = 'Static declines As Integer\nDim doubled As Double = 2\nMatched = d'
    const offered = (parseType: number) =>
      complete(keywordSource(cursorOptions(parseType)), doc).result?.options.filter(
        (option) => option.type === 'variable',
      )

    expect(offered(3)).toMatchObject([
      {label: 'declines', detail: ' As Integer', info: 'Kept for the scan'},
      {label: 'doubled', detail: ' As Double', info: 'Kept for the record'},
    ])
    expect(offered(5)).toEqual([])
  })

  it('offers the methods of the type a declared variable has', () => {
    const doc = 'Static total As Double\nMatched = total.Ro'
    const labels = (parseType: number) =>
      complete(memberSource(cursorOptions(parseType)), doc).result?.options.map(
        (option) => option.label,
      )

    expect(labels(3)).toEqual(['Round'])
    expect(labels(5)).toBeUndefined()
  })

  it('offers the declaration snippets at the start of a line of an abstraction rule only', () => {
    const declarations = (parseType: number, doc: string) =>
      complete(keywordSource(cursorOptions(parseType)), doc)
        .result?.options.filter((option) => option.detail?.endsWith('name As Type'))
        .map((option) => option.label)

    expect(declarations(3, 'S')).toEqual(['Dim', 'Static'])
    expect(declarations(3, 'x = S')).toEqual([])
    expect(declarations(5, 'S')).toEqual([])
  })

  it('offers the methods of the type a cursor property returns', () => {
    const {result} = complete(memberSource(cursorOptions(3)), 'Return Cursor.Result.Ro')

    expect(result?.options.map((option) => option.label)).toEqual(['Round'])
  })

  it('keeps cursor properties out of the bare keyword list', () => {
    const labels = complete(keywordSource(cursorOptions(3)), 'P').result?.options.map(
      (option) => option.label,
    )

    expect(labels).toContain('Payload')
    expect(labels).toContain('Cursor')
    expect(labels).not.toContain('Position')
  })

  it('offers the cursor patterns at the start of a line of an abstraction rule only', () => {
    const patterns = (parseType: number, doc: string) =>
      complete(keywordSource(cursorOptions(parseType)), doc)
        .result?.options.filter((option) => patternLabels.includes(option.label))
        .map((option) => option.label)

    expect(patterns(3, 'T')).toEqual([
      'The Nth record',
      'The first N matching records',
      'Running total until a limit',
      'Skip the transaction being processed',
      'Return a value the rule computes',
    ])
    expect(patterns(3, 'x = T')).toEqual([])
    expect(patterns(5, 'T')).toEqual([])
    expect(
      complete(keywordSource(options), 'T').result?.options.filter((option) =>
        patternLabels.includes(option.label),
      ),
    ).toEqual([])
  })

  it('carries the description of a scan word to its completion', () => {
    const exit = complete(keywordSource(cursorOptions(3)), 'Ex').result?.options.find(
      (option) => option.label === 'Exit For',
    )

    expect(exit?.info).toBe('Ends the walk.')
  })

  it('offers nothing inside a string literal', () => {
    expect(complete(keywordSource(options), 'x = "Th').result).toBeNull()
    expect(complete(fieldSource(options), 'x = "Payload.Na').result).toBeNull()
  })
})

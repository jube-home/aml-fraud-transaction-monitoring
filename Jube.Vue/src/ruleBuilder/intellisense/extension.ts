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
import type { Extension } from '@codemirror/state'
import { StateField, type EditorState } from '@codemirror/state'
import {
  EditorView,
  hoverTooltip,
  showTooltip,
  type Tooltip,
  type TooltipView,
} from '@codemirror/view'
import {
  snippet,
  type Completion as CodeMirrorCompletion,
  type CompletionContext,
  type CompletionResult,
  type CompletionSource,
} from '@codemirror/autocomplete'
import type { Completion } from '../types'
import { argumentContext, cursorContext, insideLiteralOrComment, receiverBefore } from './context'
import { blockSnippets, overloadDetail, overloadSnippet, parameterText } from './signature'
import {
  fieldTypes,
  matchingOverloads,
  overloadsFor,
  resolveType,
  type Vocabulary,
} from './typeResolver'

export interface IntellisenseLabels {
  fields: string
  methods: string
  keywords: string
  snippets: string
  field: string
}

export interface IntellisenseOptions {
  getFields: () => Completion[]
  getVocabulary: () => Vocabulary
  labels: IntellisenseLabels
}

function isIdentifier(character: string | undefined): boolean {
  return character !== undefined && /[A-Za-z0-9_]/.test(character)
}

function lineBefore(state: EditorState, pos: number): string {
  const line = state.doc.lineAt(pos)

  return line.text.slice(0, pos - line.from)
}

export function fieldSource(options: IntellisenseOptions): CompletionSource {
  return (context: CompletionContext): CompletionResult | null => {
    const word = context.matchBefore(/[A-Za-z_][\w.]*/)

    if (!word || insideLiteralOrComment(lineBefore(context.state, context.pos))) return null
    if (word.from === word.to && !context.explicit) return null

    return {
      from: word.from,
      validFor: /^[\w.]*$/,
      options: options.getFields().map((field) => ({
        label: field.name,
        apply: field.value,
        detail: field.meta,
        boost: field.score,
        type: 'variable',
        section: { name: options.labels.fields, rank: 0 },
      })),
    }
  }
}

export function memberSource(options: IntellisenseOptions): CompletionSource {
  return (context: CompletionContext): CompletionResult | null => {
    const cursor = cursorContext(lineBefore(context.state, context.pos))

    if (cursor?.kind !== 'member') return null

    const vocabulary = options.getVocabulary()
    const receiverType = resolveType(cursor.receiver, fieldTypes(options.getFields()), vocabulary)

    if (!receiverType) return null

    const suggestions: CodeMirrorCompletion[] = []

    for (const word of vocabulary.functions.values()) {
      const candidates = overloadsFor(vocabulary, word.name, receiverType)
      const exact = candidates.filter((overload) => overload.receiverType === receiverType)
      const taken = new Set(exact.map((overload) => overload.parameters.length))
      const shown = [
        ...exact,
        ...candidates.filter(
          (overload) =>
            overload.receiverType !== receiverType && !taken.has(overload.parameters.length),
        ),
      ]

      for (const overload of shown) {
        suggestions.push({
          label: word.name,
          type: 'method',
          detail: overloadDetail(overload),
          info: overload.signature,
          boost: overload.receiverType === receiverType ? 10 : 0,
          apply: snippet(overloadSnippet(word.name, overload)),
          section: { name: options.labels.methods, rank: 1 },
        })
      }
    }

    return { from: cursor.from, validFor: /^\w*$/, options: suggestions }
  }
}

export function keywordSource(options: IntellisenseOptions): CompletionSource {
  return (context: CompletionContext): CompletionResult | null => {
    const before = lineBefore(context.state, context.pos)
    const cursor = cursorContext(before)

    if (cursor?.kind !== 'word' || cursor.prefix.includes('.')) return null
    if (cursor.prefix === '' && !context.explicit) return null

    const atLineStart = before.slice(0, before.length - cursor.prefix.length).trim() === ''
    const suggestions: CodeMirrorCompletion[] = options.getVocabulary().keywords.map((word) => ({
      label: word.name,
      type: 'keyword',
      section: { name: options.labels.keywords, rank: 2 },
    }))

    if (atLineStart) {
      for (const entry of blockSnippets) {
        suggestions.push({
          label: entry.label,
          type: 'text',
          detail: entry.detail,
          apply: snippet(entry.template),
          boost: -1,
          section: { name: options.labels.snippets, rank: 3 },
        })
      }
    }

    return { from: cursor.from, validFor: /^\w*$/, options: suggestions }
  }
}

function signatureLines(
  options: IntellisenseOptions,
  name: string,
  receiverType: string,
  active: number,
): HTMLElement {
  const dom = document.createElement('div')

  dom.className = 'cm-rule-signature'

  const overloads = matchingOverloads(options.getVocabulary(), name, receiverType, null)
  const shown = overloads.filter((overload) => overload.parameters.length > active)
  const list = shown.length > 0 ? shown : overloads

  for (const overload of list) {
    const row = document.createElement('div')

    row.append(document.createTextNode(`${name}(`))

    overload.parameters.forEach((parameter, position) => {
      if (position > 0) row.append(document.createTextNode(', '))

      const part = document.createElement(position === active ? 'strong' : 'span')

      part.textContent = parameterText(parameter)
      row.append(part)
    })

    row.append(document.createTextNode(`) As ${overload.returnType}`))
    dom.append(row)
  }

  return dom
}

function signatureTooltips(options: IntellisenseOptions, state: EditorState): readonly Tooltip[] {
  const head = state.selection.main.head
  const call = argumentContext(lineBefore(state, head))

  if (!call?.receiver) return []

  const receiverType = resolveType(
    call.receiver,
    fieldTypes(options.getFields()),
    options.getVocabulary(),
  )

  if (
    !receiverType ||
    overloadsFor(options.getVocabulary(), call.name, receiverType).length === 0
  ) {
    return []
  }

  return [
    {
      pos: head,
      above: true,
      strictSide: false,
      arrow: false,
      create: (): TooltipView => ({
        dom: signatureLines(options, call.name, receiverType, call.index),
      }),
    },
  ]
}

function hoverDom(lines: string[]): HTMLElement {
  const dom = document.createElement('div')

  dom.className = 'cm-rule-hover'

  for (const line of lines) {
    const row = document.createElement('div')

    row.textContent = line
    dom.append(row)
  }

  return dom
}

function hover(options: IntellisenseOptions): Extension {
  return hoverTooltip((view, pos) => {
    const line = view.state.doc.lineAt(pos)
    const column = pos - line.from

    if (insideLiteralOrComment(line.text.slice(0, column))) return null

    let start = column
    let end = column

    while (start > 0 && isIdentifier(line.text[start - 1])) start--
    while (end < line.text.length && isIdentifier(line.text[end])) end++

    if (start === end) return null

    const name = line.text.slice(start, end)
    const receiver = line.text[start - 1] === '.' ? receiverBefore(line.text, start - 1) : null
    const fields = options.getFields()
    const fullName = receiver ? `${receiver}.${name}` : name
    const field = fields.find((candidate) => candidate.name === fullName)
    const vocabulary = options.getVocabulary()
    let lines: string[] = []

    if (field) {
      lines = [field.name, `${options.labels.field}: ${field.dataType} (${field.group})`]
    } else if (receiver) {
      const receiverType = resolveType(receiver, fieldTypes(fields), vocabulary)

      if (receiverType) {
        lines = overloadsFor(vocabulary, name, receiverType).map(
          (overload) => `${name}${overloadDetail(overload)}`,
        )
      }
    } else {
      const word = vocabulary.words.find(
        (candidate) => candidate.name.toLowerCase() === name.toLowerCase(),
      )

      if (word) lines = [word.name, word.kind]
    }

    if (lines.length === 0) return null

    return {
      pos: line.from + start,
      end: line.from + end,
      above: true,
      create: () => ({ dom: hoverDom(lines) }),
    }
  })
}

export function ruleIntellisense(options: IntellisenseOptions): Extension[] {
  const signatureHelp = StateField.define<readonly Tooltip[]>({
    create: (state) => signatureTooltips(options, state),
    update: (value, transaction) =>
      transaction.docChanged || transaction.selection
        ? signatureTooltips(options, transaction.state)
        : value,
    provide: (field) => showTooltip.computeN([field], (state) => state.field(field)),
  })

  return [
    signatureHelp,
    hover(options),
    EditorView.theme({
      '.cm-tooltip': {
        backgroundColor: 'var(--color-base-100)',
        color: 'var(--color-base-content)',
        border: '1px solid var(--color-base-300)',
        borderRadius: 'var(--radius-field)',
        fontFamily: 'var(--font-mono, monospace)',
        fontSize: '0.8125rem',
      },
      '.cm-tooltip-autocomplete ul li[aria-selected]': {
        backgroundColor: 'var(--color-primary)',
        color: 'var(--color-primary-content)',
      },
      '.cm-rule-signature, .cm-rule-hover': { padding: '0.25rem 0.5rem' },
    }),
  ]
}

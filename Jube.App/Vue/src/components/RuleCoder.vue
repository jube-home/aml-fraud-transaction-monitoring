<!-- Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 -->

<script setup vapor lang="ts">
import {onBeforeUnmount, onMounted, ref, watch} from 'vue'
import {useI18n} from 'vue-i18n'
import {EditorState, Prec} from '@codemirror/state'
import {EditorView, keymap} from '@codemirror/view'
import {indentWithTab} from '@codemirror/commands'
import {indentService, indentUnit as editorIndentUnit} from '@codemirror/language'
import {autocompletion} from '@codemirror/autocomplete'
import {forEachDiagnostic, setDiagnostics, type Diagnostic} from '@codemirror/lint'
import {basicSetup} from 'codemirror'
import {
  fieldSource,
  keywordSource,
  memberSource,
  ruleIntellisense,
} from '../ruleBuilder/intellisense/extension'
import {blockCloser, leadingSpace} from '../ruleBuilder/intellisense/blocks'
import {localProblems, type LocalProblem} from '../ruleBuilder/intellisense/diagnostics'
import {scanGutter} from '../ruleBuilder/intellisense/scanGutter'
import {dedentOnInput, indentLevelFor, indentUnit} from '../ruleBuilder/format'
import type {Vocabulary} from '../ruleBuilder/intellisense/typeResolver'
import {ruleLanguage} from '../ruleBuilder/language/ruleLanguage'
import type {Completion, ErrorSpan} from '../ruleBuilder/types'

const props = defineProps<{
  modelValue: string
  completions: Completion[]
  vocabulary: Vocabulary
  errors: ErrorSpan[]
  label: string
  parseType?: number
}>()

const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const {t} = useI18n()
const host = ref<HTMLElement>()

let view: EditorView | undefined

const intellisense = {
  getFields: () => props.completions,
  getVocabulary: () => props.vocabulary,
  getParseType: () => props.parseType ?? 0,
  labels: {
    fields: t('ruleBuilder.intellisense.fields'),
    methods: t('ruleBuilder.intellisense.methods'),
    keywords: t('ruleBuilder.intellisense.keywords'),
    snippets: t('ruleBuilder.intellisense.snippets'),
    field: t('ruleBuilder.intellisense.field'),
    members: t('ruleBuilder.intellisense.members'),
    patterns: t('ruleBuilder.intellisense.patterns'),
    pattern: {
      nth: t('ruleBuilder.intellisense.pattern.nth'),
      firstMatching: t('ruleBuilder.intellisense.pattern.firstMatching'),
      runningTotal: t('ruleBuilder.intellisense.pattern.runningTotal'),
      skipCurrent: t('ruleBuilder.intellisense.pattern.skipCurrent'),
      ownResult: t('ruleBuilder.intellisense.pattern.ownResult'),
    },
    variables: t('ruleBuilder.intellisense.variables'),
    variable: {
      record: t('ruleBuilder.intellisense.variable.record'),
      scan: t('ruleBuilder.intellisense.variable.scan'),
    },
  },
}

const resultDiscarded = 'ResultDiscarded'

function localMessage(problem: LocalProblem): string {
  if (problem.kind === 'notAllowed') {
    return t('ruleBuilder.diagnostics.notAllowed', {word: problem.name})
  }

  if (problem.kind === 'noMember') {
    return t('ruleBuilder.diagnostics.noMember', {owner: problem.owner, name: problem.name})
  }

  const unknown = t('ruleBuilder.diagnostics.unknownField', {name: problem.name})

  return problem.suggestion
    ? `${unknown} ${t('ruleBuilder.diagnostics.didYouMean', {suggestion: problem.suggestion})}`
    : unknown
}

function localDiagnostics(state: EditorState, taken: Diagnostic[]): Diagnostic[] {
  return localProblems(state.doc.toString(), props.completions, props.vocabulary)
    .map((problem): Diagnostic => {
      const line = state.doc.line(problem.line + 1)
      const suggestion = problem.suggestion

      return {
        from: line.from + problem.column,
        to: line.from + problem.endColumn,
        severity: 'warning',
        message: localMessage(problem),
        actions: suggestion
          ? [
            {
              name: t('ruleBuilder.diagnostics.useSuggestion', {suggestion}),
              apply: (target, from, to) =>
                target.dispatch({changes: {from, to, insert: suggestion}}),
            },
          ]
          : undefined,
      }
    })
    .filter((local) => !taken.some((server) => server.from < local.to && local.from < server.to))
}

function toDiagnostics(state: EditorState, errors: ErrorSpan[]): Diagnostic[] {
  return errors.map((error) => {
    const line = state.doc.line(Math.min(Math.max(error.line + 1, 1), state.doc.lines))
    const located =
      typeof error.column === 'number' && error.column >= 0 && error.column < line.length

    if (!located) return {from: line.from, to: line.to, severity: 'error', message: error.message}

    const from = line.from + error.column!
    const end = typeof error.endColumn === 'number' ? error.endColumn : error.column! + 1
    const to = Math.min(line.from + Math.max(end, error.column! + 1), line.to)

    return {
      from,
      to,
      severity: 'error',
      message: error.message,
      actions:
        error.code === resultDiscarded
          ? [
            {
              name: t('ruleBuilder.diagnostics.assignToMatched'),
              apply: (target, start) =>
                target.dispatch({changes: {from: start, insert: 'Matched = '}}),
            },
          ]
          : undefined,
    }
  })
}

function show(server: Diagnostic[]): void {
  if (!view) return

  view.dispatch(setDiagnostics(view.state, [...server, ...localDiagnostics(view.state, server)]))
}

function showErrors(): void {
  if (view) show(toDiagnostics(view.state, props.errors))
}

function refreshLocal(): void {
  if (!view) return

  const server: Diagnostic[] = []

  forEachDiagnostic(view.state, (diagnostic, from, to) => {
    if (diagnostic.severity === 'error') server.push({...diagnostic, from, to})
  })

  show(server)
}

function closeBlock(target: EditorView): boolean {
  const selection = target.state.selection.main

  if (!selection.empty || target.state.selection.ranges.length > 1) return false

  const line = target.state.doc.lineAt(selection.head)

  if (line.text.slice(selection.head - line.from).trim() !== '') return false

  const closer = blockCloser(target.state.doc.toString().split('\n'), line.number - 1)

  if (!closer) return false

  const indent = leadingSpace(line.text)
  const body = `\n${indent}${indentUnit}`

  target.dispatch({
    changes: {from: selection.head, insert: `${body}\n${indent}${closer}`},
    selection: {anchor: selection.head + body.length},
    scrollIntoView: true,
    userEvent: 'input',
  })

  return true
}

onMounted(() => {
  if (!host.value) return

  view = new EditorView({
    parent: host.value,
    state: EditorState.create({
      doc: props.modelValue,
      extensions: [
        Prec.high(keymap.of([{key: 'Enter', run: closeBlock}])),
        ...scanGutter({
          stop: t('ruleBuilder.gutter.stop'),
          skip: t('ruleBuilder.gutter.skip'),
          answer: t('ruleBuilder.gutter.answer'),
        }),
        basicSetup,
        editorIndentUnit.of(indentUnit),
        keymap.of([indentWithTab]),
        EditorView.lineWrapping,
        ...ruleLanguage(dedentOnInput),
        Prec.high(
          indentService.of((context, pos) => {
            const line = context.state.doc.lineAt(pos)
            const before = context.state.sliceDoc(0, line.from)

            return indentLevelFor(before, line.text) * indentUnit.length
          }),
        ),
        autocompletion({
          selectOnOpen: false,
          override: [
            fieldSource(intellisense),
            memberSource(intellisense),
            keywordSource(intellisense),
          ],
        }),
        ...ruleIntellisense(intellisense),
        EditorView.contentAttributes.of({'aria-label': props.label}),
        EditorView.theme({
          '&': {
            backgroundColor: 'var(--color-base-100)',
            color: 'var(--color-base-content)',
            height: '100%',
          },
          '&.cm-focused': {outline: '2px solid var(--color-primary)'},
          '.cm-scroller': {
            fontFamily: 'var(--font-mono, monospace)',
            overflow: 'auto',
            lineHeight: '1.5',
          },
          '.cm-diagnostic, .cm-completionSection': {unicodeBidi: 'plaintext'},
          '.cm-gutters': {
            backgroundColor: 'var(--color-base-200)',
            color: 'var(--color-secondary)',
            borderRight: '1px solid var(--color-base-300)',
          },
          '.cm-activeLine, .cm-activeLineGutter': {backgroundColor: 'var(--color-base-200)'},
          '.cm-cursor': {borderLeftColor: 'var(--color-base-content)'},
        }),
        EditorView.updateListener.of((update) => {
          if (!update.docChanged) return

          emit('update:modelValue', update.state.doc.toString())
          queueMicrotask(refreshLocal)
        }),
      ],
    }),
  })

  showErrors()
})

onBeforeUnmount(() => {
  view?.destroy()
  view = undefined
})

watch(
  () => props.modelValue,
  (value) => {
    if (!view || value === view.state.doc.toString()) return

    view.dispatch({changes: {from: 0, to: view.state.doc.length, insert: value}})
  },
)

watch(() => props.errors, showErrors)
watch(() => [props.completions, props.vocabulary], refreshLocal)

function focusAt(line: number, column: number | null, endColumn: number | null): void {
  if (!view) return

  const target = view.state.doc.line(Math.min(Math.max(line + 1, 1), view.state.doc.lines))
  const from = target.from + Math.min(column ?? 0, target.length)
  const to = endColumn === null ? target.to : target.from + Math.min(endColumn, target.length)

  view.dispatch({selection: {anchor: from, head: Math.max(to, from)}, scrollIntoView: true})
  view.focus()
}

defineExpose({focusAt})
</script>

<template>
  <div
    ref="host"
    dir="ltr"
    class="min-h-40 flex-1 overflow-hidden rounded-field border border-base-300"
    data-testid="rule-coder"
  ></div>
</template>

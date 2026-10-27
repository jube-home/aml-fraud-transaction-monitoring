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
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { EditorState, Prec } from '@codemirror/state'
import { EditorView } from '@codemirror/view'
import { StreamLanguage, indentService } from '@codemirror/language'
import { vbScript } from '@codemirror/legacy-modes/mode/vbscript'
import { autocompletion } from '@codemirror/autocomplete'
import { setDiagnostics, type Diagnostic } from '@codemirror/lint'
import { basicSetup } from 'codemirror'
import {
  fieldSource,
  keywordSource,
  memberSource,
  ruleIntellisense,
} from '../ruleBuilder/intellisense/extension'
import { dedentOnInput, indentLevelFor, indentUnit } from '../ruleBuilder/format'
import type { Vocabulary } from '../ruleBuilder/intellisense/typeResolver'
import type { Completion, ErrorSpan } from '../ruleBuilder/types'

const props = defineProps<{
  modelValue: string
  completions: Completion[]
  vocabulary: Vocabulary
  errors: ErrorSpan[]
  label: string
}>()

const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const { t } = useI18n()
const host = ref<HTMLElement>()

let view: EditorView | undefined

const intellisense = {
  getFields: () => props.completions,
  getVocabulary: () => props.vocabulary,
  labels: {
    fields: t('ruleBuilder.intellisense.fields'),
    methods: t('ruleBuilder.intellisense.methods'),
    keywords: t('ruleBuilder.intellisense.keywords'),
    snippets: t('ruleBuilder.intellisense.snippets'),
    field: t('ruleBuilder.intellisense.field'),
  },
}

function toDiagnostics(state: EditorState, errors: ErrorSpan[]): Diagnostic[] {
  return errors.map((error) => {
    const line = state.doc.line(Math.min(Math.max(error.line + 1, 1), state.doc.lines))
    const located =
      typeof error.column === 'number' && error.column >= 0 && error.column < line.length

    if (!located) return { from: line.from, to: line.to, severity: 'error', message: error.message }

    const from = line.from + error.column!
    const end = typeof error.endColumn === 'number' ? error.endColumn : error.column! + 1
    const to = Math.min(line.from + Math.max(end, error.column! + 1), line.to)

    return { from, to, severity: 'error', message: error.message }
  })
}

function showErrors(): void {
  if (!view) return

  view.dispatch(setDiagnostics(view.state, toDiagnostics(view.state, props.errors)))
}

onMounted(() => {
  if (!host.value) return

  view = new EditorView({
    parent: host.value,
    state: EditorState.create({
      doc: props.modelValue,
      extensions: [
        basicSetup,
        EditorView.lineWrapping,
        StreamLanguage.define({ ...vbScript, languageData: { indentOnInput: dedentOnInput } }),
        Prec.high(
          indentService.of((context, pos) => {
            const line = context.state.doc.lineAt(pos)
            const before = context.state.sliceDoc(0, line.from)

            return indentLevelFor(before, line.text) * indentUnit.length
          }),
        ),
        autocompletion({
          override: [
            fieldSource(intellisense),
            memberSource(intellisense),
            keywordSource(intellisense),
          ],
        }),
        ...ruleIntellisense(intellisense),
        EditorView.contentAttributes.of({ 'aria-label': props.label }),
        EditorView.theme({
          '&': {
            backgroundColor: 'var(--color-base-100)',
            color: 'var(--color-base-content)',
            height: '100%',
          },
          '&.cm-focused': { outline: '2px solid var(--color-primary)' },
          '.cm-scroller': { fontFamily: 'var(--font-mono, monospace)', overflow: 'auto' },
          '.cm-gutters': {
            backgroundColor: 'var(--color-base-200)',
            color: 'var(--color-secondary)',
            borderRight: '1px solid var(--color-base-300)',
          },
          '.cm-activeLine, .cm-activeLineGutter': { backgroundColor: 'var(--color-base-200)' },
          '.cm-cursor': { borderLeftColor: 'var(--color-base-content)' },
        }),
        EditorView.updateListener.of((update) => {
          if (update.docChanged) emit('update:modelValue', update.state.doc.toString())
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

    view.dispatch({ changes: { from: 0, to: view.state.doc.length, insert: value } })
  },
)

watch(() => props.errors, showErrors)
function focusAt(line: number, column: number | null, endColumn: number | null): void {
  if (!view) return

  const target = view.state.doc.line(Math.min(Math.max(line + 1, 1), view.state.doc.lines))
  const from = target.from + Math.min(column ?? 0, target.length)
  const to = endColumn === null ? target.to : target.from + Math.min(endColumn, target.length)

  view.dispatch({ selection: { anchor: from, head: Math.max(to, from) }, scrollIntoView: true })
  view.focus()
}

defineExpose({ focusAt })
</script>

<template>
  <div
    ref="host"
    class="min-h-56 flex-1 overflow-hidden rounded-field border border-base-300"
    data-testid="rule-coder"
  ></div>
</template>

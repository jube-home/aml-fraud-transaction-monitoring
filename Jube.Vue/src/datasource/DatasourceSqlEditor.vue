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
import { EditorState } from '@codemirror/state'
import { EditorView } from '@codemirror/view'
import { setDiagnostics, type Diagnostic } from '@codemirror/lint'
import { StreamLanguage } from '@codemirror/language'
import { pgSQL } from '@codemirror/legacy-modes/mode/sql'
import { basicSetup } from 'codemirror'
import { editorLook } from './editorTheme'

const props = defineProps<{
  modelValue: string
  label: string
  readonly?: boolean
  problem?: { line: number; column: number; message: string } | null
}>()

const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const host = ref<HTMLElement>()

let view: EditorView | undefined

function showProblem(): void {
  if (!view) return

  const found = props.problem
  const diagnostics: Diagnostic[] = []

  if (found && found.line >= 1 && found.line <= view.state.doc.lines) {
    const line = view.state.doc.line(found.line)
    const from = Math.min(line.from + Math.max(found.column - 1, 0), line.to)
    const to = Math.min(Math.max(from + 1, from), Math.max(line.to, from))

    diagnostics.push({
      from,
      to: to > from ? to : line.to,
      severity: 'error',
      message: found.message,
    })
  }

  view.dispatch(setDiagnostics(view.state, diagnostics))
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
        StreamLanguage.define(pgSQL),
        EditorState.readOnly.of(props.readonly ?? false),
        EditorView.contentAttributes.of({ 'aria-label': props.label }),
        ...editorLook(),
        EditorView.updateListener.of((update) => {
          if (update.docChanged) emit('update:modelValue', update.state.doc.toString())
        }),
      ],
    }),
  })

  showProblem()
})

onBeforeUnmount(() => {
  view?.destroy()
  view = undefined
})

watch(() => props.problem, showProblem)

watch(
  () => props.modelValue,
  (value) => {
    if (!view || value === view.state.doc.toString()) return

    view.dispatch({ changes: { from: 0, to: view.state.doc.length, insert: value } })
  },
)
</script>

<template>
  <div
    ref="host"
    class="min-h-40 overflow-hidden rounded-field border border-base-300"
    data-testid="datasource-sql"
  ></div>
</template>

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
import { StreamLanguage } from '@codemirror/language'
import { html } from '@codemirror/legacy-modes/mode/xml'
import { basicSetup } from 'codemirror'
import { editorLook } from './editorTheme'

const props = defineProps<{ modelValue: string; label: string; readonly?: boolean }>()

const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const host = ref<HTMLElement>()

let view: EditorView | undefined

onMounted(() => {
  if (!host.value) return

  view = new EditorView({
    parent: host.value,
    state: EditorState.create({
      doc: props.modelValue,
      extensions: [
        basicSetup,
        EditorView.lineWrapping,
        StreamLanguage.define(html),
        EditorState.readOnly.of(props.readonly ?? false),
        EditorView.contentAttributes.of({ 'aria-label': props.label }),
        ...editorLook(),
        EditorView.updateListener.of((update) => {
          if (update.docChanged) emit('update:modelValue', update.state.doc.toString())
        }),
      ],
    }),
  })
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
</script>

<template>
  <div
    ref="host"
    class="min-h-56 overflow-hidden rounded-field border border-base-300"
    data-testid="datasource-template"
  ></div>
</template>

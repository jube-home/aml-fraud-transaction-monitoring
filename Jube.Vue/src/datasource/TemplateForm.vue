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
import { computed, onMounted, useId, useTemplateRef, watch } from 'vue'
import { apply, clean, collect, decorate, showErrors, type FieldValues } from './templateForm'

const props = defineProps<{
  html: string
  label: string
  visibleFields?: string[]
  values?: FieldValues
  errors?: Record<string, string>
}>()

const emit = defineEmits<{ 'update:values': [values: FieldValues] }>()

const host = useTemplateRef<HTMLDivElement>('host')
const prefix = `tf-${useId()}`

const structure = computed(() => (props.visibleFields ? props.visibleFields.join('|') : props.html))

function mount(): void {
  const target = host.value

  if (!target) return

  const focused = document.activeElement instanceof HTMLElement ? document.activeElement : null
  const focusedName = focused && target.contains(focused) ? focused.getAttribute('name') : null
  const parsed = new DOMParser().parseFromString(props.html, 'text/html')

  clean(parsed.body)
  target.replaceChildren(
    ...[...parsed.body.childNodes].map((node) => document.importNode(node, true)),
  )
  decorate(target, prefix)

  if (props.values) apply(target, props.values)

  showErrors(target, props.errors ?? {})

  if (focusedName) target.querySelector<HTMLElement>(`[name="${CSS.escape(focusedName)}"]`)?.focus()
}

function changed(): void {
  if (host.value) emit('update:values', collect(host.value))
}

onMounted(mount)
watch(structure, mount)
watch(
  () => props.errors,
  (errors) => {
    if (host.value) showErrors(host.value, errors ?? {})
  },
)
</script>

<template>
  <div
    ref="host"
    role="group"
    :aria-label="label"
    class="min-w-0"
    data-testid="template-form"
    @input="changed"
    @change="changed"
  ></div>
</template>

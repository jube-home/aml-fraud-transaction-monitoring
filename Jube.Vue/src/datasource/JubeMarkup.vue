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
import { onMounted, useTemplateRef, watch } from 'vue'
import { attributeAllowed, templateTags } from './templateVocabulary'

const props = defineProps<{ html: string; label: string }>()

const host = useTemplateRef<HTMLDivElement>('host')

function clean(parent: Node): void {
  for (const node of [...parent.childNodes]) {
    if (node.nodeType === Node.TEXT_NODE) continue

    if (node.nodeType !== Node.ELEMENT_NODE) {
      node.parentNode?.removeChild(node)
      continue
    }

    const element = node as Element

    if (!templateTags.has(element.localName)) {
      element.remove()
      continue
    }

    for (const attribute of [...element.attributes]) {
      if (!attributeAllowed(attribute.name)) element.removeAttribute(attribute.name)
    }

    clean(element)
  }
}

function mount(): void {
  const target = host.value

  if (!target) return

  const parsed = new DOMParser().parseFromString(props.html, 'text/html')

  clean(parsed.body)
  target.replaceChildren(
    ...[...parsed.body.childNodes].map((node) => document.importNode(node, true)),
  )
}

onMounted(mount)
watch(() => props.html, mount)
</script>

<template>
  <div
    ref="host"
    role="group"
    :aria-label="label"
    class="min-w-0 overflow-auto"
    data-testid="datasource-markup"
  ></div>
</template>

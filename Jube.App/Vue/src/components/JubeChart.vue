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
import {onMounted, useTemplateRef, watch} from 'vue'
import {useUiStore} from '../stores/ui'
import {chartThemeOf} from './chartTheme'

const props = defineProps<{
  svg: string
  label: string
  interactive?: boolean
  selected?: string | null
  selectedEdge?: string | null
  hidden?: Set<string>
  emphasised?: Set<string>
}>()

const emit = defineEmits<{ pick: [id: string | null]; pickEdge: [id: string | null] }>()

const host = useTemplateRef<HTMLDivElement>('host')
const ui = useUiStore()

const blocked = new Set([
  'script',
  'foreignobject',
  'iframe',
  'object',
  'embed',
  'animate',
  'set',
  'animatetransform',
  'animatemotion',
])

function clean(root: Element): void {
  for (const node of [...root.querySelectorAll('*')]) {
    if (blocked.has(node.localName.toLowerCase())) {
      node.remove()
      continue
    }

    for (const attribute of [...node.attributes]) {
      const name = attribute.name.toLowerCase()

      if (name.startsWith('on')) node.removeAttribute(attribute.name)
      else if (name === 'href' || name.endsWith(':href')) {
        if (!attribute.value.trim().startsWith('#')) node.removeAttribute(attribute.name)
      }
    }
  }
}

function mount(): void {
  const target = host.value

  if (!target) return

  const parsed = new DOMParser().parseFromString(props.svg, 'image/svg+xml')
  const root = parsed.documentElement

  if (root.localName !== 'svg' || parsed.querySelector('parsererror')) {
    target.replaceChildren()

    return
  }

  clean(root)
  target.replaceChildren(document.importNode(root, true))
  theme()
  reach()
  mark()
  markEdges()
  hide()
  emphasise()
}

function reach(): void {
  if (!props.interactive) return

  for (const group of host.value?.querySelectorAll('[data-node], [data-edge]') ?? []) {
    if (!group.hasAttribute('tabindex')) group.setAttribute('tabindex', '0')
  }
}

function markEdges(): void {
  for (const edge of host.value?.querySelectorAll('[data-edge]') ?? []) {
    if (edge.getAttribute('data-edge') === props.selectedEdge)
      edge.setAttribute('data-selected', 'true')
    else edge.removeAttribute('data-selected')
  }
}

function flag(attribute: string, ids: Set<string> | undefined): void {
  for (const group of host.value?.querySelectorAll('[data-node], [data-edge]') ?? []) {
    const id = group.getAttribute('data-node') ?? group.getAttribute('data-edge') ?? ''

    if (ids?.has(id)) group.setAttribute(attribute, 'true')
    else group.removeAttribute(attribute)
  }
}

function hide(): void {
  flag('data-hidden', props.hidden)
}

function emphasise(): void {
  flag('data-emphasis', props.emphasised)
}

function theme(): void {
  host.value
    ?.querySelector(':scope > svg')
    ?.setAttribute('data-theme', chartThemeOf(ui.resolvedTheme))
}

function mark(): void {
  for (const node of host.value?.querySelectorAll('[data-node]') ?? []) {
    if (node.getAttribute('data-node') === props.selected)
      node.setAttribute('data-selected', 'true')
    else node.removeAttribute('data-selected')
  }
}

function idOf(target: EventTarget | null): string | null {
  return (target as Element | null)?.closest('[data-node]')?.getAttribute('data-node') ?? null
}

function edgeOf(target: EventTarget | null): string | null {
  return (target as Element | null)?.closest('[data-edge]')?.getAttribute('data-edge') ?? null
}

function onClick(event: MouseEvent): void {
  if (!props.interactive) return

  const id = idOf(event.target)

  if (id === null) {
    const edge = edgeOf(event.target)

    if (edge !== null) {
      emit('pickEdge', edge)

      return
    }
  }

  emit('pick', id)
}

function onKey(event: KeyboardEvent): void {
  if (!props.interactive) return

  if (event.key === 'Escape') {
    emit('pick', null)
    emit('pickEdge', null)
  } else if (event.key === 'Enter' || event.key === ' ') {
    const id = idOf(event.target)

    if (id) {
      event.preventDefault()
      emit('pick', id)

      return
    }

    const edge = edgeOf(event.target)

    if (edge) {
      event.preventDefault()
      emit('pickEdge', edge)
    }
  }
}

onMounted(mount)
watch(() => props.svg, mount)
watch(() => props.selected, mark)
watch(() => props.selectedEdge, markEdges)
watch(() => props.hidden, hide)
watch(() => props.emphasised, emphasise)
watch(() => ui.resolvedTheme, theme)
</script>

<template>
  <div
    ref="host"
    :role="interactive ? 'group' : 'img'"
    :aria-label="label"
    class="w-full [&>svg]:h-auto [&>svg]:max-w-full [&_[data-node]]:cursor-pointer [&_[data-edge]]:cursor-pointer [&_[data-selected=true]_.nb]:stroke-primary [&_[data-selected=true]_.nb]:stroke-[3px] [&_[data-selected=true]_.ep]:stroke-primary [&_[data-selected=true]_.ep]:stroke-[3px] [&_[data-emphasis=true]_.ep]:stroke-info [&_[data-emphasis=true]_.ep]:stroke-[3px] [&_[data-emphasis=true]_.nb]:stroke-info [&_[data-emphasis=true]_.nb]:stroke-[3px] [&_[data-hidden=true]]:hidden"
    data-testid="jube-chart"
    @click="onClick"
    @keydown="onKey"
  ></div>
</template>

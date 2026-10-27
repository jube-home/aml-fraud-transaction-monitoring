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
import { useI18n } from 'vue-i18n'
import { ChevronUp } from 'lucide-vue-next'
import type { FactTone, StoryFact } from '../story/model'

withDefaults(
  defineProps<{
    id: string
    title: string
    name?: string
    facts?: StoryFact[]
    changed?: boolean
    unsaved?: boolean
    working: boolean
  }>(),
  { facts: () => [] },
)

const emit = defineEmits<{ reopen: [] }>()

const { t } = useI18n()

const tones: Record<FactTone, string> = {
  neutral: 'badge-neutral',
  success: 'badge-success',
  warning: 'badge-warning',
  error: 'badge-error',
  info: 'badge-info',
}

function toneOf(fact: StoryFact): string {
  return tones[fact.tone ?? 'neutral']
}
</script>

<template>
  <li
    class="scroll-mt-14"
    :aria-current="working ? 'step' : undefined"
    :data-testid="`beat-${id}`"
    :data-state="working ? 'working' : 'receipt'"
  >
    <button
      v-if="!working"
      key="receipt"
      type="button"
      class="group flex w-full flex-wrap items-center gap-x-3 gap-y-1 rounded-box border border-base-300 bg-base-200 px-4 py-2 text-start hover:bg-base-100 focus-visible:outline-2 focus-visible:outline-offset-2"
      :data-testid="`receipt-${id}`"
      @click="emit('reopen')"
    >
      <span class="sr-only">{{ t('story.goBack') }}</span>
      <span class="font-mono text-xs text-secondary">{{ title }}</span>
      <span v-if="name" key="name" class="text-sm font-semibold">{{ name }}</span>
      <span class="flex flex-1 flex-wrap items-center gap-1.5">
        <span
          v-for="fact in facts"
          :key="fact.text"
          class="badge badge-sm font-mono"
          :class="toneOf(fact)"
          >{{ fact.text }}</span
        >
        <span v-if="changed" key="changed" class="badge badge-soft badge-info badge-sm font-mono">{{
          t('story.changed')
        }}</span>
      </span>
      <ChevronUp class="size-4 text-secondary" aria-hidden="true" />
    </button>

    <section
      v-else
      key="working"
      class="flex flex-col gap-4 rounded-box border border-base-300 border-t-2 border-t-accent bg-base-100 p-4 transition-opacity duration-200 starting:opacity-0 motion-reduce:transition-none"
      :aria-labelledby="`beat-title-${id}`"
    >
      <header class="flex flex-wrap items-center gap-x-3 gap-y-1">
        <h2 :id="`beat-title-${id}`" tabindex="-1" class="text-lg font-semibold outline-none">
          {{ title }}
        </h2>
        <span v-if="name" key="name" class="badge badge-soft font-mono">{{ name }}</span>
        <span
          v-for="fact in facts"
          :key="fact.text"
          class="badge badge-sm font-mono"
          :class="toneOf(fact)"
          >{{ fact.text }}</span
        >
        <span v-if="changed" key="changed" class="badge badge-soft badge-info badge-sm font-mono">{{
          t('story.changed')
        }}</span>
        <span
          v-if="unsaved"
          key="unsaved"
          class="badge badge-sm font-mono badge-warning"
          :data-testid="`unsaved-${id}`"
          >{{ t('story.unsaved') }}</span
        >
      </header>
      <slot />
    </section>
  </li>
</template>

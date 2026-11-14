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
import {computed, onBeforeUnmount, onMounted, ref} from 'vue'
import {useI18n} from 'vue-i18n'
import {useQuery} from '@tanstack/vue-query'
import {X} from 'lucide-vue-next'
import {journalApi} from './api'
import ModelFacts from './ModelFacts.vue'

const props = defineProps<{ workflow: string; id: number }>()

const emit = defineEmits<{ close: [] }>()

const {t} = useI18n()

const now = new Date()

const card = useQuery({
  queryKey: computed(
    () => ['service', 'Case', 'keyjournal', 'transaction', props.workflow, props.id] as const,
  ),
  queryFn: () => journalApi.transaction(props.workflow, props.id),
  staleTime: 30_000,
  retry: false,
})

const models = computed(() => card.data.value?.adaptations ?? [])
const opened = ref<boolean[]>([])

function isOpen(index: number): boolean {
  return opened.value[index] ?? index === 0
}

function toggled(index: number, event: Event): void {
  const next = [...opened.value]

  next[index] = (event.target as HTMLDetailsElement).open
  opened.value = next
}

function jump(index: number): void {
  const next = [...opened.value]

  next[index] = true
  opened.value = next

  document.getElementById(`journal-facts-model-${index}`)?.scrollIntoView?.({block: 'start'})
}

function keys(event: KeyboardEvent): void {
  if (event.key === 'Escape') emit('close')
}

onMounted(() => window.addEventListener('keydown', keys))
onBeforeUnmount(() => window.removeEventListener('keydown', keys))
</script>

<template>
  <aside
    class="fixed inset-y-0 end-0 z-30 flex w-full max-w-xl flex-col gap-3 overflow-y-auto border-s border-base-300 bg-base-100 p-4 shadow-lg"
    :aria-label="t('journal.facts.label')"
    data-testid="journal-facts-drawer"
  >
    <div class="flex items-center justify-between gap-2">
      <h3 class="font-mono text-base font-semibold" data-testid="journal-facts-title">
        {{ t('journal.facts.title', {id}) }}
      </h3>
      <button
        type="button"
        class="btn btn-ghost btn-sm btn-square"
        :aria-label="t('journal.facts.close')"
        data-testid="journal-facts-close"
        @click="emit('close')"
      >
        <X class="size-4" aria-hidden="true"/>
      </button>
    </div>

    <div v-if="card.isPending.value" key="loading" class="flex flex-col gap-2" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
      <div class="skeleton h-6 w-40"></div>
      <div class="skeleton h-32 w-full"></div>
    </div>

    <div v-else-if="card.isError.value" key="failed" role="alert" class="alert alert-error">
      <span>{{ t('journal.facts.failed') }}</span>
    </div>

    <p
      v-else-if="models.length === 0"
      key="none"
      class="text-sm text-base-content/70"
      role="status"
    >
      {{ t('journal.facts.none') }}
    </p>

    <template v-else>
      <nav
        class="sticky top-0 z-10 -mx-4 flex flex-wrap gap-1 border-b border-base-300 bg-base-100 px-4 py-2"
        :aria-label="t('journal.facts.jump')"
        data-testid="journal-facts-jump"
      >
        <button
          v-for="(model, index) in models"
          :key="`${model.source}-${model.name}`"
          type="button"
          class="btn btn-sm btn-ghost"
          :data-testid="`journal-facts-jump-${model.name}`"
          @click="jump(index)"
        >
          {{ model.name }}
        </button>
      </nav>

      <details
        v-for="(model, index) in models"
        :id="`journal-facts-model-${index}`"
        :key="`${model.source}-${model.name}`"
        class="collapse collapse-arrow border border-base-300 bg-base-100"
        :open="isOpen(index)"
        :data-testid="`journal-facts-model-${model.name}`"
        @toggle="toggled(index, $event)"
      >
        <summary class="collapse-title text-sm font-semibold">{{ model.name }}</summary>
        <div class="collapse-content">
          <ModelFacts :adaptation="model" :workflow="workflow" :id="id" :now="now"/>
        </div>
      </details>
    </template>
  </aside>
</template>

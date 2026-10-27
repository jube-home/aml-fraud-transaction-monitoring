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
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { decodeStory, encodeStory, type Beat, type StoryFact } from '../../story/model'
import StorySpine from '../../components/StorySpine.vue'

interface Choice {
  id: string
  name: string
  facts: StoryFact[]
}

const models: Choice[] = [
  {
    id: 'card',
    name: 'Card fraud model',
    facts: [
      { text: 'v14' },
      { text: 'Active', tone: 'success' },
      { text: 'Approved', tone: 'success' },
    ],
  },
  {
    id: 'aml',
    name: 'AML transaction model',
    facts: [
      { text: 'v6' },
      { text: 'Locked', tone: 'warning' },
      { text: 'Pending approval', tone: 'warning' },
    ],
  },
]

const areas: Choice[] = [
  {
    id: 'rules',
    name: 'Activation rules',
    facts: [{ text: '12' }, { text: '2 awaiting approval', tone: 'warning' }],
  },
  { id: 'tags', name: 'Tags', facts: [{ text: '8' }] },
  { id: 'lists', name: 'Lists', facts: [{ text: '4' }] },
]

const records: Choice[] = [
  { id: 'velocity', name: 'High velocity', facts: [{ text: 'Active', tone: 'success' }] },
  { id: 'geo', name: 'Impossible travel', facts: [{ text: 'Locked', tone: 'warning' }] },
]

const levels = [models, areas, records]

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const choices = ref<string[]>(
  decodeStory(typeof route.query.story === 'string' ? route.query.story : undefined).slice(0, 3),
)
const changed = ref(false)
const unsaved = ref(false)

const titles = computed(() => [
  t('story.bench.models'),
  t('story.bench.areas'),
  t('story.bench.records'),
  t('story.bench.record'),
])

const beats = computed<Beat[]>(() =>
  titles.value.slice(0, choices.value.length + 1).map((title, index) => {
    const picked = levels[index]?.find((choice) => choice.id === choices.value[index])

    return {
      id: `level-${index}`,
      title,
      name: picked?.name,
      facts: picked?.facts ?? [],
      changed: index === 0 && changed.value && picked !== undefined,
      unsaved: index === choices.value.length && index === 3 && unsaved.value,
    }
  }),
)

function remember(): void {
  void router.replace({
    query: {
      ...route.query,
      story: choices.value.length > 0 ? encodeStory(choices.value) : undefined,
    },
  })
}

function pick(choice: Choice): void {
  choices.value = [...choices.value, choice.id]
  unsaved.value = false
  remember()
}

function back(index: number): void {
  choices.value = choices.value.slice(0, index)
  unsaved.value = false
  remember()
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto" data-testid="story-bench">
    <div class="flex flex-wrap items-center gap-x-6 gap-y-2">
      <label class="flex items-center gap-2 text-sm">
        <input
          v-model="changed"
          type="checkbox"
          class="checkbox checkbox-sm"
          data-testid="bench-changed"
        />
        {{ t('story.bench.simulateChanged') }}
      </label>
      <label class="flex items-center gap-2 text-sm">
        <input
          v-model="unsaved"
          type="checkbox"
          class="checkbox checkbox-sm"
          data-testid="bench-unsaved"
        />
        {{ t('story.bench.simulateUnsaved') }}
      </label>
    </div>

    <StorySpine :beats="beats" @reopen="back">
      <template #default="{ index }">
        <ul v-if="index < 3" key="choose" class="flex flex-col gap-2">
          <li
            v-for="choice in levels[index]"
            :key="choice.id"
            class="flex flex-wrap items-center gap-2"
          >
            <button
              type="button"
              class="btn btn-sm"
              :data-testid="`pick-${choice.id}`"
              @click="pick(choice)"
            >
              {{ t('story.bench.pick', { name: choice.name }) }}
            </button>
            <span
              v-for="fact in choice.facts"
              :key="fact.text"
              class="badge badge-sm badge-neutral font-mono"
              >{{ fact.text }}</span
            >
          </li>
        </ul>
        <div v-else key="record" class="flex flex-col gap-2">
          <p class="text-sm">{{ t('story.bench.hint') }}</p>
          <label class="flex max-w-sm flex-col gap-1 text-sm">
            {{ t('story.bench.name') }}
            <input type="text" class="input input-sm" :value="records[0]?.name" />
          </label>
        </div>
      </template>
    </StorySpine>
  </div>
</template>

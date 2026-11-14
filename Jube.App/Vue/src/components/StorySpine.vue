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
import {computed, nextTick, ref, watch} from 'vue'
import {useI18n} from 'vue-i18n'
import {whereOf, type Beat} from '../story/model'
import StoryBeat from './StoryBeat.vue'

const props = defineProps<{ beats: Beat[] }>()

const emit = defineEmits<{ reopen: [index: number] }>()

defineSlots<{ default(props: { beat: Beat; index: number }): unknown }>()

const {t} = useI18n()
const announcement = ref('')

const last = computed(() => props.beats.length - 1)

watch(
  () => props.beats.map((beat) => beat.id).join(','),
  async (now, before) => {
    if (before === undefined || now === before) return

    const current = props.beats[last.value]

    announcement.value = current ? t('story.showing', {where: whereOf(current)}) : ''
    await nextTick()
    if (!current) return

    const heading = document.getElementById(`beat-title-${current.id}`)

    heading?.focus({preventScroll: true})
    heading?.scrollIntoView?.({block: 'start', behavior: 'smooth'})
  },
)
</script>

<template>
  <div class="flex flex-col gap-3" data-testid="story">
    <p class="sr-only" role="status" aria-live="polite" data-testid="story-announce">
      {{ announcement }}
    </p>

    <ol class="flex flex-col gap-3" :aria-label="t('story.list')">
      <template v-for="(beat, index) in beats" :key="beat.id">
        <StoryBeat
          v-if="index === last"
          key="working"
          :id="beat.id"
          :title="beat.title"
          :name="beat.name"
          :facts="beat.facts"
          :changed="beat.changed"
          :unsaved="beat.unsaved"
          :working="true"
        >
          <slot :beat="beat" :index="index"/>
        </StoryBeat>
        <StoryBeat
          v-else
          key="receipt"
          :id="beat.id"
          :title="beat.title"
          :name="beat.name"
          :facts="beat.facts"
          :changed="beat.changed"
          :working="false"
          @reopen="emit('reopen', index)"
        />
      </template>
    </ol>
  </div>
</template>

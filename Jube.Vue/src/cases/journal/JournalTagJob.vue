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
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Undo2 } from 'lucide-vue-next'
import { useFormat } from '../../composables/useFormat'
import { isActive, percent } from './jobs'
import type { TagJob } from './types'

const props = defineProps<{ job: TagJob; busy: boolean }>()

const emit = defineEmits<{ cancel: []; undo: []; dismiss: [] }>()

const { t } = useI18n()
const { number } = useFormat()

const undoing = computed(() => props.job.kind === 'Undo')
const active = computed(() => isActive(props.job))

const headline = computed(() => {
  const n = { done: number(props.job.processed), total: number(props.job.total) }
  const changed = { changed: number(props.job.changed), total: number(props.job.total) }

  if (props.job.state === 'Queued') return t('journal.job.queued')
  if (props.job.state === 'Running')
    return undoing.value ? t('journal.job.undoing', n) : t('journal.job.running', n)
  if (props.job.state === 'Done')
    return undoing.value ? t('journal.job.restored', changed) : t('journal.job.done', changed)
  if (props.job.state === 'Cancelled') return t('journal.job.cancelled', n)

  return t('journal.job.failed', { error: props.job.error ?? '' })
})

const notes = computed(() =>
  [
    props.job.skipped > 0 ? t('journal.job.skipped', { n: number(props.job.skipped) }) : '',
    props.job.locked > 0 ? t('journal.tags.locked', { n: number(props.job.locked) }) : '',
  ].filter((note) => note !== ''),
)
</script>

<template>
  <div
    role="status"
    class="alert alert-soft alert-vertical sm:alert-horizontal"
    data-testid="journal-job"
  >
    <div class="flex min-w-0 flex-1 flex-col gap-2">
      <span class="text-sm font-semibold" data-testid="journal-job-headline">{{ headline }}</span>
      <progress
        v-if="active"
        key="progress"
        class="progress w-full"
        :value="job.state === 'Queued' ? undefined : percent(job)"
        max="100"
        :aria-label="t('journal.job.progress')"
        data-testid="journal-job-progress"
      ></progress>
      <span v-if="notes.length > 0" key="notes" class="text-xs" data-testid="journal-job-notes">
        {{ notes.join(' ') }}
      </span>
    </div>
    <div class="flex gap-2">
      <button
        v-if="active"
        key="cancel"
        type="button"
        class="btn btn-sm"
        :disabled="busy"
        data-testid="journal-job-cancel"
        @click="emit('cancel')"
      >
        {{ t('journal.job.cancel') }}
      </button>
      <button
        v-if="job.canUndo"
        key="undo"
        type="button"
        class="btn btn-sm"
        :disabled="busy"
        data-testid="journal-job-undo"
        @click="emit('undo')"
      >
        <Undo2 class="size-4" aria-hidden="true" />
        {{ t('journal.tags.undo') }}
      </button>
      <button
        v-if="!active"
        key="dismiss"
        type="button"
        class="btn btn-sm btn-ghost"
        data-testid="journal-job-dismiss"
        @click="emit('dismiss')"
      >
        {{ t('journal.tags.dismiss') }}
      </button>
    </div>
  </div>
</template>

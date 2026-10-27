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
import { CircleStop, RefreshCw } from 'lucide-vue-next'
import { useFormat } from '../composables/useFormat'
import { canStop, phaseOf } from '../exhaustive/model'
import type { ExhaustiveInstance } from '../exhaustive/types'

const props = defineProps<{ instance: ExhaustiveInstance; busy?: boolean; stopping?: boolean }>()

const emit = defineEmits<{ stop: []; refresh: [] }>()

const { t } = useI18n()
const { dateTime, fixed, number } = useFormat()

const phase = computed(() => phaseOf(props.instance.statusId))
const stage = computed(() => Math.min(props.instance.statusId, 16))

const label = computed(() => {
  switch (props.instance.statusId) {
    case 0:
      return t('exhaustive.status.awaitingServer')
    case 1:
      return t('exhaustive.status.baseQuery')
    case 2:
      return t('exhaustive.status.baseStatistics')
    case 3:
      return t('exhaustive.status.normalisingUnsupervised')
    case 4:
      return t('exhaustive.status.unsupervisedTraining')
    case 5:
      return t('exhaustive.status.unsupervisedRecall')
    case 6:
      return t('exhaustive.status.unsupervisedStatistics')
    case 7:
      return t('exhaustive.status.classQuery')
    case 8:
      return t('exhaustive.status.classStatistics')
    case 9:
      return t('exhaustive.status.shuffling')
    case 10:
      return t('exhaustive.status.normalising')
    case 11:
      return t('exhaustive.status.oversampling')
    case 12:
      return t('exhaustive.status.storing')
    case 13:
      return t('exhaustive.status.correlation')
    case 14:
      return t('exhaustive.status.multicollinearity')
    case 15:
      return t('exhaustive.status.supervised')
    case 16:
      return t('exhaustive.status.finished')
    case 17:
      return t('exhaustive.status.finishedNoClass')
    case 18:
      return t('exhaustive.status.stopping')
    case 19:
      return t('exhaustive.status.stopped')
    case 20:
      return t('exhaustive.status.noData')
    default:
      return t('exhaustive.status.unexpected')
  }
})

const when = (value: string | null) => (value ? dateTime(new Date(value)) : '–')
</script>

<template>
  <div
    class="sticky top-0 z-20 flex flex-wrap items-center gap-x-6 gap-y-2 rounded-box border border-base-300 border-t-2 border-t-accent bg-base-100 px-4 py-2"
    data-testid="exhaustive-band"
  >
    <div class="flex min-w-0 flex-col gap-1">
      <p class="flex items-center gap-2 text-sm font-semibold" data-testid="exhaustive-status">
        <span
          :class="[
            'status',
            phase === 'finished'
              ? 'status-success'
              : phase === 'running'
                ? 'status-info'
                : phase === 'stopping'
                  ? 'status-warning'
                  : 'status-error',
          ]"
          aria-hidden="true"
        ></span>
        {{ label }}
      </p>
      <div
        v-if="phase === 'running' || phase === 'stopping'"
        key="stage"
        class="flex items-center gap-2 text-xs text-secondary"
      >
        <progress
          class="progress w-32"
          :value="stage"
          max="16"
          :aria-label="t('exhaustive.stageLabel')"
          data-testid="exhaustive-progress"
        ></progress>
        <span class="font-mono tabular-nums">{{
          t('exhaustive.stage', { stage, total: 16 })
        }}</span>
      </div>
    </div>

    <dl class="flex flex-wrap gap-x-5 gap-y-1 text-xs">
      <div class="flex flex-col">
        <dt class="text-secondary">{{ t('exhaustive.band.models') }}</dt>
        <dd class="font-mono tabular-nums" data-testid="exhaustive-models">
          {{ number(instance.models) }}
        </dd>
      </div>
      <div class="flex flex-col">
        <dt class="text-secondary">{{ t('exhaustive.band.sinceBest') }}</dt>
        <dd class="font-mono tabular-nums">{{ number(instance.modelsSinceBest) }}</dd>
      </div>
      <div class="flex flex-col">
        <dt class="text-secondary">{{ t('exhaustive.band.best') }}</dt>
        <dd class="font-mono tabular-nums" data-testid="exhaustive-score">
          {{ fixed(instance.score, 4) }}
        </dd>
      </div>
      <div class="flex flex-col">
        <dt class="text-secondary">{{ t('exhaustive.band.updated') }}</dt>
        <dd class="font-mono tabular-nums">{{ when(instance.updatedDate) }}</dd>
      </div>
      <div v-if="instance.completedDate" key="completed" class="flex flex-col">
        <dt class="text-secondary">{{ t('exhaustive.band.completed') }}</dt>
        <dd class="font-mono tabular-nums">{{ when(instance.completedDate) }}</dd>
      </div>
    </dl>

    <div class="ms-auto flex items-center gap-2">
      <button
        type="button"
        class="btn btn-sm"
        :disabled="busy"
        data-testid="exhaustive-refresh"
        @click="emit('refresh')"
      >
        <RefreshCw aria-hidden="true" class="size-4" />
        {{ t('exhaustive.refresh') }}
      </button>
      <button
        v-if="canStop(instance.statusId)"
        key="stop"
        type="button"
        class="btn btn-sm btn-error btn-outline"
        :disabled="stopping"
        data-testid="exhaustive-stop"
        @click="emit('stop')"
      >
        <CircleStop aria-hidden="true" class="size-4" />
        {{ t('exhaustive.stop') }}
      </button>
    </div>
  </div>
</template>

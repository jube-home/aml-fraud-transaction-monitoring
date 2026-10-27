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
import { useAnalyticsLabels } from '../askJoobyAnalytics/labels'
import type { LiveStanding } from '../askJoobyAnalytics/model'
import type { ModelVersion, TrainedModel } from '../askJoobyAnalytics/types'

const props = defineProps<{
  model: TrainedModel
  standing: LiveStanding
  viewed: ModelVersion | undefined
  instanceName?: string
}>()

const emit = defineEmits<{ live: [] }>()

const { t } = useI18n()
const { kindLabel, sourceLabel } = useAnalyticsLabels()

const subtitle = computed(() =>
  [kindLabel(props.model.kind), sourceLabel(props.model.source), props.instanceName]
    .filter(Boolean)
    .join(' · '),
)

const viewingOther = computed(
  () => props.viewed !== undefined && props.model.activeVersionId !== props.viewed.id,
)
</script>

<template>
  <div
    class="sticky top-0 z-20 flex flex-wrap items-center gap-x-6 gap-y-2 rounded-box border border-base-300 border-t-2 border-t-accent bg-base-100 px-4 py-2"
    data-testid="analytics-band"
  >
    <div class="flex min-w-0 flex-col gap-1">
      <p class="truncate text-sm font-semibold" data-testid="analytics-model-name">
        {{ model.name }}
      </p>
      <p class="text-xs text-secondary">{{ subtitle }}</p>
    </div>

    <p class="flex items-center gap-2 text-sm font-semibold" data-testid="analytics-live">
      <span
        :class="['status', model.activeVersionNumber ? 'status-success' : 'status-warning']"
        aria-hidden="true"
      ></span>
      {{
        model.activeVersionNumber
          ? t('analytics.band.live', { version: model.activeVersionNumber })
          : t('analytics.band.noLive')
      }}
    </p>

    <p
      v-if="standing.liveId !== null && standing.of > 0"
      key="standing"
      class="font-mono text-xs tabular-nums"
      data-testid="analytics-standing"
    >
      {{
        standing.allBest
          ? t('analytics.band.bestOnAll')
          : t('analytics.band.bestOn', { best: standing.best, of: standing.of })
      }}
    </p>

    <p
      v-if="viewed"
      key="viewed"
      class="flex items-center gap-2 text-xs"
      data-testid="analytics-viewing"
    >
      <span class="text-secondary">{{
        t('analytics.band.viewing', { version: viewed.versionNumber })
      }}</span>
      <span
        v-if="viewed.warningCodes.length > 0"
        key="warnings"
        class="badge badge-sm badge-soft"
        >{{
          t(
            'analytics.band.warnings',
            { count: viewed.warningCodes.length },
            viewed.warningCodes.length,
          )
        }}</span
      >
    </p>

    <button
      v-if="viewingOther"
      key="back"
      type="button"
      class="btn btn-sm ms-auto"
      data-testid="analytics-back-to-live"
      @click="emit('live')"
    >
      {{ t('analytics.band.backToLive') }}
    </button>
  </div>
</template>

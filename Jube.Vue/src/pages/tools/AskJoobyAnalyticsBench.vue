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
import { useQuery } from '@tanstack/vue-query'
import { hasPermission, meQueryOptions } from '../../auth/me'
import { analyticsPermission, type InstanceRow } from '../../askJoobyAnalytics/model'
import type { TrainedModel } from '../../askJoobyAnalytics/types'
import AskJoobyAnalyticsInstances from '../../components/AskJoobyAnalyticsInstances.vue'
import AskJoobyAnalyticsModelCanvas from '../../components/AskJoobyAnalyticsModelCanvas.vue'

const { t } = useI18n()
const me = useQuery(meQueryOptions)

const allowed = computed(() => hasPermission(me.data.value, ...analyticsPermission.view))
const canSetActive = computed(() => hasPermission(me.data.value, analyticsPermission.setActive))
const canDelete = computed(() => hasPermission(me.data.value, analyticsPermission.delete))
const canDeleteAllocated = computed(() => canDelete.value && canSetActive.value)

const opened = ref<{ model: TrainedModel; instance: InstanceRow } | null>(null)

function open(model: TrainedModel, instance: InstanceRow): void {
  opened.value = { model, instance }
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto" data-testid="analytics-bench">
    <nav class="breadcrumbs shrink-0 text-sm" :aria-label="t('analytics.trail')">
      <ul>
        <li>
          <button
            v-if="opened"
            key="up"
            type="button"
            class="link"
            data-testid="analytics-trail-instances"
            @click="opened = null"
          >
            {{ t('analytics.instances.title') }}
          </button>
          <span v-else key="here" aria-current="page" class="font-semibold">{{
            t('analytics.instances.title')
          }}</span>
        </li>
        <li v-if="opened" key="instance">{{ opened.instance.name }}</li>
        <li v-if="opened" key="model" aria-current="page" class="font-semibold">
          {{ opened.model.name }}
        </li>
      </ul>
    </nav>

    <p
      v-if="!allowed && !me.isPending.value"
      key="refused"
      class="text-sm"
      data-testid="analytics-refused"
    >
      {{ t('analytics.refused') }}
    </p>
    <AskJoobyAnalyticsModelCanvas
      v-else-if="opened"
      :key="opened.model.id"
      :model-id="opened.model.id"
      :instance-name="opened.instance.name"
    />
    <AskJoobyAnalyticsInstances
      v-else
      key="list"
      :can-set-active="canSetActive"
      :can-delete="canDelete"
      :can-delete-allocated="canDeleteAllocated"
      @open="open"
    />
  </div>
</template>

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
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import PanelCard from '../components/PanelCard.vue'
import PreservationCompare from './PreservationCompare.vue'
import PreservationRestore from './PreservationRestore.vue'
import PreservationSnapshots from './PreservationSnapshots.vue'
import PreservationTake from './PreservationTake.vue'
import { snapshotApi } from './api'
import { compareDefaults } from './state'

const { t } = useI18n()

const query = useQuery({
  queryKey: queryKeys.preservationSnapshots(),
  queryFn: snapshotApi.list,
})

const snapshots = computed(() => query.data.value ?? [])

const left = ref('live')
const right = ref('live')
const run = ref(0)
const restoring = ref<number | null>(null)

const restoringName = computed(() => {
  const snapshot = snapshots.value.find((item) => item.id === restoring.value)

  return snapshot?.name?.trim()
    ? snapshot.name
    : `${t('preservation.snapshots.unnamed')} ${restoring.value}`
})

let defaulted = false

watch(
  snapshots,
  (list) => {
    if (defaulted || list.length === 0) return

    defaulted = true

    const defaults = compareDefaults(list)

    left.value = defaults.left
    right.value = defaults.right
  },
  { immediate: true },
)

function compare(id: number): void {
  left.value = String(id)
  right.value = 'live'
  run.value += 1
}

function restore(id: number): void {
  restoring.value = id
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="preservation-safeguard">
    <p
      v-if="query.isError.value"
      key="error"
      role="alert"
      class="alert alert-error text-sm"
      data-testid="safeguard-error"
    >
      {{ t('preservation.snapshots.loadFailed') }}
    </p>

    <PanelCard :level="2" :title="t('preservation.take.title')" :step="1">
      <PreservationTake />
    </PanelCard>

    <PanelCard :level="2" :title="t('preservation.snapshots.title')" :step="2">
      <div
        v-if="query.isPending.value"
        key="loading"
        class="skeleton h-24 w-full"
        data-testid="snapshots-loading"
      ></div>
      <PreservationSnapshots v-else :snapshots="snapshots" @compare="compare" @restore="restore" />
    </PanelCard>

    <PanelCard :level="2" :title="t('preservation.compare.title')" :step="3">
      <PreservationCompare
        v-model:left="left"
        v-model:right="right"
        :snapshots="snapshots"
        :run="run"
      />
    </PanelCard>

    <PanelCard
      v-if="restoring !== null"
      key="restore"
      :title="t('preservation.restore.title')"
      :step="4"
    >
      <PreservationRestore
        :id="restoring"
        :name="restoringName"
        @close="restoring = null"
        @restored="restoring = null"
      />
    </PanelCard>
  </div>
</template>

<!--  Copyright (C) 2022-present Jube Holdings Limited.

  This file is part of Jube™ software.

  Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
  as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
  Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
  of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

  You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
  see <https://www.gnu.org/licenses/>.
 -->

<script setup vapor lang="ts">
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import {useQuery} from '@tanstack/vue-query'
import {describeApiError} from '../../api/problems'
import ProblemAlert from '../../components/ProblemAlert.vue'
import SvirfdRecord from '../../components/svirfd/SvirfdRecord.vue'
import {queryKeys} from '../../api/queryKeys'
import {svirfdApi} from '../../svirfd/api'
import {tenantRemoteOff} from '../../svirfd/status'

const props = defineProps<{ workflow: string; id: number; field: string }>()

const emit = defineEmits<{ close: [] }>()

const {t} = useI18n()

const comparison = useQuery({
  queryKey: computed(
    () =>
      ['service', 'Case', 'keyjournal', 'svirfd', props.workflow, props.id, props.field] as const,
  ),
  queryFn: () => svirfdApi.journal(props.workflow, props.id, props.field),
  staleTime: 60_000,
  retry: false,
})

const status = useQuery({
  queryKey: queryKeys.svirfdStatus(),
  queryFn: () => svirfdApi.status(),
  staleTime: 60_000,
})

const tenantOff = computed(() => tenantRemoteOff(status.data.value))

const data = computed(() => comparison.data.value)

const reasons = computed(() =>
  comparison.error.value
    ? describeApiError(comparison.error.value, {
      failed: t('journal.svirfd.failed'),
      refused: t('svirfd.forbidden'),
    })
    : [],
)
</script>

<template>
  <section class="flex flex-col gap-3" data-testid="journal-svirfd">
    <header class="flex items-center justify-between gap-2">
      <h4 dir="auto" class="text-sm font-semibold">
        {{ t('journal.svirfd.title', {ip: data?.ip ?? ''}) }}
      </h4>
      <button
        type="button"
        class="btn btn-sm"
        data-testid="journal-svirfd-close"
        @click="emit('close')"
      >
        {{ t('journal.svirfd.close') }}
      </button>
    </header>

    <p
      v-if="tenantOff"
      class="alert alert-info text-sm"
      role="status"
      data-testid="journal-svirfd-tenant-remote-off"
    >
      {{ t('svirfd.tenantRemoteOff') }}
    </p>

    <div v-if="comparison.isPending.value" class="skeleton h-24 w-full" role="status">
      <span class="sr-only">{{ t('journal.svirfd.loading') }}</span>
    </div>

    <ProblemAlert v-else-if="comparison.isError.value" :reasons="reasons"/>

    <template v-else-if="data">
      <template v-if="data.fresh && data.stored">
        <SvirfdRecord
          :record="data.fresh.record"
          :rtt-chart-svg="data.fresh.rttChartSvg"
          :headline="data.fresh.headline"
          :narrative="data.fresh.narrative"
          :attribution="data.fresh.attribution"
          :licence-url="data.fresh.licenceUrl"
          :mode="data.fresh.mode"
          :stored="data.stored"
          :stored-at="data.storedAt"
          :drift="data.drift"
        />
      </template>
      <template v-else-if="data.stored">
        <p class="alert alert-soft text-sm" role="status" data-testid="journal-svirfd-unavailable">
          {{ t('journal.svirfd.unavailable') }}
        </p>
        <SvirfdRecord
          :record="data.stored"
          :attribution="data.fresh?.attribution ?? ''"
          :stored-at="data.storedAt"
        />
      </template>
    </template>
  </section>
</template>

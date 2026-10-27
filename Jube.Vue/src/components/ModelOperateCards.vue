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
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { integrityApi } from '../integrity/api'
import { verdictOf } from '../integrity/model'
import type { Operate } from '../story/path'
import HubActionCard from './HubActionCard.vue'

const props = defineProps<{
  modelId: number
  canSample: boolean
  canIntegrity: boolean
  canAnalytics: boolean
}>()

const emit = defineEmits<{ operate: [which: Operate] }>()

const { t } = useI18n()

const report = useQuery({
  queryKey: computed(() => queryKeys.integrityReport(props.modelId)),
  queryFn: () => integrityApi.report(props.modelId),
  enabled: computed(() => props.canIntegrity),
  retry: false,
  staleTime: 30_000,
})

const verdict = computed(() => (report.data.value ? verdictOf(report.data.value) : undefined))

const integrityPill = computed(() => {
  const found = verdict.value

  if (!found) return undefined

  return found === 'unsound'
    ? t('integrity.verdict.unsound')
    : found === 'warnings'
      ? t('integrity.verdict.warnings')
      : t('integrity.verdict.sound')
})

const integrityTone = computed(() => {
  const found = verdict.value

  return found === 'unsound' ? 'error' : found === 'warnings' ? 'warning' : 'success'
})
</script>

<template>
  <div v-if="canSample || canIntegrity || canAnalytics" class="flex flex-col gap-2">
    <p class="text-xs font-medium text-secondary">{{ t('models.operate') }}</p>
    <div class="grid grid-cols-[repeat(auto-fill,minmax(15rem,1fr))] gap-3">
      <HubActionCard
        v-if="canAnalytics"
        key="analytics"
        id="analytics"
        :title="t('models.analytics.title')"
        :hint="t('models.analytics.hint')"
        :open-label="t('hierarchy.open')"
        @open="emit('operate', 'analytics')"
      />
      <HubActionCard
        v-if="canSample"
        key="sample"
        id="sample"
        :title="t('models.sample.title')"
        :hint="t('models.sample.hint')"
        :open-label="t('hierarchy.open')"
        @open="emit('operate', 'sample')"
      />
      <HubActionCard
        v-if="canIntegrity"
        key="integrity"
        id="integrity"
        :title="t('models.integrity.title')"
        :hint="t('models.integrity.hint')"
        :pill="integrityPill"
        :tone="integrityTone"
        :open-label="t('hierarchy.open')"
        @open="emit('operate', 'integrity')"
      />
    </div>
  </div>
</template>

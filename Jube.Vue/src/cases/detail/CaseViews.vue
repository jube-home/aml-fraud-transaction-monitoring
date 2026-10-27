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
import { useRoute, useRouter } from 'vue-router'
import TemplateForm from '../../datasource/TemplateForm.vue'
import CaseDetailsSection from './CaseDetailsSection.vue'
import { operationsApi } from './operationsApi'
import type { CaseDetail } from './types'

const props = defineProps<{ detail: CaseDetail }>()

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const displays = useQuery({
  queryKey: computed(() => ['service', 'Case', 'displays', props.detail.caseWorkflowGuid] as const),
  queryFn: () => operationsApi.displays(props.detail.caseWorkflowGuid),
  staleTime: 60_000,
})

const choices = computed(() => displays.data.value ?? [])

const selected = computed(() => {
  const raw = Array.isArray(route.query.display) ? route.query.display[0] : route.query.display
  const id = Number(raw)

  return choices.value.some((choice) => choice.id === id) ? id : 0
})

const rendered = useQuery({
  queryKey: computed(
    () => ['service', 'Case', 'display', props.detail.id, selected.value] as const,
  ),
  queryFn: () => operationsApi.runDisplay(props.detail.id, selected.value),
  enabled: computed(() => selected.value !== 0),
  staleTime: 15_000,
  retry: false,
})

function choose(event: Event): void {
  const value = Number((event.target as HTMLSelectElement).value)

  void router.replace({
    query: { ...route.query, display: value === 0 ? undefined : String(value) },
  })
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="case-views">
    <label v-if="choices.length > 0" key="picker" class="flex w-fit flex-col gap-1">
      <span class="label text-xs">{{ t('caseDetail.views.label') }}</span>
      <select
        class="select select-sm"
        :value="selected"
        data-testid="case-view-select"
        @change="choose"
      >
        <option :value="0">{{ t('caseDetail.views.payload') }}</option>
        <option v-for="choice in choices" :key="choice.id" :value="choice.id">
          {{ choice.name }}
        </option>
      </select>
    </label>

    <CaseDetailsSection
      v-if="selected === 0"
      key="payload"
      :fields="detail.formattedPayload ?? []"
    />
    <template v-else>
      <div v-if="rendered.isPending.value" key="loading" class="skeleton h-40 w-full"></div>
      <div v-else-if="rendered.isError.value" key="failed" role="alert" class="alert alert-error">
        <span>{{ t('caseDetail.views.failed') }}</span>
      </div>
      <TemplateForm
        v-else
        key="display"
        :html="rendered.data.value ?? ''"
        :label="t('caseDetail.views.label')"
        data-testid="case-display"
      />
    </template>
  </div>
</template>

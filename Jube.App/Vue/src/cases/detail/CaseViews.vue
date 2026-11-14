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
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import {useQuery} from '@tanstack/vue-query'
import {useRoute, useRouter} from 'vue-router'
import TemplateForm from '../../datasource/TemplateForm.vue'
import CaseDetailsSection from './CaseDetailsSection.vue'
import {useFormat} from '../../composables/useFormat'
import {operationsApi} from './operationsApi'
import type {CaseDetail} from './types'

const props = defineProps<{ detail: CaseDetail }>()

const {t} = useI18n()
const {number} = useFormat()
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

function choose(id: number): void {
  void router.replace({
    query: {...route.query, display: id === 0 ? undefined : String(id)},
  })
}

const fieldCount = computed(() => props.detail.formattedPayload?.length ?? 0)

const tabs = computed(() => [
  {id: 0, name: t('caseDetail.views.payload')},
  ...choices.value.map((choice) => ({id: choice.id, name: choice.name ?? ''})),
])
</script>

<template>
  <div
    class="flex min-w-0 flex-col overflow-hidden rounded-box border border-base-300 bg-base-100"
    data-testid="case-views"
  >
    <div class="flex min-w-0 items-center justify-between gap-3 border-b border-base-300 bg-base-200/50 px-2">
      <div
        v-if="choices.length > 0"
        key="picker"
        role="tablist"
        class="tabs tabs-border tabs-sm min-w-0 flex-nowrap overflow-x-auto"
        :aria-label="t('caseDetail.views.label')"
        data-testid="case-view-tabs"
      >
        <button
          v-for="tab in tabs"
          :key="tab.id"
          type="button"
          role="tab"
          class="tab whitespace-nowrap"
          :class="selected === tab.id ? 'tab-active font-semibold' : ''"
          :aria-selected="selected === tab.id"
          :data-testid="`case-view-tab-${tab.id}`"
          @click="choose(tab.id)"
        >
          {{ tab.name }}
        </button>
      </div>
      <h4 v-else key="title" class="px-2 py-2 text-sm font-semibold">
        {{ t('caseDetail.details.title') }}
      </h4>
      <span
        v-if="selected === 0 && fieldCount > 0"
        key="count"
        class="badge badge-ghost badge-sm me-2 shrink-0 font-mono tabular-nums"
        data-testid="case-view-count"
      >
        {{ number(fieldCount) }}
      </span>
    </div>

    <div class="min-w-0 p-4">
      <CaseDetailsSection v-if="selected === 0" key="payload" :fields="detail.formattedPayload ?? []"/>
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
  </div>
</template>

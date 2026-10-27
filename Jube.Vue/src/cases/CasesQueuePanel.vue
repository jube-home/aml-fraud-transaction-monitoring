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
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/vue-query'
import { useRouter } from 'vue-router'
import { casesApi } from './api'
import CaseSearchEditor from './CaseSearchEditor.vue'
import CasePeek from './CasePeek.vue'
import CasesPulse from './CasesPulse.vue'
import CasesList from './CasesList.vue'
import CasesRail from './CasesRail.vue'
import { defaultView, requestFor, sourceKey, type CaseSource } from './pageState'
import { exportCap } from './exportRows'
import { useCasesExport } from './useCasesExport'
import { useCasesState } from './useCasesState'

const props = defineProps<{ workflow: string }>()

const { t } = useI18n()
const client = useQueryClient()
const router = useRouter()
const { state, go } = useCasesState()
const exporter = useCasesExport()

const editing = ref<{ guid: string | null } | null>(null)
const enabled = computed(() => props.workflow !== '')

const counts = useQuery({
  queryKey: computed(() => ['service', 'Case', 'counts', props.workflow] as const),
  queryFn: () => casesApi.counts(props.workflow),
  enabled,
  placeholderData: keepPreviousData,
})

const page = useQuery({
  queryKey: computed(
    () =>
      [
        'service',
        'Case',
        'page',
        props.workflow,
        sourceKey(state.value.source),
        state.value.sort,
      ] as const,
  ),
  queryFn: () => casesApi.page(requestFor(state.value)),
  enabled,
  placeholderData: keepPreviousData,
})

const analytics = useQuery({
  queryKey: computed(() => ['service', 'Case', 'analytics', props.workflow] as const),
  queryFn: () => casesApi.analytics(props.workflow),
  enabled,
  staleTime: 60_000,
  retry: false,
})

function chooseSource(source: CaseSource): void {
  go({ ...state.value, source, caseId: null })
}

function chooseSort(sort: number): void {
  go({ ...state.value, sort }, true)
}

const ownSource = computed(() => {
  const { source } = state.value

  return source.kind === 'own' &&
    counts.data.value?.some(
      (count) => count.kind === 'Own' && count.key === source.guid && count.mine,
    )
    ? source.guid
    : null
})

function startNew(): void {
  editing.value = { guid: null }
}

function startEdit(): void {
  if (ownSource.value) editing.value = { guid: ownSource.value }
}

function saved(guid: string): void {
  editing.value = null
  void client.invalidateQueries({ queryKey: ['service', 'Case'] })
  chooseSource({ kind: 'own', guid })
}

function deleted(): void {
  editing.value = null
  void client.invalidateQueries({ queryKey: ['service', 'Case'] })
  chooseSource({ kind: 'view', view: defaultView })
}

function peek(caseId: number): void {
  go({ ...state.value, caseId }, true)
}

function closePeek(): void {
  go({ ...state.value, caseId: null }, true)
}

function open(caseId: number): void {
  void router.push({ name: 'caseDetail', params: { caseId } })
}

function typing(target: EventTarget | null): boolean {
  return (
    target instanceof HTMLElement &&
    (['INPUT', 'SELECT', 'TEXTAREA'].includes(target.tagName) || target.isContentEditable)
  )
}

function keys(event: KeyboardEvent): void {
  if (event.ctrlKey || event.metaKey || event.altKey || typing(event.target)) return

  if (event.key === 'Escape' && state.value.caseId !== null) closePeek()
}

onMounted(() => window.addEventListener('keydown', keys))
onBeforeUnmount(() => window.removeEventListener('keydown', keys))
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4 lg:flex-row" data-testid="cases-queue-panel">
    <h2 class="sr-only">{{ t('cases.tab.queue') }}</h2>

    <CasesRail
      :counts="counts.data.value ?? []"
      :source="state.source"
      @source="chooseSource"
      @new-filter="startNew"
    />

    <div class="flex min-w-0 grow flex-col gap-3">
      <CasesPulse v-if="analytics.data.value" key="headline" :summary="analytics.data.value" />

      <div
        v-if="page.isPending.value"
        key="listloading"
        class="flex flex-col gap-2"
        role="status"
        data-testid="cases-list-loading"
      >
        <span class="sr-only">{{ t('cases.loading') }}</span>
        <div class="skeleton h-16 w-full"></div>
        <div class="skeleton h-16 w-full"></div>
        <div class="skeleton h-16 w-full"></div>
      </div>

      <div
        v-else-if="page.isError.value"
        key="listfailed"
        role="alert"
        class="alert alert-error"
        data-testid="cases-list-failed"
      >
        <span>{{ t('cases.list.failed') }}</span>
      </div>

      <CasesList
        v-else-if="page.data.value"
        key="list"
        :page="page.data.value"
        :sort="state.sort"
        :editable="ownSource !== null"
        @sort="chooseSort"
        @peek="peek"
        :exporting="exporter.busy.value"
        @open="open"
        @edit="startEdit"
        @export-csv="exporter.run('csv', state)"
        @export-excel="exporter.run('excel', state)"
      />

      <p
        v-if="exporter.capped.value"
        key="capped"
        class="text-xs text-base-content/70"
        role="status"
        data-testid="cases-export-capped"
      >
        {{ t('cases.export.capped', { n: exportCap }) }}
      </p>
      <p
        v-if="exporter.failed.value"
        key="exportfailed"
        class="text-xs"
        role="alert"
        data-testid="cases-export-failed"
      >
        {{ t('cases.export.failed') }}
      </p>
    </div>

    <CaseSearchEditor
      :open="editing !== null"
      :workflow="workflow"
      :search-guid="editing?.guid ?? null"
      @close="editing = null"
      @saved="saved"
      @deleted="deleted"
    />

    <CasePeek
      v-if="state.caseId !== null"
      key="peek"
      :case-id="state.caseId"
      @close="closePeek"
      @open="open"
    />
  </section>
</template>

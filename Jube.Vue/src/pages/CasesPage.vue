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
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import { useRouter } from 'vue-router'
import { FastForward } from 'lucide-vue-next'
import { hasPermission, meQueryOptions } from '../auth/me'
import { casesApi } from '../cases/api'
import CasesAnalyticsPanel from '../cases/CasesAnalyticsPanel.vue'
import CasesQueuePanel from '../cases/CasesQueuePanel.vue'
import CasesSupervisionPanel from '../cases/CasesSupervisionPanel.vue'
import { requestFor, tabs, type CasesTab } from '../cases/pageState'
import { loadLastUsed, saveLastUsed } from '../cases/lastUsed'
import { useCasesState } from '../cases/useCasesState'

const casesPermission = 1
const supervisionPermission = 18
const skimKey = 'W'

const { t } = useI18n()
const client = useQueryClient()
const me = useQuery(meQueryOptions)
const router = useRouter()
const { state, go } = useCasesState()

const allowed = computed(() => hasPermission(me.data.value, casesPermission))
const supervisor = computed(() => hasPermission(me.data.value, supervisionPermission))
const nothingToSkim = ref(false)

const workflows = useQuery({
  queryKey: ['service', 'Case', 'workflows'] as const,
  queryFn: casesApi.workflows,
  enabled: allowed,
  staleTime: 60_000,
})

const hasWorkflow = computed(() => state.value.workflow !== '')

const visibleTabs = computed(() => tabs.filter((tab) => tab !== 'supervision' || supervisor.value))

const activeTab = computed<CasesTab>(() =>
  visibleTabs.value.includes(state.value.tab) ? state.value.tab : 'queue',
)

const skim = useMutation({
  mutationFn: () => casesApi.skim(requestFor(state.value)),
  onSuccess: (result) => {
    void client.invalidateQueries({ queryKey: ['service', 'Case'] })

    if (result.case) {
      nothingToSkim.value = false
      void router.push({ name: 'caseDetail', params: { caseId: result.case.caseId } })
    } else {
      nothingToSkim.value = true
    }
  },
})

function chooseWorkflow(guid: string): void {
  nothingToSkim.value = false
  go({ ...state.value, workflow: guid, caseId: null })
}

function tabLabel(tab: CasesTab): string {
  if (tab === 'health') return t('cases.tab.health')
  if (tab === 'quality') return t('cases.tab.quality')
  if (tab === 'supervision') return t('cases.tab.supervision')

  return t('cases.tab.queue')
}

function chooseTab(tab: CasesTab): void {
  go({ ...state.value, tab }, true)
}

function moveTab(event: KeyboardEvent, index: number): void {
  const step = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0

  if (step === 0) return

  event.preventDefault()

  const list = visibleTabs.value
  const next = list[(index + step + list.length) % list.length]!

  chooseTab(next)
  void Promise.resolve().then(() =>
    document.querySelector<HTMLElement>(`[data-testid="cases-tab-${next}"]`)?.focus(),
  )
}

watch(
  () => [workflows.data.value, state.value.workflow] as const,
  ([choices, current]) => {
    if (!choices || choices.length === 0) return
    if (choices.some((choice) => choice.guid === current)) return

    const last = current === '' ? loadLastUsed() : null

    if (last && choices.some((choice) => choice.guid === last.workflow)) {
      go({ ...state.value, workflow: last.workflow, source: last.source, sort: last.sort }, true)

      return
    }

    go({ ...state.value, workflow: choices[0]!.guid }, true)
  },
  { immediate: true },
)

watch(
  () => state.value,
  (current) => {
    if (workflows.data.value?.some((choice) => choice.guid === current.workflow))
      saveLastUsed(current)
  },
  { immediate: true },
)

function typing(target: EventTarget | null): boolean {
  return (
    target instanceof HTMLElement &&
    (['INPUT', 'SELECT', 'TEXTAREA'].includes(target.tagName) || target.isContentEditable)
  )
}

function keys(event: KeyboardEvent): void {
  if (event.ctrlKey || event.metaKey || event.altKey || typing(event.target)) return

  if ((event.key === 'w' || event.key === 'W') && hasWorkflow.value && !skim.isPending.value)
    skim.mutate()
}

onMounted(() => window.addEventListener('keydown', keys))
onBeforeUnmount(() => window.removeEventListener('keydown', keys))
</script>

<template>
  <section
    class="flex min-h-[calc(100dvh-6rem)] min-w-0 flex-col gap-4 rounded-box border border-base-300 bg-base-100 p-4"
    data-testid="cases-page"
  >
    <h1 class="sr-only">{{ t('nav.cases') }}</h1>

    <div
      v-if="me.isPending.value"
      key="loading"
      class="flex flex-col gap-3"
      role="status"
      data-testid="cases-loading"
    >
      <span class="sr-only">{{ t('cases.loading') }}</span>
      <div class="skeleton h-24 w-full"></div>
      <div class="skeleton h-4 w-48"></div>
    </div>

    <div
      v-else-if="!allowed"
      key="refused"
      role="alert"
      class="alert alert-error"
      data-testid="cases-refused"
    >
      <span>{{ t('cases.refused') }}</span>
    </div>

    <div
      v-else-if="workflows.isSuccess.value && workflows.data.value?.length === 0"
      key="none"
      role="status"
      class="alert"
      data-testid="cases-no-workflows"
    >
      <span>{{ t('cases.noWorkflows') }}</span>
    </div>

    <template v-else>
      <div class="flex flex-wrap items-end justify-between gap-3" data-testid="cases-bar">
        <label class="flex min-w-0 flex-col gap-1">
          <span class="label text-xs">{{ t('cases.rail.workflow') }}</span>
          <select
            class="select select-sm min-w-56 max-w-full"
            :value="state.workflow"
            data-testid="cases-workflow"
            @change="chooseWorkflow(($event.target as HTMLSelectElement).value)"
          >
            <option
              v-for="choice in workflows.data.value ?? []"
              :key="choice.guid"
              :value="choice.guid"
            >
              {{ choice.name }}
            </option>
          </select>
        </label>

        <div class="flex flex-wrap items-center gap-3">
          <span
            v-if="nothingToSkim"
            key="nothing"
            class="text-sm"
            role="status"
            data-testid="cases-skim-empty"
          >
            {{ t('cases.skim.empty') }}
          </span>
          <span v-if="skim.isError.value" key="skimfailed" class="text-sm" role="alert">
            {{ t('cases.skim.failed') }}
          </span>
          <button
            type="button"
            class="btn btn-primary btn-sm"
            :disabled="skim.isPending.value || !hasWorkflow"
            data-testid="cases-skim"
            @click="skim.mutate()"
          >
            <FastForward class="size-4" aria-hidden="true" />
            {{ t('cases.skim.button') }}
            <kbd class="kbd kbd-sm text-base-content">{{ skimKey }}</kbd>
          </button>
        </div>
      </div>

      <div role="tablist" class="tabs tabs-border" :aria-label="t('cases.tab.label')">
        <button
          v-for="(tab, index) in visibleTabs"
          :id="`cases-tab-${tab}`"
          :key="tab"
          type="button"
          role="tab"
          class="tab"
          :class="{ 'tab-active': activeTab === tab }"
          :aria-selected="activeTab === tab"
          :aria-controls="`cases-panel-${tab}`"
          :tabindex="activeTab === tab ? 0 : -1"
          :data-testid="`cases-tab-${tab}`"
          @click="chooseTab(tab)"
          @keydown="moveTab($event, index)"
        >
          {{ tabLabel(tab) }}
        </button>
      </div>

      <div
        :id="`cases-panel-${activeTab}`"
        role="tabpanel"
        class="min-w-0"
        :aria-labelledby="`cases-tab-${activeTab}`"
      >
        <CasesQueuePanel v-if="activeTab === 'queue'" key="queue" :workflow="state.workflow" />
        <CasesAnalyticsPanel
          v-else-if="activeTab === 'health'"
          key="health"
          canvas="queue"
          :workflow="state.workflow"
        />
        <CasesAnalyticsPanel
          v-else-if="activeTab === 'quality'"
          key="quality"
          canvas="quality"
          :workflow="state.workflow"
        />
        <CasesSupervisionPanel
          v-else-if="activeTab === 'supervision'"
          key="supervision"
          :workflow="state.workflow"
        />
      </div>
    </template>
  </section>
</template>

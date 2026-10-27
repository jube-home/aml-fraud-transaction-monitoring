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
import { useRouter, RouterLink, useRoute } from 'vue-router'
import { ArrowLeft, FastForward, Lock, LockOpen } from 'lucide-vue-next'
import { hasPermission, meQueryOptions } from '../auth/me'
import { useCanvasSections } from '../canvas/useCanvasSections'
import CanvasIndex from '../components/CanvasIndex.vue'
import PaneToggles from '../components/PaneToggles.vue'
import { usePanes } from '../composables/usePanes'
import CanvasSection from '../components/CanvasSection.vue'
import { casesApi } from '../cases/api'
import CaseSummaryBody from '../cases/CaseSummaryBody.vue'
import { caseDetailApi } from '../cases/detail/api'
import CaseActivationsSection from '../cases/detail/CaseActivationsSection.vue'
import CaseKeyJournal from '../cases/journal/CaseKeyJournal.vue'
import CaseDecide from '../cases/detail/CaseDecide.vue'
import CaseViews from '../cases/detail/CaseViews.vue'
import CaseHistorySection from '../cases/detail/CaseHistorySection.vue'
import CaseLede from '../cases/detail/CaseLede.vue'
import CaseNotesSection from '../cases/detail/CaseNotesSection.vue'
import CaseDispatches from '../cases/detail/CaseDispatches.vue'
import CaseFormEntries from '../cases/detail/CaseFormEntries.vue'
import CasePlaceholder from '../cases/detail/CasePlaceholder.vue'
import CaseDashboard from '../cases/detail/CaseDashboard.vue'
import CaseSoon from '../cases/detail/CaseSoon.vue'
import { caseLabel, laneKeys } from '../cases/labels'
import { loadLastUsed } from '../cases/lastUsed'
import { defaultSort, defaultView, requestFor } from '../cases/pageState'
import { useFormat } from '../composables/useFormat'

const casesPermission = 1

const ids = [
  'dashboard',
  'summary',
  'activations',
  'journal',
  'network',
  'notes',
  'events',
  'forms',
  'messages',
] as const

const { t } = useI18n()
const panes = usePanes('case')

const columns = computed(() => {
  if (panes.left.value && panes.right.value)
    return 'lg:grid-cols-[15rem_minmax(0,1fr)] xl:grid-cols-[15rem_minmax(0,1fr)_20rem]'
  if (panes.left.value) return 'lg:grid-cols-[15rem_minmax(0,1fr)]'
  if (panes.right.value) return 'xl:grid-cols-[minmax(0,1fr)_20rem]'

  return ''
})
const { number, dateTime } = useFormat()
const route = useRoute()
const router = useRouter()
const me = useQuery(meQueryOptions)

const allowed = computed(() => hasPermission(me.data.value, casesPermission))
const caseId = computed(() => Number(route.params.caseId))
const valid = computed(() => Number.isInteger(caseId.value) && caseId.value > 0)

const detail = useQuery({
  queryKey: computed(() => ['service', 'Case', 'detail', caseId.value] as const),
  queryFn: () => caseDetailApi.byId(caseId.value),
  enabled: computed(() => allowed.value && valid.value),
  retry: false,
})

const summary = useQuery({
  queryKey: computed(() => ['service', 'Case', 'summary', caseId.value] as const),
  queryFn: () => casesApi.summary(caseId.value),
  enabled: computed(() => allowed.value && valid.value),
  staleTime: 15_000,
  retry: false,
})

const workflow = computed(() => detail.data.value?.caseWorkflowGuid ?? '')
const key = computed(() => detail.data.value?.caseKey ?? '')
const keyValue = computed(() => detail.data.value?.caseKeyValue ?? '')
const emptyGuid = '00000000-0000-0000-0000-000000000000'

const visualisationGuid = computed(() => {
  const detailed = detail.data.value

  return detailed?.enableVisualisation && detailed.visualisationRegistryGuid !== emptyGuid
    ? detailed.visualisationRegistryGuid
    : null
})

const hasKey = computed(() => key.value !== '' && keyValue.value !== '')

const statuses = useQuery({
  queryKey: computed(() => ['service', 'Case', 'statuses', workflow.value] as const),
  queryFn: () => caseDetailApi.statuses(workflow.value),
  enabled: computed(() => workflow.value !== ''),
  staleTime: 60_000,
})

const workflows = useQuery({
  queryKey: ['service', 'Case', 'workflows'] as const,
  queryFn: casesApi.workflows,
  enabled: allowed,
  staleTime: 60_000,
})

const history = useQuery({
  queryKey: computed(() => ['service', 'Case', 'history', key.value, keyValue.value] as const),
  queryFn: async () => ({
    cases: await caseDetailApi.byKey(key.value, keyValue.value),
    events: await caseDetailApi.events(key.value, keyValue.value),
  }),
  enabled: hasKey,
  staleTime: 30_000,
  retry: false,
})

const notes = useQuery({
  queryKey: computed(() => ['service', 'Case', 'notes', key.value, keyValue.value] as const),
  queryFn: () => caseDetailApi.notes(key.value, keyValue.value),
  enabled: hasKey,
  staleTime: 30_000,
  retry: false,
})

const statusName = computed(
  () =>
    statuses.data.value?.find((status) => status.guid === detail.data.value?.caseWorkflowStatusGuid)
      ?.name ?? '',
)

const workflowName = computed(
  () => workflows.data.value?.find((choice) => choice.guid === workflow.value)?.name ?? '',
)

const risk = computed(() => summary.data.value?.case.risk ?? null)
const closed = computed(() => (detail.data.value?.closedStatusId ?? 0) !== 0)
const nothingNext = ref(false)
const skimFailed = ref(false)

const activationNames = computed(() =>
  (detail.data.value?.activation ?? [])
    .map((item) => item.name ?? '')
    .filter((name) => name !== ''),
)

const { opened, pick } = useCanvasSections<(typeof ids)[number]>(
  ids,
  {
    dashboard: true,
    summary: true,
    activations: true,
    journal: true,
    network: true,
    notes: true,
    events: true,
    forms: false,
    messages: false,
  },
  ref(true),
)

function titleOf(id: (typeof ids)[number]): string {
  if (id === 'dashboard') return t('caseDetail.section.dashboard')
  if (id === 'activations') return t('caseDetail.section.activations')
  if (id === 'journal') return t('caseDetail.section.journal')
  if (id === 'network') return t('caseDetail.section.network')
  if (id === 'notes') return t('caseDetail.section.notes')
  if (id === 'events') return t('caseDetail.section.events')
  if (id === 'forms') return t('caseDetail.section.forms')
  if (id === 'messages') return t('caseDetail.section.messages')

  return t('caseDetail.section.summary')
}

function helpOf(id: (typeof ids)[number]): string {
  if (id === 'dashboard') return t('caseDetail.help.dashboard')
  if (id === 'activations') return t('caseDetail.help.activations')
  if (id === 'journal') return t('caseDetail.help.journal')
  if (id === 'network') return t('caseDetail.help.network')
  if (id === 'notes') return t('caseDetail.help.notes')
  if (id === 'events') return t('caseDetail.help.events')
  if (id === 'forms') return t('caseDetail.help.forms')
  if (id === 'messages') return t('caseDetail.help.messages')

  return t('caseDetail.help.summary')
}

function hintOf(id: (typeof ids)[number]): string {
  if (id === 'dashboard') return t('caseDetail.hint.dashboard')
  if (id === 'activations') return t('caseDetail.hint.activations')
  if (id === 'journal') return t('caseDetail.hint.journal')
  if (id === 'network') return t('caseDetail.hint.network')
  if (id === 'notes') return t('caseDetail.hint.notes')
  if (id === 'events') return t('caseDetail.hint.events')
  if (id === 'forms') return t('caseDetail.hint.forms')
  if (id === 'messages') return t('caseDetail.hint.messages')

  return t('caseDetail.hint.summary')
}

const items = computed(() =>
  ids
    .filter((id) => id !== 'dashboard' || visualisationGuid.value !== null)
    .map((id, index) => ({ id, label: `${index + 1}  ${titleOf(id)}` })),
)

function labelOf(id: (typeof ids)[number]): string {
  return items.value.find((item) => item.id === id)?.label ?? titleOf(id)
}

async function next(): Promise<void> {
  nothingNext.value = false
  skimFailed.value = false

  try {
    const last = loadLastUsed()
    const result = await casesApi.skim(
      requestFor({
        workflow: workflow.value,
        source: last?.source ?? { kind: 'view', view: defaultView },
        sort: last?.sort ?? defaultSort,
        caseId: null,
        tab: 'queue',
      }),
    )

    if (result.case)
      void router.push({ name: 'caseDetail', params: { caseId: result.case.caseId } })
    else nothingNext.value = true
  } catch {
    skimFailed.value = true
  }
}
</script>

<template>
  <section
    class="flex min-h-[calc(100dvh-6rem)] min-w-0 flex-col gap-4 rounded-box border border-base-300 bg-base-100 p-4"
    data-testid="case-page"
  >
    <div v-if="me.isPending.value" key="loading" class="flex flex-col gap-3" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
      <div class="skeleton h-16 w-full"></div>
      <div class="skeleton h-64 w-full"></div>
    </div>

    <div v-else-if="!allowed" key="refused" role="alert" class="alert alert-error">
      <span>{{ t('cases.refused') }}</span>
    </div>

    <div v-else-if="!valid || detail.isError.value" key="failed" class="flex flex-col gap-3">
      <div role="alert" class="alert alert-error" data-testid="case-failed">
        <span>{{ t('caseDetail.failed') }}</span>
      </div>
      <RouterLink :to="{ name: 'cases' }" class="btn btn-sm w-fit">
        <ArrowLeft class="size-4" aria-hidden="true" />
        {{ t('caseDetail.back') }}
      </RouterLink>
    </div>

    <div v-else-if="detail.isPending.value" key="pending" class="flex flex-col gap-3" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
      <div class="skeleton h-16 w-full"></div>
      <div class="skeleton h-64 w-full"></div>
    </div>

    <template v-else-if="detail.data.value">
      <header class="flex flex-wrap items-center justify-between gap-3" data-testid="case-header">
        <div class="flex min-w-0 flex-wrap items-center gap-3">
          <RouterLink :to="{ name: 'cases' }" class="btn btn-sm" data-testid="case-back">
            <ArrowLeft class="size-4" aria-hidden="true" />
            {{ t('caseDetail.back') }}
          </RouterLink>
          <h1 class="font-mono text-xl font-semibold tabular-nums" data-testid="case-title">
            {{ caseLabel(detail.data.value.id) }}
          </h1>
          <span
            v-if="statusName !== ''"
            class="badge badge-sm"
            :style="
              detail.data.value.backColor
                ? `background-color:${detail.data.value.backColor};color:${detail.data.value.foreColor ?? 'inherit'}`
                : undefined
            "
            data-testid="case-status"
          >
            {{ statusName }}
          </span>
          <span v-if="closed" class="badge badge-ghost badge-sm">{{
            t('caseDetail.header.closed')
          }}</span>
          <span
            v-if="risk && risk.assessed"
            class="badge badge-ghost badge-sm gap-2"
            data-testid="case-risk"
          >
            <span>{{ t(laneKeys[risk.lane] ?? 'cases.lane.standard') }}</span>
            <span class="font-mono tabular-nums">
              {{ t('caseDetail.header.priority', { n: number(risk.priority) }) }}
            </span>
          </span>
          <span class="badge badge-ghost badge-sm gap-1" data-testid="case-lock">
            <Lock v-if="detail.data.value.locked" class="size-3" aria-hidden="true" />
            <LockOpen v-else class="size-3" aria-hidden="true" />
            {{
              detail.data.value.locked
                ? t('caseDetail.header.heldBy', { user: detail.data.value.lockedUser ?? '' })
                : t('caseDetail.header.free')
            }}
          </span>
        </div>

        <div class="flex flex-wrap items-center gap-3">
          <span v-if="nothingNext" class="text-sm" role="status" data-testid="case-next-empty">
            {{ t('cases.skim.empty') }}
          </span>
          <span v-if="skimFailed" class="text-sm" role="alert">{{ t('cases.skim.failed') }}</span>
          <button
            type="button"
            class="btn btn-sm btn-primary"
            data-testid="case-next"
            @click="next()"
          >
            <FastForward class="size-4" aria-hidden="true" />
            {{ t('caseDetail.header.next') }}
          </button>
        </div>
      </header>

      <h2 class="sr-only">{{ t('caseDetail.story') }}</h2>

      <PaneToggles
        :left="panes.left.value"
        :right="panes.right.value"
        :left-name="t('caseDetail.panes.left')"
        :right-name="t('caseDetail.panes.right')"
        @toggle-left="panes.toggleLeft"
        @toggle-right="panes.toggleRight"
      />

      <div class="grid min-w-0 gap-4" :class="columns" data-testid="case-columns">
        <div
          v-show="panes.left.value"
          class="flex min-w-0 flex-col gap-3 lg:self-start"
          data-testid="case-left-pane"
        >
          <aside
            class="flex min-w-0 flex-col gap-3 rounded-box border border-base-300 p-4"
            :aria-label="t('caseDetail.panel.label')"
            data-testid="case-key-panel"
          >
            <dl class="flex flex-col gap-3 text-sm">
              <div class="flex flex-col gap-0.5">
                <dt class="text-xs text-base-content/70">{{ detail.data.value.caseKey }}</dt>
                <dd class="break-all font-mono font-semibold" data-testid="case-key-value">
                  {{ detail.data.value.caseKeyValue }}
                </dd>
              </div>
              <div v-if="workflowName !== ''" key="workflow" class="flex flex-col gap-0.5">
                <dt class="text-xs text-base-content/70">{{ t('caseDetail.panel.workflow') }}</dt>
                <dd>{{ workflowName }}</dd>
              </div>
              <div class="flex flex-col gap-0.5">
                <dt class="text-xs text-base-content/70">{{ t('caseDetail.panel.created') }}</dt>
                <dd class="font-mono">{{ dateTime(detail.data.value.createdDate) }}</dd>
              </div>
            </dl>
          </aside>

          <div class="hidden lg:block">
            <CanvasIndex :items="items" :label="t('caseDetail.section.label')" @pick="pick" />
          </div>
        </div>

        <div
          class="flex min-h-0 min-w-0 flex-col gap-3 overflow-y-auto pe-1 lg:h-[max(24rem,calc(100dvh-14rem))]"
          tabindex="0"
          role="region"
          :aria-label="t('caseDetail.story')"
          data-testid="case-story"
        >
          <CanvasSection
            v-if="visualisationGuid"
            id="dashboard"
            v-model:open="opened.dashboard"
            :title="labelOf('dashboard')"
            :hint="hintOf('dashboard')"
            :help="helpOf('dashboard')"
          >
            <CaseDashboard :guid="visualisationGuid ?? ''" :case-id="detail.data.value.id" />
          </CanvasSection>

          <CanvasSection
            id="summary"
            v-model:open="opened.summary"
            :title="labelOf('summary')"
            :hint="hintOf('summary')"
            :help="helpOf('summary')"
          >
            <div v-if="summary.isPending.value" key="loading" class="skeleton h-32 w-full"></div>
            <CaseLede
              v-else-if="summary.data.value"
              key="lede"
              :case-id="detail.data.value.id"
              :summary="summary.data.value"
            />
            <p v-else key="failed" class="text-sm" role="alert">{{ t('cases.peek.failed') }}</p>
            <CaseViews :detail="detail.data.value" />
            <CaseSoon area="summary" />
          </CanvasSection>

          <CanvasSection
            id="activations"
            v-model:open="opened.activations"
            :title="labelOf('activations')"
            :hint="hintOf('activations')"
            :help="helpOf('activations')"
          >
            <CaseActivationsSection :names="activationNames" />
            <CaseSoon area="activations" />
          </CanvasSection>

          <CanvasSection
            id="journal"
            v-model:open="opened.journal"
            :title="labelOf('journal')"
            :hint="hintOf('journal')"
            :help="helpOf('journal')"
          >
            <CaseKeyJournal
              v-if="hasKey"
              key="journal"
              :workflow="workflow"
              :case-key="key"
              :case-key-value="keyValue"
              :case-id="detail.data.value.id"
            />
            <p v-else key="nokey" class="text-sm text-base-content/70" role="status">
              {{ t('caseDetail.journal.noKey') }}
            </p>
          </CanvasSection>

          <CanvasSection
            id="network"
            v-model:open="opened.network"
            :title="labelOf('network')"
            :hint="hintOf('network')"
            :help="helpOf('network')"
          >
            <CasePlaceholder
              testid="case-network"
              :title="t('caseDetail.section.network')"
              :body="t('caseDetail.network.body')"
            />
            <CaseSoon area="network" />
          </CanvasSection>

          <CanvasSection
            id="notes"
            v-model:open="opened.notes"
            :title="labelOf('notes')"
            :hint="hintOf('notes')"
            :help="helpOf('notes')"
          >
            <CaseNotesSection
              :notes="notes.data.value ?? []"
              :loading="notes.isPending.value && hasKey"
              :failed="notes.isError.value"
            />
            <CaseSoon area="notes" />
          </CanvasSection>

          <CanvasSection
            id="events"
            v-model:open="opened.events"
            :title="labelOf('events')"
            :hint="hintOf('events')"
            :help="helpOf('events')"
          >
            <CaseHistorySection
              :cases="history.data.value?.cases ?? []"
              :events="history.data.value?.events ?? []"
              :loading="history.isPending.value && hasKey"
              :failed="history.isError.value"
            />
            <CaseSummaryBody
              v-if="summary.data.value"
              key="record"
              :case-id="detail.data.value.id"
              :data="summary.data.value"
              part="record"
            />
          </CanvasSection>

          <CanvasSection
            id="forms"
            v-model:open="opened.forms"
            :title="labelOf('forms')"
            :hint="hintOf('forms')"
            :help="helpOf('forms')"
          >
            <CaseFormEntries :case-id="detail.data.value.id" />
          </CanvasSection>

          <CanvasSection
            id="messages"
            v-model:open="opened.messages"
            :title="labelOf('messages')"
            :hint="hintOf('messages')"
            :help="helpOf('messages')"
          >
            <CaseDispatches :case-id="detail.data.value.id" />
          </CanvasSection>
        </div>

        <div
          v-show="panes.right.value"
          class="flex min-h-0 min-w-0 flex-col gap-3 overflow-y-auto pe-1 lg:col-span-full xl:col-span-1 xl:h-[max(24rem,calc(100dvh-14rem))]"
          data-testid="case-right-pane"
        >
          <CaseDecide
            :detail="detail.data.value"
            :row="summary.data.value?.case ?? null"
            :user-name="me.data.value?.userName ?? ''"
          />
        </div>
      </div>
    </template>
  </section>
</template>

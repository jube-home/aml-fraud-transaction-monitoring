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
import { useMutation, useQuery } from '@tanstack/vue-query'
import { Lock, MapPin, X } from 'lucide-vue-next'
import FieldHelp from '../../components/FieldHelp.vue'
import { useFormat } from '../../composables/useFormat'
import HoldLegs from '../detail/HoldLegs.vue'
import { stateClass, stateText } from '../detail/holdText'
import AdaptationPanel from './AdaptationPanel.vue'
import { journalApi } from './api'
import type { TransactionKey, TransactionPayload } from './types'
import type { Lane } from './state'
import type { RowPaint } from './paint'

const props = defineProps<{
  workflow: string
  id: number
  lanes: Lane[]
  canAddLane: boolean
  paint?: RowPaint | null
  canShowOnMap?: boolean
}>()

const emit = defineEmits<{
  close: []
  pivot: [key: string, value: string]
  merge: [key: string, value: string]
  open: [id: number]
  showOnMap: [guid: string]
}>()

const { t } = useI18n()
const { dateTime, fixed } = useFormat()

const card = useQuery({
  queryKey: computed(
    () => ['service', 'Case', 'keyjournal', 'transaction', props.workflow, props.id] as const,
  ),
  queryFn: () => journalApi.transaction(props.workflow, props.id),
  staleTime: 30_000,
  retry: false,
})

const aroundShown = ref(false)

const around = useQuery({
  queryKey: computed(
    () => ['service', 'Case', 'keyjournal', 'around', props.workflow, props.id] as const,
  ),
  queryFn: () => journalApi.around(props.workflow, props.id),
  enabled: aroundShown,
  staleTime: 30_000,
  retry: false,
})

const payload = ref<TransactionPayload | null>(null)

const loadPayload = useMutation({
  mutationFn: () => journalApi.payload(props.workflow, props.id),
  onSuccess: (result) => {
    payload.value = result
  },
})

const data = computed(() => card.data.value)

const groups = computed(() => {
  const grouped = new Map<string, { name: string; value: unknown }[]>()

  for (const value of data.value?.values ?? []) {
    grouped.set(value.group, [...(grouped.get(value.group) ?? []), value])
  }

  return [...grouped.entries()]
})

function groupText(group: string): string {
  if (group === 'abstraction') return t('journal.group.abstraction')
  if (group === 'calculation') return t('journal.group.calculation')
  if (group === 'counter') return t('journal.group.counter')
  if (group === 'dictionary') return t('journal.group.dictionary')
  if (group === 'sanction') return t('journal.group.sanction')

  return t('journal.group.http')
}

function show(value: unknown): string {
  if (typeof value === 'number') return fixed(value, Number.isInteger(value) ? 0 : 3)

  return value === null || value === undefined ? '' : String(value)
}

function pivotable(key: TransactionKey): boolean {
  return key.payloadKey !== null && key.value !== null && key.value !== ''
}

function inLane(key: string, value: string): boolean {
  return props.lanes.some((lane) => lane.key === key && lane.value === value)
}

function keys(event: KeyboardEvent): void {
  if (event.key === 'Escape') emit('close')
}

onMounted(() => window.addEventListener('keydown', keys))
onBeforeUnmount(() => window.removeEventListener('keydown', keys))
</script>

<template>
  <aside
    class="fixed inset-y-0 end-0 z-30 flex w-full max-w-xl flex-col gap-4 overflow-y-auto border-s border-base-300 bg-base-100 p-4 shadow-lg"
    :aria-label="t('journal.drawer.label')"
    data-testid="journal-drawer"
  >
    <div class="flex items-center justify-between gap-2">
      <h3 class="font-mono text-base font-semibold" data-testid="journal-drawer-title">
        {{ t('journal.drawer.title', { id }) }}
      </h3>
      <button
        type="button"
        class="btn btn-ghost btn-sm btn-square"
        :aria-label="t('journal.drawer.close')"
        data-testid="journal-drawer-close"
        @click="emit('close')"
      >
        <X class="size-4" aria-hidden="true" />
      </button>
    </div>

    <div v-if="card.isPending.value" key="loading" class="flex flex-col gap-2" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
      <div class="skeleton h-6 w-40"></div>
      <div class="skeleton h-32 w-full"></div>
    </div>

    <div v-else-if="card.isError.value" key="failed" role="alert" class="alert alert-error">
      <span>{{ t('journal.drawer.failed') }}</span>
    </div>

    <template v-else-if="data">
      <p class="flex flex-wrap gap-x-3 font-mono text-xs text-base-content/70">
        <span v-if="data.referenceDate">{{ dateTime(data.referenceDate) }}</span>
        <span>{{ t('journal.drawer.elevation', { n: fixed(data.responseElevation, 0) }) }}</span>
        <span v-if="data.responseElevationName" data-testid="journal-drawer-range">{{
          data.responseElevationName
        }}</span>
        <span v-if="data.modelName">{{ data.modelName }}</span>
        <span v-for="tag in data.tags" :key="tag" class="badge badge-soft badge-sm">{{ tag }}</span>
      </p>

      <button
        v-if="canShowOnMap"
        key="map"
        type="button"
        class="btn btn-sm self-start"
        data-testid="journal-drawer-map"
        @click="emit('showOnMap', data.entityAnalysisModelInstanceEntryGuid)"
      >
        <MapPin class="size-4" aria-hidden="true" />
        {{ t('journal.drawer.map') }}
      </button>

      <section
        v-if="paint"
        key="highlights"
        class="flex flex-col gap-2"
        data-testid="journal-drawer-highlights"
      >
        <h4 class="flex items-center gap-1 text-sm font-semibold">
          {{ t('journal.drawer.highlights') }}
          <FieldHelp
            :label="t('journal.drawer.highlights')"
            :text="t('journal.help.drawer.highlights')"
          />
        </h4>
        <ul class="flex flex-col gap-1">
          <li
            v-for="reason in paint.reasons"
            :key="reason"
            class="rounded-field border border-base-300 px-2 py-1 text-sm"
            :class="{ 'font-bold': paint.row.bold }"
            :style="{ color: paint.row.fore, backgroundColor: paint.row.back }"
          >
            {{ reason }}
          </li>
        </ul>
      </section>

      <section
        v-if="data.hold"
        key="hold"
        class="flex flex-col gap-2"
        data-testid="journal-drawer-hold"
      >
        <h4 class="flex items-center gap-2 text-sm font-semibold">
          {{ t('journal.drawer.hold') }}
          <span
            class="badge badge-sm"
            :class="stateClass(data.hold.stateId)"
            data-testid="journal-drawer-hold-state"
            >{{ stateText(t, data.hold.stateId) }}</span
          >
        </h4>
        <p v-if="data.hold.decision" class="text-sm">
          {{
            t('caseDetail.hold.decision', {
              state: stateText(t, data.hold.stateId),
              user: data.hold.decision.actor ?? '',
              when: dateTime(data.hold.decision.date),
            })
          }}
          <span class="block text-xs text-base-content/70">{{
            t('caseDetail.hold.decisionReason', { reason: data.hold.decision.reason ?? '' })
          }}</span>
        </p>
        <HoldLegs :legs="data.hold.legs" testid="journal-drawer-hold-legs" />
      </section>

      <section class="flex flex-col gap-2" data-testid="journal-drawer-activations">
        <h4 class="flex items-center gap-1 text-sm font-semibold">
          {{ t('journal.drawer.activations') }}
          <FieldHelp
            :label="t('journal.drawer.activations')"
            :text="t('journal.help.drawer.activations')"
          />
        </h4>
        <p v-if="data.activations.length === 0" key="none" class="text-sm text-base-content/70">
          {{ t('journal.drawer.noActivations') }}
        </p>
        <ul v-else key="list" class="flex flex-wrap gap-1">
          <li
            v-for="activation in data.activations"
            :key="activation.name"
            class="badge badge-sm"
            :class="activation.forced ? 'badge-warning' : 'badge-soft'"
          >
            {{ activation.name }}
            <span v-if="activation.forced" class="ms-1">{{ t('journal.rule.forced') }}</span>
          </li>
        </ul>
      </section>

      <section class="flex flex-col gap-2" data-testid="journal-drawer-models">
        <h4 class="flex items-center gap-1 text-sm font-semibold">
          {{ t('journal.drawer.models') }}
          <FieldHelp :label="t('journal.drawer.models')" :text="t('journal.help.drawer.models')" />
        </h4>
        <p v-if="data.adaptations.length === 0" key="none" class="text-sm text-base-content/70">
          {{ t('journal.drawer.noModels') }}
        </p>
        <AdaptationPanel
          v-for="adaptation in data.adaptations"
          :key="`${adaptation.source}-${adaptation.name}`"
          :adaptation="adaptation"
          :workflow="workflow"
          :id="id"
        />
      </section>

      <section
        v-if="groups.length > 0"
        key="values"
        class="flex flex-col gap-2"
        data-testid="journal-drawer-values"
      >
        <h4 class="flex items-center gap-1 text-sm font-semibold">
          {{ t('journal.drawer.values') }}
          <FieldHelp :label="t('journal.drawer.values')" :text="t('journal.help.drawer.values')" />
        </h4>
        <dl v-for="[group, entries] in groups" :key="group" class="flex flex-col gap-1">
          <dt class="text-xs text-base-content/70">{{ groupText(group) }}</dt>
          <dd
            v-for="entry in entries"
            :key="entry.name"
            class="flex justify-between gap-3 border-b border-base-300 py-0.5 text-sm"
          >
            <span>{{ entry.name }}</span>
            <span class="font-mono tabular-nums">{{ show(entry.value) }}</span>
          </dd>
        </dl>
      </section>

      <section
        v-if="data.keys.length > 0"
        key="keys"
        class="flex flex-col gap-2"
        data-testid="journal-drawer-keys"
      >
        <h4 class="flex items-center gap-1 text-sm font-semibold">
          {{ t('journal.drawer.keys') }}
          <FieldHelp :label="t('journal.drawer.keys')" :text="t('journal.help.drawer.keys')" />
        </h4>
        <ul class="flex flex-col gap-1">
          <li
            v-for="key in data.keys"
            :key="key.name"
            class="flex items-center justify-between gap-2 rounded-field px-1"
            :style="{
              color: paint?.cells[key.name]?.fore,
              backgroundColor: paint?.cells[key.name]?.back,
            }"
          >
            <span class="text-sm">
              {{ key.name }}
              <span class="font-mono">{{ key.value }}</span>
            </span>
            <button
              v-if="pivotable(key)"
              type="button"
              class="btn btn-sm"
              :data-testid="`journal-pivot-${key.name}`"
              @click="emit('pivot', key.payloadKey!, key.value!)"
            >
              {{ t('journal.drawer.pivot') }}
            </button>
            <button
              v-if="pivotable(key)"
              type="button"
              class="btn btn-sm"
              :disabled="inLane(key.payloadKey!, key.value!) || !canAddLane"
              :data-testid="`journal-merge-${key.name}`"
              @click="emit('merge', key.payloadKey!, key.value!)"
            >
              {{
                inLane(key.payloadKey!, key.value!)
                  ? t('journal.merge.added')
                  : t('journal.merge.add')
              }}
            </button>
          </li>
        </ul>
      </section>

      <section class="flex flex-col gap-2" data-testid="journal-drawer-around">
        <h4 class="flex items-center gap-1 text-sm font-semibold">
          {{ t('journal.around.title') }}
          <FieldHelp :label="t('journal.around.title')" :text="t('journal.help.drawer.around')" />
        </h4>
        <template v-if="!aroundShown">
          <p class="text-xs text-base-content/70">{{ t('journal.around.note') }}</p>
          <button
            type="button"
            class="btn btn-sm w-fit"
            data-testid="journal-around-load"
            @click="aroundShown = true"
          >
            {{ t('journal.around.show') }}
          </button>
        </template>
        <div
          v-else-if="around.isPending.value"
          key="loading"
          class="skeleton h-16 w-full"
          role="status"
        >
          <span class="sr-only">{{ t('cases.loading') }}</span>
        </div>
        <p v-else-if="around.isError.value" key="failed" role="alert" class="text-sm">
          {{ t('journal.around.failed') }}
        </p>
        <p
          v-else-if="around.data.value && around.data.value.groups.length === 0"
          key="none"
          class="text-sm text-base-content/70"
          data-testid="journal-around-none"
        >
          {{ t('journal.around.none') }}
        </p>
        <div v-else-if="around.data.value" key="groups" class="flex flex-col gap-3">
          <div
            v-for="group in around.data.value.groups"
            :key="`${group.key}-${group.value}`"
            class="flex flex-col gap-1"
            data-testid="journal-around-group"
          >
            <p class="text-sm font-semibold">
              {{ group.name }}
              <span class="font-mono font-normal">{{ group.value }}</span>
            </p>
            <p v-if="group.truncated" key="more" class="text-xs text-base-content/70">
              {{ t('journal.around.truncated', { n: group.rows.length }) }}
            </p>
            <ul class="flex flex-col gap-1">
              <li
                v-for="row in group.rows"
                :key="row.id"
                class="flex items-center justify-between gap-2 border-b border-base-300 py-0.5 text-sm"
              >
                <span class="flex flex-wrap items-center gap-x-3">
                  <span class="font-mono">{{
                    row.referenceDate ? dateTime(row.referenceDate) : ''
                  }}</span>
                  <span class="font-mono tabular-nums">
                    {{ t('journal.drawer.elevation', { n: fixed(row.responseElevation, 0) }) }}
                  </span>
                  <span v-if="row.activations">{{ row.activations }}</span>
                </span>
                <button
                  type="button"
                  class="btn btn-sm"
                  data-testid="journal-around-open"
                  @click="emit('open', row.id)"
                >
                  {{ t('journal.action.open') }}
                </button>
              </li>
            </ul>
          </div>
        </div>
      </section>

      <section class="flex flex-col gap-2" data-testid="journal-drawer-payload">
        <h4 class="flex items-center gap-1 text-sm font-semibold">
          {{ t('journal.drawer.payload') }}
          <FieldHelp
            :label="t('journal.drawer.payload')"
            :text="t('journal.help.drawer.payload')"
          />
        </h4>
        <template v-if="payload === null">
          <p class="flex items-center gap-1 text-xs text-base-content/70">
            <Lock class="size-3" aria-hidden="true" />
            {{ t('journal.drawer.payloadNote') }}
          </p>
          <button
            type="button"
            class="btn btn-sm w-fit"
            :disabled="loadPayload.isPending.value"
            data-testid="journal-payload-load"
            @click="loadPayload.mutate()"
          >
            {{ t('journal.drawer.showPayload') }}
          </button>
          <p v-if="loadPayload.isError.value" key="payloadfailed" class="text-sm" role="alert">
            {{ t('journal.drawer.payloadFailed') }}
          </p>
        </template>
        <dl v-else key="payload" class="flex flex-col gap-1" data-testid="journal-payload-fields">
          <div
            v-for="(value, name) in payload.fields"
            :key="name"
            class="flex justify-between gap-3 border-b border-base-300 py-0.5 text-sm"
          >
            <dt>{{ name }}</dt>
            <dd class="font-mono tabular-nums">{{ show(value) }}</dd>
          </div>
        </dl>
      </section>
    </template>
  </aside>
</template>

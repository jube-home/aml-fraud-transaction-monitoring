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
import {computed, ref} from 'vue'
import {useI18n} from 'vue-i18n'
import {Bookmark, Trash2} from 'lucide-vue-next'
import ConfirmDialog from '../components/ConfirmDialog.vue'
import {useFormat} from '../composables/useFormat'
import GraphPatternCaseDialog from './GraphPatternCaseDialog.vue'
import {hubOverrides, patternStateKey, patternStates} from './state'
import type {GraphCommunity, GraphExploration, GraphNode, GraphPattern} from './types'

withDefaults(
  defineProps<{
    explorations?: GraphExploration[]
    pending?: boolean
    failed?: boolean
    saving?: boolean
    canSave?: boolean
    patterns?: GraphPattern[]
    patternsPending?: boolean
    patternsFailed?: boolean
    patternState: number | null
    communities?: GraphCommunity[]
    communitiesPending?: boolean
    communitiesFailed?: boolean
    hubs?: GraphNode[]
    hubsPending?: boolean
    hubsFailed?: boolean
    canResolve?: boolean
    deciding?: boolean
    activePattern?: string | null
    activeCommunity?: string | null
  }>(),
  {
    pending: false,
    failed: false,
    saving: false,
    canSave: false,
    patternsPending: false,
    patternsFailed: false,
    communitiesPending: false,
    communitiesFailed: false,
    hubsPending: false,
    hubsFailed: false,
    canResolve: false,
    deciding: false,
    activePattern: null,
    activeCommunity: null,
  },
)

const emit = defineEmits<{
  save: [name: string]
  restore: [exploration: GraphExploration]
  remove: [id: number]
  'update:patternState': [stateId: number | null]
  openPattern: [pattern: GraphPattern]
  decide: [pattern: GraphPattern, stateId: number, caseId: number | null]
  openCommunity: [community: GraphCommunity]
  hubOverride: [node: GraphNode, overrideId: number]
}>()

const {t} = useI18n()
const {fixed, number} = useFormat()

const name = ref('')

const pendingDecision = ref<{ pattern: GraphPattern; stateId: number } | null>(null)
const pendingCase = ref<GraphPattern | null>(null)
const pendingHub = ref<{ node: GraphNode; overrideId: number } | null>(null)
const hubSelectVersion = ref(0)

const stateChoices = computed(() => [
  {id: null, label: t('graph.rail.state.all')},
  {id: patternStates.new, label: t('graph.pattern.state.new')},
  {id: patternStates.seen, label: t('graph.pattern.state.seen')},
  {id: patternStates.dismissed, label: t('graph.pattern.state.dismissed')},
  {id: patternStates.cased, label: t('graph.pattern.state.cased')},
])

const stateLabels = computed<Record<'new' | 'seen' | 'dismissed' | 'cased', string>>(() => ({
  new: t('graph.pattern.state.new'),
  seen: t('graph.pattern.state.seen'),
  dismissed: t('graph.pattern.state.dismissed'),
  cased: t('graph.pattern.state.cased'),
}))

const overrideChoices = computed(() => [
  {id: hubOverrides.none, label: t('graph.rail.hubOverride.none')},
  {id: hubOverrides.never, label: t('graph.rail.hubOverride.never')},
  {id: hubOverrides.always, label: t('graph.rail.hubOverride.always')},
])

function save(): void {
  const trimmed = name.value.trim()

  if (trimmed === '') return

  emit('save', trimmed)
  name.value = ''
}

function onState(event: Event): void {
  const value = (event.target as HTMLSelectElement).value

  emit('update:patternState', value === '' ? null : Number(value))
}

function ask(pattern: GraphPattern, stateId: number): void {
  if (stateId === patternStates.cased) pendingCase.value = pattern
  else pendingDecision.value = {pattern, stateId}
}

function confirmDecision(): void {
  const decision = pendingDecision.value

  pendingDecision.value = null
  if (decision) emit('decide', decision.pattern, decision.stateId, null)
}

function confirmCase(caseId: number): void {
  const pattern = pendingCase.value

  pendingCase.value = null
  if (pattern) emit('decide', pattern, patternStates.cased, caseId)
}

function confirmHub(): void {
  const change = pendingHub.value

  pendingHub.value = null
  hubSelectVersion.value += 1
  if (change) emit('hubOverride', change.node, change.overrideId)
}

function cancelHub(): void {
  pendingHub.value = null
  hubSelectVersion.value += 1
}

function patternName(pattern: GraphPattern | null): string {
  if (!pattern) return ''

  return `${pattern.kind ?? ''} ${pattern.anchor?.value ?? ''}`.trim()
}

function decisionBody(): string {
  const decision = pendingDecision.value

  if (!decision) return ''

  return decision.stateId === patternStates.dismissed
    ? t('graph.rail.decideAsk.dismiss', {name: patternName(decision.pattern)})
    : t('graph.rail.decideAsk.seen', {name: patternName(decision.pattern)})
}

const tab = ref<'explorations' | 'patterns' | 'communities' | 'hubs'>('patterns')
</script>

<template>
  <nav
    class="flex min-w-0 flex-col gap-3 text-sm"
    :aria-label="t('graph.rail.label')"
    data-testid="graph-rail"
  >
    <div class="tabs tabs-border tabs-sm" data-testid="graph-rail-tabs">
      <label class="tab">
        <input
          v-model="tab"
          type="radio"
          name="graph-rail-tabs"
          value="explorations"
          data-testid="graph-rail-tab-explorations"
        />
        {{ t('graph.rail.explorations') }}
      </label>
      <div class="tab-content pt-3">
        <section class="flex flex-col gap-2">
          <h2 class="sr-only">{{ t('graph.rail.explorations') }}</h2>

          <form class="join" @submit.prevent="save">
            <label class="input input-sm join-item min-w-0">
              <span class="sr-only">{{ t('graph.rail.name') }}</span>
              <input
                v-model="name"
                type="text"
                maxlength="80"
                :placeholder="t('graph.rail.name')"
                :disabled="!canSave || saving"
                data-testid="graph-exploration-name"
              />
            </label>
            <button
              type="submit"
              class="btn btn-sm join-item"
              :disabled="!canSave || saving || name.trim() === ''"
              data-testid="graph-exploration-save"
            >
              <Bookmark class="size-4" aria-hidden="true"/>
              {{ t('graph.rail.save') }}
            </button>
          </form>

          <div v-if="pending" key="loading" class="skeleton h-12 w-full" role="status"></div>
          <p v-else-if="failed" key="failed" role="alert" class="text-xs">
            {{ t('graph.rail.failed') }}
          </p>
          <p
            v-else-if="!explorations || explorations.length === 0"
            key="empty"
            class="text-xs text-secondary"
            data-testid="graph-explorations-empty"
          >
            {{ t('graph.rail.empty') }}
          </p>
          <ul v-else key="list" class="menu w-full p-0" data-testid="graph-explorations">
            <li v-for="exploration in explorations" :key="exploration.id">
              <div class="flex items-center gap-1 p-0">
                <button
                  type="button"
                  class="btn btn-ghost btn-sm min-w-0 flex-1 justify-start truncate"
                  :data-testid="`graph-exploration-${exploration.id}`"
                  @click="emit('restore', exploration)"
                >
                  <bdi>{{ exploration.name }}</bdi>
                </button>
                <button
                  type="button"
                  class="btn btn-ghost btn-sm btn-square"
                  :aria-label="t('graph.rail.delete', { name: exploration.name ?? '' })"
                  :data-testid="`graph-exploration-delete-${exploration.id}`"
                  @click="emit('remove', exploration.id)"
                >
                  <Trash2 class="size-4" aria-hidden="true"/>
                </button>
              </div>
            </li>
          </ul>
        </section>
      </div>

      <label class="tab">
        <input
          v-model="tab"
          type="radio"
          name="graph-rail-tabs"
          value="patterns"
          data-testid="graph-rail-tab-patterns"
        />
        {{ t('graph.rail.patterns') }}
        <span v-if="patterns" key="count" class="badge badge-ghost badge-xs font-mono tabular-nums">
          {{ number(patterns.length) }}
        </span>
      </label>
      <div class="tab-content pt-3">
        <section class="flex flex-col gap-2" data-testid="graph-rail-patterns">
          <div class="flex items-center gap-2">
            <h2 class="sr-only">{{ t('graph.rail.patterns') }}</h2>
            <label class="ms-auto flex items-center gap-1 text-xs">
              <span class="sr-only">{{ t('graph.rail.patternState') }}</span>
              <select
                class="select select-xs"
                :value="patternState === null ? '' : String(patternState)"
                data-testid="graph-pattern-state"
                @change="onState"
              >
                <option
                  v-for="choice in stateChoices"
                  :key="String(choice.id)"
                  :value="choice.id ?? ''"
                >
                  {{ choice.label }}
                </option>
              </select>
            </label>
          </div>

          <div v-if="patternsPending && !patterns" key="loading" class="skeleton h-12 w-full"></div>
          <p v-else-if="patternsFailed && !patterns" key="failed" role="alert" class="text-xs">
            {{ t('graph.rail.patternsFailed') }}
          </p>
          <p
            v-else-if="!patterns || patterns.length === 0"
            key="empty"
            class="text-xs text-secondary"
            data-testid="graph-patterns-empty"
          >
            {{ t('graph.rail.patternsEmpty') }}
          </p>
          <ul v-else key="list" class="flex flex-col gap-1" data-testid="graph-patterns">
            <li
              v-for="pattern in patterns"
              :key="pattern.id"
              class="flex flex-col gap-1 rounded-field border border-base-300 p-2"
              :class="{ 'bg-base-200': activePattern === String(pattern.id) }"
              :data-testid="`graph-pattern-${pattern.id}`"
            >
              <button
                type="button"
                class="flex flex-wrap items-center gap-1 text-start"
                @click="emit('openPattern', pattern)"
              >
                <span dir="auto" class="badge badge-soft badge-xs">{{ pattern.kind }}</span>
                <span class="badge badge-ghost badge-xs font-mono">
                  {{ stateLabels[patternStateKey(pattern.stateId)] }}
                </span>
                <span class="font-mono text-xs">{{ pattern.anchor?.value }}</span>
                <span class="badge badge-ghost badge-xs font-mono tabular-nums">
                  {{ t('graph.rail.score', {score: fixed(pattern.score)}) }}
                </span>
                <span class="badge badge-ghost badge-xs font-mono tabular-nums">
                  {{ t('graph.rail.members', {count: number(pattern.memberCount)}) }}
                </span>
              </button>
              <p v-if="pattern.sentence" key="sentence" dir="auto" class="text-xs text-secondary">
                {{ pattern.sentence }}
              </p>
              <div v-if="canResolve" key="decide" class="flex flex-wrap gap-1">
                <button
                  v-if="pattern.stateId === patternStates.new"
                  key="seen"
                  type="button"
                  class="btn btn-xs"
                  :disabled="deciding"
                  :data-testid="`graph-pattern-seen-${pattern.id}`"
                  @click="ask(pattern, patternStates.seen)"
                >
                  {{ t('graph.rail.markSeen') }}
                </button>
                <button
                  v-if="
                    pattern.stateId !== patternStates.dismissed &&
                    pattern.stateId !== patternStates.cased
                  "
                  key="dismiss"
                  type="button"
                  class="btn btn-xs"
                  :disabled="deciding"
                  :data-testid="`graph-pattern-dismiss-${pattern.id}`"
                  @click="ask(pattern, patternStates.dismissed)"
                >
                  {{ t('graph.rail.dismiss') }}
                </button>
                <button
                  v-if="pattern.stateId !== patternStates.cased"
                  key="case"
                  type="button"
                  class="btn btn-xs"
                  :disabled="deciding"
                  :data-testid="`graph-pattern-case-${pattern.id}`"
                  @click="ask(pattern, patternStates.cased)"
                >
                  {{ t('graph.rail.toCase') }}
                </button>
                <span
                  v-if="pattern.caseId !== null"
                  key="cased"
                  class="badge badge-ghost badge-xs font-mono"
                >
                  {{ t('graph.rail.casedAs', {id: pattern.caseId}) }}
                </span>
              </div>
            </li>
          </ul>
        </section>
      </div>

      <label class="tab">
        <input
          v-model="tab"
          type="radio"
          name="graph-rail-tabs"
          value="communities"
          data-testid="graph-rail-tab-communities"
        />
        {{ t('graph.rail.communities') }}
        <span
          v-if="communities"
          key="count"
          class="badge badge-ghost badge-xs font-mono tabular-nums"
        >
          {{ number(communities.length) }}
        </span>
      </label>
      <div class="tab-content pt-3">
        <section class="flex flex-col gap-2" data-testid="graph-rail-communities">
          <h2 class="sr-only">{{ t('graph.rail.communities') }}</h2>
          <div
            v-if="communitiesPending && !communities"
            key="loading"
            class="skeleton h-12 w-full"
          ></div>
          <p
            v-else-if="communitiesFailed && !communities"
            key="failed"
            role="alert"
            class="text-xs"
          >
            {{ t('graph.rail.communitiesFailed') }}
          </p>
          <p
            v-else-if="!communities || communities.length === 0"
            key="empty"
            class="text-xs text-secondary"
            data-testid="graph-communities-empty"
          >
            {{ t('graph.rail.communitiesEmpty') }}
          </p>
          <ul v-else key="list" class="menu w-full p-0" data-testid="graph-communities">
            <li v-for="community in communities" :key="community.id">
              <button
                type="button"
                class="flex flex-wrap items-center gap-1"
                :class="{ 'menu-active': activeCommunity === String(community.id) }"
                :data-testid="`graph-community-${community.id}`"
                @click="emit('openCommunity', community)"
              >
                <span class="badge badge-ghost badge-xs font-mono tabular-nums">{{
                    t('graph.rail.communityId', {id: community.id})
                  }}</span>
                <span dir="auto" class="font-mono text-xs">{{ community.label ?? '' }}</span>
                <span class="badge badge-ghost badge-xs font-mono tabular-nums">
                  {{ t('graph.rail.members', {count: number(community.size)}) }}
                </span>
                <span
                  v-if="community.tenantRateRatio !== null"
                  key="ratio"
                  class="badge badge-xs font-mono tabular-nums"
                  :class="community.tenantRateRatio >= 2 ? 'badge-error' : 'badge-ghost'"
                  :data-testid="`graph-community-ratio-${community.id}`"
                >
                  {{ t('graph.rail.ratio', {ratio: fixed(community.tenantRateRatio, 1)}) }}
                </span>
              </button>
            </li>
          </ul>
        </section>
      </div>

      <label class="tab">
        <input
          v-model="tab"
          type="radio"
          name="graph-rail-tabs"
          value="hubs"
          data-testid="graph-rail-tab-hubs"
        />
        {{ t('graph.rail.hubs') }}
        <span v-if="hubs" key="count" class="badge badge-ghost badge-xs font-mono tabular-nums">
          {{ number(hubs.length) }}
        </span>
      </label>
      <div class="tab-content pt-3">
        <section class="flex flex-col gap-2" data-testid="graph-rail-hubs">
          <h2 class="sr-only">{{ t('graph.rail.hubs') }}</h2>
          <div v-if="hubsPending && !hubs" key="loading" class="skeleton h-12 w-full"></div>
          <p v-else-if="hubsFailed && !hubs" key="failed" role="alert" class="text-xs">
            {{ t('graph.rail.hubsFailed') }}
          </p>
          <p
            v-else-if="!hubs || hubs.length === 0"
            key="empty"
            class="text-xs text-secondary"
            data-testid="graph-hubs-empty"
          >
            {{ t('graph.rail.hubsEmpty') }}
          </p>
          <ul v-else key="list" class="flex flex-col gap-1" data-testid="graph-hub-list">
            <li
              v-for="hub in hubs"
              :key="hub.id"
              class="flex flex-wrap items-center gap-1 rounded-field border border-base-300 p-2"
              :data-testid="`graph-hub-${hub.id}`"
            >
              <span class="badge badge-ghost badge-xs font-mono">{{ hub.key }}</span>
              <span class="font-mono text-xs">{{ hub.value }}</span>
              <span class="badge badge-ghost badge-xs font-mono tabular-nums">
                {{ t('graph.rail.degree', {count: number(hub.degree)}) }}
              </span>
              <label
                v-if="canResolve"
                key="override"
                class="ms-auto flex items-center gap-1 text-xs"
              >
                <span class="sr-only">{{ t('graph.rail.hubOverride.label') }}</span>
                <select
                  :key="hubSelectVersion"
                  class="select select-xs"
                  :value="String(hub.hubOverrideId)"
                  :disabled="deciding"
                  :data-testid="`graph-hub-override-${hub.id}`"
                  @change="
                    pendingHub = {
                      node: hub,
                      overrideId: Number(($event.target as HTMLSelectElement).value),
                    }
                  "
                >
                  <option
                    v-for="choice in overrideChoices"
                    :key="choice.id"
                    :value="String(choice.id)"
                  >
                    {{ choice.label }}
                  </option>
                </select>
              </label>
            </li>
          </ul>
        </section>
      </div>
    </div>

    <ConfirmDialog
      id="graph-pattern-decide"
      :open="pendingDecision !== null"
      :title="t('graph.rail.decideAsk.title')"
      :body="decisionBody()"
      :confirm-label="t('graph.rail.decideAsk.confirm')"
      :cancel-label="t('graph.rail.decideAsk.cancel')"
      @confirm="confirmDecision"
      @cancel="pendingDecision = null"
    />
    <GraphPatternCaseDialog
      :open="pendingCase !== null"
      :pattern-name="patternName(pendingCase)"
      @confirm="confirmCase"
      @cancel="pendingCase = null"
    />
    <ConfirmDialog
      id="graph-hub-override"
      :open="pendingHub !== null"
      :title="t('graph.rail.hubAsk.title')"
      :body="t('graph.rail.hubAsk.body', { value: pendingHub?.node.value ?? '' })"
      :confirm-label="t('graph.rail.hubAsk.confirm')"
      :cancel-label="t('graph.rail.hubAsk.cancel')"
      @confirm="confirmHub"
      @cancel="cancelHub"
    />
  </nav>
</template>

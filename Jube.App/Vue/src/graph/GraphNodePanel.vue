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
import {
  Banknote,
  Camera,
  Expand,
  ExternalLink,
  Link2,
  Route,
  Rows3,
  Star,
  Tag,
  Users,
} from 'lucide-vue-next'
import ConfirmDialog from '../components/ConfirmDialog.vue'
import {useFormat} from '../composables/useFormat'
import {caseIdOf, isCaseNode, patternStateKey, tagChips} from './state'
import type {GraphCombo, GraphNode, GraphNodeFacts, GraphPattern} from './types'

const props = withDefaults(
  defineProps<{
    nodeId: string | null
    node: GraphNode | null
    combo: GraphCombo | null
    facts?: GraphNodeFacts
    pending?: boolean
    failed?: boolean
    expanded?: boolean
    adds?: number
    canCapture?: boolean
    canLink?: boolean
    pathsPicking?: boolean
    flowing?: boolean
  }>(),
  {
    pending: false,
    failed: false,
    expanded: false,
    adds: 0,
    canCapture: false,
    canLink: false,
    pathsPicking: false,
    flowing: false,
  },
)

const emit = defineEmits<{
  expand: [nodeId: string]
  pivot: [target: { key: string; value: string }]
  link: [node: GraphNode]
  openCase: [caseId: number]
  capture: []
  explodeCombo: [combo: GraphCombo]
  paths: []
  follow: [nodeId: string]
  pattern: [pattern: GraphPattern]
  community: [communityId: number]
}>()

const {t} = useI18n()
const {number, dateTime, fixed} = useFormat()

const asking = ref(false)

const shown = computed(() => props.facts?.node ?? props.node)

const caseNode = computed(() => isCaseNode(shown.value))

const openCaseId = computed(
  () =>
    caseIdOf(shown.value) ??
    shown.value?.cases.latestOpenCaseId ??
    shown.value?.cases.latestCaseId ??
    null,
)

const tags = computed(() => tagChips(shown.value))

function tagChip(tag: string, distance: number): string {
  return distance === 0
    ? t('graph.panel.tagHere', {tag})
    : t('graph.panel.tagHops', {tag, count: distance}, distance)
}

const stateLabels = computed<Record<'new' | 'seen' | 'dismissed' | 'cased', string>>(() => ({
  new: t('graph.pattern.state.new'),
  seen: t('graph.pattern.state.seen'),
  dismissed: t('graph.pattern.state.dismissed'),
  cased: t('graph.pattern.state.cased'),
}))

function expand(): void {
  if (!props.nodeId) return

  if (shown.value?.hub) {
    asking.value = true

    return
  }

  emit('expand', props.nodeId)
}

function confirmHub(): void {
  asking.value = false
  if (props.nodeId) emit('expand', props.nodeId)
}

function pivot(): void {
  if (shown.value?.key && shown.value.value)
    emit('pivot', {key: shown.value.key, value: shown.value.value})
}
</script>

<template>
  <aside
    class="flex min-w-0 flex-col gap-3 rounded-box border border-base-300 bg-base-100 p-3 text-sm"
    :aria-label="t('graph.panel.title')"
    data-testid="graph-node-panel"
  >
    <h3 class="text-xs font-semibold text-secondary">{{ t('graph.panel.title') }}</h3>

    <div v-if="combo" key="combo" class="flex flex-col gap-2" data-testid="graph-combo-panel">
      <p>{{ t('graph.panel.combo', {count: combo.count, key: combo.key ?? ''}) }}</p>
      <button
        type="button"
        class="btn btn-sm w-fit"
        data-testid="graph-combo-explode"
        @click="emit('explodeCombo', combo)"
      >
        <Rows3 class="size-4" aria-hidden="true"/>
        {{ t('graph.panel.comboExplode') }}
      </button>
    </div>

    <p v-else-if="nodeId === null" key="pick" class="text-secondary" data-testid="graph-panel-pick">
      {{ t('graph.panel.pick') }}
    </p>

    <div
      v-else-if="pending && !shown"
      key="loading"
      class="flex flex-col gap-2"
      role="status"
      data-testid="graph-panel-loading"
    >
      <span class="sr-only">{{ t('graph.panel.loading') }}</span>
      <div class="skeleton h-5 w-2/3"></div>
      <div class="skeleton h-4 w-full"></div>
      <div class="skeleton h-4 w-5/6"></div>
    </div>

    <p v-else-if="failed && !shown" key="failed" role="alert" data-testid="graph-panel-failed">
      {{ t('graph.panel.failed') }}
    </p>

    <template v-else-if="shown">
      <div class="flex flex-wrap items-center gap-2">
        <span
          v-if="caseNode"
          key="case-head"
          class="font-semibold"
          data-testid="graph-panel-value"
        >{{ t('graph.panel.caseNode', {id: shown.value ?? ''}) }}</span
        >
        <template v-else>
          <span dir="auto" class="badge badge-ghost badge-sm font-mono">{{ shown.key }}</span>
          <span
            dir="ltr"
            class="break-all font-mono font-semibold"
            data-testid="graph-panel-value"
          >{{ shown.value }}</span
          >
        </template>
        <span v-if="props.node?.seed || shown.seed" key="seed" class="badge badge-soft badge-sm">{{
            t('graph.panel.seed')
          }}</span>
        <span v-if="shown.hub" key="hub" class="badge badge-soft badge-sm">{{
            t('graph.panel.hub')
          }}</span>
        <span
          v-if="shown.cases.confirmed > 0"
          key="confirmed"
          class="badge badge-error badge-sm gap-1"
          data-testid="graph-panel-confirmed"
        >
          <Star class="size-3" aria-hidden="true"/>
          {{ t('graph.panel.confirmed', {count: shown.cases.confirmed}) }}
        </span>
      </div>

      <dl class="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1 text-xs">
        <dt class="text-secondary">{{ t('graph.panel.degree') }}</dt>
        <dd class="font-mono tabular-nums">{{ number(shown.degree) }}</dd>
        <dt class="text-secondary">{{ t('graph.panel.openCases') }}</dt>
        <dd class="font-mono tabular-nums">{{ number(shown.cases.open) }}</dd>
        <dt class="text-secondary">{{ t('graph.panel.closedCases') }}</dt>
        <dd class="font-mono tabular-nums">{{ number(shown.cases.closed) }}</dd>
        <template v-if="shown.interest !== null">
          <dt class="text-secondary">{{ t('graph.panel.interest') }}</dt>
          <dd class="font-mono tabular-nums" data-testid="graph-panel-interest">
            {{ fixed(shown.interest) }}
          </dd>
        </template>
        <template v-if="shown.communityId !== null">
          <dt class="text-secondary">{{ t('graph.panel.community') }}</dt>
          <dd class="font-mono tabular-nums">{{ shown.communityId }}</dd>
        </template>
        <template v-if="shown.distanceToConfirmed !== null">
          <dt class="text-secondary">{{ t('graph.panel.distance') }}</dt>
          <dd class="font-mono tabular-nums">{{ shown.distanceToConfirmed }}</dd>
        </template>
        <template v-if="tags.length > 0">
          <dt class="text-secondary">{{ t('graph.panel.tags') }}</dt>
          <dd class="flex flex-wrap gap-1" data-testid="graph-panel-tags">
            <span
              v-for="item in tags"
              :key="item.tag ?? ''"
              class="badge badge-soft badge-sm gap-1"
              :data-testid="`graph-panel-tag-${item.tag}`"
            >
              <Tag class="size-3" aria-hidden="true"/>
              {{ tagChip(item.tag ?? '', item.distance) }}
            </span>
          </dd>
        </template>
        <dt class="text-secondary">{{ t('graph.panel.firstSeen') }}</dt>
        <dd class="font-mono">{{ dateTime(shown.firstSeen) }}</dd>
        <dt class="text-secondary">{{ t('graph.panel.lastSeen') }}</dt>
        <dd class="font-mono">{{ dateTime(shown.lastSeen) }}</dd>
      </dl>

      <button
        v-if="facts?.community"
        key="community"
        type="button"
        class="btn btn-ghost btn-sm w-fit justify-start gap-2"
        data-testid="graph-panel-community"
        @click="emit('community', facts.community.id)"
      >
        <Users class="size-4" aria-hidden="true"/>
        <span dir="auto">{{ facts.community.label ?? t('graph.panel.community') }}</span>
        <span
          v-if="facts.community.tenantRateRatio !== null"
          key="ratio"
          class="badge badge-ghost badge-xs font-mono tabular-nums"
        >
          {{
            t('graph.panel.communityRatio', {ratio: fixed(facts.community.tenantRateRatio, 1)})
          }}
        </span>
      </button>

      <div
        v-if="facts && facts.patterns.length > 0"
        key="patterns"
        class="flex flex-wrap gap-1"
        data-testid="graph-panel-patterns"
      >
        <button
          v-for="pattern in facts.patterns"
          :key="pattern.id"
          type="button"
          class="badge badge-soft badge-sm gap-1"
          :data-testid="`graph-panel-pattern-${pattern.id}`"
          @click="emit('pattern', pattern)"
        >
          <span dir="auto">{{ pattern.kind }}</span>
          <span class="font-mono">{{ stateLabels[patternStateKey(pattern.stateId)] }}</span>
        </button>
      </div>

      <ul
        v-if="facts && facts.definitions.length > 0"
        key="definitions"
        class="flex flex-col gap-1 text-xs"
        data-testid="graph-panel-definitions"
      >
        <li
          v-for="definition in facts.definitions"
          :key="definition.guid"
          class="flex items-center justify-between gap-2"
        >
          <span dir="auto">{{ definition.name }}</span>
          <span class="badge badge-ghost badge-xs font-mono tabular-nums">
            {{ t('graph.panel.counterparts', {count: definition.counterparts}) }}
          </span>
        </li>
      </ul>

      <div v-if="facts && facts.sentences.length > 0" key="why" data-testid="graph-panel-why">
        <h4 class="text-xs font-semibold text-secondary">{{ t('graph.panel.why') }}</h4>
        <ul class="ps-4 text-xs [list-style:disc]">
          <li v-for="(sentence, index) in facts.sentences" :key="index" dir="auto">
            {{ sentence }}
          </li>
        </ul>
      </div>

      <div class="flex flex-wrap gap-2">
        <button
          type="button"
          class="btn btn-sm"
          :disabled="expanded || adds === 0"
          data-testid="graph-panel-expand"
          @click="expand"
        >
          <Expand class="size-4" aria-hidden="true"/>
          {{
            expanded ? t('graph.panel.expanded') : t('graph.panel.expand', {count: adds}, adds)
          }}
        </button>
        <button
          type="button"
          class="btn btn-sm"
          :class="{ 'btn-active': pathsPicking }"
          :aria-pressed="pathsPicking"
          data-testid="graph-panel-paths"
          @click="emit('paths')"
        >
          <Route class="size-4" aria-hidden="true"/>
          {{ pathsPicking ? t('graph.panel.pathsPicking') : t('graph.panel.paths') }}
        </button>
        <button
          type="button"
          class="btn btn-sm"
          :class="{ 'btn-active': flowing }"
          :aria-pressed="flowing"
          data-testid="graph-panel-follow"
          @click="emit('follow', nodeId!)"
        >
          <Banknote class="size-4" aria-hidden="true"/>
          {{ t('graph.panel.follow') }}
        </button>
        <button
          v-if="!caseNode"
          key="pivot"
          type="button"
          class="btn btn-sm"
          data-testid="graph-panel-pivot"
          @click="pivot"
        >
          <Rows3 class="size-4" aria-hidden="true"/>
          {{ t('graph.panel.pivot') }}
        </button>
        <button
          v-if="canLink"
          key="link"
          type="button"
          class="btn btn-sm"
          data-testid="graph-panel-link"
          @click="emit('link', shown)"
        >
          <Link2 class="size-4" aria-hidden="true"/>
          {{ t('graph.panel.link') }}
        </button>
        <button
          v-if="openCaseId !== null"
          key="case"
          type="button"
          class="btn btn-sm"
          data-testid="graph-panel-open-case"
          @click="emit('openCase', openCaseId)"
        >
          <ExternalLink class="size-4" aria-hidden="true"/>
          {{ t('graph.panel.openCase', {id: openCaseId}) }}
        </button>
        <button
          v-if="canCapture"
          key="capture"
          type="button"
          class="btn btn-sm"
          data-testid="graph-panel-capture"
          @click="emit('capture')"
        >
          <Camera class="size-4" aria-hidden="true"/>
          {{ t('graph.panel.capture') }}
        </button>
      </div>
    </template>

    <ConfirmDialog
      id="graph-hub-expand"
      :open="asking"
      :title="t('graph.panel.hubAsk.title')"
      :body="t('graph.panel.hubAsk.body', { count: shown?.degree ?? 0 })"
      :confirm-label="t('graph.panel.hubAsk.confirm')"
      :cancel-label="t('graph.panel.hubAsk.cancel')"
      @confirm="confirmHub"
      @cancel="asking = false"
    />
  </aside>
</template>

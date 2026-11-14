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
import {Lock, Plus, X} from 'lucide-vue-next'
import FieldHelp from '../../components/FieldHelp.vue'
import {useFormat} from '../../composables/useFormat'
import type {BuilderGroup, BuilderRule} from '../../ruleBuilder/types'
import {describeFilter, isGroup, ruleCount} from './filter'
import JournalFilterBuilder from './JournalFilterBuilder.vue'
import {
  isLaneOperator,
  isRangeType,
  laneClass,
  laneLetters,
  laneValueText,
  sameLane,
  type LaneOperator,
} from './lanes'
import {maximumLanes, type JournalState, type Lane} from './state'

const props = defineProps<{
  state: JournalState
  workflow: string
  primary: { key: string; value: string }
  keys: { key: string; title: string; dataTypeId?: number }[]
  shown: number
  total: number
}>()

const emit = defineEmits<{ change: [state: JournalState] }>()

const {t} = useI18n()
const {number} = useFormat()

const laneKey = ref('')
const choices = computed(() => props.keys.filter((choice) => choice.key !== props.primary.key))
const laneValue = ref('')
const laneTo = ref('')
const laneOperator = ref<LaneOperator>('eq')
const rangeKey = computed(() =>
  isRangeType(props.keys.find((choice) => choice.key === laneKey.value)?.dataTypeId),
)
const operatorChoices: { value: LaneOperator; label: string }[] = [
  {value: 'eq', label: 'journal.merge.op.eq'},
  {value: 'ge', label: 'journal.merge.op.ge'},
  {value: 'gt', label: 'journal.merge.op.gt'},
  {value: 'le', label: 'journal.merge.op.le'},
  {value: 'lt', label: 'journal.merge.op.lt'},
  {value: 'between', label: 'journal.merge.op.between'},
]

const lanes = computed<Lane[]>(() => [props.primary, ...props.state.merge])

const chips = computed(() =>
  (props.state.filter?.rules ?? []).map((node) => ({
    text: isGroup(node) ? `(${describeFilter(node, operatorName, words())})` : ruleText(node),
  })),
)

const joiner = computed(() =>
  props.state.filter?.condition === 'OR' ? t('ruleBuilder.or') : t('ruleBuilder.and'),
)

const active = computed(() => ruleCount(props.state.filter) > 0 || props.state.merge.length > 0)

function words(): { and: string; or: string } {
  return {and: t('ruleBuilder.and'), or: t('ruleBuilder.or')}
}

function operatorName(operator: string): string {
  const names: Record<string, string> = {
    equal: 'equal',
    not_equal: 'notEqual',
    less: 'less',
    less_or_equal: 'lessOrEqual',
    greater: 'greater',
    greater_or_equal: 'greaterOrEqual',
    like: 'like',
    not_like: 'notLike',
  }

  return names[operator] ? t(`filterBuilder.operator.${names[operator]}`) : operator
}

function ruleText(rule: BuilderRule): string {
  return `${rule.id} ${operatorName(rule.operator)}${rule.value === undefined ? '' : ` "${String(rule.value)}"`}`
}

function update(patch: Partial<JournalState>): void {
  emit('change', {...props.state, transaction: null, ...patch})
}

function dropLane(index: number): void {
  update({merge: props.state.merge.filter((_, position) => position !== index)})
}

function addLane(): void {
  const key = laneKey.value.trim()
  const value = laneValue.value.trim()
  const to = laneTo.value.trim()
  const op: LaneOperator =
    rangeKey.value && isLaneOperator(laneOperator.value) ? laneOperator.value : 'eq'

  if (key === '' || value === '' || props.state.merge.length >= maximumLanes) return
  if (op === 'between' && to === '') return

  const lane: Lane =
    op === 'eq' ? {key, value} : op === 'between' ? {key, value, op, to} : {key, value, op}
  const exists = lanes.value.some((existing) => sameLane(existing, lane))

  if (!exists) update({merge: [...props.state.merge, lane]})

  laneKey.value = ''
  laneValue.value = ''
  laneTo.value = ''
  laneOperator.value = 'eq'
  document.getElementById('journal-add-key')?.hidePopover?.()
}

function dropChip(index: number): void {
  const filter = props.state.filter

  if (!filter) return

  const rules = filter.rules.filter((_, position) => position !== index)
  const next: BuilderGroup | null = rules.length > 0 ? {...filter, rules} : null

  update({filter: next})
}

function clearAll(): void {
  update({filter: null, merge: []})
}
</script>

<template>
  <div
    class="grid min-w-0 grid-cols-[3.5rem_minmax(0,1fr)] items-start gap-x-3 gap-y-2"
    role="group"
    :aria-label="t('journal.scope.label')"
    data-testid="journal-scope"
  >
    <div class="flex items-center gap-1 pt-1">
      <span class="text-xs font-semibold">{{ t('journal.scope.keys') }}</span>
      <FieldHelp :label="t('journal.scope.label')" :text="t('journal.help.scope')"/>
    </div>

    <div class="flex min-w-0 flex-wrap items-center gap-2">
      <ul
        class="flex flex-wrap items-center gap-2"
        :aria-label="t('journal.merge.title')"
        data-testid="journal-lanes"
      >
        <li
          v-for="(lane, index) in lanes"
          :key="`${lane.key}-${lane.op ?? 'eq'}-${lane.value}-${lane.to ?? ''}`"
          class="inline-flex h-8 items-center gap-2 rounded-field border px-2 text-sm"
          :class="laneClass(laneLetters[index] ?? 'A')"
          data-testid="journal-lane"
        >
          <span class="font-mono font-semibold">{{ laneLetters[index] }}</span>
          <span>{{ lane.key }}</span>
          <span class="font-mono">{{ laneValueText(lane) }}</span>
          <Lock
            v-if="index === 0"
            key="locked"
            class="size-3"
            :aria-label="t('journal.scope.locked')"
            role="img"
          />
          <button
            v-else
            key="remove"
            type="button"
            class="btn btn-ghost btn-xs btn-circle"
            :aria-label="t('journal.merge.remove', { key: lane.key, value: laneValueText(lane) })"
            data-testid="journal-lane-remove"
            @click="dropLane(index - 1)"
          >
            <X class="size-3" aria-hidden="true"/>
          </button>
        </li>
      </ul>

      <template v-if="state.merge.length < maximumLanes">
        <button
          type="button"
          class="btn btn-ghost btn-sm"
          popovertarget="journal-add-key"
          style="anchor-name: --journal-add-key"
          data-testid="journal-lane-open"
        >
          <Plus class="size-4" aria-hidden="true"/>
          {{ t('journal.scope.addKey') }}
        </button>
        <form
          id="journal-add-key"
          class="dropdown card card-sm z-1 w-72 bg-base-100 shadow-md"
          popover
          style="position-anchor: --journal-add-key"
          @submit.prevent="addLane"
        >
          <div class="card-body gap-2">
            <label class="flex flex-col gap-1">
              <span class="text-xs">{{ t('journal.scope.key') }}</span>
              <select
                v-model="laneKey"
                class="select select-sm w-full"
                data-testid="journal-lane-key"
              >
                <option value="" disabled>{{ t('journal.scope.chooseKey') }}</option>
                <option v-for="choice in choices" :key="choice.key" :value="choice.key">
                  {{ choice.title }}
                </option>
              </select>
              <span
                v-if="choices.length === 0"
                key="nokeys"
                class="text-xs"
                data-testid="journal-lane-nokeys"
              >
                {{ t('journal.scope.noKeys') }}
              </span>
            </label>
            <label v-if="rangeKey" class="flex flex-col gap-1">
              <span class="text-xs">{{ t('journal.scope.operator') }}</span>
              <select
                v-model="laneOperator"
                class="select select-sm w-full"
                data-testid="journal-lane-operator"
              >
                <option v-for="choice in operatorChoices" :key="choice.value" :value="choice.value">
                  {{ t(choice.label) }}
                </option>
              </select>
            </label>
            <label class="flex flex-col gap-1">
              <span class="text-xs">{{ t('journal.scope.value') }}</span>
              <input
                v-model="laneValue"
                type="text"
                class="input input-sm w-full"
                data-testid="journal-lane-value"
              />
            </label>
            <label v-if="rangeKey && laneOperator === 'between'" class="flex flex-col gap-1">
              <span class="text-xs">{{ t('journal.scope.to') }}</span>
              <input
                v-model="laneTo"
                type="text"
                class="input input-sm w-full"
                data-testid="journal-lane-to"
              />
            </label>
            <button
              type="submit"
              class="btn btn-sm"
              :disabled="
                laneKey === '' ||
                laneValue.trim() === '' ||
                (rangeKey && laneOperator === 'between' && laneTo.trim() === '')
              "
              data-testid="journal-lane-add"
            >
              {{ t('journal.scope.add') }}
            </button>
          </div>
        </form>
      </template>
    </div>

    <span class="pt-1 text-xs font-semibold">{{ t('journal.filter.label') }}</span>

    <div class="flex min-w-0 flex-wrap items-center gap-2">
      <ul
        v-if="chips.length > 0"
        key="chips"
        class="flex flex-wrap items-center gap-2"
        :aria-label="t('journal.filter.label')"
        data-testid="journal-filter-chips"
      >
        <template v-for="(chip, index) in chips" :key="`${index}-${chip.text}`">
          <li v-if="index > 0" key="joiner" class="text-xs" aria-hidden="true">{{ joiner }}</li>
          <li
            class="badge badge-soft h-8 max-w-full gap-1 whitespace-normal"
            data-testid="journal-filter-chip"
          >
            <span class="break-words">{{ chip.text }}</span>
            <button
              type="button"
              class="btn btn-ghost btn-xs btn-circle size-5 min-h-0"
              :aria-label="t('journal.scope.removeCondition', { condition: chip.text })"
              data-testid="journal-filter-chip-remove"
              @click="dropChip(index)"
            >
              <X class="size-3" aria-hidden="true"/>
            </button>
          </li>
        </template>
      </ul>

      <JournalFilterBuilder
        :workflow="workflow"
        :filter="state.filter"
        @apply="update({ filter: $event })"
      />
    </div>

    <div
      class="col-span-2 flex flex-wrap items-center justify-between gap-2 border-t border-base-300 pt-2"
    >
      <div class="flex flex-wrap items-center gap-2">
        <span
          v-if="state.filter"
          key="count"
          class="text-sm"
          role="status"
          data-testid="journal-scope-count"
        >
          {{ t('journal.scope.match', {shown: number(shown), total: number(total)}) }}
        </span>

        <button
          v-if="active"
          key="clear"
          type="button"
          class="btn btn-ghost btn-xs"
          data-testid="journal-scope-clear"
          @click="clearAll"
        >
          <X class="size-3" aria-hidden="true"/>
          {{ t('journal.scope.clear') }}
        </button>
      </div>

      <div class="ms-auto flex flex-wrap items-center gap-2">
        <slot/>
      </div>
    </div>
  </div>
</template>

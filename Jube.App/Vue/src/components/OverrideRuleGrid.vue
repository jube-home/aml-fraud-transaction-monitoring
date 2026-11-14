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
import {useQuery, useQueryClient} from '@tanstack/vue-query'
import {queryKeys} from '../api/queryKeys'
import {directionFor} from '../i18n'
import {useApprovalState} from '../composables/useApprovalState'
import {approvalKey, useRecordApprovals} from '../composables/useRecordApprovals'
import {overrideApi} from '../overrides/api'
import {approvalBadgeCell, choiceCell, expiryCell, switchCell} from '../overrides/cells'
import {
  approvalStateOf,
  awaitingCount,
  forceTargets,
  isGoverned,
  ruleOverrideArea,
} from '../overrides/forceApproval'
import {kindKey, offeredKinds} from '../overrides/state'
import type {OverrideSelection} from '../overrides/selection'
import type {OverrideActions} from '../overrides/useOverrideActions'
import type {OverrideRule, OverrideScope} from '../overrides/types'
import DataGrid from './DataGrid.vue'
import OverrideApprovalReview from './OverrideApprovalReview.vue'
import type {GridActionEvent, GridColumn} from './dataGridTypes'

const props = withDefaults(
  defineProps<{
    scope: OverrideScope
    entityAnalysisModelGuid: string
    actions: OverrideActions
    canForce?: boolean
    selection?: OverrideSelection
  }>(),
  {canForce: true},
)

const {t, locale} = useI18n()

const direction = computed(() => directionFor(locale.value))

const isolate = (cell: unknown) => (cell == null ? '' : `\u2068${String(cell)}\u2069`)

const queryClient = useQueryClient()
const approvalState = useApprovalState()
const reviewing = ref<{ id: number; name: string }>()

const rules = useQuery({
  queryKey: computed(() => queryKeys.overrideRules(props.scope, props.entityAnalysisModelGuid)),
  queryFn: () => overrideApi.rules(props.scope, props.entityAnalysisModelGuid),
  retry: false,
})

const baseRows = computed(() =>
  (rules.data.value ?? []).map((rule) => ({
    ...rule,
    hasOverride: props.selection
      ? props.selection.hasRule(rule.entityAnalysisModelGuid, rule.name)
      : rule.hasOverride,
    overrideKind: props.selection
      ? props.selection.kindOfRule(rule.entityAnalysisModelGuid, rule.name)
      : rule.overrideKind,
    id: rule.entityAnalysisModelActivationRuleOverrideId || rule.name,
  })),
)

const statuses = useRecordApprovals(
  computed(() => (props.selection ? [] : forceTargets(baseRows.value, ruleOverrideArea))),
)

const rows = computed(() =>
  baseRows.value.map((row) => ({
    ...row,
    approvalState: isGoverned(row)
      ? approvalStateOf(
        statuses.value.get(approvalKey(ruleOverrideArea.approval.kind, row.overrideId)),
      )
      : undefined,
  })),
)

const awaiting = computed(() => awaitingCount(rows.value.map((row) => row.approvalState)))

function drawApproval(row: object): Element {
  const state = (row as { approvalState?: Parameters<typeof approvalState.label>[0] }).approvalState

  return approvalBadgeCell(
    state ? approvalState.label(state) : undefined,
    state ? approvalState.tone(state) : 'neutral',
    t('override.approval.note'),
  )
}

function reviewActions(row: object) {
  const rule = row as OverrideRule

  return !props.selection && isGoverned(rule)
    ? [{id: 'review', label: t('override.approval.review')}]
    : []
}

function onAction(event: GridActionEvent): void {
  const rule = event.row as OverrideRule

  reviewing.value = {id: rule.overrideId, name: rule.name}
}

async function reviewed(): Promise<void> {
  await queryClient.invalidateQueries({queryKey: ['override']})
  await queryClient.invalidateQueries({queryKey: ['approval']})
}

function draw(field: 'switch' | 'kind' | 'expiry') {
  return (row: object): Element => {
    const rule = row as OverrideRule

    if (field === 'switch') {
      return switchCell(
        rule.hasOverride,
        `${t('override.col.override')}: ${isolate(rule.name)}`,
        (active) => props.actions.setRule(rule.entityAnalysisModelGuid, rule.name, active),
        rule.pendingRemoval,
      )
    }

    if (field === 'kind') {
      return choiceCell(
        rule.overrideKind,
        offeredKinds(rule.overrideKind, rule.enableForce && props.canForce).map((value) => ({
          value,
          label:
            kindKey(value) === 'force' ? t('override.kind.force') : t('override.kind.suppress'),
        })),
        !rule.hasOverride || rule.pendingRemoval,
        `${t('override.col.kind')}: ${isolate(rule.name)}`,
        (kind) =>
          props.actions.setRuleKind(rule.entityAnalysisModelGuid, rule.name, kind === 1 ? 1 : 0),
      )
    }

    return expiryCell(
      rule.deleteExpiryDate,
      !rule.hasOverride || rule.pendingRemoval,
      `${t('override.col.expiry')}: ${isolate(rule.name)}`,
      props.actions.reportTooEarly,
      (expiry) => props.actions.setRuleExpiry(rule.entityAnalysisModelGuid, rule.name, expiry),
    )
  }
}

const columns = computed<GridColumn[]>(() =>
  [
    {field: 'hasOverride', title: t('override.col.override'), width: 110, draw: draw('switch')},
    {field: 'overrideKind', title: t('override.col.kind'), width: 150, draw: draw('kind')},
    {
      field: 'deleteExpiryDate',
      title: t('override.col.expiry'),
      width: 230,
      draw: draw('expiry'),
    },
    {field: 'name', title: t('override.col.rule'), minWidth: 200, format: isolate},
    {field: 'approvalState', title: t('override.col.approval'), width: 150, draw: drawApproval},
    {
      field: 'review',
      title: '',
      hiddenTitle: t('override.col.actions'),
      width: 100,
      actions: reviewActions,
    },
  ].filter(
    (column) =>
      !props.selection ||
      (column.field !== 'deleteExpiryDate' &&
        column.field !== 'approvalState' &&
        column.field !== 'review'),
  ),
)
</script>

<template>
  <div class="min-w-0" :data-testid="`override-rules-${entityAnalysisModelGuid}`">
    <p v-if="rules.isError.value" key="failed" role="alert" class="text-sm text-error">
      {{ t('override.failed') }}
    </p>
    <template v-else>
      <p
        v-if="awaiting > 0"
        key="awaiting"
        class="mb-2 text-sm"
        data-testid="override-rules-awaiting"
      >
        {{ t('override.approval.awaiting', awaiting) }}
      </p>
      <DataGrid
        key="grid"
        :columns="columns"
        :rows="rows"
        :direction="direction"
        :empty-text="t('override.noRules')"
        :label="t('override.rulesLabel')"
        auto-height
        @action="onAction"
      />
    </template>
    <OverrideApprovalReview
      v-if="reviewing"
      key="review"
      :area="ruleOverrideArea"
      :entity-id="reviewing.id"
      :name="reviewing.name"
      @changed="reviewed"
      @close="reviewing = undefined"
    />
  </div>
</template>

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
import { queryKeys } from '../api/queryKeys'
import { directionFor } from '../i18n'
import { overrideApi } from '../overrides/api'
import { choiceCell, expiryCell, switchCell } from '../overrides/cells'
import { kindKey, offeredKinds } from '../overrides/state'
import type { OverrideActions } from '../overrides/useOverrideActions'
import type { OverrideRule, OverrideScope } from '../overrides/types'
import DataGrid from './DataGrid.vue'
import type { GridColumn } from './dataGridTypes'

const props = withDefaults(
  defineProps<{
    scope: OverrideScope
    entityAnalysisModelGuid: string
    actions: OverrideActions
    canForce?: boolean
  }>(),
  { canForce: true },
)

const { t, locale } = useI18n()

const direction = computed(() => directionFor(locale.value))

const rules = useQuery({
  queryKey: computed(() => queryKeys.overrideRules(props.scope, props.entityAnalysisModelGuid)),
  queryFn: () => overrideApi.rules(props.scope, props.entityAnalysisModelGuid),
  retry: false,
})

const rows = computed(() =>
  (rules.data.value ?? []).map((rule) => ({
    ...rule,
    id: rule.entityAnalysisModelActivationRuleOverrideId || rule.name,
  })),
)

function draw(field: 'switch' | 'kind' | 'expiry') {
  return (row: object): Element => {
    const rule = row as OverrideRule

    if (field === 'switch') {
      return switchCell(rule.hasOverride, t('override.col.override'), (active) =>
        props.actions.setRule(rule.entityAnalysisModelGuid, rule.name, active),
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
        !rule.hasOverride,
        t('override.col.kind'),
        (kind) =>
          props.actions.setRuleKind(rule.entityAnalysisModelGuid, rule.name, kind === 1 ? 1 : 0),
      )
    }

    return expiryCell(
      rule.deleteExpiryDate,
      !rule.hasOverride,
      t('override.col.expiry'),
      props.actions.reportTooEarly,
      (expiry) => props.actions.setRuleExpiry(rule.entityAnalysisModelGuid, rule.name, expiry),
    )
  }
}

const columns = computed<GridColumn[]>(() => [
  { field: 'hasOverride', title: t('override.col.override'), width: 110, draw: draw('switch') },
  { field: 'overrideKind', title: t('override.col.kind'), width: 150, draw: draw('kind') },
  { field: 'deleteExpiryDate', title: t('override.col.expiry'), width: 230, draw: draw('expiry') },
  { field: 'name', title: t('override.col.rule'), minWidth: 200 },
])
</script>

<template>
  <div class="min-w-0" :data-testid="`override-rules-${entityAnalysisModelGuid}`">
    <p v-if="rules.isError.value" key="failed" role="alert" class="text-sm text-error">
      {{ t('override.failed') }}
    </p>
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('override.noRules')"
      :label="t('override.rulesLabel')"
      auto-height
    />
  </div>
</template>

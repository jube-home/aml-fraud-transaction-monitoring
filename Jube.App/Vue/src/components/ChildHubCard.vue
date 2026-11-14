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
import {queryKeys} from '../api/queryKeys'
import {formModelApi} from '../formModel/api'
import {iconForArea} from '../formModel/areaIcons'
import {summariseChildren} from '../formModel/hierarchy'
import type {FormChild, FormModel} from '../formModel/types'
import HubTile from './HubTile.vue'

const props = defineProps<{
  child: FormChild
  childModel: FormModel | undefined
  parentValue: number | string | undefined
  awaiting?: number
}>()

const emit = defineEmits<{ open: []; add: [] }>()

const {t} = useI18n()

const rows = useQuery({
  queryKey: computed(() => queryKeys.formRecords(props.child.area, props.parentValue)),
  queryFn: () => formModelApi.records(props.childModel!, props.parentValue, props.child.listRoute),
  enabled: computed(() => props.childModel !== undefined && props.parentValue !== undefined),
  retry: false,
})

const summary = computed(() => summariseChildren(props.childModel, rows.data.value ?? []))
const canAdd = computed(() => props.childModel?.operations.includes('create') ?? false)
const icon = computed(() => iconForArea(props.child.area))

const meta = computed(() => {
  if (!rows.isSuccess.value) return ''

  if (summary.value.total === 0) return t('hierarchy.none')

  const parts: string[] = []

  if (summary.value.active !== null)
    parts.push(t('hierarchy.active', {count: summary.value.active}))

  if (summary.value.locked !== null && summary.value.locked > 0) {
    parts.push(t('hierarchy.locked', {count: summary.value.locked}))
  }

  return parts.join(', ')
})

const pill = computed(() =>
  props.awaiting !== undefined && props.awaiting > 0
    ? t('hierarchy.awaiting', {count: props.awaiting})
    : undefined,
)
</script>

<template>
  <HubTile
    :id="child.area"
    :label="child.label"
    :icon="icon"
    :meta="meta"
    :count="rows.isSuccess.value ? summary.total : undefined"
    :pending="rows.isPending.value"
    :failed="rows.isError.value ? t('hierarchy.failed') : undefined"
    :pill="pill"
    tone="warning"
    :order="child.order"
    :add-label="canAdd ? t('hierarchy.add', { label: child.label }) : undefined"
    @open="emit('open')"
    @add="emit('add')"
  />
</template>

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
import type {AreaRoot} from '../areaStory/types'
import {formModelApi} from '../formModel/api'
import {iconForArea} from '../formModel/areaIcons'
import HubTile from './HubTile.vue'

const props = defineProps<{ root: AreaRoot }>()

const emit = defineEmits<{ open: [] }>()

const {t} = useI18n()

const model = useQuery({
  queryKey: computed(() => queryKeys.formModel(props.root.area ?? '')),
  queryFn: () => formModelApi.model(props.root.area!),
  enabled: computed(() => props.root.area !== undefined),
  staleTime: 30_000,
  retry: false,
})

const rows = useQuery({
  queryKey: computed(() => queryKeys.formRecords(props.root.area ?? '', undefined)),
  queryFn: () => formModelApi.records(model.data.value!, undefined),
  enabled: computed(() => model.data.value !== undefined),
  retry: false,
})

const icon = computed(() => props.root.icon ?? iconForArea(props.root.area ?? ''))
</script>

<template>
  <HubTile
    :id="root.id"
    :label="root.title"
    :icon="icon"
    :meta="root.hint"
    :count="root.area && rows.isSuccess.value ? (rows.data.value?.length ?? 0) : undefined"
    :pending="root.area !== undefined && rows.isPending.value"
    :failed="root.area && rows.isError.value ? t('hierarchy.failed') : undefined"
    @open="emit('open')"
  />
</template>

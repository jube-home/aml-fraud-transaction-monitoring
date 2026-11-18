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
import {useQuery} from '@tanstack/vue-query'
import {queryKeys} from '../api/queryKeys'
import {formModelApi} from '../formModel/api'
import {useFormModels} from '../formModel/useFormModels'
import type {PendingEntity} from '../integrity/model'
import type {StoryStep} from '../story/path'
import GridRowSummary from './GridRowSummary.vue'
import RecordSummary from './RecordSummary.vue'

const props = defineProps<{ entity: PendingEntity }>()

const emit = defineEmits<{ open: [steps: StoryStep[]] }>()

const forms = useFormModels()

const model = computed(() =>
  forms.withApproval.value.find((candidate) => candidate.approval?.kind === props.entity.kind),
)

const record = useQuery({
  queryKey: computed(() => queryKeys.formRecord(model.value?.area ?? '', props.entity.entityId)),
  queryFn: async () => (await formModelApi.record(model.value!, props.entity.entityId)) ?? null,
  enabled: computed(() => model.value !== undefined),
  retry: false,
})

const failed = computed(
  () =>
    forms.query.isError.value ||
    record.isError.value ||
    record.data.value === null ||
    (forms.query.isSuccess.value && model.value === undefined),
)
</script>

<template>
  <RecordSummary
    v-if="model && record.data.value"
    key="summary"
    :model="model"
    :row="record.data.value"
    @open="(steps) => emit('open', steps)"
  />
  <GridRowSummary
    v-else
    key="waiting"
    :loading="!failed"
    :failed="failed"
    :testid="`pending-summary-${entity.kind}-${entity.entityId}`"
  />
</template>

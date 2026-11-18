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
import {computed, ref, useSlots} from 'vue'
import {useI18n} from 'vue-i18n'
import {useMutation, useQuery, useQueryClient} from '@tanstack/vue-query'
import {ArrowLeft, LockOpen} from 'lucide-vue-next'
import {queryKeys} from '../api/queryKeys'
import {describeApiError} from '../api/problems'
import {hasPermission, meQueryOptions} from '../auth/me'
import type {OpenedRecord} from '../formModel/hierarchy'
import type {FormChild, FormModel} from '../formModel/types'
import {integrityApi} from '../integrity/api'
import {useBeatBack} from '../story/beatBack'
import {modelArea} from '../story/path'
import {modelUnlockPermission, storyApi} from '../story/api'
import ChildHubs from './ChildHubs.vue'
import CrudPanel from './CrudPanel.vue'

const props = defineProps<{
  record: OpenedRecord
  model: FormModel | undefined
  models: FormModel[]
  modelId: number
  root: boolean
  backLabel: string
  parentId?: number | string
  listRoute?: string
}>()

const emit = defineEmits<{
  child: [child: FormChild, startNew: boolean]
  back: []
  opened: [record: OpenedRecord]
  dirty: [dirty: boolean]
}>()

defineSlots<{ operate?(): unknown; extra?(): unknown }>()

const slots = useSlots()
const {t} = useI18n()
const hosted = useBeatBack(() => ({label: props.backLabel, testid: 'record-back', run: () => emit('back')}))
const queryClient = useQueryClient()
const me = useQuery(meQueryOptions)

const canUnlock = computed(
  () =>
    props.root &&
    props.record.values.locked === true &&
    hasPermission(me.data.value, modelUnlockPermission),
)

const reloads = ref(0)

const unlock = useMutation({
  mutationFn: () => storyApi.unlock(props.record.id),
  onSuccess: async () => {
    await queryClient.invalidateQueries({queryKey: queryKeys.formRecords(modelArea, undefined)})
    reloads.value += 1
  },
})

const unlockFailure = computed(() =>
  unlock.error.value
    ? describeApiError(unlock.error.value, {
      failed: t('models.unlockFailed'),
      refused: t('models.refused'),
    })
    : undefined,
)

const pending = useQuery({
  queryKey: computed(() => queryKeys.integrityPending(props.modelId)),
  queryFn: () => integrityApi.pending(props.modelId),
  retry: false,
  staleTime: 30_000,
})

const ownPending = computed(() =>
  (pending.data.value ?? []).filter((row) => props.root || row.parentId === props.record.id),
)

const awaitingByKind = computed(() => {
  const counts = new Map<number, number>()

  for (const row of ownPending.value) counts.set(row.kind, (counts.get(row.kind) ?? 0) + 1)

  return counts
})
</script>

<template>
  <div class="flex flex-col gap-3" data-testid="record-beat">
    <div>
      <button v-if="!hosted" type="button" class="btn btn-sm" data-testid="record-back" @click="emit('back')">
        <ArrowLeft class="size-4 rtl:rotate-180" aria-hidden="true"/>
        {{ t('story.backTo', {label: backLabel}) }}
      </button>
      <button
        v-if="canUnlock"
        key="unlock"
        type="button"
        class="btn btn-sm ms-2"
        :disabled="unlock.isPending.value"
        :title="t('models.unlockHint')"
        data-testid="record-unlock"
        @click="unlock.mutate()"
      >
        <LockOpen class="size-4" aria-hidden="true"/>
        {{ t('models.unlock') }}
      </button>
      <ul
        v-if="unlockFailure"
        class="mt-2 text-sm text-error"
        role="alert"
        data-testid="unlock-failed"
      >
        <li v-for="reason in unlockFailure" :key="reason">{{ reason }}</li>
      </ul>
    </div>

    <ChildHubs
      :model="model"
      :record="record"
      :models="models"
      :awaiting="awaitingByKind"
      @open="(child) => emit('child', child, false)"
      @add="(child) => emit('child', child, true)"
    >
      <template v-if="slots.operate" #operate>
        <slot name="operate"/>
      </template>
      <template v-if="slots.extra" #extra>
        <slot name="extra"/>
      </template>
    </ChildHubs>

    <section data-testid="record-properties">
      <CrudPanel
        v-if="model"
        :key="reloads"
        :area="model.area"
        :parent-id="parentId"
        :list-route="listRoute"
        :initial-id="record.id"
        form-only
        @opened="(opened) => emit('opened', opened)"
        @closed="emit('back')"
        @deleted="emit('back')"
        @dirty="(value) => emit('dirty', value)"
      >
      </CrudPanel>
    </section>
  </div>
</template>

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
import {useMutation, useQuery, useQueryClient} from '@tanstack/vue-query'
import {Eraser} from 'lucide-vue-next'
import FieldHelp from '../components/FieldHelp.vue'
import {meQueryOptions} from '../auth/me'
import {useToasts} from '../stores/toasts'
import {
  canDecide,
  canRetract,
  erasureApi,
  erasureKey,
  erasurePermission,
  maximumKeyName,
  maximumKeyValue,
  maximumReason,
  subjectReady,
  type ErasurePreview,
} from './erasureApi'

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()
const me = useQuery(meQueryOptions)

const held = computed(() =>
  Array.isArray(me.data.value?.permissions) ? me.data.value.permissions : [],
)
const user = computed(() => me.data.value?.userName ?? null)
const canRequest = computed(() => held.value.includes(erasurePermission.request))
const canApprove = computed(() => held.value.includes(erasurePermission.approve))

const requests = useQuery({
  queryKey: erasureKey,
  queryFn: () => erasureApi.list(),
  enabled: computed(() => canRequest.value || canApprove.value),
  staleTime: 10_000,
  retry: false,
})

const keyName = ref('')
const keyValue = ref('')
const reason = ref('')
const preview = ref<ErasurePreview | null>(null)
const notes = ref<Record<string, string>>({})
const ready = computed(() => subjectReady(keyName.value, keyValue.value))

function refresh(): void {
  void client.invalidateQueries({queryKey: ['service', 'Erasure']})
}

const previewing = useMutation({
  mutationFn: () => erasureApi.preview(keyName.value.trim(), keyValue.value.trim()),
  onSuccess: (result) => {
    preview.value = result
  },
  onError: (error) => toasts.apiError(error),
})

const requesting = useMutation({
  mutationFn: () =>
    erasureApi.request(keyName.value.trim(), keyValue.value.trim(), reason.value.trim()),
  onSuccess: () => {
    preview.value = null
    reason.value = ''
    toasts.success(t('erasure.recorded'))
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})

const exporting = useMutation({
  mutationFn: () => erasureApi.export(keyName.value.trim(), keyValue.value.trim()),
  onSuccess: (bundle) => {
    const blob = new Blob([JSON.stringify(bundle, null, 2)], {type: 'application/json'})
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = `subject-export-${Date.now()}.json`
    link.click()
    URL.revokeObjectURL(link.href)
    toasts.success(bundle.truncated ? t('erasure.truncated') : t('erasure.exported'))
  },
  onError: (error) => toasts.apiError(error),
})

const deciding = useMutation({
  mutationFn: (work: () => Promise<unknown>) => work(),
  onSuccess: () => {
    toasts.success(t('erasure.recorded'))
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})

function approve(guid: string): void {
  deciding.mutate(() => erasureApi.approve(guid, notes.value[guid]?.trim() || null))
}

function reject(guid: string): void {
  deciding.mutate(() => erasureApi.reject(guid, (notes.value[guid] ?? '').trim()))
}

function retract(guid: string): void {
  deciding.mutate(() => erasureApi.retract(guid))
}
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="adminErasure-page">
    <h1 class="flex items-center gap-2 text-lg font-semibold">
      <Eraser class="size-4" aria-hidden="true"/>
      {{ t('erasure.title') }}
      <FieldHelp :label="t('erasure.title')" :text="t('erasure.hint')"/>
    </h1>

    <fieldset v-if="canRequest" class="fieldset" data-testid="erasure-subject">
      <div class="flex flex-wrap gap-2">
        <input
          v-model="keyName"
          type="text"
          class="input input-sm"
          :maxlength="maximumKeyName"
          :placeholder="t('erasure.keyName')"
          :aria-label="t('erasure.keyName')"
          data-testid="erasure-key-name"
        />
        <input
          v-model="keyValue"
          type="text"
          class="input input-sm"
          :maxlength="maximumKeyValue"
          :placeholder="t('erasure.keyValue')"
          :aria-label="t('erasure.keyValue')"
          data-testid="erasure-key-value"
        />
        <input
          v-model="reason"
          type="text"
          class="input input-sm grow"
          :maxlength="maximumReason"
          :placeholder="t('erasure.reason')"
          :aria-label="t('erasure.reason')"
          data-testid="erasure-reason"
        />
      </div>
      <div class="flex flex-wrap gap-2">
        <button
          type="button"
          class="btn btn-sm"
          :disabled="!ready || previewing.isPending.value"
          data-testid="erasure-preview"
          @click="previewing.mutate()"
        >
          {{ t('erasure.preview') }}
        </button>
        <button
          type="button"
          class="btn btn-sm btn-primary"
          :disabled="!ready || !reason.trim() || requesting.isPending.value"
          data-testid="erasure-request"
          @click="requesting.mutate()"
        >
          {{ t('erasure.request') }}
        </button>
        <button
          type="button"
          class="btn btn-sm"
          :disabled="!ready || exporting.isPending.value"
          data-testid="erasure-export"
          @click="exporting.mutate()"
        >
          {{ t('erasure.export') }}
        </button>
      </div>
      <p v-if="preview" class="text-sm" data-testid="erasure-preview-result">
        {{
          preview.archiveRows > 0
            ? t('erasure.rows', {count: preview.archiveRows})
            : t('erasure.noRows')
        }}
        <span v-if="preview.refusalReasonId" class="text-error">
          {{ t(`erasure.refusals.${preview.refusalReasonId}`) }}
        </span>
      </p>
    </fieldset>

    <h2 class="text-sm font-semibold">{{ t('erasure.requests') }}</h2>
    <p v-if="requests.isError.value" role="alert" class="text-sm">{{ t('erasure.failed') }}</p>
    <p
      v-else-if="(requests.data.value ?? []).length === 0"
      class="text-base-content/70 text-sm"
      data-testid="erasure-empty"
    >
      {{ t('erasure.none') }}
    </p>

    <div
      v-for="item in requests.data.value ?? []"
      :key="item.guid"
      class="border-base-300 flex flex-col gap-1 border-b py-2 text-sm"
      :data-testid="`erasure-${item.guid}`"
    >
      <div class="flex flex-wrap items-center gap-2">
        <span class="font-semibold">{{ item.keyName }}</span>
        <span class="font-mono">{{ item.keyValue }}</span>
        <span class="badge badge-sm" :data-testid="`erasure-state-${item.guid}`">{{
            t(`erasure.states.${item.stateId}`)
          }}</span>
        <span v-if="item.refusalReasonId" class="text-error">{{
            t(`erasure.refusals.${item.refusalReasonId}`)
          }}</span>
        <span class="text-base-content/70">{{
            t('erasure.by', {user: item.requestedUser})
          }}</span>
      </div>
      <p class="text-base-content/70">{{ item.reason }}</p>
      <p v-if="item.stateId === 3" class="text-base-content/70">
        {{
          t('erasure.removed', {
            archive: item.archiveRowsRemoved ?? 0,
            nodes: item.graphNodesRemoved ?? 0,
            edges: item.graphEdgesRemoved ?? 0,
            cases: item.casePayloadsNulled ?? 0,
          })
        }}
      </p>

      <div v-if="canApprove && canDecide(item, user)" class="join w-full">
        <input
          v-model="notes[item.guid]"
          type="text"
          class="input input-sm join-item grow"
          :maxlength="maximumReason"
          :placeholder="t('erasure.decision')"
          :aria-label="t('erasure.decision')"
          :data-testid="`erasure-note-${item.guid}`"
        />
        <button
          type="button"
          class="btn btn-sm btn-primary join-item"
          :data-testid="`erasure-approve-${item.guid}`"
          @click="approve(item.guid)"
        >
          {{ t('erasure.approve') }}
        </button>
        <button
          type="button"
          class="btn btn-sm join-item"
          :disabled="!(notes[item.guid] ?? '').trim()"
          :data-testid="`erasure-reject-${item.guid}`"
          @click="reject(item.guid)"
        >
          {{ t('erasure.reject') }}
        </button>
      </div>
      <div v-if="canRequest && canRetract(item, user)">
        <button
          type="button"
          class="btn btn-sm"
          :data-testid="`erasure-retract-${item.guid}`"
          @click="retract(item.guid)"
        >
          {{ t('erasure.retract') }}
        </button>
      </div>
    </div>
  </section>
</template>

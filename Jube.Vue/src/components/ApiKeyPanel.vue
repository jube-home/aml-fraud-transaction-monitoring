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
import { computed, nextTick, ref, useTemplateRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import { Check, Copy, KeyRound, Plus } from 'lucide-vue-next'
import { queryKeys } from '../api/queryKeys'
import { ApiError } from '../api/useApi'
import { validationReasons } from '../api/problems'
import { apiKeyApi } from '../apiKeys/api'
import {
  draftProblems,
  emptyDraft,
  maxDescriptionLength,
  maxNameLength,
  newestFirst,
  payloadOf,
  prefixOf,
  type ApiKey,
  type ApiKeyDraft,
  type ApiKeyProblem,
} from '../apiKeys/model'
import { useFormat } from '../composables/useFormat'
import { directionFor } from '../i18n'
import { useToasts } from '../stores/toasts'
import ConfirmDialog from './ConfirmDialog.vue'
import DataGrid from './DataGrid.vue'
import PanelCard from './PanelCard.vue'
import ProblemAlert from './ProblemAlert.vue'
import type { GridActionEvent, GridColumn } from './dataGridTypes'

const props = defineProps<{ userRegistryId: number }>()

const emit = defineEmits<{ changed: [] }>()

const { t, locale } = useI18n()
const { dateTime } = useFormat()
const toasts = useToasts()
const queryClient = useQueryClient()

const direction = computed(() => directionFor(locale.value))
const queryKey = computed(() => queryKeys.apiKeys(props.userRegistryId))

const keys = useQuery({
  queryKey,
  queryFn: () => apiKeyApi.list(props.userRegistryId),
  retry: false,
})

const creating = ref(false)
const draft = ref<ApiKeyDraft>(emptyDraft())
const reasons = ref<string[]>([])
const revealed = ref<{ id: number; key: string; name: string }>()
const copied = ref(false)
const pendingRevoke = ref<ApiKey>()
const nameInput = useTemplateRef<HTMLInputElement>('nameInput')
const keyInput = useTemplateRef<HTMLInputElement>('keyInput')

const rows = computed(() => newestFirst(keys.data.value ?? []))

const problemText: Record<ApiKeyProblem, string> = {
  nameRequired: t('security.apiKeys.nameRequired'),
  nameTooLong: t('security.apiKeys.nameTooLong', { max: maxNameLength }),
  descriptionTooLong: t('security.apiKeys.descriptionTooLong', { max: maxDescriptionLength }),
}

const columns = computed<GridColumn[]>(() => [
  { field: 'name', title: t('security.apiKeys.col.name'), minWidth: 160 },
  { field: 'description', title: t('security.apiKeys.col.description'), minWidth: 200 },
  {
    field: 'apiKeyDisplay',
    title: t('security.apiKeys.col.key'),
    width: 170,
    mono: true,
    format: (value) => t('security.apiKeys.prefix', { prefix: prefixOf(String(value ?? '')) }),
  },
  {
    field: 'createdDate',
    title: t('security.apiKeys.col.created'),
    width: 190,
    mono: true,
    type: 'date',
    format: (value) => (value ? dateTime(String(value)) : ''),
  },
  {
    field: '',
    title: t('security.apiKeys.col.actions'),
    width: 120,
    pinned: true,
    actions: () => [{ id: 'revoke', label: t('security.apiKeys.revoke'), tone: 'error' }],
  },
])

watch(
  () => props.userRegistryId,
  () => {
    creating.value = false
    draft.value = emptyDraft()
    reasons.value = []
    revealed.value = undefined
    copied.value = false
  },
)

async function refresh(): Promise<void> {
  await queryClient.invalidateQueries({ queryKey: queryKey.value })
  emit('changed')
}

function failure(error: unknown): void {
  if (error instanceof ApiError && error.kind === 'validation') {
    reasons.value = validationReasons(error, t('security.apiKeys.refused'))

    return
  }

  toasts.apiError(error)
}

const create = useMutation({
  mutationFn: () => apiKeyApi.create(payloadOf(props.userRegistryId, draft.value)),
  onSuccess: async (created) => {
    reasons.value = []
    creating.value = false
    copied.value = false
    revealed.value = {
      id: created.id,
      key: created.apiKeyDisplay ?? '',
      name: created.name ?? draft.value.name.trim(),
    }
    draft.value = emptyDraft()
    toasts.success(t('security.apiKeys.created'))
    await refresh()
    await nextTick()
    keyInput.value?.focus()
    keyInput.value?.select()
  },
  onError: failure,
})

const revoke = useMutation({
  mutationFn: (key: ApiKey) => apiKeyApi.revoke(key.id),
  onSuccess: async (_result, key) => {
    if (revealed.value?.id === key.id) revealed.value = undefined

    toasts.success(t('security.apiKeys.revoked'))
    await refresh()
  },
  onError: failure,
})

async function startCreate(): Promise<void> {
  reasons.value = []
  revealed.value = undefined
  draft.value = emptyDraft()
  creating.value = true
  await nextTick()
  nameInput.value?.focus()
}

function cancelCreate(): void {
  creating.value = false
  reasons.value = []
  draft.value = emptyDraft()
}

function submit(): void {
  const problems = draftProblems(draft.value)

  if (problems.length > 0) {
    reasons.value = problems.map((problem) => problemText[problem])
    nameInput.value?.focus()

    return
  }

  reasons.value = []
  create.mutate()
}

async function copyKey(): Promise<void> {
  if (!revealed.value) return

  try {
    await navigator.clipboard.writeText(revealed.value.key)
    copied.value = true
    toasts.success(t('security.apiKeys.copied'))
  } catch {
    keyInput.value?.focus()
    keyInput.value?.select()
    toasts.error(t('security.apiKeys.copyFailed'))
  }
}

function selectKey(event: FocusEvent): void {
  ;(event.target as HTMLInputElement).select()
}

function dismissReveal(): void {
  revealed.value = undefined
  copied.value = false
}

function onAction(event: GridActionEvent): void {
  if (event.id === 'revoke') pendingRevoke.value = event.row as ApiKey
}

function confirmRevoke(): void {
  const key = pendingRevoke.value

  pendingRevoke.value = undefined

  if (key) revoke.mutate(key)
}
</script>

<template>
  <PanelCard :title="t('security.apiKeys.panelTitle')" data-testid="api-key-panel">
    <template #icon><KeyRound class="size-4" aria-hidden="true" /></template>
    <template #actions>
      <button
        type="button"
        class="btn btn-sm btn-primary ms-auto"
        :disabled="creating || create.isPending.value"
        data-testid="api-key-generate"
        @click="startCreate"
      >
        <Plus aria-hidden="true" class="size-4" />
        {{ t('security.apiKeys.generate') }}
      </button>
    </template>

    <p class="text-sm text-secondary">{{ t('security.apiKeys.hint') }}</p>

    <form
      v-if="creating"
      key="create"
      class="flex flex-col gap-3 rounded-box border border-base-300 bg-base-200 p-4"
      novalidate
      data-testid="api-key-form"
      @submit.prevent="submit"
    >
      <div class="grid gap-3 @lg:grid-cols-2">
        <fieldset class="fieldset">
          <label class="label" for="api-key-name">{{ t('security.apiKeys.name') }}</label>
          <input
            id="api-key-name"
            ref="nameInput"
            v-model="draft.name"
            type="text"
            class="input input-sm w-full"
            :maxlength="maxNameLength"
            :placeholder="t('security.apiKeys.namePlaceholder')"
            required
            aria-required="true"
            data-testid="api-key-name"
          />
        </fieldset>
        <fieldset class="fieldset">
          <label class="label" for="api-key-description">
            {{ t('security.apiKeys.description') }}
            <span class="badge badge-sm font-mono">{{ t('security.apiKeys.optional') }}</span>
          </label>
          <input
            id="api-key-description"
            v-model="draft.description"
            type="text"
            class="input input-sm w-full"
            :maxlength="maxDescriptionLength"
            :placeholder="t('security.apiKeys.descriptionPlaceholder')"
            data-testid="api-key-description"
          />
        </fieldset>
      </div>
      <div class="flex flex-wrap justify-end gap-2">
        <button
          type="button"
          class="btn btn-sm"
          :disabled="create.isPending.value"
          data-testid="api-key-cancel"
          @click="cancelCreate"
        >
          {{ t('security.apiKeys.cancel') }}
        </button>
        <button
          type="submit"
          class="btn btn-sm btn-primary"
          :disabled="create.isPending.value"
          data-testid="api-key-submit"
        >
          <span
            v-if="create.isPending.value"
            key="busy"
            class="loading loading-spinner loading-xs"
            aria-hidden="true"
          ></span>
          {{ t('security.apiKeys.create') }}
        </button>
      </div>
    </form>

    <ProblemAlert v-if="reasons.length > 0" :reasons="reasons" data-testid="api-key-reasons" />

    <div
      v-if="revealed"
      key="reveal"
      role="alert"
      class="alert alert-info alert-vertical grid-cols-1 justify-items-stretch text-start"
      data-testid="api-key-reveal"
    >
      <div class="flex items-start gap-3">
        <KeyRound class="mt-0.5 size-5 shrink-0" aria-hidden="true" />
        <div>
          <h3 class="font-semibold">
            {{ t('security.apiKeys.revealTitle', { name: revealed.name }) }}
          </h3>
          <p class="text-sm">{{ t('security.apiKeys.revealBody') }}</p>
        </div>
      </div>
      <div class="flex flex-wrap items-center gap-2">
        <label class="sr-only" for="api-key-value">{{ t('security.apiKeys.keyLabel') }}</label>
        <input
          id="api-key-value"
          ref="keyInput"
          class="input input-sm min-w-0 flex-1 font-mono"
          type="text"
          readonly
          :value="revealed.key"
          data-testid="api-key-value"
          @focus="selectKey"
        />
        <button type="button" class="btn btn-sm" data-testid="api-key-copy" @click="copyKey">
          <Check v-if="copied" key="copied" aria-hidden="true" class="size-4" />
          <Copy v-else key="copy" aria-hidden="true" class="size-4" />
          {{ copied ? t('security.apiKeys.copied') : t('security.apiKeys.copy') }}
        </button>
        <button type="button" class="btn btn-sm" data-testid="api-key-done" @click="dismissReveal">
          {{ t('security.apiKeys.done') }}
        </button>
      </div>
    </div>

    <p
      v-if="keys.isError.value"
      key="error"
      role="alert"
      class="alert alert-error"
      data-testid="api-key-load-failed"
    >
      {{ t('security.apiKeys.loadFailed') }}
    </p>
    <div
      v-else-if="keys.isPending.value"
      key="loading"
      class="flex flex-col gap-2"
      role="status"
      data-testid="api-key-loading"
    >
      <span class="sr-only">{{ t('security.apiKeys.loading') }}</span>
      <div class="skeleton h-8 w-full"></div>
      <div class="skeleton h-8 w-full"></div>
    </div>
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('security.apiKeys.empty')"
      :label="t('security.apiKeys.title')"
      auto-height
      @action="onAction"
    />

    <ConfirmDialog
      id="api-key-revoke"
      :open="pendingRevoke !== undefined"
      :title="t('security.apiKeys.revokeTitle')"
      :body="t('security.apiKeys.revokeBody', { name: pendingRevoke?.name ?? '' })"
      :confirm-label="t('security.apiKeys.revoke')"
      :cancel-label="t('security.apiKeys.cancel')"
      @confirm="confirmRevoke"
      @cancel="pendingRevoke = undefined"
    />
  </PanelCard>
</template>

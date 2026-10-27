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
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import { ApiError } from '../api/useApi'
import { queryKeys } from '../api/queryKeys'
import { describeApiError } from '../api/problems'
import { directionFor } from '../i18n'
import { useExpandHosts } from '../composables/useExpandHosts'
import { integrityApi } from '../integrity/api'
import { approvalKindOf, pendingGroups, type PendingEntity } from '../integrity/model'
import ApprovalPanel from './ApprovalPanel.vue'
import DataGrid from './DataGrid.vue'
import type { GridColumn } from './dataGridTypes'
import PanelCard from './PanelCard.vue'
import PendingEntitiesDetail from './PendingEntitiesDetail.vue'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{ modelId: number }>()

const emit = defineEmits<{ changed: [] }>()

const { t, locale } = useI18n()
const { hosts, onExpand } = useExpandHosts()
const queryClient = useQueryClient()

const direction = computed(() => directionFor(locale.value))
const problems = ref<string[]>([])
const note = ref('')
const reviewing = ref<PendingEntity>()

const pending = useQuery({
  queryKey: computed(() => queryKeys.integrityPending(props.modelId)),
  queryFn: () => integrityApi.pending(props.modelId),
  retry: false,
})

const groups = computed(() => pendingGroups(pending.data.value ?? []))
const rows = computed(() => groups.value.map((group) => ({ ...group, id: group.kind })))
const forbidden = computed(
  () => pending.error.value instanceof ApiError && pending.error.value.kind === 'forbidden',
)

const reviewKind = computed(() =>
  reviewing.value ? approvalKindOf(reviewing.value.kind) : undefined,
)

async function refresh(): Promise<void> {
  await queryClient.invalidateQueries({ queryKey: queryKeys.integrityPending(props.modelId) })
  emit('changed')
}

function failure(error: unknown, action: 'approve' | 'reject' | 'values'): void {
  note.value = ''
  problems.value = describeApiError(error, {
    failed:
      action === 'values'
        ? t('integrity.pending.valuesFailed')
        : t('integrity.pending.refusedAction', {
            action:
              action === 'approve' ? t('integrity.pending.approve') : t('integrity.pending.reject'),
          }),
    refused: t('integrity.pending.noPermission'),
  })
}

const decide = useMutation({
  mutationFn: (input: { action: 'Approve' | 'Reject'; entity: PendingEntity }) =>
    integrityApi.decide(input.action, {
      kind: input.entity.kind,
      entityId: input.entity.entityId,
      version: input.entity.version,
      note: null,
    }),
  onSuccess: async () => {
    problems.value = []
    note.value = ''
    await refresh()
  },
  onError: (error, input) => failure(error, input.action === 'Approve' ? 'approve' : 'reject'),
})

const approveValues = useMutation({
  mutationFn: (entity: PendingEntity) => integrityApi.approveValues(entity.kind, entity.entityId),
  onSuccess: async (result) => {
    problems.value = result.refusals
    note.value = t('integrity.pending.approvedValues', { count: result.approved })
    reviewing.value = undefined
    await refresh()
  },
  onError: (error) => failure(error, 'values'),
})

function onEntityAction(action: string, entity: PendingEntity): void {
  if (action === 'approve') decide.mutate({ action: 'Approve', entity })
  else if (action === 'reject') decide.mutate({ action: 'Reject', entity })
  else if (action === 'values') approveValues.mutate(entity)
  else if (action === 'review') reviewing.value = entity
}

function reviewed(): void {
  reviewing.value = undefined
  void refresh()
}

const columns = computed<GridColumn[]>(() => [
  { field: 'kindName', title: t('integrity.pending.col.kind'), minWidth: 220 },
  {
    field: 'count',
    title: t('integrity.pending.col.waiting'),
    align: 'end',
    mono: true,
    width: 240,
  },
  {
    field: 'deletions',
    title: t('integrity.pending.col.deletions'),
    align: 'end',
    mono: true,
    width: 190,
  },
])
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="pending-approvals">
    <p v-if="note" key="note" class="text-sm" role="status" data-testid="pending-note">
      {{ note }}
    </p>
    <ProblemAlert
      v-if="problems.length > 0"
      key="problems"
      :reasons="problems"
      data-testid="pending-problems"
    />

    <ProblemAlert
      v-if="forbidden"
      key="forbidden"
      :reasons="[t('integrity.pending.noView')]"
      data-testid="pending-error"
    />
    <ProblemAlert
      v-else-if="pending.isError.value"
      key="failed"
      :reasons="[t('integrity.pending.loadFailed')]"
      data-testid="pending-error"
    />
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('integrity.pending.nothing')"
      :label="t('integrity.pending.label')"
      expandable
      auto-height
      :expand-labels="{
        expand: t('integrity.pending.showEntities'),
        collapse: t('integrity.pending.hideEntities'),
      }"
      @expand="onExpand"
    />

    <template v-for="[kind, host] in hosts" :key="String(kind)">
      <Teleport :to="host">
        <PendingEntitiesDetail
          :entities="groups.find((group) => group.kind === kind)?.entities ?? []"
          :busy="decide.isPending.value || approveValues.isPending.value"
          @action="onEntityAction"
        />
      </Teleport>
    </template>

    <PanelCard
      v-if="reviewing && reviewKind?.endpoint && reviewKind.historyRoute"
      key="review"
      :title="t('integrity.pending.reviewTitle', { name: reviewing.name })"
      compact
    >
      <template #actions>
        <button
          type="button"
          class="btn btn-sm btn-ghost ms-auto"
          data-testid="pending-close-review"
          @click="reviewing = undefined"
        >
          {{ t('integrity.pending.closeReview') }}
        </button>
      </template>
      <ApprovalPanel
        :endpoint="reviewKind.endpoint"
        :approval="{ kind: reviewKind.kind, historyRoute: reviewKind.historyRoute }"
        :entity-id="reviewing.entityId"
        @changed="reviewed"
      />
    </PanelCard>
  </div>
</template>

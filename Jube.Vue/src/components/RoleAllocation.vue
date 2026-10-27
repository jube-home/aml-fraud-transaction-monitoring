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
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import { Save, Undo2 } from 'lucide-vue-next'
import { queryKeys } from '../api/queryKeys'
import { useToasts } from '../stores/toasts'
import {
  applyRoleChanges,
  roleAllocationApi,
  type RoleAllocationTarget,
} from '../roleAllocation/api'
import { changeCount, planRoleChanges } from '../roleAllocation/plan'
import TransferList from './TransferList.vue'

const props = defineProps<RoleAllocationTarget>()

const emit = defineEmits<{ saved: [] }>()

const { t } = useI18n()
const toasts = useToasts()
const queryClient = useQueryClient()

const target = computed<RoleAllocationTarget>(() => ({
  baseEndpoint: props.baseEndpoint,
  parentKeyField: props.parentKeyField,
  guid: props.guid,
}))

const key = computed(() => queryKeys.roleAllocation(props.baseEndpoint, props.guid))

const roles = useQuery({ queryKey: queryKeys.permissionRoles(), queryFn: roleAllocationApi.roles })
const assignments = useQuery({
  queryKey: key,
  queryFn: () => roleAllocationApi.assignments(target.value),
})

const desired = ref<ReadonlySet<string>>(new Set())

watch(
  () => assignments.data.value,
  (rows) => {
    desired.value = new Set((rows ?? []).map((row) => row.roleRegistryGuid))
  },
  { immediate: true },
)

const items = computed(() =>
  (roles.data.value ?? []).map((role) => ({ key: role.guid, label: role.name })),
)

const changes = computed(() => planRoleChanges(assignments.data.value ?? [], desired.value))
const pending = computed(() => changeCount(changes.value))
const dirty = computed(() => pending.value > 0)

function discard(): void {
  desired.value = new Set((assignments.data.value ?? []).map((row) => row.roleRegistryGuid))
}

const save = useMutation({
  mutationFn: () => applyRoleChanges(target.value, changes.value),
  onSuccess: async () => {
    toasts.success(t('roleAllocation.saved'))
    await queryClient.invalidateQueries({ queryKey: key.value })
    emit('saved')
  },
  onError: async (error) => {
    toasts.apiError(error)
    await queryClient.invalidateQueries({ queryKey: key.value })
  },
})

defineExpose({ dirty })
</script>

<template>
  <section class="flex flex-col gap-3" data-testid="role-allocation">
    <p
      v-if="roles.isError.value || assignments.isError.value"
      role="alert"
      class="alert alert-error"
    >
      {{ t('roleAllocation.loadFailed') }}
    </p>
    <p
      v-else-if="roles.isPending.value || assignments.isPending.value"
      role="status"
      class="text-sm"
    >
      {{ t('roleAllocation.loading') }}
    </p>
    <template v-else>
      <TransferList
        v-model="desired"
        :items="items"
        :available-title="t('roleAllocation.available')"
        :assigned-title="t('roleAllocation.assigned')"
        :disabled="save.isPending.value"
      />
      <div class="flex items-center gap-2 border-t border-base-300 pt-3">
        <button
          type="button"
          class="btn btn-sm btn-primary"
          :disabled="!dirty || save.isPending.value"
          data-testid="role-save"
          @click="save.mutate()"
        >
          <Save aria-hidden="true" class="size-4" />
          {{ t('roleAllocation.save') }}
        </button>
        <button
          type="button"
          class="btn btn-sm"
          :disabled="!dirty || save.isPending.value"
          data-testid="role-discard"
          @click="discard"
        >
          <Undo2 aria-hidden="true" class="size-4" />
          {{ t('roleAllocation.discard') }}
        </button>
        <span v-if="dirty" class="badge badge-sm font-mono" data-testid="role-dirty">
          {{ t('roleAllocation.unsaved', { count: pending }) }}
        </span>
      </div>
    </template>
  </section>
</template>

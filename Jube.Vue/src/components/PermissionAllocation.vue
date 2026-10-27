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
import { Lock, Save, Undo2 } from 'lucide-vue-next'
import { queryKeys } from '../api/queryKeys'
import { useToasts } from '../stores/toasts'
import { groupPermissions, type AreaKey } from '../permissions/areas'
import { applyChanges, permissionApi } from '../permissions/api'
import { grantedIds, planChanges } from '../permissions/plan'

const props = defineProps<{ roleRegistryId?: number }>()

const emit = defineEmits<{ saved: [roleRegistryId: number] }>()

const { t } = useI18n()
const toasts = useToasts()
const queryClient = useQueryClient()

const roleId = ref<number | undefined>(props.roleRegistryId)
const search = ref('')
const desired = ref<Set<number>>(new Set())

const roles = useQuery({ queryKey: queryKeys.permissionRoles(), queryFn: permissionApi.roles })
const specifications = useQuery({
  queryKey: queryKeys.permissionSpecifications(),
  queryFn: permissionApi.specifications,
})
const grants = useQuery({
  queryKey: computed(() => queryKeys.permissionGrants(roleId.value ?? 0)),
  queryFn: () => permissionApi.grants(roleId.value!),
  enabled: computed(() => roleId.value !== undefined),
})

watch(
  () => props.roleRegistryId,
  (next) => {
    roleId.value = next
  },
)

watch(
  () => grants.data.value,
  (rows) => {
    desired.value = grantedIds(rows ?? [])
  },
  { immediate: true },
)

const original = computed(() => grantedIds(grants.data.value ?? []))
const lockedIds = computed(
  () =>
    new Set(
      (grants.data.value ?? [])
        .filter((grant) => grant.locked)
        .map((g) => g.permissionSpecificationId),
    ),
)

const changes = computed(() => planChanges(grants.data.value ?? [], desired.value))
const dirty = computed(() => changes.value.length > 0)

const groups = computed(() => {
  const needle = search.value.trim().toLowerCase()
  const visible = (specifications.data.value ?? []).filter(
    (item) => needle === '' || item.name.toLowerCase().includes(needle),
  )

  return groupPermissions(visible)
})

const grantedCount = computed(() => desired.value.size)

const areaTitles: Record<AreaKey, string> = {
  cases: t('permissions.area.cases'),
  rules: t('permissions.area.rules'),
  models: t('permissions.area.models'),
  lists: t('permissions.area.lists'),
  visualisation: t('permissions.area.visualisation'),
  security: t('permissions.area.security'),
  operations: t('permissions.area.operations'),
  analytics: t('permissions.area.analytics'),
  other: t('permissions.area.other'),
}

function toggle(id: number, checked: boolean): void {
  const next = new Set(desired.value)

  if (checked) next.add(id)
  else next.delete(id)

  desired.value = next
}

function discard(): void {
  desired.value = new Set(original.value)
}

const save = useMutation({
  mutationFn: () => applyChanges(roleId.value!, grants.data.value ?? [], changes.value),
  onSuccess: async () => {
    toasts.success(t('permissions.saved'))
    await queryClient.invalidateQueries({ queryKey: queryKeys.permissionGrants(roleId.value!) })
    emit('saved', roleId.value!)
  },
  onError: async (error) => {
    toasts.apiError(error)
    await queryClient.invalidateQueries({ queryKey: queryKeys.permissionGrants(roleId.value!) })
  },
})

defineExpose({ dirty })
</script>

<template>
  <section class="flex min-h-0 flex-1 flex-col gap-3" data-testid="permission-allocation">
    <div class="flex flex-wrap items-end gap-3">
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('permissions.role') }}</span>
        <select
          v-model.number="roleId"
          class="select select-sm min-w-56"
          :disabled="props.roleRegistryId !== undefined || dirty"
          data-testid="permission-role"
        >
          <option :value="undefined" disabled>{{ t('permissions.pickRole') }}</option>
          <option v-for="role in roles.data.value ?? []" :key="role.id" :value="role.id">
            {{ role.name }}
          </option>
        </select>
      </label>
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('permissions.search') }}</span>
        <input
          v-model="search"
          type="search"
          class="input input-sm w-64"
          data-testid="permission-search"
        />
      </label>
      <p
        v-if="roleId !== undefined"
        class="badge badge-sm font-mono"
        data-testid="permission-count"
      >
        {{ t('permissions.granted', { count: grantedCount }) }}
      </p>
    </div>

    <p
      v-if="roles.isError.value || specifications.isError.value"
      role="alert"
      class="alert alert-error"
    >
      {{ t('permissions.loadFailed') }}
    </p>
    <p v-else-if="roleId === undefined" class="text-sm" data-testid="permission-empty">
      {{ t('permissions.choose') }}
    </p>
    <p v-else-if="grants.isError.value" role="alert" class="alert alert-error">
      {{ t('permissions.loadFailed') }}
    </p>
    <p
      v-else-if="grants.isPending.value || specifications.isPending.value"
      class="text-sm"
      role="status"
    >
      {{ t('permissions.loading') }}
    </p>
    <p v-else-if="groups.length === 0" class="text-sm" data-testid="permission-none">
      {{ t('permissions.noMatches') }}
    </p>

    <div
      v-else
      class="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto"
      data-testid="permission-groups"
    >
      <fieldset v-for="group in groups" :key="group.area" class="fieldset">
        <legend class="fieldset-legend">{{ areaTitles[group.area] }}</legend>
        <ul class="grid gap-x-6 gap-y-1 md:grid-cols-2">
          <li v-for="item in group.items" :key="item.id">
            <label class="label cursor-pointer justify-start gap-3 text-base-content">
              <input
                type="checkbox"
                class="toggle toggle-sm toggle-primary"
                :checked="desired.has(item.id)"
                :disabled="lockedIds.has(item.id) || save.isPending.value"
                :data-testid="`permission-${item.id}`"
                @change="toggle(item.id, ($event.target as HTMLInputElement).checked)"
              />
              <span class="flex-1">{{ item.name }}</span>
              <span class="badge badge-ghost badge-sm font-mono">{{ item.id }}</span>
              <span v-if="lockedIds.has(item.id)" class="badge badge-sm font-mono">
                <Lock aria-hidden="true" class="size-3" />
                {{ t('permissions.locked') }}
              </span>
            </label>
          </li>
        </ul>
      </fieldset>
    </div>

    <div
      v-if="roleId !== undefined"
      class="flex shrink-0 items-center gap-2 border-t border-base-300 pt-3"
    >
      <button
        type="button"
        class="btn btn-sm btn-primary"
        :disabled="!dirty || save.isPending.value"
        data-testid="permission-save"
        @click="save.mutate()"
      >
        <Save aria-hidden="true" class="size-4" />
        {{ t('permissions.save') }}
      </button>
      <button
        type="button"
        class="btn btn-sm"
        :disabled="!dirty || save.isPending.value"
        data-testid="permission-discard"
        @click="discard"
      >
        <Undo2 aria-hidden="true" class="size-4" />
        {{ t('permissions.discard') }}
      </button>
      <span v-if="dirty" class="badge badge-sm font-mono" data-testid="permission-dirty">
        {{ t('permissions.unsaved', { count: changes.length }) }}
      </span>
    </div>
  </section>
</template>

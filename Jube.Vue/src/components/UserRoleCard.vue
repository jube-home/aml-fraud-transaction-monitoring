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
import { ChevronRight } from 'lucide-vue-next'
import { queryKeys } from '../api/queryKeys'
import { formModelApi } from '../formModel/api'

const props = defineProps<{ roleGuid: string | undefined; canOpen: boolean }>()

const emit = defineEmits<{ open: [roleId: number] }>()

const { t } = useI18n()

const model = useQuery({
  queryKey: computed(() => queryKeys.formModel('RoleRegistry')),
  queryFn: () => formModelApi.model('RoleRegistry'),
  staleTime: 30_000,
  retry: false,
})

const roles = useQuery({
  queryKey: computed(() => queryKeys.formRecords('RoleRegistry', undefined)),
  queryFn: () => formModelApi.records(model.data.value!, undefined),
  enabled: computed(() => model.data.value !== undefined),
  retry: false,
})

const role = computed(() => (roles.data.value ?? []).find((row) => row.guid === props.roleGuid))
</script>

<template>
  <div class="card card-border border-base-300 bg-base-100" data-testid="hub-role">
    <div class="card-body gap-3 p-4">
      <div class="flex items-start justify-between gap-2">
        <h4 class="card-title text-base">{{ t('security.roleCard.title') }}</h4>
        <span
          v-if="roles.isPending.value"
          key="pending"
          class="skeleton h-6 w-24"
          role="status"
          :aria-label="t('crud.loading')"
        ></span>
        <span
          v-else-if="role"
          key="name"
          class="badge badge-neutral font-mono"
          data-testid="hub-role-name"
          >{{ role.name }}</span
        >
        <span v-else key="none" class="badge badge-sm font-mono">{{
          t('security.roleCard.none')
        }}</span>
      </div>
      <p class="min-h-5 text-sm text-secondary">{{ t('security.roleCard.hint') }}</p>
      <div v-if="role && canOpen" key="actions" class="card-actions justify-end">
        <button
          type="button"
          class="btn btn-sm"
          data-testid="hub-open-role"
          @click="emit('open', role.id as number)"
        >
          {{ t('hierarchy.open') }}
          <ChevronRight aria-hidden="true" class="size-3.5" />
        </button>
      </div>
    </div>
  </div>
</template>

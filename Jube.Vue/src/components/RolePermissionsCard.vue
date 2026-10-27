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
import { permissionApi } from '../permissions/api'

const props = defineProps<{ roleRegistryId: number }>()

const emit = defineEmits<{ open: [] }>()

const { t } = useI18n()

const grants = useQuery({
  queryKey: computed(() => queryKeys.permissionGrants(props.roleRegistryId)),
  queryFn: () => permissionApi.grants(props.roleRegistryId),
  retry: false,
})

const granted = computed(() => (grants.data.value ?? []).filter((grant) => grant.active).length)
</script>

<template>
  <div class="card card-border border-base-300 bg-base-100" data-testid="hub-permissions">
    <div class="card-body gap-3 p-4">
      <div class="flex items-start justify-between gap-2">
        <h4 class="card-title text-base">{{ t('security.permissions.title') }}</h4>
        <span
          v-if="grants.isPending.value"
          key="pending"
          class="skeleton h-6 w-10"
          role="status"
          :aria-label="t('crud.loading')"
        ></span>
        <span
          v-else-if="grants.isSuccess.value"
          key="total"
          class="font-mono text-2xl font-semibold tabular-nums"
          data-testid="hub-total-permissions"
          >{{ granted }}</span
        >
      </div>
      <p class="min-h-5 text-sm text-secondary">{{ t('security.permissionsCard.hint') }}</p>
      <div class="card-actions justify-end">
        <button
          type="button"
          class="btn btn-sm"
          data-testid="hub-open-permissions"
          @click="emit('open')"
        >
          {{ t('hierarchy.open') }}
          <ChevronRight aria-hidden="true" class="size-3.5" />
        </button>
      </div>
    </div>
  </div>
</template>

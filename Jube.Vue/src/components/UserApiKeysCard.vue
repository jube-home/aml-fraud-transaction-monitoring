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
import { ChevronRight, KeyRound } from 'lucide-vue-next'
import { queryKeys } from '../api/queryKeys'
import { apiKeyApi } from '../apiKeys/api'

const props = defineProps<{ userRegistryId: number }>()

const emit = defineEmits<{ open: [] }>()

const { t } = useI18n()

const keys = useQuery({
  queryKey: computed(() => queryKeys.apiKeys(props.userRegistryId)),
  queryFn: () => apiKeyApi.list(props.userRegistryId),
  retry: false,
})
</script>

<template>
  <div class="card card-border border-base-300 bg-base-100" data-testid="hub-apiKeys">
    <div class="card-body gap-3 p-4">
      <div class="flex items-start justify-between gap-2">
        <h4 class="card-title flex items-center gap-2 text-base">
          <KeyRound class="size-4 text-secondary" aria-hidden="true" />
          {{ t('security.apiKeys.title') }}
        </h4>
        <span
          v-if="keys.isPending.value"
          key="pending"
          class="skeleton h-6 w-10"
          role="status"
          :aria-label="t('crud.loading')"
        ></span>
        <span
          v-else-if="keys.isSuccess.value"
          key="total"
          class="font-mono text-2xl font-semibold tabular-nums"
          data-testid="hub-total-apiKeys"
          >{{ keys.data.value?.length ?? 0 }}</span
        >
        <span v-else key="unavailable" class="badge badge-sm font-mono">{{
          t('security.apiKeys.unavailable')
        }}</span>
      </div>
      <p class="min-h-5 text-sm text-secondary">{{ t('security.apiKeys.cardHint') }}</p>
      <div class="card-actions justify-end">
        <button
          type="button"
          class="btn btn-sm"
          data-testid="hub-open-apiKeys"
          @click="emit('open')"
        >
          {{ t('hierarchy.open') }}
          <ChevronRight aria-hidden="true" class="size-3.5" />
        </button>
      </div>
    </div>
  </div>
</template>

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
import type { AreaRoot } from '../areaStory/types'
import { formModelApi } from '../formModel/api'

const props = defineProps<{ root: AreaRoot }>()

const emit = defineEmits<{ open: [] }>()

const { t } = useI18n()

const model = useQuery({
  queryKey: computed(() => queryKeys.formModel(props.root.area ?? '')),
  queryFn: () => formModelApi.model(props.root.area!),
  enabled: computed(() => props.root.area !== undefined),
  staleTime: 30_000,
  retry: false,
})

const rows = useQuery({
  queryKey: computed(() => queryKeys.formRecords(props.root.area ?? '', undefined)),
  queryFn: () => formModelApi.records(model.data.value!, undefined),
  enabled: computed(() => model.data.value !== undefined),
  retry: false,
})
</script>

<template>
  <div class="card card-border border-base-300 bg-base-100" :data-testid="`root-${root.id}`">
    <div class="card-body gap-3 p-4">
      <div class="flex items-start justify-between gap-2">
        <h3 class="card-title text-base">{{ root.title }}</h3>
        <span
          v-if="root.area && rows.isPending.value"
          key="pending"
          class="skeleton h-6 w-10"
          role="status"
          :aria-label="t('crud.loading')"
        ></span>
        <span
          v-else-if="root.area && rows.isSuccess.value"
          key="total"
          class="font-mono text-2xl font-semibold tabular-nums"
          :data-testid="`root-total-${root.id}`"
          >{{ rows.data.value?.length ?? 0 }}</span
        >
      </div>
      <p class="min-h-5 text-sm text-secondary">{{ root.hint }}</p>
      <div class="card-actions justify-end">
        <button
          type="button"
          class="btn btn-sm"
          :data-testid="`root-open-${root.id}`"
          @click="emit('open')"
        >
          {{ t('hierarchy.open') }}
          <ChevronRight aria-hidden="true" class="size-3.5" />
        </button>
      </div>
    </div>
  </div>
</template>

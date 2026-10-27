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
import { ApiError, useApi } from '../api/useApi'
import { queryKeys } from '../api/queryKeys'

const { t } = useI18n()

const query = useQuery({
  queryKey: queryKeys.antiforgery(),
  queryFn: () => useApi<{ token: string }>('/api/AntiforgeryToken'),
})

const errorText = computed(() => {
  const error = query.error.value

  if (!(error instanceof ApiError)) return t('api.server')

  switch (error.kind) {
    case 'unauthorized':
      return t('api.unauthorized')
    case 'forbidden':
      return t('api.forbidden')
    case 'notFound':
      return t('api.notFound')
    case 'conflict':
      return t('api.conflict')
    case 'validation':
      return t('api.validation')
    case 'network':
      return t('api.network')
    default:
      return t('api.server')
  }
})
</script>

<template>
  <section class="flex flex-col gap-4">
    <h1 class="text-2xl font-semibold text-primary">{{ t('api.title') }}</h1>
    <p>{{ t('api.intro') }}</p>

    <div v-if="query.isPending.value" role="status" class="flex items-center gap-2">
      <span class="loading loading-spinner loading-sm" aria-hidden="true"></span>
      {{ t('api.loading') }}
    </div>

    <div v-else-if="query.isError.value" role="alert" class="alert alert-error alert-soft">
      <span data-testid="api-error">{{ errorText }}</span>
      <button type="button" class="btn btn-sm" @click="query.refetch()">
        {{ t('api.retry') }}
      </button>
    </div>

    <div v-else role="status" class="alert alert-success">
      <span data-testid="api-success">
        {{ t('api.success', { length: query.data.value?.token.length ?? 0 }) }}
      </span>
      <button type="button" class="btn btn-sm" @click="query.refetch()">
        {{ t('api.refresh') }}
      </button>
    </div>
  </section>
</template>

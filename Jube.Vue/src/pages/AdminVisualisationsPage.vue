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
import { ShieldAlert } from 'lucide-vue-next'
import { hasPermission, meQueryOptions } from '../auth/me'
import type { AreaRoot } from '../areaStory/types'
import AreaStory from '../components/AreaStory.vue'
import { useStoryPath } from '../composables/useStoryPath'

const { t } = useI18n()
const me = useQuery(meQueryOptions)
const { start, generation, remember } = useStoryPath()

const allowed = computed(() => hasPermission(me.data.value, 31, 32, 33))

const roots = computed<AreaRoot[]>(() => [
  {
    id: 'adminVisualisations',
    area: 'VisualisationRegistry',
    title: t('adminVisualisations.title'),
    singular: t('adminVisualisations.singular'),
    hint: t('adminVisualisations.hint'),
  },
])
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="adminVisualisations-page">
    <h1 class="sr-only">{{ t('nav.adminVisualisations') }}</h1>

    <div
      v-if="me.isPending.value"
      key="loading"
      class="flex flex-col gap-3"
      role="status"
      data-testid="adminVisualisations-loading"
    >
      <span class="sr-only">{{ t('adminVisualisations.loading') }}</span>
      <div class="skeleton h-10 w-full"></div>
      <div class="skeleton h-48 w-full"></div>
    </div>

    <div
      v-else-if="me.isError.value || !allowed"
      key="denied"
      role="alert"
      class="alert alert-error"
      data-testid="adminVisualisations-denied"
    >
      <ShieldAlert class="size-5" aria-hidden="true" />
      <span>{{ t('adminVisualisations.denied') }}</span>
    </div>

    <AreaStory
      v-else
      :key="generation"
      :path="start"
      :title="t('nav.adminVisualisations')"
      :roots="roots"
      :views="{}"
      solo
      @path="remember"
    />
  </section>
</template>

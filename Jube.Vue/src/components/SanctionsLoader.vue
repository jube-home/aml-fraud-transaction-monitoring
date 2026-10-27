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
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { describeApiError } from '../api/problems'
import { queryKeys } from '../api/queryKeys'
import { sanctionsApi } from '../sanctions/api'
import FileUpload from './FileUpload.vue'
import PanelCard from './PanelCard.vue'
import ProblemAlert from './ProblemAlert.vue'

withDefaults(defineProps<{ allowed?: boolean }>(), { allowed: true })

const { t } = useI18n()
const id = useId()

const sourceId = ref('')
const problems = ref<string[]>([])
const loaded = ref('')

const sources = useQuery({
  queryKey: queryKeys.sanctionSources(),
  queryFn: sanctionsApi.sources,
  retry: false,
})

const list = computed(() => sources.data.value ?? [])

watch(
  list,
  (value) => {
    if (sourceId.value === '' && value.length > 0) sourceId.value = String(value[0]!.id)
  },
  { immediate: true },
)

const upload = (file: File) => sanctionsApi.importFile(Number(sourceId.value), file)

function uploaded(filename: string): void {
  problems.value = []
  loaded.value = filename
}

function failed(error: unknown): void {
  loaded.value = ''
  problems.value = describeApiError(error, {
    failed: t('sanctionsLoader.failed'),
    refused: t('sanctionsLoader.refused'),
  })
}
</script>

<template>
  <PanelCard :title="t('sanctionsLoader.title')" compact>
    <div class="flex min-w-0 flex-col gap-4" data-testid="sanctions-loader">
      <p class="text-sm text-secondary">{{ t('sanctionsLoader.hint') }}</p>

      <p
        v-if="!allowed"
        key="refused"
        class="text-sm"
        role="status"
        data-testid="sanctions-refused"
      >
        {{ t('sanctionsLoader.refused') }}
      </p>

      <template v-else>
        <label class="flex flex-col gap-1" :for="`${id}-source`">
          <span class="label text-xs">{{ t('sanctionsLoader.source') }}</span>
          <select
            :id="`${id}-source`"
            v-model="sourceId"
            class="select select-sm w-full max-w-sm"
            :disabled="sources.isPending.value"
            data-testid="sanctions-source"
          >
            <option v-for="source in list" :key="source.id" :value="String(source.id)">
              {{ source.name }}
            </option>
          </select>
        </label>

        <FileUpload
          :label="t('sanctionsLoader.file')"
          :extensions="['.csv']"
          :upload="upload"
          :disabled="sourceId === ''"
          @uploaded="uploaded"
          @failed="failed"
        />

        <ProblemAlert
          v-if="problems.length > 0"
          key="problems"
          :reasons="problems"
          data-testid="sanctions-problems"
        />
        <p
          v-else-if="loaded"
          key="loaded"
          class="text-sm"
          role="status"
          data-testid="sanctions-loaded"
        >
          {{ t('sanctionsLoader.loaded', { name: loaded }) }}
        </p>
      </template>
    </div>
  </PanelCard>
</template>

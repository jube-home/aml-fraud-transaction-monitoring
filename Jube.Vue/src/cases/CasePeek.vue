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
import { ExternalLink, X } from 'lucide-vue-next'
import CaseDiaryPanel from './CaseDiaryPanel.vue'
import CaseSummaryBody from './CaseSummaryBody.vue'
import { useFormat } from '../composables/useFormat'
import { casesApi } from './api'
import { caseLabel } from './labels'

const props = defineProps<{ caseId: number }>()

const emit = defineEmits<{ close: []; open: [caseId: number] }>()

const { t } = useI18n()
const { number, dateTime } = useFormat()

const summary = useQuery({
  queryKey: computed(() => ['service', 'Case', 'summary', props.caseId] as const),
  queryFn: () => casesApi.summary(props.caseId),
  staleTime: 15_000,
})

const data = computed(() => summary.data.value)
</script>

<template>
  <aside
    class="fixed inset-y-0 end-0 z-30 flex w-full max-w-md flex-col gap-3 overflow-y-auto border-s border-base-300 bg-base-100 p-4 shadow-lg 2xl:static 2xl:z-auto 2xl:max-w-sm 2xl:shrink-0 2xl:shadow-none"
    :aria-label="t('cases.peek.label')"
    data-testid="cases-peek"
  >
    <div class="flex items-center justify-between gap-2">
      <h2 class="font-mono text-base font-semibold">{{ caseLabel(caseId) }}</h2>
      <div class="flex items-center gap-1">
        <button
          type="button"
          class="btn btn-primary btn-sm"
          data-testid="cases-peek-open"
          @click="emit('open', caseId)"
        >
          <ExternalLink class="size-4" aria-hidden="true" />
          {{ t('cases.action.open') }}
        </button>
        <button
          type="button"
          class="btn btn-ghost btn-sm btn-square"
          :aria-label="t('cases.peek.close')"
          data-testid="cases-peek-close"
          @click="emit('close')"
        >
          <X class="size-4" aria-hidden="true" />
        </button>
      </div>
    </div>

    <div
      v-if="summary.isPending.value"
      key="loading"
      class="flex flex-col gap-2"
      role="status"
      data-testid="cases-peek-loading"
    >
      <span class="sr-only">{{ t('cases.peek.loading') }}</span>
      <div class="skeleton h-6 w-40"></div>
      <div class="skeleton h-24 w-full"></div>
      <div class="skeleton h-24 w-full"></div>
    </div>

    <div v-else-if="summary.isError.value" key="error" role="alert" class="alert alert-error">
      <span>{{ t('cases.peek.failed') }}</span>
    </div>

    <template v-else-if="data">
      <CaseSummaryBody :case-id="caseId" :data="data" />

      <CaseDiaryPanel :row="data.case" />

      <section class="flex flex-col gap-1" data-testid="cases-peek-key">
        <h3 class="text-sm font-semibold">{{ t('cases.peek.keyHistory') }}</h3>
        <p class="font-mono text-xs">
          {{
            t('cases.peek.keySummary', {
              closed: number(data.keyHistory.closed),
              confirmed: number(data.keyHistory.confirmed),
              open: number(data.keyHistory.otherOpenCases),
            })
          }}
        </p>
      </section>

      <section v-if="data.notes.length > 0" key="notes" class="flex flex-col gap-1">
        <h3 class="text-sm font-semibold">{{ t('cases.peek.notes') }}</h3>
        <ul class="list">
          <li v-for="(note, index) in data.notes" :key="index" class="list-row px-0 py-1">
            <div class="list-col-grow">
              <div class="text-sm">{{ note.note }}</div>
              <div class="font-mono text-xs text-base-content/70">
                {{ [note.user, note.date ? dateTime(note.date) : ''].filter((x) => x).join(' · ') }}
              </div>
            </div>
          </li>
        </ul>
      </section>
    </template>
  </aside>
</template>

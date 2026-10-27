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
import { useFormat } from '../../composables/useFormat'
import TemplateForm from '../../datasource/TemplateForm.vue'
import { operationsApi } from './operationsApi'

const props = defineProps<{ caseId: number }>()

const { t } = useI18n()
const format = useFormat()

const entries = useQuery({
  queryKey: computed(() => ['service', 'Case', 'formEntries', props.caseId] as const),
  queryFn: () => operationsApi.formEntries(props.caseId),
  staleTime: 15_000,
  retry: false,
})

const list = computed(() => entries.data.value ?? [])
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="case-forms">
    <div v-if="entries.isPending.value" key="loading" class="skeleton h-32 w-full"></div>
    <div v-else-if="entries.isError.value" key="failed" role="alert" class="alert alert-error">
      <span>{{ t('caseDetail.forms.entriesFailed') }}</span>
    </div>
    <p
      v-else-if="list.length === 0"
      key="none"
      class="text-sm text-base-content/70"
      data-testid="case-forms-empty"
    >
      {{ t('caseDetail.forms.noEntries') }}
    </p>
    <template v-else>
      <article
        v-for="entry in list"
        :key="entry.id"
        class="flex min-w-0 flex-col gap-3 rounded-box border border-base-300 bg-base-100 p-4"
        :data-testid="`case-form-entry-${entry.id}`"
      >
        <header class="flex flex-wrap items-baseline justify-between gap-2">
          <h4 class="text-sm font-semibold">{{ entry.formName }}</h4>
          <span class="font-mono text-xs text-base-content/70">
            {{
              t('caseDetail.forms.submittedBy', {
                user: entry.createdUser,
                when: entry.createdDate ? format.dateTime(entry.createdDate) : '',
              })
            }}
          </span>
        </header>
        <div
          v-if="entry.error"
          key="error"
          role="alert"
          class="alert alert-warning text-sm"
          data-testid="case-form-entry-error"
        >
          <span>{{ t('caseDetail.forms.cannotDraw', { reason: entry.error }) }}</span>
        </div>
        <TemplateForm v-else :html="entry.html" :label="entry.formName" />
        <section
          v-if="entry.otherValues.length > 0"
          key="other"
          class="flex flex-col gap-1"
          data-testid="case-form-entry-other"
        >
          <h5 class="text-xs font-semibold text-base-content/70">
            {{ t('caseDetail.forms.otherValues') }}
          </h5>
          <dl class="grid grid-cols-[max-content_1fr] gap-x-3 gap-y-1 text-sm">
            <template v-for="value in entry.otherValues" :key="value.name">
              <dt class="font-mono text-xs text-base-content/70">{{ value.name }}</dt>
              <dd class="min-w-0 wrap-break-word">{{ value.value }}</dd>
            </template>
          </dl>
        </section>
      </article>
    </template>
  </div>
</template>

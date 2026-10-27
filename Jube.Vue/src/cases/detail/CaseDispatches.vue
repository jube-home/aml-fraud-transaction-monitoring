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
import { channelText, kindText, modeText, outcomeText, prettyRows } from './dispatchText'
import HoldLegs from './HoldLegs.vue'
import { operationsApi } from './operationsApi'

const props = defineProps<{ caseId: number }>()

const { t } = useI18n()
const format = useFormat()

const dispatches = useQuery({
  queryKey: computed(() => ['service', 'Case', 'dispatches', props.caseId] as const),
  queryFn: () => operationsApi.dispatches(props.caseId),
  staleTime: 15_000,
  retry: false,
})

const list = computed(() => dispatches.data.value ?? [])

const holds = useQuery({
  queryKey: computed(() => ['service', 'Case', 'holds', props.caseId] as const),
  queryFn: () => operationsApi.holds(props.caseId),
  staleTime: 15_000,
  retry: false,
})

const holdMessages = computed(() => (holds.data.value ?? []).filter((hold) => hold.legs.length > 0))
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="case-messages">
    <div v-if="dispatches.isPending.value" key="loading" class="skeleton h-32 w-full"></div>
    <div v-else-if="dispatches.isError.value" key="failed" role="alert" class="alert alert-error">
      <span>{{ t('caseDetail.messages.failed') }}</span>
    </div>
    <p
      v-else-if="list.length === 0 && holdMessages.length === 0"
      key="none"
      class="text-sm text-base-content/70"
      data-testid="case-messages-empty"
    >
      {{ t('caseDetail.messages.none') }}
    </p>
    <section
      v-if="holdMessages.length > 0"
      key="holds"
      class="flex min-w-0 flex-col gap-2"
      data-testid="case-hold-messages"
    >
      <h4 class="text-sm font-semibold">{{ t('caseDetail.hold.messages') }}</h4>
      <div
        v-for="hold in holdMessages"
        :key="hold.entityAnalysisModelInstanceEntryGuid"
        class="flex min-w-0 flex-col gap-2 rounded-box border border-base-300 bg-base-100 p-4"
      >
        <span class="text-sm font-medium">{{ hold.rangeName }} {{ hold.entryKeyValue }}</span>
        <HoldLegs
          :legs="hold.legs"
          :testid="`case-hold-message-legs-${hold.entityAnalysisModelInstanceEntryGuid}`"
        />
      </div>
    </section>
    <ol
      v-if="!dispatches.isPending.value && !dispatches.isError.value && list.length > 0"
      key="list"
      class="flex min-w-0 flex-col gap-3"
      role="list"
    >
      <li
        v-for="item in list"
        :key="item.id"
        class="flex min-w-0 flex-col gap-2 rounded-box border border-base-300 bg-base-100 p-4"
        :data-testid="`case-message-${item.id}`"
      >
        <header class="flex flex-wrap items-center gap-2">
          <span
            class="badge badge-sm"
            :class="item.outcomeId === 1 ? 'badge-success' : 'badge-error'"
            :data-testid="`case-message-outcome-${item.id}`"
            >{{ outcomeText(t, item.outcomeId) }}</span
          >
          <span class="badge badge-sm badge-ghost">{{ channelText(t, item.channelId) }}</span>
          <h4 class="min-w-0 text-sm font-semibold">
            {{
              t('caseDetail.messages.from', {
                kind: kindText(t, item.sourceKindId),
                name: item.sourceName,
              })
            }}
          </h4>
          <span class="ms-auto font-mono text-xs text-base-content/70">
            {{
              t('caseDetail.messages.sentBy', {
                user: item.createdUser,
                when: item.createdDate ? format.dateTime(item.createdDate) : '',
              })
            }}
          </span>
        </header>
        <dl class="grid grid-cols-[max-content_1fr] gap-x-3 gap-y-1 text-sm">
          <template v-if="item.destination">
            <dt class="text-xs text-base-content/70">{{ t('caseDetail.messages.to') }}</dt>
            <dd class="min-w-0 wrap-break-word font-mono text-xs">{{ item.destination }}</dd>
          </template>
          <template v-if="item.subject">
            <dt class="text-xs text-base-content/70">{{ t('caseDetail.messages.subject') }}</dt>
            <dd class="min-w-0 wrap-break-word">{{ item.subject }}</dd>
          </template>
          <template v-if="item.responseStatus">
            <dt class="text-xs text-base-content/70">{{ t('caseDetail.messages.answer') }}</dt>
            <dd class="font-mono text-xs">{{ item.responseStatus }}</dd>
          </template>
          <dt class="text-xs text-base-content/70">{{ t('caseDetail.messages.drawn') }}</dt>
          <dd class="text-xs">{{ modeText(t, item.modeId) }}</dd>
        </dl>
        <p
          v-if="item.detail"
          key="detail"
          class="text-sm wrap-break-word text-error"
          :data-testid="`case-message-detail-${item.id}`"
        >
          {{ item.detail }}
        </p>
        <details v-if="item.body" key="body" class="text-sm">
          <summary class="cursor-pointer text-xs">{{ t('caseDetail.messages.body') }}</summary>
          <pre
            class="mt-1 max-h-64 overflow-auto rounded-field bg-base-200 p-2 font-mono text-xs whitespace-pre-wrap"
            >{{ item.body }}</pre>
        </details>
        <details v-if="item.rows" key="rows" class="text-sm">
          <summary class="cursor-pointer text-xs">{{ t('caseDetail.messages.rows') }}</summary>
          <pre
            class="mt-1 max-h-64 overflow-auto rounded-field bg-base-200 p-2 font-mono text-xs"
            >{{ prettyRows(item.rows) }}</pre>
        </details>
      </li>
    </ol>
  </div>
</template>

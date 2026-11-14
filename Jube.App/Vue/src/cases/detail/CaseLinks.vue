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
import {computed, ref} from 'vue'
import {useI18n} from 'vue-i18n'
import {useMutation, useQuery, useQueryClient} from '@tanstack/vue-query'
import {Link2, X} from 'lucide-vue-next'
import {RouterLink} from 'vue-router'
import {ApiError} from '../../api/useApi'
import FieldHelp from '../../components/FieldHelp.vue'
import {useToasts} from '../../stores/toasts'
import {caseLabel} from '../labels'
import {linksApi, linksKey, maximumReason, parseCaseNumber} from './linksApi'

const props = defineProps<{ caseId: number }>()

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()

const links = useQuery({
  queryKey: computed(() => linksKey(props.caseId)),
  queryFn: () => linksApi.list(props.caseId),
  staleTime: 15_000,
  retry: false,
})

const number = ref('')
const reason = ref('')
const target = computed(() => parseCaseNumber(number.value))
const ready = computed(() => target.value !== null && target.value !== props.caseId)

function refresh(): void {
  void client.invalidateQueries({queryKey: ['service', 'CaseLink']})
}

const link = useMutation({
  mutationFn: () =>
    linksApi.link(props.caseId, target.value as number, reason.value.trim() || null),
  onSuccess: () => {
    number.value = ''
    reason.value = ''
    toasts.success(t('caseDetail.links.linked'))
    refresh()
  },
  onError: (error) => {
    if (error instanceof ApiError && error.kind === 'conflict')
      toasts.error(t('caseDetail.links.already'))
    else if (error instanceof ApiError && error.kind === 'notFound')
      toasts.error(t('caseDetail.links.notFound'))
    else toasts.apiError(error)
  },
})

const unlink = useMutation({
  mutationFn: (otherCaseId: number) => linksApi.unlink(props.caseId, otherCaseId),
  onSuccess: () => {
    toasts.success(t('caseDetail.links.unlinked'))
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})
</script>

<template>
  <fieldset class="fieldset" data-testid="case-links">
    <legend class="fieldset-legend">
      <Link2 class="size-3.5" aria-hidden="true"/>
      {{ t('caseDetail.links.title') }}
      <FieldHelp :label="t('caseDetail.links.title')" :text="t('caseDetail.links.hint')"/>
    </legend>

    <div v-if="links.isPending.value" key="loading" class="skeleton h-6 w-2/3" role="status"></div>
    <p v-else-if="links.isError.value" key="failed" role="alert" class="text-sm">
      {{ t('caseDetail.links.failed') }}
    </p>
    <template v-else>
      <p
        v-if="(links.data.value ?? []).length === 0"
        key="empty"
        class="text-sm text-base-content/70"
        data-testid="case-links-empty"
      >
        {{ t('caseDetail.links.empty') }}
      </p>
      <ul v-else class="flex flex-col gap-1" data-testid="case-links-list">
        <li
          v-for="entry in links.data.value"
          :key="entry.otherCaseId"
          class="flex items-center gap-2 text-sm"
          :data-testid="`case-link-${entry.otherCaseId}`"
        >
          <RouterLink class="link" :to="`/cases/${entry.otherCaseId}`">{{
              caseLabel(entry.otherCaseId)
            }}
          </RouterLink>
          <span class="min-w-0 grow truncate" :title="entry.reason ?? ''">
            {{ entry.caseKeyValue ?? '' }}
            <span v-if="entry.reason" class="text-base-content/70">- {{ entry.reason }}</span>
          </span>
          <span v-if="entry.closedStatusId === 3" class="badge badge-ghost badge-sm">{{
              t('caseDetail.links.closed')
            }}</span>
          <button
            type="button"
            class="btn btn-ghost btn-xs btn-circle"
            :aria-label="t('caseDetail.links.unlink', { id: caseLabel(entry.otherCaseId) })"
            :disabled="unlink.isPending.value"
            :data-testid="`case-link-remove-${entry.otherCaseId}`"
            @click="unlink.mutate(entry.otherCaseId)"
          >
            <X class="size-3" aria-hidden="true"/>
          </button>
        </li>
      </ul>

      <div class="flex flex-col gap-1 pt-2">
        <div class="join w-full">
          <input
            v-model="number"
            type="text"
            inputmode="numeric"
            class="input input-sm join-item w-28"
            :placeholder="t('caseDetail.links.number')"
            :aria-label="t('caseDetail.links.number')"
            data-testid="case-links-number"
          />
          <input
            v-model="reason"
            type="text"
            class="input input-sm join-item grow"
            :maxlength="maximumReason"
            :placeholder="t('caseDetail.links.reason')"
            :aria-label="t('caseDetail.links.reason')"
            data-testid="case-links-reason"
          />
          <button
            type="button"
            class="btn btn-sm join-item"
            :disabled="!ready || link.isPending.value"
            data-testid="case-links-add"
            @click="link.mutate()"
          >
            {{ t('caseDetail.links.link') }}
          </button>
        </div>
      </div>
    </template>
  </fieldset>
</template>

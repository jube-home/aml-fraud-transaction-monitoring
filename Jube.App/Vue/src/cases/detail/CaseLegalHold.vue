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
import {Scale} from 'lucide-vue-next'
import DecideCard from './DecideCard.vue'
import {meQueryOptions} from '../../auth/me'
import {useToasts} from '../../stores/toasts'
import {
  holdReady,
  legalHoldApi,
  legalHoldKey,
  legalHoldPermission,
  maximumHoldReason,
} from './legalHoldApi'

const props = defineProps<{ caseId: number }>()

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()
const me = useQuery(meQueryOptions)

const allowed = computed(
  () =>
    Array.isArray(me.data.value?.permissions) &&
    me.data.value.permissions.includes(legalHoldPermission),
)

const state = useQuery({
  queryKey: computed(() => legalHoldKey(props.caseId)),
  queryFn: () => legalHoldApi.get(props.caseId),
  enabled: allowed,
  staleTime: 15_000,
  retry: false,
})

const reason = ref('')
const holding = computed(() => state.data.value?.hold === true)

const change = useMutation({
  mutationFn: (hold: boolean) =>
    legalHoldApi.set(props.caseId, hold, hold ? reason.value.trim() : null),
  onSuccess: (_result, hold) => {
    reason.value = ''
    toasts.success(t(hold ? 'caseDetail.legalHold.placed' : 'caseDetail.legalHold.lifted'))
    void client.invalidateQueries({queryKey: legalHoldKey(props.caseId)})
  },
  onError: (error) => toasts.apiError(error),
})
</script>

<template>
  <DecideCard v-if="allowed && state.data.value"
    :title="t('caseDetail.legalHold.title')"
    :help="t('caseDetail.legalHold.hint')"
    testid="case-legal-hold"
  >
    <template #icon>
      <Scale class="size-5"/>
    </template>

    <p v-if="holding" class="text-sm" data-testid="case-legal-hold-state">
      <span class="badge badge-warning badge-sm">{{ t('caseDetail.legalHold.held') }}</span>
      {{ state.data.value.reason }}
      <span class="block text-base-content/70">{{
          t('caseDetail.legalHold.by', {user: state.data.value.user ?? ''})
        }}</span>
    </p>
    <p v-else class="text-base-content/70 text-sm" data-testid="case-legal-hold-state">
      {{ t('caseDetail.legalHold.notHeld') }}
    </p>

    <div v-if="!holding" class="join w-full">
      <input
        v-model="reason"
        type="text"
        class="input input-sm join-item grow"
        :maxlength="maximumHoldReason"
        :placeholder="t('caseDetail.legalHold.reason')"
        :aria-label="t('caseDetail.legalHold.reason')"
        data-testid="case-legal-hold-reason"
      />
      <button
        type="button"
        class="btn btn-sm join-item"
        :disabled="!holdReady(true, reason) || change.isPending.value"
        data-testid="case-legal-hold-place"
        @click="change.mutate(true)"
      >
        {{ t('caseDetail.legalHold.place') }}
      </button>
    </div>
    <button
      v-else
      type="button"
      class="btn btn-sm"
      :disabled="change.isPending.value"
      data-testid="case-legal-hold-lift"
      @click="change.mutate(false)"
    >
      {{ t('caseDetail.legalHold.lift') }}
    </button>
  </DecideCard>
</template>

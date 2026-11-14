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
import {ShieldAlert} from 'lucide-vue-next'
import FieldHelp from '../components/FieldHelp.vue'
import {meQueryOptions} from '../auth/me'
import {useToasts} from '../stores/toasts'
import {
  canDecide,
  canRetract,
  entityRiskApi,
  entityRiskKey,
  entityRiskPermission,
  maximumKeyName,
  maximumKeyValue,
  maximumReason,
  proposalReady,
  type RatingHistory,
} from './entityRiskApi'

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()
const me = useQuery(meQueryOptions)

const held = computed(() =>
  Array.isArray(me.data.value?.permissions) ? me.data.value.permissions : [],
)
const user = computed(() => me.data.value?.userName ?? null)
const canWrite = computed(() => held.value.includes(entityRiskPermission.write))
const canApprove = computed(() => held.value.includes(entityRiskPermission.approve))

const keyName = ref('')
const keyValue = ref('')
const tier = ref(2)
const score = ref<number | null>(null)
const review = ref('')
const reason = ref('')
const notes = ref<Record<string, string>>({})
const found = ref<RatingHistory | null>(null)

const requests = useQuery({
  queryKey: [...entityRiskKey, 'requests'],
  queryFn: () => entityRiskApi.requests(),
  retry: false,
})

const due = useQuery({
  queryKey: [...entityRiskKey, 'due'],
  queryFn: () => entityRiskApi.due(),
  retry: false,
})

const ready = computed(() => proposalReady(keyName.value, keyValue.value, tier.value, reason.value))

function refresh(): void {
  void client.invalidateQueries({queryKey: entityRiskKey})
}

const lookup = useMutation({
  mutationFn: () => entityRiskApi.byKey(keyName.value.trim(), keyValue.value),
  onSuccess: (result) => {
    found.value = result
  },
  onError: (error) => toasts.apiError(error),
})

const propose = useMutation({
  mutationFn: () =>
    entityRiskApi.propose({
      keyName: keyName.value.trim(),
      keyValue: keyValue.value,
      tierId: tier.value,
      score: score.value,
      reviewByDate: review.value ? `${review.value}T00:00:00Z` : null,
      reason: reason.value.trim(),
    }),
  onSuccess: () => {
    reason.value = ''
    toasts.success(t('entityRisk.proposed'))
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})

const decide = useMutation({
  mutationFn: (work: () => Promise<unknown>) => work(),
  onSuccess: () => {
    toasts.success(t('entityRisk.recorded'))
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})

function approve(guid: string): void {
  decide.mutate(() => entityRiskApi.approve(guid, notes.value[guid]?.trim() || null))
}

function reject(guid: string): void {
  decide.mutate(() => entityRiskApi.reject(guid, (notes.value[guid] ?? '').trim()))
}

function retract(guid: string): void {
  decide.mutate(() => entityRiskApi.retract(guid))
}
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="adminEntityRisk-page">
    <h1 class="flex items-center gap-2 text-lg font-semibold">
      <ShieldAlert class="size-4" aria-hidden="true"/>
      {{ t('entityRisk.title') }}
      <FieldHelp :label="t('entityRisk.title')" :text="t('entityRisk.hint')"/>
    </h1>

    <fieldset class="fieldset" data-testid="entity-risk-form">
      <div class="flex flex-wrap gap-2">
        <input
          v-model="keyName"
          type="text"
          class="input input-sm"
          :maxlength="maximumKeyName"
          :placeholder="t('entityRisk.keyName')"
          :aria-label="t('entityRisk.keyName')"
          data-testid="entity-risk-key-name"
        />
        <input
          v-model="keyValue"
          type="text"
          class="input input-sm"
          :maxlength="maximumKeyValue"
          :placeholder="t('entityRisk.keyValue')"
          :aria-label="t('entityRisk.keyValue')"
          data-testid="entity-risk-key-value"
        />
        <button
          type="button"
          class="btn btn-sm"
          :disabled="!keyName.trim() || !keyValue || lookup.isPending.value"
          data-testid="entity-risk-lookup"
          @click="lookup.mutate()"
        >
          {{ t('entityRisk.lookup') }}
        </button>
      </div>

      <p v-if="found" class="text-sm" data-testid="entity-risk-current">
        <template v-if="found.current">
          <span class="badge badge-sm">{{ t(`entityRisk.tiers.${found.current.tierId}`) }}</span>
          <span>{{ t(`entityRisk.sources.${found.current.sourceId}`) }}</span>
          <span>{{ found.current.createdUser }}</span>
          <span v-if="found.current.reason" class="text-base-content/70">{{
              found.current.reason
            }}</span>
        </template>
        <template v-else>{{ t('entityRisk.unrated') }}</template>
      </p>

      <template v-if="canWrite">
        <div class="flex flex-wrap gap-2">
          <select
            v-model.number="tier"
            class="select select-sm"
            :aria-label="t('entityRisk.tier')"
            data-testid="entity-risk-tier"
          >
            <option v-for="id in [1, 2, 3]" :key="id" :value="id">
              {{ t(`entityRisk.tiers.${id}`) }}
            </option>
          </select>
          <input
            v-model.number="score"
            type="number"
            step="any"
            class="input input-sm w-28"
            :placeholder="t('entityRisk.score')"
            :aria-label="t('entityRisk.score')"
          />
          <input
            v-model="review"
            type="date"
            class="input input-sm"
            :aria-label="t('entityRisk.review')"
          />
          <input
            v-model="reason"
            type="text"
            class="input input-sm grow"
            :maxlength="maximumReason"
            :placeholder="t('entityRisk.reason')"
            :aria-label="t('entityRisk.reason')"
            data-testid="entity-risk-reason"
          />
          <button
            type="button"
            class="btn btn-sm btn-primary"
            :disabled="!ready || propose.isPending.value"
            data-testid="entity-risk-propose"
            @click="propose.mutate()"
          >
            {{ t('entityRisk.propose') }}
          </button>
        </div>
      </template>
    </fieldset>

    <h2 class="text-sm font-semibold">{{ t('entityRisk.pending') }}</h2>
    <p
      v-if="(requests.data.value ?? []).length === 0"
      class="text-base-content/70 text-sm"
      data-testid="entity-risk-empty"
    >
      {{ t('entityRisk.none') }}
    </p>
    <div
      v-for="item in requests.data.value ?? []"
      :key="item.guid"
      class="border-base-300 flex flex-col gap-1 border-b py-2 text-sm"
      :data-testid="`entity-risk-request-${item.guid}`"
    >
      <div class="flex flex-wrap items-center gap-2">
        <span class="font-mono">{{ item.keyName }}</span>
        <span class="font-mono">{{ item.keyValue }}</span>
        <span class="badge badge-sm">{{ t(`entityRisk.tiers.${item.tierId}`) }}</span>
        <span class="text-base-content/70">{{
            t('entityRisk.by', {user: item.requestedUser})
          }}</span>
      </div>
      <p class="text-base-content/70">{{ item.reason }}</p>
      <div v-if="canApprove && canDecide(item, user)" class="join w-full">
        <input
          v-model="notes[item.guid]"
          type="text"
          class="input input-sm join-item grow"
          :maxlength="maximumReason"
          :placeholder="t('entityRisk.note')"
          :aria-label="t('entityRisk.note')"
        />
        <button
          type="button"
          class="btn btn-sm btn-primary join-item"
          :data-testid="`entity-risk-approve-${item.guid}`"
          @click="approve(item.guid)"
        >
          {{ t('entityRisk.approve') }}
        </button>
        <button
          type="button"
          class="btn btn-sm join-item"
          :disabled="!(notes[item.guid] ?? '').trim()"
          :data-testid="`entity-risk-reject-${item.guid}`"
          @click="reject(item.guid)"
        >
          {{ t('entityRisk.reject') }}
        </button>
      </div>
      <div v-if="canWrite && canRetract(item, user)">
        <button
          type="button"
          class="btn btn-sm"
          :data-testid="`entity-risk-retract-${item.guid}`"
          @click="retract(item.guid)"
        >
          {{ t('entityRisk.retract') }}
        </button>
      </div>
    </div>

    <h2 class="text-sm font-semibold">{{ t('entityRisk.due') }}</h2>
    <p
      v-if="(due.data.value ?? []).length === 0"
      class="text-base-content/70 text-sm"
      data-testid="entity-risk-due-empty"
    >
      {{ t('entityRisk.noneDue') }}
    </p>
    <ul v-else class="text-sm" data-testid="entity-risk-due">
      <li v-for="row in due.data.value" :key="`${row.keyName}-${row.keyValue}`">
        <span class="font-mono">{{ row.keyName }} {{ row.keyValue }}</span>
        {{ t(`entityRisk.tiers.${row.tierId}`) }}
        {{ row.reviewByDate ? row.reviewByDate.slice(0, 10) : '' }}
      </li>
    </ul>
  </section>
</template>

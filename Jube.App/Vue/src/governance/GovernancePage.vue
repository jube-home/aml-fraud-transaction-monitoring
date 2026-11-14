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
import {ClipboardCheck} from 'lucide-vue-next'
import FieldHelp from '../components/FieldHelp.vue'
import {useToasts} from '../stores/toasts'
import {
  governanceApi,
  governanceKey,
  maximumOwner,
  maximumText,
  statementReady,
  type InventoryRow,
} from './governanceApi'

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()

const inventory = useQuery({
  queryKey: governanceKey,
  queryFn: () => governanceApi.inventory(),
  staleTime: 15_000,
  retry: false,
})

const editing = ref<InventoryRow | null>(null)
const owner = ref('')
const purpose = ref('')
const oversight = ref('')
const tier = ref(2)
const review = ref('')

const ready = computed(() =>
  statementReady(owner.value, purpose.value, oversight.value, tier.value),
)
const statusClass = (id: number) =>
  id === 4 ? 'badge-error' : id === 3 ? 'badge-warning' : id === 2 ? 'badge-success' : 'badge-ghost'

async function edit(row: InventoryRow): Promise<void> {
  editing.value = row
  const history = await governanceApi.get(row.kindId, row.modelGuid)
  const current = history.current
  owner.value = current?.ownerUser ?? ''
  purpose.value = current?.intendedPurpose ?? ''
  oversight.value = current?.humanOversight ?? ''
  tier.value = current?.riskTierId ?? 2
  review.value = current?.nextReviewDate ? current.nextReviewDate.slice(0, 10) : ''
}

const save = useMutation({
  mutationFn: () =>
    governanceApi.set({
      kindId: editing.value!.kindId,
      modelGuid: editing.value!.modelGuid,
      ownerUser: owner.value.trim(),
      intendedPurpose: purpose.value.trim(),
      riskTierId: tier.value,
      humanOversight: oversight.value.trim(),
      nextReviewDate: review.value ? `${review.value}T00:00:00Z` : null,
    }),
  onSuccess: () => {
    editing.value = null
    toasts.success(t('governance.saved'))
    void client.invalidateQueries({queryKey: ['service', 'ModelGovernance']})
  },
  onError: (error) => toasts.apiError(error),
})
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="adminGovernance-page">
    <h1 class="flex items-center gap-2 text-lg font-semibold">
      <ClipboardCheck class="size-4" aria-hidden="true"/>
      {{ t('governance.title') }}
      <FieldHelp :label="t('governance.title')" :text="t('governance.hint')"/>
    </h1>

    <p v-if="inventory.isError.value" role="alert" class="text-sm">{{ t('governance.failed') }}</p>
    <p
      v-else-if="(inventory.data.value ?? []).length === 0"
      class="text-base-content/70 text-sm"
      data-testid="governance-empty"
    >
      {{ t('governance.none') }}
    </p>

    <div v-else class="min-w-0 overflow-auto">
      <table class="table table-sm" data-testid="governance-table">
        <thead class="text-base-content">
        <tr>
          <th>{{ t('governance.model') }}</th>
          <th>{{ t('governance.kind') }}</th>
          <th>{{ t('governance.owner') }}</th>
          <th>{{ t('governance.tier') }}</th>
          <th>{{ t('governance.nextReview') }}</th>
          <th>{{ t('governance.status') }}</th>
          <th>
            <span class="sr-only">{{ t('governance.edit') }}</span>
          </th>
        </tr>
        </thead>
        <tbody>
        <tr v-for="row in inventory.data.value" :key="`${row.kindId}-${row.modelGuid}`">
          <td>{{ row.name }}</td>
          <td>{{ t(`governance.kinds.${row.kindId}`) }}</td>
          <td>{{ row.ownerUser ?? '' }}</td>
          <td>{{ row.riskTierId ? t(`governance.tiers.${row.riskTierId}`) : '' }}</td>
          <td>{{ row.nextReviewDate ? row.nextReviewDate.slice(0, 10) : '' }}</td>
          <td>
              <span
                class="badge badge-sm"
                :class="statusClass(row.reviewStatusId)"
                :data-testid="`governance-status-${row.modelGuid}`"
              >{{ t(`governance.statuses.${row.reviewStatusId}`) }}</span
              >
          </td>
          <td class="flex gap-1">
            <button
              type="button"
              class="btn btn-xs"
              :data-testid="`governance-edit-${row.modelGuid}`"
              @click="edit(row)"
            >
              {{ t('governance.edit') }}
            </button>
            <a
              class="btn btn-xs"
              :href="governanceApi.cardUrl(row.kindId, row.modelGuid)"
              download
              :data-testid="`governance-card-${row.modelGuid}`"
            >{{ t('governance.card') }}</a
            >
          </td>
        </tr>
        </tbody>
      </table>
    </div>

    <fieldset v-if="editing" class="fieldset" data-testid="governance-form">
      <legend class="fieldset-legend">{{ editing.name }}</legend>
      <input
        v-model="owner"
        type="text"
        class="input input-sm w-full"
        :maxlength="maximumOwner"
        :placeholder="t('governance.owner')"
        :aria-label="t('governance.owner')"
        data-testid="governance-owner"
      />
      <textarea
        v-model="purpose"
        class="textarea textarea-sm w-full"
        rows="3"
        :maxlength="maximumText"
        :placeholder="t('governance.purpose')"
        :aria-label="t('governance.purpose')"
        data-testid="governance-purpose"
      ></textarea>
      <textarea
        v-model="oversight"
        class="textarea textarea-sm w-full"
        rows="3"
        :maxlength="maximumText"
        :placeholder="t('governance.oversight')"
        :aria-label="t('governance.oversight')"
        data-testid="governance-oversight"
      ></textarea>
      <div class="flex flex-wrap gap-2">
        <select
          v-model.number="tier"
          class="select select-sm"
          :aria-label="t('governance.tier')"
          data-testid="governance-tier"
        >
          <option v-for="id in [1, 2, 3]" :key="id" :value="id">
            {{ t(`governance.tiers.${id}`) }}
          </option>
        </select>
        <input
          v-model="review"
          type="date"
          class="input input-sm"
          :aria-label="t('governance.nextReview')"
          data-testid="governance-review"
        />
        <button
          type="button"
          class="btn btn-sm btn-primary"
          :disabled="!ready || save.isPending.value"
          data-testid="governance-save"
          @click="save.mutate()"
        >
          {{ t('governance.save') }}
        </button>
        <button type="button" class="btn btn-sm" @click="editing = null">
          {{ t('governance.cancel') }}
        </button>
      </div>
    </fieldset>
  </section>
</template>

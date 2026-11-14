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
import {useMutation} from '@tanstack/vue-query'
import {describeApiError} from '../api/problems'
import {ruleToolsApi} from '../ruleTools/api'
import {findPlaceholder, maximumCandidates, parseCandidates} from '../ruleTools/model'
import type {BacktestRequest, TuneVariant} from '../ruleTools/types'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{
  ruleId: number | null
  ruleText: string
  buildRequest: () => BacktestRequest
}>()

const {t} = useI18n()

const example = '{{threshold}}'

const candidatesText = ref('')
const problems = ref<string[]>([])
const variants = ref<TuneVariant[]>([])
const saved = ref<string[]>([])

const placeholder = computed(() => findPlaceholder(props.ruleText))
const candidates = computed(() => parseCandidates(candidatesText.value))
const percent = (value: number | null) => (value === null ? '–' : `${(value * 100).toFixed(1)}%`)

const sweep = useMutation({
  mutationFn: () => {
    if (!placeholder.value || !candidates.value) throw new Error('incomplete')

    return ruleToolsApi.tune(props.buildRequest(), placeholder.value, candidates.value)
  },
  onSuccess: (result) => {
    saved.value = []

    if (!result.valid) {
      variants.value = []
      problems.value = result.errors.map((error) => `${error.propertyName}: ${error.message}`)

      return
    }

    problems.value = []
    variants.value = result.variants
  },
  onError: (error) => {
    variants.value = []
    problems.value = describeApiError(error, {
      failed: t('ruleTools.tune.failed'),
      refused: t('ruleTools.noPermission'),
    })
  },
})

const shadow = useMutation({
  mutationFn: (value: string) =>
    ruleToolsApi.saveShadow(props.ruleId ?? 0, props.ruleText, placeholder.value ?? '', value),
  onSuccess: (result) => {
    problems.value = []
    saved.value = [...saved.value, result.name]
  },
  onError: (error) => {
    problems.value = describeApiError(error, {
      failed: t('ruleTools.tune.saveFailed'),
      refused: t('ruleTools.noPermission'),
    })
  },
})
</script>

<template>
  <section class="flex min-w-0 flex-col gap-2" data-testid="backtest-tuning">
    <h3 class="text-sm font-semibold">{{ t('ruleTools.tune.title') }}</h3>
    <p class="text-secondary text-sm">{{ t('ruleTools.tune.hint', {example}) }}</p>

    <p v-if="!placeholder" class="text-sm" data-testid="tune-no-placeholder">
      {{ t('ruleTools.tune.noPlaceholder', {example}) }}
    </p>
    <form v-else class="flex flex-wrap items-end gap-3" @submit.prevent="sweep.mutate()">
      <span class="badge badge-sm font-mono" data-testid="tune-placeholder">{{ placeholder }}</span>
      <label class="flex min-w-0 grow flex-col gap-1">
        <span class="label text-xs">{{
            t('ruleTools.tune.candidates', {count: maximumCandidates})
          }}</span>
        <input
          v-model="candidatesText"
          type="text"
          class="input input-sm font-mono"
          data-testid="tune-candidates"
        />
      </label>
      <button
        type="submit"
        class="btn btn-sm btn-primary"
        :disabled="!candidates || sweep.isPending.value"
        data-testid="tune-run"
      >
        {{ t('ruleTools.tune.run') }}
      </button>
    </form>

    <ProblemAlert v-if="problems.length > 0" :reasons="problems" data-testid="tune-problems"/>

    <div v-if="variants.length > 0" class="min-w-0 overflow-auto">
      <table class="table table-sm" data-testid="tune-table">
        <thead>
        <tr>
          <th>{{ t('ruleTools.tune.value') }}</th>
          <th>{{ t('ruleTools.tune.fired') }}</th>
          <th>{{ t('ruleTools.tune.precision') }}</th>
          <th>{{ t('ruleTools.tune.recall') }}</th>
          <th>{{ t('ruleTools.tune.tp') }}</th>
          <th>{{ t('ruleTools.tune.fp') }}</th>
          <th>{{ t('ruleTools.tune.fn') }}</th>
          <th>{{ t('ruleTools.tune.tn') }}</th>
          <th>
            <span class="sr-only">{{ t('ruleTools.tune.save') }}</span>
          </th>
        </tr>
        </thead>
        <tbody>
        <tr
          v-for="variant in variants"
          :key="variant.value"
          :data-testid="`tune-row-${variant.value}`"
        >
          <td class="font-mono">{{ variant.value }}</td>
          <td>{{ variant.fired }}</td>
          <td>{{ percent(variant.precision) }}</td>
          <td>{{ percent(variant.recall) }}</td>
          <td>{{ variant.truePositives }}</td>
          <td>{{ variant.falsePositives }}</td>
          <td>{{ variant.falseNegatives }}</td>
          <td>{{ variant.trueNegatives }}</td>
          <td>
            <button
              v-if="ruleId !== null"
              type="button"
              class="btn btn-xs"
              :disabled="shadow.isPending.value"
              :data-testid="`tune-shadow-${variant.value}`"
              @click="shadow.mutate(variant.value)"
            >
              {{ t('ruleTools.tune.save') }}
            </button>
          </td>
        </tr>
        </tbody>
      </table>
    </div>

    <p v-for="name in saved" :key="name" class="text-sm" role="status" data-testid="tune-saved">
      {{ t('ruleTools.tune.saved', {name}) }}
    </p>
  </section>
</template>

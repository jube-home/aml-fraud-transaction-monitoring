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
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import ServerChart from '../../components/ServerChart.vue'
import {useFormat} from '../../composables/useFormat'
import {useUiStore} from '../../stores/ui'
import {transactionChartBody, transactionChartUrl} from './api'
import {directionOf, scoreKind, shownInputs, staleDays, trainedAgeDays, unlabelled} from './facts'
import type {Adaptation} from './types'

const props = defineProps<{ adaptation: Adaptation; workflow: string; id: number; now: Date }>()

const {t} = useI18n()
const {date, fixed, number, percent} = useFormat()
const ui = useUiStore()
const dark = computed(() => ui.resolvedTheme === 'jube-dark')

const model = computed(() => props.adaptation.model)
const inputs = computed(() => shownInputs(props.adaptation))
const hiddenInputs = computed(() => props.adaptation.contribution?.hiddenItems ?? 0)
const kind = computed(() => scoreKind(props.adaptation))
const age = computed(() => trainedAgeDays(model.value?.trainedDate, props.now))
const stale = computed(() => age.value !== null && age.value > staleDays)
const bands = computed(() =>
  (props.adaptation.calibration?.bands ?? []).filter(
    (band) => band.expected !== null && band.observed !== null,
  ),
)

const sourceText = computed(() => {
  if (props.adaptation.source === 'exhaustive') return t('journal.drawer.source.exhaustive')
  if (props.adaptation.source === 'advanced') return t('journal.drawer.source.advanced')

  return t('journal.drawer.source.http')
})

const runsOn = computed(() =>
  props.adaptation.source === 'http' ? t('journal.facts.runsHttp') : t('journal.facts.runsInHouse'),
)

const unlabelledCount = computed(() => inputs.value.filter((item) => unlabelled(item)).length)

function calibrationUrl(): string {
  return transactionChartUrl(
    'Calibration',
    props.adaptation.name,
    props.adaptation.source,
    dark.value,
  )
}

function directionText(item: (typeof inputs.value)[number]): string {
  const direction = directionOf(item)

  if (direction === 'higher') return t('journal.facts.higherRiskier')
  if (direction === 'lower') return t('journal.facts.lowerRiskier')

  return t('journal.facts.notDeclared')
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" :data-testid="`journal-facts-${adaptation.name}`">
    <section class="flex flex-col gap-1" :data-testid="`journal-facts-identity-${adaptation.name}`">
      <h4 class="text-xs font-semibold uppercase text-base-content/70">
        {{ t('journal.facts.group.identity') }}
      </h4>
      <dl class="grid grid-cols-[minmax(0,1fr)_minmax(0,2fr)] gap-x-3 gap-y-1 text-sm">
        <dt class="text-base-content/70">{{ t('journal.facts.name') }}</dt>
        <dd dir="auto">{{ adaptation.name }}</dd>
        <dt class="text-base-content/70">{{ t('journal.facts.version') }}</dt>
        <dd v-if="model?.version" dir="ltr" class="font-mono">{{ model.version }}</dd>
        <dd v-else class="text-base-content/70">{{ t('journal.facts.notDeclared') }}</dd>
        <dt class="text-base-content/70">{{ t('journal.facts.type') }}</dt>
        <dd>{{ model?.family ?? sourceText }}</dd>
        <dt class="text-base-content/70">{{ t('journal.facts.runs') }}</dt>
        <dd>{{ runsOn }}</dd>
        <dt class="text-base-content/70">{{ t('journal.facts.hash') }}</dt>
        <dd v-if="model?.artifactHash" dir="ltr" class="font-mono">
          {{ model.artifactHash.slice(0, 12) }}
        </dd>
        <dd v-else class="text-base-content/70">{{ t('journal.facts.notDeclared') }}</dd>
      </dl>
    </section>

    <section class="flex flex-col gap-1">
      <h4 class="text-xs font-semibold uppercase text-base-content/70">
        {{ t('journal.facts.group.score') }}
      </h4>
      <dl class="grid grid-cols-[minmax(0,1fr)_minmax(0,2fr)] gap-x-3 gap-y-1 text-sm">
        <dt class="text-base-content/70">{{ t('journal.facts.scoreType') }}</dt>
        <dd>
          {{
            kind === 'probability'
              ? t('journal.facts.typeProbability')
              : t('journal.facts.typeScore')
          }}
        </dd>
        <dt class="text-base-content/70">{{ t('journal.facts.value') }}</dt>
        <dd dir="ltr" class="font-mono">{{ fixed(adaptation.value, 3) }}</dd>
        <dt class="text-base-content/70">{{ t('journal.facts.threshold') }}</dt>
        <dd v-if="adaptation.result?.threshold != null" dir="ltr" class="font-mono">
          {{ fixed(adaptation.result.threshold, 2) }}
        </dd>
        <dd v-else class="text-base-content/70">{{ t('journal.facts.notDeclared') }}</dd>
        <dt class="text-base-content/70">{{ t('journal.facts.baseValue') }}</dt>
        <dd v-if="adaptation.contribution?.baseValue != null" dir="ltr" class="font-mono">
          {{ fixed(adaptation.contribution.baseValue, 2) }}
          <span v-if="adaptation.contribution.space" class="text-base-content/70">{{
              adaptation.contribution.space
            }}</span>
        </dd>
        <dd v-else class="text-base-content/70">{{ t('journal.facts.notDeclared') }}</dd>
        <dt class="text-base-content/70">{{ t('journal.facts.calibration') }}</dt>
        <dd v-if="adaptation.calibration?.method">{{ adaptation.calibration.method }}</dd>
        <dd v-else class="text-base-content/70">{{ t('journal.facts.notDeclared') }}</dd>
      </dl>
    </section>

    <section class="flex flex-col gap-1">
      <h4 class="text-xs font-semibold uppercase text-base-content/70">
        {{ t('journal.facts.group.training') }}
      </h4>
      <dl class="grid grid-cols-[minmax(0,1fr)_minmax(0,2fr)] gap-x-3 gap-y-1 text-sm">
        <dt class="text-base-content/70">{{ t('journal.facts.trained') }}</dt>
        <dd v-if="model?.trainedDate">{{ date(model.trainedDate) }}</dd>
        <dd v-else class="text-base-content/70">{{ t('journal.facts.notDeclared') }}</dd>
        <dt class="text-base-content/70">{{ t('journal.facts.features') }}</dt>
        <dd v-if="model?.featureCount != null" dir="ltr" class="font-mono">
          {{ number(model.featureCount) }}
        </dd>
        <dd v-else class="text-base-content/70">{{ t('journal.facts.notDeclared') }}</dd>
        <dt class="text-base-content/70">{{ t('journal.facts.window') }}</dt>
        <dd class="text-base-content/70">{{ t('journal.facts.notDeclared') }}</dd>
        <dt class="text-base-content/70">{{ t('journal.facts.examples') }}</dt>
        <dd class="text-base-content/70">{{ t('journal.facts.notDeclared') }}</dd>
      </dl>
    </section>

    <section class="flex flex-col gap-1">
      <h4 class="text-xs font-semibold uppercase text-base-content/70">
        {{ t('journal.facts.group.performance') }}
      </h4>
      <dl class="grid grid-cols-[minmax(0,1fr)_minmax(0,2fr)] gap-x-3 gap-y-1 text-sm">
        <dt class="text-base-content/70">{{ t('journal.facts.sample') }}</dt>
        <dd v-if="adaptation.calibration?.sample != null" dir="ltr" class="font-mono">
          {{ number(adaptation.calibration.sample) }}
        </dd>
        <dd v-else class="text-base-content/70">{{ t('journal.facts.notDeclared') }}</dd>
        <dt class="text-base-content/70">{{ t('journal.facts.brier') }}</dt>
        <dd v-if="adaptation.calibration?.brier != null" dir="ltr" class="font-mono">
          {{ fixed(adaptation.calibration.brier, 3) }}
        </dd>
        <dd v-else class="text-base-content/70">{{ t('journal.facts.notDeclared') }}</dd>
        <dt class="text-base-content/70">{{ t('journal.facts.positiveRate') }}</dt>
        <dd v-if="adaptation.result?.expectedPositiveRate != null" dir="ltr" class="font-mono">
          {{ percent(adaptation.result.expectedPositiveRate) }}
        </dd>
        <dd v-else class="text-base-content/70">{{ t('journal.facts.notDeclared') }}</dd>
      </dl>
      <div v-if="bands.length > 0" key="calibration" class="min-w-0">
        <ServerChart
          :url="calibrationUrl()"
          :body="transactionChartBody(workflow, id)"
          :label="t('journal.drawer.calibration')"
          empty-on-refusal
        />
      </div>
    </section>

    <section class="flex flex-col gap-1" :data-testid="`journal-facts-inputs-${adaptation.name}`">
      <h4 class="text-xs font-semibold uppercase text-base-content/70">
        {{ t('journal.facts.group.inputs') }}
      </h4>
      <p v-if="inputs.length === 0" key="none" class="text-sm text-base-content/70">
        {{ t('journal.facts.noInputs') }}
      </p>
      <ul v-else key="inputs" class="flex flex-col gap-1">
        <li
          v-for="item in inputs"
          :key="item.name ?? ''"
          class="flex items-baseline justify-between gap-3 text-sm"
        >
          <span dir="auto" class="min-w-0 truncate">{{ item.humanLabel ?? item.name }}</span>
          <span class="flex shrink-0 items-center gap-2 text-xs text-base-content/70">
            <span v-if="unlabelled(item)" class="badge badge-soft badge-sm">{{
                t('journal.facts.unlabelled')
              }}</span>
            {{ directionText(item) }}
          </span>
        </li>
      </ul>
      <p v-if="hiddenInputs > 0" key="hidden" class="text-xs text-base-content/70">
        {{ t('journal.drawer.hiddenInputs', {n: hiddenInputs}) }}
      </p>
      <p v-if="unlabelledCount > 0" key="unlabelled" class="text-xs text-base-content/70">
        {{ t('journal.facts.unlabelledCount', {n: unlabelledCount}) }}
      </p>
    </section>

    <section class="flex flex-col gap-1">
      <h4 class="text-xs font-semibold uppercase text-base-content/70">
        {{ t('journal.facts.group.health') }}
      </h4>
      <p v-if="adaptation.error" key="error" class="text-sm" role="alert">
        {{ t('journal.drawer.error', {text: adaptation.error}) }}
      </p>
      <ul class="flex flex-col gap-1 text-sm">
        <li v-if="stale" key="stale">
          {{ t('journal.facts.stale', {n: number(age ?? 0)}) }}
        </li>
        <li v-else-if="age !== null" key="age" class="text-base-content/70">
          {{ t('journal.facts.age', {n: number(age)}) }}
        </li>
        <li v-if="adaptation.calibration?.calibrated === false" key="uncalibrated">
          {{ t('journal.facts.uncalibrated') }}
        </li>
        <li class="text-base-content/70">{{ t('journal.facts.driftNotDeclared') }}</li>
      </ul>
    </section>

    <section class="flex flex-col gap-1">
      <h4 class="text-xs font-semibold uppercase text-base-content/70">
        {{ t('journal.facts.group.changes') }}
      </h4>
      <p class="text-sm text-base-content/70">{{ t('journal.facts.changesNotDeclared') }}</p>
    </section>
  </div>
</template>

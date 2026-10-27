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
import ServerChart from '../../components/ServerChart.vue'
import { useFormat } from '../../composables/useFormat'
import { useUiStore } from '../../stores/ui'
import { transactionChartUrl } from './api'
import type { Adaptation } from './types'

const props = defineProps<{ adaptation: Adaptation; workflow: string; id: number }>()

const { t } = useI18n()
const { fixed, number } = useFormat()
const ui = useUiStore()
const dark = computed(() => ui.resolvedTheme === 'jube-dark')

const sourceLabel = computed(() => {
  if (props.adaptation.source === 'exhaustive') return t('journal.drawer.source.exhaustive')
  if (props.adaptation.source === 'advanced') return t('journal.drawer.source.advanced')

  return t('journal.drawer.source.http')
})

const items = computed(() => props.adaptation.contribution?.items ?? [])
const visibleItems = computed(() =>
  items.value.filter((item) => !item.hidden && item.signed !== null),
)
const hasBands = computed(
  () =>
    (props.adaptation.calibration?.bands ?? []).filter(
      (band) => band.expected !== null && band.observed !== null,
    ).length > 0,
)

function contributionUrl(kind: 'Contribution' | 'Calibration'): string {
  return transactionChartUrl(
    kind,
    props.workflow,
    props.id,
    props.adaptation.name,
    props.adaptation.source,
    dark.value,
  )
}

function nodeText(index: number): string {
  const node = props.adaptation.journey[index]!

  if (node.hidden) return t('journal.drawer.hiddenInput')

  const name = node.humanLabel ?? node.feature ?? ''
  const threshold =
    node.thresholdCategory ?? (node.threshold === null ? '' : number(node.threshold))

  return [name, node.operator ?? '', threshold].filter((part) => part !== '').join(' ')
}
</script>

<template>
  <section
    class="flex min-w-0 flex-col gap-2 rounded-box border border-base-300 p-3"
    :data-testid="`journal-adaptation-${adaptation.name}`"
  >
    <div class="flex flex-wrap items-center justify-between gap-2">
      <h4 class="text-sm font-semibold">{{ adaptation.name }}</h4>
      <div class="flex flex-wrap items-center gap-1">
        <span class="badge badge-soft badge-sm">{{ sourceLabel }}</span>
        <span v-if="adaptation.result?.activated === true" class="badge badge-warning badge-sm">
          {{ t('journal.drawer.activated') }}
        </span>
        <span
          v-else-if="adaptation.result?.activated === false"
          key="below"
          class="badge badge-soft badge-sm"
        >
          {{ t('journal.drawer.belowThreshold') }}
        </span>
      </div>
    </div>

    <p class="flex flex-wrap items-baseline gap-x-3 text-sm">
      <span class="font-mono text-xl tabular-nums" data-testid="journal-adaptation-value">
        {{ adaptation.value === null ? '' : fixed(adaptation.value, 3) }}
      </span>
      <span v-if="adaptation.humanLabel" key="label">{{ adaptation.humanLabel }}</span>
      <span
        v-if="adaptation.result?.threshold !== null && adaptation.result?.threshold !== undefined"
        key="threshold"
        class="font-mono text-xs text-base-content/70"
      >
        {{ t('journal.drawer.threshold', { n: fixed(adaptation.result.threshold, 2) }) }}
      </span>
    </p>

    <p v-if="adaptation.error" key="error" class="text-sm" role="alert">
      {{ t('journal.drawer.error', { text: adaptation.error }) }}
    </p>
    <p v-if="adaptation.narrative" key="narrative" class="text-sm">{{ adaptation.narrative }}</p>

    <p v-if="adaptation.model" key="model" class="font-mono text-xs text-base-content/70">
      {{
        [
          adaptation.model.family,
          adaptation.model.version ? `v${adaptation.model.version}` : '',
          adaptation.model.artifactHash ? adaptation.model.artifactHash.slice(0, 8) : '',
        ]
          .filter((part) => part)
          .join(' ')
      }}
    </p>

    <div
      v-if="adaptation.journey.length > 0"
      key="journey"
      class="flex flex-col gap-1"
      data-testid="journal-journey"
    >
      <h5 class="text-xs text-base-content/70">{{ t('journal.drawer.journey') }}</h5>
      <ol class="list">
        <li
          v-for="(node, index) in adaptation.journey"
          :key="index"
          class="list-row items-center px-0 py-1"
        >
          <span class="badge badge-soft badge-sm font-mono tabular-nums">{{ index + 1 }}</span>
          <span
            class="text-sm"
            :class="{ 'italic text-base-content/70': node.hidden }"
            data-testid="journal-journey-node"
          >
            {{ nodeText(index) }}
          </span>
        </li>
      </ol>
    </div>

    <div
      v-if="visibleItems.length > 0"
      key="contribution"
      class="flex min-w-0 flex-col gap-1"
      data-testid="journal-contribution"
    >
      <h5 class="text-xs text-base-content/70">{{ t('journal.drawer.contribution') }}</h5>
      <ServerChart
        :url="contributionUrl('Contribution')"
        :label="t('journal.drawer.contribution')"
        empty-on-refusal
      />
      <p
        v-if="(adaptation.contribution?.hiddenItems ?? 0) > 0"
        key="hidden"
        class="text-xs text-base-content/70"
      >
        {{ t('journal.drawer.hiddenInputs', { n: adaptation.contribution?.hiddenItems }) }}
      </p>
    </div>

    <details
      v-if="hasBands"
      key="calibration"
      class="collapse collapse-arrow border border-base-300"
    >
      <summary class="collapse-title text-xs">{{ t('journal.drawer.calibration') }}</summary>
      <div class="collapse-content">
        <ServerChart
          :url="contributionUrl('Calibration')"
          :label="t('journal.drawer.calibration')"
          empty-on-refusal
        />
      </div>
    </details>
  </section>
</template>

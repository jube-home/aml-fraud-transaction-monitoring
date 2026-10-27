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
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import BacktestPanel from './BacktestPanel.vue'
import RuleDependencies from './RuleDependencies.vue'
import RuleIntegrity from './RuleIntegrity.vue'

withDefaults(
  defineProps<{
    modelId: number
    ruleId: number | null
    ruleText: string
    ruleType?: string
    canBacktest?: boolean
    canCheckIntegrity?: boolean
  }>(),
  { ruleType: 'ActivationRule', canBacktest: false, canCheckIntegrity: false },
)

const { t } = useI18n()

const opened = ref<string[]>([])

function toggled(name: string, event: Event): void {
  if ((event.target as HTMLDetailsElement).open && !opened.value.includes(name)) {
    opened.value = [...opened.value, name]
  }
}
</script>

<template>
  <section
    class="flex min-w-0 flex-col gap-2"
    :aria-label="t('ruleTools.title')"
    data-testid="rule-tools"
  >
    <details
      v-if="canBacktest"
      key="backtest"
      class="collapse collapse-arrow border border-base-300 bg-base-100"
      data-testid="rule-tools-backtest"
      @toggle="toggled('backtest', $event)"
    >
      <summary class="collapse-title font-semibold">{{ t('ruleTools.backtest') }}</summary>
      <div class="collapse-content">
        <BacktestPanel
          v-if="opened.includes('backtest')"
          :model-id="modelId"
          :rule-type="ruleType"
          :rule-id="ruleId"
          :rule-text="ruleText"
        />
      </div>
    </details>

    <details
      class="collapse collapse-arrow border border-base-300 bg-base-100"
      data-testid="rule-tools-dependencies"
      @toggle="toggled('dependencies', $event)"
    >
      <summary class="collapse-title font-semibold">{{ t('ruleTools.dependencies') }}</summary>
      <div class="collapse-content">
        <RuleDependencies
          v-if="opened.includes('dependencies')"
          :model-id="modelId"
          :rule-type="ruleType"
          :rule-id="ruleId"
        />
      </div>
    </details>

    <details
      v-if="canCheckIntegrity"
      key="integrity"
      class="collapse collapse-arrow border border-base-300 bg-base-100"
      data-testid="rule-tools-integrity"
      @toggle="toggled('integrity', $event)"
    >
      <summary class="collapse-title font-semibold">{{ t('ruleTools.integrityTitle') }}</summary>
      <div class="collapse-content">
        <RuleIntegrity
          v-if="opened.includes('integrity')"
          :model-id="modelId"
          :rule-type="ruleType"
          :rule-id="ruleId"
        />
      </div>
    </details>
  </section>
</template>

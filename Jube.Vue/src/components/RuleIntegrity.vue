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
import { queryKeys } from '../api/queryKeys'
import { integrityApi } from '../integrity/api'
import IntegrityChecksDetail from './IntegrityChecksDetail.vue'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{ modelId: number; ruleType: string; ruleId: number | null }>()

const { t } = useI18n()

const saved = computed(() => props.ruleId !== null)

const report = useQuery({
  queryKey: computed(() => queryKeys.integrityReport(props.modelId)),
  queryFn: () => integrityApi.report(props.modelId),
  enabled: saved,
  retry: false,
})

const checks = computed(() =>
  (report.data.value?.checks ?? []).filter(
    (check) => check.entityKind === props.ruleType && check.entityId === props.ruleId,
  ),
)
</script>

<template>
  <div class="flex min-w-0 flex-col gap-2" data-testid="rule-integrity">
    <p v-if="!saved" key="unsaved" class="text-sm text-secondary">
      {{ t('ruleTools.integrity.saveFirst') }}
    </p>
    <ProblemAlert
      v-else-if="report.isError.value"
      key="failed"
      :reasons="[t('ruleTools.integrity.failed')]"
    />
    <p
      v-else-if="report.isSuccess.value && checks.length === 0"
      key="none"
      class="text-sm"
      data-testid="rule-integrity-none"
    >
      {{ t('ruleTools.integrity.none') }}
    </p>
    <IntegrityChecksDetail v-else-if="report.isSuccess.value" key="checks" :checks="checks" />
  </div>
</template>

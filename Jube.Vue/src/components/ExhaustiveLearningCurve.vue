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
import { useI18n } from 'vue-i18n'
import ExhaustiveNarrative from './ExhaustiveNarrative.vue'
import ServerChart from './ServerChart.vue'

defineProps<{ instanceId: number; version: string }>()

const { t } = useI18n()
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="exhaustive-learning">
    <ExhaustiveNarrative :instance-id="instanceId" group="LearningCurve" :version="version" />
    <div class="grid min-w-0 gap-4 @container @3xl:grid-cols-2">
      <ServerChart
        :url="`/api/Chart/Exhaustive/PromotionHistory/${instanceId}`"
        :label="t('exhaustive.learning.promotion')"
        :version="version"
      />
      <ServerChart
        :url="`/api/Chart/Exhaustive/LearningCurve/${instanceId}`"
        :label="t('exhaustive.learning.curve')"
        :version="version"
      />
    </div>
  </div>
</template>

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
import {ListChecks} from 'lucide-vue-next'
import DecideTile from '../detail/DecideTile.vue'

const props = defineProps<{ done: number; required: number; breached: number }>()

const emit = defineEmits<{ select: [] }>()

const {t} = useI18n()

const hint = computed(() =>
  props.breached > 0
    ? t('caseDetail.checklist.progressBreached', {
      done: props.done,
      required: props.required,
      n: props.breached,
    })
    : t('caseDetail.checklist.progress', {done: props.done, required: props.required}),
)
</script>

<template>
  <DecideTile
    v-if="required > 0"
    key="tile"
    :icon="ListChecks"
    :label="t('caseDetail.decide.tile.checklist.label')"
    :tip="t('caseDetail.decide.tile.checklist.tip')"
    :hint="hint"
    :tone="breached > 0 ? 'error' : 'neutral'"
    testid="case-decide-tab-checklist"
    @select="emit('select')"
  />
</template>

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
import { computed, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { Info } from 'lucide-vue-next'

const props = defineProps<{ label: string; text: string }>()

const { t } = useI18n()
const popId = `help-${useId()}`
const paragraphs = computed(() => props.text.split('\n').filter((line) => line.trim() !== ''))

const anchor = computed(() => `anchor-name: --${popId}`)
const position = computed(() => `position-anchor: --${popId}`)
</script>

<template>
  <span v-if="text" class="inline-flex">
    <button
      type="button"
      class="btn btn-ghost btn-circle btn-xs text-secondary"
      :popovertarget="popId"
      :style="anchor"
      :aria-label="t('crud.about', { field: label })"
      data-testid="field-help"
    >
      <Info aria-hidden="true" class="size-3.5" />
    </button>
    <div
      :id="popId"
      popover
      class="dropdown z-50 mt-2 w-80 max-w-[90vw] rounded-box border border-base-300 bg-base-100 p-3 text-start text-sm font-normal text-base-content shadow-md"
      :style="position"
      data-testid="field-help-text"
    >
      <p class="mb-1 font-semibold text-primary">{{ props.label }}</p>
      <p v-for="(paragraph, index) in paragraphs" :key="index" class="mt-1">{{ paragraph }}</p>
    </div>
  </span>
</template>

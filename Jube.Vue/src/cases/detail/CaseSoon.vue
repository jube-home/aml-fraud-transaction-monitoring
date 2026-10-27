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
import { soonFor, type SoonArea } from './soon'

const props = defineProps<{ area: SoonArea }>()

const { t } = useI18n()

const items = computed(() => soonFor(props.area, t))
</script>

<template>
  <div
    class="flex min-w-0 flex-col gap-2 rounded-box border border-dashed border-base-300 p-4"
    :data-testid="`case-soon-${area}`"
  >
    <div class="flex items-center justify-between gap-2">
      <h4 class="text-xs text-base-content/70">{{ t('caseDetail.soon.heading') }}</h4>
      <span class="badge badge-ghost badge-sm">{{ t('caseDetail.soon.badge') }}</span>
    </div>
    <ul class="flex flex-col gap-2">
      <li v-for="item in items" :key="item.id" class="flex flex-col gap-0.5">
        <span class="text-sm font-medium">{{ item.title }}</span>
        <span class="text-xs text-base-content/70">{{ item.body }}</span>
      </li>
    </ul>
  </div>
</template>

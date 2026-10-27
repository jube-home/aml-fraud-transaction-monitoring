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
import { Zap } from 'lucide-vue-next'

defineProps<{ names: string[] }>()

const { t } = useI18n()
</script>

<template>
  <section class="flex min-w-0 flex-col gap-2" data-testid="case-activations">
    <p v-if="names.length === 0" class="text-sm text-base-content/70" role="status">
      {{ t('caseDetail.activations.empty') }}
    </p>
    <ul v-else key="list" class="list rounded-box border border-base-300 bg-base-100">
      <li v-for="(name, index) in names" :key="`${index}-${name}`" class="list-row items-center">
        <Zap class="size-4" aria-hidden="true" />
        <span class="text-sm">{{ name }}</span>
      </li>
    </ul>
  </section>
</template>

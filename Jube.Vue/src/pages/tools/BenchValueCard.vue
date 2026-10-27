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

const props = defineProps<{ title: string; value: unknown }>()

const text = computed(() => (props.value === undefined ? '' : JSON.stringify(props.value, null, 2)))
</script>

<template>
  <details
    v-if="text"
    open
    class="collapse-arrow collapse shrink-0 rounded-box border border-base-300 bg-base-100"
    data-testid="bench-value-card"
  >
    <summary class="collapse-title text-sm font-semibold">{{ title }}</summary>
    <div class="collapse-content">
      <pre
        tabindex="0"
        class="max-h-56 overflow-auto rounded-field border border-base-300 bg-base-200 p-2 font-mono text-xs"
        data-testid="bench-value"
        >{{ text }}</pre>
    </div>
  </details>
</template>

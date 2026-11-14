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
defineProps<{
  label: string
  current: string
  tabs: { key: string; label: string }[]
}>()

const emit = defineEmits<{ choose: [key: string] }>()
</script>

<template>
  <div role="tablist" class="tabs tabs-border" :aria-label="label">
    <button
      v-for="tab in tabs"
      :key="tab.key"
      role="tab"
      type="button"
      class="tab"
      :class="{ 'tab-active': current === tab.key }"
      :aria-selected="current === tab.key"
      :data-testid="`tab-${tab.key}`"
      @click="emit('choose', tab.key)"
    >
      {{ tab.label }}
    </button>
  </div>
</template>

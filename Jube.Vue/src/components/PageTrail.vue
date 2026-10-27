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
export interface Crumb {
  id: string | number
  label: string
  backLabel?: string
  testid?: string
}

defineProps<{ crumbs: Crumb[]; label: string }>()

const emit = defineEmits<{ select: [id: string | number] }>()
</script>

<template>
  <nav :aria-label="label">
    <div class="breadcrumbs text-sm">
      <ul>
        <li v-for="(crumb, index) in crumbs" :key="crumb.id">
          <span
            v-if="index === crumbs.length - 1"
            key="here"
            class="inline-flex items-center gap-2 font-semibold text-primary"
            aria-current="page"
            :data-testid="crumb.testid"
          >
            <span class="status status-primary" aria-hidden="true"></span>
            {{ crumb.label }}
          </span>
          <button
            v-else
            key="back"
            type="button"
            class="link link-hover inline-flex items-center gap-2"
            :aria-label="crumb.backLabel"
            :data-testid="crumb.testid"
            @click="emit('select', crumb.id)"
          >
            {{ crumb.label }}
          </button>
        </li>
      </ul>
    </div>
  </nav>
</template>

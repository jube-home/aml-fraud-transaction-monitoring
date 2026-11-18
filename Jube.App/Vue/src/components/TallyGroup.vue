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
defineProps<{ note?: string; testid: string }>()
</script>

<template>
  <div class="flex min-w-0 flex-col gap-2" :data-testid="testid">
    <div class="flex min-w-0 flex-wrap items-start gap-x-6 gap-y-3">
      <slot/>
    </div>
    <p
      v-if="note"
      key="note"
      class="text-sm text-base-content/70"
      role="status"
      :data-testid="`${testid}-note`"
    >
      {{ note }}
    </p>
  </div>
</template>

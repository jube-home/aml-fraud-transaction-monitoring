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
defineProps<{ reasons: string[]; title?: string }>()
</script>

<template>
  <div role="alert" class="alert alert-error items-start">
    <div class="flex flex-col gap-1">
      <p v-if="title" key="title" class="font-semibold">{{ title }}</p>
      <ul class="flex flex-col gap-1">
        <li v-for="reason in reasons" :key="reason">{{ reason }}</li>
      </ul>
    </div>
  </div>
</template>

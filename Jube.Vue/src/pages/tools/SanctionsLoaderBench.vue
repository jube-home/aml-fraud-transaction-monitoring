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
import { useQuery } from '@tanstack/vue-query'
import { meQueryOptions } from '../../auth/me'
import SanctionsLoader from '../../components/SanctionsLoader.vue'

const me = useQuery(meQueryOptions)
const allowed = computed(() => me.data.value?.landlord === true)
</script>

<template>
  <div
    class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto"
    data-testid="sanctions-loader-bench"
  >
    <SanctionsLoader :allowed="allowed" />
  </div>
</template>

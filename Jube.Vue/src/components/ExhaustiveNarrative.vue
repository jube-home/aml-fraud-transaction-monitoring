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
import { queryKeys } from '../api/queryKeys'
import { exhaustiveApi } from '../exhaustive/api'
import { narrativeParagraphs } from '../exhaustive/model'

const props = defineProps<{ instanceId: number; group: string | null; version: string }>()

const narrative = useQuery({
  queryKey: computed(() => queryKeys.exhaustive(props.instanceId, 'narrative', props.version)),
  queryFn: () => exhaustiveApi.narrative(props.instanceId),
  staleTime: 60_000,
  retry: false,
})

const paragraphs = computed(() => narrativeParagraphs(narrative.data.value, props.group))
</script>

<template>
  <div
    v-if="paragraphs.length > 0"
    class="flex max-w-prose flex-col gap-2 text-sm"
    :data-testid="`narrative-${group ?? 'holistic'}`"
  >
    <p v-for="paragraph in paragraphs" :key="paragraph">{{ paragraph }}</p>
  </div>
</template>

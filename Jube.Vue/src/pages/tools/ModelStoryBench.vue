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
import { useRoute, useRouter } from 'vue-router'
import ModelStory from '../../components/ModelStory.vue'

const route = useRoute()
const router = useRouter()

const initial = typeof route.query.path === 'string' ? route.query.path : undefined

function remember(text: string): void {
  void router.replace({ query: { ...route.query, path: text === '' ? undefined : text } })
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto" data-testid="model-story-bench">
    <ModelStory :path="initial" @path="remember" />
  </div>
</template>

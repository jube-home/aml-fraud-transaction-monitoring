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
import {computed, provide, reactive, ref} from 'vue'
import {useI18n} from 'vue-i18n'
import {Search} from 'lucide-vue-next'
import {hubDeckKey} from './hubDeck'

defineProps<{ bare?: boolean }>()

const {t} = useI18n()

const query = ref('')
const tiles = reactive(new Map<symbol, boolean>())
const container = ref<HTMLElement>()

provide(hubDeckKey, {
  query,
  report: (key, visible) => tiles.set(key, visible),
  forget: (key) => tiles.delete(key),
})

const visibleCount = computed(() => [...tiles.values()].filter(Boolean).length)
const noMatch = computed(() => query.value.trim() !== '' && tiles.size > 0 && visibleCount.value === 0)

function openFirst(): void {
  const buttons = container.value?.querySelectorAll<HTMLButtonElement>('button[data-testid^="hub-open-"]')

  for (const button of buttons ?? []) {
    if (button.offsetParent !== null) {
      button.click()

      return
    }
  }
}
</script>

<template>
  <template v-if="bare">
    <slot/>
  </template>
  <div
    v-else
    key="deck"
    ref="container"
    class="flex flex-col gap-2 rounded-box border border-base-300 bg-base-200/40 p-2"
  >
    <label class="input input-sm w-full">
      <Search aria-hidden="true" class="size-4 shrink-0 opacity-70"/>
      <input
        v-model="query"
        type="search"
        class="grow"
        :aria-label="t('hierarchy.find')"
        :placeholder="t('hierarchy.find')"
        data-testid="hub-search"
        @keydown.enter.prevent="openFirst"
        @keydown.esc="query = ''"
      />
    </label>
    <slot/>
    <p v-if="noMatch" class="py-2 text-center text-sm text-secondary" role="status" data-testid="hub-no-match">
      {{ t('hierarchy.noMatch') }}
    </p>
  </div>
</template>

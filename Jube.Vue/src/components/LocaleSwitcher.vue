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
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Check, ChevronDown, Languages } from 'lucide-vue-next'
import { useUiStore } from '../stores/ui'
import { loadLocale } from '../i18n'

const composer = useI18n()
const { t, locale } = composer
const ui = useUiStore()
const panel = ref<HTMLElement>()

const languages = computed(() => [
  { code: 'en', short: 'EN', name: t('locale.en') },
  { code: 'pl', short: 'PL', name: t('locale.pl') },
  { code: 'ar', short: 'AR', name: t('locale.ar') },
])

const current = computed(
  () => languages.value.find((language) => language.code === locale.value) ?? languages.value[0]!,
)

async function choose(next: string): Promise<void> {
  panel.value?.hidePopover?.()
  ui.setLocale(next)
  await loadLocale(composer as never, next)
}
</script>

<template>
  <button
    type="button"
    class="btn btn-sm gap-1.5 rounded-full"
    popovertarget="locale-menu"
    style="anchor-name: --locale-menu"
    :aria-label="`${current.short} ${t('prefs.language')}`"
    data-testid="locale-trigger"
  >
    <Languages aria-hidden="true" class="size-4" />
    <span class="font-mono">{{ current.short }}</span>
    <ChevronDown aria-hidden="true" class="size-3.5 opacity-70" />
  </button>
  <ul
    id="locale-menu"
    ref="panel"
    popover
    class="dropdown dropdown-end menu z-50 mt-2 max-h-80 w-56 flex-nowrap overflow-y-auto rounded-box bg-base-100 p-2 shadow-md"
    style="position-anchor: --locale-menu"
  >
    <li v-for="language in languages" :key="language.code">
      <button
        type="button"
        :lang="language.code"
        :aria-current="language.code === locale ? 'true' : undefined"
        :class="{ 'bg-base-200! font-semibold text-base-content!': language.code === locale }"
        :data-testid="`locale-${language.code}`"
        @click="choose(language.code)"
      >
        <span class="flex-1 text-start">{{ language.name }}</span>
        <Check v-if="language.code === locale" aria-hidden="true" class="size-4" />
        <span class="badge badge-ghost badge-xs font-mono">{{ language.short }}</span>
      </button>
    </li>
  </ul>
</template>

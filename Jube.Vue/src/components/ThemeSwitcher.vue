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
import { useI18n } from 'vue-i18n'
import { Monitor, Moon, Sun } from 'lucide-vue-next'
import { useUiStore, type ThemePreference } from '../stores/ui'

const { t } = useI18n()
const ui = useUiStore()

function choose(value: ThemePreference): void {
  ui.setTheme(value)
}
</script>

<template>
  <div
    role="group"
    :aria-label="t('prefs.theme')"
    class="inline-flex items-center gap-0.5 rounded-full border border-base-300 bg-base-200 p-0.5"
  >
    <button
      type="button"
      class="btn btn-xs btn-circle"
      :class="ui.theme === 'system' ? 'btn-primary' : 'btn-ghost'"
      :aria-pressed="ui.theme === 'system'"
      data-testid="theme-system"
      @click="choose('system')"
    >
      <Monitor aria-hidden="true" class="size-4" />
      <span class="sr-only">{{ t('prefs.themeSystem') }}</span>
    </button>
    <button
      type="button"
      class="btn btn-xs btn-circle"
      :class="ui.theme === 'light' ? 'btn-primary' : 'btn-ghost'"
      :aria-pressed="ui.theme === 'light'"
      data-testid="theme-light"
      @click="choose('light')"
    >
      <Sun aria-hidden="true" class="size-4" />
      <span class="sr-only">{{ t('prefs.themeLight') }}</span>
    </button>
    <button
      type="button"
      class="btn btn-xs btn-circle"
      :class="ui.theme === 'dark' ? 'btn-primary' : 'btn-ghost'"
      :aria-pressed="ui.theme === 'dark'"
      data-testid="theme-dark"
      @click="choose('dark')"
    >
      <Moon aria-hidden="true" class="size-4" />
      <span class="sr-only">{{ t('prefs.themeDark') }}</span>
    </button>
  </div>
</template>

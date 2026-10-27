/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

import { computed, ref } from 'vue'
import { defineStore } from 'pinia'

export type ThemePreference = 'system' | 'light' | 'dark'

const themeKey = 'jube.theme'
const localeKey = 'jube.locale'

function readStored(key: string): string | undefined {
  try {
    return localStorage.getItem(key) ?? undefined
  } catch {
    return undefined
  }
}

function writeStored(key: string, value: string): void {
  try {
    localStorage.setItem(key, value)
  } catch {
    // Storage can be unavailable; the preference then lasts for the page only.
  }
}

function prefersDark(): boolean {
  return typeof matchMedia === 'function' && matchMedia('(prefers-color-scheme: dark)').matches
}

export const useUiStore = defineStore('ui', () => {
  const storedTheme = readStored(themeKey)
  const theme = ref<ThemePreference>(
    storedTheme === 'light' || storedTheme === 'dark' ? storedTheme : 'system',
  )
  const locale = ref<string>(readStored(localeKey) ?? 'en')

  const resolvedTheme = computed<'jube' | 'jube-dark'>(() => {
    if (theme.value === 'system') return prefersDark() ? 'jube-dark' : 'jube'

    return theme.value === 'dark' ? 'jube-dark' : 'jube'
  })

  function applyTheme(): void {
    document.documentElement.dataset.theme = resolvedTheme.value
  }

  function setTheme(next: ThemePreference): void {
    theme.value = next
    writeStored(themeKey, next)
    applyTheme()
  }

  function setLocale(next: string): void {
    locale.value = next
    writeStored(localeKey, next)
  }

  return {
    theme,
    locale,
    resolvedTheme,
    applyTheme,
    setTheme,
    setLocale,
  }
})

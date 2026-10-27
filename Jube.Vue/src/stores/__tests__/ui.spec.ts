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

import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useUiStore } from '../ui'

function stubColourScheme(dark: boolean): void {
  vi.stubGlobal(
    'matchMedia',
    vi.fn().mockReturnValue({
      matches: dark,
      addEventListener: vi.fn<() => void>(),
      removeEventListener: vi.fn<() => void>(),
    }),
  )
}

describe('ui store', () => {
  beforeEach(() => {
    localStorage.clear()
    delete document.documentElement.dataset.theme
    setActivePinia(createPinia())
    stubColourScheme(false)
  })

  it('defaults to the system theme and English', () => {
    const ui = useUiStore()

    expect(ui.theme).toBe('system')
    expect(ui.locale).toBe('en')
    expect(ui.resolvedTheme).toBe('jube')
  })

  it('resolves the system theme from the colour-scheme preference', () => {
    stubColourScheme(true)

    expect(useUiStore().resolvedTheme).toBe('jube-dark')
  })

  it('applies and persists an explicit theme', () => {
    const ui = useUiStore()

    ui.setTheme('dark')

    expect(document.documentElement.dataset.theme).toBe('jube-dark')
    expect(localStorage.getItem('jube.theme')).toBe('dark')
  })

  it('restores a stored theme and locale', () => {
    localStorage.setItem('jube.theme', 'dark')
    localStorage.setItem('jube.locale', 'pl')

    const ui = useUiStore()

    expect(ui.theme).toBe('dark')
    expect(ui.locale).toBe('pl')
  })

  it('ignores an unknown stored theme', () => {
    localStorage.setItem('jube.theme', 'hotpink')

    expect(useUiStore().theme).toBe('system')
  })

  it('still works when storage throws', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('blocked')
    })
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('blocked')
    })

    const ui = useUiStore()
    ui.setTheme('light')
    ui.setLocale('pl')

    expect(ui.theme).toBe('light')
    expect(ui.locale).toBe('pl')

    vi.restoreAllMocks()
  })
})

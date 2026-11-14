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

import {readFileSync} from 'node:fs'
import {join} from 'node:path'
import {describe, expect, it} from 'vitest'
import en from '../../locales/en.json'
import {supportedLocales} from '../../i18n'

const source = readFileSync(join(import.meta.dirname, '..', 'LocaleSwitcher.vue'), 'utf8')

describe('the language switcher', () => {
  it('lists every supported locale from the one source of truth, never a hand-written subset', () => {
    expect(source).toContain('supportedLocales.map')
    expect(source).not.toMatch(/code: '(en|pl|ar|ka|id|sv|ur|hi|fr)'/)
  })

  it.each(supportedLocales)('has a native name for %s', (code) => {
    expect((en.locale as Record<string, string>)[code]).toBeTruthy()
  })
})

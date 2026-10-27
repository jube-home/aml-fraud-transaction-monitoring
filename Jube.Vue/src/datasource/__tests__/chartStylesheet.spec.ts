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

import { afterEach, describe, expect, it } from 'vitest'
import { adoptStylesheet, knownStylesheetVersion, resetStylesheet } from '../chartStylesheet'
import { styleMode } from '../types'

afterEach(resetStylesheet)

describe('adoptStylesheet', () => {
  it('installs the css and remembers the version', () => {
    adoptStylesheet({ modeId: styleMode.external, version: 'v1', css: '.a{}' })

    expect(knownStylesheetVersion()).toBe('v1')
    expect(document.getElementById('jube-chart-stylesheet')?.textContent).toBe('.a{}')
  })

  it('keeps the installed css when the server omits it for the same version', () => {
    adoptStylesheet({ modeId: styleMode.external, version: 'v1', css: '.a{}' })
    adoptStylesheet({ modeId: styleMode.external, version: 'v1', css: null })

    expect(document.getElementById('jube-chart-stylesheet')?.textContent).toBe('.a{}')
    expect(knownStylesheetVersion()).toBe('v1')
  })

  it('replaces the css when the version changes', () => {
    adoptStylesheet({ modeId: styleMode.external, version: 'v1', css: '.a{}' })
    adoptStylesheet({ modeId: styleMode.external, version: 'v2', css: '.b{}' })

    expect(document.getElementById('jube-chart-stylesheet')?.textContent).toBe('.b{}')
    expect(knownStylesheetVersion()).toBe('v2')
  })

  it('ignores inline styles', () => {
    adoptStylesheet({ modeId: styleMode.inline, version: 'v1', css: null })

    expect(knownStylesheetVersion()).toBeNull()
    expect(document.getElementById('jube-chart-stylesheet')).toBeNull()
  })
})

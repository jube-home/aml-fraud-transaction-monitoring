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
import { mountVapor, type MountedVapor } from '../../test-support/mountVapor'
import JubeMarkup from '../JubeMarkup.vue'

let mounted: MountedVapor | undefined

afterEach(() => mounted?.unmount())

function render(html: string): HTMLElement {
  mounted = mountVapor(JubeMarkup, { props: { html, label: 'Template' } })

  return mounted.root.querySelector('[data-testid="datasource-markup"]') as HTMLElement
}

describe('JubeMarkup', () => {
  it('draws allowed markup', () => {
    const host = render('<ul class="x"><li>a</li><li>b</li></ul>')

    expect(host.querySelectorAll('li')).toHaveLength(2)
    expect(host.querySelector('ul')?.getAttribute('class')).toBe('x')
  })

  it('removes scripts and unlisted tags even when the server should never send them', () => {
    const host = render(
      '<p>ok</p><script>window.hacked = 1</script><iframe></iframe><a href="x">y</a>',
    )

    expect(host.querySelector('script')).toBeNull()
    expect(host.querySelector('iframe')).toBeNull()
    expect(host.querySelector('a')).toBeNull()
    expect(host.textContent).toContain('ok')
  })

  it('strips event handlers, styles and other attributes', () => {
    const host = render(
      '<div onclick="x()" style="color:red" id="i" data-kind="k" aria-label="l">a</div>',
    )
    const div = host.querySelector('div')!

    expect(div.hasAttribute('onclick')).toBe(false)
    expect(div.hasAttribute('style')).toBe(false)
    expect(div.hasAttribute('id')).toBe(false)
    expect(div.getAttribute('data-kind')).toBe('k')
    expect(div.getAttribute('aria-label')).toBe('l')
  })

  it('keeps text inside removed containers out of the page', () => {
    const host = render('<svg><text>boom</text></svg><p>safe</p>')

    expect(host.querySelector('svg')).toBeNull()
    expect(host.textContent).toContain('safe')
  })
})

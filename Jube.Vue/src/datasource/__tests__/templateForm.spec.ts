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

import { describe, expect, it } from 'vitest'
import { apply, clean, collect, decorate, showErrors } from '../templateForm'

function host(html: string): HTMLElement {
  const parsed = new DOMParser().parseFromString(html, 'text/html')

  clean(parsed.body)

  const root = document.createElement('div')

  root.replaceChildren(...parsed.body.childNodes)

  return root
}

describe('templateForm', () => {
  it('keeps the form vocabulary and drops everything else', () => {
    const root = host(
      '<form><label for="a">A</label><input name="a" onfocus="x()" id="z" style="color:red"><script>1</script><a href="x">y</a></form>',
    )

    expect(root.querySelector('script')).toBeNull()
    expect(root.querySelector('a')).toBeNull()

    const input = root.querySelector('input')!

    expect(input.getAttribute('name')).toBe('a')
    expect(input.hasAttribute('onfocus')).toBe(false)
    expect(input.hasAttribute('id')).toBe(false)
    expect(input.hasAttribute('style')).toBe(false)
  })

  it('turns an unknown input type into text', () => {
    const root = host('<input name="a" type="password"><input name="b" type="file">')

    expect([...root.querySelectorAll('input')].map((input) => input.type)).toEqual(['text', 'text'])
  })

  it('styles the fields, gives each an id and points labels at them', () => {
    const root = host(
      '<label for="a">A</label><input name="a"><input type="radio" name="r" value="x"><select name="s"><option value="1">1</option></select>',
    )

    decorate(root, 'p')

    expect(root.querySelector('input[name="a"]')?.className).toContain('input')
    expect(root.querySelector('select')?.className).toContain('select')
    expect(root.querySelector('label')?.getAttribute('for')).toBe('p-a')
    expect(root.querySelector('input[type="radio"]')?.id).toBe('p-r-x')
  })

  it('collects what was typed, ticked and chosen, skipping blanks', () => {
    const root = host(
      '<input name="a"><input name="b"><input type="checkbox" name="c"><input type="radio" name="d" value="x"><input type="radio" name="d" value="y"><select name="e"><option value="p">P</option><option value="q">Q</option></select><textarea name="f"></textarea>',
    )

    apply(root, { a: 'one', c: 'true', d: 'y', e: 'q', f: 'long' })

    expect(collect(root)).toEqual({ a: 'one', c: 'true', d: 'y', e: 'q', f: 'long' })
  })

  it('reports an unticked box as false', () => {
    expect(collect(host('<input type="checkbox" name="c">'))).toEqual({ c: 'false' })
  })

  it('shows a message beside a field and clears it again', () => {
    const root = host('<input name="a">')

    decorate(root, 'p')
    showErrors(root, { a: 'Not valid' })

    expect(root.querySelector('[data-field-error="a"]')?.textContent).toBe('Not valid')
    expect(root.querySelector('input')?.getAttribute('aria-invalid')).toBe('true')

    showErrors(root, {})

    expect(root.querySelector('[data-field-error]')).toBeNull()
    expect(root.querySelector('input')?.hasAttribute('aria-invalid')).toBe(false)
  })
})

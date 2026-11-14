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

import {describe, expect, it} from 'vitest'
import {renderTaskLists} from '../taskLists'

function host(html: string): HTMLElement {
  const element = document.createElement('div')

  element.innerHTML = html

  return element
}

describe('renderTaskLists', () => {
  it('turns task markers into disabled checkboxes and strikes the done ones', () => {
    const root = host('<ul><li>[ ] open</li><li>[x] shipped</li><li>plain</li></ul>')

    renderTaskLists(root)

    const items = root.querySelectorAll('li')
    const boxes = root.querySelectorAll<HTMLInputElement>('input[type=checkbox]')

    expect(boxes).toHaveLength(2)
    expect(boxes[0]!.checked).toBe(false)
    expect(boxes[1]!.checked).toBe(true)
    expect(boxes[0]!.disabled).toBe(true)
    expect(boxes[1]!.getAttribute('aria-label')).toBe('shipped')
    expect(items[1]!.className).toContain('jooby-task-done')
    expect(items[0]!.textContent).toBe('open')
    expect(items[2]!.querySelector('input')).toBeNull()
  })

  it('handles loose items wrapped in a paragraph and does not repeat', () => {
    const root = host('<ul><li><p>[X] wrapped</p></li></ul>')

    renderTaskLists(root)
    renderTaskLists(root)

    expect(root.querySelectorAll('input')).toHaveLength(1)
    expect(root.querySelector('p')!.textContent).toBe('wrapped')
  })

  it('leaves ordered lists and bracketed text elsewhere alone', () => {
    const root = host('<ol><li>[ ] numbered</li></ol><ul><li>see [x] later</li></ul>')

    renderTaskLists(root)

    expect(root.querySelector('input')).toBeNull()
  })
})

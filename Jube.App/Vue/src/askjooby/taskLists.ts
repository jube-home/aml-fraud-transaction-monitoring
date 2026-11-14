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

const marker = /^\s*\[([ xX])\]\s+/

export function renderTaskLists(root: ParentNode): void {
  root.querySelectorAll<HTMLLIElement>('ul > li:not(.jooby-task)').forEach((item) => {
    const walker = item.ownerDocument.createTreeWalker(item, NodeFilter.SHOW_TEXT)
    const first = walker.nextNode()

    if (!first || !marker.test(first.nodeValue ?? '')) return

    const done = marker.exec(first.nodeValue!)![1] !== ' '

    first.nodeValue = first.nodeValue!.replace(marker, '')

    const box = item.ownerDocument.createElement('input')

    box.type = 'checkbox'
    box.disabled = true
    box.checked = done
    box.setAttribute('aria-label', (item.textContent ?? '').trim().split('\n')[0] ?? '')
    item.prepend(box)
    item.classList.add('jooby-task')
    if (done) item.classList.add('jooby-task-done')
  })
}

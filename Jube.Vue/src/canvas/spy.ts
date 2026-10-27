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
export interface SectionBox {
  id: string
  top: number
}

export function activeSection(
  sections: SectionBox[],
  offset: number,
  atEnd = false,
): string | null {
  if (sections.length === 0) return null

  if (atEnd) return sections[sections.length - 1]!.id

  let active = sections[0]!.id

  for (const section of sections) {
    if (section.top <= offset) active = section.id
  }

  return active
}

export function scrollParent(element: Element | null): Element | null {
  let node = element?.parentElement ?? null

  while (node) {
    const { overflowY } = getComputedStyle(node)

    if (/(auto|scroll)/.test(overflowY) && node.scrollHeight > node.clientHeight) return node

    node = node.parentElement
  }

  return null
}

export function isAtEnd(element: Element | null): boolean {
  const parent = scrollParent(element)

  if (parent) return parent.scrollTop + parent.clientHeight >= parent.scrollHeight - 2

  const root = document.scrollingElement

  return (
    root !== null &&
    root.scrollHeight > root.clientHeight &&
    root.scrollTop + root.clientHeight >= root.scrollHeight - 2
  )
}

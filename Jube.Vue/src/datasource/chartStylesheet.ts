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

import type { CardStyle } from './types'
import { styleMode } from './types'

const elementId = 'jube-chart-stylesheet'

let known: string | null = null

export function knownStylesheetVersion(): string | null {
  return known
}

export function resetStylesheet(): void {
  known = null
  document.getElementById(elementId)?.remove()
}

export function adoptStylesheet(style: CardStyle): void {
  if (style.modeId !== styleMode.external || !style.version) return

  if (style.css) {
    let element = document.getElementById(elementId)

    if (!element) {
      element = document.createElement('style')
      element.id = elementId
      document.head.append(element)
    }

    element.textContent = style.css
    element.setAttribute('data-version', style.version)
    known = style.version

    return
  }

  if (document.getElementById(elementId)?.getAttribute('data-version') === style.version) {
    known = style.version
  }
}

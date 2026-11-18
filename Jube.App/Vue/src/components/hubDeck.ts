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

import type {InjectionKey, Ref} from 'vue'

export interface HubDeckContext {
  query: Ref<string>
  report(key: symbol, visible: boolean): void
  forget(key: symbol): void
}

export const hubDeckKey: InjectionKey<HubDeckContext> = Symbol('hubDeck')

export function hubMatches(query: string, ...fields: Array<string | undefined>): boolean {
  const words = query.toLowerCase().split(/\s+/).filter(Boolean)

  if (words.length === 0) return true

  const haystack = fields.join(' ').toLowerCase()

  return words.every((word) => haystack.includes(word))
}

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

import { ref } from 'vue'
import { defineStore } from 'pinia'

export interface JoobyPageContext {
  pageName: string
  state?: string
  record?: unknown
}

export const useJoobyContextStore = defineStore('joobyContext', () => {
  const context = ref<JoobyPageContext>({ pageName: 'default' })

  function set(next: JoobyPageContext): void {
    context.value = next
  }

  function serialised(): string {
    return JSON.stringify(context.value, null, 2)
  }

  function storageKey(): string {
    const record = context.value.record ? `:${JSON.stringify(context.value.record)}` : ''

    return `jube.askjooby.widget.${context.value.pageName}${record}`
  }

  return { context, set, serialised, storageKey }
})

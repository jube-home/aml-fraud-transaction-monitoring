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

import {computed, ref, toRaw} from 'vue'
import {defineStore} from 'pinia'

export interface JoobyPageContext {
  pageName: string
  state?: string
  record?: unknown
}

export interface JoobyRouteContext {
  routeName: string
  path: string
  title?: string
  params?: Record<string, unknown>
  query?: Record<string, unknown>
}

export const useJoobyContextStore = defineStore('joobyContext', () => {
  const route = ref<JoobyRouteContext>({routeName: 'default', path: '/'})
  const page = ref<JoobyPageContext>()

  const context = computed<JoobyPageContext & { location: JoobyRouteContext }>(() => ({
    pageName: page.value?.pageName ?? route.value.routeName,
    state: page.value?.state,
    record: page.value?.record,
    location: route.value,
  }))

  function set(next: JoobyPageContext): void {
    page.value = next
  }

  function clear(owner?: JoobyPageContext): void {
    if (owner === undefined || toRaw(page.value) === owner) page.value = undefined
  }

  function setRoute(next: JoobyRouteContext, reset: boolean): void {
    route.value = next
    if (reset) page.value = undefined
  }

  function serialised(): string {
    return JSON.stringify(context.value, null, 2)
  }

  function storageKey(): string {
    const record = page.value?.record ? `:${JSON.stringify(page.value.record)}` : ''

    return `jube.askjooby.widget.${context.value.pageName}${record}`
  }

  return {context, set, clear, setRoute, serialised, storageKey}
})

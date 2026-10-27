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

import type { BuilderGroup } from './types'

export interface QueryBuilderRule {
  id: string
  $el: JQuery
  filter: { id: string; input?: string }
  operator: { type: string; nb_inputs: number }
}

export interface QueryBuilderEvent extends JQuery.Event {
  value?: unknown
}

export interface QueryBuilderSettings {
  plugins?: string[]
  filters: object[]
  operators: object[]
  lang: Record<string, unknown>
  icons: Record<string, string>
  rules?: unknown
}

export interface QueryBuilderNode {
  rules?: QueryBuilderNode[]
  filter?: unknown
}

export interface QueryBuilderSql {
  sql: string
  params: unknown[]
}

export interface QueryBuilderPlugin {
  queryBuilder(settings: QueryBuilderSettings): JQuery
  queryBuilder(
    method: 'getRules',
    options?: { allow_invalid?: boolean; skip_empty?: boolean },
  ): BuilderGroup | null
  queryBuilder(method: 'getSQL', format: string, allowInvalid?: boolean): QueryBuilderSql | null
  queryBuilder(method: 'getModel'): QueryBuilderNode
  queryBuilder(method: 'validate'): boolean
  queryBuilder(method: 'setRules', rules: BuilderGroup): void
  queryBuilder(method: 'destroy'): void
  data(key: 'queryBuilder'): { filters?: unknown[] } | undefined
}

const scriptPath = '/vendor/query-builder/query-builder.standalone.min.js'

let loading: Promise<JQueryStatic> | undefined

function loadScript(src: string): Promise<void> {
  return new Promise((resolve, reject) => {
    const script = document.createElement('script')

    script.src = src
    script.onload = () => resolve()
    script.onerror = () => reject(new Error(`Could not load ${src}`))
    document.head.append(script)
  })
}

export function loadQueryBuilder(): Promise<JQueryStatic> {
  loading ??= (async () => {
    const { default: jquery } = await import('jquery')
    const global = window as unknown as { jQuery?: JQueryStatic; $?: JQueryStatic }

    global.jQuery = jquery
    global.$ = jquery

    await loadScript(scriptPath)

    delete global.jQuery
    delete global.$

    return jquery
  })().catch((error: unknown) => {
    loading = undefined
    throw error
  })

  return loading
}

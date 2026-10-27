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
import type { Page } from '@playwright/test'

export interface ModelOverrides {
  area: string
  endpoint?: string
  approval?: { kind: number; historyRoute: string } | null
  roles?: { endpoint: string; parentKey: string } | null
  children?: {
    area: string
    label: string
    hub: string
    order: number
    parentKey: string
    parentField: 'id' | 'guid'
    listRoute: string
  }[]
  keys?: { id: string; parent: string | null; naturalKey: string | null }
  fields?: { name: string; type: string }[]
  lockFields?: string[]
  values?: {
    area: string
    endpoint: string
    parentField: string
    parentKind: number
    fields: { field: string; title: string; type: 'string' | 'number' }[]
  } | null
}

function fieldOf(field: { name: string; type: string }, index: number) {
  return {
    label: field.name,
    description: '',
    nullable: true,
    required: false,
    minLength: null,
    maxLength: null,
    min: null,
    max: null,
    exclusiveMin: false,
    exclusiveMax: false,
    patterns: [],
    options: [],
    group: '',
    order: (index + 1) * 10,
    widget: 'default',
    span: 1,
    readOnly: false,
    placeholder: null,
    help: null,
    editor: null,
    lookup: null,
    newDefault: null,
    visibleWhen: [],
    requiredWhen: [],
    enabledWhen: [],
    ...field,
  }
}

export function modelOf(overrides: ModelOverrides) {
  return {
    area: overrides.area,
    endpoint: overrides.endpoint ?? `/api/${overrides.area}`,
    operations: ['list', 'read', 'create', 'update', 'delete'],
    approval: overrides.approval ?? null,
    roles: overrides.roles ?? null,
    values: overrides.values ?? null,
    keys: overrides.keys ?? { id: 'id', parent: null, naturalKey: null },
    children: overrides.children ?? [],
    editors: [],
    lockFields: overrides.lockFields ?? [],
    groups: [],
    fields: (overrides.fields ?? []).map((field, index) => fieldOf(field, index)),
    listColumns: [],
  }
}

export async function stubFormModels(page: Page, models: ReturnType<typeof modelOf>[]) {
  await page.route('**/api/FormModel', (route) =>
    route.fulfill({ json: models.map((model) => model.area) }),
  )
  await page.route('**/api/FormModel/*', (route) => {
    const area = decodeURIComponent(new URL(route.request().url()).pathname.split('/').pop()!)
    const model = models.find((candidate) => candidate.area === area)

    return model ? route.fulfill({ json: model }) : route.fulfill({ status: 404, body: '' })
  })
}

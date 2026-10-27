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
import type { Me } from '../../auth/me'
import { visibleMenu } from '../menu'
import type { MenuNode } from '../menuTree'

function me(overrides: Partial<Me>): Me {
  return {
    userName: 'alex',
    landlord: false,
    permissions: [],
    tenantRegistryId: 1,
    canChangePassword: true,
    askJoobyInference: true,
    sessionExpiry: null,
    ...overrides,
  }
}

const keys = (value: Me | undefined): string[] =>
  visibleMenu(value)
    .map((node) => node.key)
    .filter((key) => key !== 'tools' && key !== 'askJooby')

function flatten(nodes: MenuNode[]): MenuNode[] {
  return nodes.flatMap((node) => [node, ...flatten(node.children ?? [])])
}

const keysUnder = (value: Me, key: string): string[] =>
  flatten(visibleMenu(value).find((node) => node.key === key)?.children ?? []).map(
    (node) => node.key,
  )

describe('visibleMenu', () => {
  it('offers Ask Jooby and the temporary Tools entry to every signed-in user', () => {
    expect(visibleMenu(me({})).map((node) => node.key)).toEqual(['askJooby', 'tools'])
    expect(visibleMenu(undefined)).toEqual([])
  })

  it('shows nothing else before the user is known or without permissions', () => {
    expect(keys(undefined)).toEqual([])
    expect(keys(me({}))).toEqual([])
  })

  it('shows a top-level entry only for a permission that grants it', () => {
    expect(keys(me({ permissions: [1, 29, 3, 4] }))).toEqual(['cases', 'models', 'sanctions'])
  })

  it('offers Models as one entry that opens the journey, with no tree under it', () => {
    const models = visibleMenu(me({ permissions: [6] })).find((node) => node.key === 'models')

    expect(models?.routeName).toBe('models')
    expect(models?.children).toBeUndefined()
  })

  it('lets Lists and Dictionary permissions reach Models, where they are now cards', () => {
    expect(keys(me({ permissions: [3] }))).toEqual(['models'])
    expect(keys(me({ permissions: [4] }))).toEqual(['models'])
  })

  it('has no Advanced Analytics entry of its own, because it lives under a model', () => {
    expect(keys(me({ permissions: [47] }))).toEqual(['models'])
  })

  it('shows a group only with the children its permissions allow', () => {
    expect(keysUnder(me({ permissions: [35] }), 'admin')).toEqual(['admin-security'])
    expect(keysUnder(me({ permissions: [38] }), 'admin')).toEqual(['admin-preservation'])
    expect(keysUnder(me({ permissions: [31] }), 'admin')).toEqual(['admin-visualisations'])
  })

  it('hides a group none of whose children are allowed', () => {
    expect(keys(me({ permissions: [99] }))).toEqual([])
  })

  it('gives a landlord Administration, including the landlord-only entries', () => {
    expect(keysUnder(me({ landlord: true, permissions: [34] }), 'admin')).toEqual([
      'admin-tenants',
      'admin-security',
      'admin-performance',
      'admin-telemetry',
      'admin-sanctions-loader',
    ])
  })

  it('keeps the landlord-only entries from everyone else', () => {
    const found = keysUnder(me({ permissions: [34, 35, 36] }), 'admin')

    expect(found).toEqual(['admin-security'])
  })

  it('keeps Administration one level deep, every entry a routed page', () => {
    const entries = visibleMenu(
      me({ landlord: true, permissions: Array.from({ length: 70 }, (_, i) => i + 1) }),
    ).find((node) => node.key === 'admin')?.children

    expect(entries?.length).toBe(7)
    expect(entries?.every((node) => node.routeName && !node.children)).toBe(true)
  })
})

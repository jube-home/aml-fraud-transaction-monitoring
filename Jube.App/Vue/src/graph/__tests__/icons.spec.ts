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
import { lucideIcons } from '../lucideIcons.generated'
import { iconComponent, iconNameForNodeKey, nodeIcon, nodeIconMap } from '../icons'

describe('graph icons', () => {
  it.each([
    ['AccountId', 'landmark'],
    ['ToAccountId', 'landmark'],
    ['DeviceId', 'smartphone'],
    ['IpAddress', 'globe'],
    ['Network', 'globe'],
    ['Email', 'mail'],
    ['CardNumber', 'credit-card'],
    ['Case', 'briefcase'],
    ['Payee', 'hand-coins'],
    ['Banana', 'circle-dot'],
    ['', 'circle-dot'],
  ])('chooses an icon for the node key %s by its name, as the server does', (key, expected) => {
    expect(iconNameForNodeKey(key)).toBe(expected)
  })

  it('has a component for every icon the fallback can choose', () => {
    for (const key of [
      'Case',
      'AccountId',
      'Payee',
      'DeviceId',
      'Ip',
      'Email',
      'Phone',
      'Card',
      'Merchant',
      'Customer',
      'Company',
      'Address',
      'Wallet',
      'Document',
      'Session',
      'Tag',
      'Unknown',
    ]) {
      expect(iconComponent(iconNameForNodeKey(key))).not.toBeNull()
    }
  })

  it('refuses names outside the generated set', () => {
    expect(iconComponent('not-a-real-icon')).toBeNull()
    expect(iconComponent('constructor')).toBeNull()
    expect(iconComponent(null)).toBeNull()
  })

  it('carries the curated set', () => {
    expect(Object.keys(lucideIcons).length).toBeGreaterThan(150)
  })

  it('lets the first definition that names an icon win for a node type', () => {
    const map = nodeIconMap([
      {
        fromNodeKey: 'AccountId',
        toNodeKey: 'DeviceId',
        fromNodeIcon: null,
        toNodeIcon: 'tablet-smartphone',
      },
      {
        fromNodeKey: 'AccountId',
        toNodeKey: 'DeviceId',
        fromNodeIcon: 'wallet',
        toNodeIcon: 'smartphone',
      },
    ])

    expect(map.get('AccountId')).toBe('wallet')
    expect(map.get('DeviceId')).toBe('tablet-smartphone')
  })

  it('ignores unknown configured names and falls back to the key name', () => {
    const map = nodeIconMap([
      { fromNodeKey: 'Email', toNodeKey: 'x', fromNodeIcon: '<script>', toNodeIcon: null },
    ])

    expect(map.size).toBe(0)
    expect(nodeIcon('Email', map)).toBe(iconComponent('mail'))
  })
})

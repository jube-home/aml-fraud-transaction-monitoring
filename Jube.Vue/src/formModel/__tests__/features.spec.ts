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
import { bodyOf, featureLayout, featureTitle, groupFeature } from '../features'
import type { GroupNode } from '../groups'
import type { FormCondition, FormField } from '../types'

const when = (property: string): FormCondition[] => [
  { property, op: 'equals', values: [true], matchMode: 'all' },
]

const field = (
  name: string,
  type = 'string',
  gates: { visible?: string; enabled?: string } = {},
): FormField =>
  ({
    name,
    type,
    visibleWhen: gates.visible ? when(gates.visible) : [],
    enabledWhen: gates.enabled ? when(gates.enabled) : [],
    requiredWhen: [],
  }) as unknown as FormField

const names = (fields: FormField[]): string[] => fields.map((candidate) => candidate.name)

describe('feature layout', () => {
  it('leaves fields that no switch gates as plain, switches included', () => {
    const layout = featureLayout([
      field('name'),
      field('active', 'boolean'),
      field('locked', 'boolean'),
    ])

    expect(names(layout.plain)).toEqual(['name', 'active', 'locked'])
    expect(layout.features).toEqual([])
  })

  it('gathers the fields a switch shows under that switch', () => {
    const layout = featureLayout([
      field('enableCase', 'boolean'),
      field('workflow', 'string', { visible: 'enableCase' }),
      field('key', 'string', { visible: 'enableCase' }),
    ])

    expect(layout.plain).toEqual([])
    expect(layout.features).toHaveLength(1)
    expect(layout.features[0]!.switchField.name).toBe('enableCase')
    expect(names(layout.features[0]!.fields)).toEqual(['workflow', 'key'])
  })

  it('treats a field enabled by a switch the same as one shown by it', () => {
    const layout = featureLayout([
      field('enableOverride', 'boolean'),
      field('overrideKey', 'string', { enabled: 'enableOverride' }),
    ])

    expect(names(layout.features[0]!.fields)).toEqual(['overrideKey'])
  })

  it('nests a switch gated by another and puts doubly gated fields under the inner one', () => {
    const layout = featureLayout([
      field('enableCase', 'boolean'),
      field('workflow', 'string', { visible: 'enableCase' }),
      field('enableBypass', 'boolean', { visible: 'enableCase' }),
      {
        ...field('sample', 'number', { visible: 'enableBypass' }),
        visibleWhen: [...when('enableCase'), ...when('enableBypass')],
      },
    ])

    const outer = layout.features[0]!

    expect(names(outer.fields)).toEqual(['workflow'])
    expect(outer.features).toHaveLength(1)
    expect(outer.features[0]!.switchField.name).toBe('enableBypass')
    expect(names(outer.features[0]!.fields)).toEqual(['sample'])
  })

  it('ignores a gate that is not a boolean field of the same group', () => {
    const layout = featureLayout([
      field('kind', 'integer'),
      field('detail', 'string', { visible: 'kind' }),
      field('other', 'string', { visible: 'elsewhere' }),
    ])

    expect(names(layout.plain)).toEqual(['kind', 'detail', 'other'])
  })

  it('makes a group a feature only when its one switch is all it holds', () => {
    const only = {
      path: 'Notification',
      label: 'Notification',
      collapsed: false,
      children: [],
      fields: [field('enable', 'boolean'), field('to', 'string', { visible: 'enable' })],
    } as GroupNode
    const mixed = { ...only, fields: [field('name'), ...only.fields] } as GroupNode

    expect(groupFeature(only)?.switchField.name).toBe('enable')
    expect(groupFeature(mixed)).toBeUndefined()
    expect(names(bodyOf(mixed).fields)).toEqual(['name'])
    expect(bodyOf(mixed).features).toHaveLength(1)
  })
})

describe('feature title', () => {
  const feature = (label: string) =>
    ({ switchField: { label }, fields: [], features: [] }) as unknown as Parameters<
      typeof featureTitle
    >[0]

  it('drops a leading Enable, which the card switch already says', () => {
    expect(featureTitle(feature('Enable bypass'))).toBe('Bypass')
    expect(featureTitle(feature('Send to activation watcher'))).toBe('Send to activation watcher')
    expect(featureTitle(feature('Enable'))).toBe('Enable')
  })
})

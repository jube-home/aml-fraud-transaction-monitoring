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
import {
  decodePath,
  encodePath,
  hubsOf,
  newLevel,
  rootAreas,
  summariseChildren,
} from '../hierarchy'
import type { FormChild, FormModel } from '../types'

const child = (area: string, hub: string, order: number): FormChild => ({
  area,
  label: area,
  hub,
  order,
  parentKey: 'parentId',
  parentField: 'id',
  listRoute: `/api/${area}/ByParentId`,
})

const model = (
  area: string,
  children: FormChild[] = [],
  extra: Partial<FormModel> = {},
): FormModel =>
  ({
    area,
    operations: ['list'],
    children,
    fields: [],
    lockFields: [],
    ...extra,
  }) as unknown as FormModel

describe('hierarchy roots and hubs', () => {
  it('finds the areas nothing sits under, those with children first', () => {
    const models = [
      model('Leaf'),
      model('Child'),
      model('Root', [child('Child', '', 1)]),
      model('Other'),
    ]

    expect(rootAreas(models)).toEqual(['Root', 'Leaf', 'Other'])
  })

  it('groups children by hub in order, keeping unnamed ones each on their own', () => {
    const hubs = hubsOf([
      child('B', 'References', 20),
      child('A', 'References', 10),
      child('C', '', 30),
    ])

    expect(hubs.map((hub) => [hub.hub, hub.children.map((c) => c.area)])).toEqual([
      ['References', ['A', 'B']],
      ['', ['C']],
    ])
  })
})

describe('hierarchy path', () => {
  it('writes the open record of each level and reads it back, dropping what it cannot read', () => {
    const levels = [
      { ...newLevel('Model', 'Model'), record: { id: 12, guid: undefined, name: 'a', values: {} } },
      {
        ...newLevel('CaseWorkflow', 'Workflows'),
        record: { id: 5, guid: undefined, name: 'b', values: {} },
      },
      newLevel('Xpath', 'XPath'),
    ]

    expect(encodePath(levels)).toBe('Model:12,CaseWorkflow:5')
    expect(decodePath('Model:12,CaseWorkflow:5')).toEqual([
      { area: 'Model', id: 12 },
      { area: 'CaseWorkflow', id: 5 },
    ])
    expect(decodePath('Model:x,:3,Ok:2')).toEqual([{ area: 'Ok', id: 2 }])
    expect(decodePath(undefined)).toEqual([])
  })
})

describe('child summary', () => {
  const withState = model('Child', [], {
    fields: [{ name: 'active', type: 'boolean' }] as FormModel['fields'],
    lockFields: ['locked'],
  })

  it('counts the active and locked children when the child has those states', () => {
    expect(
      summariseChildren(withState, [
        { active: true, locked: false },
        { active: true, locked: true },
        { active: false },
      ]),
    ).toEqual({ total: 3, active: 2, locked: 1 })
  })

  it('reports only the total when the child has no state to count', () => {
    expect(summariseChildren(model('Plain'), [{}, {}])).toEqual({
      total: 2,
      active: null,
      locked: null,
    })
    expect(summariseChildren(undefined, [])).toEqual({ total: 0, active: null, locked: null })
  })
})

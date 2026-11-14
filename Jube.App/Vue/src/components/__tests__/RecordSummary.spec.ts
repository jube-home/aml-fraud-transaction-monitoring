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

import {afterEach, describe, expect, it, vi} from 'vitest'
import {mountWith, type Harness} from '../../cases/checklist/__tests__/support'
import type {FormModel, RecordValues} from '../../formModel/types'
import RecordSummary from '../RecordSummary.vue'

const recordsOf =
  vi.fn<(model: FormModel, parentId: number | string | undefined) => Promise<RecordValues[]>>()

vi.mock('../../formModel/api', () => ({
  formModelApi: {
    model: (area: string) => Promise.resolve(formModel(area)),
    records: (model: FormModel, parentId: number | string | undefined) =>
      recordsOf(model, parentId),
  },
}))

function formModel(area: string, patch: Partial<FormModel> = {}): FormModel {
  return {
    area,
    endpoint: `/api/${area}`,
    operations: ['list', 'read'],
    keys: {id: 'id', naturalKey: 'name', parent: 'parentId'},
    lockFields: [],
    groups: [],
    fields: [
      {
        name: 'name',
        label: 'Name',
        type: 'string',
        options: [],
        group: '',
        order: 10,
        visibleWhen: [],
        enabledWhen: [],
      },
      {
        name: 'note',
        label: 'Note',
        type: 'string',
        options: [],
        group: '',
        order: 20,
        visibleWhen: [],
        enabledWhen: [],
      },
    ],
    listColumns: [{field: 'name', title: 'Name', order: 10}],
    children: [],
    ...patch,
  } as unknown as FormModel
}

const child = (area: string, order: number, parentField: 'id' | 'guid' = 'id') => ({
  area,
  label: area,
  hub: '',
  order,
  parentKey: 'parentId',
  parentField,
  listRoute: '',
})

let harness: Harness | undefined

afterEach(() => {
  harness?.unmount()
  harness = undefined
  vi.clearAllMocks()
})

describe('RecordSummary', () => {
  it('shows the facts the list leaves out and says nothing is below a leaf record', async () => {
    harness = await mountWith(RecordSummary, {
      model: formModel('Tag'),
      row: {id: 3, name: 'Urgent', note: 'Escalate'},
    })

    const summary = harness.byTestId('crud-summary-3')

    expect(summary?.textContent).toContain('Note')
    expect(summary?.textContent).toContain('Escalate')
    expect(summary?.textContent).not.toContain('Urgent')
    expect(harness.byTestId('crud-summary-3-children')).toBeNull()
    expect(recordsOf).not.toHaveBeenCalled()
  })

  it('says there is nothing more when a record has no facts and nothing below', async () => {
    harness = await mountWith(RecordSummary, {
      model: formModel('Tag'),
      row: {id: 3, name: 'Urgent'},
    })

    expect(harness.root.textContent).toContain('Nothing more to show for this record.')
  })

  it('leaves out the fields its grid says it already shows', async () => {
    harness = await mountWith(RecordSummary, {
      model: formModel('Tag'),
      row: {id: 3, name: 'Urgent', note: 'Escalate'},
      listed: ['note'],
    })

    expect(harness.byTestId('crud-summary-3')?.textContent).toContain('Urgent')
    expect(harness.byTestId('crud-summary-3')?.textContent).not.toContain('Escalate')
  })

  it('puts the facts in a card for each group the form declares', async () => {
    harness = await mountWith(RecordSummary, {
      model: formModel('Tag', {
        groups: [{path: 'Audit', label: 'Audit trail', order: 90, collapsed: true, widget: ''}],
        fields: [
          {
            name: 'name',
            label: 'Name',
            type: 'string',
            options: [],
            group: '',
            order: 10,
            visibleWhen: [],
            enabledWhen: [],
          },
          {
            name: 'note',
            label: 'Note',
            type: 'string',
            options: [],
            group: '',
            order: 20,
            visibleWhen: [],
            enabledWhen: [],
          },
          {
            name: 'maker',
            label: 'Maker',
            type: 'string',
            options: [],
            group: 'Audit',
            order: 10,
            visibleWhen: [],
            enabledWhen: [],
          },
        ],
      } as unknown as Partial<FormModel>),
      row: {id: 3, name: 'Urgent', note: 'Escalate', maker: 'alice'},
    })

    const loose = harness.byTestId('crud-summary-3-group-')
    const audit = harness.byTestId('crud-summary-3-group-Audit')

    expect(loose?.querySelector('h3')?.textContent).toContain('Properties')
    expect(loose?.textContent).toContain('Escalate')
    expect(loose?.textContent).not.toContain('alice')
    expect(audit?.querySelector('h3')?.textContent).toContain('Audit trail')
    expect(audit?.textContent).toContain('alice')
  })

  it('glimpses each kind of record below, in order, by the key each hangs off', async () => {
    recordsOf.mockResolvedValue([])
    harness = await mountWith(RecordSummary, {
      model: formModel('Model', {children: [child('Tag', 20, 'guid'), child('Rule', 10)]}),
      row: {id: 3, name: 'Fraud', guid: 'g-3'},
    })
    await harness.settle()

    const below = [...harness.root.querySelectorAll('li[data-testid^="glimpse-"]')].map((node) =>
      node.getAttribute('data-testid'),
    )

    expect(below).toEqual(['glimpse-Rule', 'glimpse-Tag'])
    expect(recordsOf).toHaveBeenCalledWith(expect.objectContaining({area: 'Rule'}), 3)
    expect(recordsOf).toHaveBeenCalledWith(expect.objectContaining({area: 'Tag'}), 'g-3')
  })

  it('opens a record below by its place under this one', async () => {
    recordsOf.mockResolvedValue([{id: 9, name: 'Velocity'}])
    harness = await mountWith(
      RecordSummary,
      {
        model: formModel('Model', {children: [child('Rule', 10)]}),
        row: {id: 3, name: 'Fraud'},
      },
      ['open'],
    )
    await harness.settle()
    ;(harness.byTestId('glimpse-Rule-toggle') as HTMLButtonElement).click()
    await harness.settle()
    ;(harness.byTestId('glimpse-Rule-open-9') as HTMLButtonElement).click()

    expect(harness.mounted.emitted.open).toEqual([
      [
        [
          {area: 'Model', id: 3},
          {area: 'Rule', id: 9},
        ],
      ],
    ])
  })
})

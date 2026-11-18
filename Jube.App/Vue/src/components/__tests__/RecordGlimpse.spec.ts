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
import type {FormChild, FormModel, RecordValues} from '../../formModel/types'
import RecordGlimpse from '../RecordGlimpse.vue'
import {forgetViewStates} from '../../story/viewState'

const modelOf = vi.fn<(area: string) => Promise<FormModel>>()
const recordsOf =
  vi.fn<(model: FormModel, parentId: number | string | undefined) => Promise<RecordValues[]>>()

vi.mock('../../formModel/api', () => ({
  formModelApi: {
    model: (area: string) => modelOf(area),
    records: (model: FormModel, parentId: number | string | undefined) =>
      recordsOf(model, parentId),
  },
}))

function child(area: string, patch: Partial<FormChild> = {}): FormChild {
  return {
    area,
    label: `${area}s`,
    hub: '',
    order: 10,
    parentKey: 'parentId',
    parentField: 'id',
    listRoute: '',
    ...patch,
  }
}

function formModel(area: string, children: FormChild[] = []): FormModel {
  return {
    area,
    endpoint: `/api/${area}`,
    operations: ['list'],
    keys: {id: 'id', naturalKey: 'name', parent: 'parentId'},
    lockFields: [],
    fields: [{name: 'active', type: 'boolean'}],
    children,
  } as unknown as FormModel
}

let harness: Harness | undefined

function click(testid: string): void {
  ;(harness!.byTestId(testid) as HTMLButtonElement).click()
}

afterEach(() => {
  forgetViewStates()
  harness?.unmount()
  harness = undefined
  vi.clearAllMocks()
})

describe('RecordGlimpse', () => {
  it('gives the count and the same facts as the cards before anything is opened', async () => {
    modelOf.mockResolvedValue({...formModel('Rule'), lockFields: ['locked']} as FormModel)
    recordsOf.mockResolvedValue([
      {id: 1, name: 'Velocity', active: true, locked: true},
      {id: 2, name: 'Amount', active: false, locked: false},
    ])
    harness = await mountWith(RecordGlimpse, {child: child('Rule'), parentValue: 7})
    await harness.settle()

    expect(recordsOf).toHaveBeenCalledWith(expect.objectContaining({area: 'Rule'}), 7)
    expect(harness.byTestId('hub-total-Rule')?.textContent).toBe('2')
    expect(harness.byTestId('hub-open-Rule')?.textContent).toContain('1 active, 1 locked')
    expect(harness.byTestId('hub-Rule')?.querySelector('svg')).not.toBeNull()
    expect(harness.byTestId('glimpse-Rule-records')).toBeNull()
  })

  it('opens the level below on a click of the card, like the hubs do', async () => {
    modelOf.mockResolvedValue(formModel('Rule'))
    recordsOf.mockResolvedValue([{id: 1, name: 'Velocity'}])
    harness = await mountWith(RecordGlimpse, {child: child('Rule'), parentValue: 7}, ['open'])
    await harness.settle()

    click('hub-open-Rule')

    expect(harness.mounted.emitted.open).toEqual([[[{area: 'Rule'}]]])
  })

  it('offers add on a kind that can be created and starts a new record', async () => {
    modelOf.mockResolvedValue({...formModel('Rule'), operations: ['list', 'create']} as FormModel)
    recordsOf.mockResolvedValue([])
    harness = await mountWith(RecordGlimpse, {child: child('Rule'), parentValue: 7}, ['open'])
    await harness.settle()

    click('hub-add-Rule')

    expect(harness.mounted.emitted.open).toEqual([[[{area: 'Rule', startNew: true}]]])
  })

  it('is found open again, with the rows below it, after the story has been left and returned to', async () => {
    modelOf.mockImplementation((area) =>
      Promise.resolve(area === 'Rule' ? formModel('Rule', [child('Tag')]) : formModel('Tag')),
    )
    recordsOf.mockImplementation((model) =>
      Promise.resolve(model.area === 'Rule' ? [{id: 4, name: 'Velocity'}] : [{id: 9, name: 'Urgent'}]),
    )
    harness = await mountWith(RecordGlimpse, {child: child('Rule'), parentValue: 7})
    await harness.settle()
    click('glimpse-Rule-toggle')
    await harness.settle()
    click('glimpse-Rule-below-4')
    await harness.settle()
    harness.unmount()

    harness = await mountWith(RecordGlimpse, {child: child('Rule'), parentValue: 7})
    await harness.settle()

    expect(harness.byTestId('glimpse-Rule-toggle')?.getAttribute('aria-expanded')).toBe('true')
    expect(harness.byTestId('glimpse-Rule-below-4')?.getAttribute('aria-expanded')).toBe('true')
    expect(harness.byTestId('glimpse-Tag')).not.toBeNull()
  })

  it('names the records with their state once the kind is shown', async () => {
    modelOf.mockResolvedValue({...formModel('Rule'), lockFields: ['locked']} as FormModel)
    recordsOf.mockResolvedValue([
      {id: 1, name: 'Velocity', active: true, locked: true},
      {id: 2, name: 'Amount', active: false, locked: false},
    ])
    harness = await mountWith(RecordGlimpse, {child: child('Rule'), parentValue: 7})
    await harness.settle()

    click('glimpse-Rule-toggle')
    await harness.settle()

    expect(harness.byTestId('glimpse-Rule-toggle')?.getAttribute('aria-expanded')).toBe('true')
    expect(harness.byTestId('glimpse-Rule-record-1')?.textContent).toContain('Velocity')
    expect(harness.byTestId('glimpse-Rule-record-1')?.textContent).toContain('Active')
    expect(harness.byTestId('glimpse-Rule-record-1')?.textContent).toContain('Locked')
    expect(harness.byTestId('glimpse-Rule-record-2')?.textContent).toContain('Inactive')
    expect(harness.byTestId('glimpse-Rule-record-2')?.textContent).not.toContain('Locked')
  })

  it('says so and cannot be shown when there is nothing below', async () => {
    modelOf.mockResolvedValue(formModel('Rule'))
    recordsOf.mockResolvedValue([])
    harness = await mountWith(RecordGlimpse, {child: child('Rule'), parentValue: 7})
    await harness.settle()

    expect(harness.byTestId('hub-total-Rule')?.textContent).toBe('0')
    expect(harness.byTestId('hub-open-Rule')?.textContent).toContain('None yet')
    expect((harness.byTestId('glimpse-Rule-toggle') as HTMLButtonElement).disabled).toBe(true)
  })

  it('says so when the records cannot be read', async () => {
    modelOf.mockResolvedValue(formModel('Rule'))
    recordsOf.mockRejectedValue(new Error('forbidden'))
    harness = await mountWith(RecordGlimpse, {child: child('Rule'), parentValue: 7})
    await harness.settle()

    expect(harness.byTestId('hub-total-Rule')).toBeNull()
    expect(harness.root.textContent).toContain('Could not load')
  })

  it('opens a record by its place in the hierarchy', async () => {
    modelOf.mockResolvedValue({...formModel('Rule'), operations: ['list', 'read']} as FormModel)
    recordsOf.mockResolvedValue([{id: 4, name: 'Velocity'}])
    harness = await mountWith(RecordGlimpse, {child: child('Rule'), parentValue: 7}, ['open'])
    await harness.settle()

    click('glimpse-Rule-toggle')
    await harness.settle()
    click('glimpse-Rule-open-4')

    expect(harness.mounted.emitted.open).toEqual([[[{area: 'Rule', id: 4}]]])
  })

  it('offers no open button for a kind that cannot be read', async () => {
    modelOf.mockResolvedValue(formModel('Rule'))
    recordsOf.mockResolvedValue([{id: 4, name: 'Velocity'}])
    harness = await mountWith(RecordGlimpse, {child: child('Rule'), parentValue: 7})
    await harness.settle()

    click('glimpse-Rule-toggle')
    await harness.settle()

    expect(harness.byTestId('glimpse-Rule-open-4')).toBeNull()
  })

  it('reads the level below a record only once that record is shown, and opens through it', async () => {
    modelOf.mockImplementation((area) =>
      Promise.resolve({
        ...(area === 'Rule' ? formModel('Rule', [child('Tag')]) : formModel('Tag')),
        operations: ['list', 'read'],
      } as FormModel),
    )
    recordsOf.mockImplementation((model) =>
      Promise.resolve(
        model.area === 'Rule' ? [{id: 4, name: 'Velocity'}] : [{id: 9, name: 'Urgent'}],
      ),
    )
    harness = await mountWith(RecordGlimpse, {child: child('Rule'), parentValue: 7}, ['open'])
    await harness.settle()

    click('glimpse-Rule-toggle')
    await harness.settle()

    expect(harness.byTestId('glimpse-Tag')).toBeNull()
    expect(recordsOf).toHaveBeenCalledTimes(1)

    click('glimpse-Rule-below-4')
    await harness.settle()

    expect(recordsOf).toHaveBeenLastCalledWith(expect.objectContaining({area: 'Tag'}), 4)
    expect(harness.byTestId('hub-total-Tag')?.textContent).toBe('1')

    click('glimpse-Tag-toggle')
    await harness.settle()
    click('glimpse-Tag-open-9')

    expect(harness.mounted.emitted.open).toEqual([
      [
        [
          {area: 'Rule', id: 4},
          {area: 'Tag', id: 9},
        ],
      ],
    ])
  })

  it('passes the guid down when the level below hangs off it', async () => {
    modelOf.mockImplementation((area) =>
      Promise.resolve(
        area === 'Rule' ? formModel('Rule', [child('Tag', {parentField: 'guid'})]) : formModel('Tag'),
      ),
    )
    recordsOf.mockImplementation((model) =>
      Promise.resolve(model.area === 'Rule' ? [{id: 4, name: 'Velocity', guid: 'g-4'}] : []),
    )
    harness = await mountWith(RecordGlimpse, {child: child('Rule'), parentValue: 7})
    await harness.settle()

    click('glimpse-Rule-toggle')
    await harness.settle()
    click('glimpse-Rule-below-4')
    await harness.settle()

    expect(recordsOf).toHaveBeenLastCalledWith(expect.objectContaining({area: 'Tag'}), 'g-4')
  })

  it('shows the first records and counts the rest', async () => {
    modelOf.mockResolvedValue(formModel('Rule'))
    recordsOf.mockResolvedValue(
      Array.from({length: 30}, (_, index) => ({id: index + 1, name: `Rule ${index + 1}`})),
    )
    harness = await mountWith(RecordGlimpse, {child: child('Rule'), parentValue: 7})
    await harness.settle()

    click('glimpse-Rule-toggle')
    await harness.settle()

    expect(harness.byTestId('glimpse-Rule-record-25')).not.toBeNull()
    expect(harness.byTestId('glimpse-Rule-record-26')).toBeNull()
    expect(harness.root.textContent).toContain('and 5 more')
  })
})

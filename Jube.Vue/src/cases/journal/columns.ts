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

import type { GridColumn } from '../../components/dataGridTypes'
import { stateClass, stateText } from '../detail/holdText'
import { laneClass, lettersOf } from './lanes'
import { paintFor, type RowPaint } from './paint'
import { guidOf } from './tags'
import type { NotableRow, JournalColumn, JournalRow, NoveltyField } from './types'

export type Translate = (key: string, values?: Record<string, unknown>) => string

export interface JournalContext {
  t: Translate
  number: (value: number) => string
  dateTime: (value: string) => string
  fixed: (value: number | null, digits?: number) => string
  selection?: {
    has: (guid: string) => boolean
    toggle: (guid: string, on: boolean) => void
  }
  pin?: { toggle: (guid: string, pinned: boolean) => void }
  merged?: boolean
  formatted?: boolean
  mappable?: boolean
}

export type GridRow = JournalRow & {
  __row: number
  __unusual: string
  __novel: string
  __paint: RowPaint | null
}

const leadingBase = ['ReferenceDate', 'ResponseElevation', 'HoldState', 'Activations']

export function groupLabel(group: string, t: Translate): string {
  if (group === 'abstraction') return t('journal.group.abstraction')
  if (group === 'calculation') return t('journal.group.calculation')
  if (group === 'counter') return t('journal.group.counter')
  if (group === 'dictionary') return t('journal.group.dictionary')
  if (group === 'exhaustive') return t('journal.group.exhaustive')
  if (group === 'advanced') return t('journal.group.advanced')
  if (group === 'sanction') return t('journal.group.sanction')
  if (group === 'http') return t('journal.group.http')

  return ''
}

export function buildRows(
  rows: readonly JournalRow[],
  notable: readonly NotableRow[],
  novelty: Record<string, string[]> | null,
  t: Translate,
): GridRow[] {
  const unusual = new Map(notable.map((row) => [row.id, row]))

  return rows.map((row, index) => {
    const stood = unusual.get(row.Id)
    const reason = stood?.reasons[0]

    return {
      ...row,
      __row: index,
      __unusual: stood && reason ? `${reason.field}|${reason.z.toFixed(1)}` : '',
      __novel: (novelty?.[String(row.Id)] ?? []).join('|'),
      __paint: paintFor(row, t),
    }
  })
}

function element(tag: string, className: string, text: string): HTMLElement {
  const created = document.createElement(tag)

  created.className = className
  created.textContent = text

  return created
}

function selectCell(
  row: JournalRow,
  selection: NonNullable<JournalContext['selection']>,
  t: Translate,
): HTMLElement {
  const guid = guidOf(row)
  const box = document.createElement('input')

  box.type = 'checkbox'
  box.className = 'checkbox checkbox-sm'
  box.checked = selection.has(guid)
  box.dataset.testid = 'journal-select'
  box.setAttribute('aria-label', t('journal.tags.selectRow'))
  box.addEventListener('change', () => selection.toggle(guid, box.checked))

  return box
}

function pinCell(
  row: JournalRow,
  pin: NonNullable<JournalContext['pin']>,
  t: Translate,
): HTMLElement {
  const guid = guidOf(row)
  const pinned = row.Pinned === true
  const button = document.createElement('button')

  button.type = 'button'
  button.className = pinned ? 'btn btn-sm btn-square btn-soft' : 'btn btn-sm btn-square btn-ghost'
  button.dataset.testid = 'journal-pin'
  button.setAttribute('aria-pressed', String(pinned))
  button.setAttribute('aria-label', pinned ? t('journal.pin.unpin') : t('journal.pin.pin'))
  button.textContent = pinned ? '★' : '☆'
  button.addEventListener('click', () => pin.toggle(guid, !pinned))

  return button
}

function laneCell(row: JournalRow): HTMLElement {
  const holder = element('div', 'flex flex-wrap items-center gap-1 py-0.5', '')

  for (const letter of lettersOf(row.Lane)) {
    holder.append(element('span', `badge badge-sm border font-mono ${laneClass(letter)}`, letter))
  }

  return holder
}

function chips(values: string[], className: string): HTMLElement {
  const holder = element('div', 'flex flex-wrap items-center gap-1 py-0.5', '')

  holder.append(...values.map((value) => element('span', className, value)))

  return holder
}

function unusualCell(raw: unknown, t: Translate): HTMLElement {
  const [field, z] = String(raw ?? '').split('|')

  if (!field || z === undefined) return element('span', '', '')

  const score = Number(z)
  const wrapper = element('div', 'flex flex-wrap items-center gap-1 py-0.5', '')

  wrapper.append(
    element('span', 'badge badge-sm badge-warning whitespace-nowrap', field),
    element(
      'span',
      'font-mono text-xs tabular-nums',
      `${score > 0 ? t('journal.unusual.above') : t('journal.unusual.below')} ${Math.abs(score).toFixed(1)}`,
    ),
  )

  return wrapper
}

function leadColumn(column: JournalColumn, context: JournalContext): GridColumn {
  const { t, dateTime, number } = context

  if (column.name === 'ReferenceDate') {
    return {
      field: column.name,
      title: t('journal.column.when'),
      type: 'date',
      mono: true,
      minWidth: 150,
      format: (value) => (value ? dateTime(String(value)) : ''),
    }
  }

  if (column.name === 'ResponseElevation') {
    return {
      field: column.name,
      title: t('journal.column.elevation'),
      type: 'number',
      mono: true,
      align: 'end',
      width: 150,
      draw: (row) => {
        const record = row as Record<string, unknown>
        const value = record[column.name]
        const name = record['ResponseElevationName']
        const text = typeof value === 'number' ? number(value) : ''

        return element(
          'span',
          'font-mono',
          typeof name === 'string' && name ? `${text} · ${name}` : text,
        )
      },
    }
  }

  if (column.name === 'HoldState') {
    return {
      field: column.name,
      title: t('journal.column.hold'),
      width: 120,
      draw: (row) => {
        const value = (row as Record<string, unknown>)[column.name]

        return typeof value === 'number'
          ? element(
              'span',
              `badge badge-sm whitespace-nowrap ${stateClass(value)}`,
              stateText(t, value),
            )
          : element('span', '', '')
      },
    }
  }

  return { field: column.name, title: t('journal.column.activations'), minWidth: 160 }
}

function lensColumn(column: JournalColumn, context: JournalContext): GridColumn {
  const { t, dateTime, fixed } = context
  const prefix = groupLabel(column.group, t)
  const title = prefix === '' ? column.title : `${prefix}: ${column.title}`

  if (column.group === 'rule') {
    return {
      field: column.name,
      title,
      minWidth: 120,
      draw: (row) => {
        const value = (row as Record<string, unknown>)[column.name]

        return value
          ? element(
              'span',
              `badge badge-sm whitespace-nowrap ${value === 'forced' ? 'badge-warning' : 'badge-soft'}`,
              value === 'forced' ? t('journal.rule.forced') : t('journal.rule.fired'),
            )
          : element('span', '', '')
      },
    }
  }

  if (column.type === 'array') {
    return {
      field: column.name,
      title,
      minWidth: 120,
      draw: (row) => {
        const value = (row as Record<string, unknown>)[column.name]

        return chips(
          Array.isArray(value) ? value.map((item) => String(item)) : [],
          'badge badge-sm badge-soft whitespace-nowrap',
        )
      },
    }
  }

  if (column.type === 'date') {
    return {
      field: column.name,
      title,
      type: 'date',
      mono: true,
      minWidth: 150,
      format: (value) => (value ? dateTime(String(value)) : ''),
    }
  }

  if (column.type === 'number') {
    return {
      field: column.name,
      title,
      type: 'number',
      mono: true,
      align: 'end',
      minWidth: 110,
      format: (value) =>
        typeof value === 'number' ? fixed(value, Number.isInteger(value) ? 0 : 2) : '',
    }
  }

  return {
    field: column.name,
    title,
    minWidth: 120,
    format: (value) => (value === null || value === undefined ? '' : String(value)),
  }
}

export function buildColumns(
  columns: readonly JournalColumn[],
  context: JournalContext,
  novelty: readonly NoveltyField[] | null,
): GridColumn[] {
  const { t } = context
  const byName = new Map(columns.map((column) => [column.name, column]))

  const result: GridColumn[] = []

  const { selection } = context

  if (selection) {
    result.push({
      field: '__selected',
      title: t('journal.tags.select'),
      hiddenTitle: t('journal.tags.select'),
      pinned: true,
      width: 56,
      align: 'center',
      draw: (row) => selectCell(row as JournalRow, selection, t),
    })
  }

  result.push({
    field: 'actions',
    title: t('journal.column.actions'),
    hiddenTitle: t('journal.column.actions'),
    pinned: true,
    width: context.mappable ? 190 : 110,
    actions: () => [
      { id: 'open', label: t('journal.action.open') },
      ...(context.mappable ? [{ id: 'map', label: t('journal.action.map') }] : []),
    ],
  })

  const { pin } = context

  if (pin) {
    result.push({
      field: 'Pinned',
      title: t('journal.pin.column'),
      hiddenTitle: t('journal.pin.column'),
      pinned: true,
      width: 64,
      align: 'center',
      draw: (row) => pinCell(row as JournalRow, pin, t),
    })
  }

  if (context.merged) {
    result.push({
      field: 'Lane',
      title: t('journal.merge.lane'),
      width: 90,
      draw: (row) => laneCell(row as JournalRow),
    })
  }

  for (const name of leadingBase) {
    const column = byName.get(name)

    if (column) result.push(leadColumn(column, context))
  }

  result.push({
    field: '__unusual',
    title: t('journal.column.unusual'),
    minWidth: 150,
    draw: (row) => unusualCell((row as Record<string, unknown>).__unusual, t),
  })

  if (context.formatted) {
    result.push({
      field: '__why',
      title: t('journal.format.column'),
      minWidth: 200,
      draw: (row) => {
        const paint = (row as GridRow).__paint

        return chips(
          paint?.reasons ?? [],
          'badge badge-sm badge-outline whitespace-normal h-auto py-0.5',
        )
      },
    })
  }

  if (novelty && novelty.length > 0) {
    result.push({
      field: '__novel',
      title: t('journal.column.new'),
      minWidth: 120,
      draw: (row) => {
        const raw = String((row as Record<string, unknown>).__novel ?? '')

        return chips(
          raw === '' ? [] : raw.split('|'),
          'badge badge-sm badge-soft whitespace-nowrap',
        )
      },
    })
  }

  for (const column of columns) {
    if (column.lens !== 'base') result.push(lensColumn(column, context))
  }

  const tag = byName.get('Tag')

  if (tag) result.push(lensColumn(tag, context))

  return result.map((column) =>
    column.pinned || column.field.startsWith('__')
      ? column
      : {
          ...column,
          paint: (row: object) => (row as GridRow).__paint?.cells[column.field] ?? null,
        },
  )
}

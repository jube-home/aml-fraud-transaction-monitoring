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

import type { ColumnDefinition } from 'tabulator-tables'
import type {
  GridAction,
  GridActionEvent,
  GridActionTone,
  GridBadge,
  GridColumn,
  GridDirection,
  GridInput,
  GridInputEvent,
  GridTone,
} from './dataGridTypes'

export interface GridHandlers {
  onAction?: (event: GridActionEvent) => void
  onInput?: (event: GridInputEvent) => void
}

const actionClass: Record<GridActionTone, string> = {
  neutral: 'btn btn-xs',
  primary: 'btn btn-xs btn-primary',
  success: 'btn btn-xs btn-success',
  error: 'btn btn-xs btn-error',
}

const badgeClass: Record<GridTone, string> = {
  neutral: 'badge badge-soft badge-sm',
  info: 'badge badge-soft badge-sm badge-info',
  success: 'badge badge-sm badge-success',
  warning: 'badge badge-sm badge-warning',
  error: 'badge badge-sm badge-error',
}

const formulaStart = /^[=+\-@\t\r]/
const plainNumber = /^[+-]?\d+(\.\d+)?$/

export function csvSafe(value: unknown): unknown {
  if (typeof value !== 'string') return value
  if (!formulaStart.test(value) || plainNumber.test(value)) return value

  return `'${value}`
}

export function resolveAlign(
  align: GridColumn['align'],
  direction: GridDirection,
): 'left' | 'center' | 'right' | undefined {
  if (align === undefined) return undefined
  if (align === 'center') return 'center'

  const start = direction === 'rtl' ? 'right' : 'left'
  const end = direction === 'rtl' ? 'left' : 'right'

  return align === 'start' ? start : end
}

function textElement(text: string): HTMLElement {
  const element = document.createElement('span')
  element.textContent = text

  return element
}

function badgeElement({ label, tone }: GridBadge): HTMLElement {
  const element = document.createElement('span')
  element.className = badgeClass[tone]
  element.textContent = label

  return element
}

function actionsElement(
  actions: GridAction[],
  row: object,
  onAction: GridHandlers['onAction'],
): HTMLElement {
  const element = document.createElement('span')
  element.className = 'flex flex-wrap gap-1'

  for (const action of actions) {
    const button = document.createElement('button')
    button.type = 'button'
    button.className = actionClass[action.tone ?? 'neutral']
    button.textContent = action.label
    button.disabled = action.disabled === true
    button.dataset.testid = `grid-action-${action.id}`
    button.addEventListener('click', () => onAction?.({ id: action.id, row }))
    element.append(button)
  }

  return element
}

function inputElement(
  input: GridInput,
  field: string,
  row: object,
  onInput: GridHandlers['onInput'],
): HTMLElement {
  const wrapper = document.createElement('span')
  wrapper.className = 'flex flex-col gap-1'

  if (input.prefix) wrapper.append(textElement(input.prefix))

  const element = document.createElement('input')
  element.type = input.type ?? 'text'
  if (input.type === 'number') element.step = 'any'
  element.className = 'input input-xs w-full min-w-40'
  element.placeholder = input.placeholder
  element.autocomplete = 'off'
  element.value = input.value ?? ''
  element.setAttribute('aria-label', input.label)
  element.dataset.testid = `grid-input-${field}`
  element.addEventListener('input', () => onInput?.({ field, value: element.value, row }))
  wrapper.append(element)

  return wrapper
}

export function toTabulatorColumns(
  columns: GridColumn[],
  direction: GridDirection,
  handlers: GridHandlers = {},
): ColumnDefinition[] {
  return columns.map((column) => {
    const definition: ColumnDefinition = { title: column.title, field: column.field }

    if (column.title === '' && column.hiddenTitle) {
      definition.title = column.hiddenTitle
      definition.titleFormatter = () => {
        const hidden = document.createElement('span')

        hidden.className = 'sr-only'
        hidden.textContent = column.hiddenTitle ?? ''

        return hidden
      }
    }
    const align = resolveAlign(column.align, direction)
    const { actions, badge, draw, format, input } = column

    if (align) {
      definition.hozAlign = align
      definition.headerHozAlign = align
    }

    if (column.mono) definition.cssClass = 'font-mono tabular-nums'
    if (column.width) definition.width = column.width
    if (column.minWidth) definition.minWidth = column.minWidth
    if (column.pinned) definition.frozen = true

    definition.accessorDownload = csvSafe

    if (draw || actions || input) definition.download = false

    if (draw) {
      definition.headerSort = false
      definition.formatter = (cell) => draw(cell.getRow().getData()) as HTMLElement
    } else if (actions) {
      definition.headerSort = false
      definition.formatter = (cell) =>
        actionsElement(actions(cell.getRow().getData()), cell.getRow().getData(), handlers.onAction)
    } else if (input) {
      definition.headerSort = false
      definition.formatter = (cell) => {
        const row = cell.getRow().getData()
        const spec = input(row)

        return spec
          ? inputElement(spec, column.field, row, handlers.onInput)
          : textElement(format ? format(cell.getValue()) : String(cell.getValue() ?? ''))
      }
    } else if (badge) {
      definition.formatter = (cell) => badgeElement(badge(cell.getValue()))
    } else if (format) {
      definition.formatter = (cell) => textElement(format(cell.getValue()))
    }

    if (column.paint) {
      const paint = column.paint
      const inner = definition.formatter

      definition.formatter = (cell, parameters, onRendered) => {
        const row = cell.getRow().getData()
        const element = cell.getElement()
        const colours = paint(row)

        element.style.color = colours?.fore ?? ''
        element.style.backgroundColor = colours?.back ?? ''

        return typeof inner === 'function'
          ? inner(cell, parameters, onRendered)
          : String(cell.getValue() ?? '')
      }
    }

    return definition
  })
}

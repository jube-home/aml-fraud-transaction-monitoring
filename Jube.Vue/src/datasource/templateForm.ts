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

import { formAttributeAllowed, formTags } from './templateVocabulary'

export type FieldValues = Record<string, string>

export type FieldElement = HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement

const fieldClasses: Record<string, string> = {
  form: 'flex flex-col gap-3',
  fieldset: 'fieldset',
  legend: 'fieldset-legend',
  label: 'label text-sm',
  select: 'select select-sm w-full',
  textarea: 'textarea textarea-sm min-h-20 w-full',
}

const inputClasses: Record<string, string> = {
  checkbox: 'checkbox checkbox-sm',
  radio: 'radio radio-sm',
}

const plainInput = 'input input-sm w-full'

export function clean(parent: Node): void {
  for (const node of [...parent.childNodes]) {
    if (node.nodeType === Node.TEXT_NODE) continue

    if (node.nodeType !== Node.ELEMENT_NODE) {
      node.parentNode?.removeChild(node)
      continue
    }

    const element = node as Element

    if (!formTags.has(element.localName)) {
      element.remove()
      continue
    }

    for (const attribute of [...element.attributes]) {
      if (!formAttributeAllowed(element.localName, attribute.name))
        element.removeAttribute(attribute.name)
    }

    if (element.localName === 'input') {
      const type = element.getAttribute('type') ?? 'text'

      if (
        !['text', 'number', 'date', 'datetime-local', 'email', 'tel', 'checkbox', 'radio'].includes(
          type,
        )
      )
        element.setAttribute('type', 'text')
    }

    clean(element)
  }
}

export function isField(element: Element): element is FieldElement {
  return ['input', 'select', 'textarea'].includes(element.localName)
}

export function fieldsOf(root: ParentNode): FieldElement[] {
  return [...root.querySelectorAll('input, select, textarea')].filter(isField)
}

export function decorate(root: HTMLElement, prefix: string): void {
  for (const element of root.querySelectorAll('*')) {
    const tag = element.localName
    const type = element.getAttribute('type') ?? 'text'
    const added = tag === 'input' ? (inputClasses[type] ?? plainInput) : fieldClasses[tag]

    if (added)
      element.setAttribute(
        'class',
        [element.getAttribute('class'), added].filter(Boolean).join(' '),
      )

    if (isField(element)) {
      const name = element.getAttribute('name') ?? ''

      element.id = `${prefix}-${name}${element.type === 'radio' ? `-${element.value}` : ''}`
    }
  }

  for (const label of root.querySelectorAll('label[for]')) {
    const target = fieldsOf(root).find((field) => field.name === label.getAttribute('for'))

    if (target) label.setAttribute('for', target.id)
  }
}

export function apply(root: ParentNode, values: FieldValues): void {
  for (const field of fieldsOf(root)) {
    const value = values[field.name]

    if (value === undefined) continue

    if (field instanceof HTMLInputElement && field.type === 'checkbox')
      field.checked = value === 'true'
    else if (field instanceof HTMLInputElement && field.type === 'radio')
      field.checked = field.value === value
    else field.value = value
  }
}

export function collect(root: ParentNode): FieldValues {
  const values: FieldValues = {}

  for (const field of fieldsOf(root)) {
    if (field instanceof HTMLInputElement && field.type === 'checkbox') {
      values[field.name] = field.checked ? 'true' : 'false'
    } else if (field instanceof HTMLInputElement && field.type === 'radio') {
      if (field.checked) values[field.name] = field.value
    } else if (field.value !== '') {
      values[field.name] = field.value
    }
  }

  return values
}

export function showErrors(root: ParentNode, errors: Record<string, string>): void {
  for (const old of root.querySelectorAll('[data-field-error]')) old.remove()

  for (const field of fieldsOf(root)) {
    field.removeAttribute('aria-invalid')
    field.removeAttribute('aria-describedby')

    const message = errors[field.name]

    if (!message || (field instanceof HTMLInputElement && field.type === 'radio' && !field.checked))
      continue

    const note = document.createElement('p')

    note.setAttribute('data-field-error', field.name)
    note.setAttribute('role', 'alert')
    note.id = `${field.id}-error`
    note.className = 'text-sm text-error'
    note.textContent = message
    field.setAttribute('aria-invalid', 'true')
    field.setAttribute('aria-describedby', note.id)
    field.insertAdjacentElement('afterend', note)
  }
}

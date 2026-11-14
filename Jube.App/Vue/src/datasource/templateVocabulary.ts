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

export const templateTags = new Set([
  'div',
  'span',
  'p',
  'h1',
  'h2',
  'h3',
  'h4',
  'h5',
  'h6',
  'ul',
  'ol',
  'li',
  'strong',
  'em',
  'b',
  'i',
  'small',
  'br',
  'hr',
  'table',
  'thead',
  'tbody',
  'tfoot',
  'tr',
  'th',
  'td',
  'caption',
  'section',
  'article',
  'header',
  'footer',
  'dl',
  'dt',
  'dd',
  'time',
  'code',
  'pre',
  'abbr',
  'mark',
  'sub',
  'sup',
])

const templateAttributes = new Set([
  'class',
  'title',
  'role',
  'scope',
  'colspan',
  'rowspan',
  'datetime',
  'lang',
  'dir',
])

export function attributeAllowed(name: string): boolean {
  return templateAttributes.has(name) || /^(aria|data)-[a-z0-9-]+$/.test(name)
}

export const formTags = new Set([
  ...templateTags,
  'form',
  'fieldset',
  'legend',
  'label',
  'input',
  'select',
  'option',
  'textarea',
])

const formAttributes: Record<string, Set<string>> = {
  input: new Set(['name', 'type', 'placeholder', 'value', 'checked', 'disabled']),
  select: new Set(['name', 'disabled']),
  option: new Set(['value', 'selected']),
  textarea: new Set(['name', 'placeholder', 'rows', 'disabled']),
  label: new Set(['for']),
}

export function formAttributeAllowed(tag: string, name: string): boolean {
  return attributeAllowed(name) || (formAttributes[tag]?.has(name) ?? false)
}

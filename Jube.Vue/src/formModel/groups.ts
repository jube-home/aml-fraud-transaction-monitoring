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
import { claimedNames, editorOf, firstClaimed } from './editors'
import type { FormField, FormGroup, FormModel } from './types'

export interface GroupNode {
  path: string
  label: string
  collapsed: boolean
  fields: FormField[]
  children: GroupNode[]
}

export function groupTree(model: FormModel): GroupNode[] {
  const nodes = new Map<string, GroupNode>()
  const roots: GroupNode[] = []

  const nodeFor = (path: string, declared: FormGroup | undefined): GroupNode => {
    const existing = nodes.get(path)

    if (existing) return existing

    const parts = path.split('/')
    const node: GroupNode = {
      path,
      label: declared?.label ?? parts[parts.length - 1]!,
      collapsed: declared?.collapsed ?? false,
      fields: [],
      children: [],
    }

    nodes.set(path, node)

    if (parts.length === 1) {
      roots.push(node)
    } else {
      nodeFor(
        parts.slice(0, -1).join('/'),
        model.groups.find((group) => group.path === parts.slice(0, -1).join('/')),
      ).children.push(node)
    }

    return node
  }

  for (const group of model.groups) nodeFor(group.path, group)

  const ungrouped: GroupNode = { path: '', label: '', collapsed: false, fields: [], children: [] }

  const claimed = claimedNames(model)

  for (const field of model.fields) {
    const editor = editorOf(model, field.name)

    if (claimed.has(field.name) && editor && firstClaimed(editor, model) !== field.name) continue

    if (!field.group) ungrouped.fields.push(field)
    else
      nodeFor(
        field.group,
        model.groups.find((group) => group.path === field.group),
      ).fields.push(field)
  }

  const filled = (node: GroupNode): boolean => node.fields.length > 0 || node.children.some(filled)

  const shown = roots.filter(filled)

  return ungrouped.fields.length > 0 ? [ungrouped, ...shown] : shown
}

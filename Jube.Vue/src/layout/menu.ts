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

import type { Me } from '../auth/me'
import { menuTree, type MenuNode } from './menuTree'

function allowed(node: MenuNode, me: Me): boolean {
  const restricted = (node.permissions?.length ?? 0) > 0 || node.landlord !== undefined

  if (!restricted) return true
  if (node.landlord !== undefined && me.landlord) return true
  if (node.landlord === 'only') return false

  return (node.permissions ?? []).some((permission) => me.permissions.includes(permission))
}

function visible(node: MenuNode, me: Me): MenuNode | undefined {
  if (!allowed(node, me)) return undefined
  if (!node.children) return node

  const children = node.children
    .map((child) => visible(child, me))
    .filter((child): child is MenuNode => child !== undefined)

  return children.length > 0 ? { ...node, children } : undefined
}

export function visibleMenu(me: Me | undefined): MenuNode[] {
  if (!me) return []

  return menuTree
    .map((node) => visible(node, me))
    .filter((node): node is MenuNode => node !== undefined)
}

export function containsRoute(node: MenuNode, routeName: string | undefined): boolean {
  if (!routeName) return false

  return (
    node.routeName === routeName || (node.children ?? []).some((c) => containsRoute(c, routeName))
  )
}

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

import type { BuilderGroup, BuilderRule } from '../../ruleBuilder/types'

const maximumDepth = 6

export function isGroup(node: BuilderRule | BuilderGroup): node is BuilderGroup {
  return 'rules' in node
}

export function ruleCount(group: BuilderGroup | null): number {
  if (!group) return 0

  return group.rules.reduce((total, node) => total + (isGroup(node) ? ruleCount(node) : 1), 0)
}

function validGroup(value: unknown, depth: number): value is BuilderGroup {
  if (typeof value !== 'object' || value === null || depth > maximumDepth) return false

  const record = value as Record<string, unknown>

  if (record.condition !== 'AND' && record.condition !== 'OR') return false
  if (!Array.isArray(record.rules)) return false

  return record.rules.every((node) => {
    if (typeof node !== 'object' || node === null) return false

    const child = node as Record<string, unknown>

    if ('rules' in child) return validGroup(child, depth + 1)

    return typeof child.id === 'string' && typeof child.operator === 'string'
  })
}

function clean(group: BuilderGroup): BuilderGroup {
  const rules: (BuilderRule | BuilderGroup)[] = []

  for (const node of group.rules) {
    if (isGroup(node)) {
      const inner = clean(node)

      if (inner.rules.length > 0) rules.push(inner)
    } else {
      rules.push({ id: node.id, operator: node.operator, value: node.value })
    }
  }

  return { condition: group.condition, rules }
}

export function parseFilter(raw: string | undefined): BuilderGroup | null {
  if (!raw) return null

  try {
    const parsed: unknown = JSON.parse(raw)

    if (!validGroup(parsed, 0)) return null

    const group = clean(parsed)

    return ruleCount(group) > 0 ? group : null
  } catch {
    return null
  }
}

export function serialiseFilter(group: BuilderGroup | null): string | undefined {
  if (!group) return undefined

  const cleaned = clean(group)

  return ruleCount(cleaned) > 0 ? JSON.stringify(cleaned) : undefined
}

export function describeFilter(
  group: BuilderGroup,
  operatorName: (operator: string) => string,
  words: { and: string; or: string },
): string {
  const part = (node: BuilderRule | BuilderGroup): string => {
    if (isGroup(node)) return `(${describeFilter(node, operatorName, words)})`

    const value = node.value === undefined ? '' : ` "${String(node.value)}"`

    return `${node.id} ${operatorName(node.operator)}${value}`
  }

  return group.rules.map(part).join(group.condition === 'AND' ? ` ${words.and} ` : ` ${words.or} `)
}

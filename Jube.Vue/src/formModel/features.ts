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

import type { GroupNode } from './groups'
import type { FormCondition, FormField } from './types'

export interface FeatureNode {
  switchField: FormField
  fields: FormField[]
  features: FeatureNode[]
}

export interface GroupBody {
  fields: FormField[]
  features: FeatureNode[]
  children: GroupNode[]
}

function isTrue(condition: FormCondition): boolean {
  return (
    condition.op === 'equals' &&
    condition.values.length === 1 &&
    String(condition.values[0]).toLowerCase() === 'true'
  )
}

export function featureLayout(fields: FormField[]): {
  plain: FormField[]
  features: FeatureNode[]
} {
  const byName = new Map(fields.map((field) => [field.name, field]))

  const gatesOf = (field: FormField): string[] => {
    const names = [...field.visibleWhen, ...field.enabledWhen]
      .filter(isTrue)
      .map((condition) => condition.property)
      .filter((name) => name !== field.name && byName.get(name)?.type === 'boolean')

    return [...new Set(names)]
  }

  const depths = new Map<string, number>()

  const depthOf = (name: string, seen: Set<string>): number => {
    const known = depths.get(name)

    if (known !== undefined) return known
    if (seen.has(name)) return 0

    seen.add(name)

    const gates = gatesOf(byName.get(name)!)
    const depth = gates.length === 0 ? 0 : 1 + Math.max(...gates.map((gate) => depthOf(gate, seen)))

    depths.set(name, depth)

    return depth
  }

  const parentOf = (field: FormField): string | undefined => {
    const gates = gatesOf(field)

    if (gates.length === 0) return undefined

    return gates.reduce((best, gate) =>
      depthOf(gate, new Set()) > depthOf(best, new Set()) ? gate : best,
    )
  }

  const controllers = new Set<string>()

  for (const field of fields) {
    const parent = parentOf(field)

    if (parent) controllers.add(parent)
  }

  const nodes = new Map<string, FeatureNode>()

  for (const name of controllers) {
    nodes.set(name, { switchField: byName.get(name)!, fields: [], features: [] })
  }

  const plain: FormField[] = []
  const features: FeatureNode[] = []

  for (const field of fields) {
    const parent = parentOf(field)
    const own = nodes.get(field.name)

    if (!parent) {
      if (own) features.push(own)
      else plain.push(field)
    } else if (own) {
      nodes.get(parent)!.features.push(own)
    } else {
      nodes.get(parent)!.fields.push(field)
    }
  }

  return { plain, features }
}

export function bodyOf(node: GroupNode): GroupBody {
  const { plain, features } = featureLayout(node.fields)

  return { fields: plain, features, children: node.children }
}

export function groupFeature(node: GroupNode): FeatureNode | undefined {
  const { plain, features } = featureLayout(node.fields)

  return plain.length === 0 && features.length === 1 && node.children.length === 0
    ? features[0]
    : undefined
}

export function featureBody(feature: FeatureNode): GroupBody {
  return { fields: feature.fields, features: feature.features, children: [] }
}

export function featureTitle(feature: FeatureNode): string {
  const label = feature.switchField.label
  const rest = label.replace(/^enable\s+/i, '')

  return rest === label || rest === '' ? label : `${rest.charAt(0).toUpperCase()}${rest.slice(1)}`
}

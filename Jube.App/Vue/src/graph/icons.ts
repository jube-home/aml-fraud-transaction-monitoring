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

import type { Component } from 'vue'
import { CircleDot } from 'lucide-vue-next'
import { lucideIcons } from './lucideIcons.generated'
import type { GraphEdgeDefinition } from './types'

const keyWords: [string[], string][] = [
  [['case'], 'briefcase'],
  [['account', 'iban', 'ledger'], 'landmark'],
  [['payee', 'beneficiary', 'counterparty', 'payer', 'recipient'], 'hand-coins'],
  [['device', 'handset', 'imei', 'fingerprint'], 'smartphone'],
  [['ip', 'network', 'subnet', 'asn', 'wifi', 'hotspot'], 'globe'],
  [['email', 'mail'], 'mail'],
  [['phone', 'msisdn', 'mobile', 'telephone'], 'phone'],
  [['card', 'pan', 'bin'], 'credit-card'],
  [['merchant', 'shop', 'store', 'terminal'], 'store'],
  [['customer', 'person', 'user', 'holder', 'individual'], 'user'],
  [['company', 'business', 'organisation', 'organization', 'entity', 'corporate'], 'building-2'],
  [['address', 'location', 'country', 'city', 'geo'], 'map-pin'],
  [['wallet', 'crypto', 'bitcoin'], 'wallet'],
  [['document', 'passport', 'licence', 'license', 'identity', 'kyc'], 'id-card'],
  [['session', 'browser', 'cookie'], 'monitor'],
  [['tag'], 'tag'],
]

export const fallbackIconName = 'circle-dot'

function tokenise(key: string): Set<string> {
  const tokens = new Set<string>()
  let current = ''

  for (const c of key) {
    const upper = c >= 'A' && c <= 'Z'
    const letter = /\p{L}/u.test(c)

    if (letter && upper && current !== '' && !/[A-Z]$/.test(current)) {
      tokens.add(current.toLowerCase())
      current = ''
    }

    if (/[\p{L}\p{N}]/u.test(c)) {
      current += c
    } else if (current !== '') {
      tokens.add(current.toLowerCase())
      current = ''
    }
  }

  if (current !== '') {
    tokens.add(current.toLowerCase())
  }

  return tokens
}

export function iconNameForNodeKey(key: string | null | undefined): string {
  if (key === null || key === undefined || key.trim() === '') {
    return fallbackIconName
  }

  const tokens = tokenise(key)

  for (const [words, icon] of keyWords) {
    if (words.some((word) => tokens.has(word))) {
      return icon
    }
  }

  return fallbackIconName
}

export function iconComponent(name: string | null | undefined): Component | null {
  return name !== null && name !== undefined && Object.hasOwn(lucideIcons, name)
    ? (lucideIcons[name] ?? null)
    : null
}

export function nodeIconMap(
  definitions: readonly Pick<
    GraphEdgeDefinition,
    'fromNodeKey' | 'toNodeKey' | 'fromNodeIcon' | 'toNodeIcon'
  >[],
): Map<string, string> {
  const icons = new Map<string, string>()

  for (const definition of definitions) {
    if (definition.fromNodeKey && iconComponent(definition.fromNodeIcon) !== null) {
      if (!icons.has(definition.fromNodeKey)) {
        icons.set(definition.fromNodeKey, definition.fromNodeIcon!)
      }
    }

    if (definition.toNodeKey && iconComponent(definition.toNodeIcon) !== null) {
      if (!icons.has(definition.toNodeKey)) {
        icons.set(definition.toNodeKey, definition.toNodeIcon!)
      }
    }
  }

  return icons
}

export function nodeIcon(key: string, configured: ReadonlyMap<string, string>): Component {
  return iconComponent(configured.get(key)) ?? iconComponent(iconNameForNodeKey(key)) ?? CircleDot
}

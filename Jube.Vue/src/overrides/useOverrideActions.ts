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

import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { describeApiError } from '../api/problems'
import { overrideApi } from './api'
import type { OverrideKind, OverrideScope } from './types'

export interface OverrideActions {
  problems: ReturnType<typeof ref<string[]>>
  reportTooEarly: () => void
  setModel: (guid: string, active: boolean) => Promise<boolean>
  setModelExpiry: (guid: string, expiry: Date | null) => Promise<boolean>
  setRule: (guid: string, name: string, active: boolean) => Promise<boolean>
  setRuleKind: (guid: string, name: string, kind: OverrideKind) => Promise<boolean>
  setRuleExpiry: (guid: string, name: string, expiry: Date | null) => Promise<boolean>
}

export function useOverrideActions(
  scope: () => OverrideScope,
  onUpdated: () => void,
): OverrideActions {
  const { t } = useI18n()
  const queryClient = useQueryClient()
  const problems = ref<string[]>([])

  function describe(error: unknown): string[] {
    return describeApiError(error, { failed: t('override.failed'), refused: t('override.refused') })
  }

  async function run(change: () => Promise<unknown>): Promise<boolean> {
    try {
      await change()
      problems.value = []
      onUpdated()

      return true
    } catch (error) {
      problems.value = describe(error)

      return false
    } finally {
      await queryClient.invalidateQueries({ queryKey: ['override'] })
    }
  }

  return {
    problems,
    reportTooEarly: () => {
      problems.value = [t('override.tooEarly')]
    },
    setModel: (guid, active) => run(() => overrideApi.setModel(scope(), guid, active)),
    setModelExpiry: (guid, expiry) => run(() => overrideApi.setModelExpiry(scope(), guid, expiry)),
    setRule: (guid, name, active) => run(() => overrideApi.setRule(scope(), guid, name, active)),
    setRuleKind: (guid, name, kind) =>
      run(() => overrideApi.setRuleKind(scope(), guid, name, kind)),
    setRuleExpiry: (guid, name, expiry) =>
      run(() => overrideApi.setRuleExpiry(scope(), guid, name, expiry)),
  }
}

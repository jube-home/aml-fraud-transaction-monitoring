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

import { useQueryClient } from '@tanstack/vue-query'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { resetAntiforgeryToken, useApi } from '../api/useApi'
import { useToasts } from '../stores/toasts'
import { forgetSessionExpiry } from './sessionExpiry'

export function useSessionActions() {
  const router = useRouter()
  const client = useQueryClient()
  const toasts = useToasts()
  const { t } = useI18n()

  function leave(redirect?: string): Promise<unknown> {
    client.clear()
    resetAntiforgeryToken()
    forgetSessionExpiry()

    return router.push({ name: 'login', query: redirect ? { redirect } : {} })
  }

  async function signOut(): Promise<void> {
    try {
      await useApi('/api/Authentication/Logout', { method: 'POST', anonymous: true })
    } catch {
      toasts.error(t('shell.signOutFailed'))
      return
    }

    await leave()
  }

  async function expire(): Promise<void> {
    const here = router.currentRoute.value.fullPath

    toasts.info(t('shell.sessionExpired'))
    await leave(here)
  }

  return { signOut, expire }
}

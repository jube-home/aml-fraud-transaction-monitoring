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

import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useI18n } from 'vue-i18n'
import { joobyApi } from '../../askjooby/api'
import { createJoobyController } from '../../askjooby/controller'
import { meQueryOptions } from '../../auth/me'
import { useToasts } from '../../stores/toasts'

export const conversationsKey = ['askjooby', 'conversations'] as const

export function useJooby(pageContext: () => string, greeting: 'model' | 'static' = 'model') {
  const { t } = useI18n()
  const client = useQueryClient()
  const toasts = useToasts()
  const { data: me } = useQuery(meQueryOptions)

  const controller = createJoobyController({
    api: joobyApi,
    offline: me.value?.askJoobyInference === false,
    greeting,
    isLandlord: me.value?.landlord === true,
    pageContext,
    onConversationsChanged: () => {
      void client.invalidateQueries({ queryKey: conversationsKey })
    },
    notify: (kind, text) => toasts[kind](text || t('askJooby.failed')),
  })

  async function guard(action: () => Promise<unknown>): Promise<void> {
    try {
      await action()
    } catch (error) {
      toasts.apiError(error)
    }
  }

  return {
    guard,
    controller,
    me,
    offline: me.value?.askJoobyInference === false,
    landlord: me.value?.landlord === true,
  }
}

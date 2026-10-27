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

import { useI18n } from 'vue-i18n'
import type { ApprovalState } from '../approval/model'

export type ApprovalTone = 'success' | 'warning' | 'error' | 'neutral'

export function useApprovalState() {
  const { t } = useI18n()

  function label(state: ApprovalState): string {
    switch (state) {
      case 'approved':
        return t('approval.state.approved')
      case 'awaiting':
        return t('approval.state.awaiting')
      case 'rejected':
        return t('approval.state.rejected')
      case 'deleted':
        return t('approval.state.deleted')
      case 'deletionAwaiting':
        return t('approval.state.deletionAwaiting')
      case 'deletionRejected':
        return t('approval.state.deletionRejected')
    }
  }

  function tone(state: ApprovalState): ApprovalTone {
    switch (state) {
      case 'approved':
        return 'success'
      case 'awaiting':
      case 'deletionAwaiting':
        return 'warning'
      case 'rejected':
      case 'deletionRejected':
        return 'error'
      case 'deleted':
        return 'neutral'
    }
  }

  return { label, tone }
}

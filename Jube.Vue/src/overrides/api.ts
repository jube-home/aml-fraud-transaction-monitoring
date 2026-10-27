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

import { useApi } from '../api/useApi'
import type {
  OverrideKey,
  OverrideKind,
  OverrideModel,
  OverrideRule,
  OverrideScope,
  OverrideValue,
  OverrideXPath,
} from './types'

const models = '/api/EntityAnalysisModelOverride'
const rules = '/api/EntityAnalysisModelActivationRuleOverride'

export const overrideApi = {
  keyNames: () => useApi<OverrideXPath[]>('/api/EntityAnalysisModelRequestXPath/ByOverrideKey'),
  keys: () => useApi<OverrideKey[]>('/api/GetEntityAnalysisModelOverrideQuery/Keys'),
  values: (overrideKey: string) =>
    useApi<OverrideValue[]>('/api/GetEntityAnalysisModelOverrideQuery/Values', {
      query: { overrideKey },
    }),
  models: (scope: OverrideScope) =>
    useApi<OverrideModel[]>('/api/GetEntityAnalysisModelOverrideQuery', { query: { ...scope } }),
  rules: (scope: OverrideScope, entityAnalysisModelGuid: string) =>
    useApi<OverrideRule[]>('/api/GetEntityAnalysisModelActivationRuleOverrideQuery', {
      query: { ...scope, entityAnalysisModelGuid },
    }),

  setModel: (scope: OverrideScope, entityAnalysisModelGuid: string, active: boolean) =>
    useApi(models, { method: 'PUT', body: { ...scope, entityAnalysisModelGuid, active } }),
  setModelExpiry: (scope: OverrideScope, entityAnalysisModelGuid: string, expiry: Date | null) =>
    useApi(`${models}/DeleteExpiryDate`, {
      method: 'PUT',
      body: { ...scope, entityAnalysisModelGuid, deleteExpiryDate: expiry?.toISOString() ?? null },
    }),

  setRule: (scope: OverrideScope, entityAnalysisModelGuid: string, name: string, active: boolean) =>
    useApi(rules, {
      method: 'PUT',
      body: {
        ...scope,
        entityAnalysisModelGuid,
        entityAnalysisModelActivationRuleName: name,
        active,
      },
    }),
  setRuleKind: (
    scope: OverrideScope,
    entityAnalysisModelGuid: string,
    name: string,
    overrideKind: OverrideKind,
  ) =>
    useApi(`${rules}/OverrideKind`, {
      method: 'PUT',
      body: {
        ...scope,
        entityAnalysisModelGuid,
        entityAnalysisModelActivationRuleName: name,
        overrideKind,
      },
    }),
  setRuleExpiry: (
    scope: OverrideScope,
    entityAnalysisModelGuid: string,
    name: string,
    expiry: Date | null,
  ) =>
    useApi(`${rules}/DeleteExpiryDate`, {
      method: 'PUT',
      body: {
        ...scope,
        entityAnalysisModelGuid,
        entityAnalysisModelActivationRuleName: name,
        deleteExpiryDate: expiry?.toISOString() ?? null,
      },
    }),
}

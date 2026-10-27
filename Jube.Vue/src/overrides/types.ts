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

export type OverrideKind = 0 | 1

export interface OverrideKey {
  overrideKey: string
  enabledOnModels: number
  values: number
  overrides: number
  forced: number
  activationRules: number
  allActivationRules: number
  nextExpiryDate: string | null
  lastCreatedDate: string | null
}

export interface OverrideValue {
  overrideKeyValue: string
  name: string
  entityAnalysisModelGuid: string
  overrideKind: OverrideKind
  createdUser: string | null
  createdDate: string | null
  deleteExpiryDate: string | null
}

export interface OverrideModel {
  name: string
  entityAnalysisModelGuid: string
  hasOverride: boolean
  overrideKind: OverrideKind
  deleteExpiryDate: string | null
}

export interface OverrideRule {
  name: string
  hasOverride: boolean
  entityAnalysisModelActivationRuleOverrideId: number
  entityAnalysisModelGuid: string
  enableForce: boolean
  overrideKind: OverrideKind
  deleteExpiryDate: string | null
}

export interface OverrideScope {
  overrideKey: string
  overrideKeyValue: string
}

export interface OverrideXPath {
  name: string
}

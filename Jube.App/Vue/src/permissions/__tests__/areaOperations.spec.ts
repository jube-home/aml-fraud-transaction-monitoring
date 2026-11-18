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

import {describe, expect, it} from 'vitest'
import type {Me} from '../../auth/me'
import {deniedOperations, operationPermitted, reprocessingPermission} from '../areaOperations'
import {areaOf} from '../areas'

function me(permissions: number[]): Me {
  return {
    userName: 'alex',
    landlord: false,
    permissions,
    tenantRegistryId: 1,
    canChangePassword: true,
    askJoobyInference: true,
    svirfd: false,
    sessionExpiry: null,
  }
}

const rule = 'EntityAnalysisModelReprocessingRule'
const instance = 'EntityAnalysisModelReprocessingRuleInstance'

describe('the reprocessing permission split', () => {
  it('keeps 26 for the rule and uses the next free numbers for trigger and stop', () => {
    expect(reprocessingPermission).toEqual({editRule: 26, triggerRun: 73, stopRun: 74})
  })

  it('lets only the edit permission change the rule', () => {
    for (const operation of ['create', 'update', 'delete'] as const) {
      expect(operationPermitted(rule, operation, me([26]))).toBe(true)
      expect(operationPermitted(rule, operation, me([73]))).toBe(false)
      expect(operationPermitted(rule, operation, me([74]))).toBe(false)
    }
  })

  it('lets only the trigger permission create or change a run and only stop delete one', () => {
    expect(operationPermitted(instance, 'create', me([73]))).toBe(true)
    expect(operationPermitted(instance, 'update', me([73]))).toBe(true)
    expect(operationPermitted(instance, 'create', me([26, 74]))).toBe(false)
    expect(operationPermitted(instance, 'update', me([26, 74]))).toBe(false)
    expect(operationPermitted(instance, 'delete', me([74]))).toBe(true)
    expect(operationPermitted(instance, 'delete', me([26, 73]))).toBe(false)
  })

  it('lets any of the three read both record types and nobody else', () => {
    for (const permission of [26, 73, 74]) {
      for (const area of [rule, instance]) {
        expect(operationPermitted(area, 'list', me([permission]))).toBe(true)
        expect(operationPermitted(area, 'read', me([permission]))).toBe(true)
      }
    }

    expect(operationPermitted(rule, 'list', me([1]))).toBe(false)
    expect(operationPermitted(instance, 'read', me([]))).toBe(false)
  })

  it('does not touch the operations of any other area', () => {
    expect(operationPermitted('Tag', 'delete', me([]))).toBe(true)
    expect(operationPermitted('Tag', 'create', undefined)).toBe(true)
  })

  it('treats a user who has not loaded yet as having nothing', () => {
    expect(operationPermitted(rule, 'create', undefined)).toBe(false)
  })

  it('names what a trigger-only user is denied on each record type', () => {
    const all = ['list', 'read', 'create', 'update', 'delete'] as const

    expect(deniedOperations(rule, all, me([73]))).toEqual(['create', 'update', 'delete'])
    expect(deniedOperations(instance, all, me([73]))).toEqual(['delete'])
    expect(deniedOperations(instance, all, me([26, 73, 74]))).toEqual([])
  })

  it('groups the three permissions together in the permission allocation screen', () => {
    expect(areaOf(26)).toBe('models')
    expect(areaOf(73)).toBe('models')
    expect(areaOf(74)).toBe('models')
  })
})

describe('areas whose list is open to more permissions than their writes', () => {
  const writes = ['create', 'update', 'delete'] as const

  it.each([
    ['EntityAnalysisModel', 6, 13],
    ['GraphRegistry', 50, 49],
    ['GraphRegistryEdge', 50, 49],
    ['RoleRegistry', 34, 35],
    ['VisualisationRegistry', 31, 32],
  ])('lets %s be written only with permission %i', (area, writer, reader) => {
    for (const operation of writes) {
      expect(operationPermitted(area, operation, me([writer]))).toBe(true)
      expect(operationPermitted(area, operation, me([reader]))).toBe(false)
    }

    expect(operationPermitted(area, 'list', me([reader]))).toBe(true)
    expect(operationPermitted(area, 'read', me([reader]))).toBe(true)
  })
})

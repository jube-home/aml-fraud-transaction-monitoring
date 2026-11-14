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
import {modelIdOf, ruleTextOf, ruleToolsHost} from '../ruleTools'
import type {FormEditor, FormModel} from '../types'

const builder: FormEditor = {
  kind: 'ruleBuilder',
  parseType: 5,
  roles: {
    scriptType: 'ruleScriptTypeId',
    builder: 'builderRuleScript',
    builderJson: 'json',
    coder: 'coderRuleScript',
  },
  reads: {},
}

const tools: FormEditor = {kind: 'ruleTools', parseType: 1, roles: {}, reads: {}}

const model = {area: 'EntityAnalysisModelActivationRule', editors: [builder, tools]} as FormModel

const me = (...permissions: number[]): Me => ({
  userName: 'a',
  landlord: false,
  permissions,
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: false,
  svirfd: false,
  sessionExpiry: null,
})

const values = {
  ruleScriptTypeId: 1,
  builderRuleScript: 'Amount > 5',
  coderRuleScript: 'return Amount > 9;',
}

describe('rule text', () => {
  it('reads the builder text when the rule is built and the coder text otherwise', () => {
    expect(ruleTextOf(model, values)).toBe('Amount > 5')
    expect(ruleTextOf(model, {...values, ruleScriptTypeId: 2})).toBe('return Amount > 9;')
  })

  it('is empty when the record has no rule editor or no text', () => {
    expect(ruleTextOf({...model, editors: []} as FormModel, values)).toBe('')
    expect(ruleTextOf(model, {})).toBe('')
  })
})

describe('rule tools host', () => {
  it('describes a saved activation rule with the current, unsaved text', () => {
    const host = ruleToolsHost(model, values, 7, me(61, 60, 8))

    expect(host).toEqual({
      ruleType: 'ActivationRule',
      ruleId: 7,
      ruleText: 'Amount > 5',
      canBacktest: true,
      canCheckIntegrity: true,
      canDependencies: true,
    })
  })

  it('gates each tool by its own permission', () => {
    expect(ruleToolsHost(model, values, 7, me(61))).toMatchObject({
      canBacktest: true,
      canCheckIntegrity: false,
      canDependencies: false,
    })
    expect(ruleToolsHost(model, values, 7, me(60))).toMatchObject({
      canBacktest: false,
      canCheckIntegrity: true,
    })
    expect(ruleToolsHost(model, values, 7, me(26))).toMatchObject({canDependencies: true})
    expect(ruleToolsHost(model, values, 7, undefined)).toMatchObject({
      canBacktest: false,
      canCheckIntegrity: false,
      canDependencies: false,
    })
  })

  it('is absent for an unsaved rule, a record without the editor and an unknown type', () => {
    expect(ruleToolsHost(model, values, undefined, me(61))).toBeUndefined()
    expect(ruleToolsHost({...model, editors: [builder]} as FormModel, values, 7, me(61))).toBe(
      undefined,
    )
    expect(
      ruleToolsHost(
        {...model, editors: [builder, {...tools, parseType: 99}]} as FormModel,
        values,
        7,
        me(61),
      ),
    ).toBeUndefined()
    expect(ruleToolsHost(undefined, values, 7, me(61))).toBeUndefined()
  })

  it('is unaffected by a record that is locked or pending approval', () => {
    expect(ruleToolsHost(model, {...values, locked: true}, 7, me(61))?.ruleText).toBe(
      'Amount > 5',
    )
  })
})

describe('model id', () => {
  it('takes the parent of the record when it is a whole positive number', () => {
    expect(modelIdOf(3)).toBe(3)
    expect(modelIdOf('4')).toBe(4)
    expect(modelIdOf(undefined)).toBeUndefined()
    expect(modelIdOf('x')).toBeUndefined()
    expect(modelIdOf(0)).toBeUndefined()
  })
})

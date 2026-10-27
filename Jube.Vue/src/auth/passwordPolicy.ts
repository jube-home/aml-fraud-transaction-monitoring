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

export const passwordMinLength = 12
export const passwordMaxLength = 128

export type PasswordRuleKey =
  'minLength' | 'maxLength' | 'uppercase' | 'lowercase' | 'number' | 'special' | 'repeat' | 'common'

export interface PasswordRule {
  key: PasswordRuleKey
  test: (password: string) => boolean
}

export const passwordRules: PasswordRule[] = [
  { key: 'minLength', test: (p) => p.length >= passwordMinLength },
  { key: 'maxLength', test: (p) => p.length <= passwordMaxLength },
  { key: 'uppercase', test: (p) => /[A-Z]/.test(p) },
  { key: 'lowercase', test: (p) => /[a-z]/.test(p) },
  { key: 'number', test: (p) => /[0-9]/.test(p) },
  { key: 'special', test: (p) => /[^A-Za-z0-9]/.test(p) },
  { key: 'repeat', test: (p) => !/(.)\1{2,}/.test(p) },
  { key: 'common', test: (p) => !/^(password|123456|qwerty)/i.test(p) },
]

export function failedPasswordRules(password: string): PasswordRuleKey[] {
  return passwordRules.filter((rule) => !rule.test(password)).map((rule) => rule.key)
}

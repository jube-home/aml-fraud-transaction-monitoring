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

import {computed, reactive, ref} from 'vue'
import QRCode from 'qrcode'
import {
  enrolTotp,
  readWirePasswordHash,
  signIn,
  signInByNegotiate,
  submitNegotiateMfa,
} from '../auth/authentication'
import {oauthAddress, readSignInMethods} from '../auth/methods'
import {failedPasswordRules, type PasswordRuleKey} from '../auth/passwordPolicy'

export type SignInStep = 'credentials' | 'mfa' | 'password'

export type SignInProblem =
  | { kind: 'invalid' }
  | { kind: 'mismatch' }
  | { kind: 'weak' }
  | { kind: 'rejected'; messages: string[] }
  | { kind: 'alreadyEnrolled' }
  | { kind: 'unavailable' }

export function useSignInWizard(
  onSignedIn: () => void,
  navigate: (address: string) => void = (address) => window.location.assign(address),
) {
  const form = reactive({
    userName: '',
    password: '',
    mfa: '',
    newPassword: '',
    repeatNewPassword: '',
  })

  const path = ref<SignInStep[]>(['credentials'])
  const busy = ref(false)
  const problem = ref<SignInProblem>()
  const wirePasswordHash = ref<boolean>()
  const checking = ref(true)
  const redirecting = ref(false)
  const negotiated = ref(false)
  const blocked = ref(false)
  const totpAvailable = ref(false)
  const enrolment = ref<{ qr: string; secret: string }>()
  const enrolling = ref(false)
  let returnAddress = '/'

  const step = computed(() => path.value[path.value.length - 1]!)
  const failedRules = computed<PasswordRuleKey[]>(() => failedPasswordRules(form.newPassword))

  function enter(next: SignInStep): void {
    problem.value = undefined

    if (!path.value.includes(next)) path.value = [...path.value, next]
    else path.value = path.value.slice(0, path.value.indexOf(next) + 1)
  }

  function startAgain(): void {
    form.password = ''
    form.mfa = ''
    form.newPassword = ''
    form.repeatNewPassword = ''
    wirePasswordHash.value = undefined
    problem.value = undefined
    enrolment.value = undefined
    path.value = ['credentials']
  }

  async function start(address = returnAddress): Promise<void> {
    returnAddress = address
    checking.value = true
    blocked.value = false

    const methods = await readSignInMethods()

    totpAvailable.value = methods.totp

    if (methods.oauth) {
      redirecting.value = true
      navigate(oauthAddress(returnAddress))

      return
    }

    if (methods.negotiate) {
      const outcome = await signInByNegotiate()

      if (outcome === 'signedIn') {
        onSignedIn()

        return
      }

      if (outcome === 'mfaRequired') {
        negotiated.value = true
        path.value = ['mfa']
      } else {
        blocked.value = true
      }
    }

    checking.value = false
  }

  async function enrol(): Promise<void> {
    if (enrolling.value || busy.value) return

    problem.value = undefined
    enrolling.value = true

    try {
      if (wirePasswordHash.value === undefined) {
        wirePasswordHash.value = await readWirePasswordHash(form.userName)
      }

      const outcome = await enrolTotp(form, wirePasswordHash.value)

      if (outcome.kind === 'ready') {
        enrolment.value = {
          qr: await QRCode.toDataURL(outcome.enrolment.provisioningUri, {margin: 1, width: 192}),
          secret: outcome.enrolment.secret,
        }
      } else if (outcome.kind === 'invalid') {
        problem.value = {kind: 'invalid'}
      } else if (outcome.kind === 'already') {
        problem.value = {kind: 'alreadyEnrolled'}
      } else {
        problem.value = {kind: 'unavailable'}
      }
    } catch {
      problem.value = {kind: 'unavailable'}
    } finally {
      enrolling.value = false
    }
  }

  async function submitNegotiated(): Promise<void> {
    busy.value = true

    try {
      const outcome = await submitNegotiateMfa(form.mfa)

      if (outcome === 'signedIn') {
        onSignedIn()

        return
      }

      form.mfa = ''
      problem.value = outcome === 'invalid' ? {kind: 'invalid'} : {kind: 'unavailable'}
    } finally {
      busy.value = false
    }
  }

  async function submit(): Promise<void> {
    if (busy.value) return

    problem.value = undefined

    if (negotiated.value) {
      await submitNegotiated()

      return
    }

    if (step.value === 'password') {
      if (form.newPassword !== form.repeatNewPassword) {
        problem.value = {kind: 'mismatch'}
        return
      }

      if (wirePasswordHash.value && failedRules.value.length > 0) {
        problem.value = {kind: 'weak'}
        return
      }
    }

    busy.value = true

    try {
      if (step.value === 'credentials' || wirePasswordHash.value === undefined) {
        wirePasswordHash.value = await readWirePasswordHash(form.userName)
      }

      const outcome = await signIn(form, wirePasswordHash.value, step.value === 'password')

      switch (outcome.kind) {
        case 'signedIn':
          form.password = ''
          form.newPassword = ''
          form.repeatNewPassword = ''
          onSignedIn()
          break
        case 'mfaRequired':
          form.mfa = ''
          enter('mfa')
          break
        case 'passwordChangeRequired':
          enter('password')
          break
        case 'invalid':
          form.mfa = ''
          if (step.value === 'credentials') form.password = ''
          if (step.value === 'password' && path.value.includes('mfa')) enter('mfa')
          problem.value = {kind: 'invalid'}
          break
        case 'rejected':
          problem.value = {kind: 'rejected', messages: outcome.messages}
          break
        case 'failed':
          problem.value = {kind: 'unavailable'}
          break
      }
    } catch {
      problem.value = {kind: 'unavailable'}
    } finally {
      busy.value = false
    }
  }

  return {
    form,
    step,
    path,
    busy,
    problem,
    failedRules,
    wirePasswordHash,
    checking,
    redirecting,
    negotiated,
    blocked,
    totpAvailable,
    enrolment,
    enrolling,
    enrol,
    start,
    submit,
    startAgain,
  }
}

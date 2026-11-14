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

import {beforeEach, describe, expect, it, vi} from 'vitest'
import {useSignInWizard} from '../useSignInWizard'

const methods = vi.hoisted(() => ({read: vi.fn()}))
const authentication = vi.hoisted(() => ({
  negotiate: vi.fn(),
  negotiateMfa: vi.fn(),
  enrol: vi.fn(),
  wire: vi.fn(),
}))

vi.mock('qrcode', () => ({default: {toDataURL: vi.fn(async () => 'data:image/png;base64,QR')}}))

vi.mock('../../auth/methods', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../../auth/methods')>()),
  readSignInMethods: methods.read,
}))

vi.mock('../../auth/authentication', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../../auth/authentication')>()),
  signInByNegotiate: authentication.negotiate,
  submitNegotiateMfa: authentication.negotiateMfa,
  enrolTotp: authentication.enrol,
  readWirePasswordHash: authentication.wire,
}))

describe('useSignInWizard single sign-on', () => {
  const signedIn = vi.fn()
  const navigate = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows the form at once when no single sign-on is on', async () => {
    methods.read.mockResolvedValue({negotiate: false, oauth: false})
    const wizard = useSignInWizard(signedIn, navigate)

    expect(wizard.checking.value).toBe(true)

    await wizard.start('/ask-jooby')

    expect(wizard.checking.value).toBe(false)
    expect(wizard.step.value).toBe('credentials')
    expect(authentication.negotiate).not.toHaveBeenCalled()
    expect(navigate).not.toHaveBeenCalled()
  })

  it('sends the browser to the identity provider and keeps the return address', async () => {
    methods.read.mockResolvedValue({negotiate: false, oauth: true})
    const wizard = useSignInWizard(signedIn, navigate)

    await wizard.start('/preservation/move?tab=2')

    expect(navigate).toHaveBeenCalledWith(
      '/api/Authentication/OAuth?redirectUrl=%2Fpreservation%2Fmove%3Ftab%3D2',
    )
    expect(wizard.redirecting.value).toBe(true)
    expect(wizard.checking.value).toBe(true)
    expect(authentication.negotiate).not.toHaveBeenCalled()
  })

  it('signs in without a form when Negotiate succeeds', async () => {
    methods.read.mockResolvedValue({negotiate: true, oauth: false})
    authentication.negotiate.mockResolvedValue('signedIn')
    const wizard = useSignInWizard(signedIn, navigate)

    await wizard.start('/ask-jooby')

    expect(signedIn).toHaveBeenCalledOnce()
  })

  it('asks only for the code when Negotiate needs one, then signs in', async () => {
    methods.read.mockResolvedValue({negotiate: true, oauth: false})
    authentication.negotiate.mockResolvedValue('mfaRequired')
    authentication.negotiateMfa.mockResolvedValue('signedIn')
    const wizard = useSignInWizard(signedIn, navigate)

    await wizard.start('/ask-jooby')

    expect(wizard.checking.value).toBe(false)
    expect(wizard.negotiated.value).toBe(true)
    expect(wizard.path.value).toEqual(['mfa'])

    wizard.form.mfa = '123456'
    await wizard.submit()

    expect(authentication.negotiateMfa).toHaveBeenCalledWith('123456')
    expect(signedIn).toHaveBeenCalledOnce()
  })

  it('clears a wrong code and reports it', async () => {
    methods.read.mockResolvedValue({negotiate: true, oauth: false})
    authentication.negotiate.mockResolvedValue('mfaRequired')
    authentication.negotiateMfa.mockResolvedValue('invalid')
    const wizard = useSignInWizard(signedIn, navigate)

    await wizard.start('/ask-jooby')
    wizard.form.mfa = '000000'
    await wizard.submit()

    expect(wizard.problem.value).toEqual({kind: 'invalid'})
    expect(wizard.form.mfa).toBe('')
    expect(signedIn).not.toHaveBeenCalled()
  })

  it('explains instead of offering a form when Negotiate does not sign the user in', async () => {
    methods.read.mockResolvedValue({negotiate: true, oauth: false})
    authentication.negotiate.mockResolvedValue('unavailable')
    const wizard = useSignInWizard(signedIn, navigate)

    await wizard.start('/ask-jooby')

    expect(wizard.checking.value).toBe(false)
    expect(wizard.blocked.value).toBe(true)
    expect(wizard.negotiated.value).toBe(false)

    authentication.negotiate.mockResolvedValue('signedIn')
    await wizard.start()

    expect(wizard.blocked.value).toBe(false)
    expect(signedIn).toHaveBeenCalledOnce()
  })
})

describe('useSignInWizard authenticator set up', () => {
  const signedIn = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
    authentication.wire.mockResolvedValue(false)
  })

  it('offers set up only when the server says an authenticator app is the second factor', async () => {
    methods.read.mockResolvedValue({negotiate: false, oauth: false, totp: true})
    const on = useSignInWizard(signedIn)
    await on.start()

    methods.read.mockResolvedValue({negotiate: false, oauth: false, totp: false})
    const off = useSignInWizard(signedIn)
    await off.start()

    expect(on.totpAvailable.value).toBe(true)
    expect(off.totpAvailable.value).toBe(false)
  })

  it('turns the otpauth link into a QR code and keeps the secret to type', async () => {
    methods.read.mockResolvedValue({negotiate: false, oauth: false, totp: true})
    authentication.enrol.mockResolvedValue({
      kind: 'ready',
      enrolment: {provisioningUri: 'otpauth://totp/Jube:alex?secret=ABC', secret: 'ABC'},
    })
    const wizard = useSignInWizard(signedIn)
    await wizard.start()
    wizard.form.userName = 'alex'
    wizard.form.password = 'pw'

    await wizard.enrol()

    expect(authentication.enrol).toHaveBeenCalledWith(wizard.form, false)
    expect(wizard.enrolment.value).toEqual({qr: 'data:image/png;base64,QR', secret: 'ABC'})
    expect(wizard.problem.value).toBeUndefined()
  })

  it('says so when the password is wrong, and shows nothing', async () => {
    methods.read.mockResolvedValue({negotiate: false, oauth: false, totp: true})
    authentication.enrol.mockResolvedValue({kind: 'invalid'})
    const wizard = useSignInWizard(signedIn)
    await wizard.start()

    await wizard.enrol()

    expect(wizard.problem.value).toEqual({kind: 'invalid'})
    expect(wizard.enrolment.value).toBeUndefined()
  })

  it('says so when an authenticator is already set up', async () => {
    methods.read.mockResolvedValue({negotiate: false, oauth: false, totp: true})
    authentication.enrol.mockResolvedValue({kind: 'already'})
    const wizard = useSignInWizard(signedIn)
    await wizard.start()

    await wizard.enrol()

    expect(wizard.problem.value).toEqual({kind: 'alreadyEnrolled'})
  })

  it('forgets the QR code when the user starts again', async () => {
    methods.read.mockResolvedValue({negotiate: false, oauth: false, totp: true})
    authentication.enrol.mockResolvedValue({
      kind: 'ready',
      enrolment: {provisioningUri: 'u', secret: 'S'},
    })
    const wizard = useSignInWizard(signedIn)
    await wizard.start()
    await wizard.enrol()

    wizard.startAgain()

    expect(wizard.enrolment.value).toBeUndefined()
  })
})

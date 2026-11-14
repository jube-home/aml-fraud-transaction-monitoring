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

import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest'
import {resetAntiforgeryToken, setApiHandlers, useApi} from '../useApi'
import {resetWire, setWireSchema, type WireSchema} from '../wire'
import {
  encryptedType,
  openBytes,
  requestAssociatedData,
  responseAssociatedData,
  sealBytes,
  sealRequest,
  wireInnerHeader,
  wireKeyHeader,
  wireTimeHeader,
} from '../wireCrypto'

const interopPrivateKey =
  'MIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQDdgOjUn9Se3dEs8OUKYJLI3xLNJFouoXxAkoNhAszKQJUOiSw4orP2t7EeDVZPcEio/EJ6FkrVcjCIbTUzF+YeynPpQsp0jpidoJPA6xAw3ZNQvavKsUAtV7+fIpUG2J4OYLtHmb40IkAvP6yLoG7mBqa3YjBnkzOWgWFW96SnYR3KuGN+pBOOl06cbyZBXYIQHhuRwuEsHGwu1AguyZn6rMfuKMGeOhTiTFj2OaALm4X+/FjuLIUeHcO0xOSRUbwsVHRgpvrSUdCtoTHiN8yiogukhyiNntuONCkewvq6kOdh1nYFxk4zhREV5ey5S8vCdu5rxx2OEHZ+Sr89eWGJAgMBAAECggEAFi9nIoSvownMfZfTGOr7SKsvEuTlM25xJ3aFOh4xrMAzLbWHfFSvebKERuJHFsXEQUiiz9+TtzUsrz1g6rEmF0dnvKE37dyqZH9EzTm98Wb/7kJRBogiSWhUhPfNvVXM3tvJZi574nTbwN+7xHBZKyjXYd6vwm/35scmnLNJBop/3knMmiUz1hFtWtsgb2cTG3THM4OgseSVVxjvV+/dJfP/rty3QC8i4JL6zmHKbB1swN7MFh/KrMW081rTHYn4MRVjyfEedg28dy53dbzYmY6UdBNzouwOW1PmN5ckvMJCpNEjPYIdu7J3M6PRGuLt/CrIjkd2XGpHDdKElxMiAQKBgQDz+mYIr7j/kOSwZ65rBijNWaw8EUaxghUWDpT82A/UjFwZTl5FDEYwLH2OoRl25Dz5UW4UaPLOUASwqsQB5hApG4X32rqAbv1O8g0bzgg61A6yGqAQYIISXJabj4Ucv8H2NebDI326CA23QSdvsxYtV0x/JbEV5KjuE2w9x5HBWQKBgQDoawLWNVdsKSxSiKnToACowf5T3knKDBq17IVOYsTOGKAGB6xLtM3di51cnBVdfwx1WBV8BXbV+8qxpvNfdOzMMxDA4NZ3TNupojET4wh6UBbv+jppNHTgnkLa9Ob2AgYIw5RHc3gIehVsoA21ZEcOnp3ZKHHj8SQBKNGekibrsQKBgD7RV/gglndm7RrmdygjO9oznn8Qy6zkm11Ef+PiFaFaSlRRsz//+wpoSiOxIwdTmMdwyfEDXJD2VV7PeQwsGjkbVxEMBj0PVrU+6VUhZUkQVIBTDqdo0dS8diKzf0h5rw0fr7zKpJKDhFSMuASwjaM6+P07zS6f40b5RUNa4RJxAoGASOqx1CSeDdgJCMfJiBRtwIREV2mo+tO7WZIf7Iy+M+6z3dBwdLv/zl7r+s8ZQOEINpRxARzxwIajfALl3e/gDtyCiBDCGGGrlNfGz/dthHo2CAoYY0NK2+GrFei5wTq3S+6YSaZnZTlwGUlJo7wXMvX+jc/Lk3jnE9oLkdVGkrECgYBDGKL1RIlXnFfd96c+eP1lqsJkZ4WoBu5Ii/shdkY/eC4nqJTC/wQjOceCMhohG9k/3eYANEbIZP5p0tC0u/FtpPxie65IHMdI8HBeZLgrzSDWgq7khvDo3mRWwLr7qa8QqSGMxgnGxESK1faQ297CBzS5yylQyNc0WG+eIjKNVw=='
const interopWrapped =
  'WchJkHBEDplgYNyx191e4LeIKAJ5PA9e5JcbWYEfLS9mGwGaUhbmO5CIgWRs18ivU4GLOogrlBn7p0syq+hOkf1BxlhhWkZEcYLH0lmo+Ms2s7JNK+zESnbsocAWIarjyc87W5S4fnf8MQV2XS6USBPN7/fj3c9Rs/LC+IRTeSjCgnSRVYV67+wJyxH+UGz0W4AEObf8Zjai58rznmApxYQbFdS00WWhOezKwdrCg1L4Ef9c9wqH5ScHEgCCY9voH2/xvySnZyKxrNqPP+otfbn32Pa7bfFzBas9hSv0UwfLmUlWWXpmK+k1NAZNvX/2T7h/ND6WIue9Vo/3gh0hsg=='
const interopSealed =
  'a0a1a2a3a4a5a6a7a8a9aaabc2f35402df8f6d285d60c30694a71aa624b2c67c364a1d905a6b152d6ff0dc675b7bd55fb8a7b6'

function bytesOf(hex: string): Uint8Array<ArrayBuffer> {
  return Uint8Array.from(hex.match(/../g)!.map((pair) => parseInt(pair, 16)))
}

function fromBase64(text: string): Uint8Array<ArrayBuffer> {
  return Uint8Array.from(atob(text), (character) => character.charCodeAt(0))
}

function toBase64(buffer: ArrayBuffer): string {
  return btoa(String.fromCharCode(...new Uint8Array(buffer)))
}

async function serverKeys() {
  const pair = await crypto.subtle.generateKey(
    {
      name: 'RSA-OAEP',
      modulusLength: 2048,
      publicExponent: new Uint8Array([1, 0, 1]),
      hash: 'SHA-256',
    },
    true,
    ['encrypt', 'decrypt'],
  )

  return {
    privateKey: pair.privateKey,
    publicKey: toBase64(await crypto.subtle.exportKey('spki', pair.publicKey)),
  }
}

async function unwrap(privateKey: CryptoKey, wrapped: string): Promise<CryptoKey> {
  const raw = await crypto.subtle.decrypt({name: 'RSA-OAEP'}, privateKey, fromBase64(wrapped))

  return crypto.subtle.importKey('raw', raw, 'AES-GCM', false, ['encrypt', 'decrypt'])
}

describe('wire encryption', () => {
  it('reads a key wrapped and a message sealed by the same code the server tests pin', async () => {
    const privateKey = await crypto.subtle.importKey(
      'pkcs8',
      fromBase64(interopPrivateKey),
      {name: 'RSA-OAEP', hash: 'SHA-256'},
      false,
      ['decrypt'],
    )
    const key = await unwrap(privateKey, interopWrapped)
    const aad = requestAssociatedData('post', '/api/Rows?x=1', '1700000000000', 'application/json')

    const plain = await openBytes(key, bytesOf(interopSealed), aad)

    expect(new TextDecoder().decode(plain)).toBe('{"id":7,"name":"seven"}')
  })

  it('seals deterministically for a fixed key, nonce and context, matching the pinned bytes', async () => {
    const key = await crypto.subtle.importKey(
      'raw',
      Uint8Array.from({length: 32}, (_, index) => index + 1),
      'AES-GCM',
      false,
      ['encrypt'],
    )
    const nonce = Uint8Array.from({length: 12}, (_, index) => 0xa0 + index)
    const aad = requestAssociatedData('POST', '/api/Rows?x=1', '1700000000000', 'application/json')

    const sealed = await sealBytes(
      key,
      nonce,
      new TextEncoder().encode('{"id":7,"name":"seven"}'),
      aad,
    )

    expect(Buffer.from(sealed).toString('hex')).toBe(interopSealed)
  })

  it('refuses a message opened under a different context', async () => {
    const privateKey = await crypto.subtle.importKey(
      'pkcs8',
      fromBase64(interopPrivateKey),
      {name: 'RSA-OAEP', hash: 'SHA-256'},
      false,
      ['decrypt'],
    )
    const key = await unwrap(privateKey, interopWrapped)

    await expect(
      openBytes(
        key,
        bytesOf(interopSealed),
        requestAssociatedData('POST', '/api/Other', '1700000000000', 'application/json'),
      ),
    ).rejects.toThrow()
  })

  it('wraps a fresh key per request that only the server key opens, and opens the sealed reply', async () => {
    const {privateKey, publicKey} = await serverKeys()

    const sealed = await sealRequest(
      publicKey,
      'POST',
      '/api/Rows?a=1',
      'application/json',
      new TextEncoder().encode('{"secret":"visible-only-inside"}'),
    )

    expect(sealed.headers['Content-Type']).toBe(encryptedType)
    expect(sealed.headers[wireInnerHeader]).toBe('application/json')
    expect(Buffer.from(sealed.body!).toString('utf8')).not.toContain('visible-only-inside')

    const key = await unwrap(privateKey, sealed.headers[wireKeyHeader]!)
    const time = sealed.headers[wireTimeHeader]!
    const plain = await openBytes(
      key,
      new Uint8Array(sealed.body!),
      requestAssociatedData('POST', '/api/Rows?a=1', time, 'application/json'),
    )

    expect(new TextDecoder().decode(plain)).toBe('{"secret":"visible-only-inside"}')

    const reply = await sealBytes(
      key,
      crypto.getRandomValues(new Uint8Array(12)),
      new TextEncoder().encode('{"ok":true}'),
      responseAssociatedData(time, 200, 'application/json'),
    )
    const opened = await sealed.open(
      new Response(reply, {
        status: 200,
        headers: {'Content-Type': encryptedType, [wireInnerHeader]: 'application/json'},
      }),
    )

    expect(new TextDecoder().decode(opened.bytes)).toBe('{"ok":true}')
  })

  it('uses a different key for every request', async () => {
    const {publicKey} = await serverKeys()

    const first = await sealRequest(publicKey, 'GET', '/api/Rows', 'application/json', undefined)
    const second = await sealRequest(publicKey, 'GET', '/api/Rows', 'application/json', undefined)

    expect(first.headers[wireKeyHeader]).not.toBe(second.headers[wireKeyHeader])
    expect(first.body).toBeUndefined()
  })
})

describe('useApi with wire encryption on', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    resetAntiforgeryToken()
    resetWire()
    setApiHandlers({})
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  async function schemaFor(publicKey: string): Promise<WireSchema> {
    return {
      hash: 'enc',
      mode: 'Json',
      encryption: true,
      encryptionPublicKey: publicKey,
      types: {},
      routes: [],
    }
  }

  it('seals a JSON write and a read, and opens the sealed answers', async () => {
    const {privateKey, publicKey} = await serverKeys()

    setWireSchema(await schemaFor(publicKey))

    fetchMock.mockResolvedValueOnce(new Response(JSON.stringify({token: 'tok'}), {status: 200}))
    fetchMock.mockImplementationOnce(async (_url, init) => {
      const headers = init?.headers as Record<string, string>
      const key = await unwrap(privateKey, headers[wireKeyHeader]!)
      const plain = await openBytes(
        key,
        new Uint8Array(init?.body as ArrayBuffer),
        requestAssociatedData('POST', '/api/Thing', headers[wireTimeHeader]!, 'application/json'),
      )

      expect(headers['Content-Type']).toBe(encryptedType)
      expect(headers['X-XSRF-TOKEN']).toBe('tok')
      expect(new TextDecoder().decode(plain)).toBe('{"name":"alex"}')

      const reply = await sealBytes(
        key,
        crypto.getRandomValues(new Uint8Array(12)),
        new TextEncoder().encode('{"saved":true}'),
        responseAssociatedData(headers[wireTimeHeader]!, 200, 'application/json'),
      )

      return new Response(reply, {
        status: 200,
        headers: {'Content-Type': encryptedType, [wireInnerHeader]: 'application/json'},
      })
    })

    const result = await useApi('/api/Thing', {method: 'POST', body: {name: 'alex'}})

    expect(result).toEqual({saved: true})
  })

  it('seals a read with no body and opens an encrypted error body', async () => {
    const {privateKey, publicKey} = await serverKeys()

    setWireSchema(await schemaFor(publicKey))

    fetchMock.mockImplementationOnce(async (_url, init) => {
      const headers = init?.headers as Record<string, string>
      const key = await unwrap(privateKey, headers[wireKeyHeader]!)

      expect(init?.body).toBeUndefined()
      expect(headers['Content-Type']).toBeUndefined()

      const reply = await sealBytes(
        key,
        crypto.getRandomValues(new Uint8Array(12)),
        new TextEncoder().encode('{"errors":{"Name":["Required"]}}'),
        responseAssociatedData(headers[wireTimeHeader]!, 400, 'application/json'),
      )

      return new Response(reply, {
        status: 400,
        headers: {'Content-Type': encryptedType, [wireInnerHeader]: 'application/json'},
      })
    })

    const error = (await useApi('/api/Thing').catch((caught: unknown) => caught)) as {
      fieldErrors: Record<string, string[]>
    }

    expect(error.fieldErrors).toEqual({Name: ['Required']})
  })

  it('never seals an anonymous call or a form upload', async () => {
    const {publicKey} = await serverKeys()

    setWireSchema(await schemaFor(publicKey))
    fetchMock.mockResolvedValue(new Response('{}', {status: 200}))

    await useApi('/api/Authentication/ByUserNamePassword', {
      method: 'POST',
      body: {a: 1},
      anonymous: true,
    })

    const [, anonymous] = fetchMock.mock.calls[0]!
    expect((anonymous?.headers as Record<string, string>)[wireKeyHeader]).toBeUndefined()
  })
})

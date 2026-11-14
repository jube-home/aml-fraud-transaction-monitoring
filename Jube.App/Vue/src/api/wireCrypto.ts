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

export const encryptedType = 'application/x-jube-encrypted'
export const wireKeyHeader = 'X-Wire-Key'
export const wireTimeHeader = 'X-Wire-Time'
export const wireInnerHeader = 'X-Wire-Inner'

const nonceSize = 12
const tagBits = 128
const textEncoder = new TextEncoder()
const importedKeys = new Map<string, Promise<CryptoKey>>()

export interface SealedRequest {
  headers: Record<string, string>
  body: ArrayBuffer | undefined
  open: (response: Response) => Promise<{ bytes: Uint8Array; inner: string }>
}

function subtle(): SubtleCrypto {
  const available = globalThis.crypto?.subtle

  if (available === undefined) {
    throw new Error('Wire encryption needs a secure context (HTTPS or localhost) for WebCrypto.')
  }

  return available
}

function fromBase64(text: string): Uint8Array<ArrayBuffer> {
  const binary = atob(text)
  const bytes = new Uint8Array(binary.length)

  for (let index = 0; index < binary.length; index += 1) bytes[index] = binary.charCodeAt(index)

  return bytes
}

function toBase64(buffer: ArrayBuffer): string {
  let binary = ''

  for (const byte of new Uint8Array(buffer)) binary += String.fromCharCode(byte)

  return btoa(binary)
}

function serverKey(publicKey: string): Promise<CryptoKey> {
  let imported = importedKeys.get(publicKey)

  if (imported === undefined) {
    imported = subtle().importKey(
      'spki',
      fromBase64(publicKey),
      {name: 'RSA-OAEP', hash: 'SHA-256'},
      false,
      ['wrapKey'],
    )
    importedKeys.set(publicKey, imported)
  }

  return imported
}

export function requestAssociatedData(
  method: string,
  target: string,
  time: string,
  inner: string,
): Uint8Array<ArrayBuffer> {
  return textEncoder.encode(`req\n${method.toUpperCase()}\n${target}\n${time}\n${inner}`)
}

export function responseAssociatedData(
  time: string,
  status: number,
  inner: string,
): Uint8Array<ArrayBuffer> {
  return textEncoder.encode(`res\n${time}\n${status}\n${inner}`)
}

export async function sealBytes(
  key: CryptoKey,
  nonce: Uint8Array<ArrayBuffer>,
  plaintext: Uint8Array<ArrayBuffer>,
  associatedData: Uint8Array<ArrayBuffer>,
): Promise<ArrayBuffer> {
  const cipher = new Uint8Array(
    await subtle().encrypt(
      {name: 'AES-GCM', iv: nonce, additionalData: associatedData, tagLength: tagBits},
      key,
      plaintext,
    ),
  )
  const sealed = new Uint8Array(nonceSize + cipher.length)

  sealed.set(nonce, 0)
  sealed.set(cipher, nonceSize)

  return sealed.buffer
}

export async function openBytes(
  key: CryptoKey,
  sealed: Uint8Array<ArrayBuffer>,
  associatedData: Uint8Array<ArrayBuffer>,
): Promise<Uint8Array> {
  if (sealed.length < nonceSize + tagBits / 8) throw new Error('The sealed message is too short.')

  return new Uint8Array(
    await subtle().decrypt(
      {
        name: 'AES-GCM',
        iv: sealed.slice(0, nonceSize),
        additionalData: associatedData,
        tagLength: tagBits,
      },
      key,
      sealed.slice(nonceSize),
    ),
  )
}

export function requestTarget(url: string): string {
  const parsed = new URL(url, globalThis.location?.origin ?? 'http://localhost')

  return parsed.pathname + parsed.search
}

export async function sealRequest(
  publicKey: string,
  method: string,
  url: string,
  inner: string,
  plaintext: Uint8Array<ArrayBuffer> | undefined,
): Promise<SealedRequest> {
  const rsa = await serverKey(publicKey)
  const key = await subtle().generateKey({name: 'AES-GCM', length: 256}, true, [
    'encrypt',
    'decrypt',
  ])
  const wrapped = await subtle().wrapKey('raw', key, rsa, {name: 'RSA-OAEP'})
  const time = String(Date.now())
  const headers: Record<string, string> = {
    [wireKeyHeader]: toBase64(wrapped),
    [wireTimeHeader]: time,
  }

  let body: ArrayBuffer | undefined

  if (plaintext !== undefined) {
    body = await sealBytes(
      key,
      crypto.getRandomValues(new Uint8Array(nonceSize)),
      plaintext,
      requestAssociatedData(method, requestTarget(url), time, inner),
    )
    headers['Content-Type'] = encryptedType
    headers[wireInnerHeader] = inner
  }

  return {
    headers,
    body,
    open: async (response) => {
      const innerType = response.headers.get(wireInnerHeader) ?? ''
      const sealed = new Uint8Array(await response.arrayBuffer())

      return {
        bytes: await openBytes(
          key,
          sealed,
          responseAssociatedData(time, response.status, innerType),
        ),
        inner: innerType,
      }
    },
  }
}

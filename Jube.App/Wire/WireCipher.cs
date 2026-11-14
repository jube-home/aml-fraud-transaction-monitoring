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

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Jube.App.Wire
{
    public sealed class WireCipher : IDisposable
    {
        public const int KeySize = 32;
        public const int NonceSize = 12;
        public const int TagSize = 16;

        private readonly string privatePem;
        private readonly ThreadLocal<RSA> keys;

        public WireCipher(string privateKeyBase64, string publicKeyBase64)
        {
            privatePem = Encoding.UTF8.GetString(Convert.FromBase64String(privateKeyBase64));

            using var rsa = Import(privatePem);

            if (rsa.KeySize < 2048)
            {
                throw new InvalidOperationException(
                    "WireEnvelopeEncryptionPrivateKey must be an RSA key of at least 2048 bits.");
            }

            var configured = PublicKeyOf(Encoding.UTF8.GetString(Convert.FromBase64String(publicKeyBase64)));

            if (!rsa.ExportSubjectPublicKeyInfo().AsSpan().SequenceEqual(Convert.FromBase64String(configured)))
            {
                throw new InvalidOperationException(
                    "WireEnvelopeEncryptionPublicKey is not the public half of WireEnvelopeEncryptionPrivateKey.");
            }

            PublicKey = configured;
            keys = new ThreadLocal<RSA>(() => Import(privatePem));
        }

        public string PublicKey { get; }

        public void Dispose()
        {
            keys.Dispose();
        }

        public byte[] Unwrap(byte[] wrapped)
        {
            try
            {
                var key = keys.Value!.Decrypt(wrapped, RSAEncryptionPadding.OaepSHA256);

                return key.Length == KeySize ? key : null;
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        public static byte[] Seal(byte[] key, byte[] plaintext, byte[] associatedData)
        {
            var result = new byte[NonceSize + plaintext.Length + TagSize];
            var nonce = result.AsSpan(0, NonceSize);
            RandomNumberGenerator.Fill(nonce);

            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plaintext, result.AsSpan(NonceSize, plaintext.Length),
                result.AsSpan(NonceSize + plaintext.Length, TagSize), associatedData);

            return result;
        }

        public static byte[] Open(byte[] key, byte[] sealedData, byte[] associatedData)
        {
            if (sealedData.Length < NonceSize + TagSize)
            {
                throw new CryptographicException("The sealed message is too short.");
            }

            var cipherLength = sealedData.Length - NonceSize - TagSize;
            var plaintext = new byte[cipherLength];

            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(sealedData.AsSpan(0, NonceSize), sealedData.AsSpan(NonceSize, cipherLength),
                sealedData.AsSpan(NonceSize + cipherLength, TagSize), plaintext, associatedData);

            return plaintext;
        }

        public static byte[] RequestAssociatedData(string method, string target, string time, string inner)
        {
            return Encoding.UTF8.GetBytes($"req\n{method.ToUpperInvariant()}\n{target}\n{time}\n{inner}");
        }

        public static byte[] ResponseAssociatedData(string time, int status, string inner)
        {
            return Encoding.UTF8.GetBytes($"res\n{time}\n{status}\n{inner}");
        }

        private static RSA Import(string pem)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);

            return rsa;
        }

        private static string PublicKeyOf(string pem)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pem);

            return Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
        }
    }
}

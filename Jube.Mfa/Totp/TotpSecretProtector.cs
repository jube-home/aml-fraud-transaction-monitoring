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

using System.Security.Cryptography;
using System.Text;

namespace Jube.Mfa.Totp
{
    public static class TotpSecretProtector
    {
        private const int NonceBytes = 12;
        private const int TagBytes = 16;
        private static readonly byte[] info = "jube-totp-secret-v1"u8.ToArray();

        private static byte[] DeriveKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("MfaSecretEncryptionKey is not set.");
            }

            return HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(key), 32, null, info);
        }

        public static string Protect(byte[] secret, string key)
        {
            var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
            var cipher = new byte[secret.Length];
            var tag = new byte[TagBytes];

            using var aes = new AesGcm(DeriveKey(key), TagBytes);
            aes.Encrypt(nonce, secret, cipher, tag);

            return Convert.ToBase64String([.. nonce, .. tag, .. cipher]);
        }

        public static byte[] Unprotect(string protectedSecret, string key)
        {
            var bytes = Convert.FromBase64String(protectedSecret);

            if (bytes.Length <= NonceBytes + TagBytes)
            {
                throw new CryptographicException("The protected secret is too short.");
            }

            var nonce = bytes[..NonceBytes];
            var tag = bytes[NonceBytes..(NonceBytes + TagBytes)];
            var cipher = bytes[(NonceBytes + TagBytes)..];
            var plain = new byte[cipher.Length];

            using var aes = new AesGcm(DeriveKey(key), TagBytes);
            aes.Decrypt(nonce, cipher, tag, plain);

            return plain;
        }
    }
}

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

namespace Jube.Cryptography
{
    using System.Buffers.Binary;
    using System.Security.Cryptography;
    using System.Text;
    using Exceptions;

    public class JempAesEncryption(string password)
    {
        public const int MinimumPasswordLength = 12;

        private const byte FormatVersion = 3;
        private const int Iterations = 600_000;
        private const int MinimumIterations = 100_000;
        private const int MaximumIterations = 5_000_000;
        private const int SaltLength = 16;
        private const int NonceLength = 12;
        private const int TagLength = 16;
        private const int KeyLength = 32;
        private const int MagicLength = 4;
        private const int IterationsOffset = MagicLength + 1;
        private const int SaltOffset = IterationsOffset + 4;
        private const int NonceOffset = SaltOffset + SaltLength;
        private const int HeaderLength = NonceOffset + NonceLength;

        private static readonly byte[] Magic = "JEMP"u8.ToArray();

        public static bool IsAcceptablePassword(string? candidate)
        {
            return candidate is { Length: >= MinimumPasswordLength };
        }

        public byte[] Encrypt(byte[] data)
        {
            if (!IsAcceptablePassword(password))
            {
                throw new InvalidPasswordException(MinimumPasswordLength);
            }

            try
            {
                var header = new byte[HeaderLength];

                Magic.CopyTo(header, 0);
                header[MagicLength] = FormatVersion;
                BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(IterationsOffset, 4), Iterations);
                RandomNumberGenerator.Fill(header.AsSpan(SaltOffset, SaltLength));
                RandomNumberGenerator.Fill(header.AsSpan(NonceOffset, NonceLength));

                var key = DeriveKey(header);
                var cipherText = new byte[data.Length];
                var tag = new byte[TagLength];

                using var gcm = new AesGcm(key, TagLength);
                gcm.Encrypt(header.AsSpan(NonceOffset, NonceLength), data, cipherText, tag, header);

                return [.. header, .. cipherText, .. tag];
            }
            catch (Exception ex)
            {
                throw new InvalidEncryptionException(ex.Message);
            }
        }

        public byte[] Decrypt(byte[] encryptedData)
        {
            try
            {
                if (encryptedData.Length < HeaderLength + TagLength
                    || !encryptedData.AsSpan(0, MagicLength).SequenceEqual(Magic)
                    || encryptedData[MagicLength] != FormatVersion)
                {
                    throw new InvalidDecryptionException("Not a supported Jube export package.");
                }

                var iterations = BinaryPrimitives.ReadInt32BigEndian(encryptedData.AsSpan(IterationsOffset, 4));

                if (iterations is < MinimumIterations or > MaximumIterations)
                {
                    throw new InvalidDecryptionException("Not a supported Jube export package.");
                }

                var header = encryptedData[..HeaderLength];
                var cipherText = encryptedData.AsSpan(HeaderLength, encryptedData.Length - HeaderLength - TagLength);
                var tag = encryptedData.AsSpan(encryptedData.Length - TagLength, TagLength);
                var plainText = new byte[cipherText.Length];

                using var gcm = new AesGcm(DeriveKey(header), TagLength);
                gcm.Decrypt(header.AsSpan(NonceOffset, NonceLength), cipherText, tag, plainText, header);

                return plainText;
            }
            catch (InvalidDecryptionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDecryptionException(ex.Message);
            }
        }

        private byte[] DeriveKey(byte[] header)
        {
            return Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), header.AsSpan(SaltOffset, SaltLength),
                BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(IterationsOffset, 4)), HashAlgorithmName.SHA256,
                KeyLength);
        }
    }
}

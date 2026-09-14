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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Jube.Cryptography;
using Jube.Cryptography.Exceptions;
using Xunit;

namespace Jube.Test.Cryptography
{
    [Trait("Category", "Unit")]
    public sealed class JempAesEncryptionTests
    {
        private const string Password = "unit-test-password";
        private const string Salt = "unit-test-salt";

        private static byte[] BuildLegacyEncryptedPayload(string password, string salt, byte[] plainText)
        {
            var saltBytes = Encoding.UTF8.GetBytes(salt);
            var derived = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 100_000, HashAlgorithmName.SHA256, 48);
            var key = derived[..32];
            var legacyIv = derived[32..48];

            using var hmac = new HMACSHA256(saltBytes);
            var expectedHmac = hmac.ComputeHash(plainText);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = legacyIv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(plainText, 0, plainText.Length);
            }

            return [.. expectedHmac, .. ms.ToArray()];
        }

        [Fact]
        public void EncryptThenDecryptRoundTripsTheOriginalData()
        {
            var data = Encoding.UTF8.GetBytes("some sensitive payload");
            var encryption = new JempAesEncryption(Password, Salt);

            var encrypted = encryption.Encrypt(data);
            var decrypted = encryption.Decrypt(encrypted);

            decrypted.Should().Equal(data);
        }

        [Fact]
        public void EncryptRoundTripsAnEmptyPayload()
        {
            var data = Array.Empty<byte>();
            var encryption = new JempAesEncryption(Password, Salt);

            var encrypted = encryption.Encrypt(data);
            var decrypted = encryption.Decrypt(encrypted);

            decrypted.Should().Equal(data);
        }

        [Fact]
        public void EncryptProducesDifferentCipherTextOnEachCallDueToTheRandomIv()
        {
            var data = Encoding.UTF8.GetBytes("repeatable content");
            var encryption = new JempAesEncryption(Password, Salt);

            var encryptedOne = encryption.Encrypt(data);
            var encryptedTwo = encryption.Encrypt(data);

            encryptedOne.Should().NotEqual(encryptedTwo);
        }

        [Fact]
        public void EncryptPrependsA32ByteHmacAndA16ByteIvBeforeThePkcs7PaddedCipherText()
        {
            var data = Encoding.UTF8.GetBytes("some content");
            var encryption = new JempAesEncryption(Password, Salt);

            var encrypted = encryption.Encrypt(data);

            var paddedCipherTextLength = (data.Length / 16 + 1) * 16;
            encrypted.Length.Should().Be(32 + 16 + paddedCipherTextLength);
        }

        [Fact]
        public void EncryptOfNullDataThrowsInvalidEncryptionException()
        {
            var encryption = new JempAesEncryption(Password, Salt);

            var act = () => encryption.Encrypt(null!);

            act.Should().Throw<InvalidEncryptionException>();
        }

        [Fact]
        public void DecryptWithAWrongPasswordThrowsInvalidDecryptionException()
        {
            var data = Encoding.UTF8.GetBytes("some sensitive payload");
            var encrypted = new JempAesEncryption(Password, Salt).Encrypt(data);
            var wrongPasswordEncryption = new JempAesEncryption("a-different-password", Salt);

            var act = () => wrongPasswordEncryption.Decrypt(encrypted);

            act.Should().Throw<InvalidDecryptionException>();
        }

        [Fact]
        public void DecryptWithAWrongSaltThrowsInvalidDecryptionException()
        {
            var data = Encoding.UTF8.GetBytes("some sensitive payload");
            var encrypted = new JempAesEncryption(Password, Salt).Encrypt(data);
            var wrongSaltEncryption = new JempAesEncryption(Password, "a-different-salt");

            var act = () => wrongSaltEncryption.Decrypt(encrypted);

            act.Should().Throw<InvalidDecryptionException>();
        }

        [Fact]
        public void DecryptOfTamperedCipherTextThrowsInvalidDecryptionException()
        {
            var data = Encoding.UTF8.GetBytes("some sensitive payload");
            var encryption = new JempAesEncryption(Password, Salt);
            var encrypted = encryption.Encrypt(data);
            encrypted[^1] ^= 0xFF;

            var act = () => encryption.Decrypt(encrypted);

            act.Should().Throw<InvalidDecryptionException>();
        }

        [Fact]
        public void DecryptOfTamperedHmacThrowsInvalidDecryptionException()
        {
            var data = Encoding.UTF8.GetBytes("some sensitive payload");
            var encryption = new JempAesEncryption(Password, Salt);
            var encrypted = encryption.Encrypt(data);
            encrypted[0] ^= 0xFF;

            var act = () => encryption.Decrypt(encrypted);

            act.Should().Throw<InvalidDecryptionException>();
        }

        [Fact]
        public void DecryptOfTooShortDataThrowsInvalidDecryptionException()
        {
            var encryption = new JempAesEncryption(Password, Salt);

            var act = () => encryption.Decrypt(new byte[10]);

            act.Should().Throw<InvalidDecryptionException>();
        }

        [Fact]
        public void DecryptOfNullDataThrowsInvalidDecryptionException()
        {
            var encryption = new JempAesEncryption(Password, Salt);

            var act = () => encryption.Decrypt(null!);

            act.Should().Throw<InvalidDecryptionException>();
        }

        [Fact]
        public void DecryptWithoutLegacyFallbackDoesNotRecoverLegacyFormattedData()
        {
            var data = Encoding.UTF8.GetBytes("legacy payload");
            var legacyPayload = BuildLegacyEncryptedPayload(Password, Salt, data);
            var encryption = new JempAesEncryption(Password, Salt);

            var act = () => encryption.Decrypt(legacyPayload);

            act.Should().Throw<InvalidDecryptionException>();
        }

        [Fact]
        public void DecryptWithLegacyFallbackEnabledRecoversLegacyFormattedData()
        {
            var data = Encoding.UTF8.GetBytes("legacy payload");
            var legacyPayload = BuildLegacyEncryptedPayload(Password, Salt, data);
            var encryption = new JempAesEncryption(Password, Salt, true);

            var decrypted = encryption.Decrypt(legacyPayload);

            decrypted.Should().Equal(data);
        }

        [Fact]
        public void DecryptWithLegacyFallbackEnabledStillRecoversCurrentFormatData()
        {
            var data = Encoding.UTF8.GetBytes("current format payload");
            var encryption = new JempAesEncryption(Password, Salt, true);

            var encrypted = encryption.Encrypt(data);
            var decrypted = encryption.Decrypt(encrypted);

            decrypted.Should().Equal(data);
        }

        [Fact]
        public void DecryptWithLegacyFallbackEnabledAndInvalidDataStillThrowsInvalidDecryptionException()
        {
            var encryption = new JempAesEncryption(Password, Salt, true);

            var act = () => encryption.Decrypt(new byte[10]);

            act.Should().Throw<InvalidDecryptionException>();
        }
    }
}
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
using Xunit;

namespace Jube.Test.Cryptography
{
    [Trait("Category", "Unit")]
    public sealed class AesEncryptionTests
    {
        private static byte[] DeriveKey(string key)
        {
            return Rfc2898DeriveBytes.Pbkdf2(key, Encoding.UTF8.GetBytes(key), 100_000, HashAlgorithmName.SHA256, 32);
        }

        private static string Decrypt(string key, string cipherTextBase64)
        {
            var bytes = Convert.FromBase64String(cipherTextBase64);
            var iv = bytes[..16];
            var cipherText = bytes[16..];

            using var aes = Aes.Create();
            aes.Key = DeriveKey(key);
            aes.IV = iv;

            using var ms = new MemoryStream(cipherText);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }

        [Fact]
        public void IvModePropertyReflectsTheValuePassedToTheConstructor()
        {
            var encryption = new AesEncryption("some-key", IvMode.Deterministic);

            encryption.IvMode.Should().Be(IvMode.Deterministic);
        }

        [Fact]
        public void IvModePropertyDefaultsToRandom()
        {
            var encryption = new AesEncryption("some-key");

            encryption.IvMode.Should().Be(IvMode.Random);
        }

        [Fact]
        public void EncryptReturnsCipherTextThatCanBeDecryptedBackToTheOriginalPlainText()
        {
            const string key = "unit-test-key";
            const string plainText = "the quick brown fox jumps over the lazy dog";

            var encryption = new AesEncryption(key);
            var cipherText = encryption.Encrypt(plainText, IvMode.Random);

            Decrypt(key, cipherText).Should().Be(plainText);
        }

        [Fact]
        public void EncryptWithDeterministicModeProducesTheSameCipherTextForTheSamePlainTextAndKey()
        {
            var encryption = new AesEncryption("unit-test-key");

            var cipherTextOne = encryption.Encrypt("repeatable content", IvMode.Deterministic);
            var cipherTextTwo = encryption.Encrypt("repeatable content", IvMode.Deterministic);

            cipherTextOne.Should().Be(cipherTextTwo);
        }

        [Fact]
        public void EncryptWithDeterministicModeProducesDifferentCipherTextForDifferentPlainText()
        {
            var encryption = new AesEncryption("unit-test-key");

            var cipherTextOne = encryption.Encrypt("first message", IvMode.Deterministic);
            var cipherTextTwo = encryption.Encrypt("second message", IvMode.Deterministic);

            cipherTextOne.Should().NotBe(cipherTextTwo);
        }

        [Fact]
        public void EncryptWithDeterministicModeEmbedsTheSha256HashOfThePlainTextAsTheIv()
        {
            const string plainText = "content used for the iv derivation";
            var encryption = new AesEncryption("unit-test-key");

            var cipherText = encryption.Encrypt(plainText, IvMode.Deterministic);

            var embeddedIv = Convert.FromBase64String(cipherText)[..16];
            var expectedIv = SHA256.HashData(Encoding.UTF8.GetBytes(plainText))[..16];

            embeddedIv.Should().Equal(expectedIv);
        }

        [Fact]
        public void EncryptWithRandomModeProducesDifferentCipherTextOnEachCall()
        {
            var encryption = new AesEncryption("unit-test-key");

            var cipherTextOne = encryption.Encrypt("repeatable content", IvMode.Random);
            var cipherTextTwo = encryption.Encrypt("repeatable content", IvMode.Random);

            cipherTextOne.Should().NotBe(cipherTextTwo);
        }

        [Fact]
        public void EncryptWithRandomModeProducesADifferentIvOnEachCall()
        {
            var encryption = new AesEncryption("unit-test-key");

            var ivOne = Convert.FromBase64String(encryption.Encrypt("repeatable content", IvMode.Random))[..16];
            var ivTwo = Convert.FromBase64String(encryption.Encrypt("repeatable content", IvMode.Random))[..16];

            ivOne.Should().NotEqual(ivTwo);
        }

        [Fact]
        public void EncryptWithDifferentKeysProducesDifferentCipherTextForTheSamePlainText()
        {
            var encryptionOne = new AesEncryption("first-key");
            var encryptionTwo = new AesEncryption("second-key");

            var cipherTextOne = encryptionOne.Encrypt("repeatable content", IvMode.Deterministic);
            var cipherTextTwo = encryptionTwo.Encrypt("repeatable content", IvMode.Deterministic);

            cipherTextOne.Should().NotBe(cipherTextTwo);
        }

        [Fact]
        public void EncryptRoundTripsAnEmptyPlainText()
        {
            const string key = "unit-test-key";
            var encryption = new AesEncryption(key);

            var cipherText = encryption.Encrypt("", IvMode.Random);

            Decrypt(key, cipherText).Should().Be("");
        }

        [Fact]
        public void EncryptReturnsValidBase64()
        {
            var encryption = new AesEncryption("unit-test-key");

            var cipherText = encryption.Encrypt("some content", IvMode.Random);

            var act = () => Convert.FromBase64String(cipherText);
            act.Should().NotThrow();
        }
    }
}
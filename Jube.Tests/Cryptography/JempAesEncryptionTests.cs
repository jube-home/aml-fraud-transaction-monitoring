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
using System.Linq;
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
        private const int HeaderLength = 37;
        private const int SaltOffset = 9;

        [Fact]
        public void EncryptThenDecryptRoundTripsTheOriginalData()
        {
            var data = Encoding.UTF8.GetBytes("some sensitive payload");
            var encryption = new JempAesEncryption(Password);

            encryption.Decrypt(encryption.Encrypt(data)).Should().Equal(data);
        }

        [Fact]
        public void EncryptThenDecryptRoundTripsEmptyData()
        {
            var encryption = new JempAesEncryption(Password);

            encryption.Decrypt(encryption.Encrypt([])).Should().BeEmpty();
        }

        [Fact]
        public void DecryptNeedsOnlyThePasswordFromAnotherInstance()
        {
            var data = Encoding.UTF8.GetBytes("moves between environments");
            var encrypted = new JempAesEncryption(Password).Encrypt(data);

            new JempAesEncryption(Password).Decrypt(encrypted).Should().Equal(data);
        }

        [Fact]
        public void EncryptCarriesTheFormatMarkerAndVersionInTheClear()
        {
            var encrypted = new JempAesEncryption(Password).Encrypt([1, 2, 3]);

            Encoding.ASCII.GetString(encrypted, 0, 4).Should().Be("JEMP");
            encrypted[4].Should().Be(3);
            encrypted.Length.Should().Be(HeaderLength + 3 + 16);
        }

        [Fact]
        public void EncryptingTwiceGivesADifferentSaltNonceAndCipherText()
        {
            var data = Encoding.UTF8.GetBytes("same data");
            var encryption = new JempAesEncryption(Password);

            var first = encryption.Encrypt(data);
            var second = encryption.Encrypt(data);

            first.Skip(SaltOffset).Take(16).Should().NotEqual(second.Skip(SaltOffset).Take(16));
            first.Should().NotEqual(second);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("short")]
        [InlineData("elevenchars")]
        public void EncryptRefusesAPasswordShorterThanTheMinimum(string? password)
        {
            var act = () => new JempAesEncryption(password!).Encrypt([1]);

            act.Should().Throw<InvalidPasswordException>().Which.MinimumLength.Should()
                .Be(JempAesEncryption.MinimumPasswordLength);
        }

        [Fact]
        public void EncryptAcceptsAPasswordOfExactlyTheMinimumLength()
        {
            var password = new string('p', JempAesEncryption.MinimumPasswordLength);
            var encryption = new JempAesEncryption(password);

            encryption.Decrypt(encryption.Encrypt([9])).Should().Equal(9);
        }

        [Fact]
        public void IsAcceptablePasswordAppliesTheMinimum()
        {
            JempAesEncryption.IsAcceptablePassword(null).Should().BeFalse();
            JempAesEncryption.IsAcceptablePassword("").Should().BeFalse();
            JempAesEncryption.IsAcceptablePassword(new string('a', 11)).Should().BeFalse();
            JempAesEncryption.IsAcceptablePassword(new string('a', 12)).Should().BeTrue();
        }

        [Fact]
        public void EncryptOfNullDataThrowsInvalidEncryptionException()
        {
            var act = () => new JempAesEncryption(Password).Encrypt(null!);

            act.Should().Throw<InvalidEncryptionException>();
        }

        [Fact]
        public void DecryptWithAWrongPasswordThrowsInvalidDecryptionException()
        {
            var encrypted = new JempAesEncryption(Password).Encrypt([1, 2, 3]);

            var act = () => new JempAesEncryption("a-different-password").Decrypt(encrypted);

            act.Should().Throw<InvalidDecryptionException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(8)]
        [InlineData(SaltOffset)]
        [InlineData(25)]
        [InlineData(HeaderLength)]
        [InlineData(HeaderLength + 1)]
        public void DecryptOfAnyTamperedByteThrowsInvalidDecryptionException(int offset)
        {
            var encryption = new JempAesEncryption(Password);
            var encrypted = encryption.Encrypt(Encoding.UTF8.GetBytes("tamper evident"));
            encrypted[offset] ^= 0x01;

            var act = () => encryption.Decrypt(encrypted);

            act.Should().Throw<InvalidDecryptionException>();
        }

        [Fact]
        public void DecryptOfATamperedTagThrowsInvalidDecryptionException()
        {
            var encryption = new JempAesEncryption(Password);
            var encrypted = encryption.Encrypt([1, 2, 3, 4]);
            encrypted[^1] ^= 0x80;

            var act = () => encryption.Decrypt(encrypted);

            act.Should().Throw<InvalidDecryptionException>();
        }

        [Fact]
        public void DecryptOfATruncatedPackageThrowsInvalidDecryptionException()
        {
            var encryption = new JempAesEncryption(Password);
            var encrypted = encryption.Encrypt([1, 2, 3, 4]);

            var act = () => encryption.Decrypt(encrypted[..^1]);

            act.Should().Throw<InvalidDecryptionException>();
        }

        [Fact]
        public void DecryptOfTooShortDataThrowsInvalidDecryptionException()
        {
            var act = () => new JempAesEncryption(Password).Decrypt([1, 2, 3]);

            act.Should().Throw<InvalidDecryptionException>();
        }

        [Fact]
        public void DecryptOfNullDataThrowsInvalidDecryptionException()
        {
            var act = () => new JempAesEncryption(Password).Decrypt(null!);

            act.Should().Throw<InvalidDecryptionException>();
        }

        [Fact]
        public void DecryptOfAnEarlierReleaseFormatThrowsInvalidDecryptionException()
        {
            var earlierRelease = new byte[200];
            Random.Shared.NextBytes(earlierRelease);

            var act = () => new JempAesEncryption(Password).Decrypt(earlierRelease);

            act.Should().Throw<InvalidDecryptionException>();
        }

        [Fact]
        public void DecryptRefusesAnIterationCountOutsideTheSupportedRange()
        {
            var encryption = new JempAesEncryption(Password);
            var encrypted = encryption.Encrypt([1, 2, 3]);
            encrypted[5] = 0x7F;

            var act = () => encryption.Decrypt(encrypted);

            act.Should().Throw<InvalidDecryptionException>();
        }
    }
}

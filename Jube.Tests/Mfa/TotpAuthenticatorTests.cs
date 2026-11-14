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
using FluentAssertions;
using Jube.Mfa.Totp;
using Xunit;

namespace Jube.Test.Mfa
{
    [Trait("Category", "Unit")]
    public sealed class TotpAuthenticatorTests
    {
        private static readonly byte[] rfcSecret = Encoding.ASCII.GetBytes("12345678901234567890");

        private static DateTime At(long unixSeconds) => DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;

        [Theory]
        [InlineData(59L, "287082")]
        [InlineData(1111111109L, "081804")]
        [InlineData(1111111111L, "050471")]
        [InlineData(1234567890L, "005924")]
        [InlineData(2000000000L, "279037")]
        public void TheRfc6238TestVectorsVerifyAtTheirOwnTime(long unixSeconds, string code)
        {
            TotpAuthenticator.Verify(rfcSecret, code, 0, out var step, At(unixSeconds)).Should().BeTrue();

            step.Should().Be(unixSeconds / 30);
        }

        [Fact]
        public void ACodeFromTheNeighbouringStepIsAcceptedToAllowForClockDrift()
        {
            TotpAuthenticator.Verify(rfcSecret, "287082", 0, out _, At(59 + 30)).Should().BeTrue();
            TotpAuthenticator.Verify(rfcSecret, "287082", 0, out _, At(59 - 30)).Should().BeTrue();
        }

        [Fact]
        public void ACodeFurtherAwayThanOneStepIsRefused()
        {
            TotpAuthenticator.Verify(rfcSecret, "287082", 0, out _, At(59 + 90)).Should().BeFalse();
        }

        [Fact]
        public void ACodeCanNeverBeUsedTwice()
        {
            TotpAuthenticator.Verify(rfcSecret, "287082", 0, out var step, At(59)).Should().BeTrue();

            TotpAuthenticator.Verify(rfcSecret, "287082", step, out _, At(59)).Should().BeFalse();
            TotpAuthenticator.Verify(rfcSecret, "287082", step, out _, At(59 + 30)).Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("12345")]
        [InlineData("1234567")]
        [InlineData("12345a")]
        [InlineData("１２３４５６")]
        public void ACodeThatIsNotSixAsciiDigitsIsRefusedWithoutCalculating(string? code)
        {
            TotpAuthenticator.Verify(rfcSecret, code, 0, out _, At(59)).Should().BeFalse();
        }

        [Fact]
        public void AGeneratedSecretIsTwentyRandomBytes()
        {
            var first = TotpAuthenticator.GenerateSecret();
            var second = TotpAuthenticator.GenerateSecret();

            first.Should().HaveCount(20);
            first.Should().NotEqual(second);
        }

        [Fact]
        public void TheProvisioningUriCarriesTheIssuerAccountSecretAndParameters()
        {
            var uri = TotpAuthenticator.ProvisioningUri("Jube", "ann smith@example.com", rfcSecret);

            uri.Should().StartWith("otpauth://totp/Jube:ann%20smith%40example.com?");
            uri.Should().Contain("secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ")
                .And.Contain("issuer=Jube").And.Contain("digits=6").And.Contain("period=30")
                .And.Contain("algorithm=SHA1");
        }

        [Fact]
        public void ASecretProtectedUnderAKeyComesBackUnderThatKeyOnly()
        {
            var secret = TotpAuthenticator.GenerateSecret();

            var protectedSecret = TotpSecretProtector.Protect(secret, "a long random key");

            protectedSecret.Should().NotContain(Convert.ToBase64String(secret));
            TotpSecretProtector.Unprotect(protectedSecret, "a long random key").Should().Equal(secret);
            ((Action)(() => TotpSecretProtector.Unprotect(protectedSecret, "another key"))).Should()
                .Throw<CryptographicException>();
        }

        [Fact]
        public void ProtectingTheSameSecretTwiceGivesDifferentCiphertext()
        {
            var secret = TotpAuthenticator.GenerateSecret();

            TotpSecretProtector.Protect(secret, "k").Should().NotBe(TotpSecretProtector.Protect(secret, "k"));
        }

        [Fact]
        public void ATamperedProtectedSecretIsRefused()
        {
            var bytes = Convert.FromBase64String(TotpSecretProtector.Protect(TotpAuthenticator.GenerateSecret(), "k"));
            bytes[^1] ^= 0x01;

            ((Action)(() => TotpSecretProtector.Unprotect(Convert.ToBase64String(bytes), "k"))).Should()
                .Throw<CryptographicException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ABlankKeyIsRefusedSoNoSecretIsEverProtectedWithoutOne(string? key)
        {
            ((Action)(() => TotpSecretProtector.Protect(TotpAuthenticator.GenerateSecret(), key!))).Should()
                .Throw<InvalidOperationException>();
        }
    }
}

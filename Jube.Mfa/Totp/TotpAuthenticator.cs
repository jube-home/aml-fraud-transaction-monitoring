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
using OtpNet;

namespace Jube.Mfa.Totp
{
    public static class TotpAuthenticator
    {
        public const int SecretBytes = 20;
        public const int Digits = 6;
        public const int PeriodSeconds = 30;

        public static byte[] GenerateSecret()
        {
            return RandomNumberGenerator.GetBytes(SecretBytes);
        }

        public static string ToBase32(byte[] secret)
        {
            return Base32Encoding.ToString(secret);
        }

        public static string ProvisioningUri(string issuer, string account, byte[] secret)
        {
            var label = Uri.EscapeDataString(issuer) + ":" + Uri.EscapeDataString(account);

            return $"otpauth://totp/{label}?secret={ToBase32(secret)}&issuer={Uri.EscapeDataString(issuer)}" +
                   $"&algorithm=SHA1&digits={Digits}&period={PeriodSeconds}";
        }

        public static bool Verify(byte[] secret, string? code, long lastAcceptedStep, out long step,
            DateTime? at = null)
        {
            step = 0;

            if (string.IsNullOrWhiteSpace(code) || code.Length != Digits || !code.All(char.IsAsciiDigit))
            {
                return false;
            }

            var totp = new OtpNet.Totp(secret);

            if (!totp.VerifyTotp(at ?? DateTime.UtcNow, code, out step, new VerificationWindow(1, 1)))
            {
                return false;
            }

            return step > lastAcceptedStep;
        }
    }
}

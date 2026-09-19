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

namespace Jube.Test.Service.Authentication;

public static class TestRsa
{
    private static readonly Lazy<RSA> keyA = new(() => RSA.Create(2048));
    private static readonly Lazy<RSA> keyB = new(() => RSA.Create(2048));

    public static string PrivateKeyEnv(bool other = false)
    {
        var pem = (other ? keyB : keyA).Value.ExportPkcs8PrivateKeyPem();
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(pem));
    }

    public static string Encrypt(string plain, bool other = false)
    {
        return Convert.ToBase64String((other ? keyB : keyA).Value.Encrypt(Encoding.UTF8.GetBytes(plain),
            RSAEncryptionPadding.OaepSHA256));
    }
}
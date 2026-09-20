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
using System.Threading;
using Jube.Service.Authentication;
using AuthEngine = Jube.Service.Authentication.Authentication;

namespace Jube.Test.Service.Authentication;

public sealed class FastHashScheme : IPasswordHashScheme
{
    private int dummyVerifyCalls;
    private int hashCalls;
    private int verifyCalls;

    public int VerifyCalls => Volatile.Read(ref verifyCalls);
    public int HashCalls => Volatile.Read(ref hashCalls);
    public int DummyVerifyCalls => Volatile.Read(ref dummyVerifyCalls);
    public int RealVerifyCalls => VerifyCalls - DummyVerifyCalls;

    public bool Verify(string? passwordHash, string password, string? key)
    {
        Interlocked.Increment(ref verifyCalls);
        if (passwordHash == Stored(AuthEngine.DummyPassword, key ?? String.Empty))
        {
            Interlocked.Increment(ref dummyVerifyCalls);
        }

        return key != null && passwordHash == Hash(password, key, false);
    }

    public string Hash(string password, string? key)
    {
        return Hash(password, key, true);
    }

    public static string Stored(string password, string key)
    {
        return $"H:{key}:{password}";
    }

    private string Hash(string password, string? key, bool count)
    {
        if (count)
        {
            Interlocked.Increment(ref hashCalls);
        }

        return $"H:{key}:{password}";
    }
}
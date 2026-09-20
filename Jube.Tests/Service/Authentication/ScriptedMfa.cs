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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jube.Service.Authentication;

namespace Jube.Test.Service.Authentication;

public sealed class ScriptedMfa(TimeProvider clock) : IMfaVerifier
{
    private readonly ConcurrentQueue<(string User, string Code, CancellationToken Token)> calls = new();
    private readonly Dictionary<(string, string), DateTimeOffset> valid = new();

    public Exception? Throw { get; set; }
    public IReadOnlyList<(string User, string Code, CancellationToken Token)> Calls => [.. calls];

    public void Allow(string user, string code, TimeSpan? lifetime = null)
    {
        valid[(user, code)] = clock.GetUtcNow() + (lifetime ?? TimeSpan.FromMinutes(1));
    }

    public Task<bool> VerifyAsync(string userName, string code, CancellationToken token = default)
    {
        calls.Enqueue((userName, code, token));
        if (Throw != null)
        {
            throw Throw;
        }

        token.ThrowIfCancellationRequested();

        if (!valid.TryGetValue((userName, code), out var expiry) || clock.GetUtcNow() > expiry)
        {
            return Task.FromResult(false);
        }

        valid.Remove((userName, code));
        return Task.FromResult(true);
    }
}